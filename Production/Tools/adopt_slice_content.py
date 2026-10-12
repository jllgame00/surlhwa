"""One-time selective adoption, or read-only verification, of the approved Slice text."""
from pathlib import Path
import argparse
import hashlib
import json

ROOT = Path(__file__).resolve().parents[1]
SPEC = Path('C:/Users/surlw/.codex/.chatgpt-projects/g-p-6a8cfbb439748191ac8fac094112d31b')

def sha(text):
    return hashlib.sha256(text.encode('utf-8')).hexdigest()

def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--write', action='store_true')
    parser.add_argument('--source-root', type=Path, default=SPEC)
    args = parser.parse_args()
    registry = json.loads((args.source_root / 'SURLHWA_CANON/_verification/wording_registry.json').read_text(encoding='utf-8'))
    records = {row['id']: row for row in registry['records']}
    numbers = [207,208,209,210,211,212,213,320,321,322,323,324,326,327,328,329,330,331,334,389,390,395,433]
    texts = []
    for number in numbers:
        row = records[f'SRC-RC1-{number:04d}']
        assert sha(row['text']) == row['sha256_utf8']
        role = 'display' if number == 207 else 'narration' if number == 208 else 'mixed' if number == 209 else 'request_summary' if number == 433 else 'spoken'
        adoption = 'Verbatim field; original order and role retained.'
        if 320 <= number <= 324:
            adoption = 'Previously learned Slice fixture facts only; not spoken by the postman or Prince.'
        if number == 395:
            adoption = 'Verbatim departure excerpt for the Warmth visit; no closing-visit/D09 branch, ACT completion or memory evaluation. HG07 wording/context review pending.'
        texts.append(dict(id=f'text_rc1_{number:04d}', value=row['text'], sourceId=row['id'], sourceHash=row['sha256_utf8'],
                          sourcePath=row['primary_home'], sourceField=row['field'], sourceOrder=str(row['source_order']), role=role, adoption=adoption))
    prince_name = '어린왕자'
    assert (args.source_root / 'SURLHWA_CANON/51_LITTLE_PRINCE.md').read_text(encoding='utf-8').splitlines()[0] == '# ' + prince_name
    texts.append(dict(id='text_prince_name',value=prince_name,sourceId='CANON-51-TITLE',sourceHash=sha(prince_name),
                      sourcePath='51_LITTLE_PRINCE.md',sourceField='heading1',sourceOrder='0',role='display',adoption='Canonical character title, not a spoken line.'))
    ingredient_names = [('whiskey','위스키'),('vermouth','버무스'),('water','물'),('rye_whiskey','라이 위스키'),
                        ('sweet_red_vermouth','스위트 레드 버무스'),('angostura_bitters','앙고스투라 비터스'),('gin','진'),('dry_vermouth','드라이 버무스')]
    ingredients = [dict(id=i,label=n) for i,n in ingredient_names]
    def recipe(rid,cid,amounts,dash=None,garnish=None,glass='cocktail',optional=False):
        return dict(id=rid,cocktailId=cid,method='stir',amounts=[dict(ingredientId=i,ml=m) for i,m in amounts],
                    dashIngredient=dash,requiredGarnish=garnish,glassware=glass,optionalGarnish=optional,
                    preparation=['mixing_ice','stir_after_ingredients','strain_to_serving'])
    recipes = [recipe('recipe_test_stir','cocktail_seolya',[('whiskey',59.14706),('vermouth',29.57353),('water',29.57353)],glass='old_fashioned'),
               recipe('recipe_manhattan','cocktail_manhattan',[('rye_whiskey',50),('sweet_red_vermouth',20),('angostura_bitters',1)],'angostura_bitters','cherry'),
               recipe('recipe_dry_martini','cocktail_dry_martini',[('gin',60),('dry_vermouth',10)],optional=True)]
    cocktails = [dict(id='cocktail_seolya',name='설야',tags=['softness','warmth']),
                 dict(id='cocktail_manhattan',name='Manhattan',tags=['warmth','bitterness']),
                 dict(id='cocktail_dry_martini',name='Dry Martini',tags=['clarity'])]
    tuning = dict(id='stir_slice_v1',liquidUnit='ml',precision=6,capacity=dict(jigger=60,mixing=300,serving=180),
                  flow=dict(pour=30,finePour=5,transfer=40,fineTransfer=8,dash=1),
                  technique=dict(howDegrees=360,qualityDegrees=1080,minSpeed=60,maxSpeed=300,stableFraction=.75),
                  recognition=dict(relative=.05,ingredientMl=.25,extraMl=.01))
    guests = [dict(id='guest_ordinary_postman',kind='ordinary',displayTextId='text_rc1_0207',visualBindingId='visual_ordinary'),
              dict(id='guest_little_prince',kind='special',displayTextId='text_prince_name',visualBindingId='visual_prince')]
    requests = [dict(id='request_ordinary13',kind='recommendation',tag='warmth',summaryTextId='text_rc1_0213',discoveryPolicy='normal_recognition'),
                dict(id='request_prince_warmth',kind='recommendation',tag='warmth',summaryTextId='text_rc1_0433',discoveryPolicy='normal_recognition')]
    def encounter(eid,gid,request,intro,farewell,responses):
        return dict(id=eid,guestId=gid,requestId=request,intro=[f'text_rc1_{n:04d}' for n in intro],
                    farewell=[f'text_rc1_{n:04d}' for n in farewell],responses={k:f'text_rc1_{n:04d}' for k,n in responses.items()},
                    memoryGrants=[],completesAct=False,disposition='slice_excerpt')
    encounters = [encounter('enc_ordinary13','guest_ordinary_postman','request_ordinary13',[212,213],[208,209],
                    dict(good=210,weak=326,mismatch=327,bothWeak=328,unknown=334)),
                  encounter('enc_prince_warmth_excerpt','guest_little_prince','request_prince_warmth',[389,390],[395],
                    dict(good=329,weak=330,mismatch=331,bothWeak=328,unknown=334))]
    profile = dict(id='slice_act1_stir',fixtureContext='ACT 1 어린왕자 후속 방문의 짧은 발췌입니다. 이전 대답과 서비스 결과는 기록하지 않았습니다. ACT의 결말과 별의 기억은 이 장면에 포함되지 않습니다.',
                   supply=[i for i,_ in ingredient_names],known=[dict(recipeId='recipe_test_stir',provenance='LEARN-SEOLYA'),dict(recipeId='recipe_manhattan',provenance='ARCH-SLICE-SEED / LEARN-MANHATTAN')],
                   facts=[dict(recipeId='recipe_dry_martini',fact=f,sourceIds=s) for f,s in [
                       ('amount:gin',['SRC-RC1-0320']),('amount:dry_vermouth',['SRC-RC1-0320']),('method',['SRC-RC1-0323']),
                       ('preparation',['SRC-RC1-0321']),('garnish',['SRC-RC1-0321'])]],
                   encounters=[e['id'] for e in encounters],completesAct=False,initialMemories=[],priorChoices=[],priorServices=[])
    outputs = {'Ingredients/liquids.json':dict(ingredients=ingredients),'Recipes/stir.json':dict(cocktails=cocktails,recipes=recipes),
               'Profiles/stir-tuning.json':dict(tuning=tuning),'Dialogue/slice.json':dict(texts=texts),'Guests/slice.json':dict(guests=guests),
               'Requests/warmth.json':dict(requests=requests),'Encounters/slice.json':dict(encounters=encounters),
               'Profiles/stir-slice.json':dict(contentRevision='slice_act1_stir_1',profile=profile)}
    if args.write:
        for relative,body in outputs.items():
            p=ROOT/'Content'/relative
            if p.exists(): raise SystemExit(f'Refusing to overwrite authored content: {p}')
        for relative,body in outputs.items():
            p=ROOT/'Content'/relative; p.parent.mkdir(parents=True,exist_ok=True)
            p.write_text(json.dumps(dict(schemaVersion=1,**body),ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
    actual=json.loads((ROOT/'Content/Dialogue/slice.json').read_text(encoding='utf-8'))['texts']
    for row in actual:
        if row['sourceId']=='CANON-51-TITLE':
            assert row['value']==prince_name and row['sourceHash']==sha(prince_name)
        else:
            source=records[row['sourceId']]
            assert row['value']==source['text'] and row['sourceHash']==source['sha256_utf8'] and row['sourceField']==source['field']
    print(f'SOURCE ADOPTION PASS: {len(actual)} exact text/title records; authoritative files read-only')

if __name__=='__main__':
    main()
