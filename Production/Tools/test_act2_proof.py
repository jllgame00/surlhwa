"""Evidence-validator tests; these records are not campaign saves or Player evidence."""
import copy
import unittest

from verify_act2_proof import validate_records, validate_authored_text, validate_completed_continue


def records():
    return dict(
        report=dict(status='PASS', errors=[], tier='Simulated production InputSystem; NOT native OS input',
                    scope='through_act_02', scene='Bar_Q1_Act2', services=25, departures=25, known=7, memories=2,
                    expectedServices=25, startSlot=17, startDepartures=17, startPhase='NightIntro',
                    visitedSlots=list(range(17, 25)), deliberateFailure=False, retryObserved=False),
        captures=[dict(label=f'act2-intro-{slot}-0', slot=slot, phase='Intro', textId='authored', overflow=[])
                  for slot in range(17, 25)] +
                 [dict(label=f'act2-farewell-{slot}-0', slot=slot, phase='Farewell', textId='authored', overflow=[])
                  for slot in range(17, 25)] +
                 [dict(label='earned-memory', slot=24, phase='Complete', textId='', overflow=[])],
        checkpoints=[dict(name=f'{stage}-{slot}', slot=slot, services=slot+(stage!='before-service'),
                          currentHash='a'*64, previousHash='b'*64)
                     for slot in range(17, 25) for stage in ('before-service', 'after-service', 'after-departure')] +
                    [dict(name='act2-complete', slot=24, services=25, currentHash='a'*64, previousHash='b'*64)])


class Act2ProofEvidenceTests(unittest.TestCase):
    def test_complete_record_set_is_accepted(self):
        self.assertEqual([], validate_records(**records()))

    def test_rejects_act1_result_presented_as_act2(self):
        case=records(); case['report'].update(services=17, departures=17, known=6, memories=1)
        self.assertTrue(validate_records(**case))

    def test_rejects_missing_paired_farewell_and_silence(self):
        for slot in (20, 22, 24):
            case=records(); case['captures']=[c for c in case['captures'] if c['slot']!=slot]
            self.assertTrue(validate_records(**case), slot)

    def test_rejects_missing_or_duplicate_checkpoint(self):
        case=records(); case['checkpoints'].pop()
        self.assertTrue(validate_records(**case))
        case=records(); case['checkpoints'].append(copy.deepcopy(case['checkpoints'][0]))
        self.assertTrue(validate_records(**case))

    def test_rejects_unobserved_required_failure_retry(self):
        case=records(); case['report'].update(deliberateFailure=True, services=26, expectedServices=26)
        self.assertTrue(validate_records(**case))

    def test_rejects_overflow_and_native_tier_claim(self):
        case=records(); case['captures'][0]['overflow']=['truncated authored text']
        self.assertTrue(validate_records(**case))
        case=records(); case['report']['tier']='Native OS input PASS'
        self.assertTrue(validate_records(**case))

    def test_rejects_missing_actor_material_in_capture(self):
        case=records(); case['captures'][0]['invalidRenderers']=['Chunhyang: missing material']
        self.assertTrue(validate_records(**case))

    def test_completed_continue_requires_memory_but_no_replayed_encounters(self):
        case=records(); case['report'].update(startSlot=24, startDepartures=25, startPhase='Complete', visitedSlots=[])
        case['captures']=case['captures'][-1:]; case['checkpoints']=case['checkpoints'][-1:]
        self.assertEqual([], validate_records(**case))

    def test_rejects_authored_text_missing_from_actual_visible_ui(self):
        authored={'text_line':dict(value='A remembered night.',role='narration')}
        capture=dict(textId='text_line',textRole='narration',visibleText=['A different night.'],label='intro')
        self.assertTrue(validate_authored_text(capture,authored))
        capture['visibleText']=['A remembered night.']
        self.assertEqual([],validate_authored_text(capture,authored))

    def test_farewell_continue_requires_its_actual_departure_archive(self):
        case=records(); case['report'].update(startSlot=24,startDepartures=24,startPhase='Farewell',visitedSlots=[])
        case['captures']=[c for c in case['captures'] if c['slot']==24 and c['phase']!='Intro']
        case['checkpoints']=case['checkpoints'][-1:]
        self.assertTrue(validate_records(**case))
        case['checkpoints'].append(dict(name='after-departure-24',slot=24,services=25,currentHash='a'*64,previousHash='b'*64))
        self.assertEqual([],validate_records(**case))

    def test_completed_continue_requires_unchanged_save_generations(self):
        report=dict(startPhase='Complete',visitedSlots=[])
        origin={'current.json':'a'*64,'previous.json':'b'*64}
        self.assertEqual([],validate_completed_continue(report,origin,dict(origin)))
        self.assertTrue(validate_completed_continue(report,origin,dict(origin,**{'current.json':'c'*64})))
        self.assertTrue(validate_completed_continue(report,origin,{'current.json':'a'*64}))


if __name__=='__main__':
    unittest.main()
