"""Compare the same bounded, pose-held Q1 harness; never label it normal-play GC."""
from pathlib import Path
import json
V=Path(__file__).resolve().parents[1]/'Validation'
def read(p): return json.loads(p.read_text(encoding='utf-8-sig'))
prior=read(V/'Evidence/q1-final-ordinary-04/result.json')
rows=[]
for run in ('q1-gate2-perf-05','q1-gate2-perf-06'):
    current=read(V/'Evidence'/run/'result.json')
    process=read(V/'Evidence'/run/'process.json')
    if current['errors'] or process['exitCode']!=0 or process['timedOut']: raise SystemExit('Incomplete bounded measurement')
    for now in current['measurements']:
        before=next(r for r in prior['measurements'] if r['width']==now['width'])
        rows.append(dict(run=run,width=now['width'],previous=before,current=now,
            p50ChangePercent=(now['p50']/before['p50']-1)*100,
            p95ChangePercent=(now['p95']/before['p95']-1)*100,
            allocatedChangeMiB=(now['allocated']-before['allocated'])/1048576))
result=dict(tier='Same pose-held offscreen Player harness; uncapped frame samples; not normal-play GC or GPU timing certification',
    priorRun='q1-final-ordinary-04',currentRuns=['q1-gate2-perf-05','q1-gate2-perf-06'],cpu=current['cpu'],gpu=current['gpu'],rows=rows,
    limits=['UI and controller held during measurement','No simultaneous media encoding or other Q1 Player capture','GC=0 applies only to the preallocated held-pose sample','Integrated normal play and recorded media carry additional UI/input/capture costs'])
(V/'Manifests/gate2-q1-performance.json').write_text(json.dumps(result,indent=2)+'\n')
for row in rows:
    n=row['current']
    print(row['run'],row['width'],'p50/p95',round(n['p50'],3),round(n['p95'],3),'renderers/materials',n['renderers'],n['materials'],'GC',n['gcMean'],'allocated MiB',round(n['allocated']/1048576,2))
