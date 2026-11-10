using UnityEngine;

namespace SquareTD
{
    // 바람 디버프 — 윈드 시어 · 터뷸런스 (다음 이동 최소·최대 +5, 이동한 칸 × 값 피해, 모든 아군에게 가속,
    //                                    터뷸런스 최대 Action +3, 바람 타워가 치면 그 타워의 스킬 카운트 +2)
    public class WindDebuffRule : DebuffRule
    {
        public const int GustSpeed = 5;
        public const int TurbulenceAction = 3;
        public const float WindSkillCount = 2f;

        public override void OnApply(SupportCast cast, EnemyController enemy)
        {
            if (!cast.IsSkill2) Acceleration.AddToAll(cast.Amount);
        }

        public override void OnSkill2(SupportCast cast) => Acceleration.AddToAll(cast.Amount);

        public override StatusEffect CreateEffect(SupportCast cast, EnemyController enemy) => new WindDebuffEffect(cast);
    }

    public class WindDebuffEffect : EmblemDebuffEffect
    {
        private bool m_pending = true;

        public WindDebuffEffect(SupportCast cast)
            : base(cast, $"다음 이동 최소·최대 +{WindDebuffRule.GustSpeed}, 이동한 칸 × {cast.Amount:0.##} 피해" + (cast.IsSkill2 ? $", 최대 Action +{WindDebuffRule.TurbulenceAction}" : ""),
                   cast.IsSkill2 ? new[] { StatModifier.Flat(KeyOf(TowerEmblem.Wind), StatType.MaxAction, WindDebuffRule.TurbulenceAction) } : new StatModifier[0])
        {
        }

        public override MoveRange OnDecideMove(EnemyController enemy, MoveRange range)
        {
            if (!m_pending || (range.Min == 0 && range.Max == 0)) return range;
            return new MoveRange(range.Min + WindDebuffRule.GustSpeed, range.Max + WindDebuffRule.GustSpeed, range.Action);
        }

        public override void OnMoved(EnemyController enemy, int steps)
        {
            if (!m_pending) return;
            m_pending = false;
            if (steps == 0) return;
            enemy.TakeDamage(new DamageInfo
            {
                Attacker = Cast.Owner,
                Source = DamageSource.Dot,
                Emblem = TowerEmblem.Wind,
                BaseDamage = Mathf.Abs(steps) * Cast.Amount,
                ForceFixed = true,
            });
        }

        public override void OnDamageTaken(EnemyController enemy, DamageInfo info, float damage)
        {
            if (!IsSkill2 || info.Source != DamageSource.Basic) return;
            TowerController attacker = info.Attacker;
            if (attacker == null || attacker.Definition.Emblem != TowerEmblem.Wind || !attacker.IsAttackTower) return;
            attacker.AdvanceSkillCount(WindDebuffRule.WindSkillCount, enemy);
        }
    }
}
