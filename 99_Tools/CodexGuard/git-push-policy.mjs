import { spawnSync } from 'node:child_process';
import { isAbsolute, resolve } from 'node:path';

function resolveDirectory(cwd, directory) {
  return isAbsolute(directory) ? resolve(directory) : cwd === null ? null : resolve(cwd, directory);
}

function parseGit(argv, cwd) {
  const options = [];
  let index = 0;
  for (; index < argv.length; index++) {
    const word = argv[index];
    if (word === '-C') {
      const directory = argv[++index];
      if (directory === undefined) throw new Error('git -C requires a directory');
      if (directory) cwd = resolveDirectory(cwd, directory);
    } else if (word === '-c') {
      const value = argv[++index];
      if (value === undefined) throw new Error('git -c requires a value');
      options.push('-c', value);
    } else if (/^-(?:C|c).+/.test(word)) {
      if (word[1] === 'C') cwd = resolveDirectory(cwd, word.slice(2));
      else options.push('-c', word.slice(2));
    } else if (/^--(?:git-dir|work-tree)(?:=|$)/.test(word)) {
      options.push(word);
      if (!word.includes('=')) {
        if (argv[index + 1] === undefined) throw new Error(`${word} requires a path`);
        options.push(argv[++index]);
      }
    } else if (word === '--namespace' || word === '--config-env') {
      // These options alter ref/config interpretation; no environment expression is executed.
      throw new Error(`git ${word} is not supported by local destination inspection`);
    } else if (!word.startsWith('-')) break;
  }
  return { command: argv[index], args: argv.slice(index + 1), options, cwd };
}

function parsePush(args) {
  const result = { remote: null, refspecs: [], all: false, mirror: false, tags: false, delete: false, prune: false };
  let repositoryOption = null;
  const valueOptions = new Set(['-o', '--push-option', '--receive-pack', '--exec', '--repo']);
  let positional = false;
  for (let index = 0; index < args.length; index++) {
    const argument = args[index];
    if (argument === '--' && !positional) {
      positional = true;
      continue;
    }
    if (!positional && argument.startsWith('-')) {
      const name = argument.split('=')[0];
      if (valueOptions.has(name)) {
        const value = argument.includes('=') ? argument.slice(argument.indexOf('=') + 1) : args[++index];
        if (value === undefined) throw new Error(`${name} requires a value`);
        if (name === '--repo') repositoryOption = value;
      } else if (argument.startsWith('-o')) {
        // Attached -o values are data, including words that look like a refspec.
      } else if (['--all', '--branches'].includes(argument)) result.all = true;
      else if (argument === '--mirror') result.mirror = true;
      else if (argument === '--tags') result.tags = true;
      else if (argument === '--prune') result.prune = true;
      else if (argument === '--delete' || /^-[^-]*d/.test(argument)) result.delete = true;
      continue;
    }
    if (result.remote === null) result.remote = argument;
    else if (argument === 'tag' && args[index + 1] !== undefined) result.refspecs.push(`refs/tags/${args[++index]}`);
    else result.refspecs.push(argument);
  }
  result.remote ??= repositoryOption;
  return result;
}

function deny(remote, destination) {
  return `Policy deny: Codex cannot update or delete protected branch ${destination} on remote ${remote}.`;
}

function branchDestination(refspec) {
  const clean = refspec.replace(/^\+/, '');
  const destination = clean.includes(':') ? clean.slice(clean.indexOf(':') + 1) : clean;
  if (destination.startsWith('refs/heads/')) return destination.slice(11);
  if (destination.startsWith('refs/')) return null;
  if (destination.startsWith('heads/')) return destination.slice(6);
  return destination;
}

