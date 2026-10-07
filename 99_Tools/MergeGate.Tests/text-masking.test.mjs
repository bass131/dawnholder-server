// Independent regressions for the text masking of behavior contract v3.2 (E/merge-gate-behavior-spec-v3.2.md,
// SHA256 69d3b1c9…fce66) and the T1 change it makes to the last net. Every expected decision and code
// below is read from that contract, not from 99_Tools/MergeGate:
// - §4 「글 가리기(v3)」 (user approval 1A, msg_9dc312f58c0f: only characters that are not run leave the
//   net). Two kinds of text are masked: (1) the body of a heredoc whose delimiter is quoted, received by a
//   plain lower-case cat or tee or by git commit -F -/--file -/--file=-, whose stdout is not piped and
//   which is not inside $( ) or backticks; (2) the quoted text of echo and printf arguments (unless the
//   command pipes into a command outside tee cat head tail wc cut grep sort uniq), of git commit
//   -m/--message, of gh pr create/edit -t/--title/-b/--body and of orca orchestration send/reply
//   --subject/--body. $( ), backticks and ${ } inside double quotes stay code, and only plain lower-case
//   command names count. A command whose reading fails (unclosed quote or heredoc) or that has a process
//   substitution is not masked at all; a here-string is never masked.
// - 「실행기 찾기(v3.1)」 (main decision msg_4e602fb57806): when the masked string still has a runner word
//   (shells, interpreters such as node and python, source eval exec xargs env sudo command and the rest of
//   the list, with a path or .exe allowed), a word starting with ./ ../ or ~/, or a simple command whose
//   command word is . or a path, the whole command is judged unmasked, as in v2.3. Runner words inside the
//   masked text do not count.
// - v3.2 (lead decisions on the defects V1 and V2 of the first verification, E/verify/verdict.md, and main
//   decision msg_93a8d89ec8b1 on its observation O1) only blocks more:
//   - 「bash와 다르게 읽을 수 있는 꼴(v3.2)」: $' or $" outside quotes and heredoc bodies, a quote or a
//     backslash inside a backtick substitution, a CR anywhere, or a space-like character other than space,
//     tab and newline outside quotes and heredoc bodies is a reading failure, so nothing is masked.
//   - 「묶음 뒤 파이프(v3.2)」: a | or |& after the closing ) or } of a group pipes the stdout of every simple
//     command in it, nested groups included, and a | followed by a group flows into code.
//   - 「실행기 찾기」 4·5 (v3.2): a command word holding $ or a backtick before quote removal, also inside a
//     command substitution, or the words gh and api together in the masked string turn the masking off.
// - §4 「Bash — 병합 시도 판정」: checks 1–3 and the last net read the masked string; check 4 (approval
//   injection), check 5 (state folder) and the pass conditions read the original string.
// - §4 「마지막 그물」: v3 adds the decision-none rows of the masking table and, in v3.1, the seven false
//   positives of goal 「오탐 집계」 without a runner to the everyday forms; the three with a runner stay
//   accepted false positives. Condition 3 drops a leading + before comparing (T1).
// - §4 Monitor: the Bash judgement, except that a merge attempt is always non-bash-merge.
// Every form runs for Bash in a main checkout holding a valid unused record, Bash by a subagent, Monitor
// and Bash without the marker, from a repository on a feature branch like the sessions that met the
// false positives, so a push without refspec looks up a branch that is not main.
// Before the v3.1 implementation the pass forms and T1 failed; the kept blocks passed before and after. On
// the v3.1 implementation the v3.2 kept blocks fail where it still masks the text; the v3.2 contrast rows
// get no decision before and after the v3.2 change.
// Left out: forms the contract does not settle (for example `<< 'EOF'` with a space, git global options
// before commit, upper-case runner words), run paths outside the closed runner list, which §6 keeps as an
// intended limit, and bash quoting edges v3.2 does not name: §4 「bash와 다르게 읽을 수 있는 꼴」 lets an
// implementation also treat such a form as a reading failure (and report it), so no row expects a decision
// none from one.
//
// Environment: node and git. Each test owns a temporary project passed as CLAUDE_PROJECT_DIR; one
// read-only feature branch repository serves as the input cwd of every test in this file.
import assert from 'node:assert/strict';
import { mkdtemp } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { after, before, test } from 'node:test';

import {
  approval, assertDeny, assertNoDecision, bashInput, createProject, createRepository, HEAD_A, monitorInput, PR,
  readApprovals, removeTree, runHook, writeApprovals,
} from './hook-fixture.mjs';

const tail = `${PR} --merge --match-head-commit ${HEAD_A}`;
const exactMerge = `gh pr merge ${tail}`;
const lines = (...parts) => parts.join('\n');

let featureBranchCwd;
let repositoryRoot;
before(async () => {
  repositoryRoot = await mkdtemp(join(tmpdir(), 'merge-gate-'));
  featureBranchCwd = await createRepository(repositoryRoot, 'repo-feature', 'feat/x');
});
after(() => removeTree(repositoryRoot));

// What one run decided, read from the §2 output: 'none' for an empty stdout with exit 0, else the code.
function decided(run) {
  if (run.exit === 0 && run.stdout === '') return 'none';
  try {
    const reason = JSON.parse(run.stdout).hookSpecificOutput?.permissionDecisionReason ?? '';
    return reason.split(' ')[0].replace(/^merge-gate:/, '');
  } catch {
    return `unreadable output (exit ${run.exit}): ${run.stdout}${run.stderr}`;
  }
}

