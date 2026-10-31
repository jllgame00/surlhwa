"""Reject a passing report that did not use the claimed evidence/save root."""
import unittest
import tempfile
from pathlib import Path
import package_gate2_accepted as package


class RunBindingTests(unittest.TestCase):
    def setUp(self):
        self.folder = Path('review-evidence').resolve()
        self.save = Path('completed-save-copy').resolve()
        self.expected = dict(fixture='quiet', arguments=[
            '-campaignProof', 'quiet', '-campaignEvidenceRoot', str(self.folder),
            '-campaignSaveRoot', str(self.save)])

    def test_valid_run_and_open_default_save(self):
        package.verify_run_arguments(self.expected, self.folder, 'quiet', self.save)
        package.verify_run_arguments(dict(fixture='open', arguments=[
            '-campaignProof', 'open', '-campaignEvidenceRoot', str(self.folder)]),
            self.folder, 'open', self.folder)

    def test_wrong_fixture_or_save_or_evidence_is_rejected(self):
        for index, value in [(1, 'open'), (3, str(self.save)), (5, str(self.folder))]:
            with self.subTest(index=index):
                expected = dict(self.expected, arguments=list(self.expected['arguments']))
                expected['arguments'][index] = value
                with self.assertRaises(ValueError):
                    package.verify_run_arguments(expected, self.folder, 'quiet', self.save)

    def test_missing_save_override_cannot_certify_untouched_copy(self):
        expected = dict(self.expected, arguments=self.expected['arguments'][:4])
        with self.assertRaises(ValueError):
            package.verify_run_arguments(expected, self.folder, 'quiet', self.save)


class InventoryTests(unittest.TestCase):
    def test_new_file_invalidates_exact_inventory(self):
        with tempfile.TemporaryDirectory() as folder:
            root = Path(folder)
            (root / 'Assets').mkdir()
            source = root / 'Assets/accepted.txt'
            source.write_text('accepted')
            inventory = {'Assets/accepted.txt': package.sha(source)}
            package.verify_inventory(root, ['Assets'], inventory)
            (root / 'Assets/untested.txt').write_text('new')
            with self.assertRaises(ValueError):
                package.verify_inventory(root, ['Assets'], inventory)

    def test_changed_bytes_invalidate_exact_inventory(self):
        with tempfile.TemporaryDirectory() as folder:
            root = Path(folder)
            source = root / 'accepted.txt'
            source.write_text('accepted')
            inventory = {'accepted.txt': package.sha(source)}
            source.write_text('changed')
            with self.assertRaises(ValueError):
                package.verify_inventory(root, ['.'], inventory)


if __name__ == '__main__':
    unittest.main()
