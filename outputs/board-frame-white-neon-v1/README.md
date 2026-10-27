# White board with neon grid

The board plate used by `Assets/4. Asset/1. BackGround/Tiles/BoardFrame` now uses a continuous white surface, neon grid lines and a matching thin outer rim. Asgard uses cyan; the other nine realm variants retain distinct accent hues on the same white surface.

`Assets/4. Asset/1. BackGround/Tile_BoardFrame_WhiteNeon_Atlas_v1.png` is the production atlas. It preserves all 90 sprite IDs, their 192px rectangles, 128 PPU and the original 2880x1152 atlas layout. The 19 existing Tile/RuleTile assets and Stage1's cached sprite references point to the new atlas. The existing atlas remains available.

`tools/BoardFrameNeonGenerator.cs` is the native artwork source. It draws the nine-slice-compatible plate and assembles `board-preview.png` from the real sprite rectangles. This preview shows the board plate without gameplay tiles; it is not a Unity screenshot.

`before/` contains the original reference files, `changed-references.json` lists their edits, and `verification.txt` records the reference, geometry and coordinate checks. Tile coordinates, neighbour rules and gameplay data are unchanged.

Rebuild using the installed .NET Framework C# compiler with `System.Drawing.dll`, then run the resulting executable with two arguments: the production PNG path and the preview PNG path.
