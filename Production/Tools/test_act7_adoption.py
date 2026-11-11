"""Exact ACT7 source adoption and a bounded, history-owned ending callback."""
import hashlib
import importlib
import json
from pathlib import Path
import unittest

ROOT = Path(__file__).resolve().parents[1]
ACT6_HASH = 'e6bb7131fc5424186bc2cf4b1b15160b8cead275bcf7c9824ee1ccd1bfb5dfcf'


class ActSevenAdoptionTests(unittest.TestCase):
    def adoption(self):
        self.assertTrue((ROOT/'Tools/adopt_act7_content.py').exists(), 'ACT7 source generator is absent')
        return importlib.import_module('adopt_act7_content')

    def test_exact_47_records_preserve_source_bytes_ownership_order_and_roles(self):
        adoption=self.adoption(); data=adoption.build(adoption.SPEC)
        records={r['id']:r for r in json.loads((adoption.SPEC/'SURLHWA_CANON/_verification/wording_registry.json').read_bytes())['records']}
        numbers=list(range(136,159))+list(range(231,247))+list(range(1,9))
        self.assertEqual([r['sourceId'] for r in data['texts']],[f'SRC-RC1-{n:04}' for n in numbers])
        for row in data['texts']:
            source=records[row['sourceId']]; n=int(row['sourceId'][-4:])
            for target,original in [('value','text'),('sourceText','text'),('sourceHash','sha256_utf8'),('sourcePath','primary_home'),('sourceField','field')]:
                self.assertEqual(row[target],source[original],row['id'])
            self.assertEqual(row['sourceHash'],hashlib.sha256(row['value'].encode('utf8')).hexdigest())
            self.assertEqual(row['sourceOrder'],str(source['source_order']))
            role='narration' if n in set(range(1,9))|{137,139,142,148,151,154,156,232,240} else 'mixed' if n in {233,241} else 'display' if n in {136,147,231,239} else 'request_summary' if n in {146,158,238,246} else 'spoken'
            self.assertEqual(row['role'],role)

    def test_four_authored_visits_and_last_departure_memory(self):
        adoption=self.adoption(); data=adoption.build(adoption.SPEC)
        expected=[('guest_station_worker',[236,237],[232,233],234,235,238,['warmth','bitterness']),
                  ('guest_odysseus',[142,143,144,145],[137,138,139],140,141,146,['warmth','bitterness']),
                  ('guest_postman',[244,245],[240,241],242,243,246,['warmth','clarity']),
                  ('guest_odysseus',[154,155,156,157],[148,149,150,151],152,153,158,['warmth','clarity'])]
        self.assertEqual(len(data['encounters']),4)
        for i,(row,(guest,intro,farewell,good,other,summary,tags)) in enumerate(zip(data['encounters'],expected)):
            self.assertEqual((row['id'],row['sourceRow'],row['night'],row['guestId']),(f'enc_rc1_{44+i}',f'RC1-{44+i}',19+i//2,guest))
            self.assertEqual(row['intro'],[dict(textId=f'text_rc1_{n:04}',facts=[]) for n in intro])
            self.assertEqual(row['farewell'],[f'text_rc1_{n:04}' for n in farewell])
            self.assertEqual(row['responses'],{key:f'text_rc1_{good if key=="good" else other:04}' for key in ['good','weak','mismatch','bothWeak','unknown']})
            self.assertEqual(row['summaryTextId'],f'text_rc1_{summary:04}')
            self.assertEqual(row['request'],dict(cocktailId=None,anyTags=tags,allTags=[],excludedTags=[]))
            self.assertIsNone(row['requiredDiscovery']);self.assertIsNone(row['princeVisit']);self.assertEqual(row['choiceIntros'],{})
        self.assertEqual([(e['id'],e['memoryId'],e['memoryVariant']) for e in data['encounters'] if e['memoryId']],[('enc_rc1_47','mem_07','anchor_ship_fragment')])
        self.assertTrue(all(s['fallbackId'] is None and s['whenKnownRecipe'] is None for s in data['slots']))

    def test_ending_uses_eight_common_lines_and_existing_three_recalls_without_new_history(self):
        adoption=self.adoption(); data=adoption.build(adoption.SPEC); base=adoption.accepted_base()
        self.assertEqual(data['ending'],dict(lines=[f'text_rc1_{n:04}' for n in range(1,9)],recallAfterLine=1,recallByChoice=base['prince']['recalls'],codaTextId='text_rc1_0008'))
        self.assertNotIn('known',data)
        self.assertTrue(any(d['sourceId']=='PRODUCTION-ENDING-PRINCE-RECALL' and 'new placement' in d['disposition'] for d in data['sourceDispositions']))
        self.assertTrue(all(r['sourceId'] not in {'SRC-RC1-0412','SRC-RC1-0413','SRC-RC1-0414'} for r in data['texts']))

    def test_composition_preserves_all_accepted_resources_and_43_slot_prefix(self):
        adoption=self.adoption(); raw=(ROOT/'Content/Campaign/act6.json').read_bytes()
        self.assertEqual(hashlib.sha256(raw).hexdigest(),ACT6_HASH)
        base=json.loads(raw);package=adoption.build(adoption.SPEC);target=adoption.compose(package)
        for key,value in base.items():
            if key in {'act','contentRevision'}:continue
            self.assertEqual(target[key][:len(value)] if key in {'texts','guests','encounters','slots'} else target[key],value,key)
        self.assertEqual((target['act'],target['contentRevision']),('through_act_07','campaign_through_act7_v1'))
        self.assertEqual((len(target['slots']),len(target['recipes']),len(target['supply'])),(47,9,22))
        self.assertEqual(json.loads((ROOT/'Content/Campaign/act7-adoption.json').read_bytes()),package)
        self.assertEqual(json.loads((ROOT/'Content/Campaign/act7.json').read_bytes()),target)


if __name__=='__main__':unittest.main()
