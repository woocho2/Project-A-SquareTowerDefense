# Sword pixel-art concepts — 64×64

Four pixel concepts follow the planning order: Hero (splash), Swordsman (target), Paladin (buff), Death Knight (debuff).

Every portrait is exactly 64×64 with binary alpha and 18–19 opaque colors. All four reuse the same generated empty face shape at x11..52 / y28..53, a 42×26 logical pixel area. Eye landmarks map to row40. The face-layer alpha masks are compared pixel for pixel during packing and are identical. Human characters share the same three skin tones; the Death Knight uses pale skin with the same geometry.

The native imagegen tool generated each pixel-style portrait and a separate blank common face. `Pack-Pixel64.ps1` resamples using face/eye anchors, composites the common face with each expression and head style, and limits the palette. Shape normalization is based on the face, not on the full helmet silhouette. The pixel previews use integer enlargement without smoothing.

Files:

- `preview.png`: 4× enlarged portraits, 1× originals, and 2× expression/face layers.
- `tile-preview.png`: 64px examples with the current smooth Bronze frame for style comparison.
- `Hero64.png`, `Swordsman64.png`, `Paladin64.png`, `DeathKnight64.png`: individual transparent 64×64 originals.
- `CommonFace64.png`, `Faces64_Atlas.png`, `HeadStyles64_Atlas.png`: reusable face template and intermediate layers.
- `Tower_Sword_Pixel64_Concept_Atlas_v1.png`: 128×128 2×2 atlas.
- `source/` and `prompts/`: native generated sources and prompts.

Only the combined 128×128 concept atlas is imported into `Assets/4. Asset/2. Tower/CubeConcepts/Pixel64_Concepts/`, with four sliced 64×64 sprites, Point filtering, no mipmaps/compression, center pivot, Full Rect mesh and PPU 48. Individual copies and previews are outside Assets.

This is a review concept. Existing runtime portrait selection, TowerCharacterCatalog, prefabs and gameplay code were not changed; Unity was not launched.
