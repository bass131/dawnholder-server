"""Independent verifier tests for the module boundary warning pilot.

Expectations come from the goal (01_Phases/goals/2026-10-05-module-boundary-warning)
and the implementation contract requirements 2/4/5: the three physical folders,
MB001/MB002/MB003, the exact HandlerRegistry/IPacketHandler exception for
Sessions only, warning vs failure states, and owned process settlement. Every
run goes through the public Bash entry and an explicit owned evidence root
(MODULE_BOUNDARIES_INDEPENDENT_WORK); without it the suite skips instead of
writing to a default repository path.
"""

import json
import os
import pathlib
import unittest
import uuid

from support import module_boundary_independent_fixture as fixture


WORK = fixture.work_root()
SKIP_REASON = f"Set {fixture.WORK_VARIABLE} to a new owned evidence directory to run public-entry checks"
REAL_SOURCE = os.environ.get("MODULE_BOUNDARIES_INDEPENDENT_REAL") == "1"


def by_file(items):
    grouped = {}
    for item in items:
        grouped.setdefault(pathlib.PurePosixPath(item["source"]["path"]).name, []).append(item)
    return grouped


@unittest.skipIf(WORK is None, SKIP_REASON)
class IndependentPolicyMatrix(unittest.TestCase):
    """One matrix run; each probe file isolates one requirement case."""

    @classmethod
    def setUpClass(cls):
        cls.run_result = fixture.invoke_fixture(WORK, "matrix", fixture.matrix_sources())
        cls.result = cls.run_result.result

    def setUp(self):
        self.assertEqual(self.run_result.returncode, 0, self.run_result.stderr)
        self.assertIsNotNone(self.result, "public entry must leave its own result.json")

    def test_policy_warnings_complete_with_exit_zero(self):
        completed_assert = self.result["executionStatus"] == "completed"
        warning_state_assert = self.result["analysisStatus"] == "warnings"
        count_assert = self.result["violationCount"] == len(self.result["violations"]) > 0
        self.assertTrue(completed_assert, self.result["reasonCode"])
        self.assertTrue(warning_state_assert)
        self.assertTrue(count_assert)

    def test_each_forbidden_form_warns_with_only_its_rule(self):
        violations = by_file(self.result["violations"])
        for relative, (rule, _) in fixture.WARNING_PROBES.items():
            name = pathlib.PurePosixPath(relative).name
            with self.subTest(probe=relative):
                rules_seen = {item["ruleId"] for item in violations.get(name, [])}
                warned_assert = rule in rules_seen
                only_rule_assert = rules_seen == {rule}
                self.assertTrue(warned_assert, f"{relative} must produce {rule}; saw {sorted(rules_seen)}")
                self.assertTrue(only_rule_assert, f"{relative} must produce only {rule}; saw {sorted(rules_seen)}")

    def test_allowed_same_area_excluded_and_text_cases_do_not_warn(self):
        violations = by_file(self.result["violations"])
        for relative in fixture.CLEAN_PROBES:
            name = pathlib.PurePosixPath(relative).name
            with self.subTest(probe=relative):
                silent_assert = name not in violations
                self.assertTrue(silent_assert, [item["target"]["symbol"] for item in violations.get(name, [])])

    def test_no_warning_outside_the_expected_probe_files(self):
        expected_names = {pathlib.PurePosixPath(path).name for path in fixture.WARNING_PROBES}
        unexpected = set(by_file(self.result["violations"])) - expected_names
        unexpected_assert = not unexpected
        self.assertTrue(unexpected_assert, sorted(unexpected))

    def test_allowed_directions_are_observed_not_missed(self):
        references = by_file(self.result["references"])
        observed_cases = {
            "SubmitToSession.cs": ("Handlers", "Sessions", None),
            "SendViaSession.cs": ("Maps", "Sessions", None),
            "DriveMap.cs": ("Sessions", "Maps", None),
            "DispatchExact.cs": ("Sessions", "Handlers", f"{fixture.HANDLERS}.HandlerRegistry"),
        }
        for name, (source_area, target_area, target_type) in observed_cases.items():
            with self.subTest(probe=name):
                matching = [
                    item for item in references.get(name, [])
                    if item["sourceArea"] == source_area and item["target"]["area"] == target_area
                    and (target_type is None or item["target"]["type"] == target_type)
                ]
                observed_assert = bool(matching)
                self.assertTrue(observed_assert, f"{name}: allowed {source_area}->{target_area} must be analysed")

    def test_exact_interface_member_is_observed_and_allowed(self):
        references = by_file(self.result["references"]).get("DispatchExact.cs", [])
        interface_member_assert = any(
            item["target"]["type"] == f"{fixture.HANDLERS}.IPacketHandler" and "Process" in item["target"]["symbol"]
            for item in references
        )
        self.assertTrue(interface_member_assert)

    def test_excluded_targets_are_not_listed_as_boundary_references(self):
        references = by_file(self.result["references"])
        for name in ("OtherArea.cs", "ExternalTypes.cs", "NamespaceOutsideFolder.cs", "TextOnly.cs"):
            with self.subTest(probe=name):
                excluded_assert = name not in references
                self.assertTrue(excluded_assert, [item["target"]["symbol"] for item in references.get(name, [])])

    def test_generated_boundary_file_is_not_a_target(self):
        coverage = self.result["coverage"]
        generated = f"{fixture.SERVER}/Handlers/GeneratedProbe.g.cs"
        not_analyzed_assert = generated not in coverage["analyzedFiles"]
        hand_written = f"{fixture.SERVER}/Handlers/MemberCall.cs"
        analyzed_assert = hand_written in coverage["analyzedFiles"]
        self.assertTrue(not_analyzed_assert)
        self.assertTrue(analyzed_assert)

    def test_member_call_location_and_target_declaration(self):
        text = fixture.WARNING_PROBES["Handlers/MemberCall.cs"][1]
        line, column = fixture.position(text, "Advance")
        calls = [
            item for item in by_file(self.result["violations"]).get("MemberCall.cs", [])
            if "Advance" in item["target"]["symbol"]
        ]
        located_assert = any(item["source"]["line"] == line and item["source"]["column"] == column for item in calls)
        declaration_assert = all(
            item["target"]["declaration"]["path"] == f"{fixture.SERVER}/Maps/WorldMap.cs" for item in calls
        )
        self.assertTrue(located_assert, [(item["source"]["line"], item["source"]["column"]) for item in calls])
        self.assertTrue(declaration_assert)

    def test_member_targets_name_the_member(self):
        expectations = {"ExtensionCall.cs": "ToMapKey", "EventSubscribe.cs": "Changed", "StaticMember.cs": "MaxPlayers"}
        violations = by_file(self.result["violations"])
        for name, member in expectations.items():
            with self.subTest(probe=name):
                member_assert = any(member in item["target"]["symbol"] for item in violations.get(name, []))
                self.assertTrue(member_assert, [item["target"]["symbol"] for item in violations.get(name, [])])

    def test_every_warning_carries_rule_location_and_repair(self):
        for item in self.result["violations"]:
            with self.subTest(source=item["source"]["path"], line=item["source"]["line"]):
                warning_assert = item["severity"] == "warning"
                location_assert = item["source"]["line"] > 0 and item["source"]["column"] > 0
                repair_assert = bool(item["repair"].strip())
                self.assertTrue(warning_assert)
                self.assertTrue(location_assert)
                self.assertTrue(repair_assert)