// One command for every caller. The decisions are compared together first, so a failure shows all four.
async function assertDecisions(t, command, bash, monitor = bash) {
  const marked = await createProject(t);
  const written = await writeApprovals(marked.project, [approval()]);
  const unmarked = await createProject(t, { marker: false });
  const cwd = featureBranchCwd;
  const runs = {
    'Bash': runHook(marked, bashInput(marked.project, command, { cwd })),
    'Bash (subagent)': runHook(marked, bashInput(marked.project, command, { cwd, agent: true })),
    'Monitor': runHook(marked, monitorInput(marked.project, command, { cwd })),
    'Bash (no marker)': runHook(unmarked, bashInput(unmarked.project, command, { cwd })),
  };
  const expected = { 'Bash': bash, 'Bash (subagent)': bash, 'Monitor': monitor, 'Bash (no marker)': bash };
  const observed = Object.fromEntries(Object.entries(runs).map(([caller, run]) => [caller, decided(run)]));
  assert.deepEqual(observed, expected, `decisions for ${JSON.stringify(command)}`);
  for (const [caller, run] of Object.entries(runs)) {
    if (expected[caller] === 'none') assertNoDecision(run, caller);
    else assertDeny(run, expected[caller], caller);
  }
  assert.deepEqual(await readApprovals(marked.project), written, 'the record stays unused');
}

// Shortened forms of the ten false positives of goal 「오탐 집계」 (E/census/block-census.json rows with
// hookDenial, code suspect-words, from 10-07). The repository is public, so personal paths and note text
// are neutral; each form keeps what the judgement reads: the receiving command and its links before and
// after, the quote kinds and the heredoc delimiter quotes, nested substitutions in double quotes, the net
// words where they stood, and runner words only where the original had them.
const observedWithoutRunner = [
  // 10-07 05:52:41Z, Rules lead. git add && git commit -q -F - <<'EOF' whose body holds main; after the
  // heredoc a push without refspec piped into tail, then ; and a pipe into head.
  ['05:52:41Z, commit message heredoc then a push without refspec', lines(
    "git add docs/goal.md && git commit -q -F - <<'EOF'",
    'docs: record the gate checks in the main window and lead pane',
    '',
    'Co-Authored-By: Example <noreply@example.invalid>',
    'EOF',
    'git log --oneline -1 && git push -q 2>&1 | tail -2; git status -sb | head -1',
  )],
  // 10-07 08:42:13Z, main. echo of a single-quoted line holding gh and create·merge, >> to a file, && tail.
  ['08:42:13Z, echo of a single-quoted line appended to a file',
    "echo '- [메모 제목](note.md) — gh pr create·merge는 확인 창 규칙이라 리드 pane이 멈추고 알림이 안 온다' " +
    '>> notes/INDEX.md && tail -2 notes/INDEX.md'],
  // 10-07 09:53:19Z, Rules lead. Assignment && printf '%s\n' with a double-quoted argument holding
  // $(date …), escaped backticks around branch names with /, push, gh and merge; >> $E/file && tail | cut ;
  // git diff | tail.
  ['09:53:19Z, printf with $(date) and escaped backticks appended to a file', 'E=work/evidence && printf \'%s\\n\' ' +
    '"- 사후 변경($(date -u +%FT%TZ), push 전): branch 이름을 \\`docs/merge-gate-closeout\\`에서 \\`docs/gate-closeout\\`로 ' +
    '바꿨다. 이름에 merge가 들어 있으면 뒤의 gh 명령이 그물(gh·merge 낱말 조합)에 걸릴 수 있어서다." >> $E/context.md && ' +
    'tail -2 $E/context.md | cut -c1-200; git diff docs/CURRENT.md | tail -4'],
  // 10-07 10:32:37Z, Management lead. Assignment && git diff | grep | head ; cat > $E/file <<'EOF' whose
  // body holds merge and main; then git add && git commit -F file && a push of a work branch | tail ; …
  ['10:32:37Z, commit message file heredoc then a work branch push', lines(
    "E=work/evidence && git diff docs/CURRENT.md | grep \"^[-+]\" | head -4; cat > $E/commit-1.txt <<'EOF'",
    'docs(part): record the PR1 merge and the completion check',
    '',
    'The goal records the PR1 merge by main after the approval.',
    '',
    'Co-Authored-By: Example <noreply@example.invalid>',
    'EOF',
    'git add docs/CURRENT.md docs/goal.md && git commit -q -F $E/commit-1.txt && git log --oneline -1 && ' +
      'git push -q -u origin docs/part-closeout 2>&1 | tail -2; git status -sb | head -2',
  )],
  // 10-07 10:55:27Z, Management lead. cat >> file <<'EOF' whose long report body holds markdown tables
  // with |, an escaped \|, backticks, main, push and the words node and gh (a runner word that stands only
  // in masked text, 「실행기 찾기(v3.1)」); then date ; git status.
  ['10:55:27Z, report appended through a heredoc', lines(
    "cat >> work/evidence/gardener-report.md <<'EOF'",
    '',
    '---',
    '',
    '# 점검 결과',
    '',
    '| 검사 | 명령·범위·원문 | 수행 |',
    '|---|---|---|',
    '| 기준 대조 | `git merge-base HEAD main` = `0000000…`, `origin/main` 같음 | 일치 |',
    '| diff 동일성 | `git diff 1111111 2222222 \\| sha256sum` | 일치 |',
    '| 병합 사실 | 읽기 전용 node로 목록 재해시, `gh pr view 1`의 state·mergedAt | 일치 |',
    '',
    '- 실제 쓰기 파일: 보고서 한 파일. commit·push·PR·병합은 하지 않았다.',
    'EOF',
    'date -u +%Y-%m-%dT%H:%M:%SZ; git status --short; git status --short --ignored work/evidence/gardener-report.md',
  )],
  // 10-07 10:58:18Z, Rules lead. git fetch origin main | tail ; a { echo …; } group piped into tee whose
  // double-quoted echo arguments nest $(…) with single quotes inside, one of them piping into tr, and a
  // gh pr view with a nested --jq; then cat > file <<'EOF' whose body holds push and gh.
  ['10:58:18Z, echo group piped into tee then a heredoc to a file', lines(
    'E=work/evidence && git fetch origin main 2>&1 | tail -1; { ' +
      'echo "# PR1 병합 리드 대조 $(date -u +%FT%TZ)"; ' +
      'echo "origin/main: $(git log -1 --format=\'%H parents=%P committed=%cI\' origin/main)"; ' +
      'echo "second parent: $(git rev-parse 1111111^2) (승인 head 2222222)"; ' +
      'echo "tree diff head..merge (files): \'$(git diff --name-only 2222222 1111111 | tr \'\\n\' \' \')\'"; ' +
      'echo "gh: $(gh pr view 1 --json state,mergedAt,headRefOid ' +
      '--jq \'"\\(.state) mergedAt=\\(.mergedAt) headRefOid=\\(.headRefOid)"\')"; ' +
      'echo "remote branch docs/part-closeout: \'$(git ls-remote --heads origin docs/part-closeout)\'"; ' +
      "} | tee $E/merged-check.txt; cat > $E/session/manual-check.md <<'EOF'",
    '# 수신 대조',
    '',
    '- 병합 사실은 리드가 git·gh로 따로 확인했다.',
    '- 판정: 처리한다. 지시대로 추적 파일 쓰기·push·새 작업자 기동 없이 대기한다.',
    'EOF',
    'git status --short | head -3; git branch --show-current',
  )],
  // 10-07 12:38:15Z, Rules lead. cd && assignments && git fetch origin main ; a { echo …; } group > file
  // whose double-quoted echo arguments hold merge and nest $([ "$(…)" = … ] && echo yes || echo no);
  // gh pr view with a nested --jq >> file ; echo with '$(…)' >> file ; cat.
  ['12:38:15Z, echo group redirected to a file next to gh pr view', 'cd work/repo && E=work/evidence && ' +
    'git fetch -q origin main; M=1111111; { ' +
    'echo "# PR1 병합 리드 대조 $(date -u +%FT%TZ)"; ' +
    'echo "origin/main: $(git log -1 --format=\'%H parents=%P committed=%cI\' origin/main)"; ' +
    'echo "merge commit parents: $(git log -1 --format=%P $M)"; ' +
    'echo "second parent equals approved head 2222222: $([ "$(git rev-parse $M^2)" = 2222222 ] && echo yes || echo no)"; ' +
    'echo "merge tree $(git rev-parse $M^{tree}) vs simulated 3333333"; ' +
    '} > $E/merged-check.txt; gh pr view 1 --json state,mergedAt,headRefOid ' +
    '--jq \'"gh: \\(.state) mergedAt=\\(.mergedAt) headRefOid=\\(.headRefOid)"\' >> $E/merged-check.txt; ' +
    'echo "remote branch docs/part-intake: \'$(git ls-remote --heads origin docs/part-intake)\'" >> $E/merged-check.txt; ' +
    'cat $E/merged-check.txt'],
];

