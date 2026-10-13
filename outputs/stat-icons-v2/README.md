# Stat icons v2

Generated with the built-in imagegen tool using [prompt.txt](prompt.txt). Cropped and packed from the generated transparent artwork; original alpha and aspect ratios are preserved.

Project asset: `Assets/4. Asset/5. UI/Stat_Icon_Atlas_v2.png`.
One 1280×1024 texture, 5 columns × 4 rows, 20 named 256×256 Simple sprites. Transparent background, centered pivot, no mipmaps or texture compression.

| Row | Icons from left to right |
| --- | --- |
| 1 | Attack power, range, action interval, attack count, additional hit count |
| 2 | Critical chance, critical damage, skill coefficient, trait/mastery, stacks |
| 3 | Attack behavior, skill, splash radius, duration, adaptive stats |
| 4 | Enemy health, defense, resistance, movement speed, action interval |

TowerInfoPanel in `Assets/2. Prefab/Canvas.prefab` connects twelve stat Images to the corresponding new sprites. Mastery, stacks, ability value, attack behavior, and skill now have distinct icons. Image colors, rect sizes, labels, events, and gameplay values are preserved.

The old `Stat_Icon_Atlas.png` remains because `HPBar.prefab` uses its two health bar sprites. Health bars do not consume slots in the new twenty-icon atlas. Scene-local range icons outside TowerInfoPanel keep their existing references.

`manifest.json` records sprite rectangles and stable file IDs; `assignments.json` records panel assignments. `preview.png` is a labeled contact sheet outside Assets.

Validation: Unity 6000.3.9f1 imported all twenty sprites in an isolated project. Checked exact serialized IDs, 15 tower/5 enemy split, 256×256 rects, transparent alpha and clear cell boundaries. See `validation.log`.

The open Unity prefab stage initially saved its previous references over the disk edit. A temporary Editor migration synchronized that stage and saved all twelve bindings through Unity; see `editor-connection.txt`. The temporary Editor script was removed after completion; its source is archived as `SyncOpenPrefab.cs` outside Assets.
