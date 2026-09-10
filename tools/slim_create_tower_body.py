"""Narrow only the tower body while leaving its green creation badge intact."""

from pathlib import Path

import numpy as np
from PIL import Image


ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "Assets/4. DotAsset/5. UI/Buttons/Btn_Symbol_CreateTower.png"
DESTINATION = ROOT / "Assets/4. DotAsset/5. UI/Buttons/Btn_Symbol_CreateTower_BodySlim.png"
WIDTH_FACTOR = 0.90
TOWER_BODY_AXIS_X = 508


def dilate(mask: np.ndarray, steps: int) -> np.ndarray:
    expanded = mask.copy()
    for _ in range(steps):
        expanded = (
            expanded
            | np.roll(expanded, 1, axis=0)
            | np.roll(expanded, -1, axis=0)
            | np.roll(expanded, 1, axis=1)
            | np.roll(expanded, -1, axis=1)
        )
    return expanded


def main() -> None:
    source = Image.open(SOURCE).convert("RGBA")
    pixels = np.asarray(source).copy()
    red, green, blue, alpha = [pixels[..., channel].astype(int) for channel in range(4)]

    # The bright-green component is unique to the create badge.  Expanding it
    # captures its dark outline, but avoids changing the tower's own shading.
    green_core = (alpha > 0) & (green - red > 35) & (green - blue > 10)
    badge_mask = dilate(green_core, 8) & (alpha > 0)

    body_pixels = pixels.copy()
    body_pixels[badge_mask] = (0, 0, 0, 0)
    body = Image.fromarray(body_pixels, "RGBA")
    left, top, right, bottom = body.getbbox()
    crop = body.crop((left, top, right, bottom))
    narrowed = crop.resize((round(crop.width * WIDTH_FACTOR), crop.height), Image.Resampling.NEAREST)

    # Center by the tower body's alpha-weighted visual mass, not by the badge.
    narrow_alpha = np.asarray(narrowed)[..., 3]
    weights = narrow_alpha.astype(np.float64)
    local_centroid_x = (weights.sum(axis=0) * np.arange(narrowed.width)).sum() / weights.sum()
    destination_left = round(TOWER_BODY_AXIS_X - local_centroid_x)

    result = Image.new("RGBA", source.size)
    result.alpha_composite(narrowed, (destination_left, top))
    badge = Image.fromarray(np.where(badge_mask[..., None], pixels, 0), "RGBA")
    result.alpha_composite(badge)
    result.save(DESTINATION)
    print(f"Created {DESTINATION}; body centroid aligned to x={TOWER_BODY_AXIS_X}")


if __name__ == "__main__":
    main()