function readLocalRepository(parsed, deadline) {
  function git(args, absentAllowed = false) {
    const remaining = deadline - Date.now();
    if (remaining <= 0) throw new Error('local Git inspection budget expired');
    // Only these read-only subcommands are built here. Never dispatch the input executable,
    // a shell string, a remote helper, a credential helper, fetch, or push.
    const result = spawnSync('git', [...parsed.options, ...args], {
      cwd: parsed.cwd, encoding: 'utf8', timeout: Math.min(3000, remaining), maxBuffer: 2 * 1024 * 1024,
      windowsHide: true, shell: false,
      env: { ...process.env, GIT_OPTIONAL_LOCKS: '0', GIT_TERMINAL_PROMPT: '0', GIT_NO_LAZY_FETCH: '1' },
      stdio: ['ignore', 'pipe', 'pipe'],
    });
    if (result.error || result.signal || (result.status !== 0 && !(absentAllowed && result.status === 1))) {
      throw new Error(`local Git read ${args[0]} failed (${result.error?.code ?? result.signal ?? result.status}): ` +
        (result.stderr ?? '').trim().slice(0, 400));
    }
    return result.stdout ?? '';
  }
  const config = new Map();
  for (const record of git(['config', '--null', '--list']).split('\0').filter(Boolean)) {
    const split = record.indexOf('\n');
    const key = split < 0 ? record : record.slice(0, split);
    const value = split < 0 ? '' : record.slice(split + 1);
    const values = config.get(key) ?? [];
    values.push(value);
    config.set(key, values);
  }
  const refs = new Map();
  for (const line of git(['for-each-ref', '--format=%(refname)%00%(symref)']).split(/\r?\n/).filter(Boolean)) {
    const [name, target] = line.split('\0');
    refs.set(name, target);
  }
  const head = git(['symbolic-ref', '--quiet', 'HEAD'], true).trim();
  const current = head.startsWith('refs/heads/') ? head.slice(11) : null;
  const get = key => config.get(key)?.at(-1);
  const remotes = [...new Set([...config.keys()].flatMap(key =>
    /^remote\.(.+)\.(?:url|pushurl)$/.exec(key)?.[1] ?? []))];
  return { config, refs, current, get, remotes };
}

function rewriteUrl(url, config) {
  let match = null;
  for (const [key, values] of config) {
    const rule = /^url\.(.+)\.(pushinsteadof|insteadof)$/.exec(key);
    if (!rule) continue;
    for (const prefix of values) {
      if (url.startsWith(prefix) && (!match || prefix.length > match.prefix.length)) {
        match = { prefix, replacement: rule[1] };
      }
    }
  }
  return match ? match.replacement + url.slice(match.prefix.length) : url;
}

