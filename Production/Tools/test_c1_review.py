"""C1 must retain failure gates while keeping accepted HG01/HG02 separate from HG03."""
import importlib
import importlib.util
import test_c_review

class C1OutcomeTests(test_c_review.COutcomeTests):
    def setUp(self):
        super().setUp()
        self.assertIsNotNone(importlib.util.find_spec('inspect_c1_review'), 'C1 evidence validator is absent')
        self.validate = importlib.import_module('inspect_c1_review').outcome_failures
        for row in self.run['captures']: row['file'] = row['file'].replace('C_', 'C1_', 1)
        self.log = 'C1_CAPTURE_FINISHED 12 files; errors 0'

    def test_previous_c_matrix_cannot_be_presented_as_c1(self):
        for row in self.run['captures']: row['file'] = row['file'].replace('C1_', 'C_', 1)
        self.assertTrue(self.check())
