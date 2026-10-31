"""Current cumulative-campaign evidence validation; no native-input claim."""
import argparse
import json
from pathlib import Path
import struct

from verify_act2_proof import read, lines, sha, validate_authored_text, validate_completed_continue

ROOT=Path(__file__).resolve().parents[1]
PROFILES={
    'through_act_02':dict(act=2,slots=25,known=7,memories=2,prefix='act2',scene='Bar_Q1_Act2'),
    'through_act_03':dict(act=3,slots=29,known=7,memories=3,prefix='act3',scene='Bar_Q1_Act3'),
    'through_act_04':dict(act=4,slots=33,known=8,memories=4,prefix='act4',scene='Bar_Q1_Act4'),
    'through_act_05':dict(act=5,slots=37,known=8,memories=5,prefix='act5',scene='Bar_Q1_Act5'),
    'through_act_06':dict(act=6,slots=43,known=9,memories=6,prefix='act6',scene='Bar_Q1_Act6')}


def profile_for_act(act):
    return PROFILES[f'through_act_{act:02}']


def validate_records(report,captures,checkpoints,expected_supply=None):
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
    if profile['act']>=3:
        final=next((c for c in captures if c['label']=='earned-memory'),{})
        require(final.get('windmillVisible') and final.get('memories')==profile['memories'],'Missing final windmill memory visibility')
        if report.get('startDepartures',0)<28:
            memory=next((c for c in captures if c['label']=='act3-earned-memory-after-27'),{})
            require(memory.get('slot')==28 and memory.get('phase') in ('Arriving','NightEnd','NightIntro') and memory.get('departures')==28 and memory.get('memories')==3 and memory.get('windmillVisible'),'Missing immediate MEM-03 after Quixote departure')
            # Inherited failed attempts can make services exceed departures, so bind the
            # intermediate count to the final total minus exactly the ordinary service.
            if profile['act']==3: require(memory.get('services')==report.get('services',0)-1,'MEM-03 capture occurred after final ordinary service')
    if profile['act']>=4:
        require(final.get('hamletVisible'),'Missing final Hamlet memory visibility')
        if profile['act']<=5 and 29 in report.get('visitedSlots',[]):
            supplied=set(report.get('suppliedIngredients',[]))
            if expected_supply is not None: require(supplied==set(expected_supply),'Reported physical supply differs from loaded content')
            pages=[c for c in captures if c['label'].startswith('act4-shelf-page-')]
            require(bool(supplied) and supplied==set(report.get('shelfSelections',[])),'Not every supplied ingredient was selected through visible controls')
            require(len(pages)>=2 and supplied=={i for c in pages for i in c.get('visibleIngredients',[])},'Missing visible physical shelf page coverage')
        if report.get('frenchFailure'):
            require(report.get('frenchRetryObserved') and 'failed-service-29' in names,'Missing actual French failure/retry')
        if report.get('frenchRetryObserved'):
            require({'french-retry','french-retry-read'}<=set(names) and any(c['phase']=='Retry' and c.get('textId')=='text_rc1_0039' for c in captures),'Missing authored French retry reading')
    if profile['act']>=5:
        require(final.get('faustVisible'),'Missing final Faust memory visibility')
    if profile['act']==5:
        # A report cannot omit services to opt out of their evidence checks.
        # ACT5 recommendations accept one service; a post-service reload resumes
        # its reading/departure without serving that already committed cup again.
        after_service=report.get('startPhase') in ('Reaction','Result','Discovery','Farewell','Departing')
        first=max(33,report.get('startSlot',0)+(1 if after_service else 0))
        expected=[] if report.get('startPhase')=='Complete' else list(range(first,total))
        require(sorted(x for x in report.get('visitedSlots',[]) if x>=33)==expected,'Reported ACT 5 service visits do not match the resumed phase')
    if profile['act']>=6:
        require(final.get('simCheongVisible'),'Missing final SimCheong memory visibility')
        if report.get('startDepartures',0)<42:
            memory=next((c for c in captures if c['label']=='act6-earned-memory-after-41'),{})
            require(memory.get('slot')==42 and memory.get('phase') in ('Arriving','NightEnd','NightIntro') and memory.get('departures')==42 and memory.get('memories')==6 and memory.get('simCheongVisible'),'Missing immediate MEM-06 after SimCheong departure')
            require(memory.get('services')==report.get('services',0)-1,'MEM-06 capture occurred after final ordinary service')
        if 40 in report.get('visitedSlots',[]):
            supplied=set(report.get('suppliedIngredients',[]));pages=[c for c in captures if c['label'].startswith('act6-shelf-page-')]
            if expected_supply is not None: require(supplied==set(expected_supply),'Reported physical supply differs from loaded content')
            require(bool(supplied) and supplied==set(report.get('shelfSelections',[])),'Not every supplied ACT 6 ingredient was selected through visible controls')
            require(len(pages)>=2 and supplied=={i for c in pages for i in c.get('visibleIngredients',[])},'Missing ACT 6 physical shelf page coverage')
        if report.get('lastWordFailure'):require(report.get('lastWordRetryObserved') and 'failed-service-40' in names,'Missing actual Last Word failure/retry')
        if report.get('lastWordRetryObserved'):
            require({'last-word-retry','last-word-retry-read'}<=set(names) and any(c['phase']=='Retry' and c.get('textId')=='text_rc1_0059' for c in captures),'Missing authored Last Word retry reading')
    return errors


