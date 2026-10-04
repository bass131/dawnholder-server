"""Contract v1 requirement TDD through the same public Linux/CI entry point."""

import importlib.util
import hashlib
import json
import os
import pathlib
import subprocess
import time
import unittest
import uuid

from support.module_boundary_fixture import PROJECT, write_fixture, write_git_fixture


REPO = pathlib.Path(__file__).resolve().parents[2]
ENTRY = REPO / "99_Tools/Architecture/check-module-boundaries.sh"
RULES = REPO / "99_Tools/Architecture/Boundaries/module-boundaries.json"
WORK_VALUE = os.environ.get("MODULE_BOUNDARIES_TEST_WORK")
WORK = None
SKIP_REASON = "Set MODULE_BOUNDARIES_TEST_WORK to a new owned absolute directory to run SDK10.0.301 requirement checks"


@unittest.skipIf(WORK_VALUE is None, SKIP_REASON)
class ModuleBoundaryRequirements(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        global WORK
        if not WORK_VALUE or not pathlib.Path(WORK_VALUE).is_absolute():
            raise ValueError("MODULE_BOUNDARIES_TEST_WORK must be a nonempty absolute path")
        root = pathlib.Path(WORK_VALUE)
        if ".." in root.parts or any(path.is_symlink() for path in (root, *root.parents)):
            raise ValueError("MODULE_BOUNDARIES_TEST_WORK must not traverse parents or a symlink")
        permitted = [REPO / ".backups"]
        runner_temp = os.environ.get("RUNNER_TEMP")
        if runner_temp and pathlib.Path(runner_temp).is_absolute():
            permitted.append(pathlib.Path(runner_temp))
        if not any(parent in root.parents for parent in permitted):
            raise ValueError("MODULE_BOUNDARIES_TEST_WORK must be below .backups or RUNNER_TEMP")
        if root.exists():
            raise ValueError("MODULE_BOUNDARIES_TEST_WORK must be a new owned directory")
        root.mkdir(parents=True)
        WORK = root
        cls.cache = {}

    def invoke(self, mode="matrix", extra=(), environment=None, cache=False):
        if cache and mode in self.cache:
            return self.cache[mode]
        run = WORK / uuid.uuid4().hex
        source = write_fixture(run / "source", mode)
        output = run / "result"
        command = ["bash", str(ENTRY), "--source-root", str(source), "--output-root", str(output), "--project", PROJECT, "--input-kind", "fixture", *map(str, extra)]
        env = dict(os.environ)
        env["PYTHONDONTWRITEBYTECODE"] = "1"
        if environment:
            env.update(environment)
        started = time.perf_counter()
        completed = subprocess.run(command, cwd=REPO, env=env, capture_output=True, text=True, timeout=660)
        raw = {"argv": command, "exitCode": completed.returncode, "elapsedSeconds": time.perf_counter() - started}
        (run / "invocation.json").write_text(json.dumps(raw, indent=2) + "\n", encoding="utf-8")
        (run / "stdout.txt").write_text(completed.stdout, encoding="utf-8")
        (run / "stderr.txt").write_text(completed.stderr, encoding="utf-8")
        result_path = output / "result.json"
        result = json.loads(result_path.read_text()) if result_path.is_file() else None
        value = completed, result, run
        if cache:
            self.cache[mode] = value
        return value

    def matrix(self):
        completed, result, _ = self.invoke(cache=True)
        self.assertEqual(completed.returncode, 0, completed.stderr)
        self.assertIsNotNone(result, "Public entry must produce a current result.json")
        self.assertEqual(result["executionStatus"], "completed")
        self.assertEqual(result["analysisStatus"], "warnings")
        return result

    def test_handlers_map_alias_field_is_mb001(self):
        result = self.matrix()
        field_assert = any(v["ruleId"] == "MB001" and v["source"]["path"].endswith("FieldDependency.cs") for v in result["violations"])
        self.assertTrue(field_assert, "alias + field type must resolve to Maps")

    def test_fully_qualified_signature_is_mb001(self):
        result = self.matrix()
        signature_assert = any(v["ruleId"] == "MB001" and v["source"]["path"].endswith("SignatureDependency.cs") for v in result["violations"])
        self.assertTrue(signature_assert)

    def test_compiled_member_call_is_mb001(self):
        result = self.matrix()
        call_assert = any(v["ruleId"] == "MB001" and v["source"]["path"].endswith("CallDependency.cs") and "Tick" in v["target"]["symbol"] for v in result["violations"])
        self.assertTrue(call_assert)

    def test_maps_handlers_is_mb002(self):
        result = self.matrix()
        reverse_assert = any(v["ruleId"] == "MB002" and v["source"]["path"].endswith("ReverseDependency.cs") for v in result["violations"])
        self.assertTrue(reverse_assert)

    def test_sessions_concrete_handler_is_mb003(self):
        result = self.matrix()
        concrete_assert = any(v["ruleId"] == "MB003" and v["source"]["path"].endswith("ConcreteDependency.cs") for v in result["violations"])
        self.assertTrue(concrete_assert)

    def test_exact_dispatcher_types_are_allowed(self):
        result = self.matrix()
        dispatcher_assert = not any(v["source"]["path"].endswith("DispatcherUse.cs") for v in result["violations"])
        observed_assert = any(
            r["source"]["path"].endswith("DispatcherUse.cs")
            and r["target"]["type"] == "Dawnholder.Server.GameServer.Handlers.HandlerRegistry"
            for r in result["references"]
        )
        self.assertTrue(dispatcher_assert)
        self.assertTrue(observed_assert, "exception must be observed, not absent from analysis")

    def test_submit_send_and_session_map_directions_are_allowed(self):
        result = self.matrix()
        allowed_names = {"SubmitUse.cs", "SendUse.cs", "MapUse.cs"}
        observed_names = {pathlib.PurePosixPath(r["source"]["path"]).name for r in result["references"]}
        violation_names = {pathlib.PurePosixPath(v["source"]["path"]).name for v in result["violations"]}
        allowed_assert = not (allowed_names & violation_names)
        observed_assert = allowed_names <= observed_names
        self.assertTrue(allowed_assert)
        self.assertTrue(observed_assert)

    def test_comments_and_strings_do_not_warn(self):
        result = self.matrix()
        text_assert = not any(v["source"]["path"].endswith("TextOnly.cs") for v in result["violations"])
        self.assertTrue(text_assert)

    def test_source_target_and_raw_metrics_are_traceable(self):
        result = self.matrix()
        for violation in result["violations"]:
            self.assertGreater(violation["source"]["line"], 0)
            self.assertGreater(violation["source"]["column"], 0)
            self.assertTrue(violation["target"]["declaration"]["path"].endswith(".cs"))
        self.assertGreater(result["input"]["sourceFileCount"], 0)
        self.assertEqual(result["coverage"]["expectedBoundaryFiles"], result["coverage"]["analyzedBoundaryFiles"])
        self.assertGreater(result["elapsedSeconds"], 0)
        self.assertTrue(result["tool"]["files"])
        self.assertTrue(all(stage["argv"] and stage["exitCode"] == 0 for stage in result["stages"]))

    def test_zero_violations_is_distinct_completed_clean(self):
        completed, result, _ = self.invoke("clean", cache=True)
        self.assertEqual(completed.returncode, 0, completed.stderr)
        self.assertEqual(result["analysisStatus"], "clean")
        self.assertEqual(result["violations"], [])

    def assert_failure(self, mode="clean", extra=(), environment=None, reason=None):
        completed, result, _ = self.invoke(mode, extra, environment)
        self.assertNotEqual(completed.returncode, 0)
        self.assertIsNotNone(result, "Safe output root should preserve a failure result")
        self.assertNotEqual(result["executionStatus"], "completed")
        self.assertEqual(result["analysisStatus"], "not_completed")
        self.assertIsNone(result["violationCount"], "Unrun/partial analysis cannot claim zero violations")
        if reason:
            self.assertEqual(result["reasonCode"], reason)
        return result

    def test_unresolved_compile_is_analysis_failure(self):
        result = self.assert_failure("unresolved", reason="analysis_failed")
        unresolved_assert = any(d["kind"] == "compiler" and d["id"] == "CS0246" and "MissingType" in d["message"] for d in result["diagnostics"])
        self.assertTrue(unresolved_assert, "The source error itself must be observed, rather than an unrelated tool error")

    def test_inferred_array_and_generic_element_dependencies_are_mb001(self):
        completed, result, _ = self.invoke("inferred", cache=True)
        self.assertEqual(completed.returncode, 0, completed.stderr)
        for name in ("InferredArray.cs", "InferredGeneric.cs"):
            shape_assert = any(
                v["ruleId"] == "MB001" and v["source"]["path"].endswith(name)
                and v["target"]["type"] == "Dawnholder.Server.GameServer.Maps.GameMap"
                for v in result["violations"]
            )
            self.assertTrue(shape_assert, f"Resolved inferred element type is still a static dependency: {name}")

    def test_malformed_policy_shape_is_invalid_rules(self):
        for field, value in (("areas", []), ("project", "unexpected.csproj"), ("rules", None)):
            with self.subTest(field=field):
                policy = json.loads(RULES.read_text())
                policy[field] = value
                path = WORK / (field + "-malformed.json")
                path.write_text(json.dumps(policy))
                self.assert_failure(extra=("--rules", path), reason="invalid_rules")

    def test_output_inside_fixture_source_does_not_modify_source(self):
        run = WORK / uuid.uuid4().hex
        source = write_fixture(run / "source", "clean")
        output = source / "nested-output"
        completed = subprocess.run(
            ["bash", str(ENTRY), "--source-root", str(source), "--output-root", str(output), "--input-kind", "fixture"],
            cwd=REPO, capture_output=True, text=True,
        )
        self.assertNotEqual(completed.returncode, 0)
        untouched_assert = not output.exists()
        self.assertTrue(untouched_assert, "Reject source/output overlap before creating output or owner metadata")

    def test_source_symlink_is_rejected_before_copy(self):
        run = WORK / uuid.uuid4().hex
        source = write_fixture(run / "source", "clean")
        original = source / "02_Server/GameServer/Maps/GameMap.cs"
        alias = original.with_name("MapAlias.cs")
        alias.symlink_to(original)
        completed = subprocess.run(
            ["bash", str(ENTRY), "--source-root", str(source), "--output-root", str(run / "result"), "--input-kind", "fixture"],
            cwd=REPO, capture_output=True, text=True,
        )
        self.assertNotEqual(completed.returncode, 0)
        result = json.loads((run / "result/result.json").read_text())
        self.assertEqual(result["reasonCode"], "unsafe_path")

    def test_restore_execution_failure_is_not_policy_warning(self):
        host = WORK / "failing-host"
        host.write_text('#!/bin/sh\nif [ "$1" = "--version" ]; then echo 10.0.301; exit 0; fi\necho "deliberate restore failure" >&2\nexit 23\n')
        host.chmod(0o700)
        result = self.assert_failure(environment={"DAWNHOLDER_DOTNET": str(host)}, reason="execution_failed")
        stage_assert = result["stages"][-1]["name"] == "tool-restore" and result["stages"][-1]["exitCode"] == 23
        self.assertTrue(stage_assert)

    def test_warning_annotations_use_real_locations_and_fixture_label(self):
        completed, result, _ = self.invoke(extra=("--github-annotations",))
        annotations = [line for line in completed.stdout.splitlines() if line.startswith("::warning ")]
        self.assertEqual(completed.returncode, 0, completed.stderr)
        self.assertEqual(len(annotations), result["violationCount"])
        annotation_assert = bool(annotations) and all("file=" in line and ",line=" in line and ",col=" in line and "[fixture demonstration]" in line for line in annotations)
        self.assertTrue(annotation_assert)

    def test_missing_boundary_is_failure(self):
        self.assert_failure("missing", reason="missing_boundary")

    def test_zero_targets_is_failure(self):
        self.assert_failure("zero", reason="zero_targets")

    def test_source_count_upper_bound(self):
        self.assert_failure(extra=("--max-source-files", "1"), reason="input_limit")

    def test_source_bytes_upper_bound(self):
        self.assert_failure(extra=("--max-source-bytes", "1"), reason="input_limit")

    def test_missing_runtime_is_unavailable(self):
        self.assert_failure(environment={"DAWNHOLDER_DOTNET": str(WORK / "missing-dotnet")}, reason="tool_unavailable")

    def test_invalid_rules_fail_without_analysis(self):
        path = WORK / "invalid-rules.json"
        path.write_text('{"version": "1", "rules": []}\n')
        self.assert_failure(extra=("--rules", path), reason="invalid_rules")

    def test_missing_rules_fail_without_analysis(self):
        self.assert_failure(extra=("--rules", WORK / "absent.json"), reason="invalid_rules")

    def test_existing_output_is_preserved(self):
        run = WORK / uuid.uuid4().hex
        source = write_fixture(run / "source", "clean")
        output = run / "result"
        output.mkdir()
        marker = output / "result.json"
        marker.write_text("prior-owned-evidence\n")
        completed = subprocess.run(
            ["bash", str(ENTRY), "--source-root", str(source), "--output-root", str(output), "--input-kind", "fixture"],
            cwd=REPO, capture_output=True, text=True,
        )
        rejected_assert = completed.returncode != 0 and "output" in completed.stderr.lower()
        self.assertTrue(rejected_assert, completed.stderr)
        self.assertEqual(marker.read_text(), "prior-owned-evidence\n")

    def test_rule_symlink_is_rejected(self):
        alias = WORK / "rule-link.json"
        alias.symlink_to(RULES)
        self.assert_failure(extra=("--rules", alias), reason="unsafe_path")

    def test_project_root_escape_is_rejected(self):
        self.assert_failure(extra=("--project", "../outside.csproj"), reason="unsafe_path")

    def test_workspace_untracked_source_provenance_matches_snapshot(self):
        run = WORK / uuid.uuid4().hex
        fixture = write_git_fixture(run / "source", WORK)
        source = fixture["root"]
        revision = fixture["head"]
        untracked = fixture["untrackedPath"]
        results = {}
        index_path = source / ".git/index"
        index_before = index_path.read_bytes()
        for mode, extra in (("workspace", ()), ("git_blobs", ("--source-ref", revision))):
            output = run / mode
            command = [
                "bash", str(ENTRY), "--source-root", str(source),
                "--output-root", str(output), *extra,
            ]
            started = time.perf_counter()
            completed = subprocess.run(command, cwd=REPO, capture_output=True, text=True, timeout=660)
            (run / f"{mode}-command.json").write_text(json.dumps({
                "argv": command, "exitCode": completed.returncode,
                "elapsedSeconds": time.perf_counter() - started,
            }, indent=2) + "\n", encoding="utf-8")
            (run / f"{mode}-stdout.txt").write_text(completed.stdout, encoding="utf-8")
            (run / f"{mode}-stderr.txt").write_text(completed.stderr, encoding="utf-8")
            self.assertEqual(completed.returncode, 0, completed.stdout + completed.stderr)
            result = json.loads((output / "result.json").read_text(encoding="utf-8"))
            self.assertEqual(result["executionStatus"], "completed")
            self.assertEqual(result["sourceSha"], revision)
            self.assertEqual(result["checkoutSha"], revision)
            self.assertEqual(result["sourceMode"], mode)
            results[mode] = result
        workspace = results["workspace"]
        blobs = results["git_blobs"]
        self.assertEqual(workspace.get("workspaceUntrackedInputs"), [untracked])
        self.assertEqual(blobs.get("workspaceUntrackedInputs"), [])
        workspace_files = {item["path"]: item for item in workspace["input"]["files"]}
        blob_paths = {item["path"] for item in blobs["input"]["files"]}
        self.assertEqual(workspace_files[untracked]["sha256"], hashlib.sha256((source / untracked).read_bytes()).hexdigest())
        self.assertNotIn(untracked, blob_paths)
        self.assertIn("02_Server/GameServer/Maps/GameMap.cs", blob_paths)
        self.assertNotIn("notes.md", workspace_files)
        self.assertNotIn(".backups/ignored-evidence.txt", workspace_files)
        self.assertNotEqual(workspace["input"]["sha256"], blobs["input"]["sha256"])
        self.assertGreater(workspace["ruleCounts"]["MB001"], 0)
        self.assertEqual(blobs["ruleCounts"], {"MB001": 0, "MB002": 0, "MB003": 0})
        self.assertEqual(index_path.read_bytes(), index_before, "Public inspection must not modify the fixture index")

    def test_owned_process_timeout_is_recorded(self):
        module_path = REPO / "99_Tools/Architecture/Boundaries/processes.py"
        spec = importlib.util.spec_from_file_location("module_boundary_processes", module_path)
        module = importlib.util.module_from_spec(spec)
        spec.loader.exec_module(module)
        folder = WORK / "process-timeout"
        folder.mkdir()
        record = module.run_process(["python3", "-B", "-c", "import time; time.sleep(30)"], REPO, folder, os.environ.copy(), timeout=0.05)
        timeout_assert = record["exitCode"] != 0 and record["reasonCode"] == "timeout" and record["elapsedSeconds"] > 0
        self.assertTrue(timeout_assert)
        self.assertTrue((folder / "stdout.txt").is_file())


if __name__ == "__main__":
    unittest.main()
