# Enemy character sprites

Each of the 27 existing `Assets/4. Asset/4. Enemy/**/*_Character.png` textures now also imports as one full-rectangle Sprite, using the entire existing 512 x 512 image.

Only three importer fields changed: `textureType` (Sprite), `spriteMode` (Single), and `spriteMeshType` (Full Rect). Existing PNG contents, texture GUIDs, pixels per unit, center pivots, filtering, compression, alpha settings, and equipment-mask textures remain unchanged. Enemy appearance assets retain their Texture2D references (file ID 2800000). Sprite references can use the same asset GUID with file ID 21300000.

No duplicate image assets were created. No scenes, prefabs, scripts or UI assignments were edited. Unity was not launched.

`before/` retains the exact original importer metadata. `Configure.ps1` applies the settings and checks that no unrelated metadata, PNG data, texture GUID or appearance-asset reference changed. `verification.json` lists all converted characters.
