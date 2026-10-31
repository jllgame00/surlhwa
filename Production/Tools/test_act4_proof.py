"""Validator-only records; no synthetic records are published as runtime saves."""
import copy
import unittest

import verify_campaign_proof as proof
from test_campaign_proof import progress


def french_service(slot=29, gentle=True):
    amounts={'gin':30,'fresh_lemon_juice':15,'simple_syrup':15,'champagne':60}
    trace=[dict(sequence=1,kind='Strain',source='shaker',target='Serving',acceptedMl=60,components={k:v for k,v in amounts.items() if k!='champagne'}),
           dict(sequence=2,kind='Pour',source='champagne',target='Serving',acceptedMl=60,components={'champagne':60})]
    if gentle: trace.append(dict(sequence=3,kind='GentleStir',source=None,target='Serving',acceptedMl=0,components={}))
    return dict(occurrenceId=f'run:{slot}',discovered=slot==29 and gentle,
                snapshot=dict(amounts=amounts,hasIce=False,garnish=None,trace=trace,
                              shake=dict(howGood=True,qualityGood=True,amounts={k:v for k,v in amounts.items() if k!='champagne'})),
                outcome=dict(recipeId='recipe_french_75',preparation='Good' if gentle else 'Wrong',qualityGood=True,fits=slot==29))


def act4_progress(mismatch=False, failed=False):
    value=progress(); value['completedOccurrences'] += [f'run:{i}' for i in range(29,33)]
    value['knownReceipts']['recipe_french_75']='earned'
    value['memories'].append(dict(id='mem_04',variant='stage_skull_shadow',occurrenceId='run:32'))
    value['consumedLines']=['run:29:Retry:0:text_rc1_0039'] if failed else []
    if failed:value['services'].append(french_service(gentle=False))
    value['services'].append(french_service())
    for slot in (30,31,32):
        value['services'].append(french_service(32) if mismatch and slot==32 else dict(occurrenceId=f'run:{slot}',outcome=dict(recipeId='recipe_dry_martini',preparation='Good',qualityGood=True,fits=True)))
    return value


def act4_records():
    return dict(report=dict(status='PASS',errors=[],scope='through_act_04',scene='Bar_Q1_Act4',tier='Simulated production InputSystem; NOT native OS input',
                            services=33,expectedServices=33,departures=33,known=8,memories=4,startSlot=29,startDepartures=29,startPhase='NightEnd',visitedSlots=[29,30,31,32],
                            shelfSelections=['gin','fresh_lemon_juice','champagne'],suppliedIngredients=['gin','fresh_lemon_juice','champagne']),
                captures=[dict(label=f'act4-{phase.lower()}-{slot}-0',slot=slot,phase=phase,textId='authored',overflow=[])
                          for slot in range(29,33) for phase in ('Intro','Farewell')]+
                         [dict(label='act4-shelf-page-1',phase='Brewing',slot=29,visibleIngredients=['gin']),
                          dict(label='act4-shelf-page-2',phase='Brewing',slot=29,visibleIngredients=['fresh_lemon_juice','champagne']),
                          dict(label='earned-memory',phase='Complete',slot=32,memories=4,windmillVisible=True,hamletVisible=True)],
                checkpoints=[dict(name=f'{stage}-{slot}',currentHash='a'*64) for slot in range(29,33) for stage in ('before-service','after-service','after-departure')]+[dict(name='act4-complete',currentHash='a'*64)])


