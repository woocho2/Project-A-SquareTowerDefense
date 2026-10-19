# Tower card pastel cartoon refresh

Four existing production PNG atlases were replaced in place using the built-in imagegen tool. Generation instructions are recorded in `prompts.json`; follow-up transparency instructions are in `transparency-prompts.json`.

| Atlas | Result |
| --- | --- |
| Tower_Card_Attack_Atlas.png | Four plain pastel color circles: coral, blue, yellow, muted plum |
| Tower_Card_Icon_Atlas_v1.png | Six cartoon stat tiles; attack-count tile uses yellow background |
| Tower_Card_Emblem_Atlas.png | Fourteen matching weapon/element emblems; remaining two cells empty |
| Tower_Card_Tier_Atlas.png | Six softer cartoon tier diamonds, including ruby synergy |

Existing atlas dimensions, 512px cell rectangles, sprite names, ordering, centered pivots, texture GUIDs, sprite IDs and file IDs were preserved. Importer `.meta` files were not edited. No scene, prefab, script or UI layout changes were made. Unity was not launched.

`before/` contains the exact original PNGs and metadata. `before.json` records their hashes. `source-*.png` are unchanged generated sources. `packed-*.png` match the deployed PNGs.

`Pack.ps1` performs sprite cropping, center/size normalization, gutter-fragment removal and atlas packing, verifies transparent cell boundaries, checks original file hashes for concurrent edits, and confirms that importer metadata remains unchanged. Per-sprite bounds and final hashes are recorded in `verification.json`.
