# Shared pastel cartoon UI action atlas

Created with built-in imagegen. The exact final prompt is in `prompt.txt`, and the untouched generated source is in `source.png`.

Production asset: `Assets/4. Asset/5. UI/UI_Action_Atlas.png` plus its importer metadata.
1536 x 1536 atlas; nine 512 x 512 cells; centered pivots; minimum 32px transparent margins; PPU 100; no mipmaps or texture compression.

| Row | Left | Middle | Right |
| --- | --- | --- | --- |
| Top | Symbol_GameSpeed | Symbol_Menu | ColorUpgrade_Splash |
| Middle | ColorUpgrade_Single | ColorUpgrade_Buff | ColorUpgrade_Debuff |
| Bottom | Symbol_CreateTower | Symbol_TierUpgrade | Symbol_UpgradePlus |

The menu is one sprite containing three bars. Existing Canvas menu objects were consolidated into the retained middle Image, now `Img_Option_Menu`. Its parent control and event bindings remain intact. Scene/prefab sprite references were migrated to the new atlas; removed tower-card badges now reference the separate emblem, attack and tier atlases.

`Tower_Card_Icon_Atlas_v1.png` retains only its six CardStat sprites, cropped losslessly to 1536 x 1024. `UI_Icon_Atlas.png` retains gold, gem and the two hearts, repacked losslessly into 1024 x 1024. All ten retained sprites preserve their pixels, names, GUIDs, file IDs and sprite IDs.

Replaced standalone speed, single menu bar, four-color upgrade atlas and plus-button assets were removed from Assets after reference migration. Exact original files are backed up in `before/` outside Assets. No runtime/editor C# code was changed. Unity was not launched.

`Pack.ps1` packs generated artwork; `Consolidate.ps1` performs the lossless crops and reference migration with backup and concurrent-save checks. `atlas.json`, `consolidation.json`, and `verification.json` record the resulting assets and checks.
