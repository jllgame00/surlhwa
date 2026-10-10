import hashlib
import json
import xml.etree.ElementTree as ET
from pathlib import Path
production = Path(__file__).resolve().parents[1]
evidence = production / 'Validation/Evidence'
xml = evidence / 'final-unity-tests.xml'
result = ET.parse(xml).getroot()
summary = {'evidenceTier': 'Native Unity 6000.3.7f1 EditMode', 'result': result.attrib['result'],
           'total': int(result.attrib['total']), 'passed': int(result.attrib['passed']), 'failed': int(result.attrib['failed']),
           'durationSeconds': float(result.attrib['duration']), 'testRunSha256': hashlib.sha256(xml.read_bytes()).hexdigest(),
           'tests': [{'name': case.attrib['fullname'], 'result': case.attrib['result']} for case in result.iter('test-case')],
           'redEvidence': ['foundation-red.log: 0/23', 'application-red.log: 23/25', 'spatial-red.xml: 0/4',
                           'contact-red.xml: 0/1', 'stool-torso-red.xml: 0/1', 'bundle-regeneration-red.xml: 0/1'],
           'scope': 'Content/schema/IDs/Unicode/immutability, review selection, scene references/anchors/cameras/materials/counter/stool/bundle regeneration. Artistic approval is excluded.'}
(production / 'Validation/Manifests/tests.json').write_text(json.dumps(summary, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
print(summary['result'], f"{summary['passed']}/{summary['total']} native Unity tests")
if summary['result'] != 'Passed' or summary['failed']: raise SystemExit(1)
