# Axe tower face concepts

Four preview concepts based on `Assets/27. 기획 통합.txt`: chapter 4's tower concept table and chapter 4-4's axe skills.

| Class | Character | Visual concept | Skill cue |
| --- | --- | --- | --- |
| Red splash | Viking | Compact iron helmet, ginger beard/braids, single axe crest | Heavy downward area-impact accents; Guillotine |
| Blue target | Berserker | Bear-pelt hood, blue band/war paint, crossed-axe crest, fierce expression | Splitting/fury accents; guaranteed critical state |
| White buff | Skald | Ivory fur cap, blond braids, face-level war horn | Sound waves / battle chant; allied critical damage and action/mastery support |
| Black debuff | Executioner | Black cloth hood and mouth wrap, living human face | Sentence/execution marks; instant-kill threshold |

All are face-only SD cartoon humans with soft squared cheeks and three-tone cel shading, following the existing sword tower art. Tier identity remains on the separate reusable frame rather than requiring five differently painted faces. No large floating axe images or shields.

Built-in imagegen, using the current sword character atlas as a style reference. [Prompt set](prompts), [four full-resolution transparent source PNGs](source), [framed and separate-layer preview](preview.png). `Build-Preview.ps1` fits the generated portraits for a 256px tile-size comparison and places the existing bronze frame behind them.

The Skald was corrected to a straight-on horn-playing pose: centered mouthpiece touching the lips, a compact circular bell facing the viewer at the lower center, and small sound-wave cues beside it. Head and eyes stay front-facing with the white cap and blond braids retained. [Front-horn edit prompt](prompts/Skald-front-horn.txt); current source is `source/Skald.png`. The earlier side horn is archived in `history/Skald-side-horn-v1.png`. The other three character PNGs retain their original bytes.

These are review concepts saved outside Assets. No Unity execution, gameplay code changes, runtime binding, prefab changes or replacement of existing art was performed.
