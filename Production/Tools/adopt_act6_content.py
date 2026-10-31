"""Append exact ACT6 source and Last Word while preserving accepted ACT5 bytes."""
import argparse
import copy
import hashlib
import json
from pathlib import Path
from adopt_act1_content import ROOT, SPEC

ACCEPTED_ACT5_SHA256 = 'bc76a5546c7f30d6dfa417fd3754a81fe4c52f8040b15bc86b42c1a954f34339'


def accepted_base():
    raw = (ROOT / 'Content/Campaign/act5.json').read_bytes()
    assert hashlib.sha256(raw).hexdigest() == ACCEPTED_ACT5_SHA256, 'ACT5 is not the accepted source'
    return json.loads(raw)


def build(source_root):
    base = accepted_base()
    registry_path = source_root / 'SURLHWA_CANON/_verification/wording_registry.json'
    records = {r['id']: r for r in json.loads(registry_path.read_bytes())['records']}
    existing = {x['id']: x for x in base['texts']}
    texts = {}

    def source(number):
        record = records[f'SRC-RC1-{number:04d}']
        assert hashlib.sha256(record['text'].encode('utf-8')).hexdigest() == record['sha256_utf8']
        role = 'narration' if number in {52, 216, 224, 271, 280, 282, 293, 297} else 'mixed' if number in {217, 225} else 'display' if number in {50, 215, 223, 268, 279, 290} else 'request_summary' if number in {58, 222, 230, 278, 289, 300} else 'spoken'
        tid = f'text_rc1_{number:04d}'
        row = dict(id=tid, value=record['text'], sourceText=record['text'], sourceId=record['id'],
                   sourceHash=record['sha256_utf8'], sourcePath=record['primary_home'], sourceField=record['field'],
                   sourceOrder=str(record['source_order']), role=role,
                   adoption='Verbatim field and original order; bounded ACT6 adoption after stable ACT5.')
        if tid in existing:
            assert all(existing[tid][key] == value for key, value in row.items() if key != 'adoption')
            row = copy.deepcopy(existing[tid])
        texts[tid] = row
        return tid

    for number in list(range(50, 60)) + list(range(215, 231)) + list(range(268, 301)):
        source(number)

    def encounter(number, night, guest, before, after, good, failure, request, tags, memory=False):
        return dict(id=f'enc_rc1_{number:02d}', sourceRow=f'RC1-{number:02d}', night=night, guestId=guest,
                    companionGuestId=None, memoryId='mem_06' if memory else None,
                    memoryVariant='half_submerged_lotus_slow_ripples' if memory else None,
                    request=dict(cocktailId=None, anyTags=tags, allTags=[], excludedTags=[]),
                    summaryTextId=source(request), requiredDiscovery=None, princeVisit=None,
                    intro=[dict(textId=source(n), facts=[]) for n in before], farewell=[source(n) for n in after],
                    responses={key: source(good if key == 'good' else failure) for key in ['good', 'weak', 'mismatch', 'bothWeak', 'unknown']}, choiceIntros={})

    lesson = encounter(41, 17, 'guest_snow_worker', [55, 56, 57], [51, 52], 53, 54, 58, [])
    lesson.update(requiredDiscovery='recipe_last_word', discoveryRetryTextId=source(59))
    lesson['request']['cocktailId'] = 'cocktail_last_word'
    for line, facts in zip(lesson['intro'], [['identity'], ['amount:gin', 'amount:green_chartreuse', 'amount:maraschino_luxardo', 'amount:fresh_lime_juice'], ['method', 'preparation', 'garnish']]):
        line['facts'] = [dict(recipeId='recipe_last_word', fact=f, sourceIds=[line['textId'].replace('text_rc1_', 'SRC-RC1-')]) for f in facts]
    final_ordinary = encounter(43, 18, 'guest_snow_worker', [228, 229], [224, 225], 226, 227, 230, ['clarity'])
    fallback = copy.deepcopy(next(e for e in base['encounters'] if e['id'] == 'enc_rc1_29'))
    fallback.update(id='enc_rc1_41_known', sourceRow='RC1-41', night=17)
    return dict(schemaVersion=1, kind='act_adoption_package', act='act_06',
                status='SOURCE_ADOPTED_RUNTIME_INTEGRATION_PENDING', acceptedAct5Sha256=ACCEPTED_ACT5_SHA256,
                sourceRegistrySha256=hashlib.sha256(registry_path.read_bytes()).hexdigest(),
                ingredients=[dict(id='green_chartreuse', label='그린 샤르트뢰즈'), dict(id='maraschino_luxardo', label='마라스키노 룩사르도'), dict(id='fresh_lime_juice', label='프레시 라임 주스')],
                cocktails=[dict(id='cocktail_last_word', name='라스트 워드', tags=['clarity'])],
                recipes=[dict(id='recipe_last_word', cocktailId='cocktail_last_word', method='shake',
                              amounts=[dict(ingredientId=k, ml=22.5) for k in ['gin', 'green_chartreuse', 'maraschino_luxardo', 'fresh_lime_juice']],
                              dashIngredient=None, requiredGarnish=None, glassware='cocktail', optionalGarnish=False,
                              preparation=['shaker_ice', 'closed_shake', 'strain_from_shaker', 'no_serving_ice'],
                              garnishPosition='rim', lateIngredient=None, layerOrder=[])],
                supply=['green_chartreuse', 'maraschino_luxardo', 'fresh_lime_juice'],
                supplyGrants=[dict(beforeEncounter='enc_rc1_40', ingredientIds=['green_chartreuse', 'maraschino_luxardo', 'fresh_lime_juice'])],
                guests=[dict(id='guest_simcheong', kind='special', displayTextId=source(268), visualBindingId='visual_simcheong')],
                texts=list(texts.values()),
                encounters=[encounter(38, 16, 'guest_repairer', [220, 221], [216, 217], 218, 219, 222, ['bitterness', 'warmth']),
                            encounter(39, 16, 'guest_simcheong', range(274, 278), range(269, 272), 272, 273, 278, ['warmth', 'clarity']),
                            encounter(40, 17, 'guest_simcheong', range(285, 289), range(280, 283), 283, 284, 289, ['warmth', 'bitterness']),
                            lesson,
                            encounter(42, 18, 'guest_simcheong', range(296, 300), range(291, 294), 294, 295, 300, ['warmth', 'clarity'], True),
                            final_ordinary, fallback],
                slots=[dict(encounterId=f'enc_rc1_{n:02d}', fallbackId='enc_rc1_41_known' if n == 41 else None,
                            whenKnownRecipe='recipe_last_word' if n == 41 else None) for n in range(38, 44)],
                sourceDispositions=[dict(sourceId='SRC-RC1-0050', disposition='Existing guest_snow_worker identity retained; visit-specific display preserved.'),
                                    dict(sourceId='SRC-RC1-0059', disposition='Owned Retry reading after failed required discovery; advancing consumes the existing reading receipt.'),
                                    dict(sourceId='SRC-RC1-0215', disposition='Existing guest_repairer identity retained; visit-specific display preserved.'),
                                    dict(sourceId='SRC-RC1-0223', disposition='Existing guest_snow_worker identity retained; visit-specific display preserved.'),
                                    dict(sourceId='SRC-RC1-0279', disposition='Same guest_simcheong identity as Before display SRC-RC1-0268; both display records preserved.'),
                                    dict(sourceId='SRC-RC1-0290', disposition='Same guest_simcheong identity as Before display SRC-RC1-0268; all three display records preserved.'),
                                    dict(sourceId='PRODUCTION-KNOWN-LASTWORD-FALLBACK', disposition='Reuse accepted enc_rc1_29 ordinary snow-worker Bitterness request and reading; remap only id/sourceRow/night to RC1-41 night17. Preserve Ordinary16 returning-footsteps reading for its authored position after SimCheongReturn. No changed earlier row, repeated lesson or invented dialogue.')],
                recipeAdoption=dict(source='SURLHWA_CANON/31_CURRENT_AND_ORIGINAL_COCKTAILS.md#drink-09',
                                    officialUrl='https://iba-world.com/iba-cocktail/last-word/', reverifiedOn='2026-10-31',
                                    disposition='Four exact ingredient identities at nominal22.5ml each. Shake with ice then Shaker Strain, no serving ice or authored garnish. Cocktail glass remains metadata.'),
                supplyAdoption=dict(source='SURLHWA_CANON/33_INGREDIENT_AND_GLASSWARE_POLICY.md#supply-004',
                                    disposition='SUPPLY-N15 maps to production night17 at slot39 before SimCheongResolve, one visit before the slot40 lesson; no knowledge grant.'))


