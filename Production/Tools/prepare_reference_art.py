"""Select unchanged PNGs from this worktree's committed RC1 reference, never its dirty sibling."""
import hashlib
import json
from pathlib import Path

PRODUCTION = Path(__file__).resolve().parents[1]
REPOSITORY = PRODUCTION.parent
SOURCES = {
    "Bartender.png": "Assets/_SURLHWA/Art/Characters/Chibi/Bartender.png",
    "OrdinaryStanding.png": "Assets/_SURLHWA/Art/Characters/Chibi/OrdinaryCoat.png",
    "QuixoteStanding.png": "Assets/_SURLHWA/Art/Characters/Chibi/DonQuixote.png",
    "OrdinarySeated.png": "Assets/_SURLHWA/Art/Characters/Motion/OrdinaryCoat-Seated.png",
    "QuixoteSeated.png": "Assets/_SURLHWA/Art/Characters/Motion/DonQuixote-Seated.png",
}
SOURCES.update({f"OrdinaryWalk{i}.png": f"Assets/_SURLHWA/Art/Characters/Motion/OrdinaryCoat-Walk{i}.png" for i in range(4)})

def main():
    destination = PRODUCTION / "Unity/Assets/Surlhwa/Art"
    destination.mkdir(parents=True, exist_ok=True)
    rows = []
    for name, source in SOURCES.items():
        data = (REPOSITORY / source).read_bytes()
        (destination / name).write_bytes(data)
        rows.append({"source": source, "sourceRevision": "93939bb6179e51504a1bf3b036abcbe2cbb895b3",
                     "sha256": hashlib.sha256(data).hexdigest(), "bytes": len(data),
                     "destination": f"Production/Unity/Assets/Surlhwa/Art/{name}",
                     "classification": "MIGRATE_AS_REFERENCE_ASSET", "transform": "Unchanged PNG bytes; new Unity metadata",
                     "approval": "HG02/HG03 OWNER_REVIEW_PENDING; FIRST_PASS / HUMAN_ART_TUNING_LATER",
                     "provenance": "RC1 Phase L internal imagegen reference; seated Phase N deterministic pose derivation",
                     "license": "Internal owner review reference; final release adoption/rights review pending"})
    ledger = PRODUCTION / "SourceArt/PROVENANCE.json"
    ledger.parent.mkdir(parents=True, exist_ok=True)
    ledger.write_text(json.dumps({"schemaVersion": 1, "assets": rows,
        "sourceNotes": ["ArtSource/Pixel/BarRestart/README.md", "ArtSource/Pixel/CharacterMotion/README.md", "Docs/CREDITS.md"],
        "modelSource": "Production/Unity/Assets/Surlhwa/Editor/SpatialProofBuilder.cs; deterministic primitives/meshes, newly authored"},
        ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(f"Copied {len(rows)} unchanged reference PNGs with provenance.")

if __name__ == "__main__":
    main()
