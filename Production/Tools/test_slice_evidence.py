"""Focused fail-closed tests for the new Player evidence validator."""
import copy
import unittest
from unittest.mock import patch
import verify_slice_evidence as evidence

class EvidenceRejectionTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.folder=evidence.PRODUCTION/'Validation/Evidence/hg04-close-dry-1080'
        cls.current=evidence.read(cls.folder/'inventory.json')
        evidence.validate(cls.folder,cls.current) # Positive baseline isolates mutations from later build freshness.

    def test_unmodified_baseline_passes(self): evidence.validate(self.folder,self.current)

    def reject(self,name,edit):
        original=evidence.read
        def read(path):
            result=original(path)
            if path.name==name:
                edit(result)
            return result
        with patch.object(evidence,'read',side_effect=read),self.assertRaises(AssertionError):
            evidence.validate(self.folder,self.current)

    def test_nonzero_process_is_rejected(self): self.reject('process.json',lambda x:x.update(exitCode=1))
    def test_false_manifest_is_rejected(self): self.reject('run.json',lambda x:x.update(passed=False))
    def test_wrong_run_is_rejected(self): self.reject('run.json',lambda x:x.update(runId='foreign'))
    def test_runtime_errors_are_rejected(self): self.reject('run.json',lambda x:x.update(errors=['error']))
    def test_stale_full_inventory_is_rejected(self): self.reject('inventory.json',lambda x:x['player'].update(sha256='stale'))
    def test_wrong_resolution_is_rejected(self): self.reject('run.json',lambda x:x.update(width=640))
    def test_unearned_memory_is_rejected(self): self.reject('01-menu.json',lambda x:x.update(memories=1))
    def test_overflow_is_rejected(self): self.reject('01-menu.json',lambda x:x.update(overflowText=['clipped']))
    def test_summary_speaker_misattribution_is_rejected(self): self.reject('11-request-1.json',lambda x:x.update(displayedSpeaker='어린왕자'))
    def test_duplicate_input_owner_is_rejected(self): self.reject('01-menu.json',lambda x:x.update(eventSystems=2))

if __name__=='__main__': unittest.main(verbosity=2)
