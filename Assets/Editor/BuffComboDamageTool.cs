using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SquareTD
{
    // 다이아 공격 타워 28종 × 버프 3개 조합 364가지 × 차지 16가지의 한 발·스킬 피해를 게임 코드로 계산해 Balance 폴더에 남김
    public static class BuffComboDamageTool
    {
        private const float DummyHp = 10000000000f;
        private const int Tier = 5;
        private const int PathLength = 12;
        private const int DotTurns = 40;
        private const string SkillErrorPrefix = "스킬 ";

        private static readonly CultureInfo s_inv = CultureInfo.InvariantCulture;

        private sealed class DamageProbe : StatusEffect
        {
            public double Total;
            public DamageProbe() : base(1) { }
            public override string Key => "피해 측정";
            public override bool IsPermanent => true;
            public override void OnDamageTaken(EnemyController enemy, DamageInfo info, float damage) => Total += damage;
            public override string Describe() => $"측정 {Total:0}";
        }

        private struct Row
        {
            public string Attack, Buffs, Charge, Error;
            public float PerShot, PerShotCrit, Expected, PerAction, Skill, AfterSkill;
        }

        [MenuItem("Tools/밸런스/버프 조합 피해 측정")]
        public static void Run()
        {
            var database = new TowerDatabase();
            database.Load(AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Resources/TowerData.csv").text);

            EnemyDefinition dummy = MakeDummyDefinition();
            BoardGrid grid = MakeGrid();
            var rows = new List<Row>(170000);
            var root = new GameObject("BuffComboDamageTool");
            bool logs = Debug.unityLogger.logEnabled;
            Debug.unityLogger.logEnabled = false;

            var emblems = new List<TowerEmblem>();
            for (int e = 1; e <= 14; e++) emblems.Add((TowerEmblem)e);
            var combos = new List<TowerEmblem[]>();
            for (int a = 0; a < 14; a++)
                for (int b = a + 1; b < 14; b++)
                    for (int c = b + 1; c < 14; c++)
                        combos.Add(new[] { emblems[a], emblems[b], emblems[c] });

            int total = 28 * combos.Count, done = 0;
            bool cancelled = false;
            try
            {
                foreach (TowerColor color in new[] { TowerColor.Red, TowerColor.Blue })
                {
                    foreach (TowerEmblem attackEmblem in emblems)
                    {
                        foreach (TowerEmblem[] combo in combos)
                        {
                            if (done % 50 == 0 && EditorUtility.DisplayCancelableProgressBar("버프 조합 피해 측정",
                                    $"{TowerId.Make(Tier, color, attackEmblem)} + {Names(combo)} ({done} / {total})", done / (float)total))
                            {
                                cancelled = true;
                                break;
                            }
                            for (int mask = 0; mask < 16; mask++)
                                rows.Add(Measure(database, dummy, grid, root.transform, color, attackEmblem, combo, mask));
                            done++;
                        }
                        if (cancelled) break;
                    }
                    if (cancelled) break;
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                Debug.unityLogger.logEnabled = logs;
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(dummy);
                EmblemSharedState.ResetOutsideGame();
            }

            string path = Write(rows, cancelled);
            Debug.Log($"[BuffComboDamageTool] {rows.Count}줄{(cancelled ? " (중간에 멈춤)" : "")} → {path}");
            EditorUtility.RevealInFinder(path);
        }

        private static Row Measure(TowerDatabase database, EnemyDefinition dummy, BoardGrid grid, Transform root,
                                   TowerColor color, TowerEmblem attackEmblem, TowerEmblem[] combo, int mask)
        {
            var row = new Row { Attack = $"{TowerId.Make(Tier, color, attackEmblem)} {EmblemNames.GetConceptName(TowerId.Make(Tier, color, attackEmblem))}",
                                Buffs = Names(combo), Charge = ChargeLabel(mask) };
            var created = new List<GameObject>();
            EmblemSharedState.ResetOutsideGame();
            try
            {
                var occupancy = new PathOccupancy();
                var grounds = new GroundEffectRegistry();
                AttackSkill skill = CreateSkill(color, attackEmblem, grid, occupancy, grounds);
                TowerController attack = MakeTower(database, TowerId.Make(Tier, color, attackEmblem), 0, root, created, skill);

                for (int i = 0; i < 3; i++)
                {
                    TowerController buff = MakeTower(database, TowerId.Make(Tier, TowerColor.Yellow, combo[i]), i + 1, root, created, null);
                    SupportBuffs.GiveSkill2(SupportCast.Skill2(buff, (mask & (2 << i)) != 0), attack);
                }

                int distance = Mathf.Clamp(attack.Stats.Range, 1, PathLength - 1);
                EnemyController enemy = MakeEnemy(dummy, occupancy, distance, root, created);
                var probe = new DamageProbe();
                enemy.Status.Add(probe);

                row.PerShot = enemy.PreviewDamage(TowerAction.BasicHit(attack, attack.Stats, enemy, distance, false));
                row.PerShotCrit = enemy.PreviewDamage(TowerAction.BasicHit(attack, attack.Stats, enemy, distance, true));
                row.Expected = Expected(attack, enemy, row.PerShot, row.PerShotCrit);
                int shots = attack.Stats.AttackCount * (color == TowerColor.Blue ? TargetAction.GetShotsPerAttack(attack.Stats.Mastery) : 1);
                row.PerAction = row.Expected * shots;

                try
                {
                    if ((mask & 1) != 0) attack.State.Stack.Add(attack.State.Stack.Max);
                    skill.Fire(attack, enemy);
                    for (int turn = 0; turn < DotTurns; turn++)
                    {
                        skill.OnEnemyTurnStart(attack);
                        grounds.RunTurn();
                        enemy.Status.OnTurn();
                    }
                }
                catch (Exception exception)
                {
                    row.Error = SkillErrorPrefix + exception.GetType().Name + ": " + exception.Message.Replace(',', ' ');
                }
                row.Skill = (float)probe.Total;

                float afterShot = enemy.PreviewDamage(TowerAction.BasicHit(attack, attack.Stats, enemy, distance, false));
                float afterCrit = enemy.PreviewDamage(TowerAction.BasicHit(attack, attack.Stats, enemy, distance, true));
                row.AfterSkill = Expected(attack, enemy, afterShot, afterCrit);
            }
            catch (Exception exception)
            {
                row.Error = exception.GetType().Name + ": " + exception.Message.Replace(',', ' ');
            }
            finally
            {
                foreach (GameObject go in created) Object.DestroyImmediate(go);
            }
            return row;
        }

        private static float Expected(TowerController attack, EnemyController enemy, float plain, float crit)
        {
            float rate = Mathf.Clamp01(CriticalRoll.RateAgainst(attack, attack.Stats, enemy));
            return plain + (crit - plain) * rate;
        }

        private static AttackSkill CreateSkill(TowerColor color, TowerEmblem emblem, BoardGrid grid, PathOccupancy occupancy, GroundEffectRegistry grounds)
        {
            if (color == TowerColor.Red && emblem == TowerEmblem.Bow) return new ArrowSplashSkill(grid, occupancy, grounds);
            if (color == TowerColor.Red && emblem == TowerEmblem.Darkness) return new DarknessSplashSkill(grid, occupancy, grounds);
            return SkillRegistry.CreateAttackSkill(color, emblem, grid, occupancy) ?? new DamageOnlySkill(grid, occupancy);
        }

        private static TowerController MakeTower(TowerDatabase database, int id, int slot, Transform root, List<GameObject> created, AttackSkill skill)
        {
            TowerDefinition definition = database.Get(id);
            TowerDefinition tierOne = database.Get(TowerId.Make(1, definition.Color, definition.Emblem));
            var go = new GameObject(id.ToString());
            go.transform.SetParent(root);
            created.Add(go);
            var tower = go.AddComponent<TowerController>();
            tower.Init(definition, slot, null, tierOne, null, skill);
            return tower;
        }

        private static EnemyController MakeEnemy(EnemyDefinition definition, PathOccupancy occupancy, int pathIndex, Transform root, List<GameObject> created)
        {
            var go = new GameObject("Dummy");
            go.transform.SetParent(root);
            created.Add(go);
            var movement = go.AddComponent<EnemyMovement>();
            var enemy = go.AddComponent<EnemyController>();
            SetPrivate(typeof(EnemyController), enemy, "Movement", movement);
            SetPrivate(typeof(EnemyMovement), movement, "PathIndex", pathIndex);
            enemy.Setup(definition, null, 1);
            occupancy.Add(enemy, pathIndex);
            return enemy;
        }

        private static void SetPrivate(Type type, object target, string property, object value) =>
            type.GetProperty(property, BindingFlags.Public | BindingFlags.Instance).GetSetMethod(true).Invoke(target, new[] { value });

        private static EnemyDefinition MakeDummyDefinition()
        {
            var definition = ScriptableObject.CreateInstance<EnemyDefinition>();
            var so = new SerializedObject(definition);
            so.FindProperty("<Kind>k__BackingField").enumValueIndex = (int)EnemyKind.Normal;
            so.FindProperty("<MaxHP>k__BackingField").floatValue = DummyHp;
            so.FindProperty("<Defense>k__BackingField").floatValue = 0f;
            so.FindProperty("<BaseResistance>k__BackingField").floatValue = 0f;
            so.FindProperty("<Action>k__BackingField").intValue = 3;
            so.FindProperty("<SpeedMin>k__BackingField").intValue = 2;
            so.FindProperty("<SpeedMax>k__BackingField").intValue = 4;
            so.ApplyModifiedPropertiesWithoutUndo();
            return definition;
        }

        // 스폰 칸 (0,1), 길 0 ~ 11번 = (i,0). 스폰에서 i번 칸까지의 거리는 max(i, 1)
        private static BoardGrid MakeGrid()
        {
            var path = new List<Vector3Int>();
            for (int i = 0; i < PathLength; i++) path.Add(new Vector3Int(i, 0, 0));
            var spawn = new List<Vector3Int> { new Vector3Int(0, 1, 0), new Vector3Int(0, 3, 0), new Vector3Int(1, 3, 0), new Vector3Int(2, 3, 0) };
            return new BoardGrid(path, spawn);
        }

        private static string Names(TowerEmblem[] combo) =>
            $"{EmblemNames.GetEmblemName(combo[0])}+{EmblemNames.GetEmblemName(combo[1])}+{EmblemNames.GetEmblemName(combo[2])}";

        private static string ChargeLabel(int mask)
        {
            if (mask == 0) return "차지 없음";
            var parts = new List<string>();
            if ((mask & 1) != 0) parts.Add("공격");
            for (int i = 0; i < 3; i++) if ((mask & (2 << i)) != 0) parts.Add($"버프{i + 1}");
            return string.Join("+", parts);
        }

        private static string F(float value) => value.ToString("0.##", s_inv);

        private static string Write(List<Row> rows, bool cancelled)
        {
            string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Balance"));
            Directory.CreateDirectory(folder);
            string stamp = DateTime.Now.ToString("yyyyMMdd_HHmm", s_inv);
            string csvPath = Path.Combine(folder, $"buff_combo_damage_{stamp}.csv");
            string mdPath = Path.Combine(folder, $"buff_combo_damage_{stamp}_summary.md");

            var csv = new StringBuilder();
            csv.AppendLine("공격 타워,버프 1+2+3,차지,한 발(일반),한 발(치명),한 발(기대),행동당(기대),스킬 2 피해,스킬 뒤 한 발(기대),오류");
            foreach (Row row in rows)
                csv.AppendLine($"{row.Attack},{row.Buffs},{row.Charge},{F(row.PerShot)},{F(row.PerShotCrit)},{F(row.Expected)},{F(row.PerAction)},{F(row.Skill)},{F(row.AfterSkill)},{row.Error}");
            File.WriteAllText(csvPath, csv.ToString(), new UTF8Encoding(true));

            var ok = rows.FindAll(row => string.IsNullOrEmpty(row.Error));
            var shotOk = rows.FindAll(row => string.IsNullOrEmpty(row.Error) || row.Error.StartsWith(SkillErrorPrefix));
            var md = new StringBuilder();
            md.AppendLine("# 버프 조합 피해 측정");
            md.AppendLine();
            md.AppendLine($"- 만든 때: {DateTime.Now:yyyy-MM-dd HH:mm}{(cancelled ? " (중간에 멈춤)" : "")}. 전체 {rows.Count}줄, 오류 {rows.Count - ok.Count}줄. 전체 기록: `{Path.GetFileName(csvPath)}`");
            md.AppendLine("- 공격 타워 28종 다이아(컬러 레벨 1) + 다이아 버프 3기의 **스킬 2**. \"차지\"는 그 타워가 스킬 2를 차지로 씀");
            md.AppendLine("- 적: 체력 100억, 방어력 0, 저항 0, 공격 타워 사거리 끝(거리 = 사거리). 판 밖이라 한도 20, 골드 0, 침식 카운트 0, 초월 아님");
            md.AppendLine("- 한 발(기대) = 일반 + (치명 − 일반) × 치명타 확률. 스킬 2 피해는 장판·지속 피해를 끝까지 합친 값. 자기·아군 강화 스킬은 0이고 대신 \"스킬 뒤 한 발\"이 오름");
            md.AppendLine("- TowerData.csv의 지금 값 그대로 (버프·디버프 AbilityValue 10 등 임시값 포함)");
            md.AppendLine();

            AppendTop(md, shotOk, "한 발(기대)", row => row.Expected);
            AppendTop(md, shotOk, "행동당(기대)", row => row.PerAction);
            AppendTop(md, ok.FindAll(row => row.Skill > 0f), "스킬 2 피해 (피해가 있는 스킬만)", row => row.Skill);
            AppendPerAttack(md, shotOk);

            if (rows.Count > ok.Count)
            {
                md.AppendLine("## 오류");
                var seen = new HashSet<string>();
                foreach (Row row in rows)
                    if (!string.IsNullOrEmpty(row.Error) && seen.Add(row.Attack + row.Error)) md.AppendLine($"- {row.Attack}: {row.Error}");
            }
            File.WriteAllText(mdPath, md.ToString(), new UTF8Encoding(false));
            return mdPath;
        }

        private static void AppendTop(StringBuilder md, List<Row> rows, string title, Func<Row, float> value)
        {
            var sorted = new List<Row>(rows);
            sorted.Sort((a, b) => value(b).CompareTo(value(a)));
            md.AppendLine($"## {title} — 가장 센 조합 10");
            md.AppendLine("| 공격 타워 | 버프 | 차지 | 값 |");
            md.AppendLine("|---|---|---|---:|");
            for (int i = 0; i < 10 && i < sorted.Count; i++) md.AppendLine($"| {sorted[i].Attack} | {sorted[i].Buffs} | {sorted[i].Charge} | {value(sorted[i]):N0} |");
            md.AppendLine();
            md.AppendLine($"## {title} — 가장 약한 조합 10");
            md.AppendLine("| 공격 타워 | 버프 | 차지 | 값 |");
            md.AppendLine("|---|---|---|---:|");
            for (int i = 0; i < 10 && i < sorted.Count; i++)
            {
                Row row = sorted[sorted.Count - 1 - i];
                md.AppendLine($"| {row.Attack} | {row.Buffs} | {row.Charge} | {value(row):N0} |");
            }
            md.AppendLine();
        }

        private static void AppendPerAttack(StringBuilder md, List<Row> rows)
        {
            md.AppendLine("## 공격 타워마다 — 차지 없음 기준, 한 발(기대)가 가장 센·약한 버프 조합");
            md.AppendLine("| 공격 타워 | 가장 센 버프 | 한 발(기대) | 가장 약한 버프 | 한 발(기대) | 스킬 2 피해 (가장 센 조합) |");
            md.AppendLine("|---|---|---:|---|---:|---:|");
            var byAttack = new Dictionary<string, List<Row>>();
            var order = new List<string>();
            foreach (Row row in rows)
            {
                if (row.Charge != "차지 없음") continue;
                if (!byAttack.TryGetValue(row.Attack, out List<Row> list)) { byAttack[row.Attack] = list = new List<Row>(); order.Add(row.Attack); }
                list.Add(row);
            }
            foreach (string attack in order)
            {
                List<Row> list = byAttack[attack];
                Row best = list[0], worst = list[0];
                foreach (Row row in list)
                {
                    if (row.Expected > best.Expected) best = row;
                    if (row.Expected < worst.Expected) worst = row;
                }
                md.AppendLine($"| {attack} | {best.Buffs} | {best.Expected:N0} | {worst.Buffs} | {worst.Expected:N0} | {best.Skill:N0} |");
            }
            md.AppendLine();
        }
    }
}
