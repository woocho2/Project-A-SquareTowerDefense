# Shared decoration: shading-only correction

## Latest correction: matched original curves

The final applied asset is now `Board_Decoration_Matched.png`, built by `Match-OriginalCurves.ps1`. The earlier shading-only filter left the original crooked alpha contour in place. After the user asked to address that too, the original corner silhouette was measured and fitted with smooth cubic curves, with the shading contours following those curves. The rejected enlarged elbow was 86px; this fit follows the original elbow at approximately 72px, with the original light corner face near 59px.

Canvas, 484x484 centered outer bounds, original 20px straight-edge thickness, grayscale 150..255, GUID, PPU and palette values are retained. The curve cleanup changes boundary alpha where necessary, rather than retaining the old distorted contour. Four quadrants share the same raster geometry. Opaque area changes from 65,512 to 65,104 pixels and the measured top-left corner area from 12,538 to 12,436, both less than 1% smaller. The corners were not enlarged. `matched-verification.txt` records these checks; `matched-comparison.png` shows the actual selected before/after. `before-curve-fit.png` preserves the immediately preceding applied asset. The common image rebuild helper now rebuilds this final version.

## Earlier work (superseded)

Final asset: `Assets/4. Asset/0. Common/Board_Decoration.png`.

The user rejected the thicker redesigned frame. Neither that generated study (`generated-source.png`, built-in image_gen edit, full `prompt.txt`) nor the traced curve study (`Build-CleanFrame.ps1`, `Board_Decoration.png`, `comparison.png`) was applied to Assets. They are discarded studies.

The earlier applied result was `Board_Decoration_Refined.png`, built by `Refine-Shading.ps1` from the exact existing frame saved as `before.png`. It changed only grayscale shading:

- Preserve the original width, height, all outer and inner contours, corner shapes, and every alpha byte.
- Remove separate bright specks and broken highlight fragments while retaining the four large corner faces. Remove tiny dark shading specks.
- Make the straight-edge paint bands consistent using the existing top-edge profile.
- Apply a small color-only median/Gaussian refinement to soften stepped shadow transitions. Alpha is never blurred or regenerated.
- Maintain visible grayscale minimum 150 and maximum 255. Background remains 200..255; user palette values, sprite metadata and materials are unchanged.

`shading-only-comparison.png` and `verification.txt` record that superseded shading-only pass. Unity was not launched for either pass.
