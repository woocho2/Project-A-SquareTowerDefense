using System;

namespace SquareTD
{
    // 타워 스폰 칸의 특수 타일 (20번 2장). 네 종류 모두 "서 있는 동안의 스탯 수정자"라 표 하나로 충분함
    // 버프·디버프 타워는 받지 않음 → TowerStatCalculator가 공격 타워에만 수정자를 더하므로 여기서 따로 막지 않음
    public static class SpawnTileEffects
    {
        public const string Source = "스폰 특수 타일";

        // 칸 번호 = SpawnTileType 번호
        private static readonly StatModifier[][] s_modifiers =
        {
            Array.Empty<StatModifier>(),                                                    // None
            new[] { StatModifier.Percent(Source, StatType.Power, 0.3f) },                   // 공격력 +30%
            new[] { StatModifier.Flat(Source, StatType.ActionGainPerTurn, 1f) },            // 매 턴 행동 카운트 +1
            new[] { StatModifier.Flat(Source, StatType.AttackCount, 1f) },                  // 공격 횟수 +1
            new[] { StatModifier.Flat(Source, StatType.Mastery, 1f) },                      // Mastery +1
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
