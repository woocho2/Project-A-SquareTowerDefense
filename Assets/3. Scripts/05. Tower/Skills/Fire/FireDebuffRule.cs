using System;
using System.Collections.Generic;

namespace SquareTD
{
    // 불 디버프 — 화상(스킬 1) · 연소(스킬 2) (20번 4-9장)
    //   매 턴 최대 체력의 (0.001 × Power × AbilityValue × 스택)% 피해, 받는 치유도 같은 값만큼 감소. 저항력만큼 줄어듦
    //   연소의 고유 효과 (번지는 불): 연소인 적과 같은 칸에 있는 적들은 화상을 임시로 받음 (스택 1, 같은 칸에 있는 동안만, 다시 번지지 않음)
    //   화상·연소·세기가 다른 화상이 함께 걸리면 높은 것이 우선 (같은 키 — EnemyStatusEffects가 센 쪽을 남김)
    public class FireDebuffRule : DebuffRule
    {
        private readonly Func<PathOccupancy> m_occupancy;       // 번지는 불이 같은 칸의 적을 찾을 곳 (게임에서는 BoardManager의 점유)

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

    // 화상·연소가 적에게 걸려 있는 동안. 화염구가 거는 화상도 이것 (스택 1 고정)
    public class FireDebuffEffect : EmblemDebuffEffect
    {
        private readonly TowerController m_owner;
        private readonly float m_turnDamage;                    // 매 턴 최대 체력의 이 비율 (= 받는 치유 감소)
        private readonly Func<PathOccupancy> m_spreadTo;        // 연소만: 같은 칸의 적에게 번짐 (null이면 번지지 않음)

        public FireDebuffEffect(TowerController owner, ZoneStage stage, int durationActions, float turnDamage, Func<PathOccupancy> spreadTo)
            : base(TowerEmblem.Fire, stage, durationActions, turnDamage,
                   $"매 턴 최대 체력 {turnDamage * 100f:0.##}% 피해, 받는 치유 −{turnDamage * 100f:0.##}%" + (spreadTo != null ? ", 같은 칸의 적에게 번짐" : ""),
                   StatModifier.Flat(KeyOf(TowerEmblem.Fire), StatType.HealingTaken, -turnDamage))
        {
            m_owner = owner;
            m_turnDamage = turnDamage;
            m_spreadTo = spreadTo;
        }

        // 화염구의 화상: 스택 1로 고정한 식의 값 (0.001 × Power × AbilityValue × 1)%, 그 적이 (레벨 × 2)번 행동하는 동안
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

        // 연소: 같은 칸의 다른 적에게 임시 화상 (스택 1)
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

    // 연소에서 옮겨붙은 화상: 연소인 적과 같은 칸에 있는 동안만. 칸이 갈리거나 그 적이 죽으면 꺼짐. 다시 번지지 않음
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
