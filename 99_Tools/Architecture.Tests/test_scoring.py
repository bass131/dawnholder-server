"""Closed-domain scoring through the public score command.

The rules come from the frozen scope: same-name wrong targets are not hits, a missed answer
stays FN with the denominator unchanged, out-of-domain output is counted separately, and
unresolved candidates are never confirmed."""
import copy
import csv
import unittest

from support import mini_inputs
from support.mini_inputs import Workspace


def _group(score, category, key):
    return next(item for item in score["groups"] if item["category"] == category and item["key"] == key)


def _row(score, kind, source, target):
    return next(row for row in score["rows"] if (row["kind"], row["source"], row["target"]) == (kind, source, target))


def _retarget_roslyn(raw, source, old_target, new_target):
    for relation in raw["relations"]:
        if relation["sourceKey"] == mini_inputs.roslyn_key(source) and relation["targetKey"] == mini_inputs.roslyn_key(old_target):
            relation["targetKey"] = mini_inputs.roslyn_key(new_target)
            return
    raise AssertionError("relation not found")


def _retarget_codegraph(raw, source, old_target, new_target):
    for edge in raw["edges"]:
        if edge["source"] == mini_inputs.codegraph_id(source) and edge["target"] == mini_inputs.codegraph_id(old_target):
            edge["target"] = mini_inputs.codegraph_id(new_target)
            return
    raise AssertionError("edge not found")


