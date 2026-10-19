# Boss stats and tower attack-type icons

Generated with built-in imagegen using the existing tower stat atlas as the style reference. Final prompts are in `prompts.json`; untouched generated sources are `source-Boss.png` and `source-AttackType.png`.

Production PNGs and Unity importer metadata:

- `Assets/4. Asset/5. UI/Boss_Stat_Icon_Atlas.png`: 1536 x 1024; six 512 x 512 cells.
- `Assets/4. Asset/5. UI/Tower_AttackType_Icon_Atlas.png`: 1024 x 1024; four 512 x 512 cells.

Boss atlas reading order: Defense (shield), Resistance (magical shield), Movement (winged boot), DamageTaken (shield receiving impact), Healing (heart and plus), Weight (calibration mass).

Attack-type atlas reading order: Splash (multi-target burst), Single (one target), Buff (up arrow), Debuff (down arrow).

All ten tiles use centered pivots, PPU 100, equal 448 x 448 visible bounds, 32px transparent padding, full-rectangle meshes, bilinear filtering, no mipmaps and no texture compression. Individual sprite names and IDs are listed in `verification.json`.

`Pack.ps1` crops and aligns generated tiles, removes only tiny disconnected gutter fragments, checks transparent cell boundaries and writes Sprite Multiple metadata. Existing assets are hash-checked and left unchanged, including the plain circular attack-type badges. These illustrated attack-type stat icons are separate UI assets. No skill icons were produced, and no scripts, prefabs, scenes or UI bindings were modified. Unity was not launched.
