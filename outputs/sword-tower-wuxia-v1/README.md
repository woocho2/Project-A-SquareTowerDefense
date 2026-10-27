# Sword tower atlas

Applied texture: `Assets/4. Asset/2. Tower/TowerCharacter/Tower_Sword_Front_Atlas_v2.png`.

Four 512 x 512 cells in the existing 1024 x 1024 transparent atlas:

| Position | Character | Existing sprite fileID |
| --- | --- | --- |
| Top left | 검성, Sword Saint | 52010 |
| Top right | 검객, Swordsman | 52011 |
| Bottom left | 검선, Sword Immortal | 52012 |
| Bottom right | 검귀, Sword Demon | 52013 |

All creative image edits were made using the built-in image_gen tool. `prompts.json` contains the initial production prompts. `final-edits.json` records the applied forehead corrections and selected outputs. The discarded swept-back hairstyle was not used.

The Sword Saint has ruby-red eyes. The Sword Immortal's forehead-overlapping bangs are removed while retaining side and back hair. The Swordsman's forehead locks are tidied. All portraits use the Sword Immortal's face-to-torso proportions and a common canvas scale.

`sources/` contains the four selected full-resolution transparent sprites. `Build-Atlas.ps1` packs these into the existing grid, using the same scale and a straight lower-chest crop with transparent gutters. `Atlas_Preview.png` adds labels on a dark background for review only; the actual texture has no labels or opaque background.

The original PNG and `.meta` are in `original/`. The existing importer metadata, sprite IDs, atlas GUID and catalog references were retained. See `Validation.txt` for the completed checks. Unity gameplay rendering was not exercised.
