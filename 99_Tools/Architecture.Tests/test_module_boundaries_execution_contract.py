"""Independent re-verification of the module boundary execution contract.

Requirement sources, never the checker's own calculations:
- goal 01_Phases/goals/2026-10-05-module-boundary-warning/goal.md, completion 2/5/6/7
  and the invariants (source SHA, input hash, tool, command, exit and time traceable).
- .backups/verification/2026-10-05-module-boundary-warning/correction-contract.md
  requirements 1-6 (#1 opt-out/opt-in, SDK absence still fails, #2 provenance) and
  reverification-contract.md judgments 2-6 and #ENV-1.

Opt-in: MODULE_BOUNDARIES_EXECUTION_WORK names a new absolute directory below the
repository .backups/. Without it every class skips at import with no process, network
or write, so default `test_*.py` discovery keeps its installation-independent contract.
MODULE_BOUNDARIES_EXECUTION_MAIN_SHA optionally names the resolved main commit to inspect.
"""

import fnmatch
import hashlib
import os
import pathlib
import re
import shutil
import sys
import time
import unittest

from support import module_boundary_execution_fixture as fixture


WORK_VALUE = os.environ.get(fixture.WORK_VARIABLE)
SKIP_REASON = (
    f"Set {fixture.WORK_VARIABLE} to a new owned absolute directory below .backups/ "
    "to run execution-contract checks (SDK10.0.301, Git, WSL interop)"
)
REQUIREMENT_MODULE = "test_module_boundaries"
DISCOVERY_MODULE = "test_module_boundaries_discovery"
# correction-contract requirement 2 and reverification judgment 3: the 28 earlier
# requirement methods plus the #2 provenance regression, and the 5 discovery regressions.
REQUIRED_COUNTS = {REQUIREMENT_MODULE: 29, DISCOVERY_MODULE: 5}
FROZEN_IMPACT_FILTER = "test_zero_violations_is_distinct_completed_clean"
ABSENT_SDK = "/nonexistent/dawnholder-execution-contract/dotnet"
RUN_SUMMARY = re.compile(r"^Ran (\d+) tests? in", re.MULTILINE)


def python_unittest(*arguments):
    return [sys.executable, "-B", "-m", "unittest", "discover", "-s", str(fixture.TESTS),
            "-t", str(fixture.TESTS), *arguments]


def tests_ran(run):
    found = RUN_SUMMARY.findall(run.stderr)
    return int(found[-1]) if found else None


