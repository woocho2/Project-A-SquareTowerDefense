# Action UI symbols

Final assets: Assets/4. DotAsset/5. UI/Buttons/

- TowerCreate_Symbol.png: tower + green plus.
- TierUpgrade_Symbol.png: tower + gold upward arrow.
- ColorUpgrade_Symbol.png: red, blue, white and charcoal color cluster.
- Speed_Symbol.png: two identical blue arrows with symmetric motion marks.
- Skip_Symbol.png: mint next arrow and vertical stop bar.

All five are 1024x1024 RGBA with real transparent margins and crisp 8px pixel blocks.
Most artwork is fitted proportionally into the existing skull bounds (344,320)-(672,696),
using its exact artwork center (508,508). Different aspect ratios preserve their
shapes, so visible heights vary. Speed and Skip share a 344px artwork height,
the same center and the same top/bottom edges. Skip retains its original size;
Speed is enlarged proportionally to 520x344px and is wider than the skull bounds.
Same RectTransform size and position as
MiddleBoss_Symbol makes them directly interchangeable. Use white Image.color to
preserve the artwork colors. Keep full canvas; do not trim.

Importer: Sprite Single, Full Rect, Point, no mipmaps, no compression, alpha enabled.
The preview board contains checkerboards and assembled examples ONLY for review;
the five sprite PNGs contain neither checkerboards nor button backgrounds.

Generation: built-in ImageGen with the existing skull as style reference, then
Python/Pillow alpha extraction, palette reduction and exact placement. The speed
symbol was rebuilt mathematically after review: both arrows are copies of one
shape and the full icon is vertically symmetric. No CLI image generation was used.

The generation drafts are in drafts/. To rebuild from these drafts run:
tools/prepare_action_ui_symbols.py with Name=source.png arguments.
validation.json records canvas, positions, bounds and alpha verification.

Shared generation direction: isolated chunky 8-bit pastel UI symbol, thin dark-plum
outline, cream highlights, limited stepped shadows, front view, compact silhouette,
no frame, no text, no currency; solid #FF00FF extraction backdrop excluded from
the artwork (removed in final files).

Per-symbol generation prompts:

TowerCreate: A small cream-colored stone castle turret with three crenellations, a dark arch doorway, soft warm sand shading and restrained pale blue accent. A clear mint-green plus sign overlaps the lower-right edge of the turret, joined to the silhouette. Single turret plus +, instantly means build a tower. Compact nearly square shape.

TierUpgrade: A small cream-colored stone castle turret with three crenellations, a dark arch doorway, soft warm sand shading and restrained pale blue accent, same basic build-tower visual. A substantial gold upward arrow overlaps the right edge of the turret, pointing UP, no stairs, no second tower. Single turret plus upward arrow instantly means upgrade tier. Compact nearly square shape.

ColorUpgrade: Four small faceted color tiles arranged in one compact symmetric diamond-shaped cluster, each a softly beveled square: top pastel coral RED, right pastel sky BLUE, bottom pearl WHITE, left dark charcoal BLACK. Narrow dark-plum outlines, a few crisp highlights, light cream bevels. The four touching tiles form one compact icon that instantly means color upgrade/change. No rainbow spectrum, no green or yellow tile, no paint palette, no brush, no arrow. Distinguishable from currency diamond.

Speed: A compact fast-forward emblem: TWO equal right-pointing filled chevrons/triangular arrowheads side by side, in pastel sky blue with cream highlights and dark-plum outlines; 2 short horizontal blue motion streaks on the left connected visually to the first arrowhead. Clear simple silhouette instantly means run faster. NO lightning, NO clock, NO terminal vertical bar. Chunky pixel-art shading matching the skull.

Skip: A compact skip-forward emblem: ONE right-pointing filled triangular arrowhead followed closely by ONE upright vertical end-stop bar, BOTH pastel mint with cream highlights, equal height, consistent dark-plum outline and chunky pixel-art shadows matching the skull. Perfectly clear >| silhouette. No second arrowhead, no motion streaks, no clock.
