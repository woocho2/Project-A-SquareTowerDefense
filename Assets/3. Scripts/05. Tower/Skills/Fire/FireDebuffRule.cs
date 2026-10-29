using System;
using System.Collections.Generic;

namespace SquareTD
{
    // 불 디버프 — 화상 · 연소
    public class FireDebuffRule : DebuffRule
    {
        private readonly Func<PathOccupancy> m_occupancy;

        public FireDebuffRule(Func<PathOccupancy> occupancy)
        {
            m_occupancy = occupancy;
        }

        public override StatusEffect CreateEffect(Zone zone, EnemyController enemy)
        {
            bool isSkill2 = zone.Stage == ZoneStage.Skill2;
            float value = Resist(enemy, SkillFormula.ZoneValue(zone));
            return new FireDebuffEffect(zone.Owner, zone.Stage, zone.Owner.Stats.Duration, value, isSkill2 ? m_occupancy : null);
        }
    }

    public class FireDebuffEffect : EmblemDebuffEffect
    {
        private readonly TowerController m_owner;
        private readonly float m_turnDamage;
        private readonly Func<PathOccupancy> m_spreadTo;

        public FireDebuffEffect(TowerController owner, ZoneStage stage, int durationActions, float turnDamage, Func<PathOccupancy> spreadTo)
            : base(TowerEmblem.Fire, stage, durationActions, turnDamage,
                   $"매 턴 최대 체력 {turnDamage * 100f:0.##}% 피해, 받는 치유 −{turnDamage * 100f:0.##}%" + (spreadTo != null ? ", 같은 칸의 적에게 번짐" : ""),
                   StatModifier.Flat(KeyOf(TowerEmblem.Fire), StatType.HealingTaken, -turnDamage))
        {
            m_owner = owner;
            m_turnDamage = turnDamage;
            m_spreadTo = spreadTo;
        }

        public static FireDebuffEffect Fireball(TowerController owner)
        {
            TowerFinalStats stats = owner.Stats;
            float value = SkillFormula.SupportSkill1(stats.Power, stats.AbilityValue, 1f, 1f);
            return new FireDebuffEffect(owner, ZoneStage.Skill1, stats.ColorLevel * 2, value, null);
        }

        public override void OnTurn(EnemyController enemy)
        {
            if (m_spreadTo != null) Spread(enemy);
            if (m_turnDamage <= 0f) return;

            enemy.TakeDamage(new DamageInfo
            {
                Attacker = m_owner != null ? m_owner : null,
                Source = DamageSource.Dot,
                Emblem = TowerEmblem.Fire,
                BaseDamage = enemy.MaxHp * m_turnDamage,
                ForceFixed = true,
            });
        }

        private void Spread(EnemyController enemy)
        {
            PathOccupancy occupancy = m_spreadTo();
            if (occupancy == null || m_owner == null) return;

            float value = SkillFormula.SupportSkill1(m_owner.Stats.Power, m_owner.Stats.AbilityValue, 1f, 1f);
            foreach (ITarget other in new List<ITarget>(occupancy.GetAt(enemy.PathIndex)))
            {
                EnemyController body = other.Body;
                if (other.IsDummy || body == null || body == enemy || !body.IsAlive) continue;
                body.Status.Add(new SpreadBurnEffect(m_owner, enemy, body.Resistance.ResistWeak(value)));
            }
        }
    }

    public class SpreadBurnEffect : FireDebuffEffect
    {
        private readonly EnemyController m_source;

        public SpreadBurnEffect(TowerController owner, EnemyController source, float turnDamage)
            : base(owner, ZoneStage.Skill1, int.MaxValue, turnDamage, null)
        {
            m_source = source;
        }

        public override void OnTurn(EnemyController enemy)
        {
            if (m_source == null || !m_source.IsAlive || m_source.PathIndex != enemy.PathIndex)
            {
                enemy.Status.Remove(Key);
                return;
            }
            base.OnTurn(enemy);
        }

        public override string Describe() => $"{Key}: 옮겨붙은 화상 (연소인 적과 같은 칸에 있는 동안)";
    }
}
