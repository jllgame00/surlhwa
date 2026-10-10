"""Regression tests for capture-process trust, independent of images or Unity."""
import unittest
from inspect_captures import review_outcome_failures


class ReviewOutcomeTests(unittest.TestCase):
    def setUp(self):
        self.run = {'runId': 'fresh-run', 'initialized': True, 'errors': []}
        self.expected = {'runId': 'fresh-run'}
        self.process = {'runId': 'fresh-run', 'exitCode': 0}
        self.log = 'SPATIAL_CAPTURE_FINISHED 24 files; errors 0\n'

    def check(self):
        return review_outcome_failures(self.run, self.expected, self.process, self.log)

    def test_complete_success_is_accepted(self):
        self.assertEqual([], self.check())

    def test_initialization_exception_even_before_collected_errors_is_rejected(self):
        self.log = 'InvalidOperationException: Authored guest/visual binding mismatch\n' + self.log
        self.assertTrue(self.check())

    def test_stage_never_initialized_is_rejected(self):
        self.run['initialized'] = False
        self.assertTrue(self.check())

    def test_process_failure_with_old_complete_artifacts_is_rejected(self):
        self.process['exitCode'] = 1
        self.assertTrue(self.check())

    def test_previous_run_cannot_satisfy_a_new_capture(self):
        self.expected['runId'] = 'next-run'
        self.assertTrue(self.check())

    def test_unfinished_log_is_rejected(self):
        self.log = '[Subsystems] Discovering subsystems\n'
        self.assertTrue(self.check())

    def test_native_allocation_warning_remains_disclosed_nonfatal_evidence(self):
        self.log = 'Internal: deleting an allocation that is older than its permitted lifetime of 4 frames (age = 5)\n' + self.log
        self.assertEqual([], self.check())


if __name__ == '__main__':
    unittest.main()
