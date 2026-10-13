# Common face layout for Sword and Axe

All eight portraits use a common face construction region in a 256×256 cell:

- Face base: x44..212, y114..212 (168×98).
- Eye baseline: y160.
- Hair, helmets, headbands, beards and other attachments retain each character's concept.
- Size/position is derived from facial landmarks, not the entire head or helmet alpha bounds.
- The visible skin can be covered by a beard, mouth wrap, hair or helmet; the underlying face reference stays the same.

Native imagegen edited the Sword and Axe sheets using the same face construction diagram. `Pack-Final.ps1` packs both into 512×512 atlases and fixes face landmarks to the common layout. It separately fits upper headgear and side decorations so that their size does not shrink the face. The old 8px Sword pivot offset is removed because face placement is now baked into the atlas.

`Tower_Frames_Atlas_v1.png` fills all six interior windows with opaque white. The imagegen frame edit is preserved as `source-frames.png`; final packing applies the white fill through the original inner alpha window and preserves the existing outer rims, ornaments and exterior transparency pixel for pixel. This prevents the generated frame variant's gray noise from entering the asset.

Project assets:

- `Assets/4. Asset/2. Tower/CubeConcepts/Tower_Sword_Characters_Atlas_v2.png` (existing sprite IDs retained).
- `Assets/4. Asset/2. Tower/CubeConcepts/Tower_Axe_Characters_Atlas_v1.png` (four sliced portraits, new asset, no runtime hookup).
- `Assets/4. Asset/2. Tower/CubeConcepts/Tower_Frames_Atlas_v1.png` (existing sprite IDs retained).

`preview.png` shows all eight with the same Bronze frame. `face-guide-preview.png` overlays the common face rectangle and eye baseline. Native prompts are in `prompts/`. Before assets are in `before/`.

Only raster assets/import metadata were changed. No Unity execution, prefab modifications or gameplay-code changes were made.
