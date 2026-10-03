"""Extractor selection, evidence roots, locks and execution results through the real entry shell.

Requirement source: 01_Phases/goals/2026-10-03-codegraph-adapter-cleanup/execution-contract.md.
Roslyn is the default and must not need or look at the CodeGraph installation; CodeGraph and
Compare are explicit. A new evidence root lives below Git-excluded `.backups/`; unsafe, foreign
or historical roots are refused before anything is written. Every attempt leaves
execution-result.json: `completed` (exit 0, analysis partial kept apart), `unavailable` (a
prerequisite is missing or a tool cannot start) or `failed` (a started command, timeout or
validation failed), both nonzero, with a reason, a message and a repair. check accepts only the
stored results of the latest completed run of the same selection and inputs.

The runs use support/stand_in_runtime.py: the real run-wsl.sh and Pipeline with stand-in
CodeGraph, Node and .NET programs inside a temporary Git source and HOME. They prove selection,
files, processes and results, never real extractor output; the real runs are recorded separately.
"""
import hashlib
import json
import os
import pathlib
import shutil
import subprocess
import sys
import textwrap
import time
import unittest

from support.stand_in_runtime import LABELS, StandInRuntime, template
from test_measure_sequence import full_comparison_steps

CODEGRAPH_INSTALLATION = ("node_modules", "codegraph-linux-x64", "/bundle", "syntax-context")
ROSLYN_TOOLING = ("dotnet-10.0.301", "roslyn-input", "/Roslyn/", "global.json", "nuget")


def _windows_temporary_folder():
    """The Windows TEMP folder as a WSL path, or None without WSL Windows interop."""
    if not all(shutil.which(name) for name in ("git.exe", "wslpath", "cmd.exe")) or not pathlib.Path("/mnt/c").is_dir():
        return None
    completed = subprocess.run(["cmd.exe", "/d", "/c", "echo %TEMP%"], capture_output=True, text=True, cwd="/mnt/c")
    folder = completed.stdout.strip()
    if completed.returncode != 0 or not folder:
        return None
    return subprocess.run(["wslpath", "-u", folder], capture_output=True, text=True, check=True).stdout.strip()


WINDOWS_TEMPORARY = _windows_temporary_folder()


def _read(path):
    return json.loads(pathlib.Path(path).read_text(encoding="utf-8-sig"))


def _tree(root):
    """{relative path: SHA256 or 'dir'} of everything below root (empty when absent)."""
    root = pathlib.Path(root)
    if not root.exists():
        return {}
    return {
        path.relative_to(root).as_posix(): "dir" if path.is_dir() else hashlib.sha256(path.read_bytes()).hexdigest()
        for path in sorted(root.rglob("*")) if not path.is_symlink()
    }


class SelectionCase(unittest.TestCase):
    def start(self, **options):
        runtime = StandInRuntime(**options)
        self.addCleanup(runtime.close)
        return runtime

    def assert_completed(self, completed, runtime, action):
        self.assertEqual(0, completed.returncode, completed.stderr)
        result = _read(runtime.evidence / "execution-result.json")
        self.assertEqual((action, runtime.selection, "completed"), (result["action"], result["selection"], result["executionStatus"]))
        return result

    def assert_result(self, completed, runtime, action, status, reason):
        self.assertEqual(1, completed.returncode, completed.stdout)
        self.assertIn("Repair:", completed.stderr)
        result = _read(runtime.evidence / "execution-result.json")
        self.assertEqual((action, runtime.selection, status, reason),
                         (result["action"], result["selection"], result["executionStatus"], result["reasonCode"]))
        self.assertTrue(result["repair"])
        self.assertIn(f"-Action {action}", result["retryCommand"])
        attempts = sorted((runtime.evidence / "attempts").glob("*.json"))
        self.assertEqual(result, _read(attempts[-1]), "each attempt is kept beside the latest result")
        return result

    def batch_folders(self, runtime):
        batch = pathlib.Path(_read(runtime.evidence / "latest-run.json")["batch"])
        return batch, sorted(path.parent.relative_to(batch).as_posix() for path in batch.rglob("command.json"))


