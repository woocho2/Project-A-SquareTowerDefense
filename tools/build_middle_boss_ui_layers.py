"""Rebuild UI geometry on a 128px grid; export aligned 1024px RGBA layers."""
from collections import deque
from pathlib import Path
import json
import math

import numpy as np
from PIL import Image, ImageDraw, ImageFont


ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / 'Assets/4. DotAsset/5. UI/MiddleBossSummon.png'
DEST = ROOT / 'Assets/4. DotAsset/5. UI/MiddleBossSummon_Layers'
PREVIEW = ROOT / 'output/ui/MiddleBossSummon_Layers'
SIZE, SCALE = 128, 8
CREAM = (252, 242, 221, 255)


def empty():
    return np.zeros((SIZE, SIZE, 4), dtype=np.uint8)


def panel_distance():
    """Euclidean inward distance to one symmetric clipped-square polygon.

    Continuous bounds [6, 122], 10px corner cuts. Pixel centers remove
    off-by-one asymmetry. All bevels are offsets of these same eight edges.
    """
    y, x = np.mgrid[:SIZE, :SIZE].astype(float) + 0.5
    root2 = math.sqrt(2)
    return np.minimum.reduce([
        x - 6, 122 - x, y - 6, 122 - y,
        (x + y - 22) / root2, (234 - x - y) / root2,
        (x - y + 106) / root2, (y - x + 106) / root2,
    ])


def extract_symbol():
    """Remove only border-connected cream around the original skull.

    The crop deliberately excludes both text plaques. Flood fill preserves
    pale details enclosed inside the skull instead of color-keying them out.
    """
    source = Image.open(SOURCE).convert('RGBA')
    assert source.size == (128, 128)
    crop = np.array(source.crop((38, 40, 90, 91)))
    bg = np.array(source.getpixel((64, 40))[:3], dtype=np.int16)
    candidate = np.max(np.abs(crop[:, :, :3].astype(np.int16) - bg), axis=2) <= 8
    h, w = candidate.shape
    outside = np.zeros((h, w), dtype=bool)
    queue = deque([(x, 0) for x in range(w)] + [(x, h - 1) for x in range(w)]
                  + [(0, y) for y in range(h)] + [(w - 1, y) for y in range(h)])
    while queue:
        x, y = queue.popleft()
        if not (0 <= x < w and 0 <= y < h) or outside[y, x] or not candidate[y, x]:
            continue
        outside[y, x] = True
        queue.extend(((x - 1, y), (x + 1, y), (x, y - 1), (x, y + 1)))
    crop[outside] = 0
    ys, xs = np.where(crop[:, :, 3] > 0)
    crop = crop[ys.min():ys.max() + 1, xs.min():xs.max() + 1]
    h, w = crop.shape[:2]
    # Add one transparent column/row if needed to keep the full-canvas center.
    canvas = empty()
    left, top = (SIZE - w) // 2, (SIZE - h) // 2
    canvas[top:top + h, left:left + w] = crop
    return canvas


def build_layers():
    distance = panel_distance()
    frame = empty()
    # Flat neutral bands keep all RGB channels equal for Unity Image.color tint.
    for start, end, value in [(0, 2, 148), (2, 4, 210), (4, 6, 255), (6, 8, 223), (8, 9, 172)]:
        frame[(distance >= start) & (distance < end)] = (value, value, value, 255)

    background = empty()
    background[distance >= 8] = (255, 255, 255, 255)
    background[distance >= 11] = CREAM

    boxes = empty()
    # Equal 76x20 rectangles mirrored across the horizontal centerline.
    for top in (18, 90):
        boxes[top:top + 20, 26:102] = (202, 202, 202, 255)
        boxes[top + 1:top + 19, 27:101] = (255, 255, 255, 255)

    return {'Background': background, 'Symbol': extract_symbol(),
            'Frame': frame, 'TextBoxes': boxes}


