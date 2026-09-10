"""Convert ImageGen chroma-key drafts into aligned, transparent UI sprites.

Usage: python tools/prepare_action_ui_symbols.py Name=source.png [...]
The final grid, canvas and anchor are taken from MiddleBoss_Symbol.png.
"""
from pathlib import Path
import json
import shutil
import sys

import numpy as np
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[1]
UI = ROOT / 'Assets/4. DotAsset/5. UI'
DEST = UI / 'Buttons'
OUT = ROOT / 'output/ui/ActionSymbols'
LAYERS = UI / 'Buttons'
NAMES = {'TowerCreate': '타워 생성', 'TierUpgrade': '티어 강화',
         'ColorUpgrade': '컬러 강화', 'Speed': '배속', 'Skip': '스킵'}
SCALE = 8
PLAYBACK_HEIGHT = 43


def prepare(path, reference, max_height=None):
    image = Image.open(path).convert('RGBA')
    a = np.array(image)
    r, g, b = [a[:, :, i].astype(float) for i in range(3)]
    # Background is explicitly magenta and excluded from the intended artwork.
    key = (r > 100) & (b > 100) & (g < np.minimum(r, b) * 0.70)
    assert key.mean() > 0.15, 'Unexpected background; inspect draft before conversion'
    a[key] = 0
    cutout = Image.fromarray(a)
    bounds = cutout.getbbox()
    assert bounds is not None
    cutout = cutout.crop(bounds)
    refbox = reference.getbbox()
    max_w = (refbox[2] - refbox[0]) // SCALE
    max_h = (refbox[3] - refbox[1]) // SCALE
    if max_height is not None:
        max_h = min(max_h, max_height)
    ratio = min(max_w / cutout.width, max_h / cutout.height)
    # Odd dimensions align to the same half-logical-pixel axis as the skull.
    width = max(3, int(round(cutout.width * ratio)))
    height = max(3, int(round(cutout.height * ratio)))
    width -= 1 - width % 2
    height -= 1 - height % 2
    width, height = min(width, max_w), min(height, max_h)
    reduced = cutout.resize((width, height), Image.Resampling.LANCZOS)
    small = np.array(reduced)
    mask = small[:, :, 3] >= 128
    # Explicitly clean key-colored remnants after resampling at tiny UI scale.
    sr, sg, sb = [small[:, :, i].astype(float) for i in range(3)]
    mask &= ~((sr > 100) & (sb > 100) & (sg < np.minimum(sr, sb) * 0.70))
    # A limited shared pixel-art palette keeps the icons readable at this scale.
    palette = Image.fromarray(small[:, :, :3]).quantize(colors=24, dither=Image.Dither.NONE).convert('RGB')
    small[:, :, :3] = np.array(palette)
    small[:, :, 3] = np.where(mask, 255, 0)
    small[~mask] = 0
    sprite = Image.fromarray(small).resize((width * SCALE, height * SCALE), Image.Resampling.NEAREST)
    center_x = (refbox[0] + refbox[2]) // 2
    center_y = (refbox[1] + refbox[3]) // 2
    left, top = center_x - sprite.width // 2, center_y - sprite.height // 2
    canvas = Image.new('RGBA', reference.size)
    canvas.alpha_composite(sprite, (left, top))
    alpha = np.array(canvas)[:, :, 3]
    assert set(np.unique(alpha)) == {0, 255}
    bbox = canvas.getbbox()
    assert bbox[0] >= refbox[0] and bbox[1] >= refbox[1]
    assert bbox[2] <= refbox[2] and bbox[3] <= refbox[3]
    return canvas, {'logical_size': [width, height], 'placement': [left, top],
                    'anchor': [center_x, center_y], 'visible_bounds': list(bbox)}


