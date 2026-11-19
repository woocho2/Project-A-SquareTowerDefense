namespace SquareTD
{
    // 적 큐브 면 효과와 최종 보스의 고유 기믹 면 판정
    public static class EnemyCubeFaces
    {
        public const string Source = "큐브 면";

        public static bool IsBossSkillFace(EnemyCubeFaceAsset face, EnemyKind kind) =>
            kind == EnemyKind.FinalBoss && face == EnemyCubeFaceAsset.Locked;

        public static StatModifier[] GetModifiers(EnemyCubeFaceAsset face, int adaptive, EnemyKind kind = EnemyKind.Normal)
        {
            if (IsBossSkillFace(face, kind)) return new StatModifier[0];

            switch (face)
            {
                case EnemyCubeFaceAsset.Defense:
                    return new[] { StatModifier.Percent(Source, StatType.Defense, 0.05f * adaptive) };

                case EnemyCubeFaceAsset.Action:
                    return new[] { StatModifier.Flat(Source, StatType.ActionGainPerTurn, adaptive) };

                case EnemyCubeFaceAsset.Mobility:
                    return new[] { StatModifier.Flat(Source, StatType.SpeedMin, adaptive),
                                   StatModifier.Flat(Source, StatType.SpeedMax, adaptive) };

                case EnemyCubeFaceAsset.Locked:
                    return new[] { StatModifier.Percent(Source, StatType.DamageTaken, 0.1f * adaptive) };

                case EnemyCubeFaceAsset.Hourglass:
                    return new[] { StatModifier.Percent(Source, StatType.DamageTaken, -0.1f * adaptive) };

                default:
                    return new StatModifier[0];
            }
        }

        public static float GetHealRate(EnemyCubeFaceAsset face, int adaptive) => face == EnemyCubeFaceAsset.SelfHeal ? HealPerAdaptive * adaptive : 0f;

        public const float HealPerAdaptive = 0.025f;

        public static void Apply(EnemyStats stats, EnemyCubeFaceAsset face, int adaptive, EnemyKind kind = EnemyKind.Normal)
        {
            stats.Modifiers.SetSource(Source, GetModifiers(face, adaptive, kind));
        }
    }
}
