namespace SquareTD
{
    // 물 디버프 — 미스트 · 세이렌 송 (젖음 스택, 세이렌 송 받는 피해 +10%, 물 타워에게 맞으면 +10% 더, 다음 행동 이동력 +2)
    public class WaterDebuffRule : DebuffRule
    {
        public const float SirenDamageTaken = 0.1f;
        public const float WaterDamageTaken = 0.1f;
        public const int SlipSpeed = 2;

        public override void OnApply(SupportCast cast, EnemyController enemy) => WetEffect.Add(enemy, Resist(enemy, cast.Amount));

        public override StatusEffect CreateEffect(SupportCast cast, EnemyController enemy) =>
            cast.IsSkill2 ? new WaterDebuffEffect(cast, Resist(enemy, SirenDamageTaken), Resist(enemy, WaterDamageTaken)) : null;
    }

    public class WaterDebuffEffect : EmblemDebuffEffect
    {
        private readonly float m_waterTaken;
        private bool m_slip;

        public WaterDebuffEffect(SupportCast cast, float damageTaken, float waterTaken)
            : base(cast, $"받는 피해 +{damageTaken * 100f:0.#}%, 물 타워에게 +{waterTaken * 100f:0.#}% 더 (맞으면 다음 행동 이동력 +{WaterDebuffRule.SlipSpeed})",
                   StatModifier.Flat(KeyOf(TowerEmblem.Water), StatType.DamageTaken, damageTaken))
        {
            m_waterTaken = waterTaken;
        }

        private static bool IsWaterHit(DamageInfo info) =>
            info.Attacker != null && info.Attacker.Definition.Emblem == TowerEmblem.Water && info.Attacker.IsAttackTower &&
            (info.Source == DamageSource.Basic || info.Source == DamageSource.Skill);

        public override float ModifyIncomingDamage(EnemyController enemy, DamageInfo info, float damage)
        {
            if (!IsWaterHit(info)) return damage;
            float taken = enemy.Stats.Get(StatType.DamageTaken);
            return taken > 0f ? damage * (taken + m_waterTaken) / taken : damage;
        }

        public override void OnDamageTaken(EnemyController enemy, DamageInfo info, float damage)
        {
            if (IsWaterHit(info)) m_slip = true;
        }

        public override MoveRange OnDecideMove(EnemyController enemy, MoveRange range)
        {
            if (!m_slip || (range.Min == 0 && range.Max == 0)) return range;
            m_slip = false;
            return new MoveRange(range.Min + WaterDebuffRule.SlipSpeed, range.Max + WaterDebuffRule.SlipSpeed, range.Action);
        }
    }
}
