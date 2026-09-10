"""Build a Tower Sell UI symbol from the existing Create Tower pixel art."""

from pathlib import Path

import numpy as np
from PIL import Image


ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "Assets/4. DotAsset/5. UI/Buttons/Btn_Symbol_CreateTower.png"
DESTINATION = ROOT / "Assets/4. DotAsset/5. UI/Buttons/Btn_Symbol_SellTower.png"
CELL = 8
BADGE_LEFT = 552
BADGE_TOP = 544

OUTLINE = np.array((66, 17, 70, 255), dtype=np.uint8)
SHADOW = np.array((182, 108, 16, 255), dtype=np.uint8)
GOLD = np.array((255, 205, 54, 255), dtype=np.uint8)
HIGHLIGHT = np.array((255, 244, 164, 255), dtype=np.uint8)


def dilate(mask: np.ndarray, steps: int = 1) -> np.ndarray:
    expanded = mask.copy()
    for _ in range(steps):
        expanded = (
            expanded
            | np.roll(expanded, 1, axis=0)
            | np.roll(expanded, -1, axis=0)
            | np.roll(expanded, 1, axis=1)
            | np.roll(expanded, -1, axis=1)
            | np.roll(np.roll(expanded, 1, axis=0), 1, axis=1)
            | np.roll(np.roll(expanded, 1, axis=0), -1, axis=1)
            | np.roll(np.roll(expanded, -1, axis=0), 1, axis=1)
            | np.roll(np.roll(expanded, -1, axis=0), -1, axis=1)
        )
    return expanded


def remove_create_badge(pixels: np.ndarray) -> np.ndarray:
    """Remove the green plus, including its full dark outer shadow."""
    red, green, blue, alpha = [pixels[..., channel].astype(int) for channel in range(4)]
    green_core = (alpha > 0) & (green - red > 35) & (green - blue > 10)
    # The source plus has a two-logical-pixel dark outline/shadow beyond its
    # green fill.  Removing only one logical pixel left those dark fragments
    # visible around the sell badge on a light UI background.
    badge = dilate(green_core, 16) & (alpha > 0)
    tower = pixels.copy()
    tower[badge] = (0, 0, 0, 0)

    # The plus overlaps the lower-right wall of the otherwise symmetric tower.
    # Rebuild only the erased right half from its untouched left counterpart;
    # this keeps the tower silhouette whole without retaining any plus pixels.
    mirrored = pixels[:, ::-1]
    right_half = np.arange(pixels.shape[1])[None, :] > (pixels.shape[1] - 1) // 2
    restore = badge & right_half & (mirrored[..., 3] > 0)
    tower[restore] = mirrored[restore]
    return tower


def dollar_pattern() -> tuple[np.ndarray, np.ndarray]:
    # 13x14 logical pixels: a clear $ mark that fits the original plus badge.
    rows = (
        ".....X.......",
        ".....X.......",
        "..XXXXXXX....",
        ".XX.....XX...",
        ".XX..........",
        "..XXXXXX.....",
        ".......XX....",
        "........XX...",
        ".XX.....XX...",
        ".XX.....XX...",
        "..XXXXXXX....",
        ".....X.......",
        ".....X.......",
        ".............",
    )
    fill = np.array([[cell == "X" for cell in row] for row in rows], dtype=bool)
    # Keep the central stroke continuous so it reads as $ rather than an S at
    # small UI sizes.
    fill[:13, 5] = True
    highlight = np.zeros_like(fill)
    highlight[2, 2:6] = True
    highlight[3, 1:3] = True
    highlight[5, 2:5] = True
    return fill, highlight


def paint_cells(canvas: np.ndarray, mask: np.ndarray, color: np.ndarray, left: int, top: int) -> None:
    for y, x in zip(*np.where(mask)):
        y0, x0 = top + y * CELL, left + x * CELL
        canvas[y0:y0 + CELL, x0:x0 + CELL] = color


def main() -> None:
    pixels = np.asarray(Image.open(SOURCE).convert("RGBA")).copy()
    result = remove_create_badge(pixels)
    fill, highlight = dollar_pattern()
    outline = dilate(fill)

    # A one-cell offset purple/bronze shadow gives the badge the same compact,
    # raised silhouette as the other action markers.
    paint_cells(result, outline, SHADOW, BADGE_LEFT + CELL // 2, BADGE_TOP + CELL // 2)
    paint_cells(result, outline, OUTLINE, BADGE_LEFT, BADGE_TOP)
    paint_cells(result, fill, GOLD, BADGE_LEFT, BADGE_TOP)
    paint_cells(result, highlight & fill, HIGHLIGHT, BADGE_LEFT, BADGE_TOP)

    Image.fromarray(result, "RGBA").save(DESTINATION)
    print(f"Created {DESTINATION}")


if __name__ == "__main__":
    main()