class ActFourProofTests(unittest.TestCase):
    def test_container_source_is_serialized_identity_while_target_is_enum(self):
        snapshot=french_service()['snapshot']
        self.assertTrue(proof.validate_french_snapshot(snapshot))
        for field,value in [('source','Shaker'),('source','mixing'),('target','serving'),('target','Mixing')]:
            changed=copy.deepcopy(snapshot);changed['trace'][0][field]=value
            self.assertFalse(proof.validate_french_snapshot(changed),(field,value))

    def validate(self,payload,report,source=None):
        validator=getattr(proof,'validate_act4_progress',None)
        self.assertTrue(callable(validator),'ACT4 progress must have a bounded evidence validator')
        return validator(payload,report,source)

    def test_act4_completion_requires_all_memories_and_actual_supply_page_reachability(self):
        self.assertEqual([],proof.validate_records(**act4_records()))
        for change in ('hidden','unselected','unseen'):
            case=act4_records()
            if change=='hidden':case['captures'][-1]['hamletVisible']=False
            if change=='unselected':case['report']['shelfSelections'].remove('champagne')
            if change=='unseen':case['captures'].pop(-2)
            self.assertTrue(proof.validate_records(**case),change)

    def test_complete_and_final_farewell_starts_do_not_require_replayed_discovery(self):
        for phase,departures in [('Complete',33),('Farewell',32)]:
            case=act4_records();case['report'].update(startSlot=32,startDepartures=departures,startPhase=phase,visitedSlots=[],shelfSelections=[],suppliedIngredients=[])
            case['captures']=[c for c in case['captures'] if c['label']=='earned-memory' or (phase=='Farewell' and c['phase']=='Farewell' and c['slot']==32)]
            case['checkpoints']=[c for c in case['checkpoints'] if c['name']=='act4-complete' or (phase=='Farewell' and c['name']=='after-departure-32')]
            self.assertEqual([],proof.validate_records(**case))

    def test_report_cannot_hide_supplied_ingredients_from_both_page_and_selection_lists(self):
        case=act4_records();case['report']['suppliedIngredients'].remove('champagne');case['report']['shelfSelections'].remove('champagne')
        case['captures'][-2]['visibleIngredients'].remove('champagne')
        self.assertTrue(proof.validate_records(**case,expected_supply=['gin','fresh_lemon_juice','champagne']))

    def test_failed_french_retry_requires_authored_reading_and_archives(self):
        case=act4_records();case['report'].update(frenchFailure=True,frenchRetryObserved=True)
        self.assertTrue(proof.validate_records(**case))
        case['captures'].append(dict(label='french-required-retry',phase='Retry',slot=29,textId='text_rc1_0039'))
        case['checkpoints'] += [dict(name=n,currentHash='a'*64) for n in ('failed-service-29','french-retry','french-retry-read')]
        self.assertEqual([],proof.validate_records(**case))
        case['captures'][-1]['textId']='unrelated'
        self.assertTrue(proof.validate_records(**case))

    def test_act4_prior_requires_act3_namespace(self):
        self.assertIn('through_act_04',proof.PROFILES)
        self.assertEqual([],proof.validate_history_binding('/run','Prior','/run/history',['campaign-through-act-03'],act=4))
        self.assertTrue(proof.validate_history_binding('/run','Prior','/run/history',['campaign-through-act-02'],act=4))

    def test_french_requires_measured_finish_and_distinct_fresh_lemon(self):
        report=dict(visitedSlots=[29,30,31,32])
        self.assertEqual([],self.validate(act4_progress(),report))
        for change in ('gentle','splash','strain','lemon','early','shaken_champagne'):
            case=act4_progress();s=case['services'][29]['snapshot']
            if change=='gentle':s['trace'].pop()
            if change=='splash':s['trace'][1]['kind']='Splash'
            if change=='strain':s['trace'][0]['source']='Mixing'
            if change=='lemon':s['amounts']['lemon_juice']=s['amounts'].pop('fresh_lemon_juice')
            if change=='early':s['trace'][0],s['trace'][1]=s['trace'][1],s['trace'][0]
            if change=='shaken_champagne':s['shake']['amounts']['champagne']=60
            self.assertTrue(self.validate(case,report),change)

    def test_failure_cannot_grant_known_and_success_requires_discovery_receipt(self):
        report=dict(visitedSlots=[29,30,31,32],frenchFailure=True)
        self.assertEqual([],self.validate(act4_progress(failed=True),report))
        for change in ('failed_grant','success_missing','no_failure'):
            case=act4_progress(failed=True)
            if change=='failed_grant':case['services'][29]['discovered']=True
            if change=='success_missing':case['services'][30]['discovered']=False
            if change=='no_failure':case['services'].pop(29)
            self.assertTrue(self.validate(case,report),change)

    def test_hamlet_mismatch_naturally_awards_one_mem04(self):
        report=dict(visitedSlots=[29,30,31,32],hamletMismatchObserved=True)
        self.assertEqual([],self.validate(act4_progress(True),report))
        for change in ('fits','owner','duplicate'):
            case=act4_progress(True)
            if change=='fits':case['services'][-1]['outcome']['fits']=True
            if change=='owner':case['memories'][-1]['occurrenceId']='run:31'
            if change=='duplicate':case['services'].append(copy.deepcopy(case['services'][-1]))
            self.assertTrue(self.validate(case,report),change)

    def test_failed_french_requires_consumed_retry_reading_even_when_inherited(self):
        case=act4_progress(failed=True);case['consumedLines']=[]
        self.assertTrue(self.validate(case,dict(visitedSlots=[29,30,31,32])))

    def test_inherited_failure_and_hamlet_mismatch_survive_reload_unchanged(self):
        final=act4_progress(True,True);source=copy.deepcopy(final);source.update(phase='Farewell',slotIndex=32,lineIndex=1)
        source['completedOccurrences'].pop();source['memories'].pop()
        report=dict(visitedSlots=[],hamletMismatchObserved=False)
        self.assertEqual([],self.validate(final,report,source))
        for index in (27,29,33):
            changed=copy.deepcopy(final);changed['services'][index]['outcome']['fits']=not changed['services'][index]['outcome']['fits']
            self.assertTrue(self.validate(changed,report,source),index)


if __name__=='__main__':unittest.main()
