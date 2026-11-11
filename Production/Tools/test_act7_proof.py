"""Validator-only fixtures; none are published or used as Player save evidence."""
import copy
import json
import shutil
import subprocess
import unittest
from pathlib import Path

import verify_campaign_proof as proof
from test_act6_proof import act6_progress, act6_records, last_word_service
from test_act4_proof import french_service

CONTENT=json.loads((Path(__file__).resolve().parents[1]/'Content/Campaign/act7.json').read_text(encoding='utf-8'))
TEXTS={t['id']:t for t in CONTENT['texts']}


def ending_lines(choice='keep'):
    return ['text_rc1_0001','text_rc1_0002',{'keep':'text_rc1_0412','seek':'text_rc1_0413','none':'text_rc1_0414'}[choice or 'none']]+[f'text_rc1_{i:04}' for i in range(3,9)]


def act7_progress(choice='keep',mismatch=False):
    p=act6_progress();p.update(slotIndex=46,lineIndex=0,princeChoice=choice,farewell='Open')
    p['selectedEncounters'] += [f'enc_rc1_{i}' for i in range(44,48)]
    p['completedOccurrences'] += [f'run:{i}' for i in range(43,47)]
    p['memories'].append(dict(id='mem_07',variant='anchor_ship_fragment',occurrenceId='run:46'))
    for slot,recipe in {43:'recipe_manhattan',44:'recipe_negroni',45:'recipe_dry_martini'}.items():
        p['services'].append(dict(occurrenceId=f'run:{slot}',discovered=False,outcome=dict(recipeId=recipe,preparation='Good',qualityGood=True,fits=True)))
    p['services'].append(french_service(46) if mismatch else last_word_service(46))
    p['consumedLines'] += [f'run:46:{"EndingEntered" if i==0 else "EndingReading"}:{i}:{text}' for i,text in enumerate(ending_lines(choice))]
    return p


def ending_state(final,phase,line=0):
    state=copy.deepcopy(final);state.update(phase=phase,lineIndex=line)
    count=9 if phase=='Complete' else line if phase=='EndingReading' else 0
    state['consumedLines']=final['consumedLines'][:-9]+final['consumedLines'][-9:][:count]
    return state


def act7_records(phase='NightEnd',line=0,choice='keep',mismatch=False):
    final=act7_progress(choice,mismatch);ending=phase.startswith('Ending') or phase=='Complete'
    source=ending_state(final,phase,line) if ending else act6_progress()
    source.update(slotIndex=46 if ending else 42,lineIndex=line,princeChoice=choice,farewell='Open')
    report=dict(status='PASS',errors=[],scope='through_act_07',scene='Bar_Q1_Act7',tier='Simulated production InputSystem; NOT native OS input',
                services=47,expectedServices=47,departures=47,known=9,memories=7,startSlot=46 if ending else 43,startDepartures=47 if ending else 43,
                startPhase=phase,visitedSlots=[] if ending else list(range(43,47)),odysseusMismatchObserved=mismatch and not ending)
    captures=[];checkpoints=[];archives={}
    def capture(label,state):
        p=state['phase'];i=state['lineIndex'];text=ending_lines(choice)[i] if p in ('EndingEntered','EndingReading') else 'text_rc1_0008' if p=='Complete' else None
        return dict(label=label,slot=state['slotIndex'],phase=p,line=i,textId=text or '',textRole=TEXTS[text]['role'] if text else '',
                    visibleText=[TEXTS[text]['value']] if text else [],displayedSpeaker='기억 속 어린왕자' if text and TEXTS[text]['role']=='spoken' else '',visibleActors=['guest_bartender'],
                    services=len(state['services']),departures=len(state['completedOccurrences']),memories=len(state['memories']),known=9,
                    loversVisible=True,windmillVisible=True,hamletVisible=True,faustVisible=True,simCheongVisible=True,odysseusVisible=len(state['memories'])==7)
    def checkpoint(label,state):
        archives[label]=state
        checkpoints.append(dict(name=label,slot=state['slotIndex'],phase=state['phase'],services=len(state['services']),departures=len(state['completedOccurrences']),currentHash='a'*64))
    initial=copy.deepcopy(source)
    if not ending:initial.update(phase='NightEnd',slotIndex=43)
    captures.append(capture('act7-start',initial));checkpoint('act7-start',initial)
    if not ending:
        captures += [dict(label=f'act7-{p.lower()}-{slot}-0',slot=slot,phase=p,textId='authored',odysseusVisible=False) for slot in range(43,47) for p in ('Intro','Farewell')]
        checkpoints += [dict(name=f'{stage}-{slot}',currentHash='a'*64) for slot in range(43,47) for stage in ('before-service','after-service','after-departure')]
        ready=ending_state(final,'EndingReady');archives['after-departure-46']=ready
        checkpoints[-1].update(slot=46,phase='EndingReady',services=47,departures=47)
    labels=[]
    if phase not in ('EndingEntered','EndingReading','Complete'):labels.append(('act7-ending-ready','EndingReady',0))
    if phase!='Complete':labels += [(f'act7-ending-{i}','EndingEntered' if i==0 else 'EndingReading',i) for i in range(line if phase=='EndingReading' else 0,9)]
    for label,p,i in labels:
        state=ending_state(final,p,i);captures.append(capture(label,state));checkpoint(label,state)
    checkpoint('act7-complete',final);captures.append(capture('earned-memory',final))
    return dict(report=report,captures=captures,checkpoints=checkpoints,payload=final,source=source,archives=archives)


