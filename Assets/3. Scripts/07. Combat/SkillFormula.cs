using UnityEngine;

namespace SquareTD
{
    // 스킬의 공통 식 (공격 스킬, 차지, 버프·디버프 스킬 1·2, 저항, 유지 시간)
    public static class SkillFormula
    {
        public const float ChargeRate = 0.01f;
        public const float PercentRate = 0.01f;

        public static float AttackSkill(float power, float abilityValue) => power * abilityValue;

        public static float ChargedAbility(float abilityValue, float charge, float chargeValue) => abilityValue * (1f + charge * ChargeRate * chargeValue);

        public static float WithStack(float value, float stack, float stackValue) => value * (1f + stack * ChargeRate * stackValue);

        public static float SupportSkill1(float power) => power;

        public static float SupportSkill2(float power, float abilityValue) => power * (1f + abilityValue);

        public static float Resist(float value, float resistancePercent) => value * (1f - Mathf.Min(resistancePercent, 100f) / 100f);

        public static int EffectDuration(int colorLevel, int tier) => Mathf.Max(1, colorLevel * tier);

        public static int EffectDuration(TowerController owner) => EffectDuration(owner.Stats.ColorLevel, owner.Definition.Tier);
    }
}
