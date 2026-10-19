# Color upgrade symbols

Generated with the built-in imagegen tool using the bottom action-dock mockup as a design reference. The final generation prompt is in `prompt.txt`; the untouched generation is retained as `source.png`.

Production asset: `Assets/4. Asset/5. UI/UI_Color_Upgrade_Atlas.png` and its Unity importer `.meta` file.

Atlas: 1024 x 1024, four 512 x 512 cells, 32px transparent padding, centered pivots, PPU 100, bilinear filtering, no mipmaps or texture compression.

Top row: `ColorUpgrade_Splash` (red), `ColorUpgrade_Single` (blue).
Bottom row: `ColorUpgrade_Buff` (yellow), `ColorUpgrade_Debuff` (purple).

`Pack.ps1` crops and normalizes the four generated sprites for packing, removes only tiny disconnected gutter fragments, and writes slice metadata. It refuses to overwrite an existing atlas. `verification.json` records dimensions, bounds, alpha counts, file IDs, GUID and final PNG hash.

No scripts, scenes, prefabs, existing atlases or UI assignments were changed. Unity import/runtime verification remains with the user.
