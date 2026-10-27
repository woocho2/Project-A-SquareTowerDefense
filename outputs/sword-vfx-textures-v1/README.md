# Sword VFX textures

Runtime deliverables: `Assets/Resources/Effects/FX_*.png` and their TextureImporter `.meta` files.

Generated with the built-in `image_gen` tool, one image per asset. The original prompts are in `prompts.json`; the selected thin-ring replacement prompt is in `ring-final-prompt.json`. Selected original images are preserved in `raw/`, with the exact-size exports in `final/`.

Export preserves the source art, resizes to the requested dimensions, normalizes textures 1–7 to white RGB with intensity in alpha, and retains color for the two flower textures. The light streak's unused vertical canvas was cropped before resizing. Noise is opaque grayscale and periodically blended so opposite edges match exactly.

`FX_Textures_Preview.png` shows all ten assets on a dark diagnostic background. This background is not in the runtime PNGs. `Noise_Tiling_Preview.png` shows a 3×3 repeat.

All ten files passed dimension, grayscale/color, alpha, and importer-setting checks. The noise's opposite-edge difference is zero. The ring center and the outer borders of the transparent effects are transparent. Full results are in `Validation.json`.

Import settings: Default, Alpha Is Transparency enabled, Bilinear, no compression, Clamp and no mipmaps. Noise uses Repeat and mipmaps instead. Maximum texture size is 2048, so all requested dimensions are retained. Noise is imported as linear data.

No game code was edited for this asset task. A before/after fingerprint comparison of 133 game scripts detected a separate change to `TowerFactory.cs` while generation was running; it was left untouched. Helper export code lives under `Temp/SwordVfxBuild`, outside `Assets`. Unity playback was not run.
