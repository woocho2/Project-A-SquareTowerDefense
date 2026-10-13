# Sword splash tower: face and tier helmets

Project asset: `Assets/4. Asset/2. Tower/CubeConcepts/Tower_Sword_Splash_Tiers_Layer_Atlas_v1.png`.

One 768×512 atlas contains six 256×256 sprites, PPU 192, centered pivots and Full Rect mesh. The complete layered head matches the existing spawn-sized rounded-square face. Use two SpriteRenderers at the same position/scale, both white: common face below, selected tier helmet above.

| Slot | Sprite | File ID |
| --- | --- | --- |
| Top-left | Tower_Sword_Splash_Face_Common | 31001 |
| Top-middle | Tower_Sword_Splash_Helmet_Bronze | 31011 |
| Top-right | Tower_Sword_Splash_Helmet_Silver | 31012 |
| Bottom-left | Tower_Sword_Splash_Helmet_Gold | 31013 |
| Bottom-middle | Tower_Sword_Splash_Helmet_Mithril | 31014 |
| Bottom-right | Tower_Sword_Splash_Helmet_Diamond | 31015 |

The common face, eyes, mouth and bronze helmet retain the approved original pixels. Splitting and reassembling bronze reconstructs the original exactly. The equipment layers share the same alpha window and outer silhouette. Original crimson splash accents, sword emblem and dark recess pixels remain constant; only metal artwork varies by tier.

The final tiers use the same plain knight design and differ by metal colors. Added corner engravings, runes and gemstones were removed at the user's request. Silver is cool gray; gold is warm yellow; mithril is blue-green; diamond is substantially brighter, predominantly ice white with very pale blue shadows. Shared red splash panels, original rivets and sword emblem remain. Final built-in imagegen edit prompt: `prompt-color-only.txt`. Earlier per-tier prompts are work history.

`Prepare-Layers.ps1` splits original pixels into complementary layers. `Crop-PlainSources.ps1` extracts the four color references from the edited sheet. `Build-Atlas.ps1` fits generated metal artwork to the original equipment mask, preserves common details, copies RGBA directly into the atlas, and makes `preview.png`. Original equipment masks provide true transparency for the final asset. Work layers/previews remain outside Assets. Runtime TowerVisual/prefab bindings have not been modified.

Validation checks source reconstruction, common-face preservation, tier alpha equality and constant non-metal details. `TierAtlasCheck.cs` additionally checks real Unity imports, serialized sprite IDs, bounds/pivots, PPU and alpha composition; its log is `validation.log`.
