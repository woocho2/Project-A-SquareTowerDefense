# Valkyrie equipment mask correction

Applied only to `Assets/4. Asset/4. Enemy/Asgard/Enemy_Valkyrie_EquipmentMask.png`.

The user narrowed the task to equipment regions only and will adjust saturation personally. No palette, material, shader, portrait PNG, other enemy artwork or metadata was changed by this correction.

The previous mask selected isolated neutral pixels in polygon windows, leaving highlights and shadows as holes. `Fix-Mask.ps1` replaces that classification with full visible contours for the headband, centre jewel, wing clips, collar, cloak and shoulders. Hair boundaries are traced separately from equipment, and the neck skin and transparent pixels are excluded. The data mask remains opaque binary black/white at 512x512, with its existing GUID and material references.

`mask-comparison.png` shows the same untouched portrait and the same current Mobility material color on both sides, with only the equipment mask changed. The preview reads the material color instead of assigning a new color. `verification.txt` records the mask, protected sample points and unchanged-file checks. Unity was not launched.

`before/` contains the old mask, unchanged portrait and reference material snapshots. The mask generated here supersedes the earlier mask in `../enemy-pastel-style/assets/`. The earlier image generator was not rerun and the character was not redrawn; this correction edits the existing code-native mask authoring contours.
