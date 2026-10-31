"""Evidence-validator fixtures only; never used as published campaign saves."""
import copy
import json
import unittest
import verify_campaign_proof as proof
from test_act5_proof import act5_progress
from test_act4_proof import french_service


def last_word_service(slot=40,strained=True):
    amounts={k:22.5 for k in ('gin','green_chartreuse','maraschino_luxardo','fresh_lime_juice')}
    return dict(occurrenceId=f'run:{slot}',discovered=slot==40 and strained,
                outcome=dict(recipeId='recipe_last_word',preparation='Good' if strained else 'Weak',qualityGood=True,fits=True),
                snapshot=dict(amounts=amounts,hasIce=False,garnish=None,shake=dict(howGood=True,qualityGood=True,amounts=amounts.copy()),
                              trace=[dict(sequence=1,kind='Strain' if strained else 'PourFromContainer',source='shaker',target='Serving',acceptedMl=90,components=amounts.copy())]))


def act6_progress(mismatch=False,failed=False,early=False):
    p=act5_progress();p['completedOccurrences'] += [f'run:{i}' for i in range(37,43)];p['knownReceipts']['recipe_last_word']='earned'
    p['memories'].append(dict(id='mem_06',variant='half_submerged_lotus_slow_ripples',occurrenceId='run:41'))
    p['consumedLines'] += ['run:40:Retry:0:text_rc1_0059'] if failed else []
    for slot,recipe in {37:'recipe_negroni',38:'recipe_dry_martini',39:'recipe_manhattan',40:'recipe_last_word',41:'recipe_last_word',42:'recipe_dry_martini'}.items():
        if slot==40 and failed:p['services'].append(last_word_service(strained=False))
        if slot==41 and mismatch:p['services'].append(french_service(41))
        elif slot in (40,41):p['services'].append(last_word_service(slot))
        else:p['services'].append(dict(occurrenceId=f'run:{slot}',discovered=False,outcome=dict(recipeId=recipe,preparation='Good',qualityGood=True,fits=True)))
    p['selectedEncounters']=[f'enc_rc1_{i+1:02}' for i in range(43)]
    if early:
        p['selectedEncounters'][40]='enc_rc1_41_known'
        p['services'][39]=last_word_service(39);p['services'][39]['discovered']=True;p['services'][39]['outcome']['fits']=False
        p['services'][40]=dict(occurrenceId='run:40',discovered=False,outcome=dict(recipeId='recipe_negroni',preparation='Good',qualityGood=True,fits=True))
        p['knownReceipts']['recipe_last_word']='service:run:39'
    return p


def act6_records():
    return dict(report=dict(status='PASS',errors=[],scope='through_act_06',scene='Bar_Q1_Act6',tier='Simulated production InputSystem; NOT native OS input',
                            services=43,expectedServices=43,departures=43,known=9,memories=6,startSlot=37,startDepartures=37,startPhase='NightEnd',visitedSlots=list(range(37,43)),
                            suppliedIngredients=['gin','green_chartreuse','maraschino_luxardo','fresh_lime_juice'],shelfSelections=['gin','green_chartreuse','maraschino_luxardo','fresh_lime_juice']),
                captures=[dict(label=f'act6-{phase.lower()}-{slot}-0',slot=slot,phase=phase,textId='authored',overflow=[]) for slot in range(37,43) for phase in ('Intro','Farewell')]+
                         [dict(label='act6-shelf-page-1',slot=40,phase='Brewing',visibleIngredients=['gin']),dict(label='act6-shelf-page-2',slot=40,phase='Brewing',visibleIngredients=['green_chartreuse','maraschino_luxardo','fresh_lime_juice']),
                          dict(label='act6-earned-memory-after-41',slot=42,phase='Arriving',departures=42,services=42,memories=6,simCheongVisible=True),
                          dict(label='earned-memory',phase='Complete',slot=42,memories=6,windmillVisible=True,hamletVisible=True,faustVisible=True,simCheongVisible=True)],
                checkpoints=[dict(name=f'{stage}-{slot}',currentHash='a'*64) for slot in range(37,43) for stage in ('before-service','after-service','after-departure')]+[dict(name='act6-complete',currentHash='a'*64)])


