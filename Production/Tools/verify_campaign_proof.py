"""Current cumulative-campaign evidence validation; no native-input claim."""
import argparse
import json
from pathlib import Path
import struct

from verify_act2_proof import read, lines, sha, validate_authored_text, validate_completed_continue

ROOT=Path(__file__).resolve().parents[1]
PROFILES={
    'through_act_02':dict(act=2,slots=25,known=7,memories=2,prefix='act2',scene='Bar_Q1_Act2'),
    'through_act_03':dict(act=3,slots=29,known=7,memories=3,prefix='act3',scene='Bar_Q1_Act3')}


def profile_for_act(act):
    return PROFILES[f'through_act_{act:02}']


def validate_records(report,captures,checkpoints):
    errors=[]
    def require(condition,message):
        if not condition: errors.append(message)
    profile=PROFILES.get(report.get('scope'))
    if profile is None: return ['Unsupported campaign evidence scope']
    total=profile['slots'];prefix=profile['prefix']
    require(report.get('status')=='PASS' and not report.get('errors'),'Player did not pass')
    require(report.get('scene')==profile['scene'],'Wrong campaign scene')
    require('Simulated production InputSystem' in report.get('tier','') and 'NOT native OS input' in report.get('tier',''),'Evidence tier must exclude native OS input')
    require(report.get('departures')==total and report.get('known')==profile['known'] and report.get('memories')==profile['memories'],'Campaign completion counts are incomplete')
    require(report.get('services')==report.get('expectedServices') and report.get('services',0)>=total,'Wrong actual service count')
    require(not any(c.get('overflow') for c in captures),'Text overflow')
    require(not any(c.get('invalidRenderers') for c in captures),'Visible asset/material failure')
    names=[c['name'] for c in checkpoints]
    require(len(names)==len(set(names)),'Duplicate checkpoint name')
    require(prefix+'-complete' in names,'Missing completed checkpoint')
    require(any(c['label']=='earned-memory' and c['phase']=='Complete' for c in captures),'Missing earned memory capture')
    for c in checkpoints: require(len(c.get('currentHash',''))==64,'Missing published generation: '+c['name'])
    for slot in report.get('visitedSlots',[]):
        if slot>=17:
            for stage in ('before-service','after-service','after-departure'):
                require(f'{stage}-{slot}' in names,f'Missing {stage} checkpoint at slot {slot}')
    if report.get('startPhase')!='Complete':
        for slot in range(max(17,report.get('startDepartures',0)),total):
            require(f'after-departure-{slot}' in names,f'Missing natural departure checkpoint at slot {slot}')
        for slot in range(max(17,report.get('startSlot',0)),total):
            for phase in ('Intro','Farewell'):
                if slot==report.get('startSlot') and ((phase=='Intro' and report.get('startPhase') not in ('Welcome','NightIntro','NightEnd','Arriving','Intro')) or (phase=='Farewell' and report.get('startPhase')=='Departing')):
                    continue
                require(any(c['slot']==slot and c['phase']==phase and c.get('textId') for c in captures),f'Missing authored {phase} at slot {slot}')
    if report.get('deliberateFailure'):
        require(report.get('retryObserved') and {'failed-service-21','hanky-retry'}<=set(names),'Missing required Hanky failure/retry')
    if profile['act']==3:
        final=next((c for c in captures if c['label']=='earned-memory'),{})
        require(final.get('windmillVisible') and final.get('memories')==3,'Missing final windmill memory visibility')
        if report.get('startDepartures',0)<28:
            memory=next((c for c in captures if c['label']=='act3-earned-memory-after-27'),{})
            require(memory.get('slot')==28 and memory.get('phase') in ('Arriving','NightEnd','NightIntro') and memory.get('departures')==28 and memory.get('memories')==3 and memory.get('windmillVisible'),'Missing immediate MEM-03 after Quixote departure')
            # Inherited failed attempts can make services exceed departures, so bind the
            # intermediate count to the final total minus exactly the ordinary service.
            require(memory.get('services')==report.get('services',0)-1,'MEM-03 capture occurred after final ordinary service')
    return errors