def checker(size):
    im = Image.new('RGBA', (size, size))
    draw = ImageDraw.Draw(im)
    for y in range(0, size, 12):
        for x in range(0, size, 12):
            draw.rectangle((x, y, x + 11, y + 11), fill='#535d68' if (x // 12 + y // 12) % 2 else '#65717d')
    return im


def exact_speed(reference):
    """Two translated copies of one symmetric arrow, not independently drawn."""
    arrow = Image.new('RGBA', (15, 27))
    draw = ImageDraw.Draw(arrow)
    draw.polygon([(0, 0), (14, 13), (0, 26)], fill='#302038')
    draw.polygon([(1, 3), (12, 13), (1, 23)], fill='#609bc9')
    draw.polygon([(2, 5), (11, 13), (2, 21)], fill='#9fd4ee')
    draw.polygon([(3, 7), (9, 13), (3, 19)], fill='#d9f0f6')
    arrow_pixels = np.array(arrow)
    assert np.array_equal(arrow_pixels, arrow_pixels[::-1])
    icon = Image.new('RGBA', (41, 27))
    d = ImageDraw.Draw(icon)
    for top in (6, 16):
        d.rectangle((0, top, 7, top + 4), fill='#302038')
        d.rectangle((1, top + 1, 7, top + 3), fill='#9fd4ee')
    icon.alpha_composite(arrow, (10, 0))
    icon.alpha_composite(arrow, (26, 0))
    assert np.array_equal(np.array(icon.crop((10, 0, 25, 27))),
                          np.array(icon.crop((26, 0, 41, 27))))
    assert np.array_equal(np.array(icon), np.array(icon)[::-1])
    # Match the original Skip height (43 logical pixels), retaining Speed's
    # proportions and crisp pixel grid rather than shrinking Skip.
    enlarged_width = round(icon.width * PLAYBACK_HEIGHT / icon.height)
    icon = icon.resize((enlarged_width, PLAYBACK_HEIGHT), Image.Resampling.NEAREST)
    assert np.array_equal(np.array(icon), np.array(icon)[::-1])
    refbox = reference.getbbox()
    cx, cy = (refbox[0] + refbox[2]) // 2, (refbox[1] + refbox[3]) // 2
    left, top = cx - icon.width * SCALE // 2, cy - icon.height * SCALE // 2
    canvas = Image.new('RGBA', reference.size)
    canvas.alpha_composite(icon.resize((icon.width * SCALE, icon.height * SCALE), Image.Resampling.NEAREST), (left, top))
    return canvas, {'logical_size': [icon.width, icon.height], 'placement': [left, top],
                    'anchor': [cx, cy], 'visible_bounds': list(canvas.getbbox()),
                    'geometry': 'Identical arrow copies; exact vertical mirror symmetry'}


def align_tower_body(canvas, report):
    """Place the tower's visual mass on the shared x=508 symbol axis.

    The green plus intentionally sits off to the lower right, so centering the
    whole icon makes the turret itself look left-heavy. The measured tower-body
    centroid is x=492.53 on the initial placement; 15px is the nearest grid-safe
    shift that centers it on the 508px axis.
    """
    shifted = Image.new('RGBA', canvas.size)
    shifted.alpha_composite(canvas, (15, 0))
    report['placement'][0] += 15
    report['visible_bounds'] = list(shifted.getbbox())
    report['tower_body_alignment'] = {
        'axis_x': 508,
        'horizontal_shift_px': 15,
        'basis': 'tower body visual centroid; plus mark excluded from alignment',
    }
    return shifted, report


def composed(symbol, tint):
    result = Image.open(LAYERS / 'MiddleBoss_Background.png').convert('RGBA')
    result.alpha_composite(symbol)
    frame = np.array(Image.open(LAYERS / 'MiddleBoss_Frame.png').convert('RGBA'))
    frame[:, :, :3] = np.rint(frame[:, :, :3].astype(float) * np.array(tint) / 255).astype(np.uint8)
    result.alpha_composite(Image.fromarray(frame))
    result.alpha_composite(Image.open(LAYERS / 'MiddleBoss_TextBoxes.png').convert('RGBA'))
    return result


def preview(sprites, reference):
    size, margin, gap, label_h = 184, 24, 24, 52
    card_w = size * 2 + 12
    card_h = size + label_h
    board = Image.new('RGB', (margin * 2 + card_w * 3 + gap * 2,
                              margin * 2 + card_h * 2 + gap), '#303742')
    draw = ImageDraw.Draw(board)
    font = ImageFont.truetype('C:/Windows/Fonts/malgun.ttf', 21)
    entries = [('MiddleBoss', reference), *sprites.items()]
    tints = [(222, 119, 132), (231, 187, 121), (135, 188, 161),
             (173, 151, 201), (120, 177, 223), (135, 188, 161)]
    for i, (name, symbol) in enumerate(entries):
        x = margin + i % 3 * (card_w + gap)
        y = margin + i // 3 * (card_h + gap)
        for j, picture in enumerate((symbol, composed(symbol, tints[i]))):
            tile = checker(size)
            tile.alpha_composite(picture.resize((size, size), Image.Resampling.NEAREST))
            board.paste(tile.convert('RGB'), (x + j * (size + 12), y))
        title = '중간보스 · 크기 기준' if name == 'MiddleBoss' else NAMES[name]
        draw.text((x + card_w // 2, y + size + 12), title, anchor='mt', font=font, fill='white')
    board.save(OUT / 'Preview.png')


def main():
    DEST.mkdir(parents=True, exist_ok=True)
    (OUT / 'drafts').mkdir(parents=True, exist_ok=True)
    reference = Image.open(LAYERS / 'MiddleBoss_Symbol.png').convert('RGBA')
    sprites = {name: Image.open(DEST / f'{name}_Symbol.png').convert('RGBA')
               for name in NAMES if (DEST / f'{name}_Symbol.png').exists()}
    report_path = OUT / 'validation.json'
    report = json.loads(report_path.read_text(encoding='utf-8')) if report_path.exists() else {}
    for argument in sys.argv[1:]:
        name, path = argument.split('=', 1)
        assert name in NAMES
        source = Path(path)
        sprites[name], report[name] = exact_speed(reference) if name == 'Speed' else prepare(
            source, reference, max_height=PLAYBACK_HEIGHT if name == 'Skip' else None)
        if name == 'TowerCreate':
            sprites[name], report[name] = align_tower_body(sprites[name], report[name])
        target = DEST / f'{name}_Symbol.png'
        sprites[name].save(target)
        draft_path = OUT / 'drafts' / f'{name}.png'
        if source.resolve() != draft_path.resolve():
            shutil.copy2(source, draft_path)
        check = Image.open(target)
        assert check.mode == 'RGBA' and check.size == reference.size
        assert check.getchannel('A').getextrema() == (0, 255)
    if 'Speed' in sprites and 'Skip' in sprites:
        speed_box, skip_box = sprites['Speed'].getbbox(), sprites['Skip'].getbbox()
        assert speed_box[1] == skip_box[1] and speed_box[3] == skip_box[3]
        assert speed_box[0] + speed_box[2] == skip_box[0] + skip_box[2]
        report['playback_alignment'] = {'height': speed_box[3] - speed_box[1],
                                        'same_center': True, 'same_top_bottom': True}
    preview(sprites, reference)
    report['canvas'] = list(reference.size)
    report['reference_bounds'] = list(reference.getbbox())
    report['alpha_verified'] = True
    (OUT / 'validation.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
    print(json.dumps(report, indent=2))


if __name__ == '__main__':
    main()
