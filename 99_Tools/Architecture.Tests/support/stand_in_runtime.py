"""Source/runtime/evidence-shaped workspace whose extractors are stand-in programs.

`run-architecture.ps1` captures Git metadata and calls `run-wsl.sh <action> --extractor
roslyn|codegraph|compare --evidence .backups/...`. `run-wsl.sh` derives the owned runtime from
the source, selection and evidence root, copies only the selected inputs and starts
`Pipeline/runner.py` from the source. This module builds the same layout in a temporary
folder: a Git source (with `.backups/` excluded) holding the real Pipeline modules, entry
points and settings, and a separate HOME so the derived runtime, locks and pinned SDK path stay
inside the folder. The CodeGraph launcher, the bundled Node and the .NET host are small Python
programs that accept the same argv and build a CodeGraph SQLite database, a syntax-context file
and a Roslyn raw file from the synthetic fixture in mini_inputs.

`prepare()` and `wsl()` run the real `run-wsl.sh`; the PowerShell capture is replaced by the
same JSON on stdin. `measure()` starts `runner.py measure` directly with run-wsl.sh's arguments
and environment, so a test can change the prepared runtime first. A run therefore shows
selection, process order, argv, cwd, files, results and failure handling. It never starts
CodeGraph, Node, .NET, a restore or a network step, so it says nothing about real extractor
output.
"""
import datetime
import fcntl
import hashlib
import json
import os
import pathlib
import subprocess
import sys
import tempfile

from support import mini_inputs

TOOL_ROOT = mini_inputs.REPOSITORY_ROOT / "99_Tools" / "Architecture"
PIPELINE = TOOL_ROOT / "Pipeline"
SETTINGS = TOOL_ROOT / "comparison-settings.json"
ENTRY_POINTS = ("run-wsl.sh", "run-architecture.ps1", "Architecture.Common.ps1")
LABELS = ("cold", "warm1", "warm2", "warm3")
BUNDLE_RELATIVE = "99_Tools/Architecture/CodeGraph/node_modules/@colbymchenry/codegraph-linux-x64"
# Tracked tool files other than Pipeline and entry points. Their bytes only identify the copy;
# the Roslyn files show whether a selection copied the Roslyn tool at all.
STAND_IN_TOOL_FILES = {
    "CodeGraph/syntax-context.cjs": "// stand-in; never executed\n",
    "Roslyn/Architecture.Roslyn.csproj": "<!-- stand-in; never built -->\n",
    "Roslyn/Program.cs": "// stand-in; never compiled\n",
}

# Files runner.measure copies from the bundle for source inspection, plus files the bundle
# config must ignore (a non-C# grammar and an ordinary module).
INSPECTED_BUNDLE_FILES = (
    "bin/codegraph.js", "directory.js", "index.js", "installer/index.js",
    "telemetry/index.js", "extraction/languages/csharp.js",
    "extraction/tree-sitter.js", "resolution/callback-synthesizer.js", "db/schema.sql",
)
C_SHARP_GRAMMAR = "lib/dist/extraction/wasm/tree-sitter-c_sharp.wasm"
OTHER_GRAMMAR = "lib/dist/extraction/wasm/tree-sitter-java.wasm"

