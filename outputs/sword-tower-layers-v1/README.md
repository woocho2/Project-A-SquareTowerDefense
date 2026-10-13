# Sword towers: separate characters and tier frames

Built with native imagegen using [the saved prompt set](prompts). Planning references: `Assets/27. 기획 통합.txt`, chapter 4 character concepts and chapter 4-3 sword skills. The current plan uses WHITE for buffs, replacing the earlier yellow concept.

Original combined source (now archived outside Assets): `outputs/sword-tower-layers-v1/source/Tower_Sword_Characters_And_Frames_Atlas_v1.png`. Superseded by [revised characters and separate frame atlas](../sword-tower-layers-v2/README.md).
One 768×768 atlas, nine 256×256 sprites, PPU 192, centered pivots, Full Rect mesh, no mipmaps or texture compression. Texture GUID: `3dc0d1c0881d4668a3cf42a5710e7bdf`.

Four character layers include their small skill motifs. The five tier frames are separate layers with transparent centers and an identical alpha mask. Place the frame below the character at the same position and scale, with both SpriteRenderers white. The board's spawn floor remains visible through empty areas. The previews use the Asgard spawn floor underneath both layers.

| Sprite | File ID | Meaning |
| --- | --- | --- |
| Tower_Sword_Hero | 42001 | Red splash: open-faced hero knight and Excalibur sword rays |
| Tower_Sword_Swordsman | 42002 | Blue target: real conical satgat, stoic scarred face, plum petals |
| Tower_Sword_Paladin | 42003 | White buff: approved original knight helmet moved to paladin; ivory panels and blessing glints |
| Tower_Sword_DeathKnight | 42004 | Black debuff: dark human knight, spectral cursed swords |
| Tower_Frame_Bronze | 42011 | Brown bronze |
| Tower_Frame_Silver | 42012 | Cool silver |
| Tower_Frame_Gold | 42013 | Warm yellow gold |
| Tower_Frame_Mithril | 42014 | Blue-green mithril |
| Tower_Frame_Diamond | 42015 | Bright ice white / pale blue |

Compose these nine sprites into 20 towers. Tower IDs use `(tier * 1000) + (attackClass * 100) + 1`, with attack classes 1–4 in the character order above. Character art stays identical across tiers; only the selected frame changes. [All 20 combinations](preview-20.png), [character compositions and separate layers](preview-characters.png), and [sprite rects/source/prompt manifest](manifest.json).

`Build-Atlas.ps1` crops and fits generated art, preserves character aspect ratios and transparency, packs exact RGBA, and shares the clean gold frame's alpha across all tier frames. The dark inset pixels are also shared; only generated metal colors differ. This removes the bronze source's unintended faint opacity inside the frame. Character layers fit within the 224×224 inner area and frame outer alpha stays inside the spawn tile.

Packing validation checks all nine atlas layers, every saved RGBA pixel, identical frame alpha, transparent frame centers and character containment. `SwordAtlasCheck.cs` checks the real Unity import, stable file IDs, sprite rects, bounds, pivots and shared texture. Validation log: `validation.log`.

These are new art assets and previews. Runtime TowerVisual, gameplay code, scenes and prefab bindings were not changed during this art task. Earlier concept assets are retained.
