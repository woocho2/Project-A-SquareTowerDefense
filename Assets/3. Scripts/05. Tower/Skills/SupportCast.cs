namespace SquareTD
{
    // 버프·디버프 타워가 거는 스킬 1·스킬 2 한 번의 값
    public readonly struct SupportCast
    {
        public readonly TowerController Owner;
        public readonly TowerEmblem Emblem;
        public readonly AttackAttribute Attribute;
        public readonly bool IsSkill2;
        public readonly float Amount;
        public readonly int Tier;
        public readonly int ColorLevel;
        public readonly float Power;
        public readonly float AbilityValue;

        public float Rate => Amount * SkillFormula.PercentRate;
        public float Scale => Amount / SkillFormula.SupportBasePower;
        public float Bronze(float bronzeValue) => bronzeValue * Scale;
        public float Penalty(float bronzePenalty) => bronzePenalty * (2f - Scale);
        public int Duration => SkillFormula.EffectActions;

        public SupportCast(TowerController owner, bool isSkill2, float abilityValue)
        {
            Owner = owner;
            Emblem = owner.Definition.Emblem;
            Attribute = owner.Definition.Attribute;
            IsSkill2 = isSkill2;
            Tier = owner.Definition.Tier;
            ColorLevel = owner.Stats.ColorLevel;
            Power = owner.Stats.Power;
            AbilityValue = abilityValue;
            Amount = isSkill2 ? SkillFormula.SupportSkill2(Power, abilityValue) : SkillFormula.SupportSkill1(Power);
        }

        public static SupportCast Skill1(TowerController owner) => new SupportCast(owner, false, owner.Stats.AbilityValue);

        public static SupportCast Skill2(TowerController owner) => new SupportCast(owner, true, owner.Stats.AbilityValue);

        public string SkillName => IsSkill2 ? "스킬 2" : "스킬 1";
    }
}
