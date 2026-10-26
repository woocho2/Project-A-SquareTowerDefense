# New Valkyrie character, matching the existing game art

Applied `Valkyrie_Character.png` and `Valkyrie_EquipmentMask.png` to `Assets/4. Asset/4. Enemy/Asgard/`. Importer metadata, GUIDs and appearance references are preserved. `before/` contains the previously applied white-haired character and its settings.

The new character has pale golden hair with one braid, blue eyes, and a compact winged helmet. The project's Brunhild portrait and sword tower atlas were used as style and proportion references, rather than reusing the old Valkyrie identity. Generated with the built-in image_gen tool. The final prompt is `prompt-style-matched.txt`; `prompt.txt` records the earlier, superseded design study, which was not applied.

Final generated source: `C:/Users/user/.codex/generated_images/01a175ac-4aba-7072-926c-7ba5fcb44831/exec-20ec5936-29b7-401a-8028-12c190a4b7fd.png`.

`Package.ps1` downsamples the generated art to 512x512 and derives a matching equipment mask, excluding the face and colored hair. It closes small gaps in the mask caused by textured metal shading. `comparison.png` shows the previous design, the new character, and an equipment tint preview. `verification.txt` records dimensions, visible bounds and mask coverage. Applied PNGs were verified against the staged copies by hash; settings and appearance references were verified against the backup. Unity runtime verification was not performed.
