using System.Collections.Generic;

namespace SquareTD
{
    // 창 디버프 — 래서레이트 · 헤머리지 (받은 피해의 일부가 출혈, 헤머리지 최소·최대 이동력 -1, 찌르기의 출혈 2배)
    public class SpearDebuffRule : DebuffRule
    {
        public const float PierceBleedMultiplier = 2f;

        public override StatusEffect CreateEffect(SupportCast cast, EnemyController enemy)
        {
            return new SpearDebuffEffect(cast, Resist(enemy, cast.Rate));
        }
    }

    public class SpearDebuffEffect : EmblemDebuffEffect
    {
        private readonly float m_bleedRate;

        public SpearDebuffEffect(SupportCast cast, float bleedRate)
            : base(cast, $"받은 피해의 {bleedRate * 100f:0.##}%가 {cast.Duration}턴 출혈" + (cast.IsSkill2 ? ", 최소·최대 이동력 −1, 찌르기의 출혈 2배" : ""), Modifiers(cast))
        {
            m_bleedRate = bleedRate;
        }

        private static StatModifier[] Modifiers(SupportCast cast)
        {
            if (!cast.IsSkill2) return new StatModifier[0];
            string source = KeyOf(TowerEmblem.Spear);
            return new[] { StatModifier.Flat(source, StatType.SpeedMin, -1f), StatModifier.Flat(source, StatType.SpeedMax, -1f) };
        }

        public override void OnDamageTaken(EnemyController enemy, DamageInfo info, float damage)
        {
            if (damage <= 0f || m_bleedRate <= 0f) return;
            if (info.Source == DamageSource.Dot && info.Emblem == TowerEmblem.Spear) return;

            float bleed = damage * m_bleedRate;
            if (IsSkill2 && info.Attribute == AttackAttribute.Pierce) bleed *= SpearDebuffRule.PierceBleedMultiplier;
            BleedEffect.Add(enemy, Cast.Owner, bleed, Cast.Duration);
        }
    }

    // 출혈 — 피해마다 따로 생겨 각자의 턴 수만큼 매 턴 들어감
    public class BleedEffect : StatusEffect
    {
        public const string BleedKey = "출혈";

        private readonly List<(float damage, int turns)> m_bleeds = new List<(float, int)>();
        private TowerController m_owner;

        private BleedEffect() : base(1) { }

        public override string Key => BleedKey;
        public override bool IsPermanent => true;
        public float TurnDamage
        {
            get
            {
                float sum = 0f;
                foreach (var bleed in m_bleeds) sum += bleed.damage;
                return sum;
            }
        }

        public static void Add(EnemyController enemy, TowerController owner, float damage, int turns)
        {
            if (enemy == null || !enemy.IsAlive || damage <= 0f) return;
            if (!(enemy.Status.Find(BleedKey) is BleedEffect effect))
            {
                effect = new BleedEffect();
                enemy.Status.Add(effect);
            }
            effect.m_owner = owner;
            effect.m_bleeds.Add((damage, turns));
        }

        public override void OnTurn(EnemyController enemy)
        {
            float damage = TurnDamage;
            for (int i = m_bleeds.Count - 1; i >= 0; i--)
            {
                (float d, int t) = m_bleeds[i];
                if (t <= 1) m_bleeds.RemoveAt(i);
                else m_bleeds[i] = (d, t - 1);
            }
            if (damage <= 0f) return;
            enemy.TakeDamage(new DamageInfo
            {
                Attacker = m_owner,
                Source = DamageSource.Dot,
                Emblem = TowerEmblem.Spear,
                BaseDamage = damage,
                ForceFixed = true,
            });
        }

        public override string Describe() => $"출혈: 매 턴 {TurnDamage:0.#} 피해 ({m_bleeds.Count}개)";
    }
}
