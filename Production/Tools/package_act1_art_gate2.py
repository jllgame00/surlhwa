"""Bundle current real Player art evidence; keep source previews and failed runs distinct."""
from pathlib import Path
import datetime
import html
import json
import subprocess
import sys
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
VALIDATION = ROOT / 'Validation'
EVIDENCE = VALIDATION / 'Evidence'


def read(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))


def href(path):
    return html.escape(path.relative_to(VALIDATION).as_posix(), quote=True)


records, sections = [], []
for folder in sorted(EVIDENCE.glob('gate2-player-art-*')):
    if not folder.is_dir():
        continue
    result = read(folder / 'player-result.json') if (folder / 'player-result.json').exists() else {}
    process = read(folder / 'process.json') if (folder / 'process.json').exists() else {}
    expected = read(folder / 'expected-run.json')
    review = read(folder / 'render-review.json') if (folder / 'render-review.json').exists() else {}
    current = False
    status = review.get('status', result.get('status', 'INCOMPLETE'))
    if result.get('status') == 'PASS' and process.get('exitCode') == 0 and not process.get('timedOut'):
        audit = subprocess.run([sys.executable, str(ROOT / 'Tools/act1_inventory.py'),
                                'verify', str(folder / 'inventory.json')], capture_output=True)
        current = audit.returncode == 0
        status = 'PASS_CURRENT' if current else 'SUPERSEDED_PASS'
    media = read(folder / 'media.json') if (folder / 'media.json').exists() else {}
    row = dict(name=folder.name, status=status, current=current, height=expected['height'],
               fixture=expected['fixture'], checks=len(result.get('checks', [])),
               captures=result.get('captures', 0), media=media.get('status') == 'PASS',
               errors=result.get('errors', []), review=review)
    records.append(row)
    if not current:
        continue
    labels = ['prince-intro', 'prepared-0-', 'prepared-5-', 'prepared-6-', 'prepared-13-',
              'prince-farewell-0', 'earned-memory', 'settings-large', 'codex-clues-5']
    stills = [p for p in sorted(folder.glob('*.png')) if any(label in p.name for label in labels)]
    if folder.name == 'gate2-player-art-open-720-final':
        stills += [folder / 'frames/00933.png', folder / 'frames/01114.png']
    frame_labels = {'00933': 'Ordinary guest departure · actual recorded frame',
                    '01114': 'Active Float · spoon, stream and grip at 720p'}
    cards = ''.join(f'<figure><a href="{href(p)}"><img loading="lazy" src="{href(p)}" alt="{html.escape(frame_labels.get(p.stem, p.stem))}"></a><figcaption>{html.escape(frame_labels.get(p.stem, p.stem))}</figcaption></figure>' for p in stills)
    clips = ''
    if media.get('status') == 'PASS':
        for p in sorted(folder.glob('*.mp4')):
            if p.name == 'player-playthrough.mp4':
                continue
            clips += f'<figure><video controls preload="none" src="{href(p)}"></video><figcaption>{html.escape(p.stem)}</figcaption></figure>'
        clips += f'<p><a href="{href(folder / "player-playthrough.mp4")}">전체 ACT 1 기록</a> · <a href="{href(folder / "media.json")}">프레임·음원 기록 정보</a></p>'
    gallery=f'<div class="grid">{cards}{clips}</div>'
    if expected['height'] != 720 or expected['fixture'] != 'open':
        gallery='<details><summary>실제 해상도 화면 펼치기</summary>'+gallery+'</details>'
    sections.append(f'<section><h2>{expected["width"]} × {expected["height"]} · {html.escape(folder.name)}</h2><p>{row["checks"]} checks · {row["captures"]} captures · 검증된 현재 Player</p>{gallery}</section>')

requirements = dict(
    open720=any(r['current'] and r['fixture'] == 'open' and r['height'] == 720 and r['media'] for r in records),
    open1080=any(r['current'] and r['fixture'] == 'open' and r['height'] == 1080 for r in records),
    openContinue=any(r['current'] and r['fixture'] == 'continue' for r in records),
    quietContinue=any(r['current'] and r['fixture'] == 'quiet' and 'continue' in r['name'] for r in records))
test_path = EVIDENCE / 'art-integration-final-green.xml'
tests = ET.parse(test_path).getroot()
requirements['affectedUnitySuite'] = tests.get('failed') == '0' and int(tests.get('passed', 0)) >= 29
integration = read(VALIDATION / 'Manifests/art-integration-checks.json')
requirements['referencesAndPreservation'] = integration['status'] == 'PASS'
continuity_path = VALIDATION / 'Manifests/art-save-continuity.json'
requirements['saveContinuity'] = continuity_path.exists() and read(continuity_path)['status'] == 'PASS'
build = read(VALIDATION / 'Manifests/build-act1.json')
requirements['playerBuild'] = build['result'] == 'Succeeded' and build['errors'] == 0
status = 'READY_FOR_OWNER_ART_REVIEW_WITH_NATIVE_INPUT_DEBT' if all(requirements.values()) else 'INTEGRATION_REVIEW_IN_PROGRESS'
manifest = dict(generatedUtc=datetime.datetime.now(datetime.timezone.utc).isoformat(), status=status,
                sourceCheckpoint='dcc0297', requirements=requirements, runs=records,
                unity=dict(total=int(tests.get('total')), passed=int(tests.get('passed')), failed=int(tests.get('failed'))),
                build=build,
                nativeInput='Existing environment debt retained; no new native-input claim',
                ownerApproval='HUMAN_SIGNOFF_REQUIRED; PD01–PD11 remain OPEN',
                scope='ACT 1 Production Art Pass only; C1 and HG01–HG04 retained')
