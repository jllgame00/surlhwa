"""Verifier-only candidates, never fabricated Player saves."""
import unittest
import copy
from test_act7_proof import act7_records, CONTENT
import verify_campaign_proof as proof

RECIPES=('recipe_test_stir','recipe_manhattan','recipe_dry_martini','recipe_negroni','recipe_gin_fizz','recipe_manwol','recipe_hanky_panky','recipe_french_75','recipe_last_word')
MATRIX=[f'p09-{kind}-{setting}' for setting in range(4) for kind in ('settings','guide','coda')]+[f'p09-known-{setting}-{recipe}' for setting in range(4) for recipe in RECIPES]


def empty_card(method):
    return ['이름을 기다리는 한 잔','대화 속에서 들은 단서    ·    '+method,'재료와 양','아직 적어 둔 재료가 없습니다.','준비','준비 순서는 아직 듣지 못했습니다.']


def partial_card(recipe):
    french=recipe=='recipe_french_75'
    return ['프렌치 75' if french else '라스트 워드','대화 속에서 들은 단서    ·    Shake','재료와 양',
            '진','30 ml' if french else '22.5 ml','프레시 레몬 주스' if french else '그린 샤르트뢰즈','15 ml' if french else '22.5 ml',
            '심플 시럽' if french else '마라스키노 룩사르도','15 ml' if french else '22.5 ml',
            '샴페인' if french else '프레시 라임 주스','60 ml' if french else '22.5 ml','준비',
            '샴페인 제외 · 나머지 재료와 얼음을 셰이커에\n뚜껑을 닫고 흔들기 · 열어서 U로 거르기\n샴페인 · 마지막에 양을 재어 따르고 G로 가볍게 젓기' if french else
            '재료와 얼음을 셰이커에\n뚜껑을 닫고 흔들기 · 열어서 U로 거르기','마무리 · 서빙 얼음 없음\n가니시 없음']


def preference_capture(label,setting,**fields):
    return dict(fields,label=label+'-'+str(setting),largeText=bool(setting&1),highContrast=bool(setting&2))


def known_card(recipe_id):
    """Expected UI wording with identities and technical values from shipped content."""
    recipe=next(r for r in CONTENT['recipes'] if r['id']==recipe_id)
    names={i['id']:i['label'] for i in CONTENT['ingredients']}
    title=next(c['name'] for c in CONTENT['cocktails'] if c['id']==recipe['cocktailId'])
    card=[title,'만들어 본 기록    ·    '+recipe['method'].capitalize(),'재료와 양']
    for amount in recipe['amounts']:
        ml=format(amount['ml'],'.2f').rstrip('0').rstrip('.')
        card += [names[amount['ingredientId']],ml+' ml'+(' · Dash' if amount['ingredientId']==recipe['dashIngredient'] else '')]
    preparation={'stir':'혼합 잔에 얼음 · 재료를 담은 뒤 원을 그리며 젓기\n서빙 잔에 거르기',
                 'build':'서빙 잔에 얼음과 재료 · 가볍게 젓기'}
    if recipe['method']=='float':
        prep=' → '.join(names[i] for i in recipe['layerOrder'])+'\n각 재료를 낮게, 천천히 층으로 쌓기'
    elif recipe['method']=='shake':
        late=recipe['lateIngredient']
        prep='재료와 얼음을 셰이커에\n뚜껑을 닫고 흔들기 · '
        if late:
            prep=names[late]+' 제외 · 나머지 '+prep
            prep+=('열어서 U로 거르기\n'+names[late]+' · 마지막에 양을 재어 따르고 G로 가볍게 젓기' if 'late_measured_pour' in recipe['preparation'] else '열어 잔에 옮기기\n'+names[late]+' · 마지막에 Splash')
        else:prep+='열어서 U로 거르기'
    else:prep=preparation[recipe['method']]
    glass={'old_fashioned':'올드 패션드 잔','cocktail':'칵테일 잔','tumbler':'텀블러 잔','flute':'플루트 잔'}[recipe['glassware']]
    garnish={'cherry':'체리','orange_slice':'오렌지 슬라이스','orange_zest':'오렌지 제스트'}.get(recipe['requiredGarnish'])
    garnish=(garnish+' · '+('잔 안' if recipe['garnishPosition']=='in_glass' else '잔 가장자리')) if garnish else ('가니시 생략 가능' if recipe['optionalGarnish'] else '가니시 없음')
    return card+['준비',prep,'마무리 · '+glass+' · '+('서빙 얼음' if recipe['method']=='build' else '서빙 얼음 없음')+'\n'+garnish]