_STAND_IN = '''#!{python}
"""Stand-in for the {role} executable; see support/stand_in_runtime.py."""
import json
import os
import pathlib
import sqlite3
import sys
import time

ROLE = {role!r}
# The state folder comes from the environment so the stand-in bytes (hashed as bundle
# identity) are the same in every workspace.
STATE = pathlib.Path(os.environ["ARCHITECTURE_STAND_IN_STATE"])


def verb(argv):
    if ROLE == "codegraph":
        if argv[-1] == "--help":
            return argv[0] + "-help"
        return argv[0]
    if ROLE == "node":
        return "context"
    if argv[0] in ("--version", "restore", "build", "format"):
        return argv[0]
    return "analyze"


def write_database(root):
    database = pathlib.Path(root) / ".codegraph" / "codegraph.db"
    database.parent.mkdir(parents=True, exist_ok=True)
    if database.exists():
        database.unlink()
    tables = json.loads((STATE / "codegraph-tables.json").read_text(encoding="utf-8"))
    with sqlite3.connect(database) as connection:
        for name, rows in tables.items():
            columns = list(rows[0])
            quoted = ", ".join(f'"{{column}}"' for column in columns)
            connection.execute(f'CREATE TABLE "{{name}}" ({{quoted}})')
            marks = ", ".join("?" for _ in columns)
            connection.executemany(
                f'INSERT INTO "{{name}}" ({{quoted}}) VALUES ({{marks}})',
                [[row[column] for column in columns] for row in rows],
            )
    connection.close()


def main(argv):
    # Descriptors inherited from the caller, listed before this program opens any file.
    descriptors = sorted(int(name) for name in os.listdir("/proc/self/fd"))
    key = ROLE + ":" + verb(argv)
    calls = STATE / "calls.jsonl"
    with calls.open("a", encoding="utf-8") as log:
        log.write(json.dumps({{"key": key, "argv": argv, "cwd": os.getcwd(), "fds": descriptors, "pid": os.getpid()}}) + "\\n")
    count = sum(1 for line in calls.read_text(encoding="utf-8").splitlines() if json.loads(line)["key"] == key)
    fault = json.loads((STATE / "faults.json").read_text(encoding="utf-8")).get(key)
    if fault and fault.get("call", count) == count:
        # A delay alone keeps the normal behaviour afterwards; stdout/exit replace it.
        if "sleep" in fault:
            time.sleep(fault["sleep"])
        if "stdout" in fault or "exit" in fault:
            if "stdout" in fault:
                print(fault["stdout"])
            return fault.get("exit", 0)
    if key == "codegraph:--version":
        print("1.6.1")
    elif key == "dotnet:--version":
        print("10.0.301")
    elif key in ("codegraph:init", "codegraph:index"):
        write_database(argv[1])
    elif key == "node:context":
        pathlib.Path(argv[-1]).write_text((STATE / "syntax-context.json").read_text(encoding="utf-8"), encoding="utf-8")
    elif key == "dotnet:analyze":
        pathlib.Path(argv[3]).write_text((STATE / "roslyn-raw.json").read_text(encoding="utf-8"), encoding="utf-8")
    return 0


sys.exit(main(sys.argv[1:]))
'''

# Imported by every Python process that has the trace folder on PYTHONPATH. It records, in a
# separate log, each open/stat/listing/copy/process start whose path or argv contains one of
# ARCHITECTURE_ACCESS_PATTERNS. It changes no result; the log is opened once before hooks run.
ACCESS_TRACE = '''"""Access trace for verification runs; see support/stand_in_runtime.py."""
import json
import os
import sys

_PATTERNS = tuple(item for item in os.environ.get("ARCHITECTURE_ACCESS_PATTERNS", "").split("|") if item)
_LOG = os.open(os.environ["ARCHITECTURE_ACCESS_LOG"], os.O_WRONLY | os.O_APPEND | os.O_CREAT, 0o644)
_EVENTS = {"open", "os.listdir", "os.scandir", "shutil.copyfile", "shutil.copytree", "os.chmod",
           "subprocess.Popen", "os.posix_spawn", "os.exec", "os.rename", "os.remove", "os.symlink"}


def _text(value):
    if isinstance(value, (list, tuple)):
        return " ".join(_text(item) for item in value)
    try:
        return os.fsdecode(value)
    except TypeError:
        return str(value)


def _record(event, arguments):
    text = _text(arguments)
    if any(pattern in text for pattern in _PATTERNS):
        line = json.dumps({"pid": os.getpid(), "script": sys.argv[0] if sys.argv else None,
                           "event": event, "value": text})
        os.write(_LOG, (line + "\\n").encode())


def _hook(event, arguments):
    if event in _EVENTS:
        _record(event, arguments)


def _wrap(name):
    original = getattr(os, name)

    def traced(path, *arguments, **keywords):
        _record("os." + name, (path,))
        return original(path, *arguments, **keywords)
    setattr(os, name, traced)


for _name in ("stat", "lstat", "access"):
    _wrap(_name)
sys.addaudithook(_hook)
'''

# runner.py imports execution.run_process by name; lowering its default bounds every command
# of that run, exercising the original process-group timeout without editing the product.
_TIMEOUT_WRAPPER = (
    "import os, runpy, sys\n"
    "sys.path.insert(0, os.path.dirname(sys.argv[2]))\n"
    "import execution\n"
    "execution.run_process.__defaults__ = (int(sys.argv[1]),)\n"
    "sys.argv = sys.argv[2:]\n"
    "runpy.run_path(sys.argv[0], run_name='__main__')\n"
)


def codegraph_tables():
    """CodeGraph core tables holding the synthetic fixture's rows."""
    raw = mini_inputs.codegraph_raw()
    tables = {name: raw[name] for name in ("nodes", "edges", "files", "unresolved_refs")}
    tables["schema_versions"] = [{"version": 1, "applied_at": 0}]
    return tables