for (const [label, command] of observedWithoutRunner) {
  test(`§4 「마지막 그물」 v3.1 everyday form, 10-07 false positive ${label}: no decision`,
    t => assertDecisions(t, command, 'none'));
}

// 「글 가리기(v3)」 example table, the decision-none rows, the other masked places of the same section, and
// runner words that stand only inside masked text (「실행기 찾기(v3.1)」 does not count them).
const maskedForms = [
  ['table: heredoc to a file with push and main in the body', lines("cat > notes.md <<'EOF'", '- push 전에 main을 확인한다.', 'EOF')],
  ['table: git commit -q -F - heredoc with push and main in the body', lines("git commit -q -F - <<'EOF'", 'docs: note the push to main', 'EOF')],
  ['table: echo of a single-quoted gh·merge note', "echo '- gh pr create·merge 메모' >> notes.md"],
  ['table: printf with $(date) inside the double quotes', `printf '%s\\n' "push 전 $(date -u +%FT%TZ) gh merge" >> notes.md`],
  ['table: gh pr create with a quoted title', 'gh pr create --title "merge gate" --body "draft"'],
  ['table: git commit -m with push to main', 'git commit -m "fix(gate): push to main"'],
  ['table (v3.1): heredoc to a file with node, push and main in the body',
    lines("cat > notes.md <<'EOF'", 'node tools/board.mjs와 bash 스크립트는 나중에 돌린다. push 전 main 확인.', 'EOF')],
  ['heredoc with a double-quoted delimiter', lines('cat > notes.md <<"EOF"', 'gh merge 메모', 'EOF')],
  ['<<- heredoc with a tab-indented end', lines("cat > notes.md <<-'EOF'", '\tpush 전 main 확인', '\tEOF')],
  ['redirect after the heredoc operator', lines("cat <<'EOF' > notes.md", 'push 전 main 확인', 'EOF')],
  ['tee with its output redirected to a file', lines("tee -a notes.md <<'EOF' > /dev/null", 'gh pr merge 메모', 'EOF')],
  ['git commit --file -', lines("git commit --file - <<'EOF'", 'push to main', 'EOF')],
  ['git commit --file=-', lines("git commit --file=- <<'EOF'", 'gh merge note', 'EOF')],
  ['echo of a double-quoted argument', 'echo "gh merge 메모" >> notes.md'],
  ['echo piped into tee, which is on the pass list', "echo 'git push origin main' | tee -a notes.md"],
  ['printf piped into head, which is on the pass list', "printf '%s\\n' 'gh merge 메모' | head -1 >> notes.md"],
  ['git commit -m with single quotes', "git commit -m 'gh merge note'"],
  ['git commit --message <value>', 'git commit --message "push to main"'],
  ['git commit --message=<value>', 'git commit --message="push to main"'],
  ['gh pr edit -t and -b', `gh pr edit ${PR} -t 'merge gate' -b "push to main"`],
  ['gh pr create --title= and --body=', 'gh pr create --title="merge gate" --body="push to main"'],
  ['gh pr create with --base main and a body from $(cat file)', 'gh pr create --base main --head feat/x --title "merge gate" --body "$(cat body.md)"'],
  ['orca orchestration send --subject and --body', 'orca orchestration send --to term_x --type status --subject "[Rules Astra] gh merge 메모" --body "push 전 main 확인"'],
  ['orca orchestration reply --body', `orca orchestration reply --id msg_x --body 'gh pr merge ${PR} 준비 완료'`],
  ['a here-string elsewhere in the command leaves the echo masked', "echo 'gh merge 메모' >> notes.md && wc -l <<< \"line\""],
  ['|| is not a pipe, so tr after it leaves the echo masked', "echo 'git push origin main' >> notes.md || tr -d x < notes.md"],
  ['runner word inside a git commit -m value', 'git commit -m "run node later; push to main"'],
  ['runner word inside an echo argument', "echo 'bash x.sh 뒤 gh merge 확인' >> notes.md"],
  ['./ path and runner word inside a gh pr create --body value', 'gh pr create --title "merge gate" --body "run ./x.sh with bash, then push to main"'],
  ['runner words inside orca orchestration send values',
    'orca orchestration send --to term_x --subject "[Rules Astra] python 정리" --body "eval·source 없이 push 전 main 확인"'],
];