def compose(data):
    base = accepted_base()
    assert data['acceptedAct5Sha256'] == ACCEPTED_ACT5_SHA256
    assert data['sourceRegistrySha256'] == base['sourceRegistrySha256']
    base.update(act='through_act_06', contentRevision='campaign_through_act6_v1')
    for key in ['ingredients', 'cocktails', 'recipes', 'guests', 'encounters', 'slots', 'supply', 'supplyGrants']:
        base[key].extend(copy.deepcopy(data[key]))
    texts = {x['id']: x for x in base['texts']}
    for row in data['texts']:
        if row['id'] in texts: assert texts[row['id']] == row
        texts[row['id']] = copy.deepcopy(row)
    base['texts'] = list(texts.values())
    return base


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--write', action='store_true')
    parser.add_argument('--compose', action='store_true')
    parser.add_argument('--source-root', type=Path, default=SPEC)
    args = parser.parse_args()
    data = build(args.source_root)
    package = ROOT / 'Content/Campaign/act6-adoption.json'
    candidate = ROOT / 'Content/Campaign/act6.json'
    for path, value, write in [(package, data, args.write), (candidate, compose(data), args.compose)]:
        if write:
            with path.open('x', encoding='utf-8', newline='\n') as stream:
                json.dump(value, stream, ensure_ascii=False, indent=2)
                stream.write('\n')
        if path.exists(): assert json.loads(path.read_bytes()) == value, f'{path.name} differs from deterministic adoption'
    assert package.exists(), 'Source package has not been written'
    print('ACT6 SOURCE ADOPTION PASS: 6 encounters + Known fallback, 59 exact source records, supply before lesson without knowledge')


if __name__ == '__main__': main()