@unittest.skipIf(WORK_VALUE is None, SKIP_REASON)
class DefaultDiscoveryWithoutOptIn(unittest.TestCase):
    """#1 opt-out: the module boundary suites under their own CI pattern, no opt-in set.

    The child records process, network and write audit events itself; a PATH sentinel
    and DAWNHOLDER_DOTNET also log any grandchild that would try a real tool.
    """

    @classmethod
    def setUpClass(cls):
        cls.folder = fixture.new_folder("default-discovery")
        sentinel = cls.folder / "sentinel-bin"
        sentinel.mkdir()
        cls.sentinel_log = cls.folder / "sentinel-calls.txt"
        for name in ("dotnet", "git", "bash", "curl", "wget", "git.exe", "pwsh.exe", "cmd.exe"):
            program = sentinel / name
            program.write_text(f'#!/bin/sh\nprintf \'%s %s\\n\' "$0" "$*" >> "{cls.sentinel_log}"\nexit 97\n',
                               encoding="utf-8")
            program.chmod(0o755)
        harness = cls.folder / "discovery_harness.py"
        harness.write_text(fixture.DISCOVERY_HARNESS, encoding="utf-8")
        # The earlier default output root, and the tool/test trees a discovery could write into.
        watched = (fixture.REPO / ".backups/architecture", fixture.REPO / "99_Tools/Architecture", fixture.TESTS)
        before = [(path.exists(), fixture.tree_state(path)) for path in watched]
        cls.summary_path = cls.folder / "summary.json"
        cls.discovery = fixture.run_recorded(
            cls.folder,
            [sys.executable, "-B", harness, cls.summary_path, fixture.TESTS, "test_module_boundaries*.py"],
            set_environment={"PATH": f"{sentinel}{os.pathsep}{os.environ['PATH']}",
                             "DAWNHOLDER_DOTNET": str(sentinel / "dotnet")},
            timeout=300,
        )
        cls.unchanged = [before[index] == (path.exists(), fixture.tree_state(path)) for index, path in enumerate(watched)]
        cls.summary = fixture.read_json(cls.summary_path)
        cls.skips = dict(cls.summary["skipped"])

    def module_ids(self, module):
        return [name for name in self.summary["collected"] if name.startswith(module + ".")]

    def test_child_discovery_completes_without_failures(self):
        exit_assert = self.discovery.returncode == 0
        clean_assert = not self.summary["failures"] and not self.summary["errors"]
        self.assertTrue(exit_assert, self.discovery.output[-3000:])
        self.assertTrue(clean_assert, self.summary["failures"] + self.summary["errors"])

    def test_requirement_suite_is_collected_and_every_test_skips_with_reason(self):
        collected = self.module_ids(REQUIREMENT_MODULE)
        count_assert = len(collected) == REQUIRED_COUNTS[REQUIREMENT_MODULE]
        reasons = [self.skips.get(name, "") for name in collected]
        reason_assert = all("MODULE_BOUNDARIES_TEST_WORK" in reason and "SDK" in reason for reason in reasons)
        self.assertTrue(count_assert, collected)
        self.assertTrue(reason_assert, reasons)

    def test_discovery_regressions_really_run(self):
        collected = self.module_ids(DISCOVERY_MODULE)
        executed = [name for name in self.summary["started"] if name in collected and name not in self.skips]
        executed_assert = len(collected) == len(executed) == REQUIRED_COUNTS[DISCOVERY_MODULE]
        self.assertTrue(executed_assert, (collected, executed))

    def test_other_opt_in_suites_including_this_one_skip(self):
        for module, variable in (("test_module_boundaries_independent", "MODULE_BOUNDARIES_INDEPENDENT"),
                                 ("test_module_boundaries_execution_contract", fixture.WORK_VARIABLE)):
            with self.subTest(module=module):
                collected = self.module_ids(module)
                skipped_assert = bool(collected) and all(variable in self.skips.get(name, "") for name in collected)
                self.assertTrue(skipped_assert, collected)

    def test_no_process_network_or_write_happens(self):
        temporary = str(self.folder / "tmp")
        foreign = [event for event in self.summary["events"]
                   if not (event["event"] == "open-write"
                           and (event["path"] == os.devnull or event["path"].startswith(temporary + os.sep)))]
        no_event_assert = foreign == []
        sentinel_assert = not self.sentinel_log.exists()
        self.assertTrue(no_event_assert, foreign)
        self.assertTrue(sentinel_assert, self.sentinel_log.read_text(encoding="utf-8") if not sentinel_assert else "")

    def test_default_repository_outputs_and_tools_are_unchanged(self):
        unchanged_assert = self.unchanged == [True, True, True]
        self.assertTrue(unchanged_assert, "a default output, tool or test tree changed during opt-out discovery")

    def test_documented_default_pattern_still_collects_the_boundary_modules(self):
        names = sorted(path.name for path in fixture.TESTS.iterdir() if path.is_file())
        boundary = [name for name in names if fnmatch.fnmatch(name, "test_module_boundaries*.py")]
        default_assert = all(fnmatch.fnmatch(name, "test_*.py") for name in boundary) and len(boundary) == 4
        wrapper = (fixture.TESTS / "run-tests.sh").read_text(encoding="utf-8")
        readme = (fixture.REPO / "99_Tools/Architecture/README.md").read_text(encoding="utf-8")
        wrapper_assert = "--pattern 'test_*.py'" in wrapper and "no .NET" in wrapper
        readme_assert = "-p 'test_*.py'" in readme and "MODULE_BOUNDARIES_TEST_WORK" in readme
        self.assertTrue(default_assert, boundary)
        self.assertTrue(wrapper_assert)
        self.assertTrue(readme_assert)