for (const [label, command] of maskedForms) {
  test(`§4 「글 가리기(v3)」 ${label}: no decision`, t => assertDecisions(t, command, 'none'));
}

// The precise checks 1–3 read the masked string too, so masked text is never a merge attempt, a gh api
// merge or a main push.
const preciseOnMaskedText = [
  ['heredoc to a file holding the exact standalone merge', lines("cat > notes.md <<'EOF'", exactMerge, 'EOF')],
  ['echo to a file holding a main push', "echo 'git push origin main' >> notes.md"],
  ['heredoc to a file holding a main push and a gh api merge',
    lines("cat >> notes.md <<'EOF'", 'git push origin HEAD:main', `gh api -X PUT repos/o/r/pulls/${PR}/merge`, 'EOF')],
  ['git commit -m holding the exact standalone merge', `git commit -m "${exactMerge}"`],
  ['printf to a file holding git push --all', `printf '%s\\n' "git push --all" > notes.md`],
];

for (const [label, command] of preciseOnMaskedText) {
  test(`§4 「Bash — 병합 시도 판정」 1–3 read the masked string, ${label}: no decision`, t => assertDecisions(t, command, 'none'));
}

// The three false positives with a runner word in code stay accepted false positives (§4 「마지막 그물」,
// §6): 「실행기 찾기(v3.1)」 turns the masking off and the original string meets the net.
const observedWithRunner = [
  // 10-07 05:44:15Z, Rules lead. A variable assignment, && and cat > file <<'EOF' whose body holds
  // gh·merge and push·main; then a multi-line single-quoted node -e script with double quotes and
  // backticks, and a pipe into cut.
  ['05:44:15Z, heredoc to a file then node -e', lines(
    "f=docs/goal.md && cat > scratch/current-position.md <<'EOF'",
    '- **현재 위치**(2026-10-07T05:50Z):',
    '  - 지금 단계: PR1이 [#1](https://example.invalid/pull/1)로 병합됐다(병합 commit `0000000`).',
    '  - 주의: 명령 문자열에 gh·merge나 push·main 낱말 조합을 쓰지 않고, 본문은 파일로 넘긴다.',
    'EOF',
    "node -e '",
    'const fs=require("fs");const f=process.argv[1];const L=fs.readFileSync(f,"utf8").split("\\n");',
    'const rep=fs.readFileSync(process.argv[2],"utf8").replace(/\\n$/,"").split("\\n");',
    'L.splice(10,9,...rep);',
    'L[10+rep.length]=L[10+rep.length].replace("통과(`1111111`).","통과(`1111111`) → 병합(`0000000`).");',
    `fs.writeFileSync(f,L.join("\\n"));' $f scratch/current-position.md && sed -n 9,20p $f | cut -c1-160`,
  )],
  // 10-07 08:38:19Z, main. gh pr view with a single-quoted --jq holding double quotes, then ; ls ; cd &&
  // node board.mjs calls whose double-quoted arguments hold merge·push and main, and a pipe into head.
  ['08:38:19Z, gh pr view then node script arguments', 'gh pr view 1 --json headRefOid,mergeable,mergeStateStatus ' +
    '--jq \'.headRefOid+" "+.mergeable+" "+.mergeStateStatus\'; ls work/evidence/; cd tools/board && ' +
    'node board.mjs plan Part now "초안 작성 중 (도착 시각 미정)" && node board.mjs plan Part detail "## Run" ' +
    '"- run_x (회신 run:run_x)" "## 보류 goal" "- PR1 head 0000000, main 진전으로 문서 충돌(merge·push 안 함)" && ' +
    'node board.mjs stale 2>&1 | head -10'],
  // 10-07 12:38:52Z, main. cd && cat > file <<'EOF' whose JavaScript body holds a template literal with
  // ${…}, escaped backticks, gh·merge and push·main; then node --check && echo.
  ['12:38:52Z, script written through a heredoc then node --check', lines(
    "cd notes/scripts && cat > send-entry.cjs <<'EOF'",
    '// Entry message for a new lead.',
    "const { execFileSync } = require('child_process');",
    'const LEAD = process.argv[2];',
    'const body = `[메인 Claude] 새 리드다. 메인 handle은 ${LEAD}이다.',
    '## 한계',
    '- gh·merge 또는 push·main 낱말이 함께 든 명령은 막히므로 문구는 파일로 넘긴다.',
    '- 우편함은 \\`orca orchestration check\\`로 연다.`;',
    "const out = execFileSync('orca', ['orchestration', 'send', '--to', LEAD, '--subject', '[메인 Claude] 진입', " +
      "'--body', body, '--json'], { encoding: 'utf8' });",
    'console.log(JSON.parse(out).result.message.id);',
    'EOF',
    'node --check send-entry.cjs && echo ok',
  )],
];

