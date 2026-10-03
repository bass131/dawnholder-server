"""Comparison run order, argv and failure handling through `runner.py measure`.

The expected step table below is the comparison contract: CodeGraph then Roslyn, cold then
three warm runs, CodeGraph preflight before the SDK, and CodeGraph's DB dump and
syntax-context only after a successful CodeGraph analysis. One test checks that the recorded
real batch followed this table; the others run the current runner with stand-in executables
(support/stand_in_runtime.py) and compare against the same table. Stand-in runs prove
orchestration only, never real extractor output.
"""
import hashlib
import json
import os
import pathlib
import unittest

from support import mini_inputs
from support.stand_in_runtime import C_SHARP_GRAMMAR, INSPECTED_BUNDLE_FILES, LABELS, StandInRuntime, template
# Only the located batch is shared; importing the test class would collect it twice.
from test_final_batch_replay import LOCATION, SKIP_REASON

LAUNCHER = "<runtime>/bundle/bin/codegraph"
ROSLYN_PROJECT = "<runtime>/tool/Roslyn/Architecture.Roslyn.csproj"
TIMEOUT_SECONDS = 600


def _sha256(path):
    return hashlib.sha256(pathlib.Path(path).read_bytes()).hexdigest()


def _read(path):
    return json.loads(pathlib.Path(path).read_text(encoding="utf-8-sig"))


def preflight_steps():
    return [
        ("codegraph-version", [LAUNCHER, "--version"], "<runtime>"),
        ("codegraph-init-help", [LAUNCHER, "init", "--help"], "<runtime>"),
        ("codegraph-index-help", [LAUNCHER, "index", "--help"], "<runtime>"),
        ("sdk-version", ["<dotnet>", "--version"], "<runtime>"),
        ("tool-restore", ["<dotnet>", "restore", ROSLYN_PROJECT, "--nologo"], "<runtime>"),
        ("tool-build", ["<dotnet>", "build", ROSLYN_PROJECT, "--no-restore", "--nologo", "-p:UseSharedCompilation=false"], "<runtime>"),
        ("server-restore", ["<dotnet>", "restore", "02_Server/GameServer/GameServer.csproj", "--nologo"], "<runtime>/roslyn-input"),
        ("clientnet-restore", ["<dotnet>", "restore", "04_ClientNet/Dawnholder.Client.Net.csproj", "--nologo"], "<runtime>/roslyn-input"),
    ]


def codegraph_steps(label, with_context=True):
    root = "<runtime>/codegraph-input"
    verb = ["init", root, "--yes"] if label == "cold" else ["index", root, "--quiet"]
    steps = [(f"codegraph/{label}/analysis", [LAUNCHER, *verb], root)]
    if with_context:
        steps.append((f"codegraph/{label}/syntax-context", [
            "<runtime>/bundle/node", "--liftoff-only", "--disable-warning=ExperimentalWarning",
            "<runtime>/tool/CodeGraph/syntax-context.cjs", "<runtime>/bundle", root,
            "<runtime>/manifest.json", f"<batch>/codegraph/{label}/syntax-context.json",
        ], root))
    return steps


def roslyn_step(label):
    return (f"roslyn/{label}/analysis", [
        "<dotnet>", "<runtime>/tool/Roslyn/bin/Debug/net10.0/Architecture.Roslyn.dll",
        "<runtime>/roslyn-input", "<runtime>/manifest.json", f"<batch>/roslyn/{label}/raw.json",
    ], "<runtime>/roslyn-input")


def scoring_step(extractor, label, goal_path):
    folder = f"<batch>/{extractor}/{label}"
    goal = f"<source>/{goal_path}"
    return (f"{extractor}/{label}/scoring", [
        "python3", "<runtime>/tool/Pipeline/cli.py", "score",
        "--snapshot", f"{folder}/normalized.json",
        "--manifest", "<runtime>/manifest.json",
        "--scope", f"{goal}/evaluation-scope.json",
        "--truth", f"{goal}/truth.json",
        "--out", f"{folder}/score.json",
    ], "<runtime>")


def full_comparison_steps(goal_path):
    steps = preflight_steps()
    for label in LABELS:
        steps += codegraph_steps(label)
    steps += [roslyn_step(label) for label in LABELS]
    steps += [scoring_step(extractor, label, goal_path) for extractor in ("codegraph", "roslyn") for label in LABELS]
    return steps


def observed(steps):
    return [(folder, argv, cwd) for folder, argv, cwd, _ in steps]


class RecordedBatchOrderTests(unittest.TestCase):
    @unittest.skipIf(LOCATION is None, SKIP_REASON)
    def test_recorded_real_batch_followed_the_step_table(self):
        batch = LOCATION["batch"]
        evidence = LOCATION["evidence"]
        settings = _read(mini_inputs.REPOSITORY_ROOT / "99_Tools/Architecture/comparison-settings.json")
        source = evidence
        for _ in pathlib.PurePosixPath(settings["evidencePath"]).parts:
            source = source.parent
        version = _read(batch / "codegraph-version/command.json")
        sdk = _read(batch / "sdk-version/command.json")
        placeholders = {str(batch): "<batch>", version["cwd"]: "<runtime>", sdk["argv"][0]: "<dotnet>", str(source): "<source>"}
        records = sorted((_read(path)["startedUtc"], path) for path in batch.rglob("command.json"))
        steps = []
        for _, path in records:
            record = _read(path)
            steps.append((path.parent.relative_to(batch).as_posix(), [template(item, placeholders) for item in record["argv"]], template(record["cwd"], placeholders)))
            self.assertEqual(TIMEOUT_SECONDS, record["timeoutSeconds"], path)
        self.assertEqual(full_comparison_steps(settings["goalPath"]), steps)


