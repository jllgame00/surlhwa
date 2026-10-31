"""Bounded evidence-validator fixtures; these are never published runtime saves."""
import copy
import unittest

import verify_campaign_proof as proof
from test_act4_proof import act4_progress, french_service


def act5_progress(mismatch=False):
    value=act4_progress()
    value['completedOccurrences'] += [f'run:{i}' for i in range(33,37)]
    value['memories'].append(dict(id='mem_05',variant='faint_circle_broken_glass_red_light',occurrenceId='run:36'))
    recipes={33:'recipe_dry_martini',34:'recipe_gin_fizz',35:'recipe_manhattan',36:'recipe_negroni'}
    for slot,recipe in recipes.items():
        value['services'].append(french_service(36) if slot==36 and mismatch else dict(occurrenceId=f'run:{slot}',discovered=False,outcome=dict(recipeId=recipe,preparation='Good',qualityGood=True,fits=True)))
    return value


def act5_records():
    return dict(report=dict(status='PASS',errors=[],scope='through_act_05',scene='Bar_Q1_Act5',tier='Simulated production InputSystem; NOT native OS input',
                            services=37,expectedServices=37,departures=37,known=8,memories=5,startSlot=33,startDepartures=33,startPhase='NightEnd',visitedSlots=[33,34,35,36]),
                captures=[dict(label=f'act5-{phase.lower()}-{slot}-0',slot=slot,phase=phase,textId='authored',overflow=[])
                          for slot in range(33,37) for phase in ('Intro','Farewell')]+
                         [dict(label='earned-memory',phase='Complete',slot=36,memories=5,windmillVisible=True,hamletVisible=True,faustVisible=True)],
                checkpoints=[dict(name=f'{stage}-{slot}',currentHash='a'*64) for slot in range(33,37) for stage in ('before-service','after-service','after-departure')]+[dict(name='act5-complete',currentHash='a'*64)])


class ActFiveProofTests(unittest.TestCase):
    def validate(self,payload,report,source=None):
        validator=getattr(proof,'validate_act5_progress',None)
        self.assertTrue(callable(validator),'ACT5 requires its bounded progress validator')
        return validator(payload,report,source)

    def test_act5_requires_all_five_visible_memories_and_four_actual_services(self):
        self.assertEqual([],proof.validate_records(**act5_records()))
        for field in ('windmillVisible','hamletVisible','faustVisible'):
            case=act5_records();case['captures'][-1][field]=False
            self.assertTrue(proof.validate_records(**case),field)

    def test_act5_prior_is_act4_namespace(self):
        self.assertIn('through_act_05',proof.PROFILES)
        self.assertEqual([],proof.validate_history_binding('/run','Prior','/run/history',['campaign-through-act-04'],act=5))
        self.assertTrue(proof.validate_history_binding('/run','Prior','/run/history',['campaign-through-act-03'],act=5))

    def test_natural_departure_earns_memory_with_fitting_or_nonfitting_service(self):
        for mismatch in (False,True):
            self.assertEqual([],self.validate(act5_progress(mismatch),dict(visitedSlots=[33,34,35,36],faustMismatchObserved=mismatch)))

    def test_every_new_slot_has_correct_preparation_quality_and_request_result(self):
        report=dict(visitedSlots=[33,34,35,36])
        for slot in range(33,37):
            for field,value in [('recipeId','recipe_manwol'),('preparation','Wrong'),('qualityGood',False),('fits',False)]:
                case=act5_progress();case['services'][slot]['outcome'][field]=value
                self.assertTrue(self.validate(case,report),(slot,field))

    def test_fitting_result_requires_an_explicit_true_fit_value(self):
        for value in (None,'true',1):
            case=act5_progress();case['services'][-1]['outcome']['fits']=value
            self.assertTrue(self.validate(case,dict(visitedSlots=[33,34,35,36])),value)
        case=act5_progress();case['services'][-1]['outcome'].pop('fits')
        self.assertTrue(self.validate(case,dict(visitedSlots=[33,34,35,36])))

    def test_report_cannot_omit_newly_serviced_slots_to_skip_checks(self):
        case=act5_records();case['report']['visitedSlots']=[]
        self.assertTrue(proof.validate_records(**case))
        payload=act5_progress();payload['services'][-1]['outcome'].update(recipeId='recipe_manwol',fits=False)
        self.assertTrue(self.validate(payload,dict(visitedSlots=[])))

    def test_mid_farewell_start_excludes_only_its_already_served_encounter(self):
        case=act5_records();case['report'].update(startSlot=35,startDepartures=35,startPhase='Farewell',visitedSlots=[36])
        case['captures']=[c for c in case['captures'] if c['slot']==36 or c['slot']==35 and c['phase']=='Farewell']
        case['checkpoints']=[c for c in case['checkpoints'] if c['name'] in ('after-departure-35','before-service-36','after-service-36','after-departure-36','act5-complete')]
        self.assertEqual([],proof.validate_records(**case))
        case['report']['visitedSlots']=[]
        self.assertTrue(proof.validate_records(**case))

    def test_final_visit_does_not_retry_or_gate_memory_on_fit(self):
        for change in ('duplicate_service','memory_owner','memory_variant','missing_departure','new_knowledge'):
            case=act5_progress(True)
            if change=='duplicate_service':case['services'].append(copy.deepcopy(case['services'][-1]))
            if change=='memory_owner':case['memories'][-1]['occurrenceId']='run:35'
            if change=='memory_variant':case['memories'][-1]['variant']='reward'
            if change=='missing_departure':case['completedOccurrences'].pop()
            if change=='new_knowledge':case['knownReceipts']['new_recipe']='earned'
            self.assertTrue(self.validate(case,dict(visitedSlots=[33,34,35,36],faustMismatchObserved=True)),change)

    def test_nonfitting_french_still_requires_real_strain_and_measured_finish(self):
        for change in ('strain','gentle','fresh_lemon'):
            case=act5_progress(True);snap=case['services'][-1]['snapshot']
            if change=='strain':snap['trace'][0]['source']='mixing'
            if change=='gentle':snap['trace'].pop()
            if change=='fresh_lemon':snap['amounts']['lemon_juice']=snap['amounts'].pop('fresh_lemon_juice')
            self.assertTrue(self.validate(case,dict(visitedSlots=[36],faustMismatchObserved=True)),change)

    def test_farewell_resume_preserves_inherited_service_without_replaying_it(self):
        final=act5_progress(True);source=copy.deepcopy(final)
        source.update(phase='Farewell',slotIndex=36,lineIndex=1);source['completedOccurrences'].pop();source['memories'].pop()
        self.assertEqual([],self.validate(final,dict(visitedSlots=[],faustMismatchObserved=False),source))
        for slot in (0,29,32,36):
            case=copy.deepcopy(final);case['services'][slot]['outcome']['fits']=not case['services'][slot]['outcome']['fits']
            self.assertTrue(self.validate(case,dict(visitedSlots=[]),source),slot)

    def test_complete_continue_requires_no_replayed_visits(self):
        case=act5_records();case['report'].update(startSlot=36,startDepartures=37,startPhase='Complete',visitedSlots=[])
        case['captures']=case['captures'][-1:];case['checkpoints']=case['checkpoints'][-1:]
        self.assertEqual([],proof.validate_records(**case))


if __name__=='__main__':unittest.main()
