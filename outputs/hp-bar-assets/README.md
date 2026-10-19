# HP bar sprites

Generated with built-in imagegen; exact prompts are in `prompts.json` and original outputs are in `source-Background.png` and `source-Fill.png`.

Production assets:

- `Assets/4. Asset/5. UI/HPBar_Background.png`: cream rim and muted beige empty track.
- `Assets/4. Asset/5. UI/HPBar_Fill.png`: neutral white/light-gray fill, suitable for runtime tinting with green/yellow/red or any custom color.

Both are 256 x 32 transparent PNGs with centered pivots, PPU 100, Sprite Single, bilinear filtering, no mipmaps or compression. Each has sprite borders (14 left/right, 8 top/bottom) for optional slicing. The fill is inset on the same canvas so the two images can share a RectTransform size.

Suggested Image settings: background `Sliced`; fill `Filled`, `Horizontal`, origin `Left`; use `fillAmount` for remaining HP. The existing `HPBarSlider` applies runtime colors, so a neutral fill avoids multiplying one hue by another.

`Pack.ps1` performs alpha-bound trimming, gutter fragment cleanup, size normalization, and metadata creation. Dimensions, transparent margins and neutral fill color are checked and recorded in `verification.json`.

No existing assets, scene objects, prefabs or scripts were changed. Unity was not launched.
