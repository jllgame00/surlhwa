"""ACT 5 source adoption preserves accepted history and adds no gameplay resources."""
import hashlib
import importlib
import json
from pathlib import Path
import unittest

ROOT = Path(__file__).resolve().parents[1]
ACT4_HASH = 'fcc2aeb9c4269c2c9fd34018e4c82b79553ffc3645ad2b8be3970abd673ad6fe'


class ActFiveAdoptionTests(unittest.TestCase):
    def adoption(self):
        self.assertTrue((ROOT / 'Tools/adopt_act5_content.py').exists(), 'ACT 5 needs its source generator')
        return importlib.import_module('adopt_act5_content')

    def test_all_38_records_preserve_exact_bytes_fields_order_and_roles(self):
        adoption = self.adoption()
        data = adoption.build(adoption.SPEC)
        records = {r['id']: r for r in json.loads((adoption.SPEC / 'SURLHWA_CANON/_verification/wording_registry.json').read_bytes())['records']}
        numbers = list(range(60, 82)) + list(range(199, 215))
        self.assertEqual([r['sourceId'] for r in data['texts']], [f'SRC-RC1-{n:04d}' for n in numbers])
        for row in data['texts']:
            original = records[row['sourceId']]
            self.assertEqual(row['value'].encode('utf8'), original['text'].encode('utf8'))
            self.assertEqual(row['sourceText'], original['text'])
            self.assertEqual(row['sourceHash'], hashlib.sha256(original['text'].encode('utf8')).hexdigest())
            self.assertEqual(row['sourceField'], original['field'])
            self.assertEqual(row['sourceOrder'], str(original['source_order']))
            self.assertEqual(row['sourcePath'], original['primary_home'])
            n = int(row['sourceId'][-4:])
            expected = 'narration' if n in {63, 68, 73, 74, 200, 208} else 'mixed' if n in {201, 209} else 'display' if n in {60, 71, 199, 207} else 'request_summary' if n in {70, 81, 206, 214} else 'spoken'
            self.assertEqual(row['role'], expected)

    def test_four_source_rows_have_authored_readings_and_only_natural_final_memory(self):
        adoption = self.adoption()
        data = adoption.build(adoption.SPEC)
        self.assertEqual([(e['sourceRow'], e['night'], e['guestId']) for e in data['encounters']], [
            ('RC1-34', 14, 'guest_faust'), ('RC1-35', 14, 'guest_station_worker'),
            ('RC1-36', 15, 'guest_postman'), ('RC1-37', 15, 'guest_faust')])
        expected = [([66, 67, 68, 69], [61, 62, 63], 64, 65, 70, ['clarity', 'awakening']),
                    ([204, 205], [200, 201], 202, 203, 206, ['awakening', 'clarity']),
                    ([212, 213], [208, 209], 210, 211, 214, ['warmth']),
                    ([77, 78, 79, 80], [72, 73, 74], 75, 76, 81, ['bitterness', 'warmth'])]
        for row, (intro, farewell, good, other, summary, tags) in zip(data['encounters'], expected):
            self.assertEqual([l['textId'] for l in row['intro']], [f'text_rc1_{n:04d}' for n in intro])
            self.assertEqual(row['farewell'], [f'text_rc1_{n:04d}' for n in farewell])
            self.assertEqual(row['responses'], {k: f'text_rc1_{good if k == "good" else other:04d}' for k in ['good', 'weak', 'mismatch', 'bothWeak', 'unknown']})
            self.assertEqual(row['summaryTextId'], f'text_rc1_{summary:04d}')
            self.assertEqual(row['request'], dict(cocktailId=None, anyTags=tags, allTags=[], excludedTags=[]))
            self.assertIsNone(row['requiredDiscovery'])
            self.assertIsNone(row['companionGuestId'])
            self.assertTrue(all(not l['facts'] for l in row['intro']))
        self.assertEqual([(e['id'], e['memoryId'], e['memoryVariant']) for e in data['encounters'] if e['memoryId']],
                         [('enc_rc1_37', 'mem_05', 'faint_circle_broken_glass_red_light')])

    def test_composition_retains_every_accepted_definition_and_33_slot_prefix(self):
        adoption = self.adoption()
        raw = (ROOT / 'Content/Campaign/act4.json').read_bytes()
        self.assertEqual(hashlib.sha256(raw).hexdigest(), ACT4_HASH)
        base = json.loads(raw)
        package = adoption.build(adoption.SPEC)
        target = adoption.compose(package)
        for key, value in base.items():
            if key in {'act', 'contentRevision'}:
                continue
            self.assertEqual(target[key][:len(value)] if key in {'texts', 'guests', 'encounters', 'slots'} else target[key], value, key)
        self.assertEqual(target['act'], 'through_act_05')
        self.assertEqual(target['contentRevision'], 'campaign_through_act5_v1')
        self.assertEqual(len(target['slots']), 37)
        self.assertEqual(len(target['recipes']), 8)
        self.assertEqual(len(target['ingredients']), 19)
        self.assertEqual(json.loads((ROOT / 'Content/Campaign/act5-adoption.json').read_bytes()), package)
        self.assertEqual(json.loads((ROOT / 'Content/Campaign/act5.json').read_bytes()), target)


if __name__ == '__main__':
    unittest.main()