def template(value, placeholders):
    """Replace concrete roots with placeholders; longest roots first so nested roots stay exact."""
    text = str(value)
    for root, name in sorted(placeholders.items(), key=lambda item: -len(item[0])):
        text = text.replace(root, name)
    return text


def read_json(path):
    return json.loads(pathlib.Path(path).read_text(encoding="utf-8-sig"))


class MeasureRun:
    """Outcome of one `runner.py measure` invocation and the files it left behind."""

    def __init__(self, runtime, completed):
        self.runtime = runtime
        self.returncode = completed.returncode
        self.stdout = completed.stdout
        self.stderr = completed.stderr
        stamps = sorted((runtime.evidence / "runs").glob("*")) if (runtime.evidence / "runs").exists() else []
        self.batch = stamps[-1] if stamps else None

    def placeholders(self):
        return {
            str(self.batch): "<batch>",
            str(self.runtime.runtime): "<runtime>",
            str(self.runtime.dotnet): "<dotnet>",
            str(self.runtime.source): "<source>",
        }

    def steps(self):
        """(folder relative to the batch, argv template, cwd template, record) in start order."""
        if self.batch is None:
            return []
        records = []
        for path in self.batch.rglob("command.json"):
            record = json.loads(path.read_text(encoding="utf-8"))
            records.append((record["startedUtc"], path.parent.relative_to(self.batch).as_posix(), record))
        placeholders = self.placeholders()
        return [
            (folder, [template(item, placeholders) for item in record["argv"]], template(record["cwd"], placeholders), record)
            for _, folder, record in sorted(records)
        ]


