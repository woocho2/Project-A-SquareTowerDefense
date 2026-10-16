# Pastel tower decorations

Updated the existing `Assets/4. Asset/2. Tower/Tower_Decorations_Atlas_v1.png` in place.

- Six tiers: warm bronze, cool gray silver, yellow gold, turquoise mint mithril, sky blue diamond, rose ruby synergy.
- Follow-up saturation correction: the first pastel palette appeared almost white in the Inspector. The palette in `Apply-Pastel.ps1` now uses richer colors while retaining the same matte three-tone shading. No runtime tint, scale or material workaround was used. The previous pale PNG is `before-saturation.png`; `saturation-comparison.png` shows that version above the corrected version on white.
- Color/shading source created with the built-in image_gen tool, using the existing atlas as the edit target. Full prompt: `prompt.txt`; untouched generated output: `generated-source.png`.
- `Apply-Pastel.ps1` packs the generated shading into the existing 768x512 atlas, limits it to three soft matte paint tones per cell, and restores the exact original alpha at every pixel.
- Original 256x256 sprite cells and centred 242x242 silhouettes are preserved. The centres and exterior remain transparent. The source artwork is backed up under `before/`.
- Sprite metadata, IDs, GUIDs, character assets, backgrounds, prefab and code were checked by hash and remain unchanged. No Unity launch or play validation was performed.
- `comparison.png` shows before/after on the same checkerboard. `verification.txt` records asset checks.
