# Raise current sword portraits within their frames

Changed asset settings: `Assets/4. Asset/2. Tower/CubeConcepts/Tower_Sword_Characters_Atlas_v2.png.meta`.
All four sword character sprites use Custom alignment (9) and pivot `(0.5, 0.46875)` instead of the centered `(0.5, 0.5)`.

Lowering the pivot by 8/256 moves the rendered portrait UP by 8 source pixels at the same transform. At 192 PPU and the current 256/192-unit canvas, that is 0.041667 world units. Frame pivots stay centered, so the characters move within the frame. Art, sprite rectangles, size, scale, GUID and file IDs stay intact.

[Before/after illustration](preview.png). This is a local image preview of the pivot offset, not a Unity capture. Original metadata is saved in `before.png.meta`; `png-before.sha256.txt` records the unchanged bitmap. No Unity execution, runtime code changes, or raster regeneration.