function canonicalUrl(url, cwd) {
  if (/^[a-z][a-z0-9+.-]*:\/\//i.test(url) || /^[^/\\]+@[^:]+:/.test(url)) return url.replace(/\/$/, '');
  const path = resolve(cwd, url.replace(/\\/g, '/'));
  return process.platform === 'win32' ? path.toLowerCase() : path;
}

function targetRemotes(push, repository, cwd) {
  const { get, current, remotes, config } = repository;
  const selected = push.remote ?? get(`branch.${current}.pushremote`) ?? get('remote.pushdefault') ??
    get(`branch.${current}.remote`) ?? (remotes.length === 1 ? remotes[0] : 'origin');
  const named = remotes.includes(selected);
  // A named remote owns its config and remote HEAD even when transport uses a separate pushurl.
  if (named) return [{ name: selected, configName: selected }];
  const resolved = canonicalUrl(rewriteUrl(selected, config), cwd);
  const matches = remotes.filter(name => {
    const urls = [...(config.get(`remote.${name}.url`) ?? []), ...(config.get(`remote.${name}.pushurl`) ?? [])];
    return urls.some(candidate => canonicalUrl(rewriteUrl(candidate, config), cwd) === resolved);
  });
  // A literal URL can borrow local HEAD evidence, but does not select that remote's push config.
  return matches.length ? matches.map(name => ({ name, configName: null })) :
    [{ name: selected, configName: null, unmatched: true }];
}

function wildcardMatches(pattern, value) {
  const expression = pattern.split('*').map(part => part.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')).join('(.*)');
  return new RegExp(`^${expression}$`).exec(value);
}

function reachesBranch(refspec, branch, push, repository, remote) {
  const clean = refspec.replace(/^\+/, '');
  if (clean === ':') {
    return repository.refs.has(`refs/heads/${branch}`) && repository.refs.has(`refs/remotes/${remote}/${branch}`);
  }
  const colon = clean.indexOf(':');
  const source = colon < 0 ? clean : clean.slice(0, colon);
  let destination = colon < 0 ? clean : clean.slice(colon + 1);
  if (!destination) destination = source;
  if (destination === 'HEAD' || destination === '@') {
    if (!repository.current) throw new Error('cannot resolve an implicit HEAD destination from detached HEAD');
    destination = `refs/heads/${repository.current}`;
  }
  if (colon < 0 && !push.delete && !destination.startsWith('refs/') &&
      repository.refs.has(`refs/tags/${destination}`) && !repository.refs.has(`refs/heads/${destination}`)) {
    return false;
  }
  if (destination.startsWith('heads/')) destination = `refs/${destination}`;
  else if (!destination.startsWith('refs/')) destination = `refs/heads/${destination}`;
  const match = wildcardMatches(destination, `refs/heads/${branch}`);
  if (!match) return false;
  if (!destination.includes('*') || push.delete || push.prune || !source) return true;
  let sourceRef = source.replace('*', match[1]);
  if (!sourceRef.startsWith('refs/')) sourceRef = `refs/heads/${sourceRef}`;
  return repository.refs.has(sourceRef);
}

function defaultRefspecs(push, target, repository) {
  const { config, get, current } = repository;
  const configured = target.configName && config.get(`remote.${target.configName}.push`);
  if (push.refspecs.length) return push.refspecs;
  if (push.all || push.mirror || push.tags) return [];
  if (configured?.length) return configured;
  const mode = get('push.default') ?? 'simple';
  if (mode === 'nothing') return [];
  if (mode === 'matching') return [':'];
  if (!current) throw new Error('cannot determine push destination from detached HEAD');
  const upstreamRemote = get(`branch.${current}.remote`);
  const merge = get(`branch.${current}.merge`);
  if (mode === 'upstream' || mode === 'tracking') {
    if (!merge || upstreamRemote !== target.name) {
      throw new Error('push upstream destination is not locally established');
    }
    return [`HEAD:${merge}`];
  }
  if (mode !== 'current' && mode !== 'simple') throw new Error(`unknown push.default mode ${mode}`);
  if (mode === 'simple' && upstreamRemote === target.name && merge && merge !== `refs/heads/${current}`) {
    throw new Error('push.default=simple upstream branch name differs from the current branch');
  }
  return [`HEAD:refs/heads/${current}`];
}

export function evaluateGitPush(argv, cwd, budget) {
  const parsed = parseGit(argv, cwd);
  if (parsed.command !== 'push') return null;
  const push = parsePush(parsed.args);
  // Explicit main destinations do not depend on remote-HEAD evidence or successful Git reads.
  if (push.refspecs.some(refspec => branchDestination(refspec) === 'main')) {
    return deny(push.remote ?? '(configured remote)', 'main');
  }
  if (parsed.cwd === null) throw new Error('push execution directory is not an explicit path');
  const repository = readLocalRepository(parsed, budget.deadline);
  for (const target of targetRemotes(push, repository, parsed.cwd)) {
    const mirror = push.mirror || (target.configName &&
      /^(?:true|yes|on|1)$/i.test(repository.get(`remote.${target.configName}.mirror`) ?? 'false'));
    const refspecs = defaultRefspecs(push, target, repository);
    // --all supplies refs/heads/*; --prune also removes destination branches absent locally.
    const reaches = branch => mirror || (push.all && (push.prune || repository.refs.has(`refs/heads/${branch}`))) ||
      refspecs.some(refspec => reachesBranch(refspec, branch, push, repository, target.name));
    if (reaches('main')) return deny(target.name, 'main');
    const remotePrefix = `refs/remotes/${target.name}/`;
    const head = repository.refs.get(`${remotePrefix}HEAD`);
    if (!head?.startsWith(remotePrefix) || !repository.refs.has(head)) {
      const recovery = target.unmatched ?
        'Have a person identify a configured remote for this URL and verify its default branch; ' +
          'then restore git remote set-head <that-remote-name> <verified-default-branch>' :
        `After a person verifies this remote's default branch, restore the local symbolic ref with ` +
          `git remote set-head "${target.name}" <verified-default-branch>`;
      throw new Error(`remote ${target.name}: default branch cannot be confirmed from local remote HEAD. ` +
        `${recovery} (the tracking ref must already exist). ` +
        'No network query or automatic refs/config repair was performed');
    }
    const defaultBranch = head.slice(remotePrefix.length);
    if (reaches(defaultBranch)) return deny(target.name, defaultBranch);
  }
  return null;
}