class ActSixProofTests(unittest.TestCase):
    def validate(self,payload,report,source=None):
        validator=getattr(proof,'validate_act6_progress',None);self.assertTrue(callable(validator),'ACT6 needs bounded actual-progress verification')
        return validator(payload,report,source)

    def test_sixth_memory_must_appear_before_the_last_ordinary_service(self):
        self.assertEqual([],proof.validate_records(**act6_records()))
        for fault in ('hidden_final','missing_early','late_service'):
            c=act6_records()
            if fault=='hidden_final':c['captures'][-1]['simCheongVisible']=False
            if fault=='missing_early':c['captures'].pop(-2)
            if fault=='late_service':c['captures'][-2]['services']=43
            self.assertTrue(proof.validate_records(**c),fault)

    def test_both_fit_routes_and_failed_discovery_then_recovery(self):
        for mismatch in (False,True):
            for failed in (False,True):self.assertEqual([],self.validate(act6_progress(mismatch,failed),dict(visitedSlots=list(range(37,43)),simCheongMismatchObserved=mismatch,lastWordFailure=failed)))

    def test_real_strain_all_four_shaken_ingredients_and_distinct_fresh_lime(self):
        for fault in ('pour','mixing','late_lime','legacy_lime','serving_ice','garnish'):
            p=act6_progress();s=p['services'][40]['snapshot']
            if fault=='pour':s['trace'][0]['kind']='PourFromContainer'
            if fault=='mixing':s['trace'][0]['source']='mixing'
            if fault=='late_lime':s['shake']['amounts'].pop('fresh_lime_juice')
            if fault=='legacy_lime':s['amounts']['lime']=s['amounts'].pop('fresh_lime_juice')
            if fault=='serving_ice':s['hasIce']=True
            if fault=='garnish':s['garnish']='cherry'
            self.assertTrue(self.validate(p,dict(visitedSlots=list(range(37,43)))),fault)

    def test_failed_discovery_does_not_grant_known_or_skip_authored_retry(self):
        for fault in ('grant','reading','failure_not_poured','no_success'):
            p=act6_progress(failed=True)
            if fault=='grant':p['services'][40]['discovered']=True
            if fault=='reading':p['consumedLines']=[]
            if fault=='failure_not_poured':p['services'][40]['snapshot']['trace'][0]['kind']='Strain'
            if fault=='no_success':p['services'].pop(41)
            self.assertTrue(self.validate(p,dict(visitedSlots=list(range(37,43)),lastWordFailure=True)),fault)

    def test_faithful_inherited_retry_and_final_mismatch_resume(self):
        final=act6_progress(True,True);source=copy.deepcopy(final);source.update(phase='Farewell',slotIndex=41,lineIndex=1)
        source['services'].pop();source['completedOccurrences']=source['completedOccurrences'][:-2];source['memories'].pop()
        self.assertEqual([],self.validate(final,dict(visitedSlots=[42]),source))
        changed=copy.deepcopy(final);changed['services'][-2]['outcome']['fits']=True
        self.assertTrue(self.validate(changed,dict(visitedSlots=[42]),source))

    def test_new_services_cannot_be_hidden_by_the_report_and_fit_must_be_boolean(self):
        self.assertTrue(self.validate(act6_progress(),dict(visitedSlots=[])))
        for value in (None,1,'true'):
            p=act6_progress();p['services'][-1]['outcome']['fits']=value
            self.assertTrue(self.validate(p,dict(visitedSlots=list(range(37,43)))),value)

    def test_completed_continue_requires_no_replayed_visit_or_memory_moment(self):
        c=act6_records();c['report'].update(startPhase='Complete',startSlot=42,startDepartures=43,visitedSlots=[])
        c['captures']=c['captures'][-1:];c['checkpoints']=c['checkpoints'][-1:]
        self.assertEqual([],proof.validate_records(**c))
        p=act6_progress();self.assertEqual([],self.validate(p,dict(visitedSlots=[]),copy.deepcopy(p)))

    def test_act6_prior_namespace_is_act5(self):
        self.assertIn('through_act_06',proof.PROFILES)
        self.assertEqual([],proof.validate_history_binding('/run','Prior','/run/history',['campaign-through-act-05'],act=6))
        self.assertTrue(proof.validate_history_binding('/run','Prior','/run/history',['campaign-through-act-04'],act=6))

    def test_wrong_pour_is_the_only_failed_last_word_axis(self):
        for fault in ('quality','fit','batch','amount','batch_quality','direct_addition'):
            p=act6_progress(failed=True);s=p['services'][40]
            if fault=='quality':s['outcome']['qualityGood']=False
            if fault=='fit':s['outcome']['fits']=False
            if fault=='batch':s['snapshot']['shake']['amounts'].pop('fresh_lime_juice')
            if fault=='amount':s['snapshot']['amounts']['gin']=30
            if fault=='batch_quality':s['snapshot']['shake']['qualityGood']=False
            if fault=='direct_addition':s['snapshot']['trace'][0]['source']='gin'
            self.assertTrue(self.validate(p,dict(visitedSlots=list(range(37,43)),lastWordFailure=True)),fault)

    def test_retry_capture_requires_exact_rendered_0059_wording_and_role(self):
        validate=getattr(proof,'validate_last_word_retry',None);self.assertTrue(callable(validate))
        content=json.loads((proof.ROOT/'Content/Campaign/act6.json').read_bytes());authored={x['id']:x for x in content['texts']}
        c=dict(label='last-word-required-retry',textId='text_rc1_0059',textRole='spoken',visibleText=[authored['text_rc1_0059']['value']])
        self.assertEqual([],validate(c,authored))
        for field,value in [('visibleText',['unrelated retry wording']),('textRole','narration'),('textId','text_rc1_0039')]:
            changed=copy.deepcopy(c);changed[field]=value;self.assertTrue(validate(changed,authored),field)

    def test_early_discovery_selects_authored_fallback_from_actual_saved_history(self):
        self.assertEqual([],self.validate(act6_progress(early=True),dict(visitedSlots=list(range(37,43)),earlyLastWordObserved=True)))
        for fault in ('no_service','fit','quality','not_discovered','receipt','lesson_consumed','wrong_fallback','fallback_drink'):
            p=act6_progress(early=True)
            if fault=='no_service':p['services'].pop(39)
            if fault=='fit':p['services'][39]['outcome']['fits']=True
            if fault=='quality':p['services'][39]['outcome']['qualityGood']=False
            if fault=='not_discovered':p['services'][39]['discovered']=False
            if fault=='receipt':p['knownReceipts']['recipe_last_word']='seed'
            if fault=='lesson_consumed':p['consumedLines'].append('run:40:Intro:0:text_rc1_0055')
            if fault=='wrong_fallback':p['selectedEncounters'][40]='enc_rc1_41'
            if fault=='fallback_drink':p['services'][40]['outcome']['recipeId']='recipe_last_word'
            self.assertTrue(self.validate(p,dict(visitedSlots=list(range(37,43)),earlyLastWordObserved=True)),fault)

    def test_inherited_early_discovery_is_not_reperformed_on_farewell_or_complete_reload(self):
        final=act6_progress(early=True);source=copy.deepcopy(final)
        source.update(phase='Farewell',slotIndex=39,lineIndex=0)
        source['services']=source['services'][:40];source['selectedEncounters']=source['selectedEncounters'][:40]
        source['completedOccurrences']=source['completedOccurrences'][:39];source['memories'].pop()
        self.assertEqual([],self.validate(final,dict(visitedSlots=[40,41,42]),source))
        self.assertEqual([],self.validate(final,dict(visitedSlots=[]),copy.deepcopy(final)))

    def test_last_word_card_hides_unknown_details_and_exposes_learned_recipe(self):
        validate=getattr(proof,'validate_last_word_card',None);self.assertTrue(callable(validate))
        content=json.loads((proof.ROOT/'Content/Campaign/act6.json').read_bytes())
        unknown=dict(label='codex-last-word-unknown',recipeCardText=['이름을 기다리는 한 잔','대화 속에서 들은 단서 · 제조법 미상','아직 적어 둔 재료가 없습니다.','준비 순서는 아직 듣지 못했습니다.'])
        self.assertEqual([],validate(unknown,content,'unknown'))
        for leak in ('라스트 워드','그린 샤르트뢰즈','22.5 ml','Shake','U로 거르기','칵테일 잔'):
            c=copy.deepcopy(unknown);c['recipeCardText'].append(leak);self.assertTrue(validate(c,content,'unknown'),leak)
        known=dict(label='codex-last-word-early-known',recipeCardText=['라스트 워드','만들어 본 기록 · Shake','진 22.5 ml\n그린 샤르트뢰즈 22.5 ml\n마라스키노 룩사르도 22.5 ml\n프레시 라임 주스 22.5 ml','U로 거르기 · 서빙 얼음 없음 · 가니시 없음'])
        self.assertEqual([],validate(known,content,'known'))
        for field in (0,2,3):
            c=copy.deepcopy(known);c['recipeCardText'].pop(field);self.assertTrue(validate(c,content,'known'),field)

    def test_partial_last_word_card_cannot_expose_untaught_glassware(self):
        content=json.loads((proof.ROOT/'Content/Campaign/act6.json').read_bytes())
        partial=dict(label='codex-last-word-clues',recipeCardText=['라스트 워드','대화 속에서 들은 단서 · Shake','진 22.5 ml\n그린 샤르트뢰즈 22.5 ml\n마라스키노 룩사르도 22.5 ml\n프레시 라임 주스 22.5 ml','U로 거르기 · 서빙 얼음 없음 · 가니시 없음'])
        self.assertEqual([],proof.validate_last_word_card(partial,content,'partial'))
        leaked=copy.deepcopy(partial);leaked['recipeCardText'].append('칵테일 잔')
        self.assertTrue(proof.validate_last_word_card(leaked,content,'partial'))

    def test_learning_captures_cover_early_supply_before_disclosure_and_actual_service(self):
        content=json.loads((proof.ROOT/'Content/Campaign/act6.json').read_bytes())
        unknown=['이름을 기다리는 한 잔','제조법 미상','아직 적어 둔 재료가 없습니다.','준비 순서는 아직 듣지 못했습니다.']
        known=['라스트 워드','만들어 본 기록 · Shake','진 22.5 ml\n그린 샤르트뢰즈 22.5 ml\n마라스키노 룩사르도 22.5 ml\n프레시 라임 주스 22.5 ml','U로 거르기 · 서빙 얼음 없음 · 가니시 없음']
        captures=[dict(label='codex-last-word-unknown',slot=39,phase='Brewing',known=8,recipeCardText=unknown),
                  dict(label='codex-last-word-early-known',slot=39,phase='Reaction',known=9,recipeCardText=known),
                  dict(label='codex-last-word-fallback-known',slot=40,phase='Brewing',known=9,recipeCardText=known),
                  dict(label='act6-shelf-page-1',slot=39,phase='Brewing',visibleIngredients=content['supply'][:11]),
                  dict(label='act6-shelf-page-2',slot=39,phase='Brewing',visibleIngredients=content['supply'][11:])]
        report=dict(suppliedIngredients=content['supply'],shelfSelections=content['supply'])
        p=act6_progress(early=True)
        validate=proof.validate_act6_learning_captures
        self.assertEqual([],validate(p,captures,report,content))
        for fault in ('missing_unknown','missing_earned_known','wrong_known_boundary','late_supply_page','non_brewing_supply','unselected_ingredient'):
            c=copy.deepcopy(captures);r=copy.deepcopy(report)
            if fault=='missing_unknown':c.pop(0)
            if fault=='missing_earned_known':c.pop(1)
            if fault=='wrong_known_boundary':c[1]['phase']='Brewing'
            if fault=='late_supply_page':c[-1]['slot']=40
            if fault=='non_brewing_supply':c[-1]['phase']='Reaction'
            if fault=='unselected_ingredient':r['shelfSelections'].pop()
            self.assertTrue(validate(p,c,r,content),fault)
        inherited=copy.deepcopy(p);inherited['services']=inherited['services'][:40]
        later=copy.deepcopy(captures[2:]);later[1]['slot']=later[2]['slot']=40
        self.assertEqual([],validate(p,later,report,content,inherited))
        self.assertEqual([],validate(p,[],{},content,copy.deepcopy(p)))


if __name__=='__main__':unittest.main()
