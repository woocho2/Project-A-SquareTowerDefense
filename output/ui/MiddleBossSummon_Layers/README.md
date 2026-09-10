# MiddleBossSummon UI layers

Assets: `Assets/4. DotAsset/5. UI/MiddleBossSummon_Layers/`

- `MiddleBoss_Background.png`: cream #FCF2DD with white inner border.
- `MiddleBoss_Symbol.png`: crowned skull extracted from the original button.
- `MiddleBoss_Frame.png`: neutral grayscale frame for Unity Image.color tinting.
- `MiddleBoss_TextBoxes.png`: two equal white plaques, positioned symmetrically.
- `MiddleBoss_TextBox_Single.png`: extra standalone plaque for manual placement.

The four aligned layers are 1024x1024 RGBA. Use the same RectTransform size,
centered pivot and anchored position. Stack Background, Symbol, Frame, then
TextBoxes. Set only Frame's Image.color to the desired color. Add TMP text
separately. The optional standalone plaque is 640x192 including transparent
padding; use it instead of TextBoxes when placing the plaques independently.

Sprites use Single, Full Rect, Point filtering, no mipmaps and no compression.
Keep transparent margins; do not auto-trim individual layers.

Geometry is drawn on a 128x128 pixel grid and enlarged exactly 8x without
smoothing. Straight edges and 45-degree clipped corners share mathematical
offsets. Frame, Background and TextBoxes are byte-identical to their horizontal
and vertical mirrors. Skull artwork retains the original shading and is centered
to the nearest source pixel; it is not forced into a mirrored illustration.

All asset PNGs contain real alpha transparency. Checkerboards exist ONLY in
`Preview.png`, never in the individual layer assets. `Composite_Coral.png` is
an additional transparent composite preview, not a replacement for the source.

Rebuild: run `tools/build_middle_boss_ui_layers.py` with Python, Pillow and NumPy.
The script checks symmetry, alignment, non-overlap, alpha, and decoded PNGs.
These final assets were generated deterministically with code, not ImageGen.
