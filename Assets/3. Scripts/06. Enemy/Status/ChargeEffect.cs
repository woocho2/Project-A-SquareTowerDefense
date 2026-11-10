using System.Collections.Generic;
using UnityEngine;

namespace SquareTD
{
    // 적이 가지는 원소 키워드 스택의 베이스 (사라지지 않음. 문양별 장에 적힌 일로만 줄어듦)
    public abstract class EnemyStackEffect : StatusEffect
    {
        public float Stacks { get; protected set; }

        protected EnemyStackEffect(float stacks) : base(1)
        {
            Stacks = stacks;
        }

        public override bool IsPermanent => true;
        public override float Strength => Stacks;

        public static T Find<T>(ITarget target) where T : EnemyStackEffect
        {
            EnemyStatusEffects status = target?.Body != null ? target.Body.Status : target?.Status;
            if (status == null) return null;
            foreach (StatusEffect effect in status.Effects) if (effect is T stack) return stack;
            return null;
        }

        public static float Get<T>(ITarget target) where T : EnemyStackEffect => Find<T>(target)?.Stacks ?? 0f;

        public void Change(EnemyController enemy, float amount)
        {
            float before = Stacks;
            Stacks = Mathf.Max(0f, Stacks + amount);
            OnStacksChanged(enemy, before);
            enemy.Status.Refresh();
        }

        public void Set(EnemyController enemy, float stacks) => Change(enemy, stacks - Stacks);

        protected virtual void OnStacksChanged(EnemyController enemy, float before) { }
    }

    // 전기 — 전하: 받는 피해 +(전하 × 0.01)%
    public class ChargeEffect : EnemyStackEffect
    {
        public const string ChargeKey = "전하";
        public const float DamageTakenPerStack = 0.0001f;

        private ChargeEffect(float charge) : base(charge) { }

        public float Charge => Stacks;
        public override string Key => ChargeKey;

        public override IReadOnlyList<StatModifier> GetModifiers() => Stacks > 0f
            ? new[] { StatModifier.Flat(ChargeKey, StatType.DamageTaken, Stacks * DamageTakenPerStack) }
            : System.Array.Empty<StatModifier>();

        public static void Add(EnemyController enemy, float amount)
        {
            if (enemy == null || !enemy.IsAlive || amount <= 0f) return;
            if (Find<ChargeEffect>(enemy) is ChargeEffect existing) existing.Change(enemy, amount);
            else enemy.Status.Add(new ChargeEffect(amount));
        }

        public static float Get(ITarget target) => Get<ChargeEffect>(target);

        public static void Clear(ITarget target)
        {
            if (target?.Body != null && Find<ChargeEffect>(target) is ChargeEffect charge) charge.Set(target.Body, 0f);
        }

        public override string Describe() => $"전하 {(int)Stacks}: 받는 피해 +{Stacks * DamageTakenPerStack * 100f:0.##}%";
    }

    // 불 — 화상: 받는 치유 -20%, 매 턴 최대 체력의 (0.1 × 화상 스택)% 피해
    public class BurnEffect : EnemyStackEffect
    {
        public const string BurnKey = "화상";
        public const float HealingCut = 0.2f;
        public const float TurnRatePerStack = 0.001f;

        public const float InfernoTurnMultiplier = 2f;

        private BurnEffect(float stacks) : base(stacks) { }

        private float TurnMultiplier => Owner != null && EmblemDebuffEffect.HasSkill2(Owner, TowerEmblem.Fire) ? InfernoTurnMultiplier : 1f;
        private EnemyController Owner { get; set; }

        public override void OnAdded(EnemyController enemy) => Owner = enemy;

        public override string Key => BurnKey;

        public override IReadOnlyList<StatModifier> GetModifiers() => new[] { StatModifier.Flat(BurnKey, StatType.HealingTaken, -HealingCut) };

        public static void Add(EnemyController enemy, float amount)
        {
            if (enemy == null || !enemy.IsAlive || amount <= 0f) return;
            if (Find<BurnEffect>(enemy) is BurnEffect existing) existing.Change(enemy, amount);
            else enemy.Status.Add(new BurnEffect(amount));
        }

        public static float Get(ITarget target) => Get<BurnEffect>(target);

        public override void OnTurn(EnemyController enemy)
        {
            if (Stacks <= 0f) return;
            enemy.TakeDamage(new DamageInfo
            {
                Source = DamageSource.Dot,
                Emblem = TowerEmblem.Fire,
                BaseDamage = enemy.MaxHp * TurnRatePerStack * Stacks * TurnMultiplier,
                ForceFixed = true,
            });
        }

        public override string Describe() => $"화상 {(int)Stacks}: 받는 치유 −20%, 매 턴 최대 체력의 {TurnRatePerStack * 100f * Stacks * TurnMultiplier:0.##}% 피해";
    }

    // 물 — 젖음: 최대 이동력 -(젖음 × 0.1) (소수 버림)
    public class WetEffect : EnemyStackEffect
    {
        public const string WetKey = "젖음";
        public const float SpeedPerStack = 0.1f;