def inherited_quixote_evidence(payload,source_payload):
    before=[s for s in (source_payload or {}).get('services',[]) if s.get('occurrenceId','').endswith(':27')]
    after=[s for s in payload.get('services',[]) if s.get('occurrenceId','').endswith(':27')]
    preserved=len(before)==1 and before==after
    return dict(servicePresent=bool(before),servicePreserved=preserved,
                mismatchPreserved=preserved and before[0]['outcome'].get('fits') is False)


def validate_act3_progress(payload,report,source_payload=None):
    errors=[]
    def require(condition,message):
        if not condition: errors.append(message)
    completed=payload.get('completedOccurrences',[]);memories=payload.get('memories',[]);services=payload.get('services',[])
    require(payload.get('phase')=='Complete' and len(completed)==29 and len(set(completed))==29,'Incomplete ACT 3 departures')
    require(len(payload.get('knownReceipts',{}))==7 and len(memories)==3,'Wrong ACT 3 knowledge/memory count')
    memory=[m for m in memories if m.get('id')=='mem_03']
    quixote=[s for s in services if s.get('occurrenceId','').endswith(':27')]
    require(len(memory)==1 and memory[0].get('variant')=='windmill_fragment' and memory[0].get('occurrenceId','').endswith(':27') and memory[0].get('occurrenceId') in completed,'MEM-03 not earned by the Quixote departure')
    require(len(quixote)==1,'Quixote alone must have one service and no retry')
    inherited=inherited_quixote_evidence(payload,source_payload)
    require(not inherited['servicePresent'] or inherited['servicePreserved'],'Inherited Quixote service changed during Continue')
    if report.get('quixoteMismatchObserved'):
        require(len(quixote)==1 and quixote[0]['outcome'].get('recipeId')=='recipe_dry_martini' and quixote[0]['outcome'].get('fits') is False and quixote[0]['outcome'].get('preparation')=='Good' and quixote[0]['outcome'].get('qualityGood'),'Missing actual correctly prepared Quixote FIT mismatch')
    expected={25:'recipe_manhattan',26:'recipe_gin_fizz',27:'recipe_dry_martini' if report.get('quixoteMismatchObserved') else 'recipe_hanky_panky',28:'recipe_negroni'}
    for slot in report.get('visitedSlots',list(expected)):
        if slot not in expected: continue
        rows=[s for s in services if s.get('occurrenceId','').endswith(':'+str(slot))]
        fits=not(slot==27 and report.get('quixoteMismatchObserved'))
        require(len(rows)==1 and rows[0]['outcome'].get('recipeId')==expected[slot] and rows[0]['outcome'].get('preparation')=='Good' and rows[0]['outcome'].get('qualityGood') and rows[0]['outcome'].get('fits') is fits,f'Missing real ACT 3 service at slot {slot}')
    return errors


def validate_history_binding(folder,start,history_root,populated_namespaces):
    errors=[]
    if Path(history_root).resolve()!=(Path(folder)/'history').resolve(): errors.append('Campaign history lookup is not isolated under this run')
    expected={'Prior':['campaign-through-act-02'],'Legacy':['campaign-act1'],'New':[],'Checkpoint':[]}.get(start)
    if expected is None or sorted(populated_namespaces)!=expected: errors.append('Unexpected campaign history namespaces')
    return errors


def inventory(act):
    folders=('Unity/Assets','Unity/Packages','Unity/ProjectSettings','Content/Campaign',f'Validation/Builds/Act{act}Production')
    return {p.relative_to(ROOT).as_posix():sha(p) for folder in folders for p in sorted((ROOT/folder).rglob('*')) if p.is_file()}