def inherited_service_evidence(payload,source_payload,slot):
    before=[s for s in (source_payload or {}).get('services',[]) if s.get('occurrenceId','').endswith(':'+str(slot))]
    after=[s for s in payload.get('services',[]) if s.get('occurrenceId','').endswith(':'+str(slot))]
    preserved=len(before)==1 and before==after
    return dict(servicePresent=bool(before),servicePreserved=preserved,
                mismatchPreserved=preserved and before[0]['outcome'].get('fits') is False)


def inherited_quixote_evidence(payload,source_payload):
    return inherited_service_evidence(payload,source_payload,27)


def validate_french_snapshot(snapshot):
    amounts=snapshot.get('amounts',{});expected={'gin':30,'fresh_lemon_juice':15,'simple_syrup':15,'champagne':60}
    trace=snapshot.get('trace',[]);shake=snapshot.get('shake') or {}
    strains=[i for i,t in enumerate(trace) if t.get('kind')=='Strain' and t.get('source')=='shaker' and t.get('target')=='Serving' and t.get('acceptedMl',0)>0]
    pours=[i for i,t in enumerate(trace) if t.get('kind')=='Pour' and t.get('target')=='Serving' and t.get('components',{}).get('champagne',0)>0]
    gentle=[i for i,t in enumerate(trace) if t.get('kind')=='GentleStir' and t.get('target')=='Serving']
    return (set(amounts)==set(expected) and all(abs(amounts[k]-ml)<.25 for k,ml in expected.items()) and
            not snapshot.get('hasIce') and snapshot.get('garnish') is None and shake.get('howGood') and shake.get('qualityGood') and
            set(shake.get('amounts',{}))==set(expected)-{'champagne'} and
            bool(strains and pours and gentle) and max(strains)<min(pours) and max(pours)<max(gentle) and
            abs(sum(trace[i]['components']['champagne'] for i in pours)-60)<.25)


