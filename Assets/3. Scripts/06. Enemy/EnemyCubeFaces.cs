namespace SquareTD
{
    // 적 큐브 윗면의 효과를 스탯 수정자로
    public static class EnemyCubeFaces
    {
        public const string Source = "큐브 면";

        private static readonly StatModifier[][] s_faceModifiers =
        {
            new StatModifier[0],
            new[] { StatModifier.Percent(Source, StatType.Defense, 0f) },
            new[] { StatModifier.Flat(Source, StatType.ActionGainPerTurn, 0f) },
            new[] { StatModifier.Flat(Source, StatType.SpeedMin, 0f),
                    StatModifier.Flat(Source, StatType.SpeedMax, 0f) },
            new[] { StatModifier.Percent(Source, StatType.DamageTaken, 0f) },
            new StatModifier[0],
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
