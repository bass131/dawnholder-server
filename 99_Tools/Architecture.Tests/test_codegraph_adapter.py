"""CodeGraph raw boundary: public imports, SQLite dump and the recorded real batch.

`normalization.dump_codegraph`, `normalization.normalize_codegraph` and
`runner.dump_codegraph` are existing import paths and stay usable after CodeGraph moved to its
own module. The dump contract is read from the CodeGraph core schema: the four core tables
(plus schema_versions when present) in rowid order, a fixed raw header, parser diagnostics for
files with errors, a JSON file and a SQLite backup beside it, and no output when the schema
is incomplete. Recorded batch tests copy the stored raw.db and installed bundle read-only and
skip with the reason when that local evidence or installation is absent.
"""
import hashlib
import json
import os
import pathlib
import shutil
import sqlite3
import subprocess
import sys
import tempfile
import unittest

from support import mini_inputs
from support.stand_in_runtime import PIPELINE, codegraph_tables
# Only the located batch is shared; importing the test class would collect it twice.
from test_final_batch_replay import LOCATION, SKIP_REASON

INSTALLED_BUNDLE = PIPELINE.parent / "CodeGraph/node_modules/@colbymchenry/codegraph-linux-x64"
# Keys runner.measure adds to the dumped raw after the dump itself.
ADDED_AFTER_DUMP = {"syntaxContext", "analysisInput"}


def pipeline_module(name):
    """Import a Pipeline module the way runner.py/cli.py see each other, without bytecode."""
    sys.dont_write_bytecode = True
    if str(PIPELINE) not in sys.path:
        sys.path.insert(0, str(PIPELINE))
    return __import__(name)


def _sha256(path):
    return hashlib.sha256(pathlib.Path(path).read_bytes()).hexdigest()


def _read(path):
    return json.loads(pathlib.Path(path).read_text(encoding="utf-8-sig"))


def write_database(path, tables, extra_tables=()):
    """A CodeGraph-shaped SQLite file; rows are inserted in the given order."""
    with sqlite3.connect(path) as connection:
        for name, rows in tables.items():
            columns = list(rows[0])
            quoted = ", ".join(f'"{column}"' for column in columns)
            connection.execute(f'CREATE TABLE "{name}" ({quoted})')
            marks = ", ".join("?" for _ in columns)
            connection.executemany(f'INSERT INTO "{name}" ({quoted}) VALUES ({marks})', [[row[column] for column in columns] for row in rows])
        for name in extra_tables:
            connection.execute(f'CREATE TABLE "{name}" (value)')
            connection.execute(f'INSERT INTO "{name}" VALUES (1)')
    connection.close()


def database_rows(path, table):
    with sqlite3.connect(f"file:{pathlib.Path(path).as_posix()}?mode=ro", uri=True) as connection:
        connection.row_factory = sqlite3.Row
        rows = [dict(row) for row in connection.execute(f'SELECT * FROM "{table}" ORDER BY rowid')]
    connection.close()
    return rows


class PublicImportTests(unittest.TestCase):
    def test_each_entry_module_imports_alone_in_a_fresh_interpreter(self):
        for name in ("normalization", "runner", "cli", "codegraph_adapter"):
            with self.subTest(name):
                probe = f"import sys; sys.path.insert(0, {str(PIPELINE)!r}); import {name}; import json; print(json.dumps(sorted(m for m in ('normalization', 'runner') if m in sys.modules)))"
                result = subprocess.run([sys.executable, "-B", "-c", probe], capture_output=True, text=True, timeout=60, env={**os.environ, "PYTHONDONTWRITEBYTECODE": "1"})
                self.assertEqual(0, result.returncode, result.stderr)
                if name == "codegraph_adapter":
                    self.assertEqual([], json.loads(result.stdout), "the CodeGraph module must not depend on runner/normalization")

    def test_existing_import_paths_still_dump_and_normalize(self):
        normalization = pipeline_module("normalization")
        runner = pipeline_module("runner")
        self.assertIs(normalization.dump_codegraph, runner.dump_codegraph)
        raw = mini_inputs.codegraph_raw()
        manifest = mini_inputs.manifest()
        builder = normalization.SnapshotBuilder(raw, manifest, "m" * 64, "c" * 64)
        normalization.normalize_codegraph(raw, builder)
        self.assertEqual(normalization.normalize(raw, manifest, "m" * 64, "c" * 64), builder.finish())


