# Bronze sword splash tower face

Selected direction: face only, with the user's preferred three-tone bronze helmet. Armor/body and two-tone variants were rejected. The armor concept was removed from Assets; no gameplay or prefab changes were made.

Final sprite: `Assets/4. Asset/2. Tower/CubeConcepts/Tower_Bronze_Sword_Splash_Front_v3.png`.

The artwork was edited with built-in imagegen using `prompt-v3.txt`. Technical texture fitting in `Pack-TileConcept.ps1` crops/resamples the generated artwork and applies the actual spawn tile alpha outline. Final size is 256×256; PPU is 192, matching `Tile_Floor_Atlas` tower spawn sprites. Checked all 65,536 alpha values against `spawn-tile-reference.png`: identical. Pivot remains centered and sprite mesh is Full Rect.

The rejected armor prompt is retained only as work history outside Assets. Earlier v1/v2 face concepts remain available for comparison.