@unittest.skipIf(WORK_VALUE is None, SKIP_REASON)
class ExplicitRequirementOptIn(unittest.TestCase):
    """#1 opt-in: malformed roots fail before writes, SDK absence fails, CI guard is real."""

    def requirement_child(self, folder, work_value, cwd, environment=None):
        settings = {"MODULE_BOUNDARIES_TEST_WORK": work_value}
        settings.update(environment or {})
        return fixture.run_recorded(
            folder, python_unittest("-p", "test_module_boundaries.py", "-k", "test_project_root_escape_is_rejected", "-v"),
            cwd=cwd, set_environment=settings, timeout=300,
        )

    def test_malformed_explicit_work_fails_before_any_write(self):
        sandbox = fixture.new_folder("malformed-work")
        (sandbox / "existing").mkdir()
        (sandbox / "link-target").mkdir()
        try:
            (sandbox / "link").symlink_to(sandbox / "link-target", target_is_directory=True)
            symlink_case = (str(sandbox / "link/new-root"), "symlink", sandbox / "link-target/new-root")
        except OSError as error:
            symlink_case = None
            fixture.write_json(sandbox / "symlink-unavailable.json", {"error": str(error)})
        cases = [
            ("", "nonempty absolute", None),
            ("relative-owned-root", "nonempty absolute", sandbox / "relative-owned-root"),
            (str(sandbox / "dots/../through-parent"), "traverse parents", sandbox / "through-parent"),
            (str(sandbox / "existing"), "new owned directory", None),
            ("/nonexistent-dawnholder-outside/new-root", ".backups or RUNNER_TEMP", None),
        ]
        if symlink_case:
            cases.append(symlink_case)
        for index, (value, message, must_not_exist) in enumerate(cases):
            with self.subTest(value=value):
                before = fixture.tree_state(sandbox)
                run = self.requirement_child(fixture.new_folder(f"malformed-{index}"), value, sandbox)
                error_assert = run.returncode != 0 and "ERROR: setUpClass" in run.stderr and message in run.stderr
                no_test_assert = tests_ran(run) == 0 and "skipped" not in run.stderr.splitlines()[-1]
                no_write_assert = fixture.tree_state(sandbox) == before and (
                    must_not_exist is None or not os.path.lexists(must_not_exist))
                self.assertTrue(error_assert, run.stderr[-2000:])
                self.assertTrue(no_test_assert, run.stderr[-500:])
                self.assertTrue(no_write_assert, "a malformed WORK value created files")
        self.assertIsNotNone(symlink_case, "symlink case could not be created in the owned work")

    def test_sdk_absent_explicit_run_still_fails(self):
        folder = fixture.new_folder("sdk-absent")
        run = fixture.run_recorded(
            folder, python_unittest("-p", "test_module_boundaries.py", "-k", FROZEN_IMPACT_FILTER, "-v"),
            set_environment={"MODULE_BOUNDARIES_TEST_WORK": str(folder / "work"), "DAWNHOLDER_DOTNET": ABSENT_SDK},
            timeout=600,
        )
        failed_assert = run.returncode == 1 and f"FAIL: {FROZEN_IMPACT_FILTER}" in run.stderr
        reason_assert = "tool_unavailable" in run.output
        not_skipped_assert = tests_ran(run) == 1 and "skipped" not in run.stderr.splitlines()[-1]
        self.assertTrue(failed_assert, run.stderr[-3000:])
        self.assertTrue(reason_assert)
        self.assertTrue(not_skipped_assert, run.stderr[-500:])

    def workflow_child(self, label, with_work):
        folder = fixture.new_folder(label)
        evidence = folder / "runner-temp/module-boundaries-evidence"
        (evidence / "tests").mkdir(parents=True)
        settings = {"RUNNER_TEMP": str(folder / "runner-temp")}
        if with_work:
            settings["MODULE_BOUNDARIES_TEST_WORK"] = str(evidence / "tests/work")
        script = fixture.workflow_requirement_script()
        (folder / "workflow-inline.py").write_text(script, encoding="utf-8")
        counts_path = evidence / "tests/counts.json"
        run = fixture.run_recorded(folder, [sys.executable, "-B", "-", counts_path], set_environment=settings,
                                   timeout=1500, stdin_text=script)
        return run, fixture.read_json(counts_path)

    def test_workflow_guard_fails_when_required_suites_only_skip(self):
        run, counts = self.workflow_child("workflow-unset", with_work=False)
        guard_assert = run.returncode == 1 and counts["requiredSuitesExecuted"] is False
        requirement = counts["requiredSuites"][REQUIREMENT_MODULE]
        skipped_assert = requirement == {"collected": REQUIRED_COUNTS[REQUIREMENT_MODULE], "executed": 0}
        self.assertTrue(guard_assert, run.output[-2000:])
        self.assertTrue(skipped_assert, requirement)
        self.assertIn("absent or skipped", run.stderr)

    def test_workflow_runs_every_required_suite_with_explicit_work(self):
        run, counts = self.workflow_child("workflow-opt-in", with_work=True)
        expected = {module: {"collected": count, "executed": count} for module, count in REQUIRED_COUNTS.items()}
        exit_assert = run.returncode == 0 and counts["failures"] == 0 and counts["errors"] == 0
        suites_assert = counts["requiredSuites"] == expected and counts["requiredSuitesExecuted"] is True
        self.assertTrue(exit_assert, run.output[-4000:])
        self.assertTrue(suites_assert, counts)


