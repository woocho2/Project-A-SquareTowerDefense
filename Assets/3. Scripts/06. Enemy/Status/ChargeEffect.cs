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

    // 불 — 화상: 받는 치유 -20%, 그 적이 움직인 턴의 디버프 처리 때 최대 체력의 (0.1 × 화상 스택)% 피해, 그 뒤 스택 절반
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
            if (Stacks <= 0f || enemy.Movement == null || !enemy.Movement.ActedThisTurn) return;
            enemy.TakeDamage(new DamageInfo
            {
                Source = DamageSource.Dot,
                Emblem = TowerEmblem.Fire,
                BaseDamage = enemy.MaxHp * TurnRatePerStack * Stacks * TurnMultiplier,
                ForceFixed = true,
            });
            if (enemy.IsAlive) Set(enemy, Mathf.Floor(Stacks / 2f));
        }

        public override string Describe() => $"화상 {(int)Stacks}: 받는 치유 −20%, 움직인 턴에 최대 체력의 {TurnRatePerStack * 100f * Stacks * TurnMultiplier:0.##}% 피해 뒤 절반";
    }

    // 물 — 젖음: 50에 닿으면 다음 행동이 끝날 때까지 받는 피해의 30%가 고정 피해 (기본 저항력만큼 줄어듦), 그 뒤 스택 전부 사라짐
    public class WetEffect : EnemyStackEffect
    {
        public const string WetKey = "젖음";
        public const float TriggerAt = 50f;
        public const float FixedRatio = 0.3f;

        private WetEffect(float stacks) : base(stacks) { }

        public override string Key => WetKey;
        public bool IsReady => FixedDamageRatio > 0f;
        public float FixedDamageRatio { get; private set; }

        public override IReadOnlyList<StatModifier> GetModifiers() => IsReady
            ? new[] { StatModifier.Flat(WetKey, StatType.FixedDamageRatio, FixedDamageRatio) }
            : System.Array.Empty<StatModifier>();

        public static void Add(EnemyController enemy, float amount)
        {
            if (enemy == null || !enemy.IsAlive || amount <= 0f) return;
            if (Find<WetEffect>(enemy) is WetEffect existing) existing.Change(enemy, amount);
            else
            {
                var created = new WetEffect(0f);
                enemy.Status.Add(created);
                created.Change(enemy, amount);
            }
        }

        public static float Get(ITarget target) => Get<WetEffect>(target);

        protected override void OnStacksChanged(EnemyController enemy, float before)
        {
            if (Stacks < TriggerAt) FixedDamageRatio = 0f;
            else if (!IsReady) FixedDamageRatio = enemy.Resistance.ResistWeak(FixedRatio);
        }

        public override void OnMoved(EnemyController enemy, int steps)
        {
            if (IsReady) Set(enemy, 0f);
        }

        public override string Describe() => IsReady ? $"젖음 {(int)Stacks}: 다음 행동이 끝날 때까지 받는 피해의 {FixedDamageRatio * 100f:0.#}%가 고정 피해"
            : $"젖음 {(int)Stacks} / {TriggerAt:0}";
    }

    // 얼음 — 한기: 25 이상이면 최소·최대 이동력 -1, 50 이상이면 빙결 (둘 다 쌓일 때마다 저항 판정), 빙결이 풀리면 스택 전부 사라짐
    public class ChillEffect : EnemyStackEffect
    {
        public const string ChillKey = "한기";
        public const float SlowAt = 25f;
        public const float FreezeAt = 50f;

        private bool m_slowed;

        private ChillEffect(float stacks) : base(stacks) { }

        public override string Key => ChillKey;

        public override IReadOnlyList<StatModifier> GetModifiers() => m_slowed && Stacks >= SlowAt
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

        public static void Clear(EnemyController enemy)
        {
            if (Find<ChillEffect>(enemy) is ChillEffect chill) chill.Set(enemy, 0f);
        }

        protected override void OnStacksChanged(EnemyController enemy, float before)
        {
            if (Stacks < SlowAt) m_slowed = false;
            else if (!m_slowed && Stacks > before) m_slowed = enemy.Resistance.TryStrongCC();
            if (Stacks > before && Stacks >= FreezeAt && !FreezeEffect.IsFrozen(enemy)) enemy.Status.Add(new FreezeEffect());
        }

        public override string Describe() => $"한기 {(int)Stacks}: "
            + (m_slowed && Stacks >= SlowAt ? "최소·최대 이동력 −1, " : Stacks >= SlowAt ? "이동력 감소 저항함, " : "") + $"{FreezeAt:0}에 빙결";
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

    // 빛 — 실명: 10 이상에서 쌓일 때마다 저항 판정, 통하면 다음 행동은 최소·최대 이동력 × -1 (뒤로 걸음), 그 행동이 끝나면 스택 전부 사라짐
    public class BlindEffect : EnemyStackEffect
    {
        public const string BlindKey = "실명";
        public const float TriggerAt = 10f;

        private bool m_reversed;

        private BlindEffect(float stacks) : base(stacks) { }

        public override string Key => BlindKey;
        public bool IsReady { get; private set; }

        public static void Add(EnemyController enemy, float amount)
        {
            if (enemy == null || !enemy.IsAlive || amount <= 0f) return;
            if (Find<BlindEffect>(enemy) is BlindEffect existing) existing.Change(enemy, amount);
            else
            {
                var created = new BlindEffect(0f);
                enemy.Status.Add(created);
                created.Change(enemy, amount);
            }
        }

        public static bool IsBlind(ITarget target) => Get<BlindEffect>(target) > 0f;

        protected override void OnStacksChanged(EnemyController enemy, float before)
        {
            if (Stacks < TriggerAt) IsReady = false;
            else if (!IsReady && Stacks > before) IsReady = enemy.Resistance.TryStrongCC();
        }

        public override MoveRange OnDecideMove(EnemyController enemy, MoveRange range)
        {
            m_reversed = IsReady;
            return m_reversed ? new MoveRange(-range.Max, -range.Min, range.Action) : range;
        }

        public override void OnMoved(EnemyController enemy, int steps)
        {
            if (!m_reversed) return;
            m_reversed = false;
            Set(enemy, 0f);
        }

        public override string Describe() => IsReady ? $"실명 {(int)Stacks}: 다음 행동은 뒤로 걸음"
            : Stacks >= TriggerAt ? $"실명 {(int)Stacks}: 저항함 (더 쌓이면 다시 판정)" : $"실명 {(int)Stacks} / {TriggerAt:0}";
    }
}