class SelectionRunTests(SelectionCase):
    def test_default_is_roslyn_without_looking_at_codegraph_installation(self):
        runtime = self.start(selection="roslyn", bundle=False, prepare=False)
        trace = runtime.trace_environment(CODEGRAPH_INSTALLATION)
        default_path = runtime.wsl("path", defaults=True)
        self.assertEqual((0, str(runtime.runtime)), (default_path.returncode, default_path.stdout.strip()), default_path.stderr)
        self.assertIn("-roslyn-", runtime.runtime.name)
        shell_traces = []
        for action in ("prepare", "measure", "check"):
            shell_trace = runtime.root / f"xtrace-{action}.txt"
            completed = runtime.wsl(action, defaults=True, environment=trace, xtrace=shell_trace)
            result = self.assert_completed(completed, runtime, action)
            self.assertEqual(["Roslyn"], result["selectedExtractors"])
            shell_traces.append(shell_trace.read_text(encoding="utf-8"))
        self.assertEqual(runtime.source / ".backups/architecture/roslyn", runtime.evidence)

        # Neither the Python steps nor the shell looked up, copied or hashed the installation.
        self.assertEqual([], runtime.accesses())
        for text in shell_traces:
            self.assertTrue(text.strip(), "the shell trace was captured")
            self.assertEqual([], [line for line in text.splitlines() if any(item in line for item in CODEGRAPH_INSTALLATION)])
        self.assertEqual({"dotnet:--version", "dotnet:restore", "dotnet:build", "dotnet:analyze", "dotnet:format"},
                         {call["key"] for call in runtime.calls()})
        for name in ("bundle", "codegraph-input", "tool/CodeGraph"):
            self.assertFalse((runtime.runtime / name).exists(), name)

        batch, folders = self.batch_folders(runtime)
        self.assertEqual(sorted(["sdk-version", "tool-restore", "tool-build", "server-restore", "clientnet-restore",
                                 *(f"roslyn/{label}/{step}" for label in LABELS for step in ("analysis", "scoring"))]), folders)
        latest = _read(runtime.evidence / "latest-run.json")
        self.assertEqual(("roslyn", ["Roslyn"]), (latest["selection"], latest["selectedExtractors"]))
        self.assertEqual([("Roslyn", label) for label in LABELS], [(run["extractor"], run["label"]) for run in latest["runs"]])
        self.assertEqual(["roslyn-input"], _read(batch / "input-copy.json")["verifiedCopies"])
        config = _read(batch / "config.json")
        self.assertEqual(("roslyn", ["Roslyn"]), (config["selection"], config["selectedExtractors"]))
        self.assertFalse({"codegraphVersion", "codegraphMode"} & set(config))
        self.assertEqual([], [item["path"] for item in config["implementationFiles"] if item["path"].startswith("CodeGraph/")])
        self.assertFalse([key for key in _read(batch / "environment.json") if key.startswith("codegraph")])
        # Command completion and analysis completeness are separate: the fixture is partial.
        check = _read(runtime.evidence / "execution-result.json")
        self.assertEqual(["partial"] * 4, [item["status"] for item in check["analysisStatus"]])

    def test_explicit_codegraph_needs_no_sdk_and_copies_no_roslyn_tool(self):
        runtime = self.start(selection="codegraph", dotnet=False, prepare=False)
        trace = runtime.trace_environment(ROSLYN_TOOLING)
        for action in ("prepare", "measure", "check"):
            shell_trace = runtime.root / f"xtrace-{action}.txt"
            self.assert_completed(runtime.wsl(action, environment=trace, xtrace=shell_trace), runtime, action)
            lines = shell_trace.read_text(encoding="utf-8").splitlines()
            self.assertEqual([], [line for line in lines if any(item in line for item in ("dotnet-10.0.301", "global.json", "roslyn-input"))])
        self.assertEqual([], runtime.accesses())
        self.assertEqual({"codegraph"}, {call["key"].split(":")[0] for call in runtime.calls()} - {"node"})
        for name in ("roslyn-input", "tool/Roslyn", "global.json", "tool/.editorconfig"):
            self.assertFalse((runtime.runtime / name).exists(), name)
        batch, folders = self.batch_folders(runtime)
        self.assertFalse([folder for folder in folders if folder.split("/")[0] in {"sdk-version", "tool-restore", "tool-build", "server-restore", "clientnet-restore", "roslyn"}])
        self.assertEqual(["codegraph-input"], _read(batch / "input-copy.json")["verifiedCopies"])
        self.assertFalse({"sdk", "configuration", "unityMode"} & set(_read(batch / "config.json")))
        self.assertFalse((runtime.evidence / "format").exists(), "the Roslyn format check is not part of CodeGraph")

    def test_explicit_compare_keeps_both_extractors_order_and_closes_lock_descriptors(self):
        runtime = self.start(selection="compare", prepare=False)
        for action in ("prepare", "measure", "check"):
            self.assert_completed(runtime.wsl(action), runtime, action)
        batch = pathlib.Path(_read(runtime.evidence / "latest-run.json")["batch"])
        records = sorted((_read(path)["startedUtc"], path) for path in batch.rglob("command.json"))
        placeholders = {str(batch): "<batch>", str(runtime.runtime): "<runtime>", str(runtime.dotnet): "<dotnet>", str(runtime.source): "<source>"}
        steps = [(path.parent.relative_to(batch).as_posix(), [template(item, placeholders) for item in _read(path)["argv"]],
                  template(_read(path)["cwd"], placeholders)) for _, path in records]
        self.assertEqual(full_comparison_steps(runtime.settings["goalPath"]), steps)
        self.assertEqual(["roslyn-input", "codegraph-input"], _read(batch / "input-copy.json")["verifiedCopies"])
        # run-wsl.sh holds the runtime/evidence locks on descriptors 8 and 9 for the whole run;
        # no extractor or scoring child may inherit them.
        self.assertTrue(runtime.calls())
        self.assertEqual([], [call for call in runtime.calls() if {8, 9} & set(call["fds"])])


