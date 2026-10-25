namespace SquareTD
{
    // 20번 3장 [일반 큐브의 면]: 지금 보이는 면의 효과를 적의 스탯 수정자로. 수치는 12장에서 나중에 정함 → 지금은 전부 0
    public static class EnemyCubeFaces
    {
        public const string Source = "큐브 면";

        // 칸 번호 = EnemyCubeFaceAsset 번호 (SelfHeal 0, Defense 1, Action 2, Mobility 3, Locked 4, Hourglass 5)
        private static readonly StatModifier[][] s_faceModifiers =
        {
            new StatModifier[0],                                                            // 자가치유 — 스탯이 아니라 회복 (수업 22의 회복 타일과 같은 때)
            new[] { StatModifier.Percent(Source, StatType.Defense, 0f) },                   // 방어
            new[] { StatModifier.Flat(Source, StatType.ActionGainPerTurn, 0f) },            // 행동력
            new[] { StatModifier.Flat(Source, StatType.SpeedMin, 0f),
                    StatModifier.Flat(Source, StatType.SpeedMax, 0f) },                     // 이동력
            new[] { StatModifier.Percent(Source, StatType.DamageTaken, 0f) },               // 자물쇠 (모든 능력 무효는 수업 25)
            new StatModifier[0],                                                            // 모래시계 — 면역은 수업 25의 상태 효과
        };

        public static StatModifier[] GetModifiers(EnemyCubeFaceAsset face) => s_faceModifiers[(int)face];

        public static void Apply(EnemyStats stats, EnemyCubeFaceAsset face)
        {            
            stats.Modifiers.RemoveBySource(Source);

            foreach(StatModifier modifier in GetModifiers(face))
            {
                stats.Modifiers.Add(modifier);
            }
        }
    }
}