class DumpCodeGraphTests(unittest.TestCase):
    def setUp(self):
        self._directory = tempfile.TemporaryDirectory(prefix="architecture-dump-")
        self.addCleanup(self._directory.cleanup)
        self.root = pathlib.Path(self._directory.name)
        self.dump = pipeline_module("normalization").dump_codegraph

    def test_core_tables_dump_in_rowid_order_with_backup_and_parser_diagnostics(self):
        tables = codegraph_tables()
        tables["nodes"] = list(reversed(tables["nodes"]))
        tables["files"][0]["errors"] = "[]"
        tables["files"][1]["errors"] = '[{"message": "unexpected token"}]'
        database = self.root / "codegraph.db"
        write_database(database, tables, extra_tables=("vectors",))
        before = _sha256(database)
        raw = self.dump(database, self.root / "out/raw.json")

        self.assertEqual(before, _sha256(database), "the analysis database is opened read-only")
        self.assertEqual([], sorted(path.name for path in self.root.iterdir() if path.name.startswith("codegraph.db-")))
        self.assertEqual({"nodes", "edges", "files", "unresolved_refs", "schema_versions", "rawVersion", "extractor", "version", "status", "diagnostics"}, set(raw))
        for name in ("nodes", "edges", "files", "unresolved_refs", "schema_versions"):
            self.assertEqual(tables[name], raw[name], name)
        self.assertEqual((1, "CodeGraph", "1.6.1", "partial"), (raw["rawVersion"], raw["extractor"], raw["version"], raw["status"]))
        self.assertEqual("limitation", raw["diagnostics"][0]["kind"])
        self.assertEqual([{"kind": "parser", "path": tables["files"][1]["path"], "message": '[{"message": "unexpected token"}]'}], raw["diagnostics"][1:])
        self.assertEqual(raw, _read(self.root / "out/raw.json"))
        for name in ("nodes", "edges", "files", "unresolved_refs", "schema_versions", "vectors"):
            self.assertEqual(database_rows(database, name), database_rows(self.root / "out/raw.db", name), name)

    def test_schema_versions_is_optional(self):
        tables = codegraph_tables()
        del tables["schema_versions"]
        write_database(self.root / "codegraph.db", tables)
        raw = self.dump(self.root / "codegraph.db", self.root / "raw.json")
        self.assertNotIn("schema_versions", raw)

    def test_incomplete_schema_or_missing_database_writes_nothing(self):
        tables = codegraph_tables()
        del tables["unresolved_refs"]
        write_database(self.root / "partial.db", tables)
        with self.assertRaisesRegex(ValueError, "CodeGraph database lacks core schema"):
            self.dump(self.root / "partial.db", self.root / "partial/raw.json")
        with self.assertRaises(sqlite3.OperationalError):
            self.dump(self.root / "missing.db", self.root / "missing/raw.json")
        self.assertFalse((self.root / "missing.db").exists(), "a read-only open must not create the database")
        self.assertEqual([], [path.name for path in (self.root / "partial", self.root / "missing") if path.exists()])

    def test_dumped_database_normalizes_like_the_same_raw_rows(self):
        write_database(self.root / "codegraph.db", codegraph_tables())
        dumped = self.dump(self.root / "codegraph.db", self.root / "raw.json")
        dumped["syntaxContext"] = mini_inputs.codegraph_raw()["syntaxContext"]
        workspace = mini_inputs.Workspace()
        self.addCleanup(workspace.close)
        from_database = workspace.snapshot(dumped)
        from_fixture = workspace.snapshot(mini_inputs.codegraph_raw())
        self.assertEqual((from_fixture["nodes"], from_fixture["edges"]), (from_database["nodes"], from_database["edges"]))


