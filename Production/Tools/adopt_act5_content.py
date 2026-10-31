"""Append four source-tracked ACT 5 encounters without changing accepted ACT 4 bytes."""
import argparse
import copy
import hashlib
import json
from pathlib import Path
from adopt_act1_content import ROOT, SPEC

ACCEPTED_ACT4_SHA256 = 'fcc2aeb9c4269c2c9fd34018e4c82b79553ffc3645ad2b8be3970abd673ad6fe'


def accepted_base():
    raw = (ROOT / 'Content/Campaign/act4.json').read_bytes()
    assert hashlib.sha256(raw).hexdigest() == ACCEPTED_ACT4_SHA256, 'ACT 4 is not the accepted source'
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
        role = 'narration' if number in {63, 68, 73, 74, 200, 208} else 'mixed' if number in {201, 209} else 'display' if number in {60, 71, 199, 207} else 'request_summary' if number in {70, 81, 206, 214} else 'spoken'
        tid = f'text_rc1_{number:04d}'
        row = dict(id=tid, value=record['text'], sourceText=record['text'], sourceId=record['id'],
                   sourceHash=record['sha256_utf8'], sourcePath=record['primary_home'], sourceField=record['field'],
                   sourceOrder=str(record['source_order']), role=role,
                   adoption='Verbatim field and original order; bounded ACT 5 adoption after stable ACT 4.')
        if tid in existing:
            assert all(existing[tid][key] == value for key, value in row.items() if key != 'adoption')
            row = copy.deepcopy(existing[tid])
        texts[tid] = row
        return tid

    for number in list(range(60, 82)) + list(range(199, 215)):
        source(number)

    def encounter(number, night, guest, before, after, good, failure, request, tags, memory=False):
        return dict(id=f'enc_rc1_{number:02d}', sourceRow=f'RC1-{number:02d}', night=night, guestId=guest,
                    companionGuestId=None, memoryId='mem_05' if memory else None,
                    memoryVariant='faint_circle_broken_glass_red_light' if memory else None,
                    request=dict(cocktailId=None, anyTags=tags, allTags=[], excludedTags=[]),
                    summaryTextId=source(request), requiredDiscovery=None, princeVisit=None,
                    intro=[dict(textId=source(n), facts=[]) for n in before], farewell=[source(n) for n in after],
                    responses={key: source(good if key == 'good' else failure) for key in ['good', 'weak', 'mismatch', 'bothWeak', 'unknown']}, choiceIntros={})

    return dict(schemaVersion=1, kind='act_adoption_package', act='act_05',
                status='SOURCE_ADOPTED_RUNTIME_INTEGRATION_PENDING', acceptedAct4Sha256=ACCEPTED_ACT4_SHA256,
                sourceRegistrySha256=hashlib.sha256(registry_path.read_bytes()).hexdigest(),
                guests=[dict(id='guest_faust', kind='special', displayTextId=source(60), visualBindingId='visual_faust')],
                texts=list(texts.values()),
                encounters=[encounter(34, 14, 'guest_faust', range(66, 70), range(61, 64), 64, 65, 70, ['clarity', 'awakening']),
                            encounter(35, 14, 'guest_station_worker', [204, 205], [200, 201], 202, 203, 206, ['awakening', 'clarity']),
                            encounter(36, 15, 'guest_postman', [212, 213], [208, 209], 210, 211, 214, ['warmth']),
                            encounter(37, 15, 'guest_faust', range(77, 81), range(72, 75), 75, 76, 81, ['bitterness', 'warmth'], True)],
                slots=[dict(encounterId=f'enc_rc1_{n:02d}', fallbackId=None, whenKnownRecipe=None) for n in range(34, 38)],
                sourceDispositions=[dict(sourceId='SRC-RC1-0071', disposition='Same guest_faust identity as contract display SRC-RC1-0060; both display records preserved.'),
                                    dict(sourceId='SRC-RC1-0199', disposition='Existing guest_station_worker identity retained; visit-specific display source preserved.'),
                                    dict(sourceId='SRC-RC1-0207', disposition='Existing guest_postman identity retained; visit-specific display source preserved.')])


def compose(data):
    base = accepted_base()
    assert data['acceptedAct4Sha256'] == ACCEPTED_ACT4_SHA256
    assert data['sourceRegistrySha256'] == base['sourceRegistrySha256']
    base.update(act='through_act_05', contentRevision='campaign_through_act5_v1')
    for key in ['guests', 'encounters', 'slots']:
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
    package = ROOT / 'Content/Campaign/act5-adoption.json'
    candidate = ROOT / 'Content/Campaign/act5.json'
    for path, value, write in [(package, data, args.write), (candidate, compose(data), args.compose)]:
        if write:
            with path.open('x', encoding='utf-8', newline='\n') as stream:
                json.dump(value, stream, ensure_ascii=False, indent=2)
                stream.write('\n')
        if path.exists():
            assert json.loads(path.read_bytes()) == value, f'{path.name} differs from deterministic adoption'
    assert package.exists(), 'Source package has not been written'
    print('ACT 5 SOURCE ADOPTION PASS: 4 encounters, 38 exact source records; no gameplay resources added')


if __name__ == '__main__':
    main()
