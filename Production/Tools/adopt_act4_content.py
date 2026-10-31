"""Append source-tracked ACT 4, preserving the accepted ACT 1/2/3 byte boundary."""
import argparse
import copy
import hashlib
import json
from pathlib import Path
from adopt_act1_content import ROOT, SPEC

ACCEPTED_ACT3_SHA256 = '9db4998cd4f3dd8fc1c0371e8396504c07c0cce253e504caf39f48ec370555f0'


def accepted_base():
    raw = (ROOT / 'Content/Campaign/act3.json').read_bytes()
    assert hashlib.sha256(raw).hexdigest() == ACCEPTED_ACT3_SHA256, 'ACT 3 is not the accepted source'
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
        role = 'narration' if number in {32, 83, 85, 89, 94, 96, 192} else 'mixed' if number == 193 else 'display' if number in {30, 82, 93, 191} else 'request_summary' if number in {38, 92, 103, 198} else 'spoken'
        tid = f'text_rc1_{number:04d}'
        row = dict(id=tid, value=record['text'], sourceText=record['text'], sourceId=record['id'],
                   sourceHash=record['sha256_utf8'], sourcePath=record['primary_home'], sourceField=record['field'],
                   sourceOrder=str(record['source_order']), role=role,
                   adoption='Verbatim field and original order; bounded ACT 4 adoption after stable ACT 3.')
        if tid in existing:
            assert all(existing[tid][key] == value for key, value in row.items() if key != 'adoption')
            row = copy.deepcopy(existing[tid])
        texts[tid] = row
        return tid

    for number in list(range(30, 40)) + list(range(82, 104)) + list(range(191, 199)):
        source(number)

    def encounter(number, night, guest, before, after, good, failure, request, tags, memory=False):
        return dict(id=f'enc_rc1_{number:02d}', sourceRow=f'RC1-{number:02d}', night=night, guestId=guest,
                    companionGuestId=None, memoryId='mem_04' if memory else None,
                    memoryVariant='stage_skull_shadow' if memory else None,
                    request=dict(cocktailId=None, anyTags=tags, allTags=[], excludedTags=[]),
                    summaryTextId=source(request), requiredDiscovery=None, princeVisit=None,
                    intro=[dict(textId=source(n), facts=[]) for n in before], farewell=[source(n) for n in after],
                    responses={key: source(good if key == 'good' else failure) for key in ['good', 'weak', 'mismatch', 'bothWeak', 'unknown']}, choiceIntros={})

    lesson = encounter(30, 12, 'guest_station_worker', [35, 36, 37], [31, 32], 33, 34, 38, [])
    lesson.update(requiredDiscovery='recipe_french_75', discoveryRetryTextId=source(39))
    lesson['request']['cocktailId'] = 'cocktail_french_75'
    for line, facts in zip(lesson['intro'], [['identity'], ['method', 'amount:gin', 'amount:fresh_lemon_juice', 'amount:simple_syrup'], ['amount:champagne', 'preparation', 'garnish']]):
        line['facts'] = [dict(recipeId='recipe_french_75', fact=f, sourceIds=[line['textId'].replace('text_rc1_', 'SRC-RC1-')]) for f in facts]
    fallback = copy.deepcopy(next(e for e in base['encounters'] if e['id'] == 'enc_rc1_14_known'))
    fallback.update(id='enc_rc1_30_known', sourceRow='RC1-30', night=12)
    return dict(schemaVersion=1, kind='act_adoption_package', act='act_04',
                status='SOURCE_ADOPTED_RUNTIME_INTEGRATION_PENDING', acceptedAct3Sha256=ACCEPTED_ACT3_SHA256,
                sourceRegistrySha256=hashlib.sha256(registry_path.read_bytes()).hexdigest(),
                ingredients=[dict(id='fresh_lemon_juice', label='프레시 레몬 주스'), dict(id='champagne', label='샴페인')],
                cocktails=[dict(id='cocktail_french_75', name='프렌치 75', tags=['awakening'])],
                recipes=[dict(id='recipe_french_75', cocktailId='cocktail_french_75', method='shake',
                              amounts=[dict(ingredientId=k, ml=v) for k, v in [('gin', 30), ('fresh_lemon_juice', 15), ('simple_syrup', 15), ('champagne', 60)]],
                              dashIngredient=None, requiredGarnish=None, glassware='flute', optionalGarnish=False,
                              preparation=['shaker_ice', 'closed_shake', 'strain_from_shaker', 'late_measured_pour', 'gentle_stir', 'no_serving_ice'],
                              garnishPosition='rim', lateIngredient='champagne', layerOrder=[])],
                supply=['fresh_lemon_juice', 'champagne'],
                supplyGrants=[dict(beforeEncounter='enc_rc1_30', ingredientIds=['fresh_lemon_juice', 'champagne'])],
                guests=[dict(id='guest_hamlet', kind='special', displayTextId=source(82), visualBindingId='visual_hamlet')],
                texts=list(texts.values()),
                encounters=[lesson,
                            encounter(31, 12, 'guest_hamlet', range(88, 92), range(83, 86), 86, 87, 92, ['clarity', 'bitterness']),
                            encounter(32, 13, 'guest_repairer', [196, 197], [192, 193], 194, 195, 198, ['clarity', 'warmth']),
                            encounter(33, 13, 'guest_hamlet', range(99, 103), range(94, 97), 97, 98, 103, ['clarity', 'warmth'], True), fallback],
                slots=[dict(encounterId=f'enc_rc1_{n:02d}', fallbackId='enc_rc1_30_known' if n == 30 else None,
                            whenKnownRecipe='recipe_french_75' if n == 30 else None) for n in range(30, 34)],
                sourceDispositions=[dict(sourceId='SRC-RC1-0030', disposition='Existing guest_station_worker identity retained; visit-specific display source preserved.'),
                                    dict(sourceId='SRC-RC1-0039', disposition='Runtime Retry reading after failed required discovery; advancing consumes its existing reading receipt.'),
                                    dict(sourceId='SRC-RC1-0093', disposition='Same guest_hamlet identity as question display SRC-RC1-0082; both display records preserved.'),
                                    dict(sourceId='SRC-RC1-0191', disposition='Existing guest_repairer identity retained; visit-specific display source preserved.'),
                                    dict(sourceId='PRODUCTION-KNOWN-FRENCH75-FALLBACK', disposition='Copy accepted enc_rc1_14_known ordinary station-worker request; remap only id/sourceRow/night for RC1-30. No altered earlier definition, repeated lesson or invented dialogue.')],
                recipeAdoption=dict(source='SURLHWA_CANON/31_CURRENT_AND_ORIGINAL_COCKTAILS.md#drink-08',
                                    officialUrl='https://iba-world.com/iba-cocktail/french-75/', reverifiedOn='2026-10-31',
                                    disposition='Nominal 30/15/15/60 ml replace source oz-rounding artifacts. Canon Simple Syrup expresses Sugar Syrup; Champagne remains measured Pour after Strain, then GentleStir. Flute is metadata; no garnish.'))