def final_records(fresh=False,visits=()):
    c=act7_records('Complete');c['report'].update(finalCandidate=True,scene='Bar_Q1_Final',fixture='continue')
    final=c['captures'][-1]
    c['captures'] += [dict(final,label=label,image=label+'.png',largeText=bool(int(label.split('-')[2])&1),highContrast=bool(int(label.split('-')[2])&2)) for label in MATRIX]
    for capture in c['captures']:
        if capture['label'].startswith('p09-known-'):
            capture['recipeCardText']=known_card(capture['label'].split('-',3)[3])
    if fresh:
        c['report'].update(fixture='open',startSlot=0,startDepartures=0,startPhase='Welcome',visitedSlots=[0])
        c['checkpoints'] += [dict(name=f'after-departure-{slot}',currentHash='a'*64) for slot in range(17,47)]
        c['captures'] += [dict(label=f'act7-{phase}-{slot}',slot=slot,phase=phase,textId='authored',odysseusVisible=False) for slot in range(17,47) for phase in ('Intro','Farewell')]
        c['captures'] += [dict(label='act3-earned-memory-after-27',slot=28,phase='NightEnd',departures=28,memories=3,windmillVisible=True),
                          dict(label='act6-earned-memory-after-41',slot=42,phase='NightEnd',departures=42,memories=6,services=42,simCheongVisible=True),
                          dict(label='p09-method-only-clue',slot=0,phase='Request',recipeCardText=empty_card('Stir'))]
        c['captures'] += [preference_capture(label,setting,slot=0,phase=phase,recipeCardText=empty_card(method))
                          for label,phase,method in (('p09-unknown-detail','Welcome','제조법 미상'),('p09-method-only-clue','Request','Stir')) for setting in range(4)]
    for slot in visits:
        c['report']['visitedSlots'].append(slot)
        for stage in ('before-service','after-service','after-departure'):
            name=f'{stage}-{slot}'
            if not any(x['name']==name for x in c['checkpoints']):c['checkpoints'].append(dict(name=name,currentHash='a'*64))
        french=slot==29;recipe='recipe_french_75' if french else 'recipe_last_word';base='codex-french-clues' if french else 'codex-last-word-clues'
        c['captures'].append(dict(label=base,slot=slot,phase='Brewing',recipeCardText=partial_card(recipe)))
        c['captures'] += [preference_capture('p09-french-partial' if french else 'p09-last-word-partial',setting,slot=slot,phase='Brewing',recipeCardText=partial_card(recipe)) for setting in range(4)]
        if slot==40:
            c['report'].update(suppliedIngredients=['gin'],shelfSelections=['gin'])
            c['captures'] += [dict(label='act6-shelf-page-'+str(i),slot=40,phase='Brewing',visibleIngredients=['gin']) for i in (1,2)]
    return c


