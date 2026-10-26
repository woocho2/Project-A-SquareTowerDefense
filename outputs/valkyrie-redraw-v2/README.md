# Valkyrie redraw

Generated with the built-in image_gen edit tool using the existing Valkyrie portrait as the reference. Full generation prompt: `prompt.txt`.

Applied to `Assets/4. Asset/4. Enemy/Asgard/Valkyrie_Character.png` and `Valkyrie_EquipmentMask.png`. Existing importer metadata, GUIDs and appearance references remain unchanged. The original files and metadata are saved in `before/`.

The generated portrait is packaged as a 512x512 RGBA image. `Package.ps1` downsamples the generated image and derives an aligned equipment mask from neutral colors inside the metal regions, excluding the face and hair. It also creates `comparison.png`: original portrait, new portrait, and new portrait with the game's self-heal equipment tint. `verification.txt` records image bounds and mask coverage.

Source generation: `C:/Users/user/.codex/generated_images/01a175ac-4aba-7072-926c-7ba5fcb44831/exec-9c48bfc9-656b-4aa3-b92e-ddbd5e2d2fbe.png`.

Validated image dimensions, transparency, mask coverage, final copy hashes, and preservation of metadata/appearance references. Unity was not launched for runtime verification.
