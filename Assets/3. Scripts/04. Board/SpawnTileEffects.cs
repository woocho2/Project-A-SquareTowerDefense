using System;

namespace SquareTD
{
    // 타워 스폰 칸의 특수 타일 효과 (서 있는 동안의 스탯 수정자)
    public static class SpawnTileEffects
    {
        public const string Source = "스폰 특수 타일";

        private static readonly StatModifier[][] s_modifiers =
        {
            Array.Empty<StatModifier>(),
            new[] { StatModifier.Percent(Source, StatType.Power, 0.3f) },
            new[] { StatModifier.Flat(Source, StatType.ActionGainPerTurn, 1f) },
            new[] { StatModifier.Flat(Source, StatType.AttackCount, 1f) },
            new[] { StatModifier.Flat(Source, StatType.Mastery, 1f) },
        };

        public static StatModifier[] GetModifiers(SpawnTileType type) => s_modifiers[(int)type];

        public static void Apply(StatModifierList modifiers, SpawnTileType type)
        {
            modifiers.RemoveBySource(Source);

            foreach (StatModifier modifier in GetModifiers(type))
            {
                modifiers.Add(modifier);
            }
        }
    }
}
