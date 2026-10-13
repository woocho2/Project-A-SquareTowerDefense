# CubeConcepts cleanup

Removed 12 unreferenced PNG assets and their 12 `.meta` files from `Assets/4. Asset/2. Tower/CubeConcepts/`. Their exact verified copies are in `removed/`; `cleanup-manifest.json` lists every removed and retained file.

Retained six active textures:

- White-filled frame atlas.
- Sword FaceHair and HeadgearDecor atlases.
- Axe FaceHair and HeadgearDecor atlases.
- Transparent-center frame rim atlas.

All six remaining atlases are now referenced by `TowerBase.prefab`. Following the user's request to use Axe towers, `TowerVisual.cs` and `TowerBase.prefab` were updated to use both Sword and Axe layer assets. The combined Sword atlas was then removed after its last reference was replaced. GUID references were scanned in Assets, Packages, and ProjectSettings before removal and rechecked after removal. None of the removed GUIDs remain referenced. No Unity execution was performed.

Latest layer export import templates were copied outside Assets into `outputs/tower-character-layers-v1/before/`, so rebuilding the layer assets does not require the deleted combined Axe atlas.
