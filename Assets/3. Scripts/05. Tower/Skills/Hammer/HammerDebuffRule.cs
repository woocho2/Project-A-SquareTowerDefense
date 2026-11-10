namespace SquareTD
{
    // 해머 디버프 — 크랙 · 잭팟 (방어력 감소, 잭팟인 적이 죽으면 젬 +1, 타격이 처치하면 50%로 젬 +1)
    public class HammerDebuffRule : DebuffRule
    {
        public const float BluntGemChance = 0.5f;

        public override StatusEffect CreateEffect(SupportCast cast, EnemyController enemy)
        {
            return new HammerDebuffEffect(cast, Resist(enemy, cast.Rate));
        }
    }

    public class HammerDebuffEffect : EmblemDebuffEffect
    {
        public HammerDebuffEffect(SupportCast cast, float defenseCut)
            : base(cast, $"방어력 −{defenseCut * 100f:0.##}%" + (cast.IsSkill2 ? ", 죽으면 젬 +1 (타격이 처치하면 50%로 +1 더)" : ""),
                   StatModifier.Percent(KeyOf(TowerEmblem.Hammer), StatType.Defense, -defenseCut))
        {
        }

        public override int RollGemBonus(KillInfo kill)
        {
            if (!IsSkill2) return 0;
            int gems = 1;
            if (kill.Damage.Attacker != null && kill.Damage.Attribute == AttackAttribute.Blunt && Chance.Roll(HammerDebuffRule.BluntGemChance)) gems++;
            return gems;
        }
    }
}
