"""Owned work, raw process records and Git fixtures for the execution-contract suite.

Every write stays below the explicit MODULE_BOUNDARIES_EXECUTION_WORK root. Nothing
here imports the checker's runner/inputs: expectations come from the requirements
and from the bytes on disk or in Git, so a product calculation is never the oracle.
Importing this module has no side effect; the root is validated and created only
when an opted-in test class starts.
"""

import json
import os
import pathlib
import signal
import subprocess
import textwrap
import time
import uuid
from datetime import datetime, timezone

from support import module_boundary_independent_fixture as sources


REPO = pathlib.Path(__file__).resolve().parents[3]
TESTS = REPO / "99_Tools/Architecture.Tests"
ENTRY = REPO / "99_Tools/Architecture/check-module-boundaries.sh"
WORKFLOW = REPO / ".github/workflows/module-boundaries.yml"
WORK_VARIABLE = "MODULE_BOUNDARIES_EXECUTION_WORK"
MAIN_SHA_VARIABLE = "MODULE_BOUNDARIES_EXECUTION_MAIN_SHA"
# Opt-ins of every module boundary suite. A child never inherits them unless a test
# sets one on purpose, so this suite cannot recursively start itself or the others.
OPT_IN_VARIABLES = (
    "MODULE_BOUNDARIES_TEST_WORK",
    "MODULE_BOUNDARIES_INDEPENDENT_WORK",
    "MODULE_BOUNDARIES_INDEPENDENT_REAL",
    WORK_VARIABLE,
    MAIN_SHA_VARIABLE,
    "RUNNER_TEMP",
)

_work = None


def owned_work():
    """Validate and create the explicit root once; there is no default location."""
    global _work
    if _work is not None:
        return _work
    value = os.environ.get(WORK_VARIABLE)
    if not value or not os.path.isabs(value):
        raise ValueError(f"{WORK_VARIABLE} must be a nonempty absolute path")
    root = pathlib.Path(value)
    if ".." in root.parts or any(path.is_symlink() for path in (root, *root.parents)):
        raise ValueError(f"{WORK_VARIABLE} must not traverse parents or a symlink")
    if REPO / ".backups" not in root.parents:
        raise ValueError(f"{WORK_VARIABLE} must be below the repository .backups/")
    if root.exists():
        raise ValueError(f"{WORK_VARIABLE} must name a new directory")
    root.mkdir(parents=True)
    _work = root
    return root


def new_folder(label):
    folder = owned_work() / f"{label}-{uuid.uuid4().hex[:8]}"
    folder.mkdir()
    return folder


class RecordedRun:
    """One child process and the raw files kept beside it."""

    def __init__(self, folder, argv, returncode, stdout, stderr, elapsed, timed_out):
        self.folder = folder
        self.argv = argv
        self.returncode = returncode
        self.stdout = stdout
        self.stderr = stderr
        self.elapsed = elapsed
        self.timed_out = timed_out

    @property
    def output(self):
        return self.stdout + self.stderr


