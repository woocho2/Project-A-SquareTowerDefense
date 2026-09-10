"""Create a narrower, non-destructive Tower Create UI symbol variant."""

from pathlib import Path

from PIL import Image


ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "Assets/4. DotAsset/5. UI/Buttons/Btn_Symbol_CreateTower.png"
DESTINATION = ROOT / "Assets/4. DotAsset/5. UI/Buttons/Btn_Symbol_CreateTower_Slim.png"
WIDTH_FACTOR = 0.90
TOWER_BODY_AXIS_X = 508


def main() -> None:
    with Image.open(SOURCE) as source:
        source = source.convert("RGBA")
        left, top, right, bottom = source.getbbox()
        icon = source.crop((left, top, right, bottom))
        width = round(icon.width * WIDTH_FACTOR)
        narrowed = icon.resize((width, icon.height), Image.Resampling.NEAREST)

        canvas = Image.new("RGBA", source.size)
        destination_left = TOWER_BODY_AXIS_X - width // 2
        canvas.alpha_composite(narrowed, (destination_left, top))
        canvas.save(DESTINATION)

    print(f"Created {DESTINATION}")


if __name__ == "__main__":
    main()
