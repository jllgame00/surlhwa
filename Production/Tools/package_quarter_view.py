"""Package actual C1/Q captures. Never synthesize missing renders or relabel previews."""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont
import argparse
import hashlib
import html
import json
import subprocess

VALIDATION = Path(__file__).resolve().parents[1] / "Validation"


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("ordinary")
    parser.add_argument("prince")
    args = parser.parse_args()
    runs = {"Ordinary": VALIDATION / "Evidence" / args.ordinary,
            "Prince": VALIDATION / "Evidence" / args.prince}
    rows, clips, measurements = [], [], []
    for actor, folder in runs.items():
        result = json.loads((folder / "result.json").read_text(encoding="utf-8-sig"))
        process = json.loads((folder / "process.json").read_text(encoding="utf-8-sig"))
        assert process["exitCode"] == 0 and not process["timedOut"]
        assert not result["errors"] and result["actor"] == actor
        assert len(result["measurements"]) == 6, "Final measurement run required"
        assert result["motion"], "Actual ACT 1 motion samples required"
        for shot in result["shots"]:
            path = folder / shot["file"]
            with Image.open(path) as im:
                assert im.size == (shot["width"], shot["height"])
            rows.append(dict(actor=actor, **shot, path=path.relative_to(VALIDATION).as_posix(), sha256=sha(path)))
        measurements.extend(dict(actor=actor, **m) for m in result["measurements"])
        for candidate in ("C1", "Q1", "Q2"):
            for width in (1280, 1920):
                frames = folder / f"{candidate}_{actor}_motion_{width}"
                images = sorted(frames.glob("*.png"))
                assert len(images) == len(result["motion"])
                lines = ["ffconcat version 1.0"]
                for index, path in enumerate(images):
                    with Image.open(path) as im:
                        assert im.size == (width, width * 9 // 16)
                    pose = result["motion"][index]
                    following = result["motion"][index + 1] if index + 1 < len(images) else None
                    duration = following["time"] - pose["time"] if following and following["phase"] == pose["phase"] else 1 / 12
                    lines.extend([f"file '{path.resolve().as_posix()}'", f"duration {max(.01, duration):.6f}"])
                lines.append(f"file '{images[-1].resolve().as_posix()}'")
                concat = frames.with_suffix(".ffconcat")
                concat.write_text("\n".join(lines) + "\n", encoding="utf-8")
                movie = frames.with_suffix(".mp4")
                subprocess.run(["ffmpeg", "-hide_banner", "-loglevel", "error", "-y", "-safe", "0", "-i", str(concat), "-vsync", "vfr", "-c:v", "libx264", "-crf", "18", "-pix_fmt", "yuv420p", "-movflags", "+faststart", str(movie)], check=True)
                info = json.loads(subprocess.check_output(["ffprobe", "-v", "error", "-select_streams", "v:0", "-show_entries", "stream=width,height,nb_frames,duration", "-of", "json", str(movie)]))
                assert info["streams"][0]["width"] == width
                clips.append(dict(actor=actor, candidate=candidate, width=width, path=movie.relative_to(VALIDATION).as_posix(), sha256=sha(movie), stream=info["streams"][0]))
    # Derived overview only. Every original remains untouched and linked.
    board = Image.new("RGB", (1584, 1050), "#152128")
    draw = ImageDraw.Draw(board)
    font = ImageFont.truetype("C:/Windows/Fonts/segoeui.ttf", 23)
    small = ImageFont.truetype("C:/Windows/Fonts/segoeui.ttf", 17)
    draw.text((24, 16), "SURLHWA / C1 + Quarter View / ACT 1", font=font, fill="#ebddc1")
    draw.text((24, 50), "Actual Unity renders. Q remains a reversible spike, pending owner comparison.", font=small, fill="#bdc9ce")
    for col, candidate in enumerate(("C1", "Q1", "Q2")):
        x = 24 + col * 520
        draw.text((x, 90), {"C1": "C1 / accepted composition", "Q1": "Q1 / soft orthographic", "Q2": "Q2 / restrained perspective"}[candidate], font=small, fill="#ebc686")
        for row, (actor, fixture) in enumerate((("Prince", "Composition"), ("Ordinary", "Wide"), ("Prince", "Grounding"))):
            item = next(r for r in rows if r["actor"] == actor and r["candidate"] == candidate and r["fixture"] == fixture and r["width"] == 1920)
            with Image.open(VALIDATION / item["path"]) as im:
                board.paste(im.resize((504, 284), Image.Resampling.NEAREST), (x, 122 + row * 306))
            draw.text((x, 408 + row * 306), actor + " / " + fixture, font=small, fill="#b9c6ca")
    board.save(VALIDATION / "QUARTER_VIEW_COMPARISON.png")
    style = "body{margin:0;background:#152128;color:#e8ddc8;font:16px/1.6 system-ui}main{max-width:1520px;margin:auto;padding:32px}a{color:#dfbd83}h1,h2{font-weight:500}.grid{display:grid;grid-template-columns:repeat(3,1fr);gap:14px}figure{margin:0}img,video{width:100%}figcaption{font-size:13px;color:#b8c4c8}table{border-collapse:collapse;width:100%}td,th{text-align:left;padding:8px;border-bottom:1px solid #456}"
    body = ['<!doctype html><html lang="en"><meta charset="utf-8"><title>SURLHWA / Quarter View</title><style>' + style + '</style><main>', '<h1>SURLHWA / Quarter View comparison</h1><p>QUARTER_VIEW_READY_FOR_OWNER_COMPARISON · C1 remains the Production baseline.</p>', '<p><a href="QUARTER_VIEW_REVIEW.md">Interpretation, validation and remaining costs</a></p>', '<a href="QUARTER_VIEW_COMPARISON.png"><img src="QUARTER_VIEW_COMPARISON.png" alt="C1, Q1 and Q2 comparison board"></a>', '<p>World plates hide reading UI and the resident workbench equally in all candidates. ContextUI frames retain them. Original 720p/1080p frames open directly; this page does not grant visual approval.</p>']
    for actor in ("Ordinary", "Prince"):
        for fixture in ("Empty", "Seated", "Composition", "Wide", "Grounding", "ExistingSprites", "ContextUI"):
            matches = [r for r in rows if r["actor"] == actor and r["fixture"] == fixture]
            if not matches:
                continue
            body.append(f'<h2>{actor} / {fixture}</h2><div class="grid">')
            for candidate in ("C1", "Q1", "Q2"):
                r = next(r for r in matches if r["candidate"] == candidate and r["width"] == 1280)
                large = next(r for r in matches if r["candidate"] == candidate and r["width"] == 1920)
                body.append(f'<figure><a href="{r["path"]}"><img loading="lazy" src="{r["path"]}" alt="{candidate} {actor} {fixture}"></a><figcaption>{candidate} · <a href="{r["path"]}">1280×720</a> · <a href="{large["path"]}">1920×1080</a></figcaption></figure>')
            body.append('</div>')
    body.append('<h2>Recorded ACT 1 movement</h2><p>Presentation replay of actual controller poses and their sample times. Ordinary: arrival, rear-pose seated/reaction study, departure. Prince: original-pose seated/reaction/departure control from the retained earned checkpoint; rear-pose study is in the stills. Walking uses accepted frames. Q Prince ContextUI is a forced camera diagnostic of the existing workbench and original pose; normal active bartending returns to C1. Technical service commands reach departure; no native-input, audio or 60 fps motion claim.</p><div class="grid">')
    for clip in clips:
        if clip["width"] == 1280:
            large = next(c for c in clips if c["actor"] == clip["actor"] and c["candidate"] == clip["candidate"] and c["width"] == 1920)
            body.append(f'<figure><video controls preload="none" src="{clip["path"]}"></video><figcaption>{clip["candidate"]} / {clip["actor"]} · <a href="{large["path"]}">1080p original</a></figcaption></figure>')
    body.append('</div><h2>Bounded performance</h2><p>10 seconds per candidate/resolution after 60 warmup frames. Uncapped Development Player with explicit URP offscreen requests; world plates, UI/workbench hidden. Harness cost included. Not a minimum-PC or GPU-time certification.</p><table><tr><th>Actor / view / width</th><th>P50 / P95 ms</th><th>Renderers / materials</th><th>Draws / batches</th><th>GC mean bytes</th><th>Allocated MiB</th></tr>')
    for m in measurements:
        body.append(f'<tr><td>{m["actor"]} / {m["candidate"]} / {m["width"]}</td><td>{m["p50"]:.2f} / {m["p95"]:.2f}</td><td>{m["renderers"]} / {m["materials"]}</td><td>{m["draws"]:.1f} / {m["batches"]:.1f}</td><td>{m["gcMean"]:.0f}</td><td>{m["allocated"]/1048576:.1f}</td></tr>')
    body.append('</table><p><a href="Manifests/quarter-view-package.json">Native frame hashes, cameras, screen bounds, motion and performance records</a></p></main></html>')
    (VALIDATION / "review-quarter-view.html").write_text("\n".join(body), encoding="utf-8")
    (VALIDATION / "Manifests/quarter-view-package.json").write_text(json.dumps(dict(status="QUARTER_VIEW_READY_FOR_OWNER_COMPARISON", shots=rows, clips=clips, measurements=measurements), indent=2) + "\n")
    print(f"PACKAGED {len(rows)} native frames / {len(clips)} movement clips / {len(measurements)} measurements")


if __name__ == "__main__":
    main()
