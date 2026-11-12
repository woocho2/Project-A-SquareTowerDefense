using System.Collections.Generic;

namespace SquareTD
{
    // 빛 단일 — 하이페리온 ((Tier × 레벨) 턴 동안 매 턴 피해, 첫 턴 Power × AbilityValue × 0.1에서 매 턴 2배.
    //                       초월 웨이브에는 맵 전체의 적에게, 첫 턴 Power × AbilityValue에서 매 턴 4배)
    public class LightTargetSkill : AttackSkill
    {
        public const float FirstRatio = 0.1f;
        public const float DoubleGrowth = 2f;
        public const float TranscendGrowth = 4f;

        public LightTargetSkill(BoardGrid grid, PathOccupancy occupancy)
            : base(grid, occupancy) { }

        protected override bool CastInstead(TowerController owner, ITarget mainTarget, bool useStack)
        {
            TowerFinalStats stats = owner.Stats;
            float damage = SkillFormula.AttackSkill(stats.Power, ChargedAbility(stats, useStack)) * stats.FinalDamageMultiplier;
            int turns = EffectDuration(owner);

            if (EmblemSharedState.Current.IsTranscendWave)
            {
                foreach (ITarget target in SkillArea.WholeMap(m_grid, m_occupancy))
                {
                    if (target.IsAlive && target.Body != null)
                    {
                        target.Body.Status.Add(new HyperionEffect(owner, turns, damage, TranscendGrowth));
                        SkillEffectTable.PlayHyperion(owner, target.Body, 0, true);
                    }
                }
                return true;
            }

            EnemyController enemy = mainTarget?.Body;
            if (enemy == null || !enemy.IsAlive) return false;
            enemy.Status.Add(new HyperionEffect(owner, turns, damage * FirstRatio, DoubleGrowth));
            SkillEffectTable.PlayHyperion(owner, enemy, 0, true);
            return true;
        }

        protected override List<ITarget> GetArea(TowerController owner, ITarget mainTarget) => new List<ITarget>();
        protected override void OnHitTarget(TowerController owner, ITarget target, DamageInfo hit) { }
    }

    public class HyperionEffect : StatusEffect
    {
        public const string HyperionKey = "하이페리온";

        private readonly TowerController m_owner;
        private readonly float m_growth;
        private float m_damage;
        private int m_turnsLeft;
        private int m_visualPulse;

        public HyperionEffect(TowerController owner, int turns, float firstDamage, float growth) : base(1)
        {
            m_owner = owner;
            m_turnsLeft = turns;
            m_damage = firstDamage;
            m_growth = growth;
        }

        public override string Key => HyperionKey;
        public override bool IsPermanent => m_turnsLeft > 0;
        public override float Strength => m_damage;

        public override void OnTurn(EnemyController enemy)
        {
            if (m_turnsLeft <= 0) return;
            m_turnsLeft--;
            float damage = m_damage;
            m_damage *= m_growth;
            SkillEffectTable.PlayHyperion(m_owner, enemy, ++m_visualPulse, false);
            enemy.TakeDamage(new DamageInfo { Attacker = m_owner, Source = DamageSource.Skill, Emblem = TowerEmblem.Light, BaseDamage = damage });
            if (m_turnsLeft <= 0 && enemy.IsAlive) enemy.Status.Remove(HyperionKey);
        }

        public override string Describe() => $"하이페리온: 다음 턴 {m_damage:0.#} 피해 (매 턴 ×{m_growth:0}), {m_turnsLeft}턴 남음";
    }
}
