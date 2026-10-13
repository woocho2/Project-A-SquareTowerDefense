# Sword target tower: swordsman front

Face-only East Asian martial swordsman concept for the blue single-target sword tower. Three-tone cartoon shading, blue headband, dark hair, bronze sword clasp. One eye is closed as a straight horizontal stroke, and the mouth is also a straight neutral stroke for a quiet, stoic character.

Final sprite: `Assets/4. Asset/2. Tower/CubeConcepts/Tower_Bronze_Sword_Target_Front_v3.png`, with a single healed scar on the closed-eye side. Earlier v1 has the curved closed eye/smile; v2 has the straight-eye/mouth stoic expression without the scar.
256×256, PPU 192, centered pivot, Full Rect sprite. The rounded-square alpha outline matches the spawn tile exactly. Runtime tower/prefab bindings remain unchanged.

Built-in imagegen prompt set: [base character](prompt.txt), [one-eye expression](prompt-eye.txt), [straight closed eye](prompt-straight-eye.txt), [straight neutral mouth](prompt-stoic.txt), [healed scar](prompt-scar.txt).
Technical packaging reused `../bronze-sword-splash-front/Pack-TileConcept.ps1` with generated source `exec-5a5a9f9d-e66a-4cf6-a695-bbed608c7fec.png`. `Apply-Stoic-Patches.ps1` composites only the generated expression regions back into the approved v1 image; hair, headband, open eye and every pixel outside the two patches remain unchanged. Original alpha is preserved everywhere.

Scar source: `exec-d6625c60-f8bd-4a0b-b9be-af9cdf2b89c9.png`. After tile fitting, `Apply-Scar-Patch.ps1` composites only generated scar-colored pixels onto v2. Existing dark eyelid/eyebrow strokes stay on top, and all remaining v2 pixels/alpha remain unchanged.

Additional tier-identification concept: `Assets/4. Asset/2. Tower/CubeConcepts/Tower_Bronze_Sword_Target_Satgat_Front_v1.png`, with a bronze/brown conical hat and blue headband. Prompt: [satgat concept](prompt-satgat.txt). Existing v3 face remains available. `Preserve-Hat-Face.ps1` keeps all original face/scar/expression pixels and lower hair from row 130 down. Hat is intended for tier colors while the blue band retains the single-target identity.

Corrected square hat concept: `Assets/4. Asset/2. Tower/CubeConcepts/Tower_Bronze_Sword_Target_Satgat_Front_v2.png`. Hat top is flat and contained in the rounded-square outline. The forehead headband/clasp are removed; the blue band now wraps around the hat, with a plain ivory sword mark on the band. Brown hat provides tier color while blue retains the single-target identity. Prompts: [square outline correction](prompt-square-hat.txt), [move band to hat](prompt-hat-band.txt). This revision uses the complete generated image because the forehead/hair layout changed; `Preserve-Hat-Face.ps1` applies only to the earlier v1 concept. Packaged at 256×256 / PPU 192 with the exact spawn-tile alpha mask.

Latest hat revision: `Assets/4. Asset/2. Tower/CubeConcepts/Tower_Bronze_Sword_Target_Satgat_Front_v3.png`. Brim and blue hat band sit lower, closer to the eyebrows, while eyes, scar and straight mouth remain visible. The top edge stays flat inside the fixed rounded-square footprint. Prompt: [lower hat](prompt-lower-hat.txt). Native sprite size and PPU match the spawn tile.

The user still found the square hat insufficiently recognizable as a satgat. [Shape comparison](hat-shape-comparison/preview.png) compares a natural conical silhouette fully contained in a square canvas with a fully filled square outer contour. Dashed squares belong to the comparison preview only. This is a clarification artifact outside Assets, not a replacement game sprite. Prompt: [comparison](hat-shape-comparison/prompt.txt).

The subsequent choice moves the square silhouette to a separate tier-colored frame and lets the character retain a true conical hat. [Bronze frame and plum motif concept](framed-petals/preview.png), generated with [this prompt](framed-petals/prompt.txt), established the direction. The complete four-class sword set and five separate tier frames are now in `Assets/4. Asset/2. Tower/CubeConcepts/Tower_Sword_Characters_And_Frames_Atlas_v1.png`; [20 combinations and layer documentation](../sword-tower-layers-v1/README.md).

Current project assets supersede the combined atlas: `Tower_Sword_Characters_Atlas_v2.png` and `Tower_Frames_Atlas_v1.png` in `Assets/4. Asset/2. Tower/CubeConcepts`. The swordsman now has a larger face; the frame atlas also includes a white/gold synergy frame. [Current previews, prompts and composition details](../sword-tower-layers-v2/README.md).
