# HitEffect prefab migration

Modified prefabs:

| Id | Prefab | HitEffectScale |
| --- | --- | --- |
| 101 | Assets/4. Asset/6. Effect/Sword_Splash_30_v3.prefab | 4.5 |
| 102 | Assets/4. Asset/6. Effect/Hit_Axe_Splash_30_v4.prefab | 4.5 |
| 103 | Assets/4. Asset/6. Effect/Hit_Bow_Splash_30_v4.prefab | 4.5 |
| 104 | Assets/4. Asset/6. Effect/Hit_Spear_Splash_30_v4.prefab | 1.5 |
| 105 | Assets/4. Asset/6. Effect/Hit_Shield_Splash_30_v4.prefab | 4.5 |
| 106 | Assets/4. Asset/6. Effect/Hit_Hammer_Splash_30_v4.prefab | 4.5 |
| 107 | Assets/4. Asset/6. Effect/Hit_Fire_Splash_30_v4.prefab | 4.5 |
| 108 | Assets/4. Asset/6. Effect/Hit_Water_Splash_30_v4.prefab | 4.5 |
| 109 | Assets/4. Asset/6. Effect/Hit_Earth_Splash_30_v4.prefab | 4.5 |
| 110 | Assets/4. Asset/6. Effect/Hit_Wind_Splash_30_v4.prefab | 4.5 |
| 111 | Assets/4. Asset/6. Effect/Hit_Ice_Splash_30_v4.prefab | 4.5 |
| 112 | Assets/4. Asset/6. Effect/Hit_Electric_Splash_30_v4.prefab | 4.5 |
| 113 | Assets/4. Asset/6. Effect/Hit_Light_Splash_30_v4.prefab | 4.5 |
| 114 | Assets/4. Asset/6. Effect/Hit_Dark_Splash_30_v4.prefab | 4.5 |
| 201 | Assets/4. Asset/6. Effect/Sword_Target_30_v3.prefab | 4.5 |
| 202 | Assets/4. Asset/6. Effect/Hit_Axe_Target_30_v4.prefab | 2.25 |
| 203 | Assets/4. Asset/6. Effect/Hit_Bow_Target_30_v4.prefab | 4.5 |
| 204 | Assets/4. Asset/6. Effect/Hit_Spear_Target_30_v4.prefab | 1.5 |
| 205 | Assets/4. Asset/6. Effect/Hit_Shield_Target_30_v4.prefab | 4.5 |
| 206 | Assets/4. Asset/6. Effect/Hit_Hammer_Target_30_v4.prefab | 4.5 |
| 207 | Assets/4. Asset/6. Effect/Hit_Fire_Target_30_v4.prefab | 4.5 |
| 208 | Assets/4. Asset/6. Effect/Hit_Water_Target_30_v4.prefab | 4.5 |
| 209 | Assets/4. Asset/6. Effect/Hit_Earth_Target_30_v4.prefab | 4.5 |
| 210 | Assets/4. Asset/6. Effect/Hit_Wind_Target_30_v4.prefab | 4.5 |
| 211 | Assets/4. Asset/6. Effect/Hit_Ice_Target_30_v4.prefab | 4.5 |
| 212 | Assets/4. Asset/6. Effect/Hit_Electric_Target_30_v4.prefab | 4.5 |
| 213 | Assets/4. Asset/6. Effect/Hit_Light_Target_30_v4.prefab | 4.5 |
| 214 | Assets/4. Asset/6. Effect/Hit_Dark_Target_30_v4.prefab | 4.5 |

Also modified: 
Assets/2. Prefab/4. Effect/ProjectileCatalog.asset

No target prefab is shared with an effect ID outside 101-114 and 201-214.
Old EffectLibrary, other prefabs, animation clips/events and all existing meta files are preserved.
Catalog HitEffect values reference the new HitEffect component fileIDs, not GameObject fileIDs.
Backups: before/. Machine-readable verification: verification.json.