@unittest.skipIf(WORK_VALUE is None, SKIP_REASON)
class UntrackedSnapshotProvenance(unittest.TestCase):
    """#2 through the public Bash entry on an owned Git fixture, workspace and source-ref."""

    @classmethod
    def setUpClass(cls):
        cls.folder = fixture.new_folder("provenance")
        cls.source = cls.folder / "source"
        cls.head = fixture.write_git_fixture(cls.source)
        cls.git_before = fixture.git_state(cls.source)
        cls.results = {}
        cls.runs = {}
        for mode, extra in (("workspace", ()), ("git_blobs", ("--source-ref", cls.head))):
            output = cls.folder / f"{mode}-result"
            run = fixture.run_recorded(
                fixture.new_folder(f"provenance-{mode}"),
                ["bash", fixture.ENTRY, "--source-root", cls.source, "--output-root", output, *extra],
                timeout=900,
            )
            cls.runs[mode] = run
            result_path = output / "result.json"
            cls.results[mode] = fixture.read_json(result_path) if result_path.is_file() else None
        cls.git_after = fixture.git_state(cls.source)

    def setUp(self):
        for mode, run in self.runs.items():
            self.assertEqual(run.returncode, 0, f"{mode}: {run.output[-3000:]}")
            self.assertEqual(self.results[mode]["executionStatus"], "completed", mode)

    def input_paths(self, mode):
        return {item["path"] for item in self.results[mode]["input"]["files"]}

    def test_workspace_lists_exactly_the_untracked_snapshot_inputs(self):
        expected = sorted([fixture.UNTRACKED_BOUNDARY, fixture.UNTRACKED_SPACED, fixture.UNTRACKED_OUTSIDE_BOUNDARY,
                           fixture.UNTRACKED_BUILD_INPUT, fixture.IGNORED_BOUNDARY])
        listed_assert = self.results["workspace"].get("workspaceUntrackedInputs") == expected
        snapshot_assert = set(expected) <= self.input_paths("workspace")
        self.assertTrue(listed_assert, self.results["workspace"].get("workspaceUntrackedInputs"))
        self.assertTrue(snapshot_assert)

    def test_tracked_staged_unrelated_and_ignored_non_inputs_are_not_listed(self):
        listed = set(self.results["workspace"]["workspaceUntrackedInputs"])
        for path in (fixture.TRACKED_BRACKET, fixture.STAGED_SESSION, fixture.MODIFIED_TRACKED,
                     *fixture.UNRELATED_UNTRACKED, fixture.IGNORED_EVIDENCE):
            with self.subTest(path=path):
                self.assertNotIn(path, listed)
        for path in (*fixture.UNRELATED_UNTRACKED, fixture.IGNORED_EVIDENCE):
            with self.subTest(snapshot=path):
                self.assertNotIn(path, self.input_paths("workspace"))

    def test_staged_and_modified_inputs_stay_visible_in_workspace_status(self):
        status = self.results["workspace"]["workspaceStatus"].splitlines()
        staged_assert = f"A  {fixture.STAGED_SESSION}" in status
        modified_assert = f" M {fixture.MODIFIED_TRACKED}" in status
        no_untracked_status_assert = not any(line.startswith("??") for line in status)
        self.assertTrue(staged_assert, status)
        self.assertTrue(modified_assert, status)
        self.assertTrue(no_untracked_status_assert, status)

    def test_git_blob_mode_uses_only_committed_blobs_and_lists_nothing(self):
        blobs = self.results["git_blobs"]
        empty_assert = blobs.get("workspaceUntrackedInputs") == []
        excluded = {fixture.UNTRACKED_BOUNDARY, fixture.UNTRACKED_SPACED, fixture.UNTRACKED_OUTSIDE_BOUNDARY,
                    fixture.UNTRACKED_BUILD_INPUT, fixture.IGNORED_BOUNDARY, fixture.STAGED_SESSION}
        absent_assert = not (excluded & self.input_paths("git_blobs"))
        tracked_assert = fixture.TRACKED_BRACKET in self.input_paths("git_blobs")
        self.assertTrue(empty_assert, blobs.get("workspaceUntrackedInputs"))
        self.assertTrue(absent_assert, sorted(excluded & self.input_paths("git_blobs")))
        self.assertTrue(tracked_assert)

    def test_input_hashes_are_the_actual_workspace_and_blob_bytes(self):
        prefix = ["git", "-C", str(self.source)]
        workspace = {item["path"]: item for item in self.results["workspace"]["input"]["files"]}
        for path, item in workspace.items():
            with self.subTest(workspace=path):
                self.assertEqual(item["sha256"], hashlib.sha256((self.source / path).read_bytes()).hexdigest())
        blobs = {item["path"]: item for item in self.results["git_blobs"]["input"]["files"]}
        originals = fixture.blob_bytes(prefix, self.head, sorted(blobs))
        for path, item in blobs.items():
            with self.subTest(blob=path):
                self.assertEqual(item["sha256"], hashlib.sha256(originals[path]).hexdigest())
        differ_assert = workspace[fixture.MODIFIED_TRACKED]["sha256"] != blobs[fixture.MODIFIED_TRACKED]["sha256"]
        self.assertTrue(differ_assert, "the workspace edit must reach only the workspace input")
        self.assertNotEqual(self.results["workspace"]["input"]["sha256"], self.results["git_blobs"]["input"]["sha256"])

    def test_untracked_boundary_source_is_analyzed_only_in_workspace(self):
        def sources_with(mode, rule):
            return {item["source"]["path"] for item in self.results[mode]["violations"] if item["ruleId"] == rule}

        workspace_assert = fixture.UNTRACKED_BOUNDARY in sources_with("workspace", "MB001")
        blob_assert = fixture.UNTRACKED_BOUNDARY not in sources_with("git_blobs", "MB001")
        self.assertTrue(workspace_assert, self.results["workspace"]["violations"])
        self.assertTrue(blob_assert)

    def test_source_sha_is_the_fixture_head_and_git_state_is_unchanged(self):
        for mode in self.results:
            with self.subTest(mode=mode):
                result = self.results[mode]
                self.assertEqual((result["sourceSha"], result["checkoutSha"], result["sourceMode"]),
                                 (self.head, self.head, mode))
        unchanged_assert = self.git_before == self.git_after
        self.assertTrue(unchanged_assert, "public inspection changed the fixture index, HEAD or refs")


