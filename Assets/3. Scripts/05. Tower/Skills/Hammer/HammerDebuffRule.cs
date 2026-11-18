namespace SquareTD
{
    // 해머 디버프 — 크랙 · 잭팟 (방어력 감소, 잭팟인 적이 죽으면 골드 +100, 타격 타워가 처치하면 +100 더)
    public class HammerDebuffRule : DebuffRule
    {
        public const float BronzeDefenseCut = 0.05f;
        public const int JackpotGold = 100;
        public const int BluntJackpotGold = 100;

        public override StatusEffect CreateEffect(SupportCast cast, EnemyController enemy)
        {
            return new HammerDebuffEffect(cast, Resist(enemy, cast.Bronze(BronzeDefenseCut)));
        }
    }

    public class HammerDebuffEffect : EmblemDebuffEffect
    {
        public HammerDebuffEffect(SupportCast cast, float defenseCut)
            : base(cast, $"방어력 −{defenseCut * 100f:0.##}%" + (cast.IsSkill2 ? $", 죽으면 골드 +{HammerDebuffRule.JackpotGold} (타격 타워가 처치하면 +{HammerDebuffRule.BluntJackpotGold} 더)" : ""),
                   StatModifier.Percent(KeyOf(TowerEmblem.Hammer), StatType.Defense, -defenseCut))
        {
        }

        public override int RollGoldBonus(KillInfo kill)
        {
            if (!IsSkill2) return 0;
            int gold = HammerDebuffRule.JackpotGold;
            if (kill.Damage.Attacker != null && kill.Damage.Attribute == AttackAttribute.Blunt) gold += HammerDebuffRule.BluntJackpotGold;
            return gold;
        }
    }
}