def run_recorded(folder, argv, cwd=REPO, set_environment=None, unset=(), timeout=900, stdin_text=None):
    """Run argv (no shell) in its own session and keep argv/cwd/env/exit/time/stdout/stderr.

    TMPDIR defaults to the run folder so Python temporary files stay owned. On timeout
    only this child's own process group is killed.
    """
    removed = sorted(name for name in (*OPT_IN_VARIABLES, *unset) if name in os.environ)
    environment = {key: value for key, value in os.environ.items() if key not in removed}
    overrides = {"TMPDIR": str(folder / "tmp"), "PYTHONDONTWRITEBYTECODE": "1"}
    overrides.update(set_environment or {})
    pathlib.Path(overrides["TMPDIR"]).mkdir(parents=True, exist_ok=True)
    environment.update(overrides)
    started_utc = datetime.now(timezone.utc).isoformat()
    started = time.perf_counter()
    process = subprocess.Popen(
        [str(part) for part in argv], cwd=cwd, env=environment, text=True,
        stdin=subprocess.PIPE if stdin_text is not None else subprocess.DEVNULL,
        stdout=subprocess.PIPE, stderr=subprocess.PIPE, start_new_session=True,
    )
    timed_out = False
    try:
        stdout, stderr = process.communicate(stdin_text, timeout=timeout)
    except subprocess.TimeoutExpired:
        timed_out = True
        os.killpg(process.pid, signal.SIGKILL)
        stdout, stderr = process.communicate()
    elapsed = time.perf_counter() - started
    record = {
        "argv": [str(part) for part in argv], "cwd": str(cwd),
        "environmentSet": overrides, "environmentUnset": removed,
        "stdin": "text" if stdin_text is not None else "devnull",
        "startedUtc": started_utc, "timeoutSeconds": timeout, "timedOut": timed_out,
        "exitCode": process.returncode, "elapsedSeconds": elapsed,
    }
    write_json(folder / "command.json", record)
    (folder / "stdout.txt").write_text(stdout, encoding="utf-8")
    (folder / "stderr.txt").write_text(stderr, encoding="utf-8")
    return RecordedRun(folder, record["argv"], process.returncode, stdout, stderr, elapsed, timed_out)