@unittest.skipIf(LOCATION is None, SKIP_REASON)
class RecordedCodeGraphBatchTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.runs = [run for run in _read(LOCATION["batch"] / "measurements.json") if run["extractor"] == "CodeGraph"]
        cls._directory = tempfile.TemporaryDirectory(prefix="architecture-recorded-")
        cls.root = pathlib.Path(cls._directory.name)

    @classmethod
    def tearDownClass(cls):
        cls._directory.cleanup()

    def dumped(self, run):
        folder = pathlib.Path(run["folder"])
        copy = self.root / run["label"] / "analysis.db"
        copy.parent.mkdir(parents=True, exist_ok=True)
        # The recorded backup is copied first so nothing opens the original evidence file.
        shutil.copyfile(folder / "raw.db", copy)
        return folder, pipeline_module("normalization").dump_codegraph(copy, copy.with_name("raw.json"))

    def test_recorded_database_backup_dumps_to_the_recorded_raw(self):
        for run in self.runs:
            with self.subTest(run["label"]):
                folder, raw = self.dumped(run)
                recorded = _read(folder / "raw.json")
                self.assertEqual(ADDED_AFTER_DUMP, set(recorded) - set(raw))
                self.assertEqual({key: value for key, value in recorded.items() if key not in ADDED_AFTER_DUMP}, raw)

    def test_recorded_cold_database_replays_to_the_recorded_snapshot_bytes(self):
        run = next(run for run in self.runs if run["label"] == "cold")
        folder, raw = self.dumped(run)
        recorded = _read(folder / "raw.json")
        raw["syntaxContext"] = _read(folder / "syntax-context.json")
        raw["analysisInput"] = recorded["analysisInput"]
        self.assertEqual(recorded, raw)
        raw_path = self.root / "cold-replayed-raw.json"
        raw_path.write_text(json.dumps(raw), encoding="utf-8")
        out = self.root / "cold-replayed-normalized.json"
        result = mini_inputs.run_cli("normalize", "--raw", raw_path, "--config", folder / "extractor-config.json", "--manifest", LOCATION["manifest"], "--out", out)
        self.assertEqual(0, result.returncode, result.stderr)
        self.assertEqual((folder / "normalized.json").read_bytes(), out.read_bytes())

    @unittest.skipUnless(INSTALLED_BUNDLE.is_dir(), "the CodeGraph package is not installed under 99_Tools/Architecture/CodeGraph")
    def test_installed_bundle_reproduces_the_recorded_bundle_identity(self):
        runtime = self.root / "runtime"
        runtime.mkdir(exist_ok=True)
        try:
            os.symlink(INSTALLED_BUNDLE, runtime / "bundle")
        except FileExistsError:
            pass
        except OSError as error:
            self.skipTest(f"symbolic links are unavailable here: {error}")
        batch = self.root / "batch"
        batch.mkdir(exist_ok=True)
        shutil.copyfile(LOCATION["batch"] / "config.json", batch / "config.json")
        cold = pathlib.Path(next(run for run in self.runs if run["label"] == "cold")["folder"])
        config = pipeline_module("runner").extractor_config({"extractor": "CodeGraph", "version": "1.6.1"}, runtime, batch)
        self.assertEqual(_read(cold / "extractor-config.json"), config)
        environment = _read(LOCATION["batch"] / "environment.json")
        self.assertEqual(environment["codegraphNodeSha256"], _sha256(INSTALLED_BUNDLE / "node"))
        self.assertEqual(environment["codegraphLauncherSha256"], _sha256(INSTALLED_BUNDLE / "bin/codegraph"))


if __name__ == "__main__":
    unittest.main()
