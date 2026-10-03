"""Black-box snapshot contract: a valid snapshot passes, and every unknown, mismatched,
dangling or incomplete variant fails instead of being accepted as a normal result."""
import copy
import unittest

from support import mini_inputs
from support.mini_inputs import Workspace


def _first(items, predicate):
    return next(item for item in items if predicate(item))


def _resolved_call(snapshot):
    return _first(snapshot["edges"], lambda edge: edge["kind"] == "calls" and edge["resolution"] == "resolved")


def _type_node(snapshot):
    return _first(snapshot["nodes"], lambda node: node["kind"] == "type" and node["source"])


class SnapshotContractTests(unittest.TestCase):
    def setUp(self):
        self.workspace = Workspace()
        self.addCleanup(self.workspace.close)
        self.snapshot = self.workspace.snapshot(mini_inputs.roslyn_raw())

    def assert_rejected(self, mutated, git_value=None, manifest_value=None):
        result, out = self.workspace.validate(mutated, manifest_value=manifest_value, git_value=git_value)
        self.assertEqual(1, result.returncode, result.stdout + result.stderr)
        self.assertIn("ERROR:", result.stderr)
        self.assertFalse(out.exists(), "a rejected snapshot must not leave a validation result")

    def test_normalized_fixture_validates_with_exact_git_case(self):
        manifest = mini_inputs.manifest()
        result, out = self.workspace.validate(self.snapshot, git_value=mini_inputs.git_metadata(manifest))
        self.assertEqual(0, result.returncode, result.stderr)
        report = self.workspace.read(out.name)
        self.assertEqual({"valid": True, "status": "partial", "nodes": len(self.snapshot["nodes"]), "edges": len(self.snapshot["edges"])}, report)

    def test_identity_and_scope_mutations_fail(self):
        mutations = {
            "unknown schema version": lambda s: s.update(schemaVersion=2),
            "missing schema version": lambda s: s.pop("schemaVersion"),
            "short SHA": lambda s: s.update(commitSha=s["commitSha"][:12]),
            "uppercase SHA": lambda s: s.update(commitSha=s["commitSha"].upper()),
            "other full SHA": lambda s: s.update(commitSha="f" * 40),
            "unknown status": lambda s: s.update(status="done"),
            "partial without reason": lambda s: s.update(diagnostics=[]),
            "missing repository": lambda s: s.update(repository=""),
            "short config hash": lambda s: s["extractor"].update(configHash="abc"),
            "missing extractor version": lambda s: s["extractor"].pop("version"),
            "other manifest hash": lambda s: s["analysisScope"].update(manifestHash="0" * 64),
            "changed included roots": lambda s: s["analysisScope"].update(includedRoots=s["analysisScope"]["includedRoots"][:-1]),
            "changed excluded roots": lambda s: s["analysisScope"].update(excludedRoots=[]),
            "wrong input count": lambda s: s["analysisScope"].update(inputFileCount=s["analysisScope"]["inputFileCount"] + 1),
        }
        for name, mutate in mutations.items():
            with self.subTest(name):
                mutated = copy.deepcopy(self.snapshot)
                mutate(mutated)
                self.assert_rejected(mutated)

    def test_node_and_path_mutations_fail(self):
        def set_path(value):
            return lambda s: _type_node(s)["source"].update(path=value)

        mutations = {
            "absolute path": set_path("/02_Server/Game/Maps/GameMap.cs"),
            "backslash path": set_path("02_Server\\Game\\Maps\\GameMap.cs"),
            "parent segment": set_path("02_Server/Game/../Game/Maps/GameMap.cs"),
            "dot segment": set_path("02_Server/Game/./Maps/GameMap.cs"),
            "trailing slash": set_path("02_Server/Game/Maps/"),
            "url": set_path("https://example.invalid/GameMap.cs"),
            "glob": set_path("02_Server/Game/Maps/*.cs"),
            "path outside manifest": set_path("02_Server/Game/Maps/Unlisted.cs"),
            "case-folded path": set_path("02_server/game/maps/gamemap.cs"),
            "line zero": lambda s: _type_node(s)["source"].update(line=0),
            "string column": lambda s: _type_node(s)["source"].update(column="5"),
            "unknown node kind": lambda s: _type_node(s).update(kind="property"),
            "unknown resolution": lambda s: _type_node(s).update(resolution="guessed"),
            "missing display name": lambda s: _type_node(s).update(displayName=""),
            "missing layer": lambda s: _type_node(s).update(layer=""),
            "malformed node id": lambda s: s["nodes"][0].update(id="node:abc"),
        }
        for name, mutate in mutations.items():
            with self.subTest(name):
                mutated = copy.deepcopy(self.snapshot)
                mutate(mutated)
                self.assert_rejected(mutated)

    def test_git_case_is_enforced_when_tree_metadata_is_supplied(self):
        manifest = mini_inputs.manifest()
        git = mini_inputs.git_metadata(manifest)
        renamed = "02_Server/Game/Maps/GameMap.cs"
        git["tree"]["02_Server/Game/Maps/gamemap.cs"] = git["tree"].pop(renamed)
        self.assert_rejected(copy.deepcopy(self.snapshot), git_value=git)

    def test_graph_mutations_fail(self):
        def duplicate_node(s):
            s["nodes"].insert(1, copy.deepcopy(s["nodes"][0]))

        def duplicate_edge(s):
            s["edges"].insert(1, copy.deepcopy(s["edges"][0]))

        def dangling_target(s):
            _resolved_call(s)["targetId"] = "node:" + "e" * 64

        def dangling_source(s):
            _resolved_call(s)["sourceId"] = "node:" + "e" * 64

        def edge_id_not_from_relation(s):
            edge = _resolved_call(s)
            edge["kind"] = "usesType"

        def unsorted_nodes(s):
            s["nodes"].reverse()

        def unsorted_edges(s):
            s["edges"].reverse()

        mutations = {
            "duplicate node id": duplicate_node,
            "duplicate edge id": duplicate_edge,
            "dangling target": dangling_target,
            "dangling source": dangling_source,
            "edge id differs from kind/source/target": edge_id_not_from_relation,
            "unknown relation kind": lambda s: _resolved_call(s).update(kind="inherits"),
            "empty evidence": lambda s: _resolved_call(s).update(evidence=[]),
            "evidence outside manifest": lambda s: _resolved_call(s)["evidence"][0].update(path="02_Server/Game/Other.cs"),
            "evidence line zero": lambda s: _resolved_call(s)["evidence"][0].update(line=0),
            "unknown static context": lambda s: _resolved_call(s)["evidence"][0].update(context="runtime"),
            "unsorted nodes": unsorted_nodes,
            "unsorted edges": unsorted_edges,
        }
        for name, mutate in mutations.items():
            with self.subTest(name):
                mutated = copy.deepcopy(self.snapshot)
                mutate(mutated)
                self.assert_rejected(mutated)

    def test_incomplete_results_cannot_pose_as_complete_or_empty_success(self):
        failed_with_edges = copy.deepcopy(self.snapshot)
        failed_with_edges["status"] = "failed"
        not_run_with_edges = copy.deepcopy(self.snapshot)
        not_run_with_edges["status"] = "notRun"
        unresolved_raw = mini_inputs.roslyn_raw()
        unresolved_raw["relations"].append(mini_inputs.roslyn_unresolved("attackHandle", "calls", 11, 9, "pkt.Missing()", [], "unresolved"))
        complete_with_unresolved = self.workspace.snapshot(unresolved_raw)
        complete_with_unresolved["status"] = "complete"
        for name, mutated in {"failed with relations": failed_with_edges, "notRun with relations": not_run_with_edges, "complete with unresolved": complete_with_unresolved}.items():
            with self.subTest(name):
                self.assert_rejected(mutated)

    def test_missing_and_corrupt_inputs_fail_without_output(self):
        snapshot_path = self.workspace.write("snapshot.json", self.snapshot)
        manifest_path = self.workspace.write("manifest.json", mini_inputs.manifest())
        corrupt_path = self.workspace.write("corrupt.json", b"{\"schemaVersion\": 1,")
        cases = {
            "missing snapshot": ["--snapshot", self.workspace.root / "absent.json", "--manifest", manifest_path],
            "missing manifest": ["--snapshot", snapshot_path, "--manifest", self.workspace.root / "absent.json"],
            "corrupt snapshot": ["--snapshot", corrupt_path, "--manifest", manifest_path],
        }
        for name, arguments in cases.items():
            with self.subTest(name):
                out = self.workspace.root / f"{name}.json"
                result = mini_inputs.run_cli("validate", *arguments, "--out", out)
                self.assertEqual(1, result.returncode, result.stderr)
                self.assertIn("ERROR:", result.stderr)
                self.assertFalse(out.exists())

    def test_changed_manifest_bytes_invalidate_existing_snapshot(self):
        manifest = mini_inputs.manifest()
        manifest["files"][0]["sha256"] = "1" * 64
        self.assert_rejected(copy.deepcopy(self.snapshot), manifest_value=manifest)


if __name__ == "__main__":
    unittest.main()