        private WetEffect(float stacks) : base(stacks) { }

        public override string Key => WetKey;
        public int SpeedCut => Mathf.FloorToInt(Stacks * SpeedPerStack + 0.0001f);

        public override IReadOnlyList<StatModifier> GetModifiers() => SpeedCut > 0
            ? new[] { StatModifier.Flat(WetKey, StatType.SpeedMax, -SpeedCut) }
            : System.Array.Empty<StatModifier>();

        public static void Add(EnemyController enemy, float amount)
        {
            if (enemy == null || !enemy.IsAlive || amount <= 0f) return;
            if (Find<WetEffect>(enemy) is WetEffect existing) existing.Change(enemy, amount);
            else enemy.Status.Add(new WetEffect(amount));
        }

        public static float Get(ITarget target) => Get<WetEffect>(target);

        public override string Describe() => $"젖음 {(int)Stacks}: 최대 이동력 −{SpeedCut}";
    }

    // 얼음 — 한기: 최소·최대 이동력 -1, 50의 배수에 닿을 때마다 빙결
    public class ChillEffect : EnemyStackEffect
    {
        public const string ChillKey = "한기";
        public const float FreezeEvery = 50f;

        private ChillEffect(float stacks) : base(stacks) { }

        public override string Key => ChillKey;

        public override IReadOnlyList<StatModifier> GetModifiers() => Stacks > 0f
            ? new[] { StatModifier.Flat(ChillKey, StatType.SpeedMin, -1f), StatModifier.Flat(ChillKey, StatType.SpeedMax, -1f) }
            : System.Array.Empty<StatModifier>();

        public static void Add(EnemyController enemy, float amount)
        {
            if (enemy == null || !enemy.IsAlive || amount <= 0f) return;
            if (Find<ChillEffect>(enemy) is ChillEffect existing) existing.Change(enemy, amount);
            else
            {
                var created = new ChillEffect(0f);
                enemy.Status.Add(created);
                created.Change(enemy, amount);
            }
        }

        public static float Get(ITarget target) => Get<ChillEffect>(target);
        public static bool IsChilled(ITarget target) => Get(target) > 0f;

        protected override void OnStacksChanged(EnemyController enemy, float before)
        {
            if (Mathf.FloorToInt(Stacks / FreezeEvery) > Mathf.FloorToInt(before / FreezeEvery)) enemy.Status.Add(new FreezeEffect());
        }

        public override string Describe() => $"한기 {(int)Stacks}: 최소·최대 이동력 −1, 50마다 빙결";
    }

    // 어둠 — 침식: 따로 걸리는 디버프 없음. 죽으면 침식 스택만큼 판 공용 침식 카운트 +
    public class ErosionEffect : EnemyStackEffect
    {
        public const string ErosionKey = "침식";

        private ErosionEffect(float stacks) : base(stacks) { }

        public override string Key => ErosionKey;

        public static void Add(EnemyController enemy, float amount)
        {
            if (enemy == null || !enemy.IsAlive || amount <= 0f) return;
            if (Find<ErosionEffect>(enemy) is ErosionEffect existing) existing.Change(enemy, amount);
            else enemy.Status.Add(new ErosionEffect(amount));
        }

        public static float Get(ITarget target) => Get<ErosionEffect>(target);

        public override void OnDeath(EnemyController enemy, KillInfo kill) => EmblemSharedState.Current.AddDarkness(Stacks);

        public override string Describe() => $"침식 {(int)Stacks}: 죽으면 침식 카운트 +{(int)Stacks}";
    }

    // 빛 — 실명: 적 행동 3번 동안 최소·최대 이동력 × -1 (뒤로 걸음)
    public class BlindEffect : StatusEffect
    {
        public const string BlindKey = "실명";
        public const int BlindActions = 3;

        public float Stacks { get; private set; }

        private BlindEffect(float stacks) : base(BlindActions)
        {
            Stacks = stacks;
        }

        public override string Key => BlindKey;
        public override float Strength => Stacks;

        public static void Add(EnemyController enemy, float amount)
        {
            if (enemy == null || !enemy.IsAlive || amount <= 0f) return;
            float stacks = amount + (enemy.Status.Find(BlindKey) is BlindEffect existing ? existing.Stacks : 0f);
            enemy.Status.Remove(BlindKey);
            enemy.Status.Add(new BlindEffect(stacks));
        }

        public static bool IsBlind(ITarget target)
        {
            EnemyStatusEffects status = target?.Body != null ? target.Body.Status : target?.Status;
            return status != null && status.Has(BlindKey);
        }

        public override MoveRange OnDecideMove(EnemyController enemy, MoveRange range) => new MoveRange(-range.Max, -range.Min, range.Action);

        public override string Describe() => $"실명 {(int)Stacks}: 뒤로 걸음 ({RemainingActions}회 행동 남음)";
    }
}
