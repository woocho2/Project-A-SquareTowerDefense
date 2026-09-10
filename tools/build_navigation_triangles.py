"""Create matching warm pixel-art previous/next triangle symbols."""

from collections import deque
from pathlib import Path

import numpy as np
from PIL import Image


ROOT = Path(__file__).resolve().parents[1]
CONCEPT = Path(
    r"C:\Users\user\.codex\generated_images\01a064d2-c8d9-7780-93e6-a51a25095313"
    r"\exec-328afb78-8fc1-4f2d-ab84-a47e22965497.png"
)
DESTINATION_DIRECTORY = ROOT / "Assets/4. DotAsset/5. UI/Buttons"
NEXT = DESTINATION_DIRECTORY / "Btn_Symbol_Next.png"
PREVIOUS = DESTINATION_DIRECTORY / "Btn_Symbol_Previous.png"
CELL = 16
PALETTE = np.array(
    (
        (74, 42, 25, 255),
        (132, 75, 28, 255),
        (235, 183, 79, 255),
        (255, 232, 166, 255),
        (255, 249, 224, 255),
    ),
    dtype=np.uint8,
)


def keep_center_component(mask: np.ndarray) -> np.ndarray:
    height, width = mask.shape
    kept = np.zeros_like(mask)
    center = (height // 2, width // 2)
    kept[center] = True
    waiting = deque([center])
    while waiting:
        y, x = waiting.popleft()
        for y_offset in (-1, 0, 1):
            for x_offset in (-1, 0, 1):
                if y_offset == 0 and x_offset == 0:
                    continue
                next_y, next_x = y + y_offset, x + x_offset
                if (
                    0 <= next_y < height
                    and 0 <= next_x < width
                    and mask[next_y, next_x]
                    and not kept[next_y, next_x]
                ):
                    kept[next_y, next_x] = True
                    waiting.append((next_y, next_x))
    return kept


def clean_pixel_art(source: np.ndarray) -> np.ndarray:
    source[~keep_center_component(source[..., 3] > 0)] = (0, 0, 0, 0)
    result = np.zeros_like(source)
    for y in range(0, 1024, CELL):
        for x in range(0, 1024, CELL):
            cell = source[y:y + CELL, x:x + CELL]
            filled = cell[..., 3] > 0
            if filled.mean() < 0.3:
                continue
            colors = cell[..., :3][filled].astype(np.float32)
            brightness = (colors[:, 0] * 0.2126 + colors[:, 1] * 0.7152 + colors[:, 2] * 0.0722).mean()
            palette_index = np.digitize(brightness, (70, 125, 185, 232))
            result[y:y + CELL, x:x + CELL] = PALETTE[palette_index]
    return result


def main() -> None:
    # Crop rather than resize so the generated pixel steps remain square.
    source = np.asarray(Image.open(CONCEPT).convert("RGBA").crop((115, 115, 1139, 1139))).copy()
    next_symbol = clean_pixel_art(source)
    Image.fromarray(next_symbol, "RGBA").save(NEXT)
    Image.fromarray(next_symbol[:, ::-1], "RGBA").save(PREVIOUS)
    print(f"Created {NEXT} and {PREVIOUS}")


if __name__ == "__main__":
    main()