def validate_act4_progress(payload,report,source_payload=None):
    errors=[]
    def require(condition,message):
        if not condition: errors.append(message)
    services=payload.get('services',[]);completed=payload.get('completedOccurrences',[]);memories=payload.get('memories',[])
    require(payload.get('phase')=='Complete' and len(completed)==33 and len(set(completed))==33,'Incomplete ACT 4 departures')
    require(len(payload.get('knownReceipts',{}))==8 and len(memories)==4,'Wrong ACT 4 knowledge/memory count')
    memory=[m for m in memories if m.get('id')=='mem_04']
    require(len(memory)==1 and memory[0].get('variant')=='stage_skull_shadow' and memory[0].get('occurrenceId','').endswith(':32') and memory[0].get('occurrenceId') in completed,'MEM-04 not earned by Hamlet natural departure')
    inherited=(source_payload or {}).get('services',[])
    require(services[:len(inherited)]==inherited,'Inherited service history changed during Continue')
    french=[s for s in services if s.get('occurrenceId','').endswith(':29')]
    successful=[s for s in french if s['outcome'].get('preparation')=='Good' and s['outcome'].get('qualityGood')]
    failures=[s for s in french if s not in successful]
    require(len(successful)==1 and successful[0].get('discovered') is True and successful[0]['outcome'].get('fits') is True and successful[0]['outcome'].get('recipeId')=='recipe_french_75','French discovery must follow one successful actual service')
    require(all(s.get('discovered') is False and s['outcome'].get('recipeId')=='recipe_french_75' and not any(t.get('kind')=='GentleStir' for t in s.get('snapshot',{}).get('trace',[])) for s in failures),'Failed French finish granted discovery or did not omit GentleStir')
    if failures:
        require(any(x.endswith(':29:Retry:0:text_rc1_0039') for x in payload.get('consumedLines',[])),'Failed French discovery lacks consumed authored retry reading')
    if report.get('frenchFailure'): require(bool(failures),'Missing actual failed French service')
    for s in services:
        if s['outcome'].get('recipeId')=='recipe_french_75' and s['outcome'].get('preparation')=='Good':
            require(validate_french_snapshot(s.get('snapshot',{})),'French service lacks actual Shaker Strain / measured Champagne / fresh GentleStir')
    hamlet=[s for s in services if s.get('occurrenceId','').endswith(':32')]
    require(len(hamlet)==1,'Final Hamlet must have one service and no retry')
    recipes={30:'recipe_dry_martini',31:'recipe_dry_martini',32:'recipe_french_75' if report.get('hamletMismatchObserved') else 'recipe_dry_martini'}
    for slot in report.get('visitedSlots',[]):
        if slot not in recipes:continue
        rows=[s for s in services if s.get('occurrenceId','').endswith(':'+str(slot))]
        require(len(rows)==1 and rows[0]['outcome'].get('recipeId')==recipes[slot] and rows[0]['outcome'].get('preparation')=='Good' and rows[0]['outcome'].get('qualityGood') and rows[0]['outcome'].get('fits') is not(slot==32 and report.get('hamletMismatchObserved',False)),f'Missing actual ACT 4 service at slot {slot}')
    if report.get('hamletMismatchObserved'):
        require(len(hamlet)==1 and hamlet[0]['outcome'].get('recipeId')=='recipe_french_75' and hamlet[0]['outcome'].get('fits') is False,'Missing actual Hamlet mismatch')
    return errors


def validate_act5_progress(payload,report,source_payload=None):
    errors=[]
    def require(condition,message):
        if not condition: errors.append(message)
    services=payload.get('services',[]);completed=payload.get('completedOccurrences',[]);memories=payload.get('memories',[])
    require(payload.get('phase')=='Complete' and len(completed)==37 and len(set(completed))==37,'Incomplete ACT 5 departures')
    require(len(payload.get('knownReceipts',{}))==8 and len(memories)==5,'Wrong ACT 5 knowledge/memory count')
    memory=[m for m in memories if m.get('id')=='mem_05']
    require(len(memory)==1 and memory[0].get('variant')=='faint_circle_broken_glass_red_light' and memory[0].get('occurrenceId','').endswith(':36') and memory[0].get('occurrenceId') in completed,'MEM-05 not earned by Faust natural departure')
    inherited=(source_payload or {}).get('services',[])
    require(services[:len(inherited)]==inherited,'Inherited service history changed during Continue')
    for s in services:
        if s['outcome'].get('recipeId')=='recipe_french_75' and s['outcome'].get('preparation')=='Good':
            require(validate_french_snapshot(s.get('snapshot',{})),'French service lacks actual Shaker Strain / measured Champagne / fresh GentleStir')
    recipes={33:'recipe_dry_martini',34:'recipe_gin_fizz',35:'recipe_manhattan',36:'recipe_french_75' if report.get('faustMismatchObserved') else 'recipe_negroni'}
    for slot in range(33,37):
        rows=[s for s in services if s.get('occurrenceId','').endswith(':'+str(slot))]
        require(len(rows)==1 and rows[0].get('discovered') is not True,'ACT 5 visit must have one service without a new discovery: '+str(slot))
        if any(s.get('occurrenceId','').endswith(':'+str(slot)) for s in inherited): continue
        mismatch=slot==36 and report.get('faustMismatchObserved',False)
        require(len(rows)==1 and rows[0]['outcome'].get('recipeId')==recipes[slot] and rows[0]['outcome'].get('preparation')=='Good' and rows[0]['outcome'].get('qualityGood') is True and rows[0]['outcome'].get('fits') is (not mismatch),f'Missing actual ACT 5 service at slot {slot}')
    if report.get('faustMismatchObserved'):
        final=[s for s in services if s.get('occurrenceId','').endswith(':36')]
        require(len(final)==1 and final[0]['outcome'].get('recipeId')=='recipe_french_75' and final[0]['outcome'].get('fits') is False,'Missing actual Faust mismatch')
    return errors


