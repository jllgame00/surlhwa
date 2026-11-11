import unittest
import numpy as np
import author_final_audio as audio


class FinalAudioTests(unittest.TestCase):
    def test_contacts_are_deterministic_bounded_and_have_finite_tails(self):
        for make in (audio.foot, lambda: audio.foot(True), audio.shaker):
            x=make(); self.assertTrue(np.array_equal(x,make()))
            self.assertTrue(np.isfinite(x).all()); self.assertLess(np.max(abs(x)),.98)
            self.assertGreater(np.count_nonzero(x),1000); self.assertLess(np.max(abs(x[-480:])),.0001)

    def test_arrangements_preserve_authored_space_and_are_distinct(self):
        full=audio.motif(); sparse=audio.motif(True)
        self.assertEqual(full.shape,(1272000,2)); self.assertFalse(np.array_equal(full,sparse))
        self.assertEqual(np.count_nonzero(full[:int(.75*audio.RATE)]),0)
        self.assertLess(np.max(abs(full[-audio.RATE//2:])),.0001)
        self.assertTrue(np.isfinite(full).all()); self.assertLess(np.max(abs(full)),.98)
        self.assertEqual([x[1] for x in audio.NOTES],[64,59,66,64,62])

if __name__ == '__main__': unittest.main()