@unittest.skipIf(WORK is None, SKIP_REASON)
class IndependentCleanAndFailureStates(unittest.TestCase):
    """Normal zero-violation completion versus input/tool/analysis failures."""

    def assert_not_completed(self, run, reason=None):
        self.assertIsNotNone(run.result, run.stderr)
        nonzero_assert = run.returncode != 0
        state_assert = run.result["executionStatus"] != "completed" and run.result["analysisStatus"] == "not_completed"
        no_zero_claim_assert = run.result["violationCount"] is None
        self.assertTrue(nonzero_assert, run.stdout)
        self.assertTrue(state_assert, run.result["executionStatus"])
        self.assertTrue(no_zero_claim_assert)
        if reason:
            self.assertEqual(run.result["reasonCode"], reason, run.result["message"])
        return run.result

    def test_clean_sources_complete_as_clean(self):
        run = fixture.invoke_fixture(WORK, "clean", fixture.clean_sources())
        self.assertIsNotNone(run.result, run.stderr)
        exit_assert = run.returncode == 0
        clean_assert = run.result["executionStatus"] == "completed" and run.result["analysisStatus"] == "clean"
        zero_assert = run.result["violationCount"] == 0 and run.result["violations"] == []
        coverage = run.result["coverage"]
        coverage_assert = coverage["analyzedBoundaryFiles"] == coverage["expectedBoundaryFiles"] > 0
        self.assertTrue(exit_assert, run.stderr)
        self.assertTrue(clean_assert)
        self.assertTrue(zero_assert)
        self.assertTrue(coverage_assert)

    def test_non_boundary_compile_error_is_analysis_failure(self):
        sources = fixture.clean_sources()
        sources["Misc/Broken.cs"] = f"namespace {fixture.NS}.Misc;\n\npublic class Broken\n{{\n    public int Value = \"text\";\n}}\n"
        result = self.assert_not_completed(fixture.invoke_fixture(WORK, "non-boundary-error", sources), "analysis_failed")
        diagnostic_assert = any(item["kind"] == "compiler" and item["id"] == "CS0029" for item in result["diagnostics"])
        self.assertTrue(diagnostic_assert)

    def test_referenced_project_error_is_analysis_failure(self):
        library = {
            "02_Server/Support/Support.csproj": fixture.PROJECT_XML.format(extra=""),
            "02_Server/Support/Helper.cs": "namespace Support;\n\npublic class Helper\n{\n    public int Value = \"text\";\n}\n",
        }
        reference = '  <ItemGroup>\n    <ProjectReference Include="../Support/Support.csproj" />\n  </ItemGroup>\n'
        run = fixture.invoke_fixture(
            WORK, "reference-error", fixture.clean_sources(), project_extra=reference, extra_files=library)
        result = self.assert_not_completed(run, "analysis_failed")
        diagnostic_assert = any(
            item["kind"] == "compiler" and item["id"] == "CS0029" and (item["source"] or {}).get("path", "").endswith("Helper.cs")
            for item in result["diagnostics"]
        )
        self.assertTrue(diagnostic_assert)

    def test_boundary_file_removed_from_compile_is_not_completed(self):
        sources = fixture.clean_sources()
        sources["Handlers/Excluded.cs"] = fixture.WARNING_PROBES["Handlers/MemberCall.cs"][1].replace("MemberCall", "Excluded")
        removal = '  <ItemGroup>\n    <Compile Remove="Handlers/Excluded.cs" />\n  </ItemGroup>\n'
        self.assert_not_completed(fixture.invoke_fixture(WORK, "compile-removed", sources, project_extra=removal))

    def test_missing_project_reference_is_input_failure(self):
        reference = '  <ItemGroup>\n    <ProjectReference Include="../Absent/Absent.csproj" />\n  </ItemGroup>\n'
        run = fixture.invoke_fixture(WORK, "absent-reference", fixture.clean_sources(), project_extra=reference)
        self.assert_not_completed(run, "input_missing")

    def test_fixture_cannot_claim_a_source_revision(self):
        run = fixture.invoke_fixture(WORK, "fixture-source-ref", fixture.clean_sources(), ["--source-ref", "a" * 40])
        self.assert_not_completed(run, "input_invalid")

    def test_widened_limits_are_rejected(self):
        for option, value in (("--max-source-files", 5001), ("--max-source-bytes", 64 * 1024 * 1024 + 1), ("--stage-timeout", 601)):
            with self.subTest(option=option):
                run = fixture.invoke_fixture(WORK, "widen" + option, fixture.clean_sources(), [option, value])
                self.assert_not_completed(run, "input_limit")

    def test_policy_outside_the_approved_scope_is_invalid(self):
        original = json.loads(fixture.RULES.read_text(encoding="utf-8"))

        def escalate(policy):
            policy["rules"][0]["severity"] = "error"

        def widen_exception(policy):
            policy["rules"][0]["allowedTypes"] = [f"{fixture.MAPS}.WorldMap"]

        def narrow_dispatcher(policy):
            policy["rules"][2]["allowedTypes"] = policy["rules"][2]["allowedTypes"][:1]

        def move_area(policy):
            policy["areas"]["Maps"] = f"{fixture.SERVER}/World"

        for name, mutate in (("severity", escalate), ("mb001-exception", widen_exception),
                             ("mb003-exception", narrow_dispatcher), ("area-path", move_area)):
            with self.subTest(mutation=name):
                policy = json.loads(json.dumps(original))
                mutate(policy)
                rules_path = fixture.new_run(WORK, "policy-" + name) / "rules.json"
                rules_path.write_text(json.dumps(policy), encoding="utf-8")
                run = fixture.invoke_fixture(WORK, "policy-" + name, fixture.clean_sources(), ["--rules", rules_path])
                self.assert_not_completed(run, "invalid_rules")

    def test_output_outside_permitted_roots_is_rejected_before_writing(self):
        output = pathlib.Path("/proc") / f"module-boundary-independent-{uuid.uuid4().hex}"
        run = fixture.invoke(WORK, "output-outside", ["--input-kind", "fixture"], output=output)
        rejected_assert = run.returncode != 0 and "unsafe_path" in run.stderr
        absent_assert = not output.exists()
        self.assertTrue(rejected_assert, run.stderr)
        self.assertTrue(absent_assert)

    def test_output_through_symlink_is_rejected_before_writing(self):
        holder = fixture.new_run(WORK, "output-link")
        real = holder / "real"
        real.mkdir()
        link = holder / "link"
        link.symlink_to(real, target_is_directory=True)
        run = fixture.invoke(WORK, "output-link", ["--input-kind", "fixture"], output=link / "result")
        rejected_assert = run.returncode != 0 and "unsafe_path" in run.stderr
        untouched_assert = list(real.iterdir()) == []
        self.assertTrue(rejected_assert, run.stderr)
        self.assertTrue(untouched_assert)

    def test_sdk_source_generator_in_non_boundary_file_completes(self):
        # Generated inputs participate in compilation; a compilable server
        # source using a standard SDK generator is not an input/tool failure.
        sources = fixture.clean_sources()
        sources["Misc/Patterns.cs"] = (
            "using System.Text.RegularExpressions;\n\n"
            f"namespace {fixture.NS}.Misc;\n\n"
            "public static partial class Patterns\n{\n"
            "    [GeneratedRegex(\"^[a-z]+$\")]\n"
            "    public static partial Regex Word();\n}\n"
        )
        run = fixture.invoke_fixture(WORK, "source-generator", sources)
        self.assertIsNotNone(run.result, run.stderr)
        completed_assert = run.returncode == 0 and run.result["executionStatus"] == "completed"
        self.assertTrue(completed_assert, f"{run.result['reasonCode']}: {run.result['message']}")