@unittest.skipIf(WORK_VALUE is None, SKIP_REASON)
class CurrentSourceEntry(unittest.TestCase):
    """Completion 2/6: the real checkout through the public entry, workspace and blobs."""

    @classmethod
    def setUpClass(cls):
        cls.git = fixture.operating_git_prefix()
        cls.head = fixture.read_git(cls.git, "rev-parse", "HEAD").strip()
        cls.main = os.environ.get(fixture.MAIN_SHA_VARIABLE)
        cls.metadata = [pathlib.Path(fixture.read_git(cls.git, "rev-parse", "--git-path", name).strip())
                        for name in ("index", "HEAD")]
        cls.metadata_before = [hashlib.sha256(path.read_bytes()).hexdigest() for path in cls.metadata]
        requests = [("workspace", ()), ("head-blobs", ("--source-ref", cls.head))]
        if cls.main:
            requests.append(("main-blobs", ("--source-ref", cls.main)))
        cls.runs, cls.results = {}, {}
        for label, extra in requests:
            folder = fixture.new_folder(f"current-{label}")
            run = fixture.run_recorded(folder, ["bash", fixture.ENTRY, "--output-root", folder / "result", *extra],
                                       timeout=900)
            cls.runs[label] = run
            path = folder / "result/result.json"
            cls.results[label] = fixture.read_json(path) if path.is_file() else None

    def setUp(self):
        for label, run in self.runs.items():
            self.assertEqual(run.returncode, 0, f"{label}: {run.output[-3000:]}")
            self.assertEqual(self.results[label]["executionStatus"], "completed", label)

    def test_identity_mode_and_untracked_list_follow_the_checkout(self):
        head_tree = set(fixture.read_git(self.git, "ls-tree", "-r", "--name-only", "HEAD").splitlines())
        for label, result in self.results.items():
            with self.subTest(label=label):
                source = self.head if label != "main-blobs" else self.main
                mode = "workspace" if label == "workspace" else "git_blobs"
                self.assertEqual((result["checkoutSha"], result["sourceSha"], result["sourceMode"]),
                                 (self.head, source, mode))
                self.assertIsNone(result["prHeadSha"])
        workspace = self.results["workspace"]
        not_in_head = sorted(item["path"] for item in workspace["input"]["files"] if item["path"] not in head_tree)
        self.assertEqual(workspace["workspaceUntrackedInputs"], not_in_head)

    def test_input_hashes_are_disk_or_blob_bytes(self):
        for item in self.results["workspace"]["input"]["files"]:
            with self.subTest(workspace=item["path"]):
                data = (fixture.REPO / item["path"]).read_bytes()
                self.assertEqual((item["sha256"], item["bytes"]), (hashlib.sha256(data).hexdigest(), len(data)))
        for label in ("head-blobs", "main-blobs"):
            if label not in self.results:
                continue
            revision = self.head if label == "head-blobs" else self.main
            files = self.results[label]["input"]["files"]
            originals = fixture.blob_bytes(self.git, revision, [item["path"] for item in files])
            for item in files:
                with self.subTest(label=label, path=item["path"]):
                    self.assertEqual(item["sha256"], hashlib.sha256(originals[item["path"]]).hexdigest())

    def test_coverage_covers_every_boundary_source_in_the_tree(self):
        areas = ("02_Server/GameServer/Handlers/", "02_Server/GameServer/Sessions/", "02_Server/GameServer/Maps/")
        for label, result in self.results.items():
            with self.subTest(label=label):
                revision = self.main if label == "main-blobs" else self.head
                tree = fixture.read_git(self.git, "ls-tree", "-r", "--name-only", revision).splitlines()
                boundary = [path for path in tree if path.startswith(areas) and path.endswith(".cs")
                            and not path.endswith((".g.cs", ".generated.cs"))
                            and not {"bin", "obj"} & set(pathlib.PurePosixPath(path).parts)]
                coverage = result["coverage"]
                self.assertGreater(len(boundary), 0)
                self.assertEqual((coverage["expectedBoundaryFiles"], coverage["analyzedBoundaryFiles"]),
                                 (len(boundary), len(boundary)))
                self.assertEqual(coverage["compiledInputFiles"], coverage["inputSourceFiles"])

    def test_tool_policy_limits_and_time_are_traceable(self):
        boundaries = fixture.REPO / "99_Tools/Architecture/Boundaries"
        # The checker itself plus its two public entry files must be traceable; extra build
        # inputs may be recorded too, but every recorded hash must be today's bytes.
        required = {f"99_Tools/Architecture/Boundaries/{path.name}" for path in boundaries.iterdir() if path.is_file()}
        required |= {"99_Tools/Architecture/check-module-boundaries.sh", "99_Tools/Architecture/check-module-boundaries.py"}
        policy_hash = hashlib.sha256((boundaries / "module-boundaries.json").read_bytes()).hexdigest()
        for label, result in self.results.items():
            with self.subTest(label=label):
                recorded = {item["path"]: item["sha256"] for item in result["tool"]["files"]}
                self.assertLessEqual(required, set(recorded), sorted(required - set(recorded)))
                current = {path: hashlib.sha256((fixture.REPO / path).read_bytes()).hexdigest() for path in recorded}
                self.assertEqual(recorded, current)
                self.assertEqual(result["tool"]["sdkVersion"], "10.0.301")
                self.assertEqual(result["policy"]["sha256"], policy_hash)
                self.assertEqual(result["limits"], {"sourceFiles": 5000, "sourceBytes": 64 * 1024 * 1024,
                                                    "stageTimeoutSeconds": 600})
                self.assertTrue(0 < result["elapsedSeconds"] <= self.runs[label].elapsed)
                self.assertEqual(result["violationCount"], len(result["violations"]))
                self.assertEqual(result["violationCount"], sum(result["ruleCounts"].values()))

    def test_unchanged_server_blobs_give_the_same_input_hash(self):
        if "main-blobs" not in self.results:
            self.skipTest(f"{fixture.MAIN_SHA_VARIABLE} not set")
        paths = [item["path"] for item in self.results["head-blobs"]["input"]["files"]]
        changed = fixture.read_git(self.git, "diff", "--name-only", self.main, self.head, "--", *paths).split()
        if changed:
            self.skipTest(f"inputs differ between main and HEAD: {changed}")
        self.assertEqual(self.results["main-blobs"]["input"]["sha256"], self.results["head-blobs"]["input"]["sha256"])

    def test_operating_index_and_head_bytes_are_unchanged(self):
        after = [hashlib.sha256(path.read_bytes()).hexdigest() for path in self.metadata]
        self.assertEqual(self.metadata_before, after, [str(path) for path in self.metadata])


