"""Validate ACT 2 Player artifacts without conflating simulated and OS input."""
import argparse
import hashlib
import json
from pathlib import Path
import struct


def validate_records(report, captures, checkpoints):
    errors=[]
    def require(condition, message):
        if not condition:
            errors.append(message)
    require(report.get('status')=='PASS' and not report.get('errors'), 'Player did not pass')
    require(report.get('scope')=='through_act_02' and report.get('scene')=='Bar_Q1_Act2', 'Wrong campaign scope or scene')
    require('Simulated production InputSystem' in report.get('tier','') and 'NOT native OS input' in report.get('tier',''), 'Evidence tier must exclude native OS input')
    require(report.get('departures')==25 and report.get('known')==7 and report.get('memories')==2, 'ACT 2 completion counts are incomplete')
    require(report.get('services')==report.get('expectedServices') and report.get('services',0)>=25, 'Wrong actual service count')
    require(not any(row.get('overflow') for row in captures), 'Text overflow')
    require(not any(row.get('invalidRenderers') for row in captures), 'Visible asset/material failure')
    names=[row['name'] for row in checkpoints]
    require(len(names)==len(set(names)), 'Duplicate checkpoint name')
    require('act2-complete' in names, 'Missing completed checkpoint')
    require(any(row['label']=='earned-memory' and row['phase']=='Complete' for row in captures), 'Missing earned memory capture')
    for row in checkpoints:
        require(len(row.get('currentHash',''))==64, 'Checkpoint lacks published current generation: '+row['name'])
    for slot in report.get('visitedSlots',[]):
        if slot<17:
            continue
        for stage in ('before-service','after-service','after-departure'):
            require(f'{stage}-{slot}' in names, f'Missing {stage} checkpoint at slot {slot}')
    if report.get('startPhase')!='Complete':
        for slot in range(max(17,report.get('startDepartures',0)),25):
            require(f'after-departure-{slot}' in names,f'Missing natural departure checkpoint at slot {slot}')
        for slot in range(max(17,report.get('startSlot',0)),25):
            # A Continue at Brewing or later cannot replay already-consumed introductory lines.
            intro_required=slot>report.get('startSlot',0) or report.get('startPhase') in ('Welcome','NightIntro','NightEnd','Arriving','Intro')
            farewell_required=slot>report.get('startSlot',0) or report.get('startPhase')!='Departing'
            if intro_required:
                require(any(c['slot']==slot and c['phase']=='Intro' and c.get('textId') for c in captures), f'Missing authored intro at slot {slot}')
            if farewell_required:
                require(any(c['slot']==slot and c['phase']=='Farewell' and c.get('textId') for c in captures), f'Missing authored farewell at slot {slot}')
    if report.get('deliberateFailure'):
        require(report.get('retryObserved') and report.get('services',0)>=26, 'Required failure did not reach retry')
        require({'failed-service-21','hanky-retry'}<=set(names), 'Missing required failure/retry saves')
        require(any(c['label']=='hanky-required-retry' for c in captures), 'Missing required retry capture')
    return errors


ROOT=Path(__file__).resolve().parents[1]
FOLDERS=('Unity/Assets','Unity/Packages','Unity/ProjectSettings','Content/Campaign','Validation/Builds/Act2Production')


def validate_authored_text(capture, authored):
    source=authored.get(capture.get('textId'))
    if source is None:
        return ['Unresolved authored text: '+capture['label']]
    if source['role']!=capture.get('textRole') or source['value'] not in capture.get('visibleText',[]):
        return ['Authored text/role not rendered verbatim: '+capture['label']]
    return []


def validate_completed_continue(report, origin_files, final_files):
    if report.get('startPhase')!='Complete':
        return []
    errors=[]
    if origin_files!=final_files:
        errors.append('Completed Continue changed published save generations')
    if report.get('visitedSlots'):
        errors.append('Completed Continue replayed a service')
    return errors


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def inventory():
    return {path.relative_to(ROOT).as_posix():sha(path) for folder in FOLDERS
            for path in sorted((ROOT/folder).rglob('*')) if path.is_file()}