class SelectionRejectionTests(SelectionCase):
    def snapshot(self, runtime):
        return _tree(runtime.source / ".backups"), _tree(runtime.home / ".cache")

    def test_unknown_selection_action_or_argument_is_refused_without_fallback(self):
        runtime = self.start(selection="roslyn", prepare=False)
        before = self.snapshot(runtime)
        status_tool = runtime.source / "99_Tools/Architecture/Pipeline/execution_status.py"
        runner = runtime.source / "99_Tools/Architecture/Pipeline/runner.py"
        cases = {
            "shell path": runtime.wsl("path", selection="Roslyn"),
            "shell prepare": runtime.wsl("prepare", selection="codegraph-or-roslyn"),
            "status path": subprocess.run([sys.executable, "-B", str(status_tool), "path", "--source", str(runtime.source), "--extractor", "auto"],
                                          capture_output=True, text=True, env=runtime.environment()),
            "runner measure": subprocess.run([sys.executable, "-B", str(runner), "measure", "--source", str(runtime.source),
                                              "--runtime", str(runtime.runtime), "--evidence", str(runtime.evidence), "--extractor", "both"],
                                             capture_output=True, text=True, env=runtime.environment()),
        }
        for name, completed in cases.items():
            with self.subTest(name):
                self.assertEqual(1, completed.returncode, completed.stderr)
                self.assertIn("Unknown extractor selection", completed.stderr)
                self.assertIn("roslyn|codegraph|compare", completed.stderr)
        for name, arguments in {"action": ("frobnicate",), "argument": ("path", "--bogus")}.items():
            with self.subTest(name):
                completed = runtime.wsl(*arguments, defaults=True)
                self.assertEqual(1, completed.returncode, completed.stderr)
                self.assertIn("Repair:", completed.stderr)
        self.assertEqual(before, self.snapshot(runtime))
        self.assertEqual([], runtime.calls())

    def test_unsafe_historical_or_foreign_evidence_roots_are_refused_before_any_write(self):
        runtime = self.start(selection="roslyn", prepare=False)
        backups = runtime.source / ".backups"
        historical = runtime.settings["evidencePath"]
        (runtime.source / historical / "runs/old").mkdir(parents=True)
        (runtime.source / historical / "latest-run.json").write_text('{"batch": "old"}', encoding="utf-8")
        (runtime.source / runtime.settings["freezeRecordPath"]).write_text("{}", encoding="utf-8")
        (backups / "old").mkdir()
        (backups / "old/result.json").write_text("{}", encoding="utf-8")
        elsewhere = runtime.root / "elsewhere"
        elsewhere.mkdir()
        os.symlink(elsewhere, backups / "linked")
        os.symlink(elsewhere, backups / "link-parent")
        owned = self.start(selection="compare", evidence=".backups/compare-owned")
        cases = {
            "absolute": "/tmp/evidence",
            "parent escape": "../outside",
            "dot-dot inside": ".backups/../escape",
            "not below .backups": "build/evidence",
            "backslash": ".backups\\evidence",
            "linked root": ".backups/linked",
            "linked ancestor": ".backups/link-parent/evidence",
            "historical root": historical,
            "inside historical root": historical + "/new",
            "around historical root": ".backups/verification",
            "around freeze record": str(pathlib.PurePosixPath(runtime.settings["freezeRecordPath"]).parent),
            "nonempty unowned root": ".backups/old",
        }
        before = self.snapshot(runtime)
        for name, evidence in cases.items():
            for action in ("path", "prepare"):
                with self.subTest(f"{name} {action}"):
                    completed = runtime.wsl(action, evidence=evidence)
                    self.assertEqual(1, completed.returncode, completed.stdout)
                    self.assertIn("Repair:", completed.stderr)
                    self.assertEqual(before, self.snapshot(runtime))
        # A root owned by another selection (in the same source) is refused, not adopted.
        owned_before = _tree(owned.evidence)
        for action in ("path", "prepare", "check"):
            with self.subTest(f"other selection {action}"):
                completed = owned.wsl(action, selection="roslyn")
                self.assertEqual(1, completed.returncode, completed.stdout)
                self.assertIn("belongs to another source/selection", completed.stderr)
        self.assertEqual(owned_before, _tree(owned.evidence))
        self.assertEqual([], runtime.calls())

    def test_evidence_root_must_be_confirmed_git_excluded(self):
        runtime = self.start(selection="roslyn", prepare=False)
        (runtime.source / ".gitignore").write_text(".backups/*\n!.backups/tracked/\n", encoding="utf-8")
        before = self.snapshot(runtime)
        completed = runtime.wsl("prepare", evidence=".backups/tracked/evidence")
        self.assertEqual(1, completed.returncode, completed.stdout)
        self.assertIn("not confirmed Git-excluded", completed.stderr)
        self.assertEqual(before, self.snapshot(runtime))

    def test_running_executor_lock_is_reported_without_stopping_it(self):
        runtime = self.start(selection="compare", faults={"codegraph:init": {"sleep": 8}})
        result_before = (runtime.evidence / "execution-result.json").read_bytes()
        executor = runtime.start_wsl("measure")
        self.addCleanup(lambda: executor.poll() is None and executor.kill())
        deadline = time.monotonic() + 60
        while not any(call["key"] == "codegraph:init" for call in runtime.calls()):
            self.assertIsNone(executor.poll(), executor.stderr.read() if executor.poll() is not None else "")
            self.assertLess(time.monotonic(), deadline, "the first executor never reached its analysis")
            time.sleep(0.2)
        result_during = (runtime.evidence / "execution-result.json").read_bytes()
        second = runtime.wsl("measure")
        self.assertEqual(1, second.returncode, second.stdout)
        self.assertIn("Another executor holds runtime lock", second.stderr)
        self.assertIn("do not stop its process", second.stderr)
        direct = runtime.measure()
        self.assertEqual(1, direct.returncode, direct.stdout)
        self.assertIn("Execution lock is held by another executor", direct.stderr)
        self.assertEqual(result_during, (runtime.evidence / "execution-result.json").read_bytes(), "a refused executor wrote no result")
        self.assertIsNone(executor.poll(), "the lock holder kept running")
        stdout, stderr = executor.communicate(timeout=120)
        self.assertEqual(0, executor.returncode, stderr)
        self.assertNotEqual(result_before, (runtime.evidence / "execution-result.json").read_bytes())
        self.assertEqual("completed", _read(runtime.evidence / "execution-result.json")["executionStatus"])
        self.assertEqual(1, len(list((runtime.evidence / "runs").iterdir())))