# test_extractor_selection methods that failed with "git.exe: Invalid argument" (#ENV-1).
ENV1_METHODS = (
    "test_extractor_selection.WindowsWorktreeEntryTests.test_windows_worktree_keeps_a_full_size_capture_in_the_batch_provenance",
    "test_extractor_selection.WindowsWorktreeEntryTests.test_windows_worktree_keeps_the_stdin_capture_through_prepare_measure_check",
    "test_extractor_selection.PowerShellEvidenceRootTests.test_a_new_root_inside_or_around_an_owned_root_is_refused_before_git_and_wsl",
    "test_extractor_selection.PowerShellEvidenceRootTests.test_an_owned_root_and_new_sibling_roots_reach_git_and_wsl",
)
# Windows refuses a process current directory longer than this (MAX_PATH less "\\" and NUL).
WINDOWS_CWD_LIMIT = 258
# Pipeline/execution_status.runtime_path below the stand-in HOME, which tempfile puts in
# TMPDIR: architecture-measure-<8>/home/.cache/dawnholder/architecture/<20>-<selection>-<12>.
RUNTIME_SUFFIX_LENGTH = (
    len("\\architecture-measure-12345678\\home\\.cache\\dawnholder\\architecture\\") + 20 + len("-compare-") + 12
)
INTEROP_TOOLS = ("git.exe", "wslpath", "cmd.exe", "pwsh.exe")


