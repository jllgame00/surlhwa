"""Validate real Player artifacts without substituting evidence tiers or stale builds."""
import argparse
import json
from pathlib import Path
from PIL import Image, ImageStat
from slice_inventory import inventory, PRODUCTION

def read(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))

def validate(folder, current):
    expected, process, run = [read(folder / name) for name in ('expected-run.json','process.json','run.json')]
    assert process['exitCode'] == 0 and not process['timedOut'], 'Player failed or timed out'
    assert run['passed'] and not run['errors'], 'Player proof failed'
    assert run['runId'] == expected['runId'] == process['runId'], 'Run identity mismatch'
    assert run['fixture'] == expected['fixture'], 'Fixture mismatch'
    assert read(folder/'inventory.json') == current, 'Source or full Player is stale'
    assert (run['width'],run['height']) == (expected['width'],expected['height']), 'Resolution mismatch'
    captures=[]
    for path in sorted(folder.glob('[0-9][0-9]-*.json')):
        capture=read(path)
        assert capture['runId']==run['runId'], 'Capture from another run'
        assert not capture['actComplete'] and capture['memories']==0, 'Unearned progression'
        assert capture['eventSystems']==1 and capture['audioListeners']==1, 'Duplicate input/audio ownership'
        assert not capture['overflowText'], f'Text overflow: {path.name}'
        if capture['textRole'] in ('narration','mixed'):
            assert capture['displayedSpeaker']=='', 'Narration attributed to guest'
        if capture['textRole']=='request_summary':
            assert capture['displayedSpeaker']=='손님이 바라는 한 잔', 'Summary attributed to guest'
        if run['fixture']!='save-error':
            assert not capture['saveError'], 'Save/startup error in ordinary proof'
        with Image.open(path.with_suffix('.png')) as picture:
            assert picture.size == (run['width'],run['height']), 'Wrong capture dimensions'
            assert max(ImageStat.Stat(picture).stddev)>5, 'Blank or invalid rendered capture'
        captures.append((path.name,capture))
    assert captures, 'No native captures'
    if run['fixture'] in ('seolya','manhattan','dry','weak','unknown','missing-actor'):
        if not expected.get('stopAt'):
            final=captures[-1][1]
            assert final['phase']=='Complete' and final['services']==2, 'Incomplete encounter flow'
        first=next((c for n,c in captures if 'reaction-0' in n),None)
        if first:
            recipe={'seolya':'recipe_test_stir','dry':'recipe_dry_martini','unknown':''}.get(run['fixture'],'recipe_manhattan')
            assert first['recipe']==recipe, 'Wrong first drink'
            assert first['fits']==(run['fixture'] not in ('dry','unknown')), 'Wrong FIT'
        if run['fixture']=='dry' and not expected.get('stopAt'):
            discoveries=[c for n,c in captures if 'discovery' in n]
            assert len(discoveries)==1 and discoveries[0]['known']==3, 'Discovery not exactly once'
            before=next(c for n,c in captures if 'guide-before-0' in n)
            assert 'Dry Martini' not in '\n'.join(before['visibleText']), 'Partial identity leaked'
    if run['fixture']=='lifecycle':
        assert len(set(run['lifecycleRunIds']))==20, 'Restart evidence incomplete'
        assert len(run['uiRoundTripMilliseconds'])>=20, 'Transition observations incomplete'
    return {'folder':folder.name,'fixture':run['fixture'],'stopAt':expected.get('stopAt'),'resolution':[run['width'],run['height']],'captures':len(captures),'runId':run['runId'],'tier':run['tier'],'p95FrameMs':run['p95ms'],'maxSaveMs':max(run['saveMilliseconds'],default=0)}

if __name__=='__main__':
    parser=argparse.ArgumentParser()
    parser.add_argument('folders',nargs='+')
    parser.add_argument('--report',type=Path)
    args=parser.parse_args()
    current=inventory()
    results=[]
    for name in args.folders:
        results.append(validate(PRODUCTION/'Validation/Evidence'/name,current))
    result={'status':'PASS','scope':'Player simulated-input/rendered artifacts only; does not approve native OS input or human feel','runs':results,'sourceSha256':current['source']['sha256'],'playerSha256':current['player']['sha256']}
    if args.report:
        args.report.write_text(json.dumps(result,indent=2),encoding='utf-8')
    print(f'PLAYER EVIDENCE PASS: {len(results)} runs, {sum(x["captures"] for x in results)} native stills; complete current source/Player identity')
