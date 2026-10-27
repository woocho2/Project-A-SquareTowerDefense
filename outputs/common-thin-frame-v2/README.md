# Existing frame without corner ornaments

Board_Decoration_Thin now uses the existing Board_Decoration_Enemy frame's measured 20px straight-edge thickness, its original grayscale paint profile and 52px outer corner radius. The large corner ornaments are removed, leaving a uniform rounded frame with a transparent centre.

The 512px source retains the same 14px source inset as the existing frame. Both imported Sprite rectangles crop that inset to 484x484 at 484 PPU, so the visible frame fills its 1x1 canvas and matches the existing frame's display scale. Existing new-variant GUIDs and Sprite IDs remain stable; Board_Thin_Frame continues to reference it.

The previous 4px interpretation is saved here for comparison and superseded. tools/CommonThinFrameGenerator.cs measures the reference frame and rebuilds the corrected variant. verification.txt records the thickness, exact straight-edge colour/alpha match and ornament-removal checks.