for (const [label, command] of observedWithRunner) {
  test(`§4 「실행기 찾기(v3.1)」 10-07 false positive ${label}: suspect-words`, t => assertDecisions(t, command, 'suspect-words'));
}

// Forms that stay blocked with the same code before and after v3.1: [label, command, Bash code, Monitor
// code when it differs]. A precise code comes first; otherwise the last net gives suspect-words.
const keptBlocks = [
  ['table: heredoc into bash holding the exact merge', lines("bash <<'EOF'", exactMerge, 'EOF'), 'compound-command', 'non-bash-merge'],
  ['table: echo piped into bash', "echo 'G=gh; $G pr merge 1' | bash", 'suspect-words'],
  ['table: an alias definition is not masked', "alias gp='git push'; gp origin HEAD:main", 'suspect-words'],
  ['table: git push arguments are not masked', 'git -C "a;b" push origin "main"', 'suspect-words'],
  ['table: heredoc with an unquoted delimiter', lines('cat <<EOF', 'gh merge 메모', 'EOF'), 'suspect-words'],

  ['quoted refspec', `git -C "a;b" push origin 'HEAD:main'`, 'suspect-words'],
  ['variable holding gh', `G="gh"; $G pr merge ${tail}`, 'suspect-words'],
  ['alias holding gh pr merge', `alias gm='gh pr merge'; gm ${tail}`, 'suspect-words'],
  ['variable holding push', 'P=push; git $P origin main', 'suspect-words'],
  ['eval of the exact merge', `eval "${exactMerge}"`, 'compound-command', 'non-bash-merge'],
  ['eval of a main push', "eval 'git push origin main'", 'push-main'],
  ['bash -c of the exact merge', `bash -c '${exactMerge}'`, 'compound-command', 'non-bash-merge'],
  ['sh -c of a main push', 'sh -c "git push origin main"', 'push-main'],
  ['node -e running the exact merge', `node -e 'require("child_process").execSync("${exactMerge}")'`, 'compound-command', 'non-bash-merge'],
  ['python -c running a main push', `python -c "import os; os.system('git push origin main')"`, 'push-main'],
  ['heredoc into bash holding a main push', lines("bash <<'EOF'", 'git push origin main', 'EOF'), 'push-main'],
  ['heredoc with a double-quoted delimiter into sh -s', lines('sh -s <<"EOF"', exactMerge, 'EOF'), 'compound-command', 'non-bash-merge'],
  ['heredoc into python3 with the push in a list', lines("python3 - <<'EOF'", 'import subprocess',
    "subprocess.run(['git', 'push', 'origin', 'main'])", 'EOF'), 'suspect-words'],
  ['heredoc into node running the exact merge', lines("node <<'EOF'", `require('child_process').execSync('${exactMerge}');`, 'EOF'),
    'compound-command', 'non-bash-merge'],
  ['cat heredoc piped into bash', lines("cat <<'EOF' | bash", 'git push origin main', 'EOF'), 'push-main'],
  ['cat heredoc piped into sh', lines("cat <<'EOF' | sh", exactMerge, 'EOF'), 'compound-command', 'non-bash-merge'],
  ['tee heredoc piped into bash', lines("tee x.sh <<'EOF' | bash", 'git push --mirror origin', 'EOF'), 'push-main'],
  ['heredoc inside $( )', lines("x=$(cat <<'EOF'", 'git push origin main', 'EOF', ')'), 'push-main'],
  ['heredoc inside $( ) given to bash -c', lines("bash -c \"$(cat <<'EOF'", exactMerge, 'EOF', ')"'), 'compound-command', 'non-bash-merge'],
  ['heredoc inside backticks', lines("x=`cat <<'EOF'", 'git push origin HEAD:main', 'EOF', '`'), 'push-main'],
  ['echo piped into bash holding a main push', "echo 'git push origin main' | bash", 'push-main'],
  ['printf piped into sh holding the exact merge', `printf '%s\\n' "${exactMerge}" | sh`, 'compound-command', 'non-bash-merge'],
  ['echo not masked because the command pipes into bash elsewhere', "echo 'git push origin main' > x.sh; cat x.sh | bash", 'push-main'],
  ['echo not masked because the command pipes into node elsewhere', 'echo "gh merge 메모" >> notes.md && cat notes.md | node', 'suspect-words'],
  ['echo not masked because |& pipes it into tr, which is off the pass list', "echo 'git push origin main' |& tr a-z A-Z", 'push-main'],
  ['$( ) inside an echo argument', 'echo "done $(git push origin main)"', 'push-main'],
  ['$( ) inside a printf argument holding the exact merge', `printf '%s\\n' "x $(${exactMerge})" >> notes.md`, 'compound-command', 'non-bash-merge'],
  ['$( ) inside a git commit -m value', 'git commit -m "note $(git push origin HEAD:main)"', 'push-main'],
  ['$( ) inside a gh pr create --body value', `gh pr create --title "t" --body "$(${exactMerge})"`, 'compound-command', 'non-bash-merge'],
  ['backticks inside an echo argument', 'echo "x `git push origin main`"', 'push-main'],
  ['${ } holding $( ) inside an echo argument', 'echo "${X:-$(git push --all)}"', 'push-main'],
  ['process substitution after a heredoc to a file', lines("cat > notes.md <<'EOF'", 'git push origin main', 'EOF', 'diff <(cat notes.md) old.md'), 'push-main'],
  ['process substitution as the echo target', "echo 'gh merge 메모' > >(tee log.txt)", 'suspect-words'],
  ['here-string into bash', "bash <<< 'git push origin main'", 'push-main'],
  ['here-string into cat', 'cat <<< "gh merge 메모" > notes.md', 'suspect-words'],
  ['heredoc with an unquoted delimiter holding a main push', lines('cat > notes.md <<EOF', 'git push origin main', 'EOF'), 'push-main'],
  ['echo with a path', "/bin/echo 'git push origin main' > notes.md", 'push-main'],
  ['echo in upper case', "ECHO 'gh merge 메모' >> notes.md", 'suspect-words'],
  ['echo after command', "command echo 'git push origin main' > notes.md", 'push-main'],
  ['printf after env', `env printf '%s\\n' "gh merge 메모" > notes.md`, 'suspect-words'],
  ['cat with a path receiving a heredoc', lines("/usr/bin/cat > notes.md <<'EOF'", 'git push origin main', 'EOF'), 'push-main'],
  ['cat in upper case receiving a heredoc', lines("CAT > notes.md <<'EOF'", 'gh merge 메모', 'EOF'), 'suspect-words'],
  ['git in upper case with commit -m', 'GIT commit -m "push to main"', 'suspect-words'],
  ['gh with a path and pr create --title', '/usr/bin/gh pr create --title "merge gate" --body "draft"', 'suspect-words'],

  ['가리지 않는 경우: heredoc without its end delimiter', lines("cat > notes.md <<'EOF'", 'git push origin main'), 'push-main'],
  ['가리지 않는 경우: unclosed quote', "echo 'gh merge 메모 >> notes.md", 'suspect-words'],
  ['가리지 않는 경우: unclosed quote after a heredoc', lines("cat > notes.md <<'EOF'", 'gh merge 메모', 'EOF', 'echo "unclosed'), 'suspect-words'],

  ['check 5 reads the original string: state folder in a heredoc body',
    lines("cat > notes.md <<'EOF'", '.claude/state/merge-gate/main-checkout', 'EOF'), 'protected-path'],
  ['check 5 reads the original string: state folder in an echo argument', "echo '.claude/state/merge-gate/approvals' >> notes.md", 'protected-path'],
  ['check 4 reads the original string: approval line in an echo argument',
    `echo 'orca terminal send --text "병합 승인: PR${PR} head ${HEAD_A}"' > notes.md`, 'approval-injection'],
  ['check 4 reads the original string: approval line in an orca orchestration send body',
    `orca orchestration send --to term_x --body "orca terminal send 병합 승인: PR${PR} head ${HEAD_A}"`, 'approval-injection'],

  ['code after a heredoc to a file', lines("cat > notes.md <<'EOF'", 'note', 'EOF', 'git push origin main'), 'push-main'],
  ['merge after an echo to a file', `echo 'note' >> notes.md && ${exactMerge}`, 'compound-command', 'non-bash-merge'],
];