def validate_last_word_snapshot(snapshot,finish='Strain'):
    if finish not in ('Strain','PourFromContainer'):return False
    keys={'gin','green_chartreuse','maraschino_luxardo','fresh_lime_juice'};amounts=snapshot.get('amounts',{});shake=snapshot.get('shake') or {}
    transfers=[t for t in snapshot.get('trace',[]) if t.get('target')=='Serving' and t.get('acceptedMl',0)>0]
    return (set(amounts)==keys and all(abs(amounts[k]-22.5)<.25 for k in keys) and snapshot.get('hasIce') is False and snapshot.get('garnish') is None and
            shake.get('howGood') is True and shake.get('qualityGood') is True and set(shake.get('amounts',{}))==keys and
            all(abs(shake['amounts'][k]-amounts[k])<.25 for k in keys) and bool(transfers) and
            all(t.get('kind')==finish and t.get('source')=='shaker' for t in transfers) and
            abs(sum(t['acceptedMl'] for t in transfers)-sum(amounts.values()))<.25 and
            all(abs(sum(t.get('components',{}).get(k,0) for t in transfers)-amounts[k])<.25 for k in keys))


def validate_last_word_retry(capture,authored):
    errors=validate_authored_text(capture,authored)
    if capture.get('textId')!='text_rc1_0059':errors.append('Wrong authored Last Word retry text')
    return errors


def validate_last_word_card(capture,content,state):
    text='\n'.join(capture.get('recipeCardText',[]));errors=[]
    def require(condition,message):
        if not condition:errors.append(message+': '+capture['label'])
    recipe=next(r for r in content['recipes'] if r['id']=='recipe_last_word')
    name=next(c['name'] for c in content['cocktails'] if c['id']==recipe['cocktailId'])
    labels={i['id']:i['label'] for i in content['ingredients']}
    ingredients=[labels[a['ingredientId']] for a in recipe['amounts']]
    if state!='known':require('칵테일 잔' not in text,'Untaught Last Word glassware leaked before Known')
    if state=='unknown':
        require(all(x in text for x in ('이름을 기다리는 한 잔','제조법 미상','아직 적어 둔 재료가 없습니다.','준비 순서는 아직 듣지 못했습니다.')),'Missing Unknown Last Word card')
        require(not any(x in text for x in [name,'22.5','Shake','거르기']+ingredients),'Unknown Last Word information leaked')
    else:
        require(name in text and all(x in text for x in ingredients) and text.count('22.5 ml')==4,'Last Word names/amounts not rendered')
        require(('만들어 본 기록' if state=='known' else '대화 속에서 들은 단서') in text,'Wrong Last Word knowledge label')
        require(all(x in text for x in ('Shake','U로 거르기','서빙 얼음 없음','가니시 없음')) and 'Splash' not in text,'Wrong Last Word preparation rendering')
    return errors


