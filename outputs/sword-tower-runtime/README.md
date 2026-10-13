# Sword character runtime integration

`TowerVisual` selects the four character sprites using the hundreds digit and the five separate frame sprites using the thousands digit for weapon ID 01. `TowerBase.prefab` contains the actual references to the character and frame atlases. The existing `TowerController.Init(TowerData)` path applies these visuals on spawning, tier upgrades and tower replacement.

- 1101–5101: Hero; 1201–5201: Swordsman; 1301–5301: Paladin; 1401–5401: Death knight, in 1000-ID tier increments.
- Both layers are white-tinted and use the same 256/192 world-unit canvas. Frames align with the character and render behind it. The old white emblem backing is hidden for sword characters.
- Other weapons retain their legacy visuals. Returning to a legacy visual restores the original tier transform and sorting order. Missing character/frame artwork also falls back to the existing emblem visual.
- Character textures are separate from projectile artwork. Dedicated projectile prefab visuals keep their existing path; legacy projectiles still read the original emblem sprite and attack-class color, never the character face.

Validation: full project runtime C# compilation using the current Unity response file; isolated native Unity visual checks for all 20 IDs through integer and real TowerData APIs, actual serialized prefab sprite bindings, visual ID override, white tint, world bounds, frame sorting/alignment, non-sword/missing-art fallback, and synergy-to-sword material/mask restoration. The visual harness uses the real TowerVisual, TowerData and SynergyCharacterCatalog; adapters supply unrelated attack/stat dependencies. Full stage gameplay was not automated. See [validation.log](validation.log).

`SwordVisualCheck.cs` and `VisualCheckAdapters.cs` belong only to this validation output folder and are not game scripts. Compiled assemblies are validation artifacts outside Assets.
