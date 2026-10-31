"""Source-package tests; these do not claim runtime reachability or art approval."""
import hashlib
import json
import unittest
import adopt_act2_content as adoption


class ActTwoAdoptionTests(unittest.TestCase):
    def setUp(self):
        self.base_path = adoption.ROOT / 'Content/Campaign/act1.json'
        self.base_bytes = self.base_path.read_bytes()
        self.data = adoption.build(adoption.SPEC)

    def test_exact_eight_source_events_preserve_accepted_act_one(self):
        self.assertEqual(self.base_path.read_bytes(), self.base_bytes)
        self.assertEqual(self.data['acceptedAct1Sha256'], hashlib.sha256(self.base_bytes).hexdigest())
        self.assertEqual([x['sourceRow'] for x in self.data['encounters'][:8]],
                         [f'RC1-{n}' for n in range(18, 26)])
        self.assertEqual(len(self.data['slots']), 8)

    def test_two_paired_visits_keep_one_request_and_only_reunion_memory(self):
        paired = [x for x in self.data['encounters'] if x['companionGuestId']]
        self.assertEqual([x['id'] for x in paired], ['enc_rc1_21', 'enc_rc1_25'])
        self.assertTrue(all(len({x['guestId'], x['companionGuestId']}) == 2 for x in paired))
        grants = [x for x in self.data['encounters'] if x['memoryId']]
        self.assertEqual([(x['id'], x['memoryId'], x['memoryVariant']) for x in grants],
                         [('enc_rc1_25', 'mem_02', 'swing_empty_place')])

    def test_hanky_lesson_grants_only_consumed_facts_and_exact_ingredient_identity(self):
        recipe = self.data['recipes'][0]
        self.assertEqual({x['ingredientId']: x['ml'] for x in recipe['amounts']},
                         {'london_dry_gin': 45, 'sweet_red_vermouth': 45, 'fernet': 7.5})
        self.assertEqual(recipe['requiredGarnish'], 'orange_zest')
        self.assertEqual(recipe['garnishPosition'], 'rim')
        lesson = next(x for x in self.data['encounters'] if x['id'] == 'enc_rc1_22')
        self.assertEqual(lesson['requiredDiscovery'], 'recipe_hanky_panky')
        self.assertEqual([f['fact'] for f in lesson['intro'][0]['facts']], ['identity'])
        self.assertEqual([f['fact'] for f in lesson['intro'][2]['facts']], ['method', 'preparation', 'garnish'])
        self.assertEqual(self.data['supplyGrants'], [{'beforeEncounter': 'enc_rc1_22',
                          'ingredientIds': ['london_dry_gin', 'fernet']}])
        self.assertNotIn('known', self.data)

    def test_known_fallback_preserves_the_repairer_visit_identity(self):
        fallback = next(x for x in self.data['encounters'] if x['id'] == 'enc_rc1_22_known')
        self.assertEqual(fallback['guestId'], 'guest_repairer')
        self.assertEqual(fallback['sourceRow'], 'RC1-22')
        self.assertIsNone(fallback['requiredDiscovery'])
        self.assertEqual(fallback['request']['anyTags'], ['bitterness'])

    def test_registry_fields_order_roles_and_hashes_are_preserved(self):
        registry = json.loads((adoption.SPEC / 'SURLHWA_CANON/_verification/wording_registry.json').read_text(encoding='utf-8'))
        source = {x['id']: x for x in registry['records']}
        accepted = {x['id']: x for x in json.loads(self.base_bytes)['texts']}
        for row in self.data['texts']:
            if row['sourceId'].startswith('PRODUCTION-'):
                self.assertEqual(row, accepted[row['id']])
                continue
            original = source[row['sourceId']]
            self.assertEqual(row['value'], original['text'])
            self.assertEqual(row['sourceHash'], hashlib.sha256(original['text'].encode()).hexdigest())
            self.assertEqual(row['sourceField'], original['field'])
            self.assertEqual(row['sourceOrder'], str(original['source_order']))
        silence = next(x for x in self.data['encounters'] if x['id'] == 'enc_rc1_23')
        texts = {x['id']: x for x in self.data['texts']}
        self.assertEqual([texts[x['textId']]['role'] for x in silence['intro']],
                         ['narration', 'narration', 'narration', 'spoken'])

    def test_checked_in_package_matches_deterministic_adoption(self):
        path = adoption.ROOT / 'Content/Campaign/act2-adoption.json'
        self.assertEqual(json.loads(path.read_text(encoding='utf-8')), self.data)
        candidate = adoption.ROOT / 'Content/Campaign/act2.json'
        self.assertEqual(json.loads(candidate.read_text(encoding='utf-8')), adoption.compose(self.data))


if __name__ == '__main__':
    unittest.main()