class ExecutionStatusTests(SelectionCase):
    def test_missing_codegraph_installation_is_unavailable_without_roslyn_fallback(self):
        for selection in ("codegraph", "compare"):
            with self.subTest(selection):
                runtime = self.start(selection=selection, bundle=False, prepare=False)
                for action in ("prepare", "measure"):
                    completed = runtime.wsl(action)
                    result = self.assert_result(completed, runtime, action, "unavailable", "codegraph_bundle_missing")
                    for text in ("install-codegraph.ps1", "package-lock.json", "Linux x64", "bundled Node", "C# grammar",
                                 "Preserve the existing installation/cache"):
                        self.assertIn(text, result["repair"])
                    self.assertIn(f"Retry: pwsh -File 99_Tools/Architecture/run-architecture.ps1 -Action {action}", completed.stderr)
                self.assertEqual([], runtime.calls(), "no CodeGraph, Node or .NET command started")
                self.assertFalse((runtime.runtime / "roslyn-input").exists(), "no Roslyn-only fallback")
                self.assertFalse((runtime.evidence / "latest-run.json").exists())
                self.assertFalse((runtime.evidence / "runs").exists())

    def test_each_missing_or_unstartable_codegraph_prerequisite_names_its_reason(self):
        cases = {
            "codegraph_node_missing": lambda runtime: (runtime.bundle_source / "node").unlink(),
            "codegraph_grammar_missing": lambda runtime: (runtime.bundle_source / "lib/dist/extraction/wasm/tree-sitter-c_sharp.wasm").unlink(),
            "codegraph_input_missing": lambda runtime: (runtime.bundle_source / "lib/dist/db/schema.sql").unlink(),
            "codegraph_context_missing": lambda runtime: (runtime.source / "99_Tools/Architecture/CodeGraph/syntax-context.cjs").unlink(),
            "codegraph_not_executable": lambda runtime: (runtime.bundle_source / "node").chmod(0o644),
        }
        for reason, damage in cases.items():
            with self.subTest(reason):
                runtime = self.start(selection="codegraph", prepare=False)
                damage(runtime)
                result = self.assert_result(runtime.prepare(), runtime, "prepare", "unavailable", reason)
                self.assertIn("install-codegraph.ps1", result["repair"])
                self.assertEqual([], runtime.calls())

    def test_missing_or_unstartable_sdk_is_unavailable_for_roslyn(self):
        runtime = self.start(selection="roslyn", dotnet=False)
        result = self.assert_result(runtime.wsl("measure"), runtime, "measure", "unavailable", "sdk_unavailable")
        self.assertIn("10.0.301", result["repair"])
        self.assertFalse((runtime.evidence / "runs").exists())

        runtime = self.start(selection="roslyn")
        runtime.dotnet.write_text("#!/nonexistent/interpreter\n", encoding="utf-8")
        result = self.assert_result(runtime.wsl("measure"), runtime, "measure", "unavailable", "command_start_unavailable")
        batch = sorted((runtime.evidence / "runs").iterdir())[-1]
        record = _read(batch / "sdk-version/command.json")
        self.assertTrue(record["startUnavailable"], record)
        self.assertEqual(str(batch / "sdk-version/command.json"), result["commandRecord"])
        self.assertFalse((runtime.evidence / "latest-run.json").exists())

    def test_missing_frozen_manifest_or_source_capture_is_unavailable(self):
        runtime = self.start(selection="roslyn", prepare=False)
        runtime.manifest_path.rename(runtime.manifest_path.with_suffix(".moved"))
        claimed = runtime.wsl("prepare", metadata=False)
        self.assert_result(claimed, runtime, "prepare", "unavailable", "manifest_missing")
        runtime.manifest_path.with_suffix(".moved").rename(runtime.manifest_path)
        self.assert_result(runtime.wsl("prepare", metadata=False), runtime, "prepare", "unavailable", "source_metadata_missing")

    def test_started_analysis_failure_is_failed_and_keeps_every_run(self):
        runtime = self.start(selection="roslyn", faults={"dotnet:analyze": {"call": 1, "exit": 2}})
        completed = runtime.wsl("measure")
        result = self.assert_result(completed, runtime, "measure", "failed", "analysis_failed")
        batch = pathlib.Path(result["batch"])
        self.assertEqual([str(batch / "roslyn/cold/analysis/command.json")], result["commandRecord"])
        self.assertEqual(["failed", "partial", "partial", "partial"], [item["status"] for item in result["analysisStatus"]])
        self.assertEqual("notScored", _read(batch / "roslyn/cold/score.json")["status"])
        self.assertEqual(4, sum(1 for call in runtime.calls() if call["key"] == "dotnet:analyze"))
        self.assertFalse((runtime.evidence / "latest-run.json").exists())

    def test_timeout_is_failed_and_ends_only_that_process_group(self):
        runtime = self.start(selection="roslyn", faults={"dotnet:analyze": {"call": 1, "sleep": 60}})
        run = runtime.measure(command_timeout=3)
        self.assertEqual(1, run.returncode, run.stderr)
        result = _read(runtime.evidence / "execution-result.json")
        self.assertEqual(("failed", "analysis_failed"), (result["executionStatus"], result["reasonCode"]))
        record = _read(run.batch / "roslyn/cold/analysis/command.json")
        self.assertEqual((124, "Timeout; terminated only this command process group"), (record["exitCode"], record["failure"]))
        sleeper = next(call["pid"] for call in runtime.calls() if call["key"] == "dotnet:analyze")
        with self.assertRaises(ProcessLookupError):
            os.kill(sleeper, 0)
        self.assertEqual(4, sum(1 for call in runtime.calls() if call["key"] == "dotnet:analyze"), "later runs still ran")

    def test_invalid_analysis_output_is_a_failed_validation(self):
        runtime = self.start(selection="roslyn")
        (runtime.state / "roslyn-raw.json").write_text('{"rawVersion": 1, "extractor": "Roslyn"}', encoding="utf-8")
        completed = runtime.wsl("measure")
        self.assertEqual(1, completed.returncode, completed.stdout)
        result = _read(runtime.evidence / "execution-result.json")
        self.assertEqual("failed", result["executionStatus"])
        self.assertNotEqual("completed", result["reasonCode"])
        self.assertFalse((runtime.evidence / "latest-run.json").exists())


