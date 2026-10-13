# Sword / Axe portrait layers

All portrait cells remain 256×256, PPU 192, center pivot (0.5, 0.5), Full Rect mesh. Matching sprites must share the same transform position and scale.

Render order from back to front:

| Order | Asset | Purpose |
| --- | --- | --- |
| 0 | `Tower_Frames_Atlas_v1.png` (existing filled frame) | White interior background |
| 1 | `Tower_Sword_FaceHair_Atlas_v1.png` or `Tower_Axe_FaceHair_Atlas_v1.png` | Face, eyes, mouth, hair, beard, braids |
| 2 | `Tower_Frame_Rims_Atlas_v1.png` | Matching tier/synergy rim, transparent center |
| 3 | `Tower_Sword_HeadgearDecor_Atlas_v1.png` or `Tower_Axe_HeadgearDecor_Atlas_v1.png` | Helmet, hat, hood, metal guards, ornaments, effects, horn |

SortingOrder 0/1/2/3 is an example; relative order matters. Use separate SpriteRenderers or separate UI Images. A filled frame belongs behind the face; only the transparent-center rim goes above it.

Both weapon atlases use the same 2×2 order as the current combined portraits:

- Sword: Hero, Swordsman / Paladin, Death Knight.
- Axe: Viking, Berserker / Skald, Executioner.

The Berserker's brown bear pelt is headgear; the black human hair is in FaceHair. Viking's orange beard and Skald's blonde braids are in FaceHair. The Skald horn and braid jewelry, Executioner hood/mouth wrap, detached petals, sparks and wisps are in HeadgearDecor.

Native imagegen generated green/magenta semantic masks using the current atlases. `Split-Layers.ps1` maps these masks onto the original image pixels and corrects face features; it does not redraw, resize or recolor the character. Every visible original RGBA pixel belongs to exactly one of the raw layers, verified during packing.

The exported FaceHair layers are additionally clipped to the shared frame opening so hair/beard tips stay behind the rim. Unclipped raw layers are preserved here as `Tower_*_FaceHair_Unclipped_Atlas_v1.png`. Areas originally hidden by headgear retain their original transparency.

`preview.png` shows the actual four-layer render order. `layers-preview.png` shows rear layers on the left, front layers on the right. Prompts and masks are archived here. `Save-Assets.ps1` saves five new PNG assets and their import metadata to `Assets/4. Asset/2. Tower/CubeConcepts/` and preserves their GUIDs on reruns.

The later Sword/Axe hookup uses these layers in `TowerVisual.cs` and `TowerBase.prefab`. Both combined character atlases and earlier unused prototypes were removed from Assets after reference checks; verified backups are in `outputs/cube-concepts-cleanup-v1/removed/`. The original import templates remain in this output folder's `before/` directory. No Unity execution or Unity validation was performed.
