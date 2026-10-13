# Thicker tier frames and subtle character overlap

Current project assets, updated in place:

- `Assets/4. Asset/2. Tower/CubeConcepts/Tower_Frames_Atlas_v1.png`: 768×512; five thicker tier rims plus white/gold synergy frame in the same outer shape/window.
- `Assets/4. Asset/2. Tower/CubeConcepts/Tower_Sword_Characters_Atlas_v2.png`: 512×512; character silhouettes overlap the front of the thicker rim, with a small satgat-brim overhang and a smaller, softer paladin helmet silhouette.

Each atlas keeps its existing 256×256 cells, layout, GUIDs, file IDs and runtime prefab references. The PNG images are replaced; their `.meta` files are retained. Previous images and metadata are archived in [before](before).

Native imagegen edit mode; [prompt set](prompts). Initial edits: `Frames.txt` and `Characters.txt`. Final targeted corrections: `Paladin-correction.txt` and `Synergy-correction.txt`.
`Pack-Assets.ps1` packs generated art into the existing layouts. It uses the existing plain-frame alpha to discard unintended generated backdrops and make all six frame footprints/windows match. Generated character images retain transparency and have a small separation margin between atlas cells. Paladin is fitted separately to avoid the oversized rectangular helmet impression.

[Character before/after preview](preview-before-after.png) and [six frame preview](preview-frames.png). Previews use the existing Asgard spawn tile underneath the separate frame and character layers.

This task edits assets only. Per the user's request, Unity execution and verification are left to the user. No gameplay code or prefab edits are part of this refinement.
