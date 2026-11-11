using UnityEngine;

namespace SquareTD
{
    // 대지 디버프 — 루트 드레인 · 라이프 하베스트 (밟을 때마다 디버프 타워 성장 +1, 최대 체력·방어력 -(값 × 성장)%, 다른 모든 아군에게 양분으로 성장 +값,
    //                                              라이프 하베스트 적응형 능력치 -1, 대지 타워가 치면 그 타워의 성장만큼 추가 피해)
    public class EarthDebuffRule : DebuffRule
    {
        public const int AdaptiveCut = 1;

        public override void OnApply(SupportCast cast, EnemyController enemy)
        {
            if (cast.IsSkill2) return;
            Growth.Add(cast.Owner, 1f);
            Nourish(cast);
        }

        public override void OnSkill2(SupportCast cast) => Nourish(cast);

        private static void Nourish(SupportCast cast)
        {
            if (BoardManager.Instance != null) Growth.AddToOthers(BoardManager.Instance.Slots, cast.Owner, cast.Amount);
        }

        public override StatusEffect CreateEffect(SupportCast cast, EnemyController enemy)
        {
            float cut = Mathf.Min(Resist(enemy, cast.Rate * Growth.Get(cast.Owner)), 0.95f);
            return new EarthDebuffEffect(cast, cut);
        }

        public static int AdaptiveFor(EnemyController enemy, int adaptive)
        {
            if (enemy == null || !EmblemDebuffEffect.HasSkill2(enemy, TowerEmblem.Earth)) return adaptive;
            return Mathf.Max(1, adaptive - AdaptiveCut);
        }
    }

    public class EarthDebuffEffect : EmblemDebuffEffect
    {
        private readonly float m_cut;

        public EarthDebuffEffect(SupportCast cast, float cut)
            : base(cast, $"최대 체력·방어력 −{cut * 100f:0.##}%" + (cast.IsSkill2 ? ", 적응형 능력치 −1, 대지 타워에게 그 타워의 성장만큼 추가 피해" : ""),
                   StatModifier.Percent(KeyOf(TowerEmblem.Earth), StatType.Defense, -cut))
        {
            m_cut = cut;
        }

        public override void OnAdded(EnemyController enemy)
        {
            if (m_cut > 0f) enemy.Health.ScaleMax(1f - m_cut);
            if (IsSkill2 && enemy.Movement != null) enemy.Movement.RefreshFace();
        }

        public override void OnRemoved(EnemyController enemy)
        {
            if (m_cut > 0f) enemy.Health.ScaleMax(1f / (1f - m_cut));
            if (IsSkill2 && enemy.Movement != null) enemy.Movement.RefreshFace();
        }

        public override void OnDamageTaken(EnemyController enemy, DamageInfo info, float damage)
        {
            if (!IsSkill2 || !enemy.IsAlive) return;
            TowerController attacker = info.Attacker;
            if (attacker == null || attacker.Definition.Emblem != TowerEmblem.Earth || !attacker.IsAttackTower) return;
            if (info.Source != DamageSource.Basic && info.Source != DamageSource.Skill) return;

            float extra = Growth.Get(attacker);
            if (extra <= 0f) return;
            enemy.TakeDamage(new DamageInfo { Attacker = attacker, Source = DamageSource.Relay, Emblem = TowerEmblem.Earth, BaseDamage = extra, ForceFixed = true });
        }
    }
}
