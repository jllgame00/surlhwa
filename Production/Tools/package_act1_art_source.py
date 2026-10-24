"""A candid source-review page; never substitutes a mockup for Player evidence."""
from pathlib import Path
import json
import html

ROOT=Path(__file__).resolve().parents[2]
source=ROOT/'Production/SourceArt/Act1Production'
manifest=json.loads((source/'assets.json').read_text(encoding='utf-8'))
tiles=[]
for asset in manifest['assets']:
    relative='../Unity/Assets/Surlhwa/Act1Assets/ProductionArt/'+Path(asset['runtime']).name
    category='characters' if asset['name'].startswith(('Bartender','Prince','Ordinary')) else 'tools' if asset['name'].startswith('Tool') else 'world'
    tiles.append(f'<figure data-category="{category}"><img src="{relative}" alt="{html.escape(asset["name"])}"><figcaption>{html.escape(asset["name"])}<small>{asset["dimensions"][0]} × {asset["dimensions"][1]} px</small></figcaption></figure>')
page='''<!doctype html><html lang="ko"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<title>SURLHWA · ACT 1 art source checkpoint</title>
<style>
*{box-sizing:border-box}body{margin:0;background:#172127;color:#e5d6ba;font:16px/1.7 system-ui,sans-serif}main{max-width:1200px;margin:auto;padding:56px 28px}h1{font-size:38px;line-height:1.2;font-weight:550;margin:10px 0 24px}h2{margin-top:50px;font-size:23px;font-weight:500}p{max-width:850px;color:#b9c2c2}a{color:#d1ac72}small{display:block;color:#929f9f}.status{border:1px solid #927342;background:#2c2922;padding:20px 24px;color:#e8cf9f}.eyebrow{font-size:12px;letter-spacing:.18em;color:#c1a375}nav{display:flex;gap:10px;margin:28px 0}button{background:#26343b;border:1px solid #627079;color:#e5d6ba;padding:8px 20px;cursor:pointer}button[aria-pressed=true]{border-color:#cfad72;background:#43392c}.gallery{display:grid;grid-template-columns:repeat(auto-fill,minmax(165px,1fr));gap:12px}figure{margin:0;background:#202a30;border:1px solid #344047;padding:16px;min-height:190px;display:flex;flex-direction:column;justify-content:space-between;align-items:center}figure img{image-rendering:pixelated;max-width:150px;height:114px;object-fit:contain}figcaption{text-align:center;font-size:13px;margin-top:15px}table{border-collapse:collapse;width:100%;max-width:850px}td{border-bottom:1px solid #3b464b;padding:12px 5px}td:first-child{width:40%;color:#b9c2c2}.audio{display:flex;flex-wrap:wrap;gap:28px}.audio p{margin:8px 0}audio{max-width:100%}.sources{display:flex;flex-wrap:wrap;gap:18px}footer{margin-top:48px;border-top:1px solid #3b464b;padding-top:20px;color:#a6afb1;font-size:14px}
</style><main>
<div class="eyebrow">SURLHWA / ACT 1 PRODUCTION ART PASS</div><h1>설원 속의 작은 불빛,<br>그 안에 머무는 사람들.</h1>
<div class="status"><strong>소스 체크포인트 · Unity 적용 대기</strong><br>새 에셋과 연결 코드는 준비되었습니다. Editor 라이선스 차단으로 씬 업그레이드·새 Player 빌드·실제 화면 검증은 아직 실행하지 않았습니다. 이 페이지는 게임 캡처 또는 Gate 2 완료 증거가 아닙니다.</div>
<p>C1 카메라와 열린 바의 치수를 유지합니다. ACT 1에 필요한 목재·선반·랜턴·설원과 바텐더·어린왕자·익명 손님 두 종류를 한 세트로 정리했습니다. 기존 조주 상태 위에 픽셀 도구를 연결하고, 기본 읽기·준비 UI에 절제된 테두리와 재질을 적용하는 코드가 포함됩니다.</p>
<h2>제작 에셋</h2><p>33개 신규 환경·재질·캐릭터 이미지와 기존 픽셀 도구 10개. 아래는 개별 소스 미리보기이며 실제 장면의 크기나 가림을 증명하지 않습니다.</p>
<nav aria-label="Asset filter"><button aria-pressed="true" data-filter="all">전체</button><button aria-pressed="false" data-filter="world">공간</button><button aria-pressed="false" data-filter="characters">인물</button><button aria-pressed="false" data-filter="tools">조주</button></nav>
<div class="gallery">'''+''.join(tiles)+'''</div>
<h2>발걸음 후보</h2><p>기존 이벤트 타이밍과 믹스 정책을 유지하는 원본 합성 음원입니다. RMS 비교만 맞췄으며 체감 무게·최종 음량·장면 내 적합성은 아직 승인되지 않았습니다.</p>
<div class="audio"><div><p>이전 ACT 1 후보</p><audio controls preload="none" src="../Unity/Assets/Surlhwa/Act1Assets/FootGrounded.wav"></audio></div><div><p>새 felt / wood 후보</p><audio controls preload="none" src="../Unity/Assets/Surlhwa/Act1Assets/ProductionArt/FootstepFeltWood.wav"></audio></div></div>
<h2>증거와 남은 작업</h2><table><tr><td>독립 도메인 검사</td><td>149 / 149 PASS</td></tr><tr><td>C# 정적 컴파일</td><td>오류 0 · 기존 경고 1</td></tr><tr><td>소스·임포터 정적 검사</td><td>해시 / RGBA / 픽셀 설정 / 기준점 / GUID</td></tr><tr><td>Unity 씬 적용·회귀 검사</td><td>VALIDATION_BLOCKED_BY_ENVIRONMENT</td></tr><tr><td>새 Player 720p / 1080p</td><td>미실행</td></tr><tr><td>네이티브 입력</td><td>기존 환경 차단 유지</td></tr><tr><td>최종 미감·소리·감정</td><td>묶음 Gate 2 검토에서 소유자 판단</td></tr></table>
<p><a href="ACT1_ART_PASS_PLAN.md">작업 원장</a> · <a href="ACT1_ART_SOURCE_REVIEW.md">독립 코드 리뷰</a> · <a href="Manifests/art-source-checks.json">정적 검사 결과</a> · <a href="../SourceArt/Act1Production/README.md">재현 방법 / 출처</a></p>
<div class="sources"><a href="../SourceArt/Act1Production/Prepared-assets.png">전체 픽셀 시트</a><a href="../SourceArt/Act1Production/assets.json">원본·추출·해시 목록</a></div>
<footer>Codex 전체 재설계, ACT 2–7, 새로운 칵테일과 진행 시스템은 이번 범위에 포함하지 않습니다. 기존 C1/Slice 자산과 사용자 수정은 보존합니다.</footer>
</main><script>document.querySelectorAll('[data-filter]').forEach(button=>button.addEventListener('click',()=>{document.querySelectorAll('[data-filter]').forEach(b=>b.setAttribute('aria-pressed',b===button));document.querySelectorAll('[data-category]').forEach(f=>f.style.display=button.dataset.filter==='all'||f.dataset.category===button.dataset.filter?'':'none')}));</script></html>'''
(ROOT/'Production/Validation/review-act1-art-source.html').write_text(page,encoding='utf-8')
print('Source review page written; pending Unity status explicit.')
