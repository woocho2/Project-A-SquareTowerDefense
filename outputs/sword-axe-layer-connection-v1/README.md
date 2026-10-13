# Sword / Axe layer hookup

`TowerVisual.cs` routes weapon ID 01 (Sword) and 02 (Axe), tiers 1..5 and attack classes 1..4, through layered portraits. Axe's class order is Viking, Berserker, Skald, Executioner. Both `Apply(int)` and `Apply(TowerData)` use this path. Dedicated projectile artwork and emblem fallback continue using weapon identity.

`TowerBase.prefab` binds 26 sprites: 4 Sword faces, 4 Sword headgear, 4 Axe faces, 4 Axe headgear, 5 filled frames and 5 rims. Two additional children, FrameRim and Headgear, are initially disabled and enabled only for layered characters.

Layer order relative to the face renderer is filled background -1, face/hair 0, rim +1, headgear/decor +2. Every layer uses the same position, rotation, sorting layer and common canvas size. At the user's request the Sword/Axe canvas is reduced by 5%, from 1.3333334 to 1.2666667 world units (95% of 256px/192 PPU). Other emblems and synergy portraits disable/clear both added overlays. Returning to a legacy emblem restores the original tier transform, material and sorting order.

Validation:

- Compiled the current full Assembly-CSharp source set with Unity's Roslyn compiler without launching Unity: exit code 0, no diagnostics (`compile.log`).
- Verified all 26 sprite GUID/fileID bindings against import metadata.
- Verified 29 unique prefab documents and all local prefab references.
- Removed the now-unreferenced combined Sword atlas; all six remaining CubeConcepts atlases are bound by TowerBase.

Unity runtime and visual verification is left to the user as requested.
