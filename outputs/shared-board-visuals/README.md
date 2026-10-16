# Shared board visuals

Tile, tower and cube-enemy backgrounds and decorations now share the two existing enemy images. The common files live in `Assets/4. Asset/0. Common/`:

- `Board_Background.png`: 512x512, one full-rect sprite, 384 PPU.
- `Board_Decoration.png`: 512x512, one full-rect sprite, 384 PPU.
- `Board_Arrow.png`: 256x256, one full-rect sprite, 192 PPU. Extracted from the existing right-facing path arrow and neutralized; all directions use rotation of this single sprite.

No character art, equipment mask, tower root size, grid spacing, enemy movement or cube orientation was changed. Background and decoration have identical centred 484x484 square outer bounds. Both fill the same 512/384 world-space canvas; this also matches the existing 256/192 tower canvas. The corrected Valkyrie equipment mask remains byte-identical.

## Editing colors in Unity

Open **Tools > Visuals > 공용 배경·장식 색상**, or select `Assets/Resources/SharedBoardVisuals.asset` directly.

- Stage tab: ten realm entries, separate spawn-tile background/decoration, path-tile background/decoration and arrow color. `TileManager > Stage Realm` selects the realm for that scene. `SetStageRealm` supports switching it from game code.
- Tower tab: six tier defaults, including ruby synergy. Optional exact tower-ID overrides take priority; runtime data IDs remain separate from portrait/visual aliases.
- Enemy tab: six face-state defaults, with independent background, decoration and equipment colors. Optional appearance-specific state entries override only those states. Empty character override lists inherit the common state colors. Existing current material colors were copied into the initial data, with equal character entries deduplicated.
- Common-image tab: sprite and Tile references. These are already connected.

Edits in the color window are saved to the asset and queued for scene preview. The Inspector also provides an Apply button, and `TileManager` has buttons to open the window and rebuild the visuals. Palette Undo/Redo refreshes the preview. In play mode, palette notifications update the tilemaps, active tower layers and indexed cube-face property blocks. Newly spawned and pooled objects read the same palette. Per-face blocks carry both character textures and colors because Unity does not merge renderer-wide blocks with indexed blocks.

## Path display

The existing Path Direction tilemap now contains the common decoration. The new Path Arrows tilemap contains only the common arrow, rotated toward the next authored path coordinate. The last visit has no arrow. START, END, JUMP and RETURN artwork is no longer referenced by the active board. Repeated cell visits reuse an arrow and update its direction as their pulse becomes active. Decoration and arrow pulse together with their own base colors; the original pulse timing and combat-strength fields remain intact.

The original authored path coordinates, occupied cells, buffs, tile occupancy and circular enemy movement rules are preserved. Changing a visual endpoint does not change how enemies move around the loop.

## Neutral image values

Before color tinting, visible background pixels use neutral grayscale 200..255, with 255 at the top and 200 at the bottom. The background minimum was raised from 150 at the user's request. Decoration retains its original three paint levels remapped to 150..255. Interior pixels are opaque, exterior pixels remain transparent, and original intermediate alpha is kept only within the one-pixel silhouette boundary. This removed the old decoration's widespread interior translucency. The nonzero-alpha footprint is unchanged. Use `Build-CommonImages.ps1 -Kind Background` to rebuild only the background without touching decoration or palette data.

Decoration was subsequently refined using the measured original contour and thickness. Its old jagged alpha edges and shadow bands were replaced with matched smooth curves; the canvas, centered outer bounds and original 20px straight-edge thickness remain the same. Corner/frame area decreases by less than 1%, with no enlargement. See `../shared-decoration-cleanup/README.md` and `matched-comparison.png`. This later decoration correction supersedes the original per-pixel-alpha-preservation statement above; background alpha remains unchanged. The common rebuild helper includes this correction.

`Build-CommonImages.ps1` rebuilds these two images from the preserved originals without rerunning the reference migration. `Preview.ps1` composes the current palette into `preview.png` for offline inspection; it is not a Unity-rendered screenshot.

## Cleanup and validation

Thirteen replaced color-specific textures and twelve duplicate boss-state materials were moved outside Assets under `retired/`. Originals are also preserved under `before/`, including changed scenes, prefabs and brush assets. The active graph uses six shared enemy state materials for their badge UVs and obtains character textures and colors from property blocks. Board floor/frame artwork and UI assets remain separate.

The one-time `Migrate.ps1` refuses to run again after `manifest.json` exists. It records the migrated GUIDs and original sampled colors. User renames made during the task were retained; reference validation resolves assets by GUID.

- `Compile.ps1`: installed Unity assembly references, runtime with editor defines, modified editor tools, and player defines; all compiled with zero warnings/errors.
- `LookupChecks.csproj`: 14 checks against the production palette lookup code with data-only Unity stand-ins, covering defaults, exact-ID and appearance overrides, partial fallback, independent colors and immediate edits.
- `Verify.ps1` / `verification.txt`: common RGB/alpha and square bounds, config references, retired-reference removal, unchanged path/cell layouts and tower scales, all 33 saved arrow rotations, enemy palette wiring and unchanged Valkyrie mask.

The replaced texture set is 41.00 MiB versus 2.25 MiB for the three common images when expressed as RGBA32 pixel payload. This is an estimate of the complete replaced set, not measured GPU memory or a claim that all previous themes were simultaneously loaded. Compression, mipmaps and platform settings affect actual memory.

Unity was not launched or used for import, shader, scene or play validation. Existing `_Recovery` snapshots were left unchanged and excluded from active-reference migration. Reopen the stage scene in Unity to inspect the migrated serialized layout if it was already open during the file edits.
