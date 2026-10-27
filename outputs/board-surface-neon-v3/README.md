# White board surface and separate neon grid

BoardFrameWhiteNeon.mat now controls the board surface only: centre grayscale 255, outer-edge grayscale 180 and a 12-source-pixel edge transition derived from the plate's alpha silhouette. The old texture RGB and grid tint are not baked into the surface. Display grayscale is converted correctly for linear rendering.

BoardGridGlow.mat and BoardGridGlow.shader provide a separate HDR-emissive grid with procedural core/halo, gentle brightness pulsing and travelling light. Grid spacing and origin are derived from the authored tilemap. Core/halo widths use screen pixels. The halo is drawn by the shader, so the grid remains visible without requiring a scene-wide bloom change.

BoardGridGlow on Stage1's BoardTile creates a visual-only tilemap child, one sorting order above the plate and below gameplay tiles. It follows source tile edits, clears itself on disable, and uses non-saving flags for editor/build preview objects. The source tilemaps, cell layout and occupancy data are never replaced. Disable the component to view the plain grayscale plate.

Adjust grayscale in BoardFrameWhiteNeon.mat; adjust emission, colour, halo and animation in BoardGridGlow.mat. Source PNGs and common tile/frame images are unchanged. verification.txt records compilation, wiring and coordinate checks. Unity runtime rendering has not been exercised.