def validate_act6_learning_captures(payload,captures,report,content,source_payload=None):
    errors=[]
    def require(condition,message):
        if not condition:errors.append(message)
    inherited=(source_payload or {}).get('services',[])
    new_slots={int(s['occurrenceId'].rsplit(':',1)[1]) for s in payload.get('services',[])[len(inherited):]}
    selected=payload.get('selectedEncounters',[]);early=len(selected)>40 and selected[40]=='enc_rc1_41_known'
    expected=[]
    if early and 39 in new_slots:expected += [('codex-last-word-unknown','unknown',39,'Brewing',8),('codex-last-word-early-known','known',39,'Reaction',9)]
    if 40 in new_slots:expected += [('codex-last-word-fallback-known' if early else 'codex-last-word-clues','known' if early else 'partial',40,'Brewing',9 if early else 8)]
    for label,state,slot,phase,known in expected:
        capture=next((c for c in captures if c['label']==label),None)
        require(capture is not None,'Missing Last Word learning capture: '+label)
        if capture is not None:
            require(capture.get('slot')==slot and capture.get('phase')==phase and capture.get('known')==known,'Wrong Last Word learning capture boundary: '+label)
            errors.extend(validate_last_word_card(capture,content,state))
    if early and 39 in new_slots or 40 in new_slots:
        supplied=set(content['supply']);pages=[c for c in captures if c['label'].startswith('act6-shelf-page-')]
        require(set(report.get('suppliedIngredients',[]))==supplied==set(report.get('shelfSelections',[])),'Last Word physical supply was not fully selected')
        require(supplied=={i for c in pages for i in c.get('visibleIngredients',[])},'Last Word physical supply lacks visible page coverage')
        if early and 39 in new_slots:
            before_discovery=[c for c in pages if c.get('slot')==39 and c.get('phase')=='Brewing']
            require(supplied=={i for c in before_discovery for i in c.get('visibleIngredients',[])},'All early Last Word supply must be shown before discovery')
    return errors


