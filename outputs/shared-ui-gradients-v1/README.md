# Shared UI grayscale gradients

Updated project asset: `Assets/4. Asset/5. UI/Shared_UI_Atlas.png`.
The existing 4096×2048 layout and `.meta` are retained, including GUID, sprite names, file IDs, borders and slicing. This task changes only the PNG asset.

| Layer | Top RGB | Bottom RGB | Sprites |
| --- | --- | --- | --- |
| A | 255,255,255 | 230,230,230 | 15 |
| B | 255,255,255 | 200,200,200 | 15 |
| C | 255,255,255 | 245,245,245 | 15 |

Each background sprite has its own smooth downward brightness ramp, uniform across each row. Neutral RGB lets Image Color multiply the existing theme color while retaining the shading. Each exact requested endpoint is reached at the visible top/bottom of the individual sprite. Transparent padding remains untouched. All original alpha values and all Frame-layer pixels remain byte-for-byte unchanged.

Native imagegen edit mode created [the grayscale gradient source](source-gradient.png), using [this prompt](prompt.txt) and [the original A/B/C reference](reference-ABC.png). `Apply-Gradients.ps1` samples and smooths those vertical brightness profiles, enforces downward-only progression and normalizes the RGB endpoints to the exact requested ranges. The generated source's shape/edge artifacts are discarded by applying only grayscale profiles to the original atlas pixels and alpha.

[Preview](preview.png) shows the neutral backgrounds and the same example sky-blue color multiplied into all three layers. [Pixel range report](pixel-ranges.txt) lists all 45 updated sprites. Image pixel checks cover endpoint ranges, alpha preservation, and unchanged frame/padding pixels. Unity verification is left to the user; Unity was not launched.

Previous PNG and metadata: [before](before). Runtime theme code, scenes, prefabs and UI layout were not edited.
