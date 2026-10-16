# Tower decoration and character atlas size correction

- Spawn decorations and all six tower decorations use the same centred 242x242 alpha bounds inside 256x256 cells.
- Regular tower atlas artwork is physically resized to 95% inside each existing cell: 14 PNG atlases, 56 portraits.
- All atlas dimensions, sprite rectangles, pivots, GUIDs, file IDs and catalog references are preserved.
- TowerVisual no longer applies the old 0.95 shrink factor. Character / Background / Decoration prefab scales are all 1.0, and both serialized world canvas sizes are 256/192.
- Backgrounds, decorations and the synergy portrait atlas were not repainted or resized.
- Original regular PNGs and metadata are in character-atlas-before. Resize-CharacterAtlases.ps1 always reads those backups to prevent repeated shrinking.
- Pixel bounds, alignment, empty margins, metadata hashes and protected artwork hashes passed verification. Editor and player code compile checks passed. Unity was not launched.