"""Align the tower body, not the tower-plus-arrow silhouette, to x=508."""

from pathlib import Path

from PIL import Image


ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "Assets/4. DotAsset/5. UI/Buttons/Btn_Symbol_TierUpgrade.png"
DESTINATION = ROOT / "Assets/4. DotAsset/5. UI/Buttons/Btn_Symbol_TierUpgrade_Corrected.png"

# The first body-centric pass over-corrected its visual weight.  Move the
# already-adjusted asset back left by two logical pixels (16 texture pixels).
BODY_AXIS_OFFSET_X = -16


def main() -> None:
    with Image.open(SOURCE) as source:
        source = source.convert("RGBA")
        result = Image.new("RGBA", source.size)
        result.alpha_composite(source, (BODY_AXIS_OFFSET_X, 0))
        result.save(DESTINATION)
    print(f"Created {DESTINATION}")


if __name__ == "__main__":
    main()
