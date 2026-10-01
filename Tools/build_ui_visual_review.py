"""Review only: native crops and HTML from actual Unity before/after captures.

No runtime art is generated or edited. Run from the repository root.
"""
from pathlib import Path
import hashlib
import json
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[1] / "Logs/Review/UIVisualFinish"
STATES = ["counter-idle", "Stir-hover", "Shake-hover", "Float-selected",
          "Float-selected-Stir-hover", "idle-bar", "settings-open", "codex-open",
          "codex-content"]
RESOLUTIONS = [(1280, 720), (1920, 1080)]


def capture(stage, state, width, height):
    return ROOT / f"phase-m-visual-{stage}-{state}-{width}x{height}.png"


def main():
    crops = ROOT / "native-crops"
    crops.mkdir(exist_ok=True)
    html = ["<!doctype html><meta charset='utf-8'><title>SURLHWA visual finish</title>",
            "<style>body{background:#20272b;color:#f1e8d5;font:16px system-ui;margin:24px}"
            "a{color:#dac69c} .pair{display:flex;gap:20px;overflow:auto;padding:12px 0}"
            "figure{margin:0;flex:none}img{display:block;max-width:none}figcaption{padding:8px 0}"
            "details{margin:20px 0}summary{cursor:pointer}</style>",
            "<h1>SURLHWA — tools and utility controls</h1>",
            "<p>Actual production-scene fixtures rendered by Unity. Seeded Night 5; "
            "direct UI events/callbacks arrange the states. These are not pointer-delivery "
            "or human playtest evidence. Offscreen capture reprojects the counter for each resolution. "
            "Images and crops below retain native pixels; scroll horizontally if needed.</p>",
            "<p>BEFORE: starting eb8e198 visual source. AFTER: task visual source. "
            "Navigation focus is on Back for idle/hover views, avoiding an unintended focused Stir. "
            "Guest walking position can differ in utility views; room/camera and button framing match.</p>"]
    for width, height in RESOLUTIONS:
        scale = width / 1280
        boxes = {"objects": (330, 300, 1025, 427),
                 "book": (16, 12, 96, 105), "settings": (1184, 12, 1264, 105)}
        html.append(f"<h2>{width} × {height}</h2>")
        for state in STATES:
            html.append(f"<details><summary>{state} — full screens</summary><div class='pair'>")
            for stage in ("before", "after"):
                path = capture(stage, state, width, height)
                with Image.open(path) as im:
                    assert im.size == (width, height), path
                    for label, box in boxes.items():
                        region = tuple(round(value * scale) for value in box)
                        im.crop(region).save(crops / f"{stage}-{state}-{label}-{width}x{height}.png")
                html.append(f"<figure><figcaption>{stage.upper()}</figcaption><a href='{path.name}'>"
                            f"<img src='{path.name}' width='{width}' height='{height}'></a></figure>")
            html.append("</div></details>")
        for state, label in [("counter-idle", "objects"), ("Stir-hover", "objects"),
                             ("Shake-hover", "objects"), ("Float-selected-Stir-hover", "objects"),
                             ("idle-bar", "book"), ("idle-bar", "settings"),
                             ("settings-open", "settings"), ("codex-open", "book")]:
            html.append(f"<h3>{state} / {label} — native crop</h3><div class='pair'>")
            for stage in ("before", "after"):
                name = f"{stage}-{state}-{label}-{width}x{height}.png"
                html.append(f"<figure><figcaption>{stage.upper()}</figcaption>"
                            f"<img src='native-crops/{name}'></figure>")
            html.append("</div>")
        html.append("<h3>Utility states — native crops</h3><div class='pair'>")
        for control, label in [("SettingsButton", "settings"), ("CodexButton", "book")]:
            for state in ("hover", "pressed", "focus", "disabled"):
                for stage in ("before", "after"):
                    with Image.open(capture(stage, f"{control}-{state}", width, height)) as im:
                        region = tuple(round(value * scale) for value in boxes[label])
                        name = f"{stage}-{control}-{state}-{width}x{height}.png"
                        im.crop(region).save(crops / name)
                    html.append(f"<figure><figcaption>{stage} {control} {state}</figcaption>"
                                f"<img src='native-crops/{name}'></figure>")
        html.append("</div>")
    (ROOT / "before-after.html").write_text("\n".join(html), encoding="utf-8")

    # A small board consists entirely of 1:1 crops, not enlarged art or a mockup.
    board = Image.new("RGB", (1450, 804), "#20272b")
    draw = ImageDraw.Draw(board)
    draw.text((24, 16), "BEFORE (eb8e198)", fill="#f1e8d5")
    draw.text((749, 16), "AFTER / HUMAN VISUAL APPROVAL PENDING", fill="#f1e8d5")
    for row, state in enumerate(["counter-idle", "Stir-hover", "Shake-hover", "Float-selected-Stir-hover"]):
        y = 52 + row * 165
        for col, stage in enumerate(["before", "after"]):
            draw.text((24+col*725, y-16), state, fill="#c4c1b6")
            with Image.open(crops / f"{stage}-{state}-objects-1280x720.png") as im:
                board.paste(im, (24+col*725, y))
    for col, stage in enumerate(["before", "after"]):
        for j, label in enumerate(["book", "settings"]):
            with Image.open(crops / f"{stage}-idle-bar-{label}-1280x720.png") as im:
                board.paste(im, (24+col*725+j*112, 698))
    board.save(ROOT / "before-after-board.png")
    manifest = {p.relative_to(ROOT).as_posix(): hashlib.sha256(p.read_bytes()).hexdigest()
                for p in sorted(ROOT.glob("phase-m-visual-*.png"))}
    (ROOT / "capture-sha256.json").write_text(json.dumps(manifest, indent=2)+"\n", encoding="utf-8")
    print(f"Indexed {len(manifest)} full-resolution captures; native crops and board in {ROOT}")


if __name__ == "__main__":
    main()
