"""Bounded ACT 2 source adoption, pending runtime schema/presentation integration.

Reads the accepted ACT 1 bytes and immutable Canon registry without changing them.
This package cannot be loaded as a campaign or silently replace an accepted bundle.
"""
import argparse
import copy
import hashlib
import json
from pathlib import Path
from adopt_act1_content import ROOT, SPEC


def build(source_root):
    registry_path = source_root / 'SURLHWA_CANON/_verification/wording_registry.json'
    registry = {r['id']: r for r in json.loads(registry_path.read_text(encoding='utf-8'))['records']}
    baseline = (ROOT / 'Content/Campaign/act1.json').read_bytes()
    base = json.loads(baseline)
    texts = {}
    # Explicit source-role audit; no speaker/role inference from line text.
    narration = {11, 20, 22, 24, 25, 26, 27, 42, 106, 110, 115, 118, 121, 128, 160, 168}
    mixed = {161, 169}

    def source(number, role=None):
        record = registry[f'SRC-RC1-{number:04d}']
        assert hashlib.sha256(record['text'].encode('utf-8')).hexdigest() == record['sha256_utf8']
        role = role or ('narration' if number in narration else 'mixed' if number in mixed else 'spoken')
        tid = f'text_rc1_{number:04d}'
        row = dict(id=tid, value=record['text'], sourceText=record['text'], sourceId=record['id'],
                   sourceHash=record['sha256_utf8'], sourcePath=record['primary_home'], sourceField=record['field'],
                   sourceOrder=str(record['source_order']), role=role,
                   adoption='Verbatim field and original order; bounded ACT 2 adoption after Gate 2.')
        if tid in texts:
            assert texts[tid] == row, (tid, 'conflicting source role')
        texts[tid] = row
        return tid

    def line(n, facts=()):
        return dict(textId=source(n), facts=[dict(recipeId='recipe_hanky_panky', fact=f,
                    sourceIds=[f'SRC-RC1-{n:04d}']) for f in facts])

    encounters = []

    def encounter(n, night, guest, before, after, success, failure, summary, tags,
                  companion=None, memory=None):
        row = dict(id=f'enc_rc1_{n:02d}', sourceRow=f'RC1-{n:02d}', night=night, guestId=guest,
                   companionGuestId=companion, memoryId='mem_02' if memory else None,
                   memoryVariant='swing_empty_place' if memory else None,
                   request=dict(cocktailId=None, anyTags=tags, allTags=[], excludedTags=[]),
                   summaryTextId=source(summary, 'request_summary'), requiredDiscovery=None,
                   princeVisit=None, intro=[line(x) for x in before], farewell=[source(x) for x in after],
                   responses={k: source(success if k == 'good' else failure)
                              for k in ['good', 'weak', 'mismatch', 'bothWeak', 'unknown']}, choiceIntros={})
        encounters.append(row)
        return row

    encounter(18, 7, 'guest_chunhyang', range(14, 18), [10, 11], 12, 13, 18, ['warmth', 'longing'])
    encounter(19, 7, 'guest_postman', [164, 165], [160, 161], 162, 163, 166, ['warmth'])
    encounter(20, 7, 'guest_mongryong', range(131, 135), [127, 128], 129, 130, 135, ['warmth', 'clarity'])
    encounter(21, 8, 'guest_mongryong', range(109, 113), [105, 106], 107, 108, 113,
              ['warmth', 'bitterness'], companion='guest_chunhyang')
    lesson = encounter(22, 8, 'guest_repairer', [45, 46, 47], [41, 42], 43, 44, 48, [])
    lesson['request']['cocktailId'] = 'cocktail_hanky_panky'
    lesson['requiredDiscovery'] = 'recipe_hanky_panky'
    lesson['intro'] = [line(45, ['identity']), line(46, ['amount:london_dry_gin', 'amount:sweet_red_vermouth', 'amount:fernet']),
                       line(47, ['method', 'preparation', 'garnish'])]
    encounter(23, 9, 'guest_chunhyang', range(25, 29), [20, 21, 22], 23, 24, 29, ['softness', 'warmth'])
    encounter(24, 9, 'guest_repairer', [172, 173], [168, 169], 170, 171, 174, ['clarity'])
    encounter(25, 9, 'guest_chunhyang', range(121, 125), [115, 116, 117, 118], 119, 120, 125,
              ['warmth', 'clarity'], companion='guest_mongryong', memory=True)
    fallback = copy.deepcopy(next(x for x in base['encounters'] if x['id'] == 'enc_rc1_13'))
    fallback.update(id='enc_rc1_22_known', sourceRow='RC1-22', night=8, guestId='guest_repairer',
                    companionGuestId=None, memoryId=None, memoryVariant=None)
    encounters.append(fallback)
    referenced = {fallback['summaryTextId'], *fallback['farewell'], *fallback['responses'].values(),
                  *(x['textId'] for x in fallback['intro'])}
    for row in base['texts']:
        if row['id'] in referenced:
            texts[row['id']] = copy.deepcopy(row)
    guests = [dict(id=gid, kind='special', displayTextId=source(n, 'display'), visualBindingId=visual)
              for gid, n, visual in [('guest_chunhyang', 9, 'visual_chunhyang'),
                                     ('guest_mongryong', 126, 'visual_mongryong')]]
    recipe = dict(id='recipe_hanky_panky', cocktailId='cocktail_hanky_panky', method='stir',
                  amounts=[dict(ingredientId=i, ml=v) for i, v in
                           [('london_dry_gin', 45), ('sweet_red_vermouth', 45), ('fernet', 7.5)]],
                  dashIngredient=None, requiredGarnish='orange_zest', garnishPosition='rim',
                  glassware='cocktail', optionalGarnish=False,
                  preparation=['mixing_ice', 'stir_after_ingredients', 'strain_to_serving'], lateIngredient=None, layerOrder=[])
    return dict(schemaVersion=1, kind='act_adoption_package', act='act_02',
                status='SOURCE_ADOPTED_RUNTIME_INTEGRATION_PENDING',
                acceptedAct1Sha256=hashlib.sha256(baseline).hexdigest(),
                sourceRegistrySha256=hashlib.sha256(registry_path.read_bytes()).hexdigest(),
                ingredients=[dict(id='london_dry_gin', label='런던 드라이 진'), dict(id='fernet', label='페르네')],
                cocktails=[dict(id='cocktail_hanky_panky', name='Hanky Panky', tags=['bitterness'])], recipes=[recipe],
                recipeSource=dict(url='https://iba-world.com/iba-cocktail/hanky-panky/', verifiedOn='2026-10-31',
                                  disposition='ADAPT_WITH_PROVENANCE',
                                  note='Nominal ml retained; game ice/Stir/Strain evidence abstracts chilling. OrangeZest/Rim is the Canon game requirement.'),
                guests=guests, texts=list(texts.values()), encounters=encounters,
                slots=[dict(encounterId=f'enc_rc1_{n:02d}', fallbackId='enc_rc1_22_known' if n == 22 else None,
                            whenKnownRecipe='recipe_hanky_panky' if n == 22 else None) for n in range(18, 26)],
                supplyGrants=[dict(beforeEncounter='enc_rc1_22', ingredientIds=['london_dry_gin', 'fernet'])])


