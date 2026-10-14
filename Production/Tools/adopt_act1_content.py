"""Selective ACT 1 adoption. Read authoritative sources; never overwrite them."""
from pathlib import Path
import argparse
import hashlib
import json

ROOT = Path(__file__).resolve().parents[1]
SPEC = Path('C:/Users/surlw/.codex/.chatgpt-projects/g-p-6a8cfbb439748191ac8fac094112d31b')

def sha(value):
    return hashlib.sha256(value.encode('utf-8')).hexdigest()

def build(source_root):
    registry_path = source_root/'SURLHWA_CANON/_verification/wording_registry.json'
    records = {r['id']: r for r in json.loads(registry_path.read_text(encoding='utf-8'))['records']}
    texts = {}
    def source(number, role='spoken', edited=None, reason=None):
        r = records[f'SRC-RC1-{number:04d}']
        assert sha(r['text']) == r['sha256_utf8']
        tid = f'text_rc1_{number:04d}' + ('_act1_edit' if edited else '')
        row = dict(id=tid,value=edited or r['text'],sourceText=r['text'],sourceId=r['id'],sourceHash=r['sha256_utf8'],
                   sourcePath=r['primary_home'],sourceField=r['field'],sourceOrder=str(r['source_order']),role=role,
                   adoption=reason or 'Verbatim field, original role retained; selected ACT 1 adoption.')
        if tid in texts:
            assert texts[tid] == row, (tid, 'conflicting source role')
        texts[tid] = row
        return tid
    def authored(tid, value, role, authority, reason):
        texts[tid] = dict(id=tid,value=value,sourceText=value,sourceId='PRODUCTION-'+tid,sourceHash=sha(value),
                          sourcePath=authority,sourceField='production_authored',sourceOrder='0',role=role,adoption=reason)
        return tid
    ingredients = json.loads((ROOT/'Content/Ingredients/liquids.json').read_text(encoding='utf-8'))['ingredients']
    ingredients += [dict(id=i,label=n) for i,n in [('vodka','보드카'),('coffee_liqueur','커피 리큐르'),('cream','크림'),('campari','캄파리'),
                                                 ('lemon_juice','레몬 주스'),('simple_syrup','심플 시럽'),('soda_water','소다 워터')]]
    existing = json.loads((ROOT/'Content/Recipes/stir.json').read_text(encoding='utf-8'))
    cocktails, recipes = existing['cocktails'], existing['recipes']
    for r in recipes:
        r.update(garnishPosition='in_glass',lateIngredient=None,layerOrder=[])
    def recipe(rid,cid,name,tags,method,amounts,preparation,garnish=None,glass='old_fashioned',optional=False,late=None,layers=None):
        cocktails.append(dict(id=cid,name=name,tags=tags))
        recipes.append(dict(id=rid,cocktailId=cid,method=method,amounts=[dict(ingredientId=i,ml=m) for i,m in amounts],dashIngredient=None,
                            requiredGarnish=garnish,garnishPosition='rim',glassware=glass,optionalGarnish=optional,preparation=preparation,lateIngredient=late,layerOrder=layers or []))
    recipe('recipe_negroni','cocktail_negroni','Negroni',['bitterness'],'build',[('gin',30),('campari',30),('sweet_red_vermouth',30)],
           ['direct_serving','serving_ice','gentle_stir','orange_slice_rim'],garnish='orange_slice')
    recipe('recipe_gin_fizz','cocktail_gin_fizz','Gin Fizz',['awakening','clarity'],'shake',[('gin',45),('lemon_juice',30),('simple_syrup',10),('soda_water',15)],
           ['shaker_ice','closed_shake','pour_from_shaker','late_soda_splash','no_serving_ice'],glass='tumbler',optional=True,late='soda_water')
    recipe('recipe_manwol','cocktail_manwol','만월',['longing','clarity'],'float',[('vodka',59.14706),('coffee_liqueur',29.57353),('cream',29.57353)],
           ['vodka_base','coffee_layer','cream_layer','no_ice'],layers=['vodka','coffee_liqueur','cream'])
    tuning=json.loads((ROOT/'Content/Profiles/stir-tuning.json').read_text(encoding='utf-8'))['tuning']
    tuning['id']='campaign_act1_v1'
    tuning['shake']=dict(revision='shake_v1',howReversals=4,qualityReversals=8,endpoint=.7,minInterval=250000,maxInterval=1000000,stableFraction=.75)
    tuning['float']=dict(revision='float_v1',maxStableRate=12,maxHeight=.25,maxHeightSpeed=.5)
    guests=[]
    for gid,number in [('guest_postman',159),('guest_repairer',167),('guest_snow_worker',183),('guest_station_worker',199)]:
        guests.append(dict(id=gid,kind='ordinary',displayTextId=source(number,'display'),visualBindingId='visual_ordinary'))
    prince_name=authored('text_prince_name','어린왕자','display','SURLHWA_CANON/51_LITTLE_PRINCE.md','Canonical title; display only.')
    guests.append(dict(id='guest_little_prince',kind='special',displayTextId=prince_name,visualBindingId='visual_prince'))
    farewell_ordinary=authored('text_ordinary_departure','손님은 빈 잔을 내려놓고, 다시 눈길로 나섭니다.','narration','SURLHWA_CANON/60_ORDINARY_GUESTS.md',
                               'Production transition; no identity, memory or emotional outcome invented.')
    summaries={'warmth':source(424,'request_summary'),'clarity':source(426,'request_summary'),
               'bitterness':source(418,'request_summary'),'awakening':source(415,'request_summary')}
    normal_intro={'warmth':source(404),'clarity':source(307),'bitterness':source(304),'awakening':source(301)}
    def facts(recipe_id,values,number):
        return [dict(recipeId=recipe_id,fact=f,sourceIds=[f'SRC-RC1-{number:04d}']) for f in values]
    def complete_facts(recipe_id,number,identity=True):
        r=next(r for r in recipes if r['id']==recipe_id)
        return facts(recipe_id,(['identity'] if identity else [])+['method','preparation','garnish']+['amount:'+a['ingredientId'] for a in r['amounts']],number)
    def line(tid,grants=None): return dict(textId=tid,facts=grants or [])
    ordinary_responses={k:source(n) for k,n in [('good',325),('weak',326),('mismatch',327),('bothWeak',328),('unknown',334)]}
    prince_responses={k:source(n) for k,n in [('good',329),('weak',330),('mismatch',331),('bothWeak',332),('unknown',334)]}
    encounters=[]
    def encounter(eid,night,guest,intro,tag='warmth',target=None,required=None,prince=None,summary=None,choice_intros=None):
        row=dict(id=eid,sourceRow='RC1-'+eid.split('_')[2],night=night,guestId=guest,
                 request=dict(cocktailId=target,anyTags=[] if target else [tag],allTags=[],excludedTags=[]),summaryTextId=summary or summaries[tag],
                 requiredDiscovery=required,princeVisit=prince,intro=intro,farewell=[source(395)] if prince else [farewell_ordinary],
                 responses=prince_responses if prince else ordinary_responses,choiceIntros=choice_intros or {})
        encounters.append(row)
        return eid
    method=source(323,edited='다음에 떠올려 볼 잔은, 얼음으로 차갑게 만들고 천천히 젓는다고 해요.',reason='Opening clue now refers to a future recipe; the current achievable request remains a known warm drink.')
    gin=source(322,edited='그 잔의 베이스는 진이에요.',reason='Refers back to the previous clue rather than falsely identifying the current served drink.')
    vermouth=source(324)
    plan=[]
    def slot(eid,fallback=None,known=None): plan.append(dict(encounterId=eid,fallbackId=fallback,whenKnownRecipe=known))
    for n,tid,f in [(1,method,'method'),(2,gin,'ingredient:gin'),(3,vermouth,'ingredient:dry_vermouth')]:
        number={1:323,2:322,3:324}[n]
        eid=encounter(f'enc_rc1_{n:02d}',1,['guest_postman','guest_repairer','guest_station_worker'][n-1],
                      [line(normal_intro['warmth']),line(tid,facts('recipe_dry_martini',[f],number))]); slot(eid)
    eid=encounter('enc_rc1_04',2,'guest_repairer',[line(source(320),facts('recipe_dry_martini',['amount:gin','amount:dry_vermouth'],320)),
                    line(source(321),facts('recipe_dry_martini',['method','preparation','garnish'],321))],target='cocktail_dry_martini',summary=source(428,'request_summary')); slot(eid)
    eid=encounter('enc_rc1_05',2,'guest_postman',[line(normal_intro['warmth'])]); alt=encounter('enc_rc1_05_clarity',2,'guest_postman',[line(normal_intro['clarity'])],tag='clarity'); slot(eid,alt,'recipe_dry_martini')
    intro=[line(source(n,'narration' if n==381 else 'spoken'),complete_facts('recipe_manwol',n) if n==385 else facts('recipe_manwol',['preparation'],n) if n==386 else None) for n in range(381,387)]
    eid=encounter('enc_rc1_06',2,'guest_little_prince',intro,target='cocktail_manwol',prince='First',summary=source(432,'request_summary')); slot(eid)
    def discovery(n,night,rid,cid,a,b,guest='guest_repairer'):
        recipe=next(r for r in recipes if r['id']==rid)
        late=recipe.get('lateIngredient')
        # Grant only the part of this authored lesson that has actually been consumed.
        first=['identity']+([] if rid=='recipe_manhattan' else ['method'])+['amount:'+x['ingredientId'] for x in recipe['amounts'] if x['ingredientId']!=late]
        second=['preparation','garnish']+(['method'] if rid=='recipe_manhattan' else [])+(['amount:'+late] if late else [])
        eid=encounter(f'enc_rc1_{n:02d}',night,guest,[line(source(a),facts(rid,first,a)),line(source(b),facts(rid,second,b))],
                      target=cid,required=rid,summary=authored('text_order_'+rid,next(c['name'] for c in cocktails if c['id']==cid),'request_summary','SURLHWA_CANON/31_CURRENT_AND_ORIGINAL_COCKTAILS.md','Selected canonical recipe title.'))
        alt=encounter(f'enc_rc1_{n:02d}_known',night,guest,[line(normal_intro['warmth'])]); slot(eid,alt,rid)
    discovery(7,3,'recipe_negroni','cocktail_negroni',402,403)
    discovery(8,3,'recipe_manwol','cocktail_manwol',400,401,'guest_station_worker')
    slot(encounter('enc_rc1_09',3,'guest_snow_worker',[line(normal_intro['bitterness'])],tag='bitterness'))
    # Manhattan teaching precedes the Warmth visit, providing two genuinely Known fitting choices.
    discovery(11,4,'recipe_manhattan','cocktail_manhattan',398,399)
    variants={'keep':[source(387),source(388)],'seek':[source(396),source(397)],'none':[source(389),source(390)]}
    slot(encounter('enc_rc1_10',4,'guest_little_prince',[line(source(389)),line(source(390))],prince='Warmth',summary=source(433,'request_summary'),choice_intros=variants))
    slot(encounter('enc_rc1_12',4,'guest_postman',[line(source(405))]))
    slot(encounter('enc_rc1_13',5,'guest_snow_worker',[line(source(305))],tag='bitterness'))
    discovery(14,5,'recipe_gin_fizz','cocktail_gin_fizz',377,378,'guest_station_worker')
    slot(encounter('enc_rc1_15',5,'guest_repairer',[line(source(308))],tag='clarity'))
    slot(encounter('enc_rc1_16',5,'guest_station_worker',[line(normal_intro['awakening'])],tag='awakening'))
    slot(encounter('enc_rc1_17',6,'guest_little_prince',[line(source(379)),line(source(380))],tag='clarity',prince='Clarity',summary=source(431,'request_summary')))
    prince=dict(prompt=source(407,'narration'),choices={'keep':source(408,'display'),'seek':source(409,'display')},
                responses={'keep':source(410),'seek':source(411)},recalls={'keep':source(412),'seek':source(413),'none':source(414)},
                openFarewell=[source(n) for n in (391,392,393)],quietFarewell=[source(n) for n in (394,395)],policyRevision='prince_return_understanding_v1')
    return dict(schemaVersion=1,mode='campaign',act='act_01',contentRevision='campaign_act1_v1',sourceRegistrySha256=hashlib.sha256(registry_path.read_bytes()).hexdigest(),
                ingredients=ingredients,cocktails=cocktails,recipes=recipes,tuning=tuning,guests=guests,texts=list(texts.values()),encounters=encounters,slots=plan,
                supply=[i['id'] for i in ingredients],known=[dict(recipeId='recipe_test_stir',provenance='LEARN-SEOLYA')],prince=prince)

def main():
    parser=argparse.ArgumentParser()
    parser.add_argument('--write',action='store_true')
    parser.add_argument('--source-root',type=Path,default=SPEC)
    args=parser.parse_args()
    result=build(args.source_root)
    destination=ROOT/'Content/Campaign/act1.json'
    if args.write:
        if destination.exists(): raise SystemExit('Refusing to overwrite existing authored ACT 1 content')
        destination.parent.mkdir(parents=True,exist_ok=True)
        destination.write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
    actual=json.loads(destination.read_text(encoding='utf-8'))
    assert actual==result,'Authored ACT 1 content differs from the explicit selective-adoption manifest'
    assert len(actual['slots'])==17 and len(actual['recipes'])==6
    print(f"ACT 1 SOURCE ADOPTION PASS: {len(actual['texts'])} sourced/authored text records, 17 slots, 6 recipes; specifications read-only")

if __name__=='__main__': main()
