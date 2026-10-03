"""Owned-runtime-shaped workspace whose extractors are stand-in programs.

`run-wsl.sh measure` copies 99_Tools/Architecture/ into an owned runtime and starts
`tool/Pipeline/runner.py measure` there. This module lays out the same source, runtime and
evidence folders in a temporary directory, copies the real Pipeline modules into tool/, and
replaces the CodeGraph launcher, the bundled Node and the .NET host with small Python
programs that accept the same argv. The stand-ins build a CodeGraph SQLite database, a
syntax-context file and a Roslyn raw file from the synthetic fixture in mini_inputs.

A run therefore shows process order, argv, cwd, files and failure handling of the comparison
pipeline. It never starts CodeGraph, Node, .NET, a restore or a network step, so it says
nothing about real extractor output.
"""
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
LABELS = ("cold", "warm1", "warm2", "warm3")

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
    if argv[0] in ("--version", "restore", "build"):
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
    key = ROLE + ":" + verb(argv)
    calls = STATE / "calls.jsonl"
    with calls.open("a", encoding="utf-8") as log:
        log.write(json.dumps({{"key": key, "argv": argv, "cwd": os.getcwd()}}) + "\\n")
    count = sum(1 for line in calls.read_text(encoding="utf-8").splitlines() if json.loads(line)["key"] == key)
    fault = json.loads((STATE / "faults.json").read_text(encoding="utf-8")).get(key)
    if fault and fault.get("call", count) == count:
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
    """Temporary source/runtime/evidence folders for one comparison run."""

    def __init__(self, pipeline=PIPELINE, faults=None):
        self._directory = tempfile.TemporaryDirectory(prefix="architecture-measure-")
        self.root = pathlib.Path(self._directory.name)
        self.source = self.root / "source"
        self.runtime = self.root / "runtime"
        self.evidence = self.root / "evidence"
        self.state = self.root / "stand-in"
        self.dotnet = self.state / "dotnet-host" / "dotnet"
        self.settings = json.loads(SETTINGS.read_text(encoding="utf-8-sig"))
        self.pipeline = pathlib.Path(pipeline)
        self._layout(faults or {})

    def close(self):
        self._directory.cleanup()

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

    def _layout(self, faults):
        manifest = mini_inputs.manifest()
        # Tool copy as run-wsl.sh makes it: Pipeline modules, settings and syntax-context.cjs.
        for module in sorted(self.pipeline.glob("*.py")):
            self._write(self.runtime / "tool/Pipeline" / module.name, module.read_bytes())
        self._write(self.runtime / "tool/comparison-settings.json", SETTINGS.read_bytes())
        self._write(self.runtime / "tool/CodeGraph/syntax-context.cjs", b"// stand-in; never executed\n")
        self._write(self.runtime / "manifest.json", manifest)
        for root in ("roslyn-input", "codegraph-input"):
            for path in mini_inputs.source_paths():
                self._write(self.runtime / root / path, f"// synthetic {path}\n")
        for name in INSPECTED_BUNDLE_FILES:
            self._write(self.runtime / "bundle/lib/dist" / name, f"// bundle {name}\n")
        self._write(self.runtime / "bundle" / C_SHARP_GRAMMAR, b"\0asm c_sharp")
        self._write(self.runtime / "bundle" / OTHER_GRAMMAR, b"\0asm java")
        self._stand_in(self.runtime / "bundle/bin/codegraph", "codegraph")
        self._stand_in(self.runtime / "bundle/node", "node")
        self._stand_in(self.dotnet, "dotnet")
        goal = self.source / self.settings["goalPath"]
        self._write(goal / "evaluation-scope.json", mini_inputs.scope())
        self._write(goal / "truth.json", mini_inputs.truth())
        self._write(self.evidence / "source-git.json", {
            "sourceCommit": manifest["sourceCommit"],
            "tree": mini_inputs.git_metadata(manifest)["tree"],
            "frozenFiles": [],
            "implementationHead": "f" * 40,
            "freezeCommit": "e" * 40,
            "freezeCommittedUtc": "2000-01-01T00:00:00Z",
        })
        self._write(self.evidence / "input-copy.json", {"copied": "stand-in"})
        self._write(self.state / "codegraph-tables.json", codegraph_tables())
        self._write(self.state / "syntax-context.json", mini_inputs.codegraph_raw()["syntaxContext"])
        self._write(self.state / "roslyn-raw.json", mini_inputs.roslyn_raw())
        self._write(self.state / "faults.json", faults)
        self._write(self.state / "calls.jsonl", "")

    def measure(self):
        environment = {
            **os.environ,
            "PYTHONDONTWRITEBYTECODE": "1",
            "DOTNET_CLI_HOME": str(self.root / "dotnet-state/cli-home"),
            "DO_NOT_TRACK": "1",
            "CODEGRAPH_TELEMETRY": "0",
            "CODEGRAPH_NO_UPDATE_CHECK": "1",
            "CODEGRAPH_NO_DAEMON": "1",
            "ARCHITECTURE_STAND_IN_STATE": str(self.state),
        }
        command = [
            sys.executable, "-B", str(self.runtime / "tool/Pipeline/runner.py"), "measure",
            "--source", str(self.source),
            "--runtime", str(self.runtime),
            "--evidence", str(self.evidence),
            "--dotnet", str(self.dotnet),
        ]
        completed = subprocess.run(command, cwd=self.runtime, capture_output=True, text=True, env=environment, timeout=600)
        return MeasureRun(self, completed)

    def calls(self):
        lines = (self.state / "calls.jsonl").read_text(encoding="utf-8").splitlines()
        return [json.loads(line) for line in lines]

