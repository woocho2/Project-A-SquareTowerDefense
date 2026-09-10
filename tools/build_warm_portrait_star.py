"""Build a clean, warm pixel-art replacement star from the approved concept."""

from collections import deque
from pathlib import Path

import numpy as np
from PIL import Image


ROOT = Path(__file__).resolve().parents[1]
CONCEPT = Path(
    r"C:\Users\user\.codex\generated_images\01a064d2-c8d9-7780-93e6-a51a25095313"
    r"\exec-22130e1e-5a51-462d-827d-88daa43eefcd.png"
)
DESTINATION = ROOT / "Assets/4. DotAsset/5. UI/TowerInfo_PortraitStar_Warm_V2.png"
PIXEL_CELL = 16
PALETTE = np.array(
    (
        (157, 100, 17, 255),
        (203, 139, 20, 255),
        (244, 181, 39, 255),
        (255, 216, 91, 255),
        (255, 248, 211, 255),
    ),
    dtype=np.uint8,
)


def central_component(mask: np.ndarray) -> np.ndarray:
    height, width = mask.shape
    center = (height // 2, width // 2)
    kept = np.zeros_like(mask)
    kept[center] = True
    waiting = deque([center])
    while waiting:
        y, x = waiting.popleft()
        for y_offset in (-1, 0, 1):
            for x_offset in (-1, 0, 1):
                if y_offset == x_offset == 0:
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


def main() -> None:
    # The central crop turns the generated 1254 canvas into exactly 1024 px
    # without resampling any pixel steps.
    source = np.asarray(Image.open(CONCEPT).convert("RGBA").crop((115, 115, 1139, 1139))).copy()
    source[~central_component(source[..., 3] > 0)] = (0, 0, 0, 0)

    result = np.zeros_like(source)
    for y in range(0, 1024, PIXEL_CELL):
        for x in range(0, 1024, PIXEL_CELL):
            cell = source[y:y + PIXEL_CELL, x:x + PIXEL_CELL]
            filled = cell[..., 3] > 0
            if filled.mean() < 0.3:
                continue
            rgb = cell[..., :3][filled].astype(np.float32)
            brightness = (rgb[:, 0] * 0.2126 + rgb[:, 1] * 0.7152 + rgb[:, 2] * 0.0722).mean()
            palette_index = np.digitize(brightness, (82, 137, 191, 230))
            result[y:y + PIXEL_CELL, x:x + PIXEL_CELL] = PALETTE[palette_index]

    Image.fromarray(result, "RGBA").save(DESTINATION)
    print(f"Created {DESTINATION}")


if __name__ == "__main__":
    main()