class StandInRuntime:
    """Temporary Git source, HOME, runtime and evidence for one selected run.

    `selection` is passed as `--extractor`; `evidence` is the repository-relative root
    (default `.backups/architecture/<selection>`, as the entry points choose). `bundle=False`
    leaves the CodeGraph installation out of the source and `dotnet=False` leaves out the
    pinned SDK host, as on a machine without them. `prepare=True` runs `run-wsl.sh prepare`
    and raises if it fails, so tests that observe a failing prepare pass `prepare=False`.
    `windows_worktree` names a Windows folder (as a WSL path) in which the source becomes a linked
    worktree made by Windows Git, like an Orca workspace; HOME and the stand-ins stay in WSL.
    The temporary HOME is passed only to the product processes this object starts.
    """

    def __init__(self, pipeline=PIPELINE, faults=None, selection="compare", evidence=None,
                 bundle=True, dotnet=True, prepare=True, windows_worktree=None):
        self._directory = tempfile.TemporaryDirectory(prefix="architecture-measure-")
        self.root = pathlib.Path(self._directory.name).resolve()
        self.home = self.root / "home"
        self._source_directory = None
        if windows_worktree is None:
            self.source = self.root / "source"
        else:
            # A linked worktree made by Windows Git: its .git file names a Windows gitdir, as in
            # an Orca workspace. It lives under the given Windows folder (seen from WSL).
            self._source_directory = tempfile.TemporaryDirectory(prefix="architecture-worktree-", dir=windows_worktree)
            self.source = self._make_windows_worktree(pathlib.Path(self._source_directory.name).resolve())
        self.state = self.root / "stand-in"
        self.trace = self.root / "trace"
        self.settings = json.loads(SETTINGS.read_text(encoding="utf-8-sig"))
        self.selection = selection
        self.evidence_relative = evidence or f".backups/architecture/{selection}"
        self.evidence = self.source / self.evidence_relative
        self.dotnet = self.home / self.settings["dotnetRelativePath"]
        self.bundle_source = self.source / BUNDLE_RELATIVE
        self.manifest_path = self.source / self.settings["goalPath"] / "input-manifest.json"
        self.pipeline = pathlib.Path(pipeline)
        self._layout(faults or {}, bundle, dotnet)
        located = self.wsl("path")
        if located.returncode != 0:
            raise RuntimeError(f"run-wsl.sh path failed: {located.stderr}")
        self.runtime = pathlib.Path(located.stdout.strip())
        if prepare:
            prepared = self.prepare()
            if prepared.returncode != 0:
                raise RuntimeError(f"run-wsl.sh prepare failed: {prepared.stderr}")

    def close(self):
        if self._source_directory is not None:
            self._source_directory.cleanup()
        self._directory.cleanup()

    @staticmethod
    def _make_windows_worktree(folder):
        def git(*arguments):
            subprocess.run(["git.exe", *arguments], check=True, capture_output=True)

        def windows(path):
            return subprocess.run(["wslpath", "-w", str(path)], check=True, capture_output=True, text=True).stdout.strip()

        main = folder / "main"
        main.mkdir()
        (main / "README.md").write_text("stand-in main worktree\n", encoding="utf-8")
        git("init", "--quiet", windows(main))
        git("-C", windows(main), "add", "README.md")
        git("-C", windows(main), "-c", "user.name=stand-in", "-c", "user.email=stand-in@example.invalid",
            "commit", "--quiet", "-m", "stand-in")
        git("-C", windows(main), "worktree", "add", "--quiet", "--detach", windows(folder / "source"))
        return folder / "source"

    def _write(self, path, value):
        path.parent.mkdir(parents=True, exist_ok=True)
        if isinstance(value, bytes):
            path.write_bytes(value)
        elif isinstance(value, str):
            path.write_text(value, encoding="utf-8")
        else:
            path.write_text(json.dumps(value, indent=2), encoding="utf-8")
        return path

    def _stand_in(self, path, role):
        self._write(path, _STAND_IN.format(python=sys.executable, role=role))
        path.chmod(0o755)

    def _layout(self, faults, bundle, dotnet):
        # Source: a Git work tree that excludes .backups/, as the evidence-root check requires.
        self._write(self.source / ".gitignore", ".backups/\n")
        if not (self.source / ".git").exists():
            subprocess.run(["git", "init", "--quiet", str(self.source)], check=True, capture_output=True)
        tool = self.source / "99_Tools/Architecture"
        for module in sorted(self.pipeline.glob("*.py")):
            self._write(tool / "Pipeline" / module.name, module.read_bytes())
        for name in ENTRY_POINTS:
            self._write(tool / name, (TOOL_ROOT / name).read_bytes())
        self._write(tool / "comparison-settings.json", SETTINGS.read_bytes())
        for relative, text in STAND_IN_TOOL_FILES.items():
            self._write(tool / relative, text)
        for name in ("global.json", ".editorconfig", "99_Tools/.editorconfig"):
            self._write(self.source / name, "# stand-in\n")
        manifest = mini_inputs.manifest()
        goal = self.manifest_path.parent
        self._write(self.manifest_path, manifest)
        self._write(goal / "evaluation-scope.json", mini_inputs.scope())
        self._write(goal / "truth.json", mini_inputs.truth())
        for path in mini_inputs.source_paths():
            self._write(self.source / path, f"// synthetic {path}\n")
        if bundle:
            for name in INSPECTED_BUNDLE_FILES:
                self._write(self.bundle_source / "lib/dist" / name, f"// bundle {name}\n")
            self._write(self.bundle_source / C_SHARP_GRAMMAR, b"\0asm c_sharp")
            self._write(self.bundle_source / OTHER_GRAMMAR, b"\0asm java")
            self._stand_in(self.bundle_source / "bin/codegraph", "codegraph")
            self._stand_in(self.bundle_source / "node", "node")
        if dotnet:
            self._stand_in(self.dotnet, "dotnet")
        self.home.mkdir(parents=True, exist_ok=True)
        self._write(self.state / "codegraph-tables.json", codegraph_tables())
        self._write(self.state / "syntax-context.json", mini_inputs.codegraph_raw()["syntaxContext"])
        self._write(self.state / "roslyn-raw.json", mini_inputs.roslyn_raw())
        self._write(self.state / "faults.json", faults)
        self._write(self.state / "calls.jsonl", "")

    def set_faults(self, faults):
        self._write(self.state / "faults.json", faults)

    def metadata(self):
        """The Git capture run-architecture.ps1 sends on stdin for prepare and measure."""
        manifest = read_json(self.manifest_path)
        return {
            "sourceCommit": manifest["sourceCommit"],
            "implementationHead": "f" * 40,
            "capturedUtc": datetime.datetime.now(datetime.timezone.utc).isoformat(),
            "manifestHash": hashlib.sha256(self.manifest_path.read_bytes()).hexdigest(),
            "tree": mini_inputs.git_metadata(manifest)["tree"],
            "frozenFiles": [],
            "freezeCommit": "e" * 40,
            "freezeCommittedUtc": "2000-01-01T00:00:00Z",
            "gitStatus": [],
        }

    def environment(self, extra=None):
        environment = {
            key: value for key, value in os.environ.items()
            if not key.startswith(("DOTNET_", "NUGET_", "CODEGRAPH_", "BASH_XTRACEFD", "PYTHONPATH"))
        }
        environment.update(HOME=str(self.home), PYTHONDONTWRITEBYTECODE="1", ARCHITECTURE_STAND_IN_STATE=str(self.state))
        environment.update(extra or {})
        return environment

    def trace_environment(self, patterns):
        """PYTHONPATH/log settings that make each Python process record matching accesses."""
        self._write(self.trace / "sitecustomize.py", ACCESS_TRACE)
        log = self.trace / "access.jsonl"
        log.touch()
        return {"PYTHONPATH": str(self.trace), "ARCHITECTURE_ACCESS_LOG": str(log),
                "ARCHITECTURE_ACCESS_PATTERNS": "|".join(patterns)}

    def accesses(self):
        log = self.trace / "access.jsonl"
        return [json.loads(line) for line in log.read_text(encoding="utf-8").splitlines()] if log.exists() else []

    def wsl(self, action, *arguments, selection=None, evidence=None, metadata=None, defaults=False,
            environment=None, xtrace=None):
        """Run the real run-wsl.sh as run-architecture.ps1 does and return CompletedProcess.

        prepare/measure receive the metadata capture on stdin unless `metadata=False`; a str is
        sent as is (already serialized, e.g. indented UTF-8 like ConvertTo-Json output).
        `defaults=True` omits --extractor/--evidence to observe the shell defaults. `xtrace`
        names a file that receives the shell's own command trace (bash -x) apart from stderr.
        """
        command = ["bash", str(self.source / "99_Tools/Architecture/run-wsl.sh"), action]
        if not defaults:
            command += ["--extractor", selection or self.selection, "--evidence", evidence or self.evidence_relative]
        command += list(arguments)
        stdin = None
        if action in ("prepare", "measure") and metadata is not False:
            command.append("--metadata-stdin")
            stdin = metadata if isinstance(metadata, str) else json.dumps(metadata or self.metadata())
        extra = dict(environment or {})
        descriptors = ()
        stream = None
        if xtrace is not None:
            stream = open(xtrace, "w", encoding="utf-8")
            # Above run-wsl.sh's lock descriptors 8/9, so the trace never replaces a lock.
            traced = fcntl.fcntl(stream.fileno(), fcntl.F_DUPFD, 20)
            descriptors = (traced,)
            extra["BASH_XTRACEFD"] = str(traced)
            command.insert(1, "-x")
        try:
            return subprocess.run(command, cwd=self.source, input=stdin, capture_output=True, text=True,
                                  env=self.environment(extra), pass_fds=descriptors, timeout=600)
        finally:
            for descriptor in descriptors:
                os.close(descriptor)
            if stream is not None:
                stream.close()

    def start_wsl(self, action):
        """Start run-wsl.sh in the background (another executor); the caller waits for it."""
        command = ["bash", str(self.source / "99_Tools/Architecture/run-wsl.sh"), action,
                   "--extractor", self.selection, "--evidence", self.evidence_relative]
        if action in ("prepare", "measure"):
            command.append("--metadata-stdin")
        process = subprocess.Popen(command, cwd=self.source, stdin=subprocess.PIPE, stdout=subprocess.PIPE,
                                   stderr=subprocess.PIPE, text=True, env=self.environment())
        process.stdin.write(json.dumps(self.metadata()) if action in ("prepare", "measure") else "")
        process.stdin.close()
        return process

    def prepare(self, **options):
        return self.wsl("prepare", **options)

    def measure(self, command_timeout=None, environment=None):
        """Start runner.py measure directly with run-wsl.sh's arguments and environment.

        `command_timeout` lowers the original helper's per-command timeout (seconds).
        """
        extra = dict(environment or {})
        runner = self.source / "99_Tools/Architecture/Pipeline/runner.py"
        arguments = ["measure", "--source", str(self.source), "--runtime", str(self.runtime),
                     "--evidence", str(self.evidence), "--extractor", self.selection]
        if self.selection != "codegraph":
            extra.update(DOTNET_CLI_HOME=str(self.root / "dotnet-state/cli-home"), DOTNET_HOST_PATH=str(self.dotnet))
            arguments += ["--dotnet", str(self.dotnet)]
        if self.selection != "roslyn":
            extra.update(DO_NOT_TRACK="1", CODEGRAPH_TELEMETRY="0", CODEGRAPH_NO_UPDATE_CHECK="1", CODEGRAPH_NO_DAEMON="1")
        if command_timeout is None:
            command = [sys.executable, "-B", str(runner), *arguments]
        else:
            command = [sys.executable, "-B", "-c", _TIMEOUT_WRAPPER, str(command_timeout), str(runner), *arguments]
        completed = subprocess.run(command, cwd=self.runtime, capture_output=True, text=True,
                                   env=self.environment(extra), timeout=600)
        return MeasureRun(self, completed)

    def calls(self):
        lines = (self.state / "calls.jsonl").read_text(encoding="utf-8").splitlines()
        return [json.loads(line) for line in lines]
