# Refined white board and neon grid

The final production plate was created with the built-in image-generation tool. `prompt.txt` records the final edit prompt. The generated PNG is copied unchanged to `Assets/4. Asset/1. BackGround/Tile_BoardFrame_All_Atlas.png`; its original GUID and all 90 sprite IDs remain stable.

The importer maps the 90 realm sprite identities to the same nine sections of the new square master. Pixels per unit is adjusted so each section remains 1.5 world units. `original-atlas.png` and its meta file preserve the previous atlas.

`BoardFrameWhiteNeon.shader` and its material draw fine cyan lines on the pale face in world space, at the authored grid spacing of 1.5 units. The material is assigned only to Stage1's BoardTile renderer. `_GridOpacity` controls the line strength and `_GridColor` controls the neon colour. The other board-cell renderers retain their materials.

`board-preview.png` assembles the actual sprite sections and approximates the material offline; it is not a Unity screenshot. `Preview.cs` rebuilds that preview. `verification.txt` records the sprite identity, world-size, image-copy and reference checks. The earlier `board-frame-white-neon-v1` candidate is superseded.
