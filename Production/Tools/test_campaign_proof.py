"""Synthetic validator records only; these tests do not create campaign saves."""
import copy
import unittest

from verify_campaign_proof import validate_records, validate_act3_progress, validate_history_binding, inherited_quixote_evidence


def records():
    return dict(
        report=dict(status='PASS',errors=[],scope='through_act_03',scene='Bar_Q1_Act3',
                    tier='Simulated production InputSystem; NOT native OS input',services=29,expectedServices=29,
                    departures=29,known=7,memories=3,startSlot=25,startDepartures=25,startPhase='NightEnd',
                    visitedSlots=[25,26,27,28],deliberateFailure=False,retryObserved=False,quixoteMismatchObserved=False),
        captures=[dict(label=f'act3-{phase.lower()}-{slot}-0',slot=slot,phase=phase,textId='authored',overflow=[])
                  for slot in range(25,29) for phase in ('Intro','Farewell')] +
                 [dict(label='act3-earned-memory-after-27',slot=28,phase='Arriving',departures=28,services=28,
                       memories=3,windmillVisible=True,textId='',overflow=[]),
                  dict(label='earned-memory',slot=28,phase='Complete',memories=3,windmillVisible=True,textId='',overflow=[])],
        checkpoints=[dict(name=f'{stage}-{slot}',slot=slot,services=slot+(stage!='before-service'),currentHash='a'*64)
                     for slot in range(25,29) for stage in ('before-service','after-service','after-departure')] +
                    [dict(name='act3-complete',slot=28,services=29,currentHash='a'*64)])


def progress(mismatch=False):
    recipes=['recipe_test_stir']*25+['recipe_manhattan','recipe_gin_fizz','recipe_dry_martini' if mismatch else 'recipe_hanky_panky','recipe_negroni']
    return dict(phase='Complete',completedOccurrences=[f'run:{i}' for i in range(29)],
                knownReceipts={str(i):'earned' for i in range(7)},
                memories=[dict(id='mem_01',variant='star_glowing',occurrenceId='run:16'),
                          dict(id='mem_02',variant='swing_empty_place',occurrenceId='run:24'),
                          dict(id='mem_03',variant='windmill_fragment',occurrenceId='run:27')],
                services=[dict(occurrenceId=f'run:{i}',outcome=dict(recipeId=recipe,preparation='Good',qualityGood=True,fits=not(mismatch and i==27)))
                          for i,recipe in enumerate(recipes)])


class CampaignProofTests(unittest.TestCase):
    def test_complete_act3_records_pass(self):
        self.assertEqual([],validate_records(**records()))

    def test_act2_completion_cannot_certify_act3(self):
        case=records();case['report'].update(departures=25,known=7,memories=2,services=25)
        self.assertTrue(validate_records(**case))

    def test_memory_capture_must_precede_final_ordinary_service(self):
        for change in ('missing','late','hidden'):
            case=records();memory=case['captures'][-2]
            if change=='missing':case['captures'].remove(memory)
            if change=='late':memory.update(services=29,departures=29,phase='Complete')
            if change=='hidden':memory['windmillVisible']=False
            self.assertTrue(validate_records(**case),change)

    def test_farewell_continue_requires_departure_but_no_replayed_service(self):
        case=records();case['report'].update(startSlot=27,startDepartures=27,startPhase='Farewell',visitedSlots=[28])
        case['captures']=[c for c in case['captures'] if c['slot']>=27 and not(c['slot']==27 and c['phase']=='Intro')]
        case['checkpoints']=[c for c in case['checkpoints'] if not c['name'].endswith('-25') and not c['name'].endswith('-26')]
        self.assertEqual([],validate_records(**case))
        case['checkpoints']=[c for c in case['checkpoints'] if c['name']!='after-departure-27']
        self.assertTrue(validate_records(**case))

    def test_complete_continue_requires_no_new_memory_grant_capture(self):
        case=records();case['report'].update(startSlot=28,startDepartures=29,startPhase='Complete',visitedSlots=[])
        case['captures']=case['captures'][-1:];case['checkpoints']=case['checkpoints'][-1:]
        self.assertEqual([],validate_records(**case))

    def test_mismatch_is_a_real_service_and_still_awards_one_memory(self):
        report=dict(quixoteMismatchObserved=True)
        self.assertEqual([],validate_act3_progress(progress(True),report))
        for change in ('fit','owner','duplicate','missing'):
            case=progress(True)
            if change=='fit':case['services'][27]['outcome']['fits']=True
            if change=='owner':case['memories'][-1]['occurrenceId']='run:28'
            if change=='duplicate':case['memories'].append(copy.deepcopy(case['memories'][-1]))
            if change=='missing':case['memories'].pop()
            self.assertTrue(validate_act3_progress(case,report),change)

    def test_normal_act3_service_must_actually_fit(self):
        case=progress();case['services'][26]['outcome']['fits']=False
        self.assertTrue(validate_act3_progress(case,dict(quixoteMismatchObserved=False)))

    def test_history_root_must_be_run_local_for_every_start(self):
        for start in ('New','Prior','Legacy','Checkpoint'):
            self.assertEqual([],validate_history_binding('/evidence/run',start,'/evidence/run/history',[] if start in ('New','Checkpoint') else ['campaign-through-act-02' if start=='Prior' else 'campaign-act1']))
            self.assertTrue(validate_history_binding('/evidence/run',start,'/owner/history',[]),start)
        self.assertTrue(validate_history_binding('/evidence/run','New','/evidence/run/history',['campaign-act1']))

    def test_mid_farewell_keeps_inherited_mismatch_distinct_from_new_service(self):
        source=progress(True);source.update(phase='Farewell',slotIndex=27,lineIndex=1)
        source['services']=source['services'][:28];source['completedOccurrences']=source['completedOccurrences'][:27];source['memories']=source['memories'][:2]
        final=progress(True);report=dict(quixoteMismatchObserved=False,visitedSlots=[28])
        self.assertEqual([],validate_act3_progress(final,report,source))
        self.assertEqual(dict(servicePresent=True,servicePreserved=True,mismatchPreserved=True),inherited_quixote_evidence(final,source))
        for field,value in [('fits',True),('recipeId','recipe_hanky_panky'),('qualityGood',False)]:
            changed=copy.deepcopy(final);changed['services'][27]['outcome'][field]=value
            self.assertTrue(validate_act3_progress(changed,report,source),field)
            self.assertFalse(inherited_quixote_evidence(changed,source)['mismatchPreserved'])


if __name__=='__main__':
    unittest.main()