def compose(data):
    base = json.loads((ROOT / 'Content/Campaign/act1.json').read_text(encoding='utf-8'))
    base.update(schemaVersion=2, act='through_act_02', contentRevision='campaign_through_act2_v1')
    for field in ['ingredients', 'cocktails', 'recipes', 'guests']:
        base[field].extend(data[field])
    text_map = {x['id']: x for x in base['texts']}
    for row in data['texts']:
        if row['id'] in text_map:
            assert text_map[row['id']] == row
        text_map[row['id']] = row
    base['texts'] = list(text_map.values())
    for row in base['encounters']:
        row.update(companionGuestId=None, memoryId=None, memoryVariant=None)
    base['encounters'].extend(data['encounters'])
    base['slots'].extend(data['slots'])
    base['supply'].extend(x['id'] for x in data['ingredients'])
    base['supplyGrants'] = data['supplyGrants']
    return base


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--write', action='store_true')
    parser.add_argument('--compose', action='store_true', help='Write a separate ACT 1–2 runtime candidate; never replace ACT 1')
    parser.add_argument('--source-root', type=Path, default=SPEC)
    args = parser.parse_args()
    data = build(args.source_root)
    destination = ROOT / 'Content/Campaign/act2-adoption.json'
    if args.write:
        with destination.open('x', encoding='utf-8', newline='\n') as stream:
            json.dump(data, stream, ensure_ascii=False, indent=2)
            stream.write('\n')
    assert json.loads(destination.read_text(encoding='utf-8')) == data, 'ACT 2 source package differs'
    base = compose(data)
    candidate = ROOT / 'Content/Campaign/act2.json'
    if args.compose:
        with candidate.open('x', encoding='utf-8', newline='\n') as stream:
            json.dump(base, stream, ensure_ascii=False, indent=2)
            stream.write('\n')
    if candidate.exists():
        assert json.loads(candidate.read_text(encoding='utf-8')) == base, 'Composed ACT 2 candidate drifted from its source packages'
    print(f"ACT 2 SOURCE ADOPTION PASS: 8 slots, {len(data['texts'])} text records; runtime integration pending")


if __name__ == '__main__':
    main()