def validate_act6_progress(payload,report,source_payload=None):
    errors=[]
    def require(condition,message):
        if not condition:errors.append(message)
    services=payload.get('services',[]);completed=payload.get('completedOccurrences',[]);memories=payload.get('memories',[]);inherited=(source_payload or {}).get('services',[])
    require(payload.get('phase')=='Complete' and len(completed)==43 and len(set(completed))==43,'Incomplete ACT 6 departures')
    require(len(payload.get('knownReceipts',{}))==9 and len(memories)==6,'Wrong ACT 6 knowledge/memory count')
    memory=[m for m in memories if m.get('id')=='mem_06']
    require(len(memory)==1 and memory[0].get('variant')=='half_submerged_lotus_slow_ripples' and memory[0].get('occurrenceId','').endswith(':41') and memory[0].get('occurrenceId') in completed,'MEM-06 not earned by SimCheong natural departure')
    require(services[:len(inherited)]==inherited,'Inherited service history changed during Continue')
    selected=payload.get('selectedEncounters',[]);old_selected=(source_payload or {}).get('selectedEncounters',[])
    require(selected[:len(old_selected)]==old_selected,'Inherited encounter selection changed during Continue')
    require(len(selected)==43 and selected[40] in ('enc_rc1_41','enc_rc1_41_known'),'Wrong Last Word encounter selection')
    early=len(selected)>40 and selected[40]=='enc_rc1_41_known'
    new_slots=sorted({int(s['occurrenceId'].rsplit(':',1)[1]) for s in services[len(inherited):] if int(s['occurrenceId'].rsplit(':',1)[1])>=37})
    require(sorted(x for x in report.get('visitedSlots',[]) if x>=37)==new_slots,'Reported ACT 6 service visits differ from actual newly committed services')
    lesson=[s for s in services if s.get('occurrenceId','').endswith(':40')]
    success=[s for s in lesson if s['outcome'].get('preparation')=='Good' and s['outcome'].get('qualityGood') is True]
    failures=[s for s in lesson if s not in success]
    if early:
        discovery=[s for s in services if s.get('occurrenceId','').endswith(':39')]
        require(len(discovery)==1 and discovery[0].get('discovered') is True and discovery[0]['outcome'].get('recipeId')=='recipe_last_word' and discovery[0]['outcome'].get('preparation')=='Good' and discovery[0]['outcome'].get('qualityGood') is True and discovery[0]['outcome'].get('fits') is False,'Early Last Word requires actual slot39 discovery independent of FIT')
        require(len(discovery)==1 and payload.get('knownReceipts',{}).get('recipe_last_word')=='service:'+discovery[0]['occurrenceId'],'Early Last Word Known receipt does not come from the actual service')
        require(len(lesson)==1 and lesson[0].get('discovered') is False and lesson[0]['outcome'].get('recipeId')=='recipe_negroni' and lesson[0]['outcome'].get('preparation')=='Good' and lesson[0]['outcome'].get('qualityGood') is True and lesson[0]['outcome'].get('fits') is True,'Known Last Word must use the authored Bitterness fallback')
        require(not any(x.endswith(tuple(':text_rc1_'+n for n in ('0055','0056','0057','0059'))) for x in payload.get('consumedLines',[])),'Known Last Word fallback consumed discovery lesson/retry text')
        require(not report.get('lastWordFailure') and not report.get('lastWordRetryObserved'),'Known Last Word fallback cannot run the discovery retry')
    else:
        require(len(success)==1 and success[0].get('discovered') is True and success[0]['outcome'].get('recipeId')=='recipe_last_word' and success[0]['outcome'].get('fits') is True,'Last Word discovery requires one successful actual service')
        require(all(s.get('discovered') is False and s['outcome'].get('recipeId')=='recipe_last_word' and s['outcome'].get('preparation')=='Weak' and s['outcome'].get('fits') is True and s['outcome'].get('qualityGood') is True and validate_last_word_snapshot(s.get('snapshot',{}),'PourFromContainer') for s in failures),'Failed Last Word must isolate the wrong Pour finish with true FIT and good complete shaken batch')
    if failures:require(any(x.endswith(':40:Retry:0:text_rc1_0059') for x in payload.get('consumedLines',[])),'Failed Last Word lacks consumed authored retry reading')
    if report.get('lastWordFailure'):require(bool(failures),'Missing actual failed Last Word service')
    for s in services:
        if s['outcome'].get('preparation')!='Good':continue
        if s['outcome'].get('recipeId')=='recipe_last_word':require(validate_last_word_snapshot(s.get('snapshot',{})),'Last Word lacks actual complete shaken batch and Shaker Strain')
        if s['outcome'].get('recipeId')=='recipe_french_75':require(validate_french_snapshot(s.get('snapshot',{})),'French service lacks actual measured finish')
    if report.get('earlyLastWordObserved'):require(early and 39 in new_slots,'Reported early Last Word was not newly served')
    if report.get('lastWordFallbackObserved'):require(early and 40 in new_slots,'Reported Last Word fallback was not newly served')
    recipes={37:'recipe_negroni',38:'recipe_dry_martini',39:'recipe_last_word' if early else 'recipe_manhattan',41:'recipe_french_75' if report.get('simCheongMismatchObserved') else 'recipe_last_word',42:'recipe_dry_martini'}
    for slot,recipe in recipes.items():
        rows=[s for s in services if s.get('occurrenceId','').endswith(':'+str(slot))]
        require(len(rows)==1 and rows[0].get('discovered') is (early and slot==39),'ACT 6 recommendation has wrong discovery ownership: '+str(slot))
        if slot not in new_slots:continue
        mismatch=(slot==41 and report.get('simCheongMismatchObserved',False)) or (early and slot==39)
        require(len(rows)==1 and rows[0]['outcome'].get('recipeId')==recipe and rows[0]['outcome'].get('preparation')=='Good' and rows[0]['outcome'].get('qualityGood') is True and rows[0]['outcome'].get('fits') is (not mismatch),f'Missing actual ACT 6 service at slot {slot}')
    if report.get('simCheongMismatchObserved'):
        final=[s for s in services if s.get('occurrenceId','').endswith(':41')]
        require(len(final)==1 and final[0]['outcome'].get('recipeId')=='recipe_french_75' and final[0]['outcome'].get('fits') is False,'Missing actual SimCheong mismatch')
    return errors


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


def validate_history_binding(folder,start,history_root,populated_namespaces,act=3):
    errors=[]
    if Path(history_root).resolve()!=(Path(folder)/'history').resolve(): errors.append('Campaign history lookup is not isolated under this run')
    expected={'Prior':[f'campaign-through-act-{act-1:02}'],'Legacy':['campaign-act1'],'New':[],'Checkpoint':[]}.get(start)
    if expected is None or sorted(populated_namespaces)!=expected: errors.append('Unexpected campaign history namespaces')
    return errors