def read(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))


def lines(path):
    return [json.loads(line) for line in path.read_text(encoding='utf-8-sig').splitlines() if line]


def verify(folder):
    report=read(folder/'player-result.json'); captures=lines(folder/'captures.jsonl'); checkpoints=lines(folder/'checkpoints.jsonl')
    errors=validate_records(report,captures,checkpoints)
    def require(condition,message):
        if not condition:
            errors.append(message)
    expected=read(folder/'expected-run.json'); process=read(folder/'process.json')
    runtime_errors=folder/'errors.log'
    require(not runtime_errors.exists() or not runtime_errors.read_text(encoding='utf-8-sig').strip(),'Runtime error log is not empty')
    require(process['exitCode']==0 and not process['timedOut'],'Player process did not complete')
    require(expected['height'] in (720,1080) and expected['width']==expected['height']*16//9,'Wrong target resolution')
    require(read(folder/'inventory.json')['files']==inventory(),'Source/Player inventory changed')
    executable=ROOT/'Validation/Builds/Act2Production/SurlhwaAct2.exe'
    require(expected['executableSha256'].lower()==sha(executable),'Executable changed')
    args=expected['arguments']
    def argument(key):
        require(args.count(key)==1,'Missing or duplicate argument: '+key)
        return args[args.index(key)+1] if key in args else ''
    require(Path(argument('-campaignEvidenceRoot')).resolve()==folder.resolve(),'Wrong evidence root')
    require(argument('-campaignProof')==expected['fixture']==report['fixture'],'Wrong proof fixture')
    save_root=Path(argument('-campaignSaveRoot'))
    require(save_root.resolve()==(folder/'save').resolve(),'Save is not isolated under this run')
    legacy_root=Path(argument('-campaignLegacySaveRoot'))
    require(legacy_root.resolve()==(folder/('origin' if expected['start']=='Legacy' else 'legacy-empty')).resolve(),'Legacy lookup is not isolated under this run')
    if expected['start']!='Legacy':
        require(not (legacy_root/'slot1/current.json').exists(),'Non-legacy run received unexpected legacy progress')
    pngs=list(folder.glob('*.png'))
    require(len(pngs)==report['captures']==len(captures),'Capture membership incomplete')
    require({c['image'] for c in captures}=={p.name for p in pngs},'Capture manifest/image mismatch')
    for png in pngs:
        with png.open('rb') as stream:
            header=stream.read(24)
        require(header[:8]==b'\x89PNG\r\n\x1a\n' and struct.unpack('>II',header[16:24])==(expected['width'],expected['height']),'Wrong PNG dimensions: '+png.name)
    for checkpoint in checkpoints:
        for generation in ('current','previous'):
            path=folder/'checkpoints'/checkpoint['name']/'slot1'/f'{generation}.json'
            digest=checkpoint.get(generation+'Hash')
            require((path.is_file() and sha(path)==digest) if digest else not path.exists(),'Checkpoint archive changed: '+str(path))
    payload=read(save_root/'slot1/current.json')['payload']
    require(payload['phase']=='Complete' and len(payload['services'])==report['services'] and len(payload['completedOccurrences'])==25,'Final published save is incomplete')
    require(len(payload['knownReceipts'])==7 and len(payload['memories'])==2,'Final published knowledge/memory differs')
    require(any(m['id']=='mem_02' and m['variant']=='swing_empty_place' for m in payload['memories']),'Missing earned MEM-02')
    content=read(ROOT/'Content/Campaign/act2.json'); authored={t['id']:t for t in content['texts']}; encounters={e['id']:e for e in content['encounters']}
    hanky=[s for s in payload['services'] if s['outcome']['recipeId']=='recipe_hanky_panky']
    require(any(s['outcome']['preparation']=='Good' and s['snapshot']['garnish']=='orange_zest' for s in hanky),'Missing actual Hanky preparation/zest')
    if report['deliberateFailure']:
        require(any(s['outcome']['preparation']!='Good' and s['snapshot']['garnish']!='orange_zest' for s in hanky),'Missing actual failed Hanky preparation')
    for row in captures:
        if row['slot']>=17 and row['phase'] in ('Intro','Farewell'):
            errors.extend(validate_authored_text(row,authored))
            encounter=encounters[payload['selectedEncounters'][row['slot']]]
            sequence=[line['textId'] for line in encounter['intro']] if row['phase']=='Intro' else encounter['farewell']
            require(0<=row['line']<len(sequence) and row['textId']==sequence[row['line']],'Authored line order changed: '+row['label'])
            require(set(row['guestIds'])<=set(row['visibleActors']),'Missing actual actor: '+row['label'])
            if row['textRole']=='narration':
                require(not row['displayedSpeaker'],'Narration incorrectly attributed: '+row['label'])
            if len(row['guestIds'])==2:
                require(row['servingVessels']==1,'Paired encounter must retain one serving vessel')
            if row['memories']<2:
                require(not row['loversVisible'],'MEM-02 appeared before earned departure')
        if row['slot']>=17 and row['label'].startswith('prepared-'):
            encounter=encounters[payload['selectedEncounters'][row['slot']]]
            require(authored[encounter['summaryTextId']]['value'] in row['visibleText'],'Player request not visible while preparing: '+row['label'])
    if report['startPhase']!='Complete':
        initial=next(c for c in captures if c['label']=='act2-start')
        for slot in range(max(17,report['startSlot']),25):
            encounter=encounters[payload['selectedEncounters'][slot]]
            for phase,sequence in (('Intro',[line['textId'] for line in encounter['intro']]),('Farewell',encounter['farewell'])):
                if slot==report['startSlot']:
                    if phase=='Intro' and report['startPhase'] not in ('Welcome','NightIntro','NightEnd','Arriving','Intro'):
                        continue
                    if phase=='Farewell' and report['startPhase']=='Departing':
                        continue
                    if report['startPhase']==phase:
                        sequence=sequence[initial['line']:]
                observed={c['textId'] for c in captures if c['slot']==slot and c['phase']==phase}
                require(set(sequence)<=observed,f'Incomplete authored {phase} coverage at slot {slot}')
    complete=next((c for c in captures if c['label']=='earned-memory'),{})
    require(complete.get('loversVisible') and complete.get('memories')==2,'Earned memory is not visible')
    origin_path=folder/'checkpoint-origin.json'
    if expected['start']!='New':
        origin=read(origin_path)
        for name,digest in origin['files'].items():
            require(sha(Path(origin['source'])/'slot1'/name)==digest,'Source checkpoint changed')
            require(sha(folder/'origin'/'slot1'/name)==digest,'Copied checkpoint provenance changed')
        final_files={name:sha(save_root/'slot1'/name) for name in ('current.json','previous.json') if (save_root/'slot1'/name).is_file()}
        errors.extend(validate_completed_continue(report,origin['files'],final_files))
        if expected['start']=='Legacy':
            require(Path(argument('-campaignLegacySaveRoot')).resolve()==(folder/'origin').resolve(),'Wrong legacy source')
    return errors


def main():
    parser=argparse.ArgumentParser(); parser.add_argument('mode',choices=('inventory','verify-inventory','verify')); parser.add_argument('path',type=Path)
    args=parser.parse_args()
    if args.mode=='inventory':
        args.path.write_text(json.dumps(dict(files=inventory()),indent=2),encoding='utf-8'); print('ACT2_INVENTORY_WRITTEN'); return
    errors=([] if read(args.path)['files']==inventory() else ['Source/Player inventory changed']) if args.mode=='verify-inventory' else verify(args.path)
    if errors:
        print(json.dumps(dict(status='FAIL',errors=errors),indent=2)); raise SystemExit(2)
    print('ACT2_PROOF_PASS' if args.mode=='verify' else 'ACT2_INVENTORY_UNCHANGED')


if __name__=='__main__':
    main()
