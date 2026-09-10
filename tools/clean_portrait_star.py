"""Keep the central star component and discard transparent-canvas speckles."""

from collections import deque
from pathlib import Path

import numpy as np
from PIL import Image


ROOT = Path(__file__).resolve().parents[1]
TARGET = ROOT / "Assets/4. DotAsset/5. UI/TowerInfo_PortraitStar_V1.png"
PIXEL_CELL = 16
WARM_PALETTE = np.array(
    (
        (157, 100, 17, 255),
        (203, 139, 20, 255),
        (244, 181, 39, 255),
        (255, 216, 91, 255),
        (255, 248, 211, 255),
    ),
    dtype=np.uint8,
)


def keep_central_component(mask: np.ndarray) -> np.ndarray:
    height, width = mask.shape
    center_y, center_x = height // 2, width // 2
    if not mask[center_y, center_x]:
        coordinates = np.argwhere(mask)
        center_y, center_x = min(
            coordinates,
            key=lambda point: (int(point[0]) - height // 2) ** 2 + (int(point[1]) - width // 2) ** 2,
        )

    kept = np.zeros_like(mask)
    kept[center_y, center_x] = True
    pending = deque([(int(center_y), int(center_x))])

    while pending:
        y, x = pending.popleft()
        for offset_y in (-1, 0, 1):
            for offset_x in (-1, 0, 1):
                if offset_y == 0 and offset_x == 0:
                    continue
                next_y, next_x = y + offset_y, x + offset_x
                if (
                    0 <= next_y < height
                    and 0 <= next_x < width
                    and mask[next_y, next_x]
                    and not kept[next_y, next_x]
                ):
                    kept[next_y, next_x] = True
                    pending.append((next_y, next_x))
    return kept


def main() -> None:
    pixels = np.asarray(Image.open(TARGET).convert("RGBA")).copy()
    kept = keep_central_component(pixels[..., 3] > 0)
    pixels[~kept] = (0, 0, 0, 0)

    # Consolidate the generated texture into clean 16 px logical pixels.  This
    # removes AI-style flecks while retaining a deliberately mild pixel-art
    # silhouette and a warm-only palette.
    result = np.zeros_like(pixels)
    for y in range(0, pixels.shape[0], PIXEL_CELL):
        for x in range(0, pixels.shape[1], PIXEL_CELL):
            cell = pixels[y:y + PIXEL_CELL, x:x + PIXEL_CELL]
            filled = cell[..., 3] > 0
            if filled.mean() < 0.3:
                continue

            rgb = cell[..., :3][filled].astype(np.float32)
            brightness = (rgb[:, 0] * 0.2126 + rgb[:, 1] * 0.7152 + rgb[:, 2] * 0.0722).mean()
            palette_index = np.digitize(brightness, (82, 137, 191, 230))
            result[y:y + PIXEL_CELL, x:x + PIXEL_CELL] = WARM_PALETTE[palette_index]

    Image.fromarray(result, "RGBA").save(TARGET)
    print(f"Removed detached speckles and pixel-cleaned {TARGET}")


if __name__ == "__main__":
    main()
