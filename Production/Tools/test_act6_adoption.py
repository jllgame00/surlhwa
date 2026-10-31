"""ACT6 preserves accepted history and source truth while adding a bounded lesson."""
import hashlib
import importlib
import json
from pathlib import Path
import unittest

ROOT = Path(__file__).resolve().parents[1]
ACT5_HASH = 'bc76a5546c7f30d6dfa417fd3754a81fe4c52f8040b15bc86b42c1a954f34339'


class ActSixAdoptionTests(unittest.TestCase):
    def adoption(self):
        self.assertTrue((ROOT / 'Tools/adopt_act6_content.py').exists(), 'ACT6 needs its source generator')
        return importlib.import_module('adopt_act6_content')

    def test_all_59_source_records_preserve_exact_bytes_fields_order_and_roles(self):
        adoption = self.adoption()
        data = adoption.build(adoption.SPEC)
        records = {r['id']: r for r in json.loads((adoption.SPEC / 'SURLHWA_CANON/_verification/wording_registry.json').read_bytes())['records']}
        numbers = list(range(50, 60)) + list(range(215, 231)) + list(range(268, 301))
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
            role = 'narration' if n in {52, 216, 224, 271, 280, 282, 293, 297} else 'mixed' if n in {217, 225} else 'display' if n in {50, 215, 223, 268, 279, 290} else 'request_summary' if n in {58, 222, 230, 278, 289, 300} else 'spoken'
            self.assertEqual(row['role'], role)

    def test_six_rows_use_exact_readings_and_memory_precedes_final_ordinary_guest(self):
        adoption = self.adoption()
        data = adoption.build(adoption.SPEC)
        expected = [([220, 221], [216, 217], 218, 219, 222, ['bitterness', 'warmth']),
                    ([274, 275, 276, 277], [269, 270, 271], 272, 273, 278, ['warmth', 'clarity']),
                    ([285, 286, 287, 288], [280, 281, 282], 283, 284, 289, ['warmth', 'bitterness']),
                    ([55, 56, 57], [51, 52], 53, 54, 58, []),
                    ([296, 297, 298, 299], [291, 292, 293], 294, 295, 300, ['warmth', 'clarity']),
                    ([228, 229], [224, 225], 226, 227, 230, ['clarity'])]
        self.assertEqual([(e['sourceRow'], e['night'], e['guestId']) for e in data['encounters'][:6]], [
            ('RC1-38', 16, 'guest_repairer'), ('RC1-39', 16, 'guest_simcheong'), ('RC1-40', 17, 'guest_simcheong'),
            ('RC1-41', 17, 'guest_snow_worker'), ('RC1-42', 18, 'guest_simcheong'), ('RC1-43', 18, 'guest_snow_worker')])
        for row, (intro, farewell, good, other, summary, tags) in zip(data['encounters'], expected):
            self.assertEqual([l['textId'] for l in row['intro']], [f'text_rc1_{n:04d}' for n in intro])
            self.assertEqual(row['farewell'], [f'text_rc1_{n:04d}' for n in farewell])
            self.assertEqual(row['responses'], {k: f'text_rc1_{good if k == "good" else other:04d}' for k in ['good', 'weak', 'mismatch', 'bothWeak', 'unknown']})
            self.assertEqual(row['summaryTextId'], f'text_rc1_{summary:04d}')
            self.assertEqual(row['request']['anyTags'], tags)
        self.assertEqual([(e['id'], e['memoryId'], e['memoryVariant']) for e in data['encounters'] if e['memoryId']],
                         [('enc_rc1_42', 'mem_06', 'half_submerged_lotus_slow_ripples')])
        self.assertIsNone(data['encounters'][5]['memoryId'])

    def test_lesson_discloses_real_recipe_without_seeding_known_and_supply_starts_one_visit_before(self):
        adoption = self.adoption()
        data = adoption.build(adoption.SPEC)
        recipe = data['recipes'][0]
        self.assertEqual(recipe['id'], 'recipe_last_word')
        self.assertEqual(recipe['amounts'], [dict(ingredientId=i, ml=22.5) for i in ['gin', 'green_chartreuse', 'maraschino_luxardo', 'fresh_lime_juice']])
        self.assertEqual(recipe['preparation'], ['shaker_ice', 'closed_shake', 'strain_from_shaker', 'no_serving_ice'])
        self.assertIsNone(recipe['lateIngredient'])
        self.assertIsNone(recipe['requiredGarnish'])
        self.assertEqual(data['supplyGrants'], [dict(beforeEncounter='enc_rc1_40', ingredientIds=['green_chartreuse', 'maraschino_luxardo', 'fresh_lime_juice'])])
        lesson = data['encounters'][3]
        self.assertEqual(lesson['requiredDiscovery'], 'recipe_last_word')
        self.assertEqual(lesson['request'], dict(cocktailId='cocktail_last_word', anyTags=[], allTags=[], excludedTags=[]))
        self.assertEqual(lesson['discoveryRetryTextId'], 'text_rc1_0059')
        self.assertEqual([[f['fact'] for f in l['facts']] for l in lesson['intro']], [['identity'],
            ['amount:gin', 'amount:green_chartreuse', 'amount:maraschino_luxardo', 'amount:fresh_lime_juice'], ['method', 'preparation', 'garnish']])
        for line in lesson['intro']:
            for fact in line['facts']:
                self.assertEqual(fact['recipeId'], 'recipe_last_word')
                self.assertEqual(fact['sourceIds'], [line['textId'].replace('text_rc1_', 'SRC-RC1-')])
        self.assertEqual(data['slots'][3], dict(encounterId='enc_rc1_41', fallbackId='enc_rc1_41_known', whenKnownRecipe='recipe_last_word'))
        fallback = next(e for e in adoption.accepted_base()['encounters'] if e['id'] == 'enc_rc1_29')
        fallback.update(id='enc_rc1_41_known', sourceRow='RC1-41', night=17)
        self.assertEqual(data['encounters'][-1], fallback)

    def test_composition_preserves_every_accepted_definition_and_37_slot_prefix(self):
        adoption = self.adoption()
        raw = (ROOT / 'Content/Campaign/act5.json').read_bytes()
        self.assertEqual(hashlib.sha256(raw).hexdigest(), ACT5_HASH)
        base = json.loads(raw)
        package = adoption.build(adoption.SPEC)
        target = adoption.compose(package)
        appended = {'texts', 'guests', 'encounters', 'slots', 'ingredients', 'cocktails', 'recipes', 'supply', 'supplyGrants'}
        for key, value in base.items():
            if key in {'act', 'contentRevision'}: continue
            self.assertEqual(target[key][:len(value)] if key in appended else target[key], value, key)
        self.assertEqual(target['act'], 'through_act_06')
        self.assertEqual(target['contentRevision'], 'campaign_through_act6_v1')
        self.assertEqual((len(target['slots']), len(target['recipes']), len(target['ingredients'])), (43, 9, 22))
        self.assertEqual(json.loads((ROOT / 'Content/Campaign/act6-adoption.json').read_bytes()), package)
        self.assertEqual(json.loads((ROOT / 'Content/Campaign/act6.json').read_bytes()), target)


if __name__ == '__main__': unittest.main()