class MeasureSequenceTests(unittest.TestCase):
    def start(self, faults=None):
        runtime = StandInRuntime(faults=faults)
        self.addCleanup(runtime.close)
        return runtime

    def assert_failed_with(self, run, message):
        self.assertEqual(1, run.returncode, run.stderr)
        self.assertIn("ERROR: " + message, run.stderr)

    def test_compare_run_keeps_order_argv_and_records(self):
        runtime = self.start()
        stale = runtime.runtime / "codegraph-input/.codegraph/stale.db"
        stale.parent.mkdir(parents=True)
        stale.write_bytes(b"previous analysis")
        run = runtime.measure()
        self.assertEqual(0, run.returncode, run.stderr)
        steps = run.steps()
        self.assertEqual(full_comparison_steps(runtime.settings["goalPath"]), observed(steps))
        self.assertEqual([TIMEOUT_SECONDS], sorted({record["timeoutSeconds"] for _, _, _, record in steps}))

        # The previous CodeGraph cache moves aside before the cold run; cold starts empty.
        stamp = run.batch.name
        self.assertEqual(b"previous analysis", (runtime.runtime / "cache-archive" / stamp / "stale.db").read_bytes())
        runs = _read(run.batch / "measurements.json")
        self.assertEqual([(extractor, label) for extractor in ("CodeGraph", "Roslyn") for label in LABELS], [(row["extractor"], row["label"]) for row in runs])
        for row in runs:
            with self.subTest(f"{row['extractor']} {row['label']}"):
                folder = pathlib.Path(row["folder"])
                raw = _read(folder / "raw.json")
                self.assertEqual({"sourceCommit": mini_inputs.SOURCE_COMMIT, "manifestHash": _sha256(runtime.runtime / "manifest.json")}, raw["analysisInput"])
                if row["extractor"] == "CodeGraph":
                    self.assertEqual(row["label"] != "cold", row["cacheBefore"]["persistentAnalysisCache"])
                    self.assertEqual([".codegraph/codegraph.db"], [item["path"] for item in row["cacheAfter"]["files"]])
                    self.assertEqual(_sha256(runtime.runtime / "codegraph-input/.codegraph/codegraph.db"), row["cacheAfter"]["files"][0]["sha256"])
                    self.assertEqual(_read(folder / "syntax-context/command.json")["elapsedSeconds"], row["syntaxContextSeconds"])
                    self.assertEqual(mini_inputs.codegraph_raw()["syntaxContext"], raw["syntaxContext"])
                    self.assertTrue((folder / "raw.db").is_file(), "the SQLite backup sits next to raw.json")
                    self.assertEqual("partial", raw["status"])
                else:
                    self.assertFalse(row["cacheBefore"]["persistentAnalysisCache"])
                    self.assertEqual("fresh", row["cacheBefore"]["process"])
                    self.assertIsNone(row["syntaxContextSeconds"])
                    self.assertFalse((folder / "syntax-context").exists())
                self.assertEqual(_sha256(folder / "extractor-config.json"), _read(folder / "normalized.json")["extractor"]["configHash"])

        inspected = runtime.evidence / "install/source-inspection"
        for name in INSPECTED_BUNDLE_FILES:
            self.assertEqual((runtime.runtime / "bundle/lib/dist" / name).read_bytes(), (inspected / name).read_bytes(), name)
        config = _read(run.batch / "codegraph/cold/extractor-config.json")
        bundle = runtime.runtime / "bundle"
        self.assertEqual(
            [{"path": path, "sha256": _sha256(bundle / path)} for path in ("lib/dist/bin/codegraph.js", C_SHARP_GRAMMAR, "node")],
            config["bundleFiles"],
            "only the launcher script, the C# grammar and Node identify the bundle",
        )
        self.assertEqual("CodeGraph blanks conditional directives and indexes both branches; no compiler defines", config["preprocessorPolicy"])
        self.assertEqual(_sha256(run.batch / "config.json"), config["baseConfigHash"])
        environment = _read(run.batch / "environment.json")
        self.assertEqual((_sha256(bundle / "node"), _sha256(bundle / "bin/codegraph")), (environment["codegraphNodeSha256"], environment["codegraphLauncherSha256"]))

        # Provenance names every executed file of the stand-in tool copy: all Pipeline modules
        # (a moved or added module included) and syntax-context.cjs; settings JSON is not code.
        recorded = {item["path"]: item["sha256"] for item in _read(run.batch / "config.json")["implementationFiles"]}
        tool = runtime.runtime / "tool"
        copied = {f"Pipeline/{path.name}": _sha256(path) for path in (tool / "Pipeline").glob("*.py")}
        copied["CodeGraph/syntax-context.cjs"] = _sha256(tool / "CodeGraph/syntax-context.cjs")
        self.assertEqual(copied, recorded)

    def test_syntax_context_failure_stops_after_keeping_the_database_dump(self):
        runtime = self.start({"node:context": {"call": 2, "exit": 4}})
        run = runtime.measure()
        folder = run.batch / "codegraph/warm1"
        self.assert_failed_with(run, f"Command failed; see {folder / 'syntax-context'}/command.json and stdout/stderr")
        expected = preflight_steps() + codegraph_steps("cold") + codegraph_steps("warm1")
        self.assertEqual(expected, observed(run.steps()))
        raw = _read(folder / "raw.json")
        self.assertNotIn("syntaxContext", raw)
        self.assertNotIn("analysisInput", raw)
        self.assertEqual(len(mini_inputs.codegraph_raw()["nodes"]), len(raw["nodes"]))
        self.assertTrue((folder / "raw.db").is_file())
        self.assertFalse((folder / "normalized.json").exists())
        self.assertEqual(["cold"], [row["label"] for row in _read(runtime.evidence / "latest-run.json")["runs"]])
        self.assertFalse((run.batch / "measurements.json").exists())

    def test_failed_codegraph_analysis_is_kept_as_failed_without_dump_or_context(self):
        runtime = self.start({"codegraph:init": {"exit": 3}})
        run = runtime.measure()
        self.assertEqual(0, run.returncode, run.stderr)
        folders = [folder for folder, _, _ in observed(run.steps())]
        self.assertNotIn("codegraph/cold/syntax-context", folders)
        self.assertIn("codegraph/warm1/syntax-context", folders)
        self.assertIn("roslyn/warm3/analysis", folders)
        cold = run.batch / "codegraph/cold"
        raw = _read(cold / "raw.json")
        self.assertEqual(("failed", [{"kind": "execution", "exitCode": 3, "commandRecord": str(cold / "analysis/command.json")}]), (raw["status"], raw["diagnostics"]))
        self.assertEqual("1.6.1", raw["version"])
        self.assertFalse((cold / "raw.db").exists())
        snapshot = _read(cold / "normalized.json")
        self.assertEqual(("failed", [], []), (snapshot["status"], snapshot["nodes"], snapshot["edges"]))
        measurement = _read(cold / "measurement.json")
        self.assertEqual((3, "failed", None), (measurement["exitCode"], measurement["analysisStatus"], measurement["syntaxContextSeconds"]))
        self.assertEqual("notScored", _read(cold / "score.json")["status"])

    def test_linked_cache_is_refused_before_any_analysis(self):
        runtime = self.start()
        target = runtime.root / "elsewhere"
        target.mkdir()
        (target / "codegraph.db").write_bytes(b"not ours")
        try:
            os.symlink(target, runtime.runtime / "codegraph-input/.codegraph")
        except OSError as error:
            self.skipTest(f"symbolic links are unavailable here: {error}")
        run = runtime.measure()
        self.assert_failed_with(run, "Linked CodeGraph cache")
        self.assertEqual(preflight_steps(), observed(run.steps()))
        self.assertTrue((run.batch / "config.json").is_file(), "provenance is written before the cache is touched")
        self.assertEqual(b"not ours", (target / "codegraph.db").read_bytes())
        self.assertTrue((runtime.runtime / "codegraph-input/.codegraph").is_symlink())
        self.assertFalse((runtime.runtime / "cache-archive").exists())

    def test_version_pins_are_checked_sdk_first_before_restore(self):
        cases = {
            "CodeGraph version": ({"codegraph:--version": {"stdout": "1.6.2"}}, "CodeGraph version differs from approved bundle"),
            "both versions": ({"codegraph:--version": {"stdout": "1.6.2"}, "dotnet:--version": {"stdout": "10.0.100"}}, "SDK pin mismatch; no fallback"),
        }
        for name, (faults, message) in cases.items():
            with self.subTest(name):
                run = self.start(faults).measure()
                self.assert_failed_with(run, message)
                self.assertEqual(preflight_steps()[:4], observed(run.steps()))
                self.assertFalse((run.batch / "config.json").exists())

    def test_preflight_command_failure_stops_at_that_command(self):
        run = self.start({"codegraph:init-help": {"exit": 2}}).measure()
        self.assert_failed_with(run, f"Command failed; see {run.batch / 'codegraph-init-help'}/command.json and stdout/stderr")
        self.assertEqual(preflight_steps()[:2], observed(run.steps()))

    def test_missing_bundle_source_stops_before_any_process(self):
        runtime = self.start()
        (runtime.runtime / "bundle/lib/dist/db/schema.sql").unlink()
        run = runtime.measure()
        self.assertEqual(1, run.returncode, run.stderr)
        self.assertIn("No such file or directory", run.stderr)
        self.assertIn("db/schema.sql", run.stderr)
        self.assertEqual([], observed(run.steps()))
        self.assertEqual([], runtime.calls())


if __name__ == "__main__":
    unittest.main()
