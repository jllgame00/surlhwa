"""Append four Canon ACT 3 encounters without modifying accepted ACT 1/2 bytes."""
import argparse
import copy
import hashlib
import json
from pathlib import Path
from adopt_act1_content import ROOT, SPEC

ACCEPTED_ACT2_SHA256 = 'e38fe3dce7cdcb004dd0731df730925c2531562379fda7fde95d922c0d620fa1'


def accepted_base():
    raw = (ROOT / 'Content/Campaign/act2.json').read_bytes()
    assert hashlib.sha256(raw).hexdigest() == ACCEPTED_ACT2_SHA256, 'ACT 2 is not the accepted source'
    return json.loads(raw)


def build(source_root):
    base = accepted_base()
    registry_path = source_root / 'SURLHWA_CANON/_verification/wording_registry.json'
    records = {r['id']: r for r in json.loads(registry_path.read_bytes())['records']}
    existing = {x['id']: x for x in base['texts']}
    texts = {}
    narration = {176, 184, 249, 250, 255, 260, 265}
    mixed = {177, 185}
    display = {175, 183, 247, 258}
    summary = {182, 190, 257, 267}

    def source(number):
        record = records[f'SRC-RC1-{number:04d}']
        assert hashlib.sha256(record['text'].encode('utf-8')).hexdigest() == record['sha256_utf8']
        role = 'narration' if number in narration else 'mixed' if number in mixed else 'display' if number in display else 'request_summary' if number in summary else 'spoken'
        tid = f'text_rc1_{number:04d}'
        row = dict(id=tid, value=record['text'], sourceText=record['text'], sourceId=record['id'],
                   sourceHash=record['sha256_utf8'], sourcePath=record['primary_home'], sourceField=record['field'],
                   sourceOrder=str(record['source_order']), role=role,
                   adoption='Verbatim field and original order; bounded ACT 3 adoption after stable ACT 2.')
        if tid in existing:
            assert all(existing[tid][key] == value for key, value in row.items() if key != 'adoption')
            row = copy.deepcopy(existing[tid])
        texts[tid] = row
        return tid

    # Preserve all source records, including identical visit-specific guestName fields.
    for number in list(range(175, 191)) + list(range(247, 268)):
        source(number)

    def encounter(number, night, guest, before, after, good, failure, request, tags, memory=False):
        return dict(id=f'enc_rc1_{number:02d}', sourceRow=f'RC1-{number:02d}', night=night, guestId=guest,
                    companionGuestId=None, memoryId='mem_03' if memory else None,
                    memoryVariant='windmill_fragment' if memory else None,
                    request=dict(cocktailId=None, anyTags=tags, allTags=[], excludedTags=[]),
                    summaryTextId=source(request), requiredDiscovery=None, princeVisit=None,
                    intro=[dict(textId=source(n), facts=[]) for n in before],
                    farewell=[source(n) for n in after],
                    responses={key: source(good if key == 'good' else failure)
                               for key in ['good', 'weak', 'mismatch', 'bothWeak', 'unknown']}, choiceIntros={})

    return dict(schemaVersion=1, kind='act_adoption_package', act='act_03',
                status='SOURCE_ADOPTED_RUNTIME_INTEGRATION_PENDING', acceptedAct2Sha256=ACCEPTED_ACT2_SHA256,
                sourceRegistrySha256=hashlib.sha256(registry_path.read_bytes()).hexdigest(),
                guests=[dict(id='guest_quixote', kind='special', displayTextId=source(258), visualBindingId='visual_quixote')],
                texts=list(texts.values()),
                encounters=[encounter(26, 10, 'guest_postman', [180, 181], [176, 177], 178, 179, 182, ['warmth', 'bitterness']),
                            encounter(27, 10, 'guest_quixote', range(263, 267), [259, 260], 261, 262, 267, ['warmth', 'awakening']),
                            encounter(28, 11, 'guest_quixote', range(253, 257), range(248, 251), 251, 252, 257, ['bitterness', 'warmth'], True),
                            encounter(29, 11, 'guest_snow_worker', [188, 189], [184, 185], 186, 187, 190, ['bitterness'])],
                slots=[dict(encounterId=f'enc_rc1_{n:02d}', fallbackId=None, whenKnownRecipe=None) for n in range(26, 30)],
                sourceDispositions=[dict(sourceId='SRC-RC1-0175', disposition='Existing guest_postman identity retained; visit-specific display source preserved.'),
                                    dict(sourceId='SRC-RC1-0183', disposition='Existing accepted text record and guest_snow_worker identity reused unchanged.'),
                                    dict(sourceId='SRC-RC1-0247', disposition='Same guest_quixote identity as declaration display SRC-RC1-0258; both source records preserved.')])


def compose(data):
    base = accepted_base()
    assert data['acceptedAct2Sha256'] == ACCEPTED_ACT2_SHA256
    assert data['sourceRegistrySha256'] == base['sourceRegistrySha256']
    base.update(act='through_act_03', contentRevision='campaign_through_act3_v1')
    base['guests'].extend(copy.deepcopy(data['guests']))
    texts = {x['id']: x for x in base['texts']}
    for row in data['texts']:
        if row['id'] in texts:
            assert texts[row['id']] == row
        texts[row['id']] = copy.deepcopy(row)
    base['texts'] = list(texts.values())
    base['encounters'].extend(copy.deepcopy(data['encounters']))
    base['slots'].extend(copy.deepcopy(data['slots']))
    return base


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--write', action='store_true')
    parser.add_argument('--compose', action='store_true')
    parser.add_argument('--source-root', type=Path, default=SPEC)
    args = parser.parse_args()
    data = build(args.source_root)
    package = ROOT / 'Content/Campaign/act3-adoption.json'
    candidate = ROOT / 'Content/Campaign/act3.json'
    for path, value, write in [(package, data, args.write), (candidate, compose(data), args.compose)]:
        if write:
            with path.open('x', encoding='utf-8', newline='\n') as stream:
                json.dump(value, stream, ensure_ascii=False, indent=2)
                stream.write('\n')
        if path.exists():
            assert json.loads(path.read_bytes()) == value, f'{path.name} differs from deterministic adoption'
    assert package.exists(), 'Source package has not been written'
    print('ACT 3 SOURCE ADOPTION PASS: 4 encounters, 37 original source records; runtime integration separate')


if __name__ == '__main__':
    main()