@unittest.skipIf(WORK is None, SKIP_REASON)
class IndependentProcessLifetime(unittest.TestCase):
    """Timeout and cancellation settle only the checker's own process groups."""

    def test_stage_timeout_is_failure_and_settles_group(self):
        run = fixture.invoke_fixture(WORK, "stage-timeout", fixture.clean_sources(), ["--stage-timeout", "0.5"])
        self.assertIsNotNone(run.result, run.stderr)
        last = run.result["stages"][-1]
        exit_assert = run.returncode == 124
        state_assert = run.result["reasonCode"] == "timeout" and run.result["executionStatus"] != "completed"
        no_zero_claim_assert = run.result["violationCount"] is None
        stage_assert = last["reasonCode"] == "timeout" and last["processSettlement"] == "terminated_owned_group"
        group_gone_assert = not fixture.group_alive(last["pid"])
        self.assertTrue(exit_assert, run.stdout)
        self.assertTrue(state_assert)
        self.assertTrue(no_zero_claim_assert)
        self.assertTrue(stage_assert, last)
        self.assertTrue(group_gone_assert)

    def test_sigterm_cancels_and_settles_running_stage(self):
        returncode, signalled, result, output = fixture.cancel_after_stage(
            WORK, "sigterm", fixture.clean_sources(), "tool-build")
        self.assertTrue(signalled, "the tool-build stage never started before the deadline")
        self.assertIsNotNone(result, "cancelled run must still write its own result.json")
        exit_assert = returncode == 130
        state_assert = result["executionStatus"] == "cancelled" and result["reasonCode"] == "cancelled"
        no_zero_claim_assert = result["violationCount"] is None and result["analysisStatus"] == "not_completed"
        stage_records = sorted((output / "stages").glob("*/command.json"))
        last = json.loads(stage_records[-1].read_text(encoding="utf-8"))
        settled_assert = last.get("reasonCode") == "cancelled" and last.get("processSettlement") is not None
        group_gone_assert = "pid" in last and not fixture.group_alive(last["pid"])
        self.assertTrue(exit_assert, returncode)
        self.assertTrue(state_assert, result["reasonCode"])
        self.assertTrue(no_zero_claim_assert)
        self.assertTrue(settled_assert, last)
        self.assertTrue(group_gone_assert, last)


