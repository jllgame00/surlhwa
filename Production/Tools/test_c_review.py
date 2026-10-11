"""Failure-contract tests for C evidence; synthetic metadata only, never review renders."""
import importlib.util
import importlib
import unittest

class COutcomeTests(unittest.TestCase):
    def setUp(self):
        self.assertIsNotNone(importlib.util.find_spec('inspect_c_review'), 'C evidence validation is not implemented')
        self.validate = importlib.import_module('inspect_c_review').outcome_failures
        self.run = {'runId': 'fresh-c', 'initialized': True, 'preview': False, 'errors': [], 'staticRootAndFeetDrift': False,
                    'measurement': {'durationSeconds': 60, 'samples': 400, 'drawsMean': 90, 'batchesMean': 90, 'gcRecorder': True, 'drawRecorder': True, 'batchRecorder': True},
                    'captures': [{'fixture': f, 'width': w, 'height': w*9//16, 'file': f'C_{f}_{w}x{w*9//16}.png'}
                                 for f in ('Empty', 'OrdinarySeated', 'SpecialSeated', 'Composition', 'GroundingInspection', 'Wide') for w in (1280,1920)]}
        self.expected = {'runId': 'fresh-c'}; self.process = {'runId': 'fresh-c', 'exitCode': 0}
        self.log = 'C_CAPTURE_FINISHED 12 files; errors 0'
    def check(self): return self.validate(self.run, self.expected, self.process, self.log)
    def test_complete_native_metadata_is_accepted(self): self.assertEqual([], self.check())
    def test_preview_cannot_be_final_evidence(self):
        self.run['preview'] = True; self.assertTrue(self.check())
    def test_duplicate_capture_does_not_hide_missing_fixture(self):
        self.run['captures'][-1] = self.run['captures'][0]; self.assertTrue(self.check())
    def test_old_identity_is_rejected(self):
        self.run['runId'] = 'old-c'; self.assertTrue(self.check())
    def test_startup_exception_is_rejected(self):
        self.log = 'InvalidOperationException: Bad C bindings\n' + self.log; self.assertTrue(self.check())
    def test_performance_without_render_submissions_is_rejected(self):
        self.run['measurement']['drawsMean'] = 0; self.assertTrue(self.check())
    def test_nonzero_process_exit_is_rejected(self):
        self.process['exitCode'] = 1; self.assertTrue(self.check())

if __name__ == '__main__': unittest.main()