def write_json(path, value):
    pathlib.Path(path).write_text(json.dumps(value, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")


def read_json(path):
    return json.loads(pathlib.Path(path).read_text(encoding="utf-8"))


def tree_state(root):
    """{relative path: (size, mtime_ns)} read without writing; empty when absent."""
    root = pathlib.Path(root)
    if not root.exists():
        return {}
    state = {}
    for current, folders, names in os.walk(root, followlinks=False):
        for name in folders + names:
            path = pathlib.Path(current) / name
            status = path.lstat()
            state[str(path.relative_to(root))] = (status.st_size, status.st_mtime_ns)
    return state


def changed_since(root, started_ns):
    """Entries below root whose content or metadata changed after started_ns (read-only walk)."""
    changed = []
    for current, folders, names in os.walk(root, followlinks=False):
        for name in folders + names:
            path = pathlib.Path(current) / name
            status = path.lstat()
            if max(status.st_mtime_ns, status.st_ctime_ns) >= started_ns:
                changed.append(str(path))
    return changed


def windows_path(path):
    completed = subprocess.run(
        ["wslpath", "-w", str(path)], capture_output=True, text=True, stdin=subprocess.DEVNULL, check=True,
    )
    return completed.stdout.strip()


# --- Git -------------------------------------------------------------------

def operating_git_prefix():
    """Read-only Git argv for this checkout; an Orca worktree names a Windows gitdir."""
    pointer = REPO / ".git"
    if pointer.is_dir():
        return ["git", "-C", str(REPO)]
    gitdir = pointer.read_text(encoding="utf-8").strip().removeprefix("gitdir: ")
    if ":" in gitdir[:3]:
        gitdir = subprocess.run(
            ["wslpath", "-u", gitdir], capture_output=True, text=True, stdin=subprocess.DEVNULL, check=True,
        ).stdout.strip()
    return ["git", "--git-dir", gitdir, "--work-tree", str(REPO), "-C", str(REPO)]


def read_git(prefix, *arguments, binary=False):
    """Run a read-only Git query; optional locks are off so no index refresh is written."""
    completed = subprocess.run(
        [*prefix, *arguments], capture_output=True, stdin=subprocess.DEVNULL, timeout=120,
        env={**os.environ, "GIT_OPTIONAL_LOCKS": "0"},
    )
    if completed.returncode:
        raise RuntimeError(f"git {arguments} failed: {completed.stderr.decode(errors='replace')}")
    return completed.stdout if binary else completed.stdout.decode("utf-8")


def blob_bytes(prefix, revision, paths):
    """{path: bytes} of `revision:path` through one `git cat-file --batch` reader."""
    request = "".join(f"{revision}:{path}\n" for path in paths).encode("utf-8")
    completed = subprocess.run(
        [*prefix, "cat-file", "--batch"], input=request, capture_output=True, timeout=300,
        env={**os.environ, "GIT_OPTIONAL_LOCKS": "0"},
    )
    if completed.returncode:
        raise RuntimeError(completed.stderr.decode(errors="replace"))
    data = completed.stdout
    found = {}
    offset = 0
    for path in paths:
        header_end = data.index(b"\n", offset)
        header = data[offset:header_end].split()
        if len(header) != 3 or header[1] != b"blob":
            raise RuntimeError(f"Not a blob in {revision}: {path}: {header!r}")
        size = int(header[2])
        found[path] = data[header_end + 1:header_end + 1 + size]
        offset = header_end + 1 + size + 1
    return found


# Untracked/ignored/staged/modified layout for requirement #2. Paths are relative to the
# fixture root; expectations follow the correction contract requirement 5 wording.
UNTRACKED_BOUNDARY = f"{sources.SERVER}/Handlers/UntrackedMapReach.cs"
UNTRACKED_SPACED = f"{sources.SERVER}/Handlers/Untracked Spaced Probe.cs"
UNTRACKED_OUTSIDE_BOUNDARY = f"{sources.SERVER}/Loose/UntrackedLooseInput.cs"
UNTRACKED_BUILD_INPUT = "Directory.Build.targets"
IGNORED_BOUNDARY = f"{sources.SERVER}/Handlers/IgnoredBoundaryProbe.cs"
TRACKED_BRACKET = f"{sources.SERVER}/Handlers/TrackedBracket[1].cs"
STAGED_SESSION = f"{sources.SERVER}/Sessions/StagedSessionProbe.cs"
MODIFIED_TRACKED = f"{sources.SERVER}/Maps/WorldMap.cs"
UNRELATED_UNTRACKED = ("notes.md", "02_Server/Elsewhere/OutsideProject.cs")
IGNORED_EVIDENCE = ".backups/ignored-evidence.txt"


def handler_source(name, body):
    return f"namespace {sources.NS}.Handlers;\n\npublic class {name}\n{{\n{body}\n}}\n"


def write_git_fixture(root):
    """Commit a clean three-folder project in a new owned fixture, then add the #2 cases.

    Git runs with GIT_CONFIG_GLOBAL=/dev/null and GIT_CONFIG_NOSYSTEM=1 so user or system
    settings neither change the fixture blobs nor get written. Only this fixture's own
    repository receives an index and history.
    """
    root = pathlib.Path(root)
    owner = owned_work()
    if owner not in root.parents or root.exists():
        raise ValueError("Git fixture must be new and below the owned work root")
    sources.write_fixture(root, sources.clean_sources(), extra_files={
        ".gitignore": ".backups/\n02_Server/GameServer/Handlers/Ignored*.cs\n",
        TRACKED_BRACKET: handler_source("TrackedBracket", "    public int Value;"),
    })
    hooks = root.parent / "fixture-empty-hooks"
    hooks.mkdir()
    environment = {**os.environ, "GIT_CONFIG_GLOBAL": "/dev/null", "GIT_CONFIG_NOSYSTEM": "1", "GIT_OPTIONAL_LOCKS": "0"}
    prefix = [
        "git", "-C", str(root), "-c", "user.name=Execution Contract Fixture",
        "-c", "user.email=execution-contract@example.invalid", "-c", "commit.gpgSign=false",
        "-c", f"core.hooksPath={hooks}", "-c", "init.defaultBranch=fixture",
    ]
    records = []

    def git(*arguments):
        completed = subprocess.run(
            [*prefix, *arguments], capture_output=True, text=True, stdin=subprocess.DEVNULL,
            env=environment, timeout=60,
        )
        records.append({"argv": [*prefix, *arguments], "exitCode": completed.returncode,
                        "stdout": completed.stdout, "stderr": completed.stderr})
        write_json(root.parent / "fixture-git-commands.json", records)
        if completed.returncode:
            raise RuntimeError(f"Fixture Git failed: {completed.stderr}")
        return completed.stdout.strip()

    git("init", "--quiet")
    git("add", "--all")
    git("commit", "--quiet", "--message", "Clean tracked module boundary fixture")
    head = git("rev-parse", "HEAD")
    reach = "    public global::" + sources.NS + ".Maps.WorldMap? Map;"
    extra = {
        UNTRACKED_BOUNDARY: handler_source("UntrackedMapReach", reach),
        UNTRACKED_SPACED: handler_source("UntrackedSpacedProbe", "    public int Count;"),
        UNTRACKED_OUTSIDE_BOUNDARY: f"namespace {sources.NS}.Loose;\n\npublic class UntrackedLooseInput\n{{\n}}\n",
        UNTRACKED_BUILD_INPUT: "<Project />\n",
        IGNORED_BOUNDARY: handler_source("IgnoredBoundaryProbe", "    public int Ignored;"),
        STAGED_SESSION: f"namespace {sources.NS}.Sessions;\n\npublic class StagedSessionProbe\n{{\n}}\n",
        UNRELATED_UNTRACKED[0]: "Unrelated untracked note.\n",
        UNRELATED_UNTRACKED[1]: "namespace Outside;\n\npublic class OutsideProject\n{\n}\n",
        IGNORED_EVIDENCE: "Ignored past evidence.\n",
    }
    for relative, text in extra.items():
        target = root / relative
        target.parent.mkdir(parents=True, exist_ok=True)
        target.write_text(text, encoding="utf-8")
    git("add", "--", STAGED_SESSION)
    with (root / MODIFIED_TRACKED).open("a", encoding="utf-8") as stream:
        stream.write("// Workspace-only edit after the fixture commit.\n")
    return head


def git_state(root):
    """Bytes that a read-only inspection must not change in the fixture repository."""
    git_folder = pathlib.Path(root) / ".git"
    return {name: (git_folder / name).read_bytes() for name in ("index", "HEAD")} | {
        "refs": sorted(str(path.relative_to(git_folder)) for path in (git_folder / "refs").rglob("*")),
    }


# --- Child harnesses -------------------------------------------------------

# Records process, network and write attempts of a discovery run in the child itself.
# Recording stops before the summary is written so the harness's own write is excluded.
DISCOVERY_HARNESS = textwrap.dedent('''\
    import json, os, sys, unittest

    summary_path, start, pattern = sys.argv[1:4]
    events = []
    recording = [True]
    WRITE = os.O_WRONLY | os.O_RDWR | os.O_CREAT | os.O_APPEND | os.O_TRUNC
    PROCESS = ("subprocess.Popen", "os.system", "os.exec", "os.posix_spawn", "os.spawn", "os.fork",
               "os.forkpty", "pty.spawn", "socket.connect", "socket.getaddrinfo", "socket.sendto")
    MUTATE = ("os.mkdir", "os.rename", "os.replace", "os.remove", "os.rmdir", "shutil.rmtree",
              "os.symlink", "os.link", "os.truncate", "os.chmod", "os.utime")

    def hook(event, arguments):
        if not recording[0]:
            return
        if event in PROCESS:
            events.append({"event": event, "arguments": repr(arguments)[:400]})
        elif event == "open":
            path, mode, flags = arguments
            writes = (isinstance(mode, str) and any(c in mode for c in "wax+")) or bool((flags or 0) & WRITE)
            if writes and isinstance(path, (str, bytes)):
                events.append({"event": "open-write", "path": os.path.abspath(os.fsdecode(path))})
        elif event in MUTATE:
            events.append({"event": event, "arguments": repr(arguments)[:400]})

    sys.addaudithook(hook)
    loader = unittest.defaultTestLoader
    suite = loader.discover(start, pattern=pattern, top_level_dir=start)

    def ids(node):
        if isinstance(node, unittest.TestSuite):
            return [name for child in node for name in ids(child)]
        return [node.id()]

    collected = ids(suite)

    class Observed(unittest.TextTestResult):
        def __init__(self, *arguments, **keywords):
            super().__init__(*arguments, **keywords)
            self.started = []

        def startTest(self, test):
            self.started.append(test.id())
            super().startTest(test)

    result = unittest.TextTestRunner(verbosity=2, resultclass=Observed).run(suite)
    recording[0] = False
    summary = {
        "collected": collected, "started": result.started, "testsRun": result.testsRun,
        "skipped": [[test.id(), reason] for test, reason in result.skipped],
        "failures": [[test.id(), text] for test, text in result.failures],
        "errors": [[test.id(), text] for test, text in result.errors],
        "events": events,
    }
    with open(summary_path, "x", encoding="utf-8") as stream:
        json.dump(summary, stream, indent=2)
    raise SystemExit(0 if result.wasSuccessful() else 1)
    ''')

# Runs named existing tests unchanged. The parent test process refuses Python writes below
# the real HOME or the repository outside the owned run; /dev/null and other devices pass.
# Grandchildren are not audited: the stand-in runtime gives product processes their own HOME.
PROTECTED_HARNESS = textwrap.dedent('''\
    import json, os, sys, unittest

    summary_path, owner, tests_root, repo = sys.argv[1:5]
    names = sys.argv[5:]
    denied = [os.path.realpath(os.path.expanduser("~")), os.path.realpath(repo)]
    owner = os.path.realpath(owner)
    blocked = []
    WRITE = os.O_WRONLY | os.O_RDWR | os.O_CREAT | os.O_APPEND | os.O_TRUNC
    # (path index, dir_fd index) of each mutating audit event; rmtree and tempfile
    # cleanup pass names relative to an open directory descriptor.
    TARGETS = {
        "os.mkdir": [(0, 2)], "os.remove": [(0, 1)], "os.rmdir": [(0, 1)], "shutil.rmtree": [(0, 1)],
        "os.chmod": [(0, 2)], "os.utime": [(0, 3)], "os.truncate": [(0, None)],
        "os.rename": [(0, 2), (1, 3)], "os.replace": [(0, 2), (1, 3)], "os.link": [(1, 3)], "os.symlink": [(1, 2)],
    }

    def below(path, root):
        return path == root or path.startswith(root + os.sep)

    def check(path, dir_fd=None):
        if not isinstance(path, (str, bytes)):
            return
        text = os.fsdecode(path)
        if not os.path.isabs(text) and isinstance(dir_fd, int):
            text = os.path.join(os.readlink(f"/proc/self/fd/{dir_fd}"), text)
        absolute = os.path.realpath(os.path.abspath(text))
        if below(absolute, owner):
            return
        if any(below(absolute, root) for root in denied):
            blocked.append(absolute)
            raise PermissionError(f"protected write outside the owned run: {absolute}")

    def hook(event, arguments):
        if event == "open":
            path, mode, flags = arguments
            if (isinstance(mode, str) and any(c in mode for c in "wax+")) or bool((flags or 0) & WRITE):
                check(path)
        for path_index, fd_index in TARGETS.get(event, ()):
            check(arguments[path_index], None if fd_index is None else arguments[fd_index])

    sys.path.insert(0, tests_root)
    sys.addaudithook(hook)
    suite = unittest.defaultTestLoader.loadTestsFromNames(names)
    result = unittest.TextTestRunner(verbosity=2).run(suite)
    summary = {
        "names": names, "testsRun": result.testsRun,
        "skipped": [[test.id(), reason] for test, reason in result.skipped],
        "failures": [[test.id(), text] for test, text in result.failures],
        "errors": [[test.id(), text] for test, text in result.errors],
        "blockedWrites": blocked,
    }
    with open(summary_path, "x", encoding="utf-8") as stream:
        json.dump(summary, stream, indent=2)
    raise SystemExit(0 if result.wasSuccessful() else 1)
    ''')


def workflow_requirement_script():
    """The inline Python of the workflow's requirement step, dedented as YAML would."""
    lines = WORKFLOW.read_text(encoding="utf-8").splitlines()
    step = next(index for index, line in enumerate(lines) if "Run checker requirement regressions" in line)
    start = next(index for index in range(step, len(lines)) if lines[index].rstrip().endswith("<<'PY'"))
    end = next(index for index in range(start + 1, len(lines)) if lines[index].strip() == "PY")
    return textwrap.dedent("\n".join(lines[start + 1:end])) + "\n"