class ActSevenProofTests(unittest.TestCase):
    def records(self,c):return proof.validate_records(c['report'],c['captures'],c['checkpoints'])
    def progress(self,c):
        validator=getattr(proof,'validate_act7_progress',None);self.assertTrue(callable(validator),'ACT7 needs actual final-progress validation')
        return validator(c['payload'],c['report'],c['source'])
    def ending(self,c):
        validator=getattr(proof,'validate_act7_ending',None);self.assertTrue(callable(validator),'ACT7 needs actual ending capture/checkpoint validation')
        return validator(c['payload'],c['report'],c['captures'],c['checkpoints'],CONTENT,c['archives'],c['source'])

    def test_profile_and_actual_act6_complete_continuation(self):
        c=act7_records();self.assertEqual([],self.records(c));self.assertEqual([],self.progress(c));self.assertEqual([],self.ending(c))
        self.assertEqual([],proof.validate_history_binding('/run','Prior','/run/history',['campaign-through-act-06'],act=7))

    def test_all_remaining_unread_lines_resume_without_farewell_replay(self):
        for phase,line in [('EndingReady',0),('EndingEntered',0)]+[('EndingReading',i) for i in range(1,9)]+[('Complete',0)]:
            with self.subTest(phase=phase,line=line):
                c=act7_records(phase,line);self.assertEqual([],self.records(c));self.assertEqual([],self.progress(c));self.assertEqual([],self.ending(c))

    def test_keep_seek_none_and_both_fit_routes_share_exact_common_ending(self):
        for choice in ('keep','seek',None):
            for mismatch in (False,True):
                c=act7_records(choice=choice,mismatch=mismatch);self.assertEqual([],self.progress(c));self.assertEqual([],self.ending(c))

    def test_final_ending_receipts_cannot_be_missing_repeated_wrong_branch_or_foreign(self):
        for fault in ('missing','repeat','branch','owner','phase','cursor','premature','extra_visit'):
            c=act7_records();p=c['payload']
            if fault=='missing':p['consumedLines'].pop()
            if fault=='repeat':p['consumedLines'].append(p['consumedLines'][-1])
            if fault=='branch':p['consumedLines'][-7]=p['consumedLines'][-7].replace('0412','0413')
            if fault=='owner':p['consumedLines'][-1]=p['consumedLines'][-1].replace('run:46:','run:45:')
            if fault=='phase':p['consumedLines'][-9]=p['consumedLines'][-9].replace('EndingEntered','EndingReading')
            if fault=='cursor':p['lineIndex']=8
            if fault=='premature':p['completedOccurrences'].pop()
            if fault=='extra_visit':p['selectedEncounters'].append('enc_fake')
            self.assertTrue(self.progress(c),fault)

    def test_memory_requires_exact_departure_and_remains_visible_through_ending(self):
        for fault in ('future','wrong_departure','hidden','too_early'):
            c=act7_records()
            if fault=='future':c['payload']['memories'][-1]['variant']='completed_ship_fixed_anchor'
            if fault=='wrong_departure':c['payload']['memories'][-1]['occurrenceId']='run:45'
            if fault=='hidden':c['captures'][-2]['odysseusVisible']=False
            if fault=='too_early':c['captures'][1]['odysseusVisible']=True
            self.assertTrue(self.progress(c)+self.records(c)+self.ending(c),fault)

    def test_ending_cannot_hide_skip_repeat_reorder_or_replay_consumed_text(self):
        for fault in ('skip','repeat','reorder','branch','text','speaker','actor','cursor','wrong_phase','replay'):
            c=act7_records('EndingReading',4);cap=next(x for x in c['captures'] if x['label']=='act7-ending-4')
            if fault=='skip':c['captures'].remove(cap)
            if fault=='repeat':c['captures'].append(copy.deepcopy(cap))
            if fault=='reorder':c['captures'][1],c['captures'][2]=c['captures'][2],c['captures'][1]
            if fault=='branch':cap['textId']='text_rc1_0413'
            if fault=='text':cap['visibleText']=['invented ending']
            if fault=='speaker':cap['displayedSpeaker']='오디세우스'
            if fault=='actor':cap['visibleActors'].append('guest_odysseus')
            if fault=='cursor':cap['line']=3
            if fault=='wrong_phase':cap['phase']='Farewell'
            if fault=='replay':c['captures'].insert(1,dict(cap,label='act7-ending-3',line=3))
            self.assertTrue(self.ending(c),fault)

    def test_archived_ending_state_matches_each_unread_cursor_and_final_publication(self):
        for fault in ('receipt','phase','cursor','premature','history','missing'):
            c=act7_records();p=c['archives']['act7-ending-4']
            if fault=='receipt':p['consumedLines'].pop()
            if fault=='phase':p['phase']='EndingEntered'
            if fault=='cursor':p['lineIndex']=5
            if fault=='premature':p['completedOccurrences'].pop()
            if fault=='history':p['services'][0]['outcome']['fits']=False
            if fault=='missing':del c['archives']['act7-ending-4']
            self.assertTrue(self.ending(c),fault)

    def test_source_cursor_and_history_cannot_be_falsified_to_skip_reading(self):
        for fault in ('reported_phase','reported_slot','reported_departures','initial_cursor','consumed_prefix','choice','memory'):
            c=act7_records('EndingReading',4)
            if fault=='reported_phase':c['report']['startPhase']='Complete'
            if fault=='reported_slot':c['report']['startSlot']=45
            if fault=='reported_departures':c['report']['startDepartures']=46
            if fault=='initial_cursor':c['captures'][0]['line']=5
            if fault=='consumed_prefix':c['source']['consumedLines'].pop()
            if fault=='choice':c['payload']['princeChoice']='seek'
            if fault=='memory':c['payload']['memories'][0]['variant']='star_dim'
            self.assertTrue(self.progress(c)+self.ending(c),fault)

    def test_new_services_cannot_be_omitted_or_discovered_again(self):
        for fault in ('report','service','discovery','fit_type'):
            c=act7_records()
            if fault=='report':c['report']['visitedSlots']=[]
            if fault=='service':c['payload']['services'].pop()
            if fault=='discovery':c['payload']['services'][-1]['discovered']=True
            if fault=='fit_type':c['payload']['services'][-1]['outcome']['fits']=1
            self.assertTrue(self.progress(c),fault)

    def test_inherited_final_mismatch_is_preserved_without_claiming_new_mismatch(self):
        c=act7_records('EndingReading',3,mismatch=True);self.assertFalse(c['report']['odysseusMismatchObserved'])
        self.assertEqual([],self.progress(c));self.assertEqual([],self.ending(c))
        c['payload']['services'][-1]['outcome']['fits']=True;self.assertTrue(self.progress(c))

    def test_resume_before_prince_choice_can_still_make_its_unmade_choice(self):
        c=act7_records();p=c['payload'];source=copy.deepcopy(p)
        source.update(phase='Choice',slotIndex=5,lineIndex=0,princeChoice=None,farewell=None,memories=[],consumedLines=[])
        source['services']=source['services'][:5];source['completedOccurrences']=source['completedOccurrences'][:5];source['selectedEncounters']=source['selectedEncounters'][:6]
        c['source']=source;c['report']['visitedSlots']=list(range(5,47))
        self.assertEqual([],self.progress(c))

    def test_act6_memory_boundary_stays_before_five_remaining_services_in_act7(self):
        c=act6_records();c['report'].update(scope='through_act_07',scene='Bar_Q1_Act7',services=47,expectedServices=47,departures=47,memories=7)
        c['captures'][-1].update(slot=46,departures=47,memories=7,odysseusVisible=True)
        c['captures'] += [dict(label=f'act7-{p}-{s}',slot=s,phase=p,textId='authored') for s in range(43,47) for p in ('Intro','Farewell')]
        c['checkpoints'] += [dict(name=f'after-departure-{s}',currentHash='a'*64) for s in range(43,47)]
        c['checkpoints'][-5]['name']='act7-complete'
        self.assertEqual([],proof.validate_records(**c))
        c['captures'][14]['services']=46
        self.assertTrue(proof.validate_records(**c))

    @unittest.skipUnless(shutil.which('powershell'),'Windows launcher preflight')
    def test_odysseus_launcher_rejects_other_acts_and_combined_fixtures_before_player_launch(self):
        launcher=Path(__file__).with_name('run_campaign_proof.ps1')
        for flags in (['-Act','6'],['-Act','7','-FailHanky']):
            result=subprocess.run(['powershell','-NoProfile','-File',str(launcher),'-EvidenceName','validator-preflight-no-player','-OdysseusMismatch']+flags,capture_output=True)
            self.assertNotEqual(0,result.returncode)
            self.assertIn(b'Odysseus mismatch is an exclusive ACT 7 fixture',result.stdout+result.stderr)


if __name__=='__main__':unittest.main()
