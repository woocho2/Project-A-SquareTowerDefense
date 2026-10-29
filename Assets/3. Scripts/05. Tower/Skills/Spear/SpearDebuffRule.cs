using UnityEngine;

namespace SquareTD
{
    // 창 디버프 — 출혈 · 과다출혈
    public class SpearDebuffRule : DebuffRule
    {
        public const float MoveDamageRate = 0.02f;

        public override StatusEffect CreateEffect(Zone zone, EnemyController enemy)
        {
            bool isSkill2 = zone.Stage == ZoneStage.Skill2;
            float healingCut = Resist(enemy, SkillFormula.ZoneValue(zone));
            float pierceMax = Resist(enemy, isSkill2 ? 0.2f : 0.1f);
            float moveDamage = isSkill2 ? Resist(enemy, MoveDamageRate) : 0f;
            return new SpearDebuffEffect(zone, healingCut, pierceMax, moveDamage);
        }
    }

    public class SpearDebuffEffect : EmblemDebuffEffect
    {
        private readonly TowerController m_owner;
        private readonly float m_pierceMax;
        private readonly float m_moveDamage;

        public SpearDebuffEffect(Zone zone, float healingCut, float pierceMax, float moveDamage)
            : base(TowerEmblem.Spear, zone.Stage, zone.Owner.Stats.Duration, healingCut,
                   $"받는 치유 −{healingCut * 100f:0.##}%, 찌르기 피해 최대 +{pierceMax * 100f:0.#}%" + (moveDamage > 0f ? $", 이동한 칸마다 최대 체력 {moveDamage * 100f:0.#}%" : ""),
                   StatModifier.Flat(KeyOf(TowerEmblem.Spear), StatType.HealingTaken, -healingCut))
        {
            m_owner = zone.Owner;
            m_pierceMax = pierceMax;
            m_moveDamage = moveDamage;
        }

        public override float ModifyIncomingDamage(EnemyController enemy, DamageInfo info, float damage)
        {
            if (info.Attribute != AttackAttribute.Pierce || enemy.MaxHp <= 0f) return damage;
            float lost = Mathf.Clamp01(1f - enemy.CurrentHp / enemy.MaxHp);
            return damage * (1f + m_pierceMax * lost);
        }

        public override void OnMoved(EnemyController enemy, int steps)
        {
            if (m_moveDamage <= 0f || steps == 0) return;
            enemy.TakeDamage(new DamageInfo
            {
                Attacker = m_owner != null ? m_owner : null,
                Source = DamageSource.Dot,
                Emblem = TowerEmblem.Spear,
                BaseDamage = enemy.MaxHp * m_moveDamage * Mathf.Abs(steps),
                ForceFixed = true,
            });
        }
    }
}