def compose(data):
    base = accepted_base()
    assert data['acceptedAct3Sha256'] == ACCEPTED_ACT3_SHA256
    assert data['sourceRegistrySha256'] == base['sourceRegistrySha256']
    base.update(act='through_act_04', contentRevision='campaign_through_act4_v1')
    for key in ['ingredients', 'cocktails', 'recipes', 'guests', 'encounters', 'slots', 'supply', 'supplyGrants']:
        base[key].extend(copy.deepcopy(data[key]))
    texts = {x['id']: x for x in base['texts']}
    for row in data['texts']:
        if row['id'] in texts:
            assert texts[row['id']] == row
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
    package = ROOT / 'Content/Campaign/act4-adoption.json'
    candidate = ROOT / 'Content/Campaign/act4.json'
    for path, value, write in [(package, data, args.write), (candidate, compose(data), args.compose)]:
        if write:
            with path.open('x', encoding='utf-8', newline='\n') as stream:
                json.dump(value, stream, ensure_ascii=False, indent=2)
                stream.write('\n')
        if path.exists():
            assert json.loads(path.read_bytes()) == value, f'{path.name} differs from deterministic adoption'
    assert package.exists(), 'Source package has not been written'
    print('ACT 4 SOURCE ADOPTION PASS: 4 encounters + Known fallback, 40 original source records; runtime integration separate')


if __name__ == '__main__':
    main()