class FinalCandidateProofTests(unittest.TestCase):
    def records(self,c):
        args=[c['report'],c['captures'],c['checkpoints']]
        if 'known_before' in c:args += [None,c['known_before']]
        return proof.validate_records(*args)

    def test_final_candidate_requires_its_explicit_scene_identity(self):
        c=final_records()
        self.assertEqual([],self.records(c))
        c['report']['scene']='Bar_Q1_Act7'
        self.assertIn('Wrong campaign scene',self.records(c))

    def test_accepted_act_scene_cannot_silently_claim_final(self):
        c=act7_records(); c['report']['scene']='Bar_Q1_Final'
        self.assertIn('Wrong campaign scene',proof.validate_records(c['report'],c['captures'],c['checkpoints']))
        c['report'].update(finalCandidate=True,scope='through_act_06')
        self.assertIn('Final presentation requires complete ACT7 scope',proof.validate_records(c['report'],c['captures'],c['checkpoints']))

    def test_every_reading_capture_is_required_even_on_completed_continue(self):
        for label in MATRIX:
            with self.subTest(label=label):
                c=final_records();c['captures']=[x for x in c['captures'] if x['label']!=label]
                self.assertIn('Missing or duplicate final reading capture: '+label,self.records(c))

    def test_duplicate_or_unearned_matrix_capture_does_not_fill_coverage(self):
        for fault in ('duplicate','phase','known','large','contrast'):
            with self.subTest(fault=fault):
                c=final_records();cap=next(x for x in c['captures'] if x['label']==MATRIX[-1])
                if fault=='duplicate':c['captures'].append(copy.deepcopy(cap))
                elif fault=='phase':cap['phase']='menu'
                elif fault=='known':cap['known']=8
                elif fault=='large':cap['largeText']=not cap['largeText']
                else:cap['highContrast']=not cap['highContrast']
                self.assertTrue(self.records(c))

    def test_every_known_capture_requires_its_selected_source_derived_recipe(self):
        for setting in range(4):
            for recipe in RECIPES:
                for fault in ('absent','wrong_page','title','partial_status','method','ingredient','amount','dash','preparation','glass','garnish'):
                    label=f'p09-known-{setting}-{recipe}'
                    with self.subTest(label=label,fault=fault):
                        c=final_records();cap=next(x for x in c['captures'] if x['label']==label);card=cap['recipeCardText']
                        if fault=='absent':del cap['recipeCardText']
                        elif fault=='wrong_page':cap['recipeCardText']=known_card(RECIPES[(RECIPES.index(recipe)+1)%len(RECIPES)])
                        elif fault=='title':card[0]='다른 한 잔'
                        elif fault=='partial_status':card[1]=card[1].replace('만들어 본 기록','대화 속에서 들은 단서')
                        elif fault=='method':card[1]='만들어 본 기록    ·    Invalid'
                        elif fault=='ingredient':card[3]='다른 재료'
                        elif fault=='amount':card[4]='999 ml'
                        elif fault=='dash':card[4]+=' · Dash' if ' · Dash' not in card[4] else ' · Invalid'
                        elif fault=='preparation':card[-2]='준비 순서는 아직 듣지 못했습니다.'
                        elif fault=='glass':card[-1]='마무리 · 다른 잔\n'+card[-1].split('\n')[-1]
                        else:card[-1]=card[-1].split('\n')[0]+'\n다른 가니시'
                        self.assertIn('Wrong final Known recipe detail: '+label,self.records(c))

    def test_known_detail_uses_loaded_content_rather_than_a_second_recipe_table(self):
        for field in ('name','ingredient','amount','glass','garnish'):
            with self.subTest(field=field):
                content=copy.deepcopy(CONTENT);recipe=content['recipes'][0];c=final_records()
                if field=='name':next(x for x in content['cocktails'] if x['id']==recipe['cocktailId'])['name']='변경된 이름'
                elif field=='ingredient':next(x for x in content['ingredients'] if x['id']==recipe['amounts'][0]['ingredientId'])['label']='변경된 재료'
                elif field=='amount':recipe['amounts'][0]['ml']+=1
                elif field=='glass':recipe['glassware']='flute'
                else:recipe['optionalGarnish']=not recipe['optionalGarnish']
                errors=proof.validate_records(c['report'],c['captures'],c['checkpoints'],content=content)
                for setting in range(4):
                    self.assertIn(f'Wrong final Known recipe detail: p09-known-{setting}-{recipe["id"]}',errors)

    def test_fresh_unknown_and_method_only_partial_are_real_selected_detail_states(self):
        self.assertEqual([],self.records(final_records(True)))
        for label in [prefix+'-'+str(setting) for prefix in ('p09-unknown-detail','p09-method-only-clue') for setting in range(4)]:
            for fault in ('missing','duplicate','index_only','identity_leak','amount_leak','wrong_method','wrong_slot','preference'):
                with self.subTest(label=label,fault=fault):
                    c=final_records(True);cap=next(x for x in c['captures'] if x['label']==label)
                    if fault=='missing':c['captures'].remove(cap)
                    elif fault=='duplicate':c['captures'].append(copy.deepcopy(cap))
                    elif fault=='index_only':cap['recipeCardText']=['아직 빈 페이지']*8
                    elif fault=='identity_leak':cap['recipeCardText'][0]='Dry Martini'
                    elif fault=='amount_leak':cap['recipeCardText'].append('60 ml')
                    elif fault=='wrong_method':cap['recipeCardText'][1]='대화 속에서 들은 단서    ·    Shake'
                    elif fault=='wrong_slot':cap['slot']=40
                    else:cap['largeText']=not cap['largeText']
                    self.assertTrue(self.records(c))

    def test_completed_continue_does_not_require_inherited_unknown_or_partial(self):
        self.assertEqual([],self.records(final_records()))
        self.assertEqual([],self.records(act7_records('Complete')),'Accepted ACT7 evidence must keep its prior contract.')

    def test_slot_zero_visit_still_requires_partial_without_requiring_a_new_unknown(self):
        for phase in ('Welcome','NightIntro','Arriving','Intro','Request'):
            with self.subTest(phase=phase):
                c=final_records(True);c['report'].update(fixture='continue',startPhase=phase)
                c['captures']=[x for x in c['captures'] if not x['label'].startswith('p09-unknown-detail')]
                self.assertEqual([],self.records(c))
                c['captures']=[x for x in c['captures'] if not x['label'].startswith('p09-method-only-clue-')]
                self.assertIn('Missing or invalid final detail capture: p09-method-only-clue-0',self.records(c))

    def test_slot_zero_brewing_continue_does_not_require_already_passed_request(self):
        c=final_records(True);c['report'].update(fixture='continue',startPhase='Brewing')
        c['captures']=[x for x in c['captures'] if not x['label'].startswith(('p09-unknown-detail','p09-method-only-clue'))]
        self.assertEqual([],self.records(c))
        c['report'].update(fixture='open',startPhase='Welcome')
        errors=self.records(c)
        self.assertIn('Missing or invalid final detail capture: p09-method-only-clue-0',errors)
        self.assertIn('Missing or invalid final detail capture: p09-unknown-detail-0',errors)

    def test_unlearned_discovery_visits_require_each_partial_setting(self):
        self.assertEqual([],self.records(final_records(visits=(29,40))))
        for slot,prefix in ((29,'p09-french-partial'),(40,'p09-last-word-partial')):
            for setting in range(4):
                for fault in ('missing','duplicate','different_card','known_status','glass_leak','wrong_phase','wrong_slot','preference'):
                    with self.subTest(slot=slot,setting=setting,fault=fault):
                        c=final_records(visits=(slot,));cap=next(x for x in c['captures'] if x['label']==prefix+'-'+str(setting))
                        if fault=='missing':c['captures'].remove(cap)
                        elif fault=='duplicate':c['captures'].append(copy.deepcopy(cap))
                        elif fault=='different_card':cap['recipeCardText'][0]='다른 한 잔'
                        elif fault=='known_status':cap['recipeCardText'][1]='만들어 본 기록    ·    Shake'
                        elif fault=='glass_leak':cap['recipeCardText'].append('플루트 잔' if slot==29 else '칵테일 잔')
                        elif fault=='wrong_phase':cap['phase']='Request'
                        elif fault=='wrong_slot':cap['slot']=0
                        else:cap['highContrast']=not cap['highContrast']
                        self.assertTrue(self.records(c))

    def test_known_before_service_skips_only_its_inherited_partial_matrix(self):
        for slot,recipe,prefix in ((29,'recipe_french_75','p09-french-partial'),(40,'recipe_last_word','p09-last-word-partial')):
            with self.subTest(slot=slot):
                c=final_records(visits=(slot,));c['captures']=[x for x in c['captures'] if not x['label'].startswith(prefix)]
                c['known_before']={slot:{recipe}}
                self.assertEqual([],self.records(c))
                c['known_before']={slot:{'recipe_negroni'}}
                self.assertTrue(self.records(c),'An unrelated inherited Known receipt must not excuse this lesson.')

if __name__ == '__main__': unittest.main()
