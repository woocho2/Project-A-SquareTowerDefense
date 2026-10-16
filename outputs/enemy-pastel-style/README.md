# Enemy artwork update

Updated the existing enemy PNGs and state material colors in `Assets/4. Asset/4. Enemy/`, preserving texture GUIDs, sprite metadata, appearance profiles, prefabs, cube mesh and movement code.

- Valkyrie uses the simple final redraw: thin headband, small wing clips and plain shoulder plates. Hair, skin and eyes keep their own colors. The source was edited with the built-in image_gen tool; the complete final prompt is `final-valkyrie-prompt.txt`, and its untouched output is `valkyrie-simple-generated.png`. Earlier generated images in this work folder are discarded studies, not imported assets.
- Brunhild and Loki keep their original silhouettes and appearance. Only pixels selected by their original equipment masks were normalized to soft gray. All unmasked RGBA pixels, including skin, hair, eyes and gold trim, match the originals exactly.
- Equipment uses a neutral gray base, shared decoration uses three neutral paint tones, and the background is near-white. The six common state palettes are rose, yellow, green, sky blue, off-white and lavender.
- `EnemyCubeUnlit.shader` multiplies neutral equipment by the existing state color through the mask, with its color-space handling retained. This allows the grayscale equipment base to receive state colors.
- `Build-Assets.ps1` creates staged assets and CPU composition previews; it does not copy staged PNGs into Assets. The eight final staged PNGs have been copied to the existing asset paths. `preview.png` shows all three enemies and six state colors, as an offline approximation of layer composition.
- `Verify-Assets.ps1` passed after applying the enemy changes: binary masks, protected face/hair regions, exact boss unmasked pixels, exact shared-layer alpha, metadata and material palette consistency. `verification.txt` records those checks. Unity was not launched; shader rendering and animation remain for the user's Unity validation.
- The protected tower hashes in that enemy verification describe the state before the later, separately requested tower-decoration saturation correction. That authorized follow-up changes only `Tower_Decorations_Atlas_v1.png` and is documented in `../tower-decoration-pastel/`.

Original enemy assets and shader are backed up under `before/`. Final assets remain in the existing Common and Asgard folders.