def inventory(act):
    folders=('Unity/Assets','Unity/Packages','Unity/ProjectSettings','Content/Campaign',f'Validation/Builds/Act{act}Production')
    return {p.relative_to(ROOT).as_posix():sha(p) for folder in folders for p in sorted((ROOT/folder).rglob('*')) if p.is_file()}


def verify(folder,act=3):
    report=read(folder/'player-result.json');captures=lines(folder/'captures.jsonl');checkpoints=lines(folder/'checkpoints.jsonl')
    content=read(ROOT/f'Content/Campaign/act{act}.json')
    errors=validate_records(report,captures,checkpoints,content['supply'] if act>=4 else None);profile=profile_for_act(act);total=profile['slots']
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
    errors.extend(validate_history_binding(folder,expected['start'],history_root,populated,act))
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
    if act>=3:
        source_payload=read(folder/'origin/slot1/current.json')['payload'] if expected['start']!='New' else None
        errors.extend({3:validate_act3_progress,4:validate_act4_progress,5:validate_act5_progress,6:validate_act6_progress}[act](payload,report,source_payload))
        if act==6:errors.extend(validate_act6_learning_captures(payload,captures,report,content,source_payload))
        if report['startDepartures']<28:
            after=read(folder/'checkpoints/after-departure-27/slot1/current.json')['payload']
            require(len(after['completedOccurrences'])==28 and not any(s['occurrenceId'].endswith(':28') for s in after['services']) and any(m['id']=='mem_03' and m['occurrenceId'].endswith(':27') for m in after['memories']),'After-departure-27 save does not prove memory before final ordinary service')
        if act>=6 and report['startDepartures']<42:
            after=read(folder/'checkpoints/after-departure-41/slot1/current.json')['payload']
            require(len(after['completedOccurrences'])==42 and not any(s['occurrenceId'].endswith(':42') for s in after['services']) and any(m['id']=='mem_06' and m['occurrenceId'].endswith(':41') for m in after['memories']),'After-departure-41 save does not prove memory before final ordinary service')
    if 'quixote-mismatch' in expected['fixture']: require(report.get('quixoteMismatchObserved'),'Requested Quixote mismatch not observed')
    if 'hamlet-mismatch' in expected['fixture']: require(report.get('hamletMismatchObserved'),'Requested Hamlet mismatch not observed')
    if 'faust-mismatch' in expected['fixture']: require(report.get('faustMismatchObserved'),'Requested Faust mismatch not observed')
    if 'simcheong-mismatch' in expected['fixture']: require(report.get('simCheongMismatchObserved'),'Requested SimCheong mismatch not observed')
    if 'last-word-retry' in expected['fixture']: require(report.get('lastWordFailure') and report.get('lastWordRetryObserved'),'Requested Last Word failure/retry not observed')
    if 'early-last-word' in expected['fixture']: require(report.get('earlyLastWordObserved') and report.get('lastWordFallbackObserved'),'Requested early Last Word discovery/fallback not observed')
    if 'french-retry' in expected['fixture']: require(report.get('frenchFailure') and report.get('frenchRetryObserved'),'Requested French failure/retry not observed')
    authored={t['id']:t for t in content['texts']};encounters={e['id']:e for e in content['encounters']}
    for c in captures:
        if c['slot']>=17 and c['phase'] in ('Intro','Farewell'):
            errors.extend(validate_authored_text(c,authored));encounter=encounters[payload['selectedEncounters'][c['slot']]]
            sequence=[line['textId'] for line in encounter['intro']] if c['phase']=='Intro' else encounter['farewell']
            require(0<=c['line']<len(sequence) and c['textId']==sequence[c['line']],'Authored line order changed: '+c['label'])
            require(set(c['guestIds'])<=set(c['visibleActors']),'Missing actual actor: '+c['label'])
            if c['textRole']=='narration': require(not c['displayedSpeaker'],'Narration incorrectly attributed: '+c['label'])
            if len(c['guestIds'])==2: require(c['servingVessels']==1,'Paired encounter must retain one serving vessel')
            if c['memories']<2: require(not c['loversVisible'],'MEM-02 appeared before earned departure')
            if act>=3 and c['slot']<=27: require(not c.get('windmillVisible'),'MEM-03 appeared before Quixote departure')
            if act>=4 and c['slot']<=32: require(not c.get('hamletVisible'),'MEM-04 appeared before Hamlet departure')
            if act>=5 and c['slot']<=36: require(not c.get('faustVisible'),'MEM-05 appeared before Faust departure')
            if act>=6 and c['slot']<=41: require(not c.get('simCheongVisible'),'MEM-06 appeared before SimCheong departure')
        if act==4 and c['phase']=='Retry' and c['slot']==29:
            errors.extend(validate_authored_text(c,authored))
            require(c.get('textId')=='text_rc1_0039','Wrong authored French retry text')
        if act>=6 and c['phase']=='Retry' and c['slot']==40:errors.extend(validate_last_word_retry(c,authored))
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
                namespace=f'campaign-through-act-{act-1:02}' if expected['start']=='Prior' else 'campaign-act1'
                require(sha(history_root/namespace/'slot1'/name)==digest,'Isolated historical source changed')
        final={name:sha(save_root/'slot1'/name) for name in ('current.json','previous.json') if (save_root/'slot1'/name).is_file()}
        errors.extend(validate_completed_continue(report,origin['files'],final))
    return errors


