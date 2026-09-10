"""Create a smooth, transparent 1024px variant of the modular button frame."""

from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw


ROOT = Path(__file__).resolve().parents[1]
DESTINATION = ROOT / "Assets/4. DotAsset/5. UI/Buttons/Btn_Frame_Smooth.png"
OUTPUT_SIZE = 1024
SUPERSAMPLE = 4

# Preserved source palette, from outer shadow through the inner bevel.
LAYERS = (
    (48, 976, 72, (148, 148, 148, 255)),
    (64, 960, 64, (210, 210, 210, 255)),
    (80, 944, 56, (255, 255, 255, 255)),
    (96, 928, 48, (223, 223, 223, 255)),
    (112, 912, 40, (172, 172, 172, 255)),
)
INNER_OPENING = (120, 904, 32)


def resize_premultiplied(image: Image.Image, size: int) -> Image.Image:
    """Resample RGBA without dark/opaque fringes around transparent pixels."""
    rgba = np.asarray(image.convert("RGBA"), dtype=np.float32)
    alpha = rgba[..., 3] / 255.0

    channels = []
    for channel in range(3):
        premultiplied = np.clip(rgba[..., channel] * alpha, 0, 255).astype(np.uint8)
        resized = Image.fromarray(premultiplied, "L").resize(
            (size, size), Image.Resampling.LANCZOS
        )
        channels.append(np.asarray(resized, dtype=np.float32))

    resized_alpha = np.asarray(
        Image.fromarray((alpha * 255).astype(np.uint8), "L").resize(
            (size, size), Image.Resampling.LANCZOS
        ),
        dtype=np.float32,
    )

    output = np.zeros((size, size, 4), dtype=np.uint8)
    nonzero = resized_alpha > 0.5
    for channel, premultiplied in enumerate(channels):
        restored = np.zeros_like(resized_alpha)
        restored[nonzero] = premultiplied[nonzero] * 255.0 / resized_alpha[nonzero]
        output[..., channel] = np.clip(np.rint(restored), 0, 255).astype(np.uint8)
    output[..., 3] = np.clip(np.rint(resized_alpha), 0, 255).astype(np.uint8)
    return Image.fromarray(output, "RGBA")


def octagon(left: int, right: int, chamfer: int, scale: int) -> list[tuple[int, int]]:
    """Return a centered, geometrically exact 45-degree chamfered square."""
    left *= scale
    right *= scale
    chamfer *= scale
    return [
        (left + chamfer, left),
        (right - chamfer, left),
        (right, left + chamfer),
        (right, right - chamfer),
        (right - chamfer, right),
        (left + chamfer, right),
        (left, right - chamfer),
        (left, left + chamfer),
    ]


def main() -> None:
    high_size = OUTPUT_SIZE * SUPERSAMPLE
    high_resolution = Image.new("RGBA", (high_size, high_size), (0, 0, 0, 0))
    draw = ImageDraw.Draw(high_resolution)

    # Each layer keeps the original bounds and thickness, but its former
    # nearest-neighbour steps become one continuous diagonal bevel.
    for left, right, chamfer, color in LAYERS:
        draw.polygon(octagon(left, right, chamfer, SUPERSAMPLE), fill=color)

    # Create the central alpha opening only after all opaque bevel layers.
    opening = Image.new("L", (high_size, high_size), 0)
    ImageDraw.Draw(opening).polygon(
        octagon(*INNER_OPENING, SUPERSAMPLE), fill=255
    )
    pixels = np.asarray(high_resolution).copy()
    pixels[np.asarray(opening) > 0] = (0, 0, 0, 0)
    smoothed = resize_premultiplied(Image.fromarray(pixels, "RGBA"), OUTPUT_SIZE)
    smoothed.save(DESTINATION)
    print(f"Created {DESTINATION}")


if __name__ == "__main__":
    main()
