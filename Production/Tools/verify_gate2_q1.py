"""Integration closure: protected bytes, targeted results, saves and Player identity."""
from pathlib import Path
import hashlib, json, struct, subprocess, xml.etree.ElementTree as ET
import gate2_q1_preservation

ROOT=gate2_q1_preservation.ROOT; V=ROOT/'Production/Validation'; M=V/'Manifests'; E=V/'Evidence'
def sha(p): return hashlib.sha256(p.read_bytes()).hexdigest()
def read(p): return json.loads(p.read_text(encoding='utf-8-sig'))
old=read(M/'gate2-q1-before.json'); current=gate2_q1_preservation.snapshot(); errors=[]
allowed={
 'Production/README.md','Docs/13_POST_I16_PRODUCTION_GAP_AUDIT.md',
 'Production/Unity/Assets/Surlhwa/Editor/Q1ProductionBuilder.cs',
 'Production/Unity/Assets/Surlhwa/Presentation/Q1ProductionView.cs',
 'Production/Unity/Assets/Surlhwa/Presentation/CampaignMemoryView.cs',
 'Production/Unity/Assets/Surlhwa/Presentation/CampaignPlayerProof.cs',
 'Production/Unity/Assets/Surlhwa/Tests/EditorIntegration/Q1ProductionTests.cs'}
changes=[p for p,h in old['files'].items() if current['files'].get(p)!=h]
errors += ['Unexpected protected change: '+p for p in changes if p not in allowed]
if old['frozen']!=current['frozen']: errors.append('Frozen specification/RC1 change')
audit=ROOT/'Docs/13_POST_I16_PRODUCTION_GAP_AUDIT.md'
prefix=audit.read_bytes().split(b'\n## Approved Q1 ACT 1 Production integration - 2026-10-27\n')[0]
if hashlib.sha256(prefix).hexdigest()!=old['files'][audit.relative_to(ROOT).as_posix()]: errors.append('Historical audit prefix changed')

latest={}; test_runs=[]
for name in ('gate2-q1-tests-01','gate2-q1-tests-02','gate2-q1-tests-03','gate2-q1-tests-04'):
    tree=ET.parse(E/(name+'.xml')); run=tree.getroot(); test_runs.append(dict(name=name,**run.attrib))
    for test in run.iter('test-case'): latest[test.attrib['fullname']]=test.attrib['result']
errors += ['Unresolved test: '+name for name,result in latest.items() if result!='Passed']
build=read(M/'q1-production-build.json')
if build['result']!='Succeeded' or build['errors']: errors.append('Build failure')
package=read(M/'gate2-q1-package.json')
native_stills=0
final_inventory=read(E/'gate2-q1-continue-open-05/inventory.json')['files']
source_equivalence=[]
for row in package['runs']:
    folder=E/row['run']; report=read(folder/'player-result.json'); expected=read(folder/'expected-run.json')
    if report['status']!='PASS' or report['errors']: errors.append('Player failed '+row['run'])
    if expected['executableSha256'].lower()!=sha(V/'Builds/Q1Production/SurlhwaQ1.exe'): errors.append('Player identity mismatch')
    inventory=read(folder/'inventory.json')['files']
    source_prefixes=('Unity/Assets/','Unity/Packages/','Unity/ProjectSettings/','Content/Campaign/')
    source_changes=[p for p in inventory.keys()|final_inventory.keys() if p.startswith(source_prefixes) and inventory.get(p)!=final_inventory.get(p)]
    recorded=row['run']=='gate2-q1-open-720-03'
    qa_only={'Unity/Assets/Surlhwa/Presentation/CampaignPlayerProof.cs','Unity/Assets/Surlhwa/Tests/EditorIntegration/Q1ProductionTests.cs'} if recorded else set()
    if set(source_changes)-qa_only: errors.append('Production source mismatch: '+str(source_changes))
    player_root=V/'Builds'/('Gate2RecordedPlayer' if recorded else 'Q1Production')
    prefix='Validation/Builds/Q1Production/'
    player_matches=all((player_root/p.removeprefix(prefix)).is_file() and sha(player_root/p.removeprefix(prefix))==digest for p,digest in inventory.items() if p.startswith(prefix))
    if not player_matches: errors.append('Exact retained Player inventory mismatch: '+row['run'])
    source_equivalence.append(dict(run=row['run'],exactPlayerRoot=player_root.relative_to(V).as_posix(),exactPlayerMatches=player_matches,sourceDifferences=source_changes,productionSourceAndAssetsIdentical=not bool(set(source_changes)-qa_only)))
    required_q1=['Q1 Production presentation root resolves separately from controller',
      'Q1 restored after final service','earned star follows Q1 facing',
      'focus reveal is created once for services, absent on completed Continue',
      'focus reveal cleaned up after service','active renderers have materials and shaders']
    errors += ['Missing Q1 postcondition in '+row['run']+': '+check for check in required_q1 if check not in report['checks']]
    images=list(folder.glob('*.png')); native_stills+=len(images)
    if len(images)!=report['captures']: errors.append('Native capture count mismatch')
    for path in images:
        with path.open('rb') as stream: header=stream.read(24)
        if header[:8]!=b'\x89PNG\r\n\x1a\n' or struct.unpack('>II',header[16:24])!=(expected['width'],expected['height']): errors.append('Native dimensions mismatch: '+str(path))