@unittest.skipIf(WORK is None or not REAL_SOURCE, "Set MODULE_BOUNDARIES_INDEPENDENT_REAL=1 for real source entries")
class IndependentRealSourceEntry(unittest.TestCase):
    """Real repository sources through the public entry (no fixture)."""

    @staticmethod
    def physical_boundary_files():
        # Requirement oracle: hand-written .cs files in the three folders.
        count = 0
        for area in ("Handlers", "Maps", "Sessions"):
            for current, subdirs, names in os.walk(fixture.REPO / fixture.SERVER / area):
                subdirs[:] = [name for name in subdirs if name not in ("bin", "obj")]
                count += sum(
                    name.endswith(".cs") and not name.endswith((".g.cs", ".generated.cs")) for name in names
                )
        return count

    def assert_completed_source(self, run, source_mode, source_sha, boundary_files):
        self.assertIsNotNone(run.result, run.stderr)
        result = run.result
        exit_assert = run.returncode == 0
        completed_assert = result["executionStatus"] == "completed"
        mode_assert = result["sourceMode"] == source_mode and result["sourceSha"] == source_sha
        coverage = result["coverage"]
        coverage_assert = coverage["analyzedBoundaryFiles"] == coverage["expectedBoundaryFiles"] == boundary_files
        state_matches_count_assert = (result["analysisStatus"] == "clean") == (result["violationCount"] == 0)
        rule_sum_assert = sum(result["ruleCounts"].values()) == result["violationCount"]
        self.assertTrue(exit_assert, run.stderr)
        self.assertTrue(completed_assert, result["reasonCode"])
        self.assertTrue(mode_assert, (result["sourceMode"], result["sourceSha"]))
        self.assertTrue(coverage_assert, coverage)
        self.assertTrue(state_matches_count_assert)
        self.assertTrue(rule_sum_assert)
        return result

    def test_workspace_checkout_with_pr_head_marker(self):
        head = os.environ["MODULE_BOUNDARIES_INDEPENDENT_HEAD"]
        marker = "f" * 40
        run = fixture.invoke(WORK, "real-workspace", [], {"MODULE_BOUNDARIES_PR_HEAD_SHA": marker})
        result = self.assert_completed_source(run, "workspace", head, self.physical_boundary_files())
        separated_assert = result["checkoutSha"] == head and result["prHeadSha"] == marker
        self.assertTrue(separated_assert)

    def test_main_and_head_blobs_under_same_condition(self):
        for label, variable in (("main", "MODULE_BOUNDARIES_INDEPENDENT_MAIN"), ("head", "MODULE_BOUNDARIES_INDEPENDENT_HEAD")):
            with self.subTest(source=label):
                sha = os.environ[variable]
                count = int(os.environ[f"MODULE_BOUNDARIES_INDEPENDENT_{label.upper()}_BOUNDARY_FILES"])
                run = fixture.invoke(WORK, f"real-{label}-blobs", ["--source-ref", sha])
                self.assert_completed_source(run, "git_blobs", sha, count)


if __name__ == "__main__":
    unittest.main()
