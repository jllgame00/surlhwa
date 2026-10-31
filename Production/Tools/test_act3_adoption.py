"""ACT 3 source fidelity and additive composition; no rendered-quality claim."""
import hashlib
import importlib
import json
from pathlib import Path
import unittest

ROOT = Path(__file__).resolve().parents[1]
ACT1_HASH = '0d347e6976825ba61ab6a9fec39c73decc8f13ac10c5f0bf4d80558b1f33e3d4'
ACT2_HASH = 'e38fe3dce7cdcb004dd0731df730925c2531562379fda7fde95d922c0d620fa1'


class ActThreeAdoptionTests(unittest.TestCase):
    def test_bounded_package_and_exact_accepted_bytes(self):
        for name, expected in [('act1.json', ACT1_HASH), ('act2.json', ACT2_HASH)]:
            self.assertEqual(hashlib.sha256((ROOT / 'Content/Campaign' / name).read_bytes()).hexdigest(), expected)
        path = ROOT / 'Content/Campaign/act3-adoption.json'
        self.assertTrue(path.exists(), 'ACT 3 needs an independently reviewable source package')
        data = json.loads(path.read_bytes())
        self.assertEqual(data['acceptedAct2Sha256'], ACT2_HASH)
        self.assertEqual([(x['sourceRow'], x['night'], x['guestId']) for x in data['encounters']],
                         [('RC1-26', 10, 'guest_postman'), ('RC1-27', 10, 'guest_quixote'),
                          ('RC1-28', 11, 'guest_quixote'), ('RC1-29', 11, 'guest_snow_worker')])
        self.assertEqual([(x['id'], x['memoryId'], x['memoryVariant']) for x in data['encounters'] if x['memoryId']],
                         [('enc_rc1_28', 'mem_03', 'windmill_fragment')])
        self.assertTrue(all(x['requiredDiscovery'] is None and x['companionGuestId'] is None for x in data['encounters']))
        self.assertTrue(all(not line['facts'] for x in data['encounters'] for line in x['intro']))

    def test_all_37_source_records_keep_exact_bytes_field_order_and_roles(self):
        adoption = importlib.import_module('adopt_act3_content')
        data = adoption.build(adoption.SPEC)
        records = json.loads((adoption.SPEC / 'SURLHWA_CANON/_verification/wording_registry.json').read_bytes())['records']
        originals = {r['id']: r for r in records if any(name in r.get('source', '') for name in
                     ['Ordinary08', 'Ordinary09', 'QuixoteDeclaration', 'QuixoteAlone'])}
        self.assertEqual(len(originals), 37)
        self.assertEqual({r['sourceId'] for r in data['texts']}, set(originals))
        narration = {176, 184, 249, 250, 255, 260, 265}
        mixed = {177, 185}
        display = {175, 183, 247, 258}
        summary = {182, 190, 257, 267}
        for row in data['texts']:
            src = originals[row['sourceId']]
            self.assertEqual(row['value'].encode('utf-8'), src['text'].encode('utf-8'))
            self.assertEqual(row['sourceText'], src['text'])
            self.assertEqual(row['sourceHash'], hashlib.sha256(src['text'].encode('utf-8')).hexdigest())
            self.assertEqual(row['sourceField'], src['field'])
            self.assertEqual(row['sourceOrder'], str(src['source_order']))
            self.assertEqual(row['sourcePath'], src['primary_home'])
            number = int(row['sourceId'].split('-')[-1])
            role = 'narration' if number in narration else 'mixed' if number in mixed else 'display' if number in display else 'request_summary' if number in summary else 'spoken'
            self.assertEqual(row['role'], role)
        rows = {x['id']: x for x in data['encounters']}
        self.assertEqual([x['textId'] for x in rows['enc_rc1_27']['intro']], [f'text_rc1_{n:04d}' for n in range(263, 267)])
        self.assertEqual(rows['enc_rc1_28']['farewell'], [f'text_rc1_{n:04d}' for n in range(248, 251)])

    def test_composition_keeps_all_accepted_fields_and_no_new_gameplay_resources(self):
        adoption = importlib.import_module('adopt_act3_content')
        data = adoption.build(adoption.SPEC)
        candidate = adoption.compose(data)
        base = json.loads((ROOT / 'Content/Campaign/act2.json').read_bytes())
        for key, value in base.items():
            if key in ['act', 'contentRevision']:
                continue
            if key in ['texts', 'guests', 'encounters', 'slots']:
                self.assertEqual(candidate[key][:len(value)], value, key)
            else:
                self.assertEqual(candidate[key], value, key)
        self.assertEqual(candidate['act'], 'through_act_03')
        self.assertEqual(len(candidate['slots']), 29)
        self.assertEqual(json.loads((ROOT / 'Content/Campaign/act3-adoption.json').read_bytes()), data)
        self.assertEqual(json.loads((ROOT / 'Content/Campaign/act3.json').read_bytes()), candidate)


if __name__ == '__main__':
    unittest.main()
