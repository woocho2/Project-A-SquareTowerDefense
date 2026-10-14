# Tower footprint and frame reference

This is a measured construction reference, not a new character design. No runtime files or existing art assets were changed. Unity was not launched.

The entire tower (character surface plus tier frame) must form one square board piece. Its final external alpha follows the actual tower-spawn tile, with all face, hair, helmet and beard drawn inside that footprint. Headgear must not change the outside silhouette into a dome, triangle or detached head.

Measured from the current Stage1 scene and actual sprite assets:

| Property | Current value |
| --- | --- |
| Sprite canvas | 256×256 pixels |
| Pixels per unit | 192 |
| Canvas world dimensions | 1.3333333×1.3333333 |
| Grid cell | 1.5×1.5 world units |
| Sprite pivot / tile anchor | (0.5, 0.5) |
| Spawn visible bounds, alpha≥128 | x6, y5, width244, height242 |
| Existing frame visible bounds, alpha≥128 | x6, y5, width244, height242 |
| Shared frame opening, alpha≥128 | x37, y31, width182, height189 |
| Shared opening, 64px point-sampled reference | x9, y8, width46, height47 |
| Existing general-tower render canvas | 1.2666667 world units (95%) |

Pixel y coordinates above are image coordinates, measured from the top of the cropped sprite. All six frame opening alpha masks are identical pixel for pixel. The current frame exterior and spawn exterior share bounds but differ at 17 corner pixels when thresholded at alpha128. Therefore bounds alone are insufficient for exact alignment: the actual spawn alpha is the final outside mask, and `face-opening-mask256.png` is the character drawing mask inside the frame.

For the matched construction, all layers share the 256×256 canvas, center/pivot and 1.3333333 world-unit display size. The existing 95% render size needs to be reconciled when the accepted art is hooked up; it was not silently changed in this measurement step. The external frame corner alpha must also be matched to the spawn mask before declaring an exact silhouette match.

The shared character base is a filled square surface behind the frame, occupying the complete opening. Facial features, hair, headgear and beard decorate this same base. Every character uses the same underlying shape and feature landmarks. Style may be smooth cartoon; a 64px reference does not require pixel-art styling.

Sources:

- `Assets/4. Asset/1. BackGround/Tile_Floor_Atlas.png`, `Tile_TowerSpawn_Asgard`, rect x1024/y512/256×256.
- `Assets/4. Asset/2. Tower/CubeConcepts/Tower_Frames_Atlas_v1.png` and `Tower_Frame_Rims_Atlas_v1.png`.
- `Assets/1. Scenes/3. Stage1.unity`: TowerSpawn/Grid transforms and tile matrices are identity; anchor is centered.
- `Assets/2. Prefab/2. Tower/TowerBase.prefab`: `m_swordCanvasWorldSize: 1.2666667`.

`layout-reference.png` shows the spawn surface, the frame/opening region and their common-canvas overlay. `spawn-tile.png` and `exact-outer-mask256-source.png` preserve the actual spawn alpha. The six named `*-face-opening-mask256.png` files are exact frame drawing masks; `measurements.txt` contains the raw measurements.