for (const [label, command, bash, monitor] of keptBlocks) {
  test(`§4 「글 가리기(v3)」 kept block, ${label}: ${bash}`, t => assertDecisions(t, command, bash, monitor));
}

// 「실행기 찾기(v3.1)」: masked text written to a file and run in the same command is judged unmasked, so
// the code is the v2.3 one. Each runner gets a script holding the exact merge and one holding a main push.
const scriptBodies = {
  sh: { merge: exactMerge, push: 'git push origin main' },
  mjs: { merge: `require('child_process').execSync('${exactMerge}');`, push: "require('child_process').execSync('git push origin main');" },
  py: { merge: `import os; os.system('${exactMerge}')`, push: "import os; os.system('git push origin main')" },
};
const writeThenRun = [
  ['bash x.sh', 'x.sh', 'sh'],
  ['sh x.sh', 'x.sh', 'sh'],
  ['node x.mjs', 'x.mjs', 'mjs'],
  ['python x.py', 'x.py', 'py'],
  ['./x.sh', 'x.sh', 'sh'],
  ['/tmp/x.sh', '/tmp/x.sh', 'sh'],
  ['. x.sh', 'x.sh', 'sh'],
  ['source x.sh', 'x.sh', 'sh'],
];
const runnerBlocks = writeThenRun.flatMap(([runLine, path, kind]) => [
  [`heredoc writes ${path} then ${runLine}, merge phrase`, lines(`cat > ${path} <<'EOF'`, scriptBodies[kind].merge, 'EOF', runLine),
    'compound-command', 'non-bash-merge'],
  [`heredoc writes ${path} then ${runLine}, main push phrase`, lines(`cat > ${path} <<'EOF'`, scriptBodies[kind].push, 'EOF', runLine),
    'push-main'],
]);
runnerBlocks.push(
  // Check 1 (the merge) comes before check 3 (the push), so the merge code wins.
  ['table: heredoc writes x.sh with a merge and a main push, then bash x.sh',
    lines("cat > x.sh <<'EOF'", exactMerge, 'git push origin main', 'EOF', 'bash x.sh'), 'compound-command', 'non-bash-merge'],
  ['table: echo writes x.sh then ./x.sh', "echo 'gh pr merge 1 --squash' > x.sh && ./x.sh", 'compound-command', 'non-bash-merge'],
  ['echo writes a main push to x.sh then ./x.sh', "echo 'git push origin main' > x.sh && ./x.sh", 'push-main'],
  // The precise reading keeps \n in the refspec destination, so only the net (word head:main) blocks it.
  ['table: printf writes /tmp/x.sh then /tmp/x.sh', "printf 'git push origin HEAD:main\\n' > /tmp/x.sh; /tmp/x.sh", 'suspect-words'],
  ['table: commit message piped into sh', 'git commit -m "gh pr merge 1" && git log -1 --format=%B | sh', 'compound-command', 'non-bash-merge'],
  ['table: node script arguments next to gh pr view', 'node board.mjs plan X detail "PR 12 merge·push 안 함" && gh pr view 12', 'suspect-words'],
  ['table: ./ redirect target turns the masking off', lines("cat > ./notes.md <<'EOF'", '- push 전에 main을 확인한다.', 'EOF'), 'suspect-words'],
  ['node script arguments (the v3 masked place v3.1 removed)', "node tools/board.mjs plan Part detail \"merge·push 안 함\" 'gh 확인, main 진전'",
    'suspect-words'],
  ['heredoc writes x.sh then env x.sh', lines("cat > x.sh <<'EOF'", exactMerge, 'EOF', 'env x.sh'), 'compound-command', 'non-bash-merge'],
  ['printf writes push arguments then xargs git', "printf '%s\\n' 'push origin main' > args.txt && xargs -a args.txt git", 'suspect-words'],
  ['heredoc writes x.sh then /usr/bin/bash x.sh', lines("cat > x.sh <<'EOF'", 'git push origin main', 'EOF', '/usr/bin/bash x.sh'), 'push-main'],
  ['heredoc writes x.sh then bash.exe x.sh', lines("cat > x.sh <<'EOF'", exactMerge, 'EOF', 'bash.exe x.sh'), 'compound-command', 'non-bash-merge'],
  ['node.exe with a script path', 'node.exe tools/board.mjs "gh merge 메모"', 'suspect-words'],
  ['node with an option before the script path', 'node --no-warnings tools/board.mjs "gh merge 메모"', 'suspect-words'],
);