class CheckTests(SelectionCase):
    def measured(self, selection="roslyn"):
        runtime = self.start(selection=selection)
        self.assert_completed(runtime.wsl("measure"), runtime, "measure")
        return runtime

    def test_check_without_stored_results_fails(self):
        runtime = self.start(selection="roslyn")
        completed = runtime.wsl("check")
        self.assertEqual(1, completed.returncode, completed.stdout)
        result = _read(runtime.evidence / "execution-result.json")
        self.assertEqual(("check", "unavailable"), (result["action"], result["executionStatus"]))
        self.assertIn("latest-run.json", result["message"])
        self.assertFalse((runtime.evidence / "latest-run.json").exists())

    def test_failed_measure_after_success_is_not_certified_by_the_earlier_batch(self):
        runtime = self.measured()
        first = pathlib.Path(_read(runtime.evidence / "latest-run.json")["batch"])
        first_tree = _tree(first)
        runtime.set_faults({"dotnet:analyze": {"call": 5, "exit": 2}})
        self.assertEqual(1, runtime.wsl("measure").returncode)
        self.assertEqual(str(first), _read(runtime.evidence / "latest-run.json")["batch"], "latest-run still names the earlier batch")
        completed = runtime.wsl("check")
        self.assertEqual(1, completed.returncode, completed.stdout)
        self.assertIn("Latest execution did not complete this selection", completed.stderr)
        self.assertEqual(first_tree, _tree(first), "the earlier batch is neither rewritten nor certified")
        self.assertEqual(2, len(list((runtime.evidence / "runs").iterdir())))

    def test_check_refuses_changed_implementation_inputs_or_incomplete_results(self):
        def implementation(runtime):
            with (runtime.source / "99_Tools/Architecture/Roslyn/Program.cs").open("a", encoding="utf-8") as stream:
                stream.write("// changed after the run\n")

        def manifest(runtime):
            value = _read(runtime.manifest_path)
            value["includedRoots"].append("03_Client")
            runtime.manifest_path.write_text(json.dumps(value), encoding="utf-8")

        def missing_run(runtime):
            latest = _read(runtime.evidence / "latest-run.json")
            latest["runs"] = latest["runs"][:3]
            (runtime.evidence / "latest-run.json").write_text(json.dumps(latest), encoding="utf-8")

        def missing_snapshot(runtime):
            batch = pathlib.Path(_read(runtime.evidence / "latest-run.json")["batch"])
            (batch / "roslyn/warm2/normalized.json").unlink()

        cases = {
            "Current implementation differs from recorded batch": implementation,
            "Stored mode/manifest/source SHA differs": manifest,
            "Selected extractor results are missing, extra or out of order": missing_run,
            "normalized.json": missing_snapshot,
        }
        for message, change in cases.items():
            with self.subTest(message):
                runtime = self.measured()
                change(runtime)
                completed = runtime.wsl("check")
                self.assertEqual(1, completed.returncode, completed.stdout)
                result = _read(runtime.evidence / "execution-result.json")
                self.assertEqual("check", result["action"])
                self.assertIn(result["executionStatus"], {"failed", "unavailable"})
                self.assertIn(message, result["message"])

    def test_selected_roots_are_separate_and_keep_earlier_batches(self):
        roslyn = self.measured("roslyn")
        first = pathlib.Path(_read(roslyn.evidence / "latest-run.json")["batch"])
        first_tree = _tree(first)
        self.assert_completed(roslyn.wsl("measure"), roslyn, "measure")
        second = pathlib.Path(_read(roslyn.evidence / "latest-run.json")["batch"])
        self.assertNotEqual(first, second)
        self.assertEqual(first_tree, _tree(first))
        roots = {}
        for selection in ("codegraph", "compare"):
            path = roslyn.wsl("path", selection=selection, evidence=f".backups/architecture/{selection}")
            self.assertEqual(0, path.returncode, path.stderr)
            roots[selection] = path.stdout.strip()
            self.assertEqual(0, roslyn.wsl("prepare", selection=selection, evidence=f".backups/architecture/{selection}").returncode)
        self.assertEqual(3, len({str(roslyn.runtime), *roots.values()}))
        for selection, root in roots.items():
            owner = _read(roslyn.source / f".backups/architecture/{selection}/.dawnholder-execution.json")
            self.assertEqual((selection, root), (owner["selection"], owner["runtimeRoot"]))
        self.assertEqual(first_tree, _tree(first))

    def test_adopted_pointer_root_is_reusable_while_unowned_history_stays_protected(self):
        runtime = self.start(selection="compare", evidence=".backups/new-compare")
        historical = runtime.source / runtime.settings["evidencePath"]
        (historical / "runs/old").mkdir(parents=True)
        (historical / "latest-run.json").write_text('{"batch": "old"}', encoding="utf-8")
        history = _tree(historical)
        self.assert_completed(runtime.wsl("measure"), runtime, "measure")
        first = pathlib.Path(_read(runtime.evidence / "latest-run.json")["batch"])
        # The later one-field settings change: evidencePath names the verified new root.
        settings_path = runtime.source / "99_Tools/Architecture/comparison-settings.json"
        settings = _read(settings_path)
        settings["evidencePath"] = ".backups/new-compare"
        settings_path.write_text(json.dumps(settings, indent=2) + "\n", encoding="utf-8")
        self.assert_completed(runtime.wsl("check"), runtime, "check")
        first_tree = _tree(first)
        self.assert_completed(runtime.wsl("measure"), runtime, "measure")
        self.assertNotEqual(str(first), _read(runtime.evidence / "latest-run.json")["batch"])
        self.assertEqual(first_tree, _tree(first), "the adopted batch keeps its bytes")
        completed = runtime.wsl("prepare", evidence=runtime.settings["evidencePath"])
        self.assertEqual(1, completed.returncode, completed.stdout)
        self.assertIn("no current execution owner", completed.stderr)
        self.assertEqual(history, _tree(historical))