continuity=[]
for kind in ('open','quiet'):
    folder=E/('gate2-q1-'+kind+'-continue-save-05'); origin=read(folder/'origin.json')
    unchanged=all(sha(folder/'slot1'/name)==digest for name,digest in origin['files'].items())
    continuity.append(dict(outcome=kind,publishedGenerationsUnchanged=unchanged,origin=origin))
    if not unchanged: errors.append('Completed Continue rewrote '+kind+' save')

outcomes=[]
for kind,folder,variant in [('Open','gate2-q1-open-720-03','star_glowing'),('Quiet','gate2-q1-quiet-save-05','star_unlit')]:
    payload=read(E/folder/'slot1/current.json')['payload']
    visits=[dict(slot=i,outcome=payload['services'][i]['outcome'],layers=len(payload['services'][i]['snapshot']['layers'])) for i in (5,10,16)]
    row=dict(outcome=kind,phase=payload['phase'],services=len(payload['services']),departures=len(payload['completedOccurrences']),known=len(payload['knownReceipts']),memories=payload['memories'],princeVisits=visits)
    outcomes.append(row)
    if payload['phase']!='Complete' or payload['farewell']!=kind or row['services']!=17 or row['departures']!=17 or row['known']!=6: errors.append('Incomplete outcome '+kind)
    if len(row['memories'])!=1 or row['memories'][0]['variant']!=variant: errors.append('Memory mismatch '+kind)
    if visits[0]['outcome']['recipeId']!='recipe_manwol' or visits[0]['layers']!=3: errors.append('First Manwol/Float evidence missing')
    if visits[1]['outcome']['fits']!=(kind=='Open') or not visits[2]['outcome']['fits']: errors.append('Return-Visit Understanding mismatch '+kind)
(M/'gate2-q1-outcomes.json').write_text(json.dumps(outcomes,indent=2)+'\n')

serialized=subprocess.check_output(['git','diff','--name-only','--','*.unity','*.prefab','Production/Unity/ProjectSettings/*'],cwd=ROOT,text=True).splitlines()
if serialized: errors.append('Unexpected serialized changes: '+str(serialized))
archive=V/'Builds/Q1ApprovedPolish'
archive_files={p.relative_to(archive).as_posix():sha(p) for p in archive.rglob('*') if p.is_file()}
approved_inventory=read(E/'q1-final-ordinary-04/inventory.json')
archive_matches=all(approved_inventory.get('Validation/Builds/Q1Production/'+p)==digest for p,digest in archive_files.items())
if not archive_matches: errors.append('Approved polish Player archive mismatch')
result=dict(status='PASS' if not errors else 'FAIL',changedExisting=changes,
    specifications=len(old['frozen']['specifications']),frozenSnapshotUnchanged=old['frozen']==current['frozen'],
    protectedFiles=len(old['files']),preservedNativeEvidenceFiles=sum(p.startswith('Production/Validation/Evidence/') for p in old['files']),
    uniqueTargetedTests=len(latest),targetedTests=latest,testRuns=test_runs,build=build,nativeOriginalStills=native_stills,
    serializedChanges=serialized,completedContinue=continuity,recordedSourceEquivalence=source_equivalence,
    approvedPolishPlayerArchiveMatchesAcceptedInventory=archive_matches,
    approvedPolishPlayerArchive=archive_files,errors=errors)
(M/'gate2-q1-validation.json').write_text(json.dumps(result,indent=2)+'\n')
print(json.dumps({k:v for k,v in result.items() if k not in ('targetedTests','testRuns','completedContinue','approvedPolishPlayerArchive')},indent=2))
if errors: raise SystemExit(1)
