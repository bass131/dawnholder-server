import { spawn } from 'node:child_process';
import { writeFile } from 'node:fs/promises';
import { join, resolve } from 'node:path';

export class Execution {
  constructor(directory, cwd) {
    this.directory = directory;
    this.cwd = cwd;
    this.commands = [];
  }

  async run(executable, args, { cwd = this.cwd, input, timeoutMs = 180000 } = {}) {
    if (this.cancelled) throw new Error('Execution was cancelled; no further commands will run.');
    const id = String(this.commands.length + 1).padStart(4, '0');
    const record = { id, executable, args, cwd, timeoutMs, startedAt: new Date().toISOString() };
    this.commands.push(record);
    const stdout = [];
    const stderr = [];
    let timedOut = false;
    let failure = null;
    let timer;
    let child;
    const terminate = () => {
      if (!child?.pid) return;
      if (process.platform === 'win32') {
        // Only the PID created by this invocation and its descendants are owned here.
        const killer = spawn('taskkill.exe', ['/PID', String(child.pid), '/T', '/F'], {
          windowsHide: true, stdio: 'ignore', shell: false,
        });
        killer.on('error', error => { failure = `Cleanup failed: ${error.message}`; });
      } else {
        try {
          process.kill(-child.pid, 'SIGKILL');
        } catch (error) {
          if (error.code !== 'ESRCH') failure = `Cleanup failed: ${error.message}`;
        }
      }
    };
    const cancelled = () => {
      this.cancelled = true;
      failure = 'Cancelled';
      terminate();
    };
    const exit = await new Promise(resolveExit => {
      try {
        child = spawn(executable, args, {
          cwd, shell: false, windowsHide: true, detached: process.platform !== 'win32',
          stdio: ['pipe', 'pipe', 'pipe'],
        });
      } catch (error) {
        failure = error.message;
        resolveExit(null);
        return;
      }
      process.once('SIGINT', cancelled);
      process.once('SIGTERM', cancelled);
      child.stdout.on('data', data => stdout.push(data));
      child.stderr.on('data', data => stderr.push(data));
      child.stdin.on('error', error => {
        if (error.code !== 'EPIPE') failure = error.message;
      });
      child.on('error', error => { failure = error.message; });
      child.on('close', (code, signal) => {
        record.signal = signal;
        resolveExit(code);
      });
      timer = setTimeout(() => {
        timedOut = true;
        terminate();
      }, timeoutMs);
      child.stdin.end(input);
    });
    clearTimeout(timer);
    process.removeListener('SIGINT', cancelled);
    process.removeListener('SIGTERM', cancelled);
    Object.assign(record, {
      exit, timedOut, error: failure, finishedAt: new Date().toISOString(),
      stdout: `${id}.stdout.txt`, stderr: `${id}.stderr.txt`,
    });
    const out = Buffer.concat(stdout);
    const err = Buffer.concat(stderr);
    await writeFile(join(this.directory, record.stdout), out, { flag: 'wx' });
    await writeFile(join(this.directory, record.stderr), err, { flag: 'wx' });
    await writeFile(join(this.directory, `${id}.command.json`), `${JSON.stringify(record, null, 2)}\n`, { flag: 'wx' });
    return { record, stdout: out, stderr: err, ok: exit === 0 && !timedOut && !failure };
  }

  async linuxPath(path, distribution) {
    if (!distribution) return resolve(path);
    const converted = await this.run('wsl.exe', [
      '-d', distribution, '--exec', 'wslpath', '-a', '-u', resolve(path).replaceAll('\\', '/'),
    ]);
    if (!converted.ok) throw new Error(`WSL path conversion failed: ${converted.stderr}`);
    const value = converted.stdout.toString('utf8').trim();
    if (!value.startsWith('/') || value.includes('\n')) throw new Error('Invalid WSL path result.');
    return value;
  }

  async python(executable, args, distribution, options = {}) {
    if (!distribution) return this.run(executable, args, options);
    const cwd = await this.linuxPath(options.cwd ?? this.cwd, distribution);
    return this.run('wsl.exe', ['-d', distribution, '--cd', cwd, '--exec', executable, ...args], options);
  }
}
