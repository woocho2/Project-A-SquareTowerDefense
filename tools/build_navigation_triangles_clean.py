"""Build exact, centered pixel-art previous and next triangle UI symbols."""

from pathlib import Path

import numpy as np
from PIL import Image


ROOT = Path(__file__).resolve().parents[1]
DIRECTORY = ROOT / "Assets/4. DotAsset/5. UI/Buttons"
NEXT = DIRECTORY / "Btn_Symbol_Next.png"
PREVIOUS = DIRECTORY / "Btn_Symbol_Previous.png"
CELL = 4
# Grayscale source colors intentionally preserve shading after Unity's Image
# tint multiplies them into any chosen button color.
FILL = np.array((226, 226, 226, 255), dtype=np.uint8)
HIGHLIGHT = np.array((255, 255, 255, 255), dtype=np.uint8)
SHADOW = np.array((194, 194, 194, 255), dtype=np.uint8)


def paint_cell(canvas: np.ndarray, grid_x: int, grid_y: int, color: np.ndarray) -> None:
    x, y = grid_x * CELL, grid_y * CELL
    canvas[y:y + CELL, x:x + CELL] = color


def make_next() -> np.ndarray:
    canvas = np.zeros((1024, 1024, 4), dtype=np.uint8)
    # Four-pixel logical cells remain faintly pixel-like at UI scale but make
    # the diagonal read as smooth instead of a large sawtooth.
    # Deliberately touch all four canvas limits: the symbol's bounding box is
    # the full 1024x1024 sprite with no transparent margin around the shape.
    left, top, middle, bottom, tip = 0, 0, 127.5, 255, 255

    # One outline-free silhouette, vertically symmetric around the canvas.
    for grid_y in range(top, bottom + 1):
        distance = abs(grid_y - middle)
        progress = (middle - distance - top) / (middle - top)
        max_x = min(tip, left + round(progress * (tip - left + 1)))
        width = max_x - left + 1
        for grid_x in range(left, max_x + 1):
            # A broad, internal three-tone surface: no colored border is left
            # around the silhouette, so Unity tint stays clean.
            position = (grid_x - left) / max(1, width - 1)
            if width >= 18 and position < 0.34:
                color = HIGHLIGHT
            elif width >= 18 and position > 0.88:
                color = SHADOW
            else:
                color = FILL
            paint_cell(canvas, grid_x, grid_y, color)

    return canvas


def main() -> None:
    next_symbol = make_next()
    Image.fromarray(next_symbol, "RGBA").save(NEXT)
    Image.fromarray(next_symbol[:, ::-1], "RGBA").save(PREVIOUS)
    print(f"Created {NEXT} and {PREVIOUS}")


if __name__ == "__main__":
    main()