for (const [label, command, bash, monitor] of runnerBlocks) {
  test(`§4 「실행기 찾기(v3.1)」 kept block, ${label}: ${bash}`, t => assertDecisions(t, command, bash, monitor));
}

// T1: forms only the last net reads; with + dropped, +main is the word main (before v3: no decision).
const plusMain = [
  ['git push with a quoted path', 'git -C "a;b" push origin +main'],
  ['an alias definition', "alias gp='git push'; gp origin +main"],
];

for (const [label, command] of plusMain) {
  test(`§4 「마지막 그물」 condition 3 (v3, T1), +main after ${label}: suspect-words`, t => assertDecisions(t, command, 'suspect-words'));
}

// v3.2 rows. The probe inputs come from the first independent verification (E/verify/probe-variants.mjs,
// rows D1m–D11 and G1–G4), copied character for character with its names M, P and GQL; only the inputs
// come from there. v3.2 judges these forms on the original string, so the expected codes are the v2.3
// ones (a precise code first, otherwise suspect-words): Bash compound-command and Monitor non-bash-merge
// for a merge attempt, push-main for a main push, api-merge for a gh api merge call.
const M = `gh pr merge ${PR} --squash --match-head-commit ${HEAD_A}`;
const P = 'git push origin HEAD:main';
const GQL = '{"query":"mutation { mergePullRequest(input: {pullRequestId: \\"X\\"}) { clientMutationId } }"}';

// 「bash와 다르게 읽을 수 있는 꼴(v3.2)」 (V1): the masking reader cannot promise to end quotes, comments and
// words where bash does, so the whole command stays unmasked. Each label starts with the number of the
// listed form; a row without a probe id covers a listed form the probe did not.
// [label, command, Bash code, Monitor code when it differs]
const unreadableForms = [
  ['1, D1m: ANSI-C quote before the merge', `echo $'\\'' ; ${M} #'`, 'compound-command', 'non-bash-merge'],
  ['1, D1p: ANSI-C quote before a main push', `echo $'\\'' ; ${P} #'`, 'push-main'],
  ['1, D2m: ANSI-C quote with an escaped quote inside a word', `printf '%s' $'it\\'s' ; ${M} # '`, 'compound-command', 'non-bash-merge'],
  ['1: locale quote $"…" next to a masked echo argument', `echo $"메모" '${M}' >> notes.md`, 'compound-command', 'non-bash-merge'],
  ['2, D3m: single quote in a backtick substitution inside double quotes', `echo "\`echo '\`" ; true '\`' ; ${M} ; echo " #"`,
    'compound-command', 'non-bash-merge'],
  ['2, D3p: the same before a main push', `echo "\`echo '\`" ; true '\`' ; ${P} ; echo " #"`, 'push-main'],
  ['2, D4m: single quote in a top-level backtick substitution', `echo \`echo '\` ; ${M} #'\``, 'compound-command', 'non-bash-merge'],
  ['2: double quote in a top-level backtick substitution', `echo \`date "+%F"\` '${M}' >> notes.md`, 'compound-command', 'non-bash-merge'],
  ['2: backslash in a backtick substitution inside double quotes', `echo "at \`date -u +%H\\:%M\`" '${P}' >> notes.md`, 'push-main'],
  ['3, D5m: CR before # outside quotes', `echo a\r#'\necho x' ; ${M} #'`, 'compound-command', 'non-bash-merge'],
  ['3, D6m: CR on the heredoc end line', `cat > f <<'EOF'\nbody\nEOF\r\necho '\nEOF\n${M} #'`, 'compound-command', 'non-bash-merge'],
  // CR counts anywhere, so a CRLF line inside a quoted heredoc body to a file also keeps the command unmasked.
  ['3: CR only inside a quoted heredoc body to a file', lines("cat > notes.md <<'EOF'", '- push 전에 main을 확인한다.\r', 'EOF'),
    'suspect-words'],
  // The D7m and ideographic space rows hold the raw characters U+00A0 and U+3000, not escapes.
  ['4, D7m: NBSP before # outside quotes', `echo a #'\necho x' ; ${M} #'`, 'compound-command', 'non-bash-merge'],
  ['4: ideographic space next to a masked echo argument', `echo a　b '${M}' >> notes.md`, 'compound-command', 'non-bash-merge'],
  ['4: vertical tab next to a masked echo argument', `echo a\vb '${P}' >> notes.md`, 'push-main'],
];