class ScoringTests(unittest.TestCase):
    def setUp(self):
        self.workspace = Workspace()
        self.addCleanup(self.workspace.close)

    def score(self, raw, scope=None, truth=None):
        snapshot = self.workspace.snapshot(raw)
        result, out = self.workspace.score(snapshot, scope, truth)
        self.assertEqual(0, result.returncode, result.stderr)
        return self.workspace.read(out.name)

    def test_correct_extraction_scores_every_answer_once(self):
        for extractor, raw in {"Roslyn": mini_inputs.roslyn_raw(), "CodeGraph": mini_inputs.codegraph_raw()}.items():
            with self.subTest(extractor):
                score = self.score(raw)
                self.assertEqual("scored", score["status"])
                self.assertEqual((10, 6, 4), (score["candidateCount"], score["positiveCount"], score["negativeCount"]))
                self.assertEqual({"tp": 6, "fp": 0, "fn": 0, "predicted": 6, "expected": 6, "precision": 1.0, "recall": 1.0}, score["overall"])
                self.assertEqual((5, 0, 0), tuple(_group(score, "layer", "Server")[name] for name in ("tp", "fp", "fn")))
                self.assertEqual((1, 0, 0), tuple(_group(score, "layer", "ClientNet")[name] for name in ("tp", "fp", "fn")))
                self.assertEqual((4, 0, 0), tuple(_group(score, "kind", "calls")[name] for name in ("tp", "fp", "fn")))
                self.assertEqual(10, len(score["rows"]))

    def test_same_name_wrong_target_inside_domain_is_fp_and_fn(self):
        roslyn = mini_inputs.roslyn_raw()
        _retarget_roslyn(roslyn, "attackHandle", "attackRead", "moveRead")
        codegraph = mini_inputs.codegraph_raw()
        _retarget_codegraph(codegraph, "mapAttack", "combatAttack", "combatAttackLong")
        cases = {
            "Roslyn other packet Read": (roslyn, ("calls", "attackHandle", "moveRead"), ("calls", "attackHandle", "attackRead")),
            "CodeGraph same qualified overload": (codegraph, ("calls", "mapAttack", "combatAttackLong"), ("calls", "mapAttack", "combatAttack")),
        }
        for name, (raw, wrong, missed) in cases.items():
            with self.subTest(name):
                score = self.score(raw)
                self.assertEqual((5, 1, 1), (score["overall"]["tp"], score["overall"]["fp"], score["overall"]["fn"]))
                self.assertEqual("FP", _row(score, *wrong)["result"])
                self.assertEqual("FN", _row(score, *missed)["result"])

    def test_same_name_wrong_target_outside_domain_is_counted_separately(self):
        baseline = self.score(mini_inputs.codegraph_raw())
        raw = mini_inputs.codegraph_raw()
        _retarget_codegraph(raw, "netRecv", "clientValidate", "serverValidate")
        score = self.score(raw)
        self.assertEqual((5, 0, 1), (score["overall"]["tp"], score["overall"]["fp"], score["overall"]["fn"]))
        self.assertEqual(6, score["overall"]["expected"], "a miss must not shrink the denominator")
        self.assertEqual(baseline["outsideScopeEdgeCount"] + 1, score["outsideScopeEdgeCount"])
        row = _row(score, "calls", "netRecv", "clientValidate")
        self.assertEqual(("FN", []), (row["result"], row["edgeIds"]))
        client_net = _group(score, "layer", "ClientNet")
        self.assertEqual((0, 0, 1, None, 0.0), (client_net["tp"], client_net["fp"], client_net["fn"], client_net["precision"], client_net["recall"]))

    def test_ambiguous_candidate_containing_answer_is_not_a_hit(self):
        raw = mini_inputs.roslyn_raw()
        raw["relations"] = [relation for relation in raw["relations"] if relation["targetKey"] != mini_inputs.roslyn_key("attackRead")]
        candidates = [mini_inputs.roslyn_key("attackRead"), mini_inputs.roslyn_key("moveRead")]
        raw["relations"].append(mini_inputs.roslyn_unresolved("attackHandle", "calls", 10, 9, "pkt.Read(buffer)", candidates, "ambiguous"))
        score = self.score(raw)
        self.assertEqual("FN", _row(score, "calls", "attackHandle", "attackRead")["result"])
        self.assertEqual("TN", _row(score, "calls", "attackHandle", "moveRead")["result"])
        self.assertEqual(1, score["unresolvedOrAmbiguousEdgeCount"])

    def test_unsupported_relation_kind_keeps_answers_as_fn(self):
        raw = mini_inputs.codegraph_raw()
        raw["edges"] = [edge for edge in raw["edges"] if edge["kind"] != "instantiates"]
        snapshot = self.workspace.snapshot(raw)
        snapshot["supports"]["usesType"] = False
        result, out = self.workspace.score(snapshot)
        self.assertEqual(0, result.returncode, result.stderr)
        score = self.workspace.read(out.name)
        row = _row(score, "usesType", "attackHandle", "attackPacket")
        self.assertEqual(("FN", False), (row["result"], row["supported"]))
        self.assertEqual((5, 0, 1, 6), (score["overall"]["tp"], score["overall"]["fp"], score["overall"]["fn"], score["overall"]["expected"]))

    def test_declaration_anchor_mismatch_is_not_rescued_by_name(self):
        raw = mini_inputs.codegraph_raw()
        node = next(item for item in raw["nodes"] if item["id"] == mini_inputs.codegraph_id("combatAttack"))
        node["start_line"] += 1
        score = self.score(raw)
        self.assertEqual("FN", _row(score, "calls", "mapAttack", "combatAttack")["result"])
        self.assertEqual("TN", _row(score, "calls", "mapAttack", "combatAttackLong")["result"])

    def test_metadata_and_domain_mismatches_fail(self):
        snapshot = self.workspace.snapshot(mini_inputs.roslyn_raw())

        def scope_other_sha(scope, truth):
            scope["sourceCommit"] = "f" * 40

        def truth_other_sha(scope, truth):
            truth["sourceCommit"] = "f" * 40

        def positive_outside_domain(scope, truth):
            truth["positives"].append({"id": "P99", "kind": "calls", "source": "netRecv", "target": "serverValidate"})

        def missing_negative(scope, truth):
            truth["negatives"].pop()

        def answer_both_ways(scope, truth):
            truth["negatives"].append(copy.deepcopy(truth["positives"][0]))

        def unknown_group_symbol(scope, truth):
            scope["groups"][0]["targets"].append("undeclaredSymbol")
            truth["negatives"].append({"id": "N99", "kind": "implements", "source": "attackType", "target": "undeclaredSymbol"})

        cases = {
            "scope SHA differs": scope_other_sha,
            "truth SHA differs": truth_other_sha,
            "positive outside frozen domain": positive_outside_domain,
            "domain candidate without answer": missing_negative,
            "answer both positive and negative": answer_both_ways,
            "group names undeclared symbol": unknown_group_symbol,
        }
        for name, mutate in cases.items():
            with self.subTest(name):
                scope, truth = mini_inputs.scope(), mini_inputs.truth()
                mutate(scope, truth)
                result, out = self.workspace.score(snapshot, scope, truth, out=f"{name}.json")
                self.assertEqual(1, result.returncode, result.stdout + result.stderr)
                self.assertIn("ERROR:", result.stderr)
                self.assertFalse(out.exists())

    # Verdict finding N2: set semantics silently absorb duplicate rows and reused IDs, and the
    # declared counts are not compared. The frozen answer table itself has unique rows and IDs.
    @unittest.expectedFailure
    def test_answer_table_rows_must_match_its_declared_identity(self):
        snapshot = self.workspace.snapshot(mini_inputs.roslyn_raw())

        def duplicate_answer_row(truth):
            truth["positives"].append({**truth["positives"][0], "id": "P99"})

        def reused_answer_id(truth):
            truth["negatives"][0]["id"] = truth["positives"][0]["id"]

        def declared_count_differs(truth):
            truth["counts"]["positive"] += 1

        cases = {"duplicate answer row": duplicate_answer_row, "reused answer id": reused_answer_id, "declared count differs": declared_count_differs}
        for name, mutate in cases.items():
            with self.subTest(name):
                truth = mini_inputs.truth()
                mutate(truth)
                result, out = self.workspace.score(snapshot, truth_value=truth, out=f"{name}.json")
                self.assertEqual(1, result.returncode, f"{name} was accepted: {result.stderr}")

    # Verdict finding N3: only the snapshot schema is checked; manifest, scope and truth
    # versions are read without a known-version check.
    @unittest.expectedFailure
    def test_unknown_input_schema_versions_are_rejected(self):
        snapshot = self.workspace.snapshot(mini_inputs.roslyn_raw())
        manifest = mini_inputs.manifest()
        manifest["schemaVersion"] = 2
        normalized, _ = self.workspace.normalize(mini_inputs.roslyn_raw(), manifest_value=manifest, out="unknown-manifest.json")
        scope = mini_inputs.scope()
        scope["schemaVersion"] = 2
        scored_scope, _ = self.workspace.score(snapshot, scope_value=scope, out="unknown-scope.json")
        truth = mini_inputs.truth()
        truth["schemaVersion"] = 2
        scored_truth, _ = self.workspace.score(snapshot, truth_value=truth, out="unknown-truth.json")
        for name, result in {"manifest": normalized, "scope": scored_scope, "truth": scored_truth}.items():
            with self.subTest(name):
                self.assertEqual(1, result.returncode, f"unknown {name} schema was accepted")

    def test_csv_mirrors_json_rows(self):
        raw = mini_inputs.roslyn_raw()
        _retarget_roslyn(raw, "attackHandle", "attackRead", "moveRead")
        snapshot = self.workspace.snapshot(raw)
        result, out = self.workspace.score(snapshot)
        self.assertEqual(0, result.returncode, result.stderr)
        rows = self.workspace.read(out.name)["rows"]
        with out.with_suffix(".csv").open(encoding="utf-8", newline="") as stream:
            table = list(csv.DictReader(stream))
        self.assertEqual([(row["kind"], row["source"], row["target"], row["result"]) for row in rows], [(row["kind"], row["source"], row["target"], row["result"]) for row in table])
        self.assertEqual(["|".join(row["edgeIds"]) for row in rows], [row["edgeIds"] for row in table])


if __name__ == "__main__":
    unittest.main()
