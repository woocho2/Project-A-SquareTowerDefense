namespace SquareTD
{
    // 적 큐브 보이는 면의 효과를 적응형 능력치만큼 스탯 수정자·치유로 (보스는 자물쇠의 받는 피해 증가가 없음 — 그 자리는 보스 고유 스킬)
    public static class EnemyCubeFaces
    {
        public const string Source = "큐브 면";

        public static bool IsBossSkillFace(EnemyCubeFaceAsset face) => face == EnemyCubeFaceAsset.Locked;

        public static StatModifier[] GetModifiers(EnemyCubeFaceAsset face, int adaptive, bool isBoss = false)
        {
            if (isBoss && IsBossSkillFace(face)) return new StatModifier[0];

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

        public static float GetHealRate(EnemyCubeFaceAsset face, int adaptive) => face == EnemyCubeFaceAsset.SelfHeal ? 0.1f * adaptive : 0f;

        public static void Apply(EnemyStats stats, EnemyCubeFaceAsset face, int adaptive, bool isBoss = false)
        {
            stats.Modifiers.RemoveBySource(Source);

            foreach(StatModifier modifier in GetModifiers(face, adaptive, isBoss))
            {
                stats.Modifiers.Add(modifier);
            }
        }
    }
}
