"""Append exact ACT7 visits and one ending, preserving accepted ACT6 history."""
import argparse
import copy
import hashlib
import json
from pathlib import Path
from adopt_act1_content import ROOT, SPEC

ACCEPTED_ACT6_SHA256 = 'e6bb7131fc5424186bc2cf4b1b15160b8cead275bcf7c9824ee1ccd1bfb5dfcf'


def accepted_base():
    raw=(ROOT/'Content/Campaign/act6.json').read_bytes()
    assert hashlib.sha256(raw).hexdigest()==ACCEPTED_ACT6_SHA256, 'ACT6 is not the accepted source'
    return json.loads(raw)


def build(source_root):
    base=accepted_base()
    registry_path=source_root/'SURLHWA_CANON/_verification/wording_registry.json'
    records={r['id']:r for r in json.loads(registry_path.read_bytes())['records']}
    texts={}
    def source(number):
        record=records[f'SRC-RC1-{number:04}']
        assert hashlib.sha256(record['text'].encode('utf8')).hexdigest()==record['sha256_utf8']
        role='narration' if number in set(range(1,9))|{137,139,142,148,151,154,156,232,240} else 'mixed' if number in {233,241} else 'display' if number in {136,147,231,239} else 'request_summary' if number in {146,158,238,246} else 'spoken'
        tid=f'text_rc1_{number:04}'
        texts[tid]=dict(id=tid,value=record['text'],sourceText=record['text'],sourceId=record['id'],sourceHash=record['sha256_utf8'],sourcePath=record['primary_home'],sourceField=record['field'],sourceOrder=str(record['source_order']),role=role,adoption='Verbatim field and original order; bounded ACT7 and common ending adoption after stable ACT6.')
        return tid
    for number in list(range(136,159))+list(range(231,247))+list(range(1,9)):source(number)
    def encounter(number,guest,intro,farewell,good,other,summary,tags):
        return dict(id=f'enc_rc1_{number}',sourceRow=f'RC1-{number}',night=19+(number-44)//2,guestId=guest,
                    companionGuestId=None,memoryId='mem_07' if number==47 else None,memoryVariant='anchor_ship_fragment' if number==47 else None,
                    request=dict(cocktailId=None,anyTags=tags,allTags=[],excludedTags=[]),summaryTextId=source(summary),requiredDiscovery=None,princeVisit=None,
                    intro=[dict(textId=source(n),facts=[]) for n in intro],farewell=[source(n) for n in farewell],
                    responses={key:source(good if key=='good' else other) for key in ['good','weak','mismatch','bothWeak','unknown']},choiceIntros={})
    return dict(schemaVersion=1,kind='act_adoption_package',act='act_07',status='SOURCE_ADOPTED_RUNTIME_INTEGRATION_PENDING',
                acceptedAct6Sha256=ACCEPTED_ACT6_SHA256,sourceRegistrySha256=hashlib.sha256(registry_path.read_bytes()).hexdigest(),
                guests=[dict(id='guest_odysseus',kind='special',displayTextId=source(136),visualBindingId='visual_odysseus')],texts=list(texts.values()),
                encounters=[encounter(44,'guest_station_worker',[236,237],[232,233],234,235,238,['warmth','bitterness']),
                            encounter(45,'guest_odysseus',range(142,146),range(137,140),140,141,146,['warmth','bitterness']),
                            encounter(46,'guest_postman',[244,245],[240,241],242,243,246,['warmth','clarity']),
                            encounter(47,'guest_odysseus',range(154,158),range(148,152),152,153,158,['warmth','clarity'])],
                slots=[dict(encounterId=f'enc_rc1_{n}',fallbackId=None,whenKnownRecipe=None) for n in range(44,48)],
                ending=dict(lines=[source(n) for n in range(1,9)],recallAfterLine=1,recallByChoice=copy.deepcopy(base['prince']['recalls']),codaTextId=source(8)),
                sourceDispositions=[dict(sourceId='SRC-RC1-0147',disposition='Same guest_odysseus identity as Return display SRC-RC1-0136; both original display fields remain.'),
                                    dict(sourceId='SRC-RC1-0231',disposition='Existing guest_station_worker display/identity retained; visit-specific display preserved.'),
                                    dict(sourceId='SRC-RC1-0239',disposition='Existing guest_postman display/identity retained; visit-specific display preserved.'),
                                    dict(sourceId='PRODUCTION-ENDING-PRINCE-RECALL',disposition='Explicit new placement after common ending line SRC-RC1-0002 of previously adopted SRC-RC1-0412/0413/0414 for saved keep/seek/none choice. Present as remembered Prince, not a returning guest or a claimed RC1 ending callback. No choice, farewell variant, star or success history is synthesized; all eight common source lines remain in order.')])


def compose(data):
    base=accepted_base()
    assert data['acceptedAct6Sha256']==ACCEPTED_ACT6_SHA256
    assert data['sourceRegistrySha256']==base['sourceRegistrySha256']
    base.update(act='through_act_07',contentRevision='campaign_through_act7_v1')
    for key in ['guests','texts','encounters','slots']:base[key].extend(copy.deepcopy(data[key]))
    base['ending']=copy.deepcopy(data['ending'])
    return base


def main():
    parser=argparse.ArgumentParser();parser.add_argument('--write',action='store_true');parser.add_argument('--compose',action='store_true')
    parser.add_argument('--source-root',type=Path,default=SPEC);args=parser.parse_args()
    data=build(args.source_root);package=ROOT/'Content/Campaign/act7-adoption.json';candidate=ROOT/'Content/Campaign/act7.json'
    for path,value,write in [(package,data,args.write),(candidate,compose(data),args.compose)]:
        if write:
            with path.open('x',encoding='utf8',newline='\n') as stream:json.dump(value,stream,ensure_ascii=False,indent=2);stream.write('\n')
        if path.exists():assert json.loads(path.read_bytes())==value,f'{path.name} differs from deterministic adoption'
    assert package.exists(),'Source package has not been written'
    print('ACT7 SOURCE ADOPTION PASS: 4 encounters, 47 exact records, one common ending with existing remembered Prince quote')


if __name__=='__main__':main()