def verify(layers):
    for name, a in layers.items():
        assert a.shape == (128, 128, 4)
        assert set(np.unique(a[:, :, 3])) == {0, 255}, name
        assert np.all(a[a[:, :, 3] == 0] == 0), name
        if name != 'Symbol':
            assert np.array_equal(a, a[:, ::-1]), (name, 'horizontal symmetry')
            assert np.array_equal(a, a[::-1]), (name, 'vertical symmetry')
        if name in ('Frame', 'Background'):
            assert np.array_equal(a, a.transpose(1, 0, 2)), (name, 'diagonal symmetry')
    frame, bg, symbol, boxes = [layers[n] for n in ('Frame', 'Background', 'Symbol', 'TextBoxes')]
    assert frame[64, 64, 3] == 0 and bg[64, 64, 3] == 255
    assert np.all(bg[:, :, 3][symbol[:, :, 3] > 0] == 255)
    assert not np.any((symbol[:, :, 3] > 0) & (boxes[:, :, 3] > 0))
    assert np.array_equal(boxes[18:38, 26:102], boxes[90:110, 26:102])
    assert np.array_equal(frame[:, :, 0], frame[:, :, 1])
    assert np.array_equal(frame[:, :, 1], frame[:, :, 2])
    return {'canvas': [1024, 1024], 'logical_grid': [128, 128],
            'alpha': 'RGBA, empty pixels A=0, solid pixels A=255',
            'symmetry': 'Frame, Background, TextBoxes: exact horizontal + vertical',
            'corners': 'Frame and Background: same 45-degree offset geometry',
            'symbol': 'Original skull extracted, centered to nearest logical pixel',
            'boxes': 'Two equal 76x20 logical-pixel rectangles; extra standalone box included'}


def composite(layers, tint=None):
    result = Image.new('RGBA', (SIZE, SIZE))
    for name in ('Background', 'Symbol', 'Frame', 'TextBoxes'):
        a = layers[name].copy()
        if name == 'Frame' and tint is not None:
            a[:, :, :3] = np.rint(a[:, :, :3].astype(float) * np.array(tint) / 255).astype(np.uint8)
        result = Image.alpha_composite(result, Image.fromarray(a))
    return result


def preview(layers):
    cell, gap, margin, label = 320, 24, 32, 46
    width = 4 * cell + 3 * gap + 2 * margin
    board = Image.new('RGB', (width, 2 * (cell + label) + 3 * margin), '#343942')
    draw = ImageDraw.Draw(board)
    try:
        font = ImageFont.truetype('C:/Windows/Fonts/malgun.ttf', 21)
    except OSError:
        font = ImageFont.load_default()
    cards = [(n, Image.fromarray(layers[n])) for n in ('Background', 'Symbol', 'Frame', 'TextBoxes')]
    cards += [('White frame', composite(layers)), ('Coral tint', composite(layers, (222, 119, 132))),
              ('Blue tint', composite(layers, (111, 176, 233))), ('Mint tint', composite(layers, (112, 194, 167)))]
    for i, (name, picture) in enumerate(cards):
        x = margin + (i % 4) * (cell + gap)
        y = margin + (i // 4) * (cell + label + margin)
        # Checker pattern exists ONLY in this clearly separate preview board.
        check = Image.new('RGBA', (cell, cell))
        check_draw = ImageDraw.Draw(check)
        for cy in range(0, cell, 16):
            for cx in range(0, cell, 16):
                color = '#555d68' if (cx // 16 + cy // 16) % 2 else '#626c79'
                check_draw.rectangle((cx, cy, cx + 15, cy + 15), fill=color)
        check.alpha_composite(picture.resize((cell, cell), Image.Resampling.NEAREST))
        board.paste(check.convert('RGB'), (x, y))
        draw.text((x + cell // 2, y + cell + 10), name, font=font, fill='white', anchor='mt')
    board.save(PREVIEW / 'Preview.png')


def main():
    layers = build_layers()
    report = verify(layers)
    DEST.mkdir(parents=True, exist_ok=True)
    PREVIEW.mkdir(parents=True, exist_ok=True)
    for name, a in layers.items():
        path = DEST / f'MiddleBoss_{name}.png'
        Image.fromarray(a).resize((SIZE * SCALE, SIZE * SCALE), Image.Resampling.NEAREST).save(path)
        # Verify actual decoded output, not only in-memory arrays.
        reopened = Image.open(path)
        assert reopened.mode == 'RGBA' and reopened.size == (1024, 1024)
        assert reopened.getchannel('A').getextrema() == (0, 255)
    standalone = Image.fromarray(layers['TextBoxes']).crop((26, 18, 102, 38))
    # 2px transparent padding on every side, useful as a separate movable box.
    padded = Image.new('RGBA', (80, 24))
    padded.alpha_composite(standalone, (2, 2))
    padded.resize((640, 192), Image.Resampling.NEAREST).save(DEST / 'MiddleBoss_TextBox_Single.png')
    composite(layers, (222, 119, 132)).resize((1024, 1024), Image.Resampling.NEAREST).save(PREVIEW / 'Composite_Coral.png')
    preview(layers)
    print(json.dumps(report, ensure_ascii=False, indent=2))
    print(f'Assets: {DEST}')
    print(f'Preview: {PREVIEW / "Preview.png"}')


if __name__ == '__main__':
    main()