@unittest.skipIf(WORK_VALUE is None, SKIP_REASON)
class WindowsWorktreeEnvironmentCases(unittest.TestCase):
    """#ENV-1: the four frozen Windows cases, unchanged, at the current HEAD.

    `measure` changes into the stand-in runtime below TMPDIR before Pipeline queries
    git.exe. The owned TMPDIR depth therefore decides whether that Windows cwd fits.
    Windows TEMP for the frozen fixtures is passed process-locally through WSLENV.
    """

    @classmethod
    def setUpClass(cls):
        if not all(shutil.which(name) for name in INTEROP_TOOLS):
            raise unittest.SkipTest("needs WSL interop with git.exe, wslpath, cmd.exe and pwsh.exe")

    def protected_run(self, label, names, windows_name, temporary_name=None):
        """Run named existing tests; TMPDIR is the owned root itself unless a name is given.

        Windows TEMP gets a short owned folder: the frozen fixture's Windows Git writes
        `.git/objects/xx/<38 hex>` below it, and Git for Windows refuses paths past 260.
        """
        folder = fixture.new_folder(label)
        temporary = fixture.owned_work()
        if temporary_name:
            temporary = temporary / temporary_name
            temporary.mkdir()
        windows_temporary = fixture.owned_work() / windows_name
        windows_temporary.mkdir()
        harness = folder / "protected_harness.py"
        harness.write_text(fixture.PROTECTED_HARNESS, encoding="utf-8")
        predicted = len(fixture.windows_path(temporary)) + RUNTIME_SUFFIX_LENGTH
        summary_path = folder / "summary.json"
        started_ns = time.time_ns()
        run = fixture.run_recorded(
            folder, [sys.executable, "-B", harness, summary_path, fixture.owned_work(), fixture.TESTS, fixture.REPO, *names],
            set_environment={"TMPDIR": str(temporary), "TEMP": str(windows_temporary), "TMP": str(windows_temporary),
                             "WSLENV": "TEMP/p:TMP/p", "GIT_OPTIONAL_LOCKS": "0"},
            timeout=1800,
        )
        cache_changes = fixture.changed_since(pathlib.Path.home() / ".cache/dawnholder", started_ns)
        fixture.write_json(folder / "cwd-budget.json", {
            "tmpdir": str(temporary), "predictedRuntimeCwdLength": predicted,
            "windowsCwdLimit": WINDOWS_CWD_LIMIT, "homeCacheChanges": cache_changes,
        })
        self.assertEqual(cache_changes, [], "the existing HOME cache changed during the run")
        return predicted, run, fixture.read_json(summary_path)

    def test_git_exe_starts_only_within_the_windows_cwd_limit(self):
        folder = fixture.new_folder("cwd-limit")
        records = []
        for length in (WINDOWS_CWD_LIMIT, WINDOWS_CWD_LIMIT + 1):
            base = folder / f"l{length}"
            base.mkdir()
            child = base / ("c" * (length - len(fixture.windows_path(base)) - 1))
            child.mkdir()
            run = fixture.run_recorded(fixture.new_folder(f"git-version-{length}"), ["git.exe", "--version"], cwd=child,
                                       timeout=60)
            records.append((len(fixture.windows_path(child)), run.returncode, run.stderr.strip()))
        self.assertEqual([record[0] for record in records], [WINDOWS_CWD_LIMIT, WINDOWS_CWD_LIMIT + 1])
        self.assertEqual(records[0][1], 0, records)
        self.assertNotEqual(records[1][1], 0, records)
        self.assertIn("Invalid argument", records[1][2])

    def test_the_four_cases_pass_when_the_runtime_cwd_fits(self):
        predicted, run, summary = self.protected_run("env1-short", ENV1_METHODS, "w1")
        self.assertLessEqual(predicted, WINDOWS_CWD_LIMIT, "owned TMPDIR too deep for this check")
        passed_assert = (run.returncode == 0 and summary["testsRun"] == len(ENV1_METHODS)
                         and not summary["skipped"] and not summary["failures"] and not summary["errors"])
        self.assertTrue(passed_assert, summary["failures"] + summary["errors"] + summary["skipped"])
        self.assertEqual(summary["blockedWrites"], [])

    def test_the_same_case_fails_only_when_tmpdir_pushes_the_cwd_past_the_limit(self):
        base = len(fixture.windows_path(fixture.owned_work()))
        name = "deep-" + "d" * (WINDOWS_CWD_LIMIT + 20 - base - 1 - RUNTIME_SUFFIX_LENGTH - len("deep-"))
        predicted, run, summary = self.protected_run("env1-deep", ENV1_METHODS[1:2], "w2", name)
        self.assertGreater(predicted, WINDOWS_CWD_LIMIT)
        failed_assert = run.returncode == 1 and len(summary["failures"]) + len(summary["errors"]) == 1
        self.assertTrue(failed_assert, summary)
        self.assertIn("git.exe: Invalid argument", (summary["failures"] + summary["errors"])[0][1])
        self.assertEqual(summary["blockedWrites"], [])


if __name__ == "__main__":
    unittest.main()
