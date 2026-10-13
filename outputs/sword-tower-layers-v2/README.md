# Revised sword characters and separate frame atlas

Native imagegen edits; [saved prompt set](prompts). Each character and frame is a separate 256×256 sprite at PPU 192, centered pivot, Full Rect mesh, no mipmaps or texture compression.

Two project assets in `Assets/4. Asset/2. Tower/CubeConcepts`:

| Atlas | Size | Sprites |
| --- | --- | --- |
| Tower_Sword_Characters_Atlas_v2.png | 512×512 (2×2) | Hero, Swordsman, Paladin, DeathKnight |
| Tower_Frames_Atlas_v1.png | 768×512 (3×2) | Bronze, Silver, Gold, Mithril, Diamond, Synergy |

The swordsman's exposed face is larger relative to the hat while preserving the pointed satgat, stoic eye/mouth/scar and pink plum blossoms. The square silhouette now belongs to the frame, while the character has a softer squared head. Hero floating swords were replaced with crimson splash impact arcs. Death knight floating swords were replaced with muted curse mist, and peach skin became pale white/ash gray. Paladin and all five ordinary tier frames retain the exact previous pixels.

The new synergy frame uses white and gold, gold twin edging and Nordic corner interlace. Its center is transparent. It is a reusable separate frame and does not depict any synergy character. Compose a frame SpriteRenderer below a character SpriteRenderer with matching position/scale and white colors. The existing spawn floor can remain behind both.

Frame names: `Tower_Frame_Bronze`, `Tower_Frame_Silver`, `Tower_Frame_Gold`, `Tower_Frame_Mithril`, `Tower_Frame_Diamond`, `Tower_Frame_Synergy`. IDs: 42011–42016.
Character names: `Tower_Sword_Hero`, `Tower_Sword_Swordsman`, `Tower_Sword_Paladin`, `Tower_Sword_DeathKnight`. IDs: 42001–42004.
See [manifest](manifest.json) for texture GUIDs and slice coordinates.

[Before/after](preview-before-after.png), [four updated characters](preview-characters.png), [six frames](preview-frames.png), [20 ordinary tier combinations](preview-20.png).

`Build-Atlases.ps1` fits new generated art while preserving aspect ratio and true alpha; copies unchanged layers directly from the previous combined source; packs exact RGBA into the two atlases. Source archive: `../sword-tower-layers-v1/source/Tower_Sword_Characters_And_Frames_Atlas_v1.png`.
Packing checks exact palette/alpha preservation for the paladin and five old frames, transparent centers, character containment, pale skin pixels and lossless atlas RGBA round trips. `SeparateAtlasesCheck.cs` checks real Unity imports, separate textures, counts, dimensions, PPU, pivots, bounds and serialized IDs. Log: [validation.log](validation.log).

The superseded combined atlas is archived outside Assets after verifying that no project assets reference its GUID. Sword character and tier-frame sprites are now linked in `TowerBase.prefab`, and `TowerVisual` selects them from the tower ID. [Runtime integration and validation](../sword-tower-runtime/README.md).

Current artwork has been refined in place: thicker rims, matched synergy frame footprint, subtle helmet/brim overlap and a smaller, softer paladin silhouette. [Latest previews and native imagegen prompts](../sword-frame-pop-v1/README.md). Unity verification for these art refinements is performed by the user.