def _capture_text(metadata):
    """The capture as run-architecture.ps1 sends it: indented JSON with UTF-8 text, not escapes."""
    return json.dumps(metadata, ensure_ascii=False, indent=2) + "\n"


class MetadataStdinOwnershipTests(SelectionCase):
    """Only the metadata reader may consume the capture that run-architecture.ps1 pipes in.

    Product defect #1 (behavior-verification/verdict.md): under WSL interop a Git child of the
    path/claim queries read the stdin it inherited, so the capture never reached source-git.json.
    The Git first on PATH here reads all of its stdin before it runs the real Git, which is what
    that interop did; a Git query that still shares the capture therefore loses it. This needs no
    Windows interop, so it also runs where WindowsWorktreeEntryTests is skipped.
    """

    def draining_git(self, runtime):
        """PATH entry whose `git` drains stdin, logs how many bytes it got, then runs the real Git."""
        real = shutil.which("git")
        folder = runtime.root / "draining-git"
        folder.mkdir()
        log = runtime.root / "draining-git.jsonl"
        program = folder / "git"
        program.write_text(textwrap.dedent(f"""\
            #!{sys.executable}
            import json, os, sys
            taken = sys.stdin.buffer.read() if sys.stdin is not None else b""
            with open({str(log)!r}, "a", encoding="utf-8") as stream:
                stream.write(json.dumps({{"argv": sys.argv[1:], "stdinBytes": len(taken)}}) + "\\n")
            os.execv({real!r}, [{real!r}, *sys.argv[1:]])
            """), encoding="utf-8")
        program.chmod(0o755)
        return {"PATH": f"{folder}{os.pathsep}{os.environ.get('PATH', '')}"}, log

    def test_git_queries_never_receive_the_metadata_capture(self):
        runtime = self.start(selection="roslyn", prepare=False)
        path, log = self.draining_git(runtime)
        captures = {}
        for action in ("prepare", "measure"):
            metadata = {**runtime.metadata(), "gitStatus": [f" M 99_Tools/설명-{action}.md"]}
            captures[action] = metadata
            self.assert_completed(runtime.wsl(action, metadata=_capture_text(metadata), environment=path), runtime, action)
            self.assertEqual({**metadata, "selection": "roslyn", "selectedExtractors": ["Roslyn"]},
                             _read(runtime.evidence / "source-git.json"), action)
        queries = [json.loads(line) for line in log.read_text(encoding="utf-8").splitlines()]
        # The draining Git really answered the exclusion queries of path and claim, every time.
        self.assertGreaterEqual(sum(1 for query in queries if "check-ignore" in query["argv"]), 4, queries)
        self.assertEqual([], [query for query in queries if query["stdinBytes"]], "a Git query took part of the capture")
        batch = pathlib.Path(_read(runtime.evidence / "latest-run.json")["batch"])
        self.assertEqual({**captures["measure"], "selection": "roslyn", "selectedExtractors": ["Roslyn"]},
                         _read(batch / "source-git.json"), "the measured batch keeps that measure's capture")


