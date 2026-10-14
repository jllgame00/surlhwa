"""Exercise the exact observation function without importing any desktop-input code."""
import ast,datetime as dt,json,unittest
from pathlib import Path

class ObservationGuardTests(unittest.TestCase):
    def setUp(self):
        source=Path(__file__).with_name('run_act1_native.py').read_text(encoding='utf-8')
        tree=ast.parse(source); functions=[n for n in tree.body if isinstance(n,ast.FunctionDef) and n.name in ('observe','wait_for')]
        self.observation=dict(observedUtc=dt.datetime.now(dt.timezone.utc).isoformat(),processId=42,nativeInterrupted=False,saving=False)
        self.g=dict(json=json,dt=dt,OUT=Path('.'),pid=42,last_observed=None,last_action_utc=None,log=lambda *a,**k:None,shared_read=lambda p:json.dumps(self.observation))
        exec(compile(ast.Module(body=functions,type_ignores=[]),'production-observer','exec'),self.g)
    def test_fresh_expected_player_is_accepted(self): self.assertEqual(self.g['observe']()['processId'],42)
    def test_other_player_is_rejected(self):
        self.observation['processId']=43
        with self.assertRaises(RuntimeError): self.g['observe']()
    def test_stale_publication_is_rejected(self):
        self.observation['observedUtc']=(dt.datetime.now(dt.timezone.utc)-dt.timedelta(seconds=2)).isoformat()
        with self.assertRaises(RuntimeError): self.g['observe']()
    def test_backwards_publication_is_rejected(self):
        self.g['observe'](); self.observation['observedUtc']=(dt.datetime.now(dt.timezone.utc)-dt.timedelta(seconds=.1)).isoformat()
        with self.assertRaises(RuntimeError): self.g['observe']()
    def test_future_publication_is_rejected(self):
        self.observation['observedUtc']=(dt.datetime.now(dt.timezone.utc)+dt.timedelta(seconds=2)).isoformat()
        with self.assertRaises(RuntimeError): self.g['observe']()
    def test_post_action_boundary_cannot_accept_pre_action_observation(self):
        # A bounded fake clock avoids any sleep/input and retains the actual wait_for predicate.
        class Clock:
            now=0
            def perf_counter(self): self.now+=.01;return self.now
            def sleep(self,n): pass
        self.g.update(time=Clock(),guard=lambda:None,last_action_utc=dt.datetime.now(dt.timezone.utc)+dt.timedelta(seconds=.1))
        with self.assertRaises(RuntimeError): self.g['wait_for'](lambda o:True,'post-action result',timeout=.05)

if __name__=='__main__': unittest.main()
