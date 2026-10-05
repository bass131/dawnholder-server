"""Installation-independent discovery must never start the SDK requirement suite.

The correction contract's #1 requires a reasoned opt-out and rejects malformed
explicit work roots. Guards observe side effects without creating default output
or recursively discovering this module.
"""

import importlib.util
import os
import pathlib
import unittest
from unittest import mock


SUITE_PATH = pathlib.Path(__file__).with_name("test_module_boundaries.py")
REPO = SUITE_PATH.resolve().parents[2]
WORK_VARIABLE = "MODULE_BOUNDARIES_TEST_WORK"


class ModuleBoundaryDiscovery(unittest.TestCase):
    def run_requirements(self, work_value, existing=False, symlink=False):
        environment = dict(os.environ)
        environment.pop(WORK_VARIABLE, None)
        if work_value is not None:
            environment[WORK_VARIABLE] = work_value
        guards = (
            mock.patch("subprocess.run", side_effect=AssertionError("external command during opt-out")),
            mock.patch("subprocess.Popen", side_effect=AssertionError("external process during opt-out")),
            mock.patch.object(pathlib.Path, "mkdir", side_effect=AssertionError("output mkdir during opt-out")),
            mock.patch.object(pathlib.Path, "write_text", side_effect=AssertionError("output text during opt-out")),
            mock.patch.object(pathlib.Path, "write_bytes", side_effect=AssertionError("output bytes during opt-out")),
            mock.patch.object(pathlib.Path, "exists", return_value=existing),
            mock.patch.object(pathlib.Path, "is_symlink", return_value=symlink),
        )
        with mock.patch.dict(os.environ, environment, clear=True):
            with guards[0] as run, guards[1] as process, guards[2] as mkdir:
                with guards[3] as text, guards[4] as data, guards[5], guards[6]:
                    spec = importlib.util.spec_from_file_location("module_boundary_discovery_probe", SUITE_PATH)
                    module = importlib.util.module_from_spec(spec)
                    spec.loader.exec_module(module)
                    suite = unittest.defaultTestLoader.loadTestsFromModule(module)
                    collected = suite.countTestCases()
                    result = unittest.TestResult()
                    suite.run(result)
        for operation in (run, process, mkdir, text, data):
            operation.assert_not_called()
        self.assertGreater(collected, 0, "The SDK requirement suite must actually be discovered")
        return collected, result

    def test_unset_work_skips_every_requirement_without_side_effects(self):
        collected, result = self.run_requirements(None)
        self.assertEqual(result.testsRun, collected)
        self.assertEqual(len(result.skipped), collected)
        self.assertTrue(result.wasSuccessful(), result.errors + result.failures)
        for _, reason in result.skipped:
            self.assertIn(WORK_VARIABLE, reason)
            self.assertIn("SDK", reason)

    def assert_invalid_work(self, value, expected, **conditions):
        _, result = self.run_requirements(value, **conditions)
        self.assertEqual(result.testsRun, 0)
        self.assertEqual(result.skipped, [])
        self.assertEqual(len(result.errors), 1)
        self.assertIn(expected, result.errors[0][1])
        self.assertFalse(result.wasSuccessful())

    def test_empty_explicit_work_fails_without_default_fallback(self):
        self.assert_invalid_work("", "absolute")

    def test_relative_explicit_work_fails_without_default_fallback(self):
        self.assert_invalid_work("relative-owned-work", "absolute")

    def test_existing_explicit_work_is_preserved(self):
        self.assert_invalid_work(str(REPO / ".backups/existing-owned-work"), "new", existing=True)

    def test_symlink_explicit_work_is_rejected_before_writes(self):
        self.assert_invalid_work(str(REPO / ".backups/linked-owned-work"), "symlink", symlink=True)


if __name__ == "__main__":
    unittest.main()