@unittest.skipIf(WINDOWS_TEMPORARY is None, "needs WSL with Windows interop (git.exe, wslpath, cmd.exe)")
class WindowsWorktreeEntryTests(SelectionCase):
    """In a linked worktree made by Windows Git (an Orca workspace, whose .git file names a Windows
    gitdir), run-architecture.ps1's capture sent on stdin must reach source-git.json, so
    prepare/measure/check behave as in a plain clone. Regression for product defect #1
    (behavior-verification/verdict.md): the capture was lost and prepare failed."""

    def test_windows_worktree_keeps_the_stdin_capture_through_prepare_measure_check(self):
        runtime = self.start(selection="roslyn", prepare=False, windows_worktree=WINDOWS_TEMPORARY)
        self.assertTrue((runtime.source / ".git").is_file(), "the source is a linked worktree")
        metadata = runtime.metadata()
        self.assert_completed(runtime.wsl("prepare", metadata=metadata), runtime, "prepare")
        self.assertEqual({**metadata, "selection": "roslyn", "selectedExtractors": ["Roslyn"]},
                         _read(runtime.evidence / "source-git.json"))
        for action in ("measure", "check"):
            self.assert_completed(runtime.wsl(action), runtime, action)

    def test_windows_worktree_keeps_a_full_size_capture_in_the_batch_provenance(self):
        """The real capture of the frozen source is about 360 KB of indented UTF-8 (2,696 tree paths),
        far beyond one pipe buffer. Every byte of it, not only a first chunk, must reach
        source-git.json, the batch copy and input-copy.json's Git tree path list."""
        runtime = self.start(selection="roslyn", prepare=False, windows_worktree=WINDOWS_TEMPORARY)
        captures = {}
        for action in ("prepare", "measure"):
            metadata = runtime.metadata()
            for index in range(3000):
                path = f"03_Client/Assets/구조-{action}/폴더 {index // 100:02d}/파일-{index:04d}.cs"
                metadata["tree"][path] = hashlib.sha1(path.encode("utf-8")).hexdigest()
            metadata["gitStatus"] = [f" M 99_Tools/설명 {index}.md" for index in range(50)]
            text = _capture_text(metadata)
            self.assertGreater(len(text.encode("utf-8")), 300 * 1024, "the capture is real-size")
            captures[action] = metadata
            self.assert_completed(runtime.wsl(action, metadata=text), runtime, action)
            self.assertEqual({**metadata, "selection": "roslyn", "selectedExtractors": ["Roslyn"]},
                             _read(runtime.evidence / "source-git.json"), action)
        batch = pathlib.Path(_read(runtime.evidence / "latest-run.json")["batch"])
        self.assertEqual({**captures["measure"], "selection": "roslyn", "selectedExtractors": ["Roslyn"]},
                         _read(batch / "source-git.json"))
        # Every captured path, not their order (stored JSON is key-sorted).
        self.assertEqual(sorted(captures["measure"]["tree"]), sorted(_read(batch / "input-copy.json")["gitTreePaths"]))
        self.assert_completed(runtime.wsl("check"), runtime, "check")

    def test_windows_git_query_failure_still_refuses_before_any_write(self):
        """Closing the query's stdin must not turn a failed Windows Git query into a pass: an
        evidence root Git does not ignore, or a worktree Git cannot open, is refused with Git's own
        error before any runtime, lock, marker or capture is written."""
        def tracked_root(runtime):
            (runtime.source / ".gitignore").write_text(".backups/*\n!.backups/tracked/\n", encoding="utf-8")
            return ".backups/tracked/evidence"

        def unreadable_worktree(runtime):
            # Still a Windows gitdir pointer (Windows Git is chosen), but to a gitdir that is gone.
            (runtime.source / ".git").write_text("gitdir: C:/dawnholder-missing-gitdir/worktrees/source\n", encoding="utf-8")
            return runtime.evidence_relative

        for name, damage in {"root not ignored": tracked_root, "worktree unreadable": unreadable_worktree}.items():
            with self.subTest(name):
                runtime = self.start(selection="roslyn", prepare=False, windows_worktree=WINDOWS_TEMPORARY)
                evidence = damage(runtime)
                windows_source = subprocess.run(["wslpath", "-w", str(runtime.source)], capture_output=True,
                                                text=True, check=True, stdin=subprocess.DEVNULL).stdout.strip()
                direct = subprocess.run(["git.exe", "-C", windows_source, "check-ignore", "--quiet", "--", evidence + "/"],
                                        capture_output=True, text=True, stdin=subprocess.DEVNULL)
                self.assertNotEqual(0, direct.returncode, "precondition: Windows Git itself does not confirm the root")
                before = (_tree(runtime.source / ".backups"), _tree(runtime.home / ".cache"))
                for action in ("path", "prepare"):
                    completed = runtime.wsl(action, evidence=evidence)
                    self.assertEqual(1, completed.returncode, completed.stdout)
                    self.assertIn("not confirmed Git-excluded", completed.stderr)
                    self.assertIn("Repair:", completed.stderr)
                    if direct.stderr.strip():
                        self.assertIn(direct.stderr.strip(), completed.stderr, "Git's own error reaches the user")
                    self.assertEqual(before, (_tree(runtime.source / ".backups"), _tree(runtime.home / ".cache")))
                self.assertEqual([], runtime.calls())


if __name__ == "__main__":
    unittest.main()
