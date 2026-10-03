"""System card codeReference join through the public join command.

Contract: join only on the same full SHA, exact Git-case repository paths, directory segment
boundaries, namespace as description only, and many-to-many membership without a primary."""
import unittest

from support import mini_inputs
from support.mini_inputs import DECLARATIONS, SOURCE_COMMIT, Workspace

MAPS = "02_Server/Game/Maps"
GAME_MAP = "02_Server/Game/Maps/GameMap.cs"
LEGACY = "02_Server/Game/MapsLegacy/OldMap.cs"


class CodeReferenceJoinTests(unittest.TestCase):
    def setUp(self):
        self.workspace = Workspace()
        self.addCleanup(self.workspace.close)
        self.snapshot = self.workspace.snapshot(mini_inputs.roslyn_raw())
        self.paths_by_node = {node["id"]: (node.get("source") or {}).get("path") for node in self.snapshot["nodes"]}

    def join(self, mappings, commit=SOURCE_COMMIT):
        result, out = self.workspace.join(self.snapshot, {"commitSha": commit, "mappings": mappings})
        self.assertEqual(0, result.returncode, result.stderr)
        return self.workspace.read(out.name)

    def matched_paths(self, joined, mapping_index):
        return {self.paths_by_node[item["nodeId"]] for item in joined["matches"] if item["mappingIndex"] == mapping_index}

    def test_directory_matches_segment_boundary_only(self):
        joined = self.join([{"path": MAPS, "kind": "directory"}])
        self.assertEqual("matched", joined["status"])
        self.assertEqual({GAME_MAP, "02_Server/Game/Maps/Systems/CombatSystem.cs"}, self.matched_paths(joined, 0))
        self.assertNotIn(LEGACY, self.matched_paths(joined, 0))

    def test_file_mapping_matches_only_that_file(self):
        joined = self.join([{"path": GAME_MAP, "kind": "file"}])
        self.assertEqual({GAME_MAP}, self.matched_paths(joined, 0))
        declared = {key for key, declaration in DECLARATIONS.items() if declaration[5] == GAME_MAP}
        self.assertGreaterEqual(len([item for item in joined["matches"] if item["mappingIndex"] == 0]), len(declared) + 1)

    def test_overlapping_mappings_keep_every_membership_and_namespace_is_not_a_filter(self):
        joined = self.join([{"path": MAPS, "kind": "directory", "namespace": "Unrelated.Namespace"}, {"path": GAME_MAP, "kind": "file", "role": "map"}])
        self.assertIn(GAME_MAP, self.matched_paths(joined, 0))
        self.assertEqual({GAME_MAP}, self.matched_paths(joined, 1))
        shared = {item["nodeId"] for item in joined["matches"] if item["mappingIndex"] == 0} & {item["nodeId"] for item in joined["matches"] if item["mappingIndex"] == 1}
        self.assertTrue(shared, "a node in both mappings must be reported for both")
        self.assertEqual(2, len(joined["mappings"]))
        self.assertEqual("partial", joined["analysisStatus"])

    def test_empty_mappings_are_unmapped_not_failure(self):
        joined = self.join([])
        self.assertEqual(("unmapped", []), (joined["status"], joined["matches"]))

    def test_other_full_sha_reports_mismatch_without_matches(self):
        joined = self.join([{"path": MAPS, "kind": "directory"}], commit="f" * 40)
        self.assertEqual({"status": "shaMismatch", "mappings": [], "matches": []}, joined)

    def test_invalid_references_fail(self):
        cases = {
            "short SHA": (SOURCE_COMMIT[:12], [{"path": MAPS, "kind": "directory"}]),
            "uppercase SHA": (SOURCE_COMMIT.upper(), [{"path": MAPS, "kind": "directory"}]),
            "absolute path": (SOURCE_COMMIT, [{"path": "/" + MAPS, "kind": "directory"}]),
            "backslash path": (SOURCE_COMMIT, [{"path": MAPS.replace("/", "\\"), "kind": "directory"}]),
            "trailing slash": (SOURCE_COMMIT, [{"path": MAPS + "/", "kind": "directory"}]),
            "parent segment": (SOURCE_COMMIT, [{"path": "02_Server/Game/../Game/Maps", "kind": "directory"}]),
            "dot segment": (SOURCE_COMMIT, [{"path": "./02_Server/Game/Maps", "kind": "directory"}]),
            "empty path": (SOURCE_COMMIT, [{"path": "", "kind": "directory"}]),
            "url": (SOURCE_COMMIT, [{"path": "https://example.invalid/Maps", "kind": "directory"}]),
            "glob": (SOURCE_COMMIT, [{"path": "02_Server/Game/Maps/*", "kind": "directory"}]),
            "case-folded directory": (SOURCE_COMMIT, [{"path": MAPS.lower(), "kind": "directory"}]),
            "case-folded file": (SOURCE_COMMIT, [{"path": GAME_MAP.lower(), "kind": "file"}]),
            "absent directory": (SOURCE_COMMIT, [{"path": "02_Server/Game/Absent", "kind": "directory"}]),
            "file kind on directory": (SOURCE_COMMIT, [{"path": MAPS, "kind": "file"}]),
            "directory kind on file": (SOURCE_COMMIT, [{"path": GAME_MAP, "kind": "directory"}]),
            "unknown kind": (SOURCE_COMMIT, [{"path": MAPS, "kind": "module"}]),
            "mappings not an array": (SOURCE_COMMIT, {"path": MAPS, "kind": "directory"}),
        }
        for name, (commit, mappings) in cases.items():
            with self.subTest(name):
                result, out = self.workspace.join(self.snapshot, {"commitSha": commit, "mappings": mappings}, out=f"{name}.json")
                self.assertEqual(1, result.returncode, result.stdout + result.stderr)
                self.assertIn("ERROR:", result.stderr)
                self.assertFalse(out.exists())


if __name__ == "__main__":
    unittest.main()
