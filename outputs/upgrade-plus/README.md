# Upgrade plus button

Generated with built-in imagegen, referencing the small gold plus buttons in the color-upgrade dock concept. Final prompt: `prompt.txt`. Untouched generation: `source.png`.

Production asset: `Assets/4. Asset/5. UI/UI_Upgrade_Plus.png` and importer `.meta`.
512 x 512, centered pivot, 32px transparent margins, PPU 100, Sprite Single, bilinear filtering, no mipmaps or compression.

`Pack.ps1` uses the color-upgrade atlas's packing helper to normalize the source size and omit tiny disconnected gutter fragments. It refuses to overwrite an existing asset. File verification is recorded in `verification.json`.

Existing assets, UI assignments, scripts, prefabs and scenes remain unchanged. Unity verification is left to the user.