// 「묶음 뒤 파이프(v3.2)」 (V2): a | or |& after a group pipes every simple command in it, nested groups
// included, and a | followed by a group flows into code. A heredoc whose receiving command is piped is not
// masked even when the pipe goes to a command on the pass list; echo and printf arguments are not masked
// when the command has a pipe that flows into code. busybox and ash are not on the runner list, so the rows
// that use them hold no runner word and only the pipe reading decides. D8–D10 also hold the words gh and
// api, which 「실행기 찾기」 5 (v3.2) catches on its own.
const groupPipeForms = [
  ['D8: echo in a subshell piped into gh api', `( echo '${GQL}' ) | gh api graphql --input -`, 'api-merge'],
  ['D9: heredoc in a subshell piped into gh api', `( cat <<'EOF' ) | gh api graphql --input -\n${GQL}\nEOF`, 'api-merge'],
  ['D10: heredoc in a brace group piped into gh api', `{ cat <<'EOF'; } | gh api graphql --input -\n${GQL}\nEOF`, 'api-merge'],
  ['D11: echo in a subshell piped into a shell off the runner list', `( echo '${M}' ) | busybox ash`, 'compound-command', 'non-bash-merge'],
  ['heredoc in a brace group piped into tee, which is on the pass list', lines("{ cat <<'EOF'; } | tee out.txt", P, 'EOF'), 'push-main'],
  ['|& after an echo in a subshell', `( echo '${M}' ) |& busybox ash`, 'compound-command', 'non-bash-merge'],
  ['nested groups: echo in a subshell inside a subshell', `( ( echo '${P}' ) ) | busybox ash`, 'push-main'],
  ['nested groups: heredoc in a subshell inside a brace group', lines("{ ( cat <<'EOF' ); } | tee out.txt", M, 'EOF'),
    'compound-command', 'non-bash-merge'],
  ['a pipe into a subshell', `echo '${M}' | ( cat > notes.md )`, 'compound-command', 'non-bash-merge'],
  ['a pipe into a brace group', `echo '${P}' | { cat >> notes.md; }`, 'push-main'],
];

// 「실행기 찾기」 4·5 (v3.2, O1): a variable or a substitution becomes the command name (4), or the same
// command's gh api may read the masked text as its body (5), so the masking is dropped.
const runnerFourAndFive = [
  ['4, G1: a substitution is the whole command', `$(echo '${M}')`, 'compound-command', 'non-bash-merge'],
  ['4, G2: a backtick substitution is the whole command', `\`printf '${M}'\``, 'compound-command', 'non-bash-merge'],
  ['4, G3: a substitution builds the gh command word', `"$(echo 'gh')" pr merge 12 --squash`, 'suspect-words'],
  ['4: $VAR as the command word after a masked echo', `echo '${M}' >> notes.md && $EDITOR notes.md`, 'compound-command', 'non-bash-merge'],
  ['4: ${VAR} as the command word after a masked echo', `echo '${P}' > x.txt; \${PAGER} x.txt`, 'push-main'],
  ['4: $VAR as the command word inside $( )', `echo '${M}' >> notes.md; n=$($COUNT notes.md)`, 'compound-command', 'non-bash-merge'],
  ['4: a backtick substitution as the command word', `echo '${P}' > x.txt; \`which less\` x.txt`, 'push-main'],
  ['5, G4: gh api reads a file written through a masked heredoc', `cat > q.json <<'EOF'\n${GQL}\nEOF\ngh api graphql --input q.json`, 'api-merge'],
  ['5: gh api reads a file written by a masked echo', `echo '${GQL}' > q.json && gh api graphql --input q.json`, 'api-merge'],
  // The cost of the broad side: a gh api call that merges nothing still unmasks the note next to it.
  ['5: a gh api read next to a masked echo note', `echo 'gh pr merge 메모' >> notes.md && gh api repos/o/r/pulls/${PR}`,
    'compound-command', 'non-bash-merge'],
];

// Contrast rows: forms v3.2 still masks, so the v3.2 change must not block them. The two table rows and the
// group piped into tee are named by v3.2; the others follow from the wording of the same items (outside
// quotes and heredoc bodies, a pipe and not a redirect, a command word, words of the masked string).
const unreadableContrasts = [
  ['table (v3.2): backtick code notation, $\'x\' and | as text in a quoted heredoc body',
    lines("cat > notes.md <<'EOF'", "- `git push origin main` 전에 `$'x'`와 `a | b` 표기를 확인한다.", 'EOF')],
  ['table (v3.2): backticks inside single quotes', "echo '`gh pr merge` 메모' >> notes.md"],
  ['escaped backticks inside double quotes are text', 'echo "\\`git push origin main\\` 전 확인" >> notes.md'],
  ['escaped backticks inside a double-quoted git commit -m value', 'git commit -m "docs: \\`gh pr merge\\` 안내"'],
  // The raw character U+00A0 stands between merge and 메모.
  ['NBSP inside a single-quoted echo argument', "echo 'gh merge 메모' >> notes.md"],
];
const groupPipeContrasts = [
  ['echo commands in a brace group piped into tee, which is on the pass list',
    "{ echo 'gh merge 메모'; echo 'git push origin main'; } | tee -a notes.md"],
  ['echo in a subshell piped into wc, which is on the pass list', "( echo 'git push origin main' ) | wc -l"],
  ['heredoc in a brace group redirected to a file, not piped', lines("{ cat <<'EOF'; } > notes.md", '- push 전에 main을 확인한다.', 'EOF')],
];
const runnerFourAndFiveContrasts = [
  ['4: $ only in an assignment and a redirect target, not in a command word', 'E=$(pwd) && echo \'gh merge 메모\' >> "$E/notes.md"'],
  ['5: gh in code and api only in masked text', "gh pr view 12 --json state && echo 'gh api 병합 메모' >> notes.md"],
];

const v32Sections = [
  ['bash와 다르게 읽을 수 있는 꼴(v3.2)', unreadableForms, unreadableContrasts],
  ['묶음 뒤 파이프(v3.2)', groupPipeForms, groupPipeContrasts],
  ['실행기 찾기 4·5(v3.2)', runnerFourAndFive, runnerFourAndFiveContrasts],
];

for (const [section, keptForms, contrasts] of v32Sections) {
  for (const [label, command, bash, monitor] of keptForms) {
    test(`§4 「${section}」 kept block, ${label}: ${bash}`, t => assertDecisions(t, command, bash, monitor));
  }
  for (const [label, command] of contrasts) {
    test(`§4 「${section}」 contrast, ${label}: no decision`, t => assertDecisions(t, command, 'none'));
  }
}