def main():
    p=argparse.ArgumentParser();p.add_argument('mode',choices=('inventory','verify-inventory','verify'));p.add_argument('path',type=Path);p.add_argument('--act',type=int,choices=(2,3,4,5,6),default=3);args=p.parse_args()
    if args.mode=='inventory':
        args.path.write_text(json.dumps(dict(act=args.act,files=inventory(args.act)),indent=2),encoding='utf-8');print('CAMPAIGN_INVENTORY_WRITTEN');return
    errors=([] if read(args.path)['files']==inventory(args.act) else ['Source/Player inventory changed']) if args.mode=='verify-inventory' else verify(args.path,args.act)
    if errors: print(json.dumps(dict(status='FAIL',errors=errors),indent=2));raise SystemExit(2)
    if args.mode=='verify' and args.act>=3:
        report=read(args.path/'player-result.json');payload=read(args.path/'save/slot1/current.json')['payload'];origin=args.path/'origin/slot1/current.json'
        inherited=inherited_quixote_evidence(payload,read(origin)['payload'] if origin.is_file() else None)
        print(json.dumps(dict(newQuixoteMismatchObserved=report.get('quixoteMismatchObserved',False),
                              inheritedQuixoteServicePreserved=inherited['servicePreserved'],
                              inheritedQuixoteMismatchPreserved=inherited['mismatchPreserved'])))
        if args.act>=4:
            hamlet=inherited_service_evidence(payload,read(origin)['payload'] if origin.is_file() else None,32)
            print(json.dumps(dict(newHamletMismatchObserved=report.get('hamletMismatchObserved',False),
                                  inheritedHamletServicePreserved=hamlet['servicePreserved'],inheritedHamletMismatchPreserved=hamlet['mismatchPreserved'])))
        if args.act>=5:
            faust=inherited_service_evidence(payload,read(origin)['payload'] if origin.is_file() else None,36)
            print(json.dumps(dict(newFaustMismatchObserved=report.get('faustMismatchObserved',False),
                                  inheritedFaustServicePreserved=faust['servicePreserved'],inheritedFaustMismatchPreserved=faust['mismatchPreserved'])))
        if args.act>=6:
            sim=inherited_service_evidence(payload,read(origin)['payload'] if origin.is_file() else None,41)
            print(json.dumps(dict(newSimCheongMismatchObserved=report.get('simCheongMismatchObserved',False),
                                  inheritedSimCheongServicePreserved=sim['servicePreserved'],inheritedSimCheongMismatchPreserved=sim['mismatchPreserved'])))
    print('CAMPAIGN_PROOF_PASS' if args.mode=='verify' else 'CAMPAIGN_INVENTORY_UNCHANGED')


if __name__=='__main__': main()