(VALIDATION / 'Manifests/art-gate2-review.json').write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
rows = ''.join(f'<tr><td>{html.escape(r["name"])}</td><td>{r["status"]}</td><td>{r["checks"]}</td><td>{r["captures"]}</td></tr>' for r in records)
comparison=''
current720=next((r for r in records if r['current'] and r['fixture']=='open' and r['height']==720),None)
before=EVIDENCE/'gate2-player-open-720-04/014-prince-intro-First.png'
if current720 and before.exists():
    after=EVIDENCE/current720['name']/'014-prince-intro-First.png'
    comparison=f'<section><h2>같은 C1 구도, 새 ACT 1 표현</h2><div class="grid"><figure><a href="{href(before)}"><img src="{href(before)}" alt="이전 ACT 1 Player"></a><figcaption>이전 ACT 1 Player · 2026-10-14 기록</figcaption></figure><figure><a href="{href(after)}"><img src="{href(after)}" alt="현재 ACT 1 아트 Player"></a><figcaption>현재 아트 패스 · 동일 카메라</figcaption></figure></div></section>'
page = f'''<!doctype html><html lang="ko"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>SURLHWA · ACT 1 Production Art / Gate 2</title>
<style>*{{box-sizing:border-box}}body{{margin:0;background:#172127;color:#ecdfc6;font:17px/1.7 system-ui,sans-serif}}main{{max-width:1420px;margin:auto;padding:48px 28px}}h1,h2{{font-weight:500;line-height:1.3}}h1{{font-size:36px}}h2{{margin-top:48px;font-size:24px}}a{{color:#d9b77b}}p{{max-width:1080px}}.notice{{border-left:4px solid #c29a5c;background:#293139;padding:18px 24px}}.grid{{display:grid;grid-template-columns:repeat(auto-fit,minmax(min(100%,520px),1fr));gap:22px}}figure{{margin:0;padding:8px;background:#10171b}}img,video{{width:100%;display:block}}figcaption{{font-size:13px;padding:7px 2px;color:#bbc6c7}}table{{width:100%;border-collapse:collapse;font-size:13px}}td,th{{padding:10px;text-align:left;border-bottom:1px solid #46525a}}.small{{font-size:14px;color:#b5c2c4}}section{{margin-bottom:44px}}</style>
<main><p class="small">SURLHWA / ACT 1 PRODUCTION MILESTONE · GATE 2</p><h1>설원 속의 바,<br>이야기가 머무는 첫 번째 밤들.</h1>
<div class="notice"><strong>{status}</strong><br>아래 화면과 영상은 새 Unity Player의 실제 렌더입니다. 미감·동작·소리·감정에 대한 최종 판단은 이번 묶음 오너 리뷰에 남아 있습니다. 기존 네이티브 입력 검증 부채는 별도로 유지합니다.</div>
<p>C1 카메라와 공간·접지 방향을 유지하면서 목재, 선반, 조명 소품, 설원, 인물 포즈, 조주 도구와 기본 UI를 연결했습니다. 원본 체크포인트 <code>dcc0297</code>의 이미지 43종은 재생성하지 않았습니다.</p>
<p><a href="ACT1_ART_GATE2_REVIEW.md">리뷰 안내·완료 범위</a> · <a href="PRESENTATION_DEBT.md">남은 Presentation Debt</a> · <a href="Manifests/art-gate2-review.json">검증 기록</a> · <a href="Builds/Act1/SurlhwaAct1.exe">ACT 1 Player 실행</a></p>
<p class="small">기록 입력은 기존 production InputSystem에 전달한 시뮬레이션입니다. OS 네이티브 플레이 기록은 아닙니다. 영상은 실제 프레임 시각을 보존한 약 5 Hz 화면 기록과 Unity 믹스이며, 60 fps 애니메이션·입력 지연·오디오 접점 동기화의 최종 승인을 대신하지 않습니다. 전체 원본은 각 실행 폴더에 보존됩니다.</p>
{comparison}{''.join(sections)}
<section><h2>검증 범위와 보존된 시도</h2><p>현재 소스·Player 해시가 일치하고 정상 종료한 기록만 위 갤러리에 포함합니다. 초기 실패와 시각 수정 중단은 별도로 남깁니다.</p><table><tr><th>실행</th><th>상태</th><th>검사</th><th>화면</th></tr>{rows}</table></section>
<p><a href="Manifests/art-integration-checks.json">씬·참조·보존 검사</a> · <a href="Manifests/art-save-continuity.json">Continue 저장 연속성</a> · <a href="Manifests/art-render-inspection.json">실제 화면 검사·영상 정보</a> · <a href="Manifests/act1-art-production-inventory.json">연결된 아트 출처</a> · <a href="ACT1_ART_PASS_PLAN.md">실행·판단 원장</a></p>
<p class="small">Codex 전체 재설계, 전체 게스트/칵테일 아트, ACT 2–7 및 최종 음악 제작은 이번 패스의 범위 밖입니다. 원본 시트는 <a href="review-act1-art-source.html">별도 소스 체크포인트</a>에 있으며, 위 실제 게임 화면과 구분됩니다.</p></main></html>'''
(VALIDATION / 'review-act1-art.html').write_text(page, encoding='utf-8')
print(status, requirements)
