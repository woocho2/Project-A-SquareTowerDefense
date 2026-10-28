using UnityEngine;

namespace SquareTD
{
    // 창 디버프 — 출혈(스킬 1) · 과다출혈(스킬 2) (20번 4-6장)
    //   받는 치유 −(0.001 × Power × AbilityValue × 스택)%
    //   과다출혈 고유 효과 (2026-10-28 사용자): 이동할 때마다 (이동한 칸 수 × 최대 체력의 2%) 피해, 방어 무시. 뒤로 간 칸도 셈
    //   찌르기 시너지: 찌르기 공격에 맞으면 잃은 체력 비례로 피해 + — 다 잃었을 때 +10%(출혈) / +20%(과다출혈), 절반이면 그 절반
    //   모든 수치는 적의 저항력만큼 줄어듦
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

    // 출혈·과다출혈이 적에게 걸려 있는 동안 하는 일. 값은 SpearDebuffRule이 저항까지 계산해서 넘김
    public class SpearDebuffEffect : EmblemDebuffEffect
    {
        private readonly TowerController m_owner;
        private readonly float m_pierceMax;         // 체력을 다 잃었을 때 찌르기 피해에 더하는 비율
        private readonly float m_moveDamage;        // 과다출혈: 한 칸 이동할 때 최대 체력의 이 비율 (출혈은 0)

        public SpearDebuffEffect(Zone zone, float healingCut, float pierceMax, float moveDamage)
            : base(TowerEmblem.Spear, zone.Stage, zone.Owner.Stats.Duration, healingCut,
                   $"받는 치유 −{healingCut * 100f:0.##}%, 찌르기 피해 최대 +{pierceMax * 100f:0.#}%" + (moveDamage > 0f ? $", 이동한 칸마다 최대 체력 {moveDamage * 100f:0.#}%" : ""),
                   StatModifier.Flat(KeyOf(TowerEmblem.Spear), StatType.HealingTaken, -healingCut))
        {
            m_owner = zone.Owner;
            m_pierceMax = pierceMax;
            m_moveDamage = moveDamage;
        }

        // 찌르기 공격이면 잃은 체력 비율만큼: 피해 × (1 + 최대치 × 잃은 비율)
        public override float ModifyIncomingDamage(EnemyController enemy, DamageInfo info, float damage)
        {
            if (info.Attribute != AttackAttribute.Pierce || enemy.MaxHp <= 0f) return damage;
            float lost = Mathf.Clamp01(1f - enemy.CurrentHp / enemy.MaxHp);
            return damage * (1f + m_pierceMax * lost);
        }

        // 이동한 뒤: 과다출혈이면 칸 수만큼 피를 흘림
        public override void OnMoved(EnemyController enemy, int steps)
        {
            if (m_moveDamage <= 0f || steps == 0) return;
            enemy.TakeDamage(new DamageInfo
            {
                Attacker = m_owner != null ? m_owner : null,    // 그사이 팔린 타워면 null
                Source = DamageSource.Dot,
                Emblem = TowerEmblem.Spear,
                BaseDamage = enemy.MaxHp * m_moveDamage * Mathf.Abs(steps),
                ForceFixed = true,
            });
        }
    }
}
