using UnityEngine;

namespace SquareTD
{
    // 스킬의 공통 식 (공격 스킬, 버프·디버프 스킬 1·2, 저항, 유지 시간)
    public static class SkillFormula
    {
        public const float PercentRate = 0.01f;

        public static float AttackSkill(float power, float abilityValue) => power * abilityValue;

        public const float SupportBasePower = 4f;

        public static float SupportSkill1(float power) => power;

        public static float SupportSkill2(float power, float abilityValue) => power * (1f + abilityValue);

        public static float Resist(float value, float resistancePercent) => value * (1f - Mathf.Min(resistancePercent, 100f) / 100f);

        public const int EffectActions = 2;
        public const int EffectTurns = 3;

        public static int LevelTierCount(int colorLevel, int tier) => Mathf.Max(1, colorLevel * tier);

        public static int LevelTierCount(TowerController owner) => LevelTierCount(owner.Stats.ColorLevel, owner.Definition.Tier);
    }
}
