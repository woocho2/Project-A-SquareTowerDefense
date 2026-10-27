# Keyboard-style board neon patterns

BoardGridGlow cycles eleven brightness animations: left/right, down/up, down-right/up-left, up-right/down-left, circular expansion, circular contraction and curved interfering waves. Geometry, the white board surface, grid colour and authored tile data are retained.

Every set of eleven is shuffled using a private random generator. Each set includes every pattern once and the first pattern of a new set cannot repeat the last of the preceding set. Default dwell time is six seconds with a one-second smooth crossfade. The shader's directional waves are sinusoidal and continuous across grid intersections. Radial waves use world distance from the actual occupied-board centre, so a rectangular board does not turn the circles into ellipses.

Adjust pattern duration and transition duration on the BoardTile object's BoardGridGlow component. Adjust Pattern Cycles Per Second, Pattern Wave Spacing, Dim Brightness, Emission and Neon Colour in BoardGridGlow.mat. The component passes unscaled animation time and pattern state through a property block; tilemap geometry is copied only when a source tile edit or validation requests it, with no new per-frame arrays or particle objects.

C# and HLSL compilation passed. Actual Unity playback has not been exercised.
