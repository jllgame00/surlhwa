"""ACT 4 source fidelity, additive authority and fair recipe disclosure."""
import hashlib
import importlib
import json
from pathlib import Path
import unittest

ROOT = Path(__file__).resolve().parents[1]
ACT3_HASH = '9db4998cd4f3dd8fc1c0371e8396504c07c0cce253e504caf39f48ec370555f0'


class ActFourAdoptionTests(unittest.TestCase):
    def adoption(self):
        self.assertTrue((ROOT / 'Tools/adopt_act4_content.py').exists(), 'ACT 4 source generator is missing')
        return importlib.import_module('adopt_act4_content')

    def test_all_40_records_preserve_source_bytes_roles_field_and_order(self):
        adoption = self.adoption()
        data = adoption.build(adoption.SPEC)
        originals = {r['id']: r for r in json.loads((adoption.SPEC / 'SURLHWA_CANON/_verification/wording_registry.json').read_bytes())['records']}
        numbers = list(range(30, 40)) + list(range(82, 104)) + list(range(191, 199))
        self.assertEqual([r['sourceId'] for r in data['texts']], [f'SRC-RC1-{n:04d}' for n in numbers])
        for row in data['texts']:
            source = originals[row['sourceId']]
            self.assertEqual(row['value'].encode('utf8'), source['text'].encode('utf8'))
            self.assertEqual(row['sourceText'], source['text'])
            self.assertEqual(row['sourceHash'], hashlib.sha256(source['text'].encode('utf8')).hexdigest())
            self.assertEqual(row['sourceField'], source['field'])
            self.assertEqual(row['sourceOrder'], str(source['source_order']))
            self.assertEqual(row['sourcePath'], source['primary_home'])
            n = int(row['sourceId'][-4:])
            role = 'narration' if n in {32, 83, 85, 89, 94, 96, 192} else 'mixed' if n == 193 else 'display' if n in {30, 82, 93, 191} else 'request_summary' if n in {38, 92, 103, 198} else 'spoken'
            self.assertEqual(row['role'], role)

    def test_compose_preserves_accepted_prefix_and_adds_only_bounded_resources(self):
        adoption = self.adoption()
        raw = (ROOT / 'Content/Campaign/act3.json').read_bytes()
        self.assertEqual(hashlib.sha256(raw).hexdigest(), ACT3_HASH)
        base = json.loads(raw)
        package = adoption.build(adoption.SPEC)
        target = adoption.compose(package)
        appended = {'ingredients', 'cocktails', 'recipes', 'guests', 'texts', 'encounters', 'slots', 'supply', 'supplyGrants'}
        for key, value in base.items():
            if key in {'act', 'contentRevision'}:
                continue
            self.assertEqual(target[key][:len(value)] if key in appended else target[key], value, key)
        self.assertEqual(target['act'], 'through_act_04')
        self.assertEqual(len(target['slots']), 33)
        self.assertEqual([(x['sourceRow'], x['night'], x['guestId']) for x in package['encounters'][:4]],
                         [('RC1-30', 12, 'guest_station_worker'), ('RC1-31', 12, 'guest_hamlet'),
                          ('RC1-32', 13, 'guest_repairer'), ('RC1-33', 13, 'guest_hamlet')])
        self.assertEqual(target['supply'][len(base['supply']):], ['fresh_lemon_juice', 'champagne'])
        self.assertEqual(target['supplyGrants'][-1], {'beforeEncounter': 'enc_rc1_30', 'ingredientIds': ['fresh_lemon_juice', 'champagne']})
        self.assertEqual(json.loads((ROOT / 'Content/Campaign/act4-adoption.json').read_bytes()), package)
        self.assertEqual(json.loads((ROOT / 'Content/Campaign/act4.json').read_bytes()), target)

    def test_discovery_facts_retry_memory_and_known_fallback_are_authored(self):
        adoption = self.adoption()
        data = adoption.build(adoption.SPEC)
        lesson, question, ordinary, stillness, fallback = data['encounters']
        self.assertEqual(lesson['requiredDiscovery'], 'recipe_french_75')
        self.assertEqual(lesson['discoveryRetryTextId'], 'text_rc1_0039')
        self.assertEqual([(x['textId'], [f['fact'] for f in x['facts']]) for x in lesson['intro']], [
            ('text_rc1_0035', ['identity']),
            ('text_rc1_0036', ['method', 'amount:gin', 'amount:fresh_lemon_juice', 'amount:simple_syrup']),
            ('text_rc1_0037', ['amount:champagne', 'preparation', 'garnish'])])
        for line in lesson['intro']:
            self.assertTrue(all(f['sourceIds'] == [line['textId'].replace('text_rc1_', 'SRC-RC1-')] for f in line['facts']))
        self.assertEqual([(e['id'], e['memoryId'], e['memoryVariant']) for e in data['encounters'] if e['memoryId']],
                         [('enc_rc1_33', 'mem_04', 'stage_skull_shadow')])
        accepted = next(e for e in adoption.accepted_base()['encounters'] if e['id'] == 'enc_rc1_14_known')
        self.assertEqual({k: v for k, v in fallback.items() if k not in {'id', 'sourceRow', 'night'}},
                         {k: v for k, v in accepted.items() if k not in {'id', 'sourceRow', 'night'}})
        self.assertEqual(data['slots'][0], {'encounterId': 'enc_rc1_30', 'fallbackId': 'enc_rc1_30_known', 'whenKnownRecipe': 'recipe_french_75'})
        recipe = data['recipes'][0]
        self.assertEqual(recipe['amounts'], [{'ingredientId': k, 'ml': v} for k, v in [('gin', 30), ('fresh_lemon_juice', 15), ('simple_syrup', 15), ('champagne', 60)]])
        self.assertEqual(recipe['preparation'], ['shaker_ice', 'closed_shake', 'strain_from_shaker', 'late_measured_pour', 'gentle_stir', 'no_serving_ice'])
        self.assertEqual(recipe['glassware'], 'flute')
        self.assertIsNone(recipe['requiredGarnish'])


if __name__ == '__main__':
    unittest.main()