def verify(folder,act=3):
    report=read(folder/'player-result.json');captures=lines(folder/'captures.jsonl');checkpoints=lines(folder/'checkpoints.jsonl')
    errors=validate_records(report,captures,checkpoints);profile=profile_for_act(act);total=profile['slots']
    def require(condition,message):
        if not condition: errors.append(message)
    expected=read(folder/'expected-run.json');process=read(folder/'process.json')
    require(expected.get('act')==act and report.get('scope')==f'through_act_{act:02}','Wrong requested campaign scope')
    require(process['exitCode']==0 and not process['timedOut'],'Player process did not complete')
    log=folder/'errors.log';require(not log.exists() or not log.read_text(encoding='utf-8-sig').strip(),'Runtime error log is not empty')
    require(expected['height'] in (720,1080) and expected['width']==expected['height']*16//9,'Wrong target resolution')
    require(read(folder/'inventory.json')['files']==inventory(act),'Source/Player inventory changed')
    require(expected['executableSha256'].lower()==sha(ROOT/f'Validation/Builds/Act{act}Production/SurlhwaAct{act}.exe'),'Executable changed')
    args=expected['arguments']
    def argument(key):
        require(args.count(key)==1,'Missing or duplicate argument: '+key)
        return args[args.index(key)+1] if key in args and args.index(key)+1<len(args) else ''
    require(Path(argument('-campaignEvidenceRoot')).resolve()==folder.resolve(),'Wrong evidence root')
    require(argument('-campaignProof')==expected['fixture']==report['fixture'],'Wrong proof fixture')
    save_root=Path(argument('-campaignSaveRoot'));history_root=Path(argument('-campaignHistoryRoot'))
    require(save_root.resolve()==(folder/'save').resolve(),'Save is not isolated under this run')
    populated=[p.name for p in history_root.iterdir() if p.is_dir() and any((p/'slot1'/name).exists() for name in ('current.json','previous.json'))] if history_root.is_dir() else []
    errors.extend(validate_history_binding(folder,expected['start'],history_root,populated))
    require(Path(argument('-campaignLegacySaveRoot')).resolve()==(history_root/'campaign-act1').resolve(),'Legacy fallback is not isolated under campaign history')
    pngs=list(folder.glob('*.png'))
    require(len(pngs)==report['captures']==len(captures) and {p.name for p in pngs}=={c['image'] for c in captures},'Capture manifest/image mismatch')
    for png in pngs:
        with png.open('rb') as stream: header=stream.read(24)
        require(header[:8]==b'\x89PNG\r\n\x1a\n' and struct.unpack('>II',header[16:24])==(expected['width'],expected['height']),'Wrong PNG dimensions: '+png.name)
    for c in checkpoints:
        for generation in ('current','previous'):
            p=folder/'checkpoints'/c['name']/'slot1'/f'{generation}.json';digest=c.get(generation+'Hash')
            require(p.is_file() and sha(p)==digest if digest else not p.exists(),'Checkpoint archive changed: '+str(p))
    payload=read(save_root/'slot1/current.json')['payload']
    require(payload['phase']=='Complete' and len(payload['services'])==report['services'] and len(payload['completedOccurrences'])==total,'Final published save is incomplete')
    require(len(payload['knownReceipts'])==profile['known'] and len(payload['memories'])==profile['memories'],'Final published knowledge/memory differs')
    require(any(m['id']=='mem_02' and m['variant']=='swing_empty_place' for m in payload['memories']),'Missing retained MEM-02')
    hanky=[s for s in payload['services'] if s['outcome']['recipeId']=='recipe_hanky_panky']
    require(any(s['outcome']['preparation']=='Good' and s['snapshot']['garnish']=='orange_zest' for s in hanky),'Missing actual Hanky preparation/zest')
    if report.get('deliberateFailure'):
        require(any(s['outcome']['preparation']!='Good' and s['snapshot']['garnish']!='orange_zest' for s in hanky),'Missing actual failed Hanky preparation')
    if act==3:
        source_payload=read(folder/'origin/slot1/current.json')['payload'] if expected['start']!='New' else None
        errors.extend(validate_act3_progress(payload,report,source_payload))
        if report['startDepartures']<28:
            after=read(folder/'checkpoints/after-departure-27/slot1/current.json')['payload']
            require(len(after['completedOccurrences'])==28 and not any(s['occurrenceId'].endswith(':28') for s in after['services']) and any(m['id']=='mem_03' and m['occurrenceId'].endswith(':27') for m in after['memories']),'After-departure-27 save does not prove memory before final ordinary service')
    if 'quixote-mismatch' in expected['fixture']: require(report.get('quixoteMismatchObserved'),'Requested Quixote mismatch not observed')
    content=read(ROOT/f'Content/Campaign/act{act}.json');authored={t['id']:t for t in content['texts']};encounters={e['id']:e for e in content['encounters']}
    for c in captures:
        if c['slot']>=17 and c['phase'] in ('Intro','Farewell'):
            errors.extend(validate_authored_text(c,authored));encounter=encounters[payload['selectedEncounters'][c['slot']]]
            sequence=[line['textId'] for line in encounter['intro']] if c['phase']=='Intro' else encounter['farewell']
            require(0<=c['line']<len(sequence) and c['textId']==sequence[c['line']],'Authored line order changed: '+c['label'])
            require(set(c['guestIds'])<=set(c['visibleActors']),'Missing actual actor: '+c['label'])
            if c['textRole']=='narration': require(not c['displayedSpeaker'],'Narration incorrectly attributed: '+c['label'])
            if len(c['guestIds'])==2: require(c['servingVessels']==1,'Paired encounter must retain one serving vessel')
            if c['memories']<2: require(not c['loversVisible'],'MEM-02 appeared before earned departure')
            if act==3 and c['slot']<=27: require(not c.get('windmillVisible'),'MEM-03 appeared before Quixote departure')
        if c['slot']>=17 and c['label'].startswith('prepared-'):
            encounter=encounters[payload['selectedEncounters'][c['slot']]]
            require(authored[encounter['summaryTextId']]['value'] in c['visibleText'],'Player request not visible while preparing: '+c['label'])
    if report['startPhase']!='Complete':
        initial=next(c for c in captures if c['label']==profile['prefix']+'-start')
        for slot in range(max(17,report['startSlot']),total):
            encounter=encounters[payload['selectedEncounters'][slot]]
            for phase,sequence in (('Intro',[line['textId'] for line in encounter['intro']]),('Farewell',encounter['farewell'])):
                if slot==report['startSlot']:
                    if phase=='Intro' and report['startPhase'] not in ('Welcome','NightIntro','NightEnd','Arriving','Intro'): continue
                    if phase=='Farewell' and report['startPhase']=='Departing': continue
                    if report['startPhase']==phase: sequence=sequence[initial['line']:]
                require(set(sequence)<={c['textId'] for c in captures if c['slot']==slot and c['phase']==phase},f'Incomplete authored {phase} coverage at slot {slot}')
    complete=next((c for c in captures if c['label']=='earned-memory'),{})
    require(complete.get('loversVisible') and complete.get('memories')==profile['memories'],'Retained memory is not visible')
    if expected['start']!='New':
        origin=read(folder/'checkpoint-origin.json')
        for name,digest in origin['files'].items():
            require(sha(Path(origin['source'])/'slot1'/name)==digest and sha(folder/'origin/slot1'/name)==digest,'Source/copied checkpoint changed')
            if expected['start'] in ('Prior','Legacy'):
                namespace='campaign-through-act-02' if expected['start']=='Prior' else 'campaign-act1'
                require(sha(history_root/namespace/'slot1'/name)==digest,'Isolated historical source changed')
        final={name:sha(save_root/'slot1'/name) for name in ('current.json','previous.json') if (save_root/'slot1'/name).is_file()}
        errors.extend(validate_completed_continue(report,origin['files'],final))
    return errors


def main():
    p=argparse.ArgumentParser();p.add_argument('mode',choices=('inventory','verify-inventory','verify'));p.add_argument('path',type=Path);p.add_argument('--act',type=int,choices=(2,3),default=3);args=p.parse_args()
    if args.mode=='inventory':
        args.path.write_text(json.dumps(dict(act=args.act,files=inventory(args.act)),indent=2),encoding='utf-8');print('CAMPAIGN_INVENTORY_WRITTEN');return
    errors=([] if read(args.path)['files']==inventory(args.act) else ['Source/Player inventory changed']) if args.mode=='verify-inventory' else verify(args.path,args.act)
    if errors: print(json.dumps(dict(status='FAIL',errors=errors),indent=2));raise SystemExit(2)
    if args.mode=='verify' and args.act==3:
        report=read(args.path/'player-result.json');payload=read(args.path/'save/slot1/current.json')['payload'];origin=args.path/'origin/slot1/current.json'
        inherited=inherited_quixote_evidence(payload,read(origin)['payload'] if origin.is_file() else None)
        print(json.dumps(dict(newQuixoteMismatchObserved=report.get('quixoteMismatchObserved',False),
                              inheritedQuixoteServicePreserved=inherited['servicePreserved'],
                              inheritedQuixoteMismatchPreserved=inherited['mismatchPreserved'])))
    print('CAMPAIGN_PROOF_PASS' if args.mode=='verify' else 'CAMPAIGN_INVENTORY_UNCHANGED')


if __name__=='__main__': main()
