Asgard rounded arrow atlas
Latest color refinement: eight active gold arrows were softened to match the existing Asgard path corner ornaments. Shapes, alpha, inactive arrows, labels and metadata remain unchanged. See output/asgard-arrow-gold-match/Changes_and_Prompt.txt.
Built-in image_gen edit of the approved source; extended to 1024x768 PNG with themed text badges.
Twelve 256x256 transparent cells in 4 columns x 3 rows.
Top row: N NE E SE. Middle row: S SW W NW. Bottom row: START END JUMP RETURN.
Every cell: exactly eight arrows, one gold active arrow at the named direction, seven navy inactive arrows, transparent center (connected-components validation passed for all eight).
Unity metadata: Multiple sprites, 12 slices, centered pivot, PPU 256 matching the existing overlay, bilinear, no texture compression. Existing eight sprite IDs and atlas GUID retained; four label sprites added.
New asset: Assets/4. Asset/1. BackGround/Tile_Path_Overlay_Asgard_Atlas.png
Existing overlay Tile assets (12), Stage1 cached sprite references and Tiles.prefab palette references now use the new atlas. 14 files were checked against backups: only sprite GUID/fileID mappings changed. No code changed. The old shared PNG is retained, but no asset/prefab/scene/script references to it remain.
The composite preview is static artwork, not a Unity play-test screenshot.

Generation prompt:
Use case: compositing
Asset type: transparent Asgard eight-direction arrow overlay sprite atlas, 4 columns by 2 rows.
Input image 1 is the approved source artwork. Reuse its exact eight outward rounded triangular arrowheads, dark navy inactive arrows and warm gold active arrow, restrained beveled cartoon style, original arrow size and positions. This is production atlas expansion, not a redesign.
Create EIGHT identical-sized square transparent cells in a strict regular 4-by-2 edge-to-edge grid, no gutters. Every cell has EXACTLY eight outward-pointing arrowheads arranged at the same positions as the source, with a completely empty center. Exactly ONE arrow per cell is gold; other SEVEN are deep navy. Keep the silhouette, sizes and coordinate positions identical across all cells. Change ONLY which arrow is colored gold, not geometry or positions. Match gold/navy highlights to the approved source, no extra saturation.
Mandatory order (left to right): TOP ROW N, NE, E, SE. BOTTOM ROW S, SW, W, NW. Thus gold arrow locations by cell are: top-left at12o'clock; top-second at1:30; top-third at3; top-fourth at4:30; bottom-left at6; bottom-second at7:30; bottom-third at9; bottom-fourth at10:30. Exactly 64 total triangles and exactly 8 gold triangles in the atlas. All others dark navy.
Arrow center coordinates within each cell approximately normalized: N(.50,.12), NE(.79,.21), E(.90,.50), SE(.79,.79), S(.50,.88), SW(.21,.79), W(.10,.50), NW(.21,.21). Arrows point outward. Maintain the source scale of each triangle. Genuine alpha transparency outside ALL triangles, inside every cell and in all centers; no backdrop.
Avoid: tile frames, tile faces, gold compass rings, white/red arrows, extra triangles, single large central arrows, text, names, cell labels, drawn grid lines, drop shadows, checkerboard, shifting arrow placement, mismatched cell scale.
