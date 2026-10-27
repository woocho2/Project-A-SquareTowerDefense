# Common tile edge fill

The Single and Sliced sprites of Board_Background, Board_Decoration_Tower and Board_Decoration_Enemy now exclude the transparent outer padding. Background/enemy rects use (14,14,484,484); tower rects use (13,13,486,486), matching the solid frame and excluding faint stray edge pixels. Pixels per unit matches the new width, keeping each sprite's native canvas at 1x1 world units.

Sliced borders are reduced by the trimmed margin, preserving the original centre-pixel region. GUIDs, Sprite IDs, PNG files and scene data remain intact. Six enemy state materials use the equivalent cropped UV region for the common background and enemy frame. EnemyCubeUnlit now respects the background texture's existing scale and offset properties.

Original metadata, materials and shader are under before/. Verification results are in verification.txt. Unity runtime visual inspection has not been performed.
