using System.Collections.Generic;
using UnityEngine;

namespace SquareTD
{
    // 시너지 타워 스킬에 넘기는 판의 상태
    public class SynergyContext
    {
        public IReadOnlyList<EnemyController> Enemies;
        public IReadOnlyList<TowerController> Towers;
        public PathOccupancy Occupancy;
        public BoardGrid Grid;
        public bool IsPlayerTurn;
        public int PathA = -1;
        public int PathB = -1;
    }

    // 시너지 스택 — 매 적 턴 n(티어 합)씩 쌓고 다 차면 멈춤. 스킬을 쓰면 0으로
    public class SynergyStack
    {
        public float MaxStacks { get; }
        public float Stacks { get; private set; }
        public float Ratio => Stacks / MaxStacks;
        public bool IsFull => Stacks >= MaxStacks;

        public SynergyStack(float maxStacks)
        {
            MaxStacks = Mathf.Max(1f, maxStacks);
        }

        public bool Gain(int tierSum)
        {
            if (IsFull) return false;
            Stacks = Mathf.Min(MaxStacks, Stacks + SynergyEffects.TierValue(tierSum));
            return IsFull;
        }

        public void Consume() => Stacks = 0f;

        public void Fill() => Stacks = MaxStacks;
    }

    // 시너지 타워 스킬의 틀 — 매 적 턴 Tick, 다 차면 버튼(TryUse)으로 발동
    public abstract class SynergySkill
    {
        public SynergyStack Stack { get; }
        public abstract int Id { get; }
        public virtual bool IsReady => Stack.IsFull;

        protected SynergySkill(float maxStacks)
        {
            Stack = new SynergyStack(maxStacks);
        }

        public virtual void Tick(SynergyContext context, int tierSum) => Stack.Gain(tierSum);

        public virtual void Fill() => Stack.Fill();

        public virtual bool TryUse(SynergyContext context)
        {
            if (!IsReady || !Cast(context)) return false;
            Stack.Consume();
            return true;
        }

        protected abstract bool Cast(SynergyContext context);
    }

    public static class SynergyEffects
    {
        public const int OdinId = 6001;
        public const int ThorId = 6002;
        public const int LokiId = 6003;
        public const int HelId = 6004;
        public const int SurtrId = 6005;
        public const int FreyId = 6006;
        public const int BaldrId = 6007;
        public const int SkadiId = 6008;
        public const int FreyjaId = 6009;
        public const int YmirId = 6010;
        public const int UllrId = 6011;
        public const int BrynhildId = 6012;
        public const int BrokkrId = 6013;
        public const int NjordId = 6014;
        public const float PercentPerTier = 1f;
        public const int EffectTurns = 5;

        public static float TierValue(int tierSum) => tierSum * PercentPerTier;

        public static SynergySkill Create(int id)
        {
            switch (id)
            {
                case OdinId: return new OdinSkill();
                case ThorId: return new ThorSkill();
                case LokiId: return new LokiSkill();
                case HelId: return new HelSkill();
                case SurtrId: return new SurtrSkill();
                case FreyId: return new FreySkill();
                case BaldrId: return new BaldrSkill();
                case SkadiId: return new SkadiSkill();
                case FreyjaId: return new FreyjaSkill();
                case YmirId: return new YmirSkill();
                case UllrId: return new UllrSkill();
                case BrynhildId: return new BrynhildSkill();
                case BrokkrId: return new BrokkrSkill();
                case NjordId: return new NjordSkill();
                default: return null;
            }
        }

        public static List<EnemyController> Alive(IReadOnlyList<EnemyController> enemies, bool includeBoss = true)
        {
            var alive = new List<EnemyController>();
            if (enemies == null) return alive;
            foreach (EnemyController enemy in enemies)
                if (enemy != null && enemy.IsAlive && (includeBoss || !enemy.IsBoss)) alive.Add(enemy);
            return alive;
        }

        public static void FixedDamage(EnemyController enemy, float damage)
        {
            if (enemy == null || !enemy.IsAlive || damage <= 0f) return;
            enemy.TakeDamage(new DamageInfo { BaseDamage = damage, Source = DamageSource.Dot, ForceFixed = true });
        }

        public static void Execute(EnemyController enemy) => enemy.Execute(new DamageInfo { Source = DamageSource.Dot });
    }

    // 오딘 (1000) — 체력 50% 이하인 일반 적, 10% 이하인 보스를 즉사
    public class OdinSkill : SynergySkill
    {
        public const float MaxStacks = 1000f;
        public const float HpRate = 0.5f;
        public const float BossHpRate = 0.1f;

        public OdinSkill() : base(MaxStacks) { }
        public override int Id => SynergyEffects.OdinId;

        protected override bool Cast(SynergyContext context)
        {
            foreach (EnemyController enemy in SynergyEffects.Alive(context.Enemies))
                if (enemy.CurrentHp <= enemy.MaxHp * (enemy.IsBoss ? BossHpRate : HpRate)) SynergyEffects.Execute(enemy);
            return true;
        }
    }

    // 토르 (250) — 무작위 길 타일 5곳에 번개 폭풍, 5턴 동안 매 적 턴 그 칸의 적에게 최대 체력 4% 고정 피해
    public class ThorSkill : SynergySkill
    {
        public const float MaxStacks = 250f;
        public const int StormCount = 5;
        public const float DamageRate = 0.04f;
        public const int StormTurns = SynergyEffects.EffectTurns;

        private readonly List<(int pathIndex, int turns)> m_storms = new List<(int, int)>();

        public ThorSkill() : base(MaxStacks) { }
        public override int Id => SynergyEffects.ThorId;
        public IReadOnlyList<(int pathIndex, int turns)> Storms => m_storms;

        public override void Tick(SynergyContext context, int tierSum)
        {
            base.Tick(context, tierSum);
            for (int i = m_storms.Count - 1; i >= 0; i--)
            {
                (int index, int turns) = m_storms[i];
                if (context.Occupancy != null)
                    foreach (ITarget target in new List<ITarget>(context.Occupancy.GetAt(index)))
                        if (target != null && target.IsAlive) SynergyEffects.FixedDamage(target.Body, target.MaxHp * DamageRate);
                if (turns <= 1) m_storms.RemoveAt(i);
                else m_storms[i] = (index, turns - 1);
            }
        }

        protected override bool Cast(SynergyContext context)
        {
            if (context.Grid == null || context.Grid.PathCount == 0) return false;
            var pool = new List<int>();
            for (int i = 0; i < context.Grid.PathCount; i++) pool.Add(i);
            for (int n = 0; n < StormCount && pool.Count > 0; n++)
            {
                int pick = Random.Range(0, pool.Count);
                m_storms.Add((pool[pick], StormTurns));
                pool.RemoveAt(pick);
            }
            return true;
        }
    }

    // 로키 (250) — 플레이어 턴에 길 타일 A와 B의 적을 서로 바꿈. 옮겨진 적은 행동 카운트가 처음부터, 도착한 칸의 존·타일을 바로 밟음
    public class LokiSkill : SynergySkill
    {
        public const float MaxStacks = 250f;

        public LokiSkill() : base(MaxStacks) { }
        public override int Id => SynergyEffects.LokiId;

        public static bool CanSwap(SynergyContext context) =>
            context != null && context.IsPlayerTurn && context.Grid != null && context.PathA >= 0 && context.PathB >= 0 &&
            context.PathA != context.PathB && context.PathA < context.Grid.PathCount && context.PathB < context.Grid.PathCount;

        public static (List<EnemyController> toB, List<EnemyController> toA) Plan(PathOccupancy occupancy, int a, int b)
        {
            var toB = new List<EnemyController>();
            var toA = new List<EnemyController>();
            foreach (ITarget target in occupancy.GetAt(a)) if (target?.Body != null && target.IsAlive) toB.Add(target.Body);
            foreach (ITarget target in occupancy.GetAt(b)) if (target?.Body != null && target.IsAlive) toA.Add(target.Body);
            return (toB, toA);
        }

        protected override bool Cast(SynergyContext context)
        {
            if (!CanSwap(context) || context.Occupancy == null) return false;
            (List<EnemyController> toB, List<EnemyController> toA) = Plan(context.Occupancy, context.PathA, context.PathB);
            foreach (EnemyController enemy in toB) Move(enemy, context.PathB);
            foreach (EnemyController enemy in toA) Move(enemy, context.PathA);
            return true;
        }

        private static void Move(EnemyController enemy, int pathIndex)
        {
            if (enemy.Movement == null) return;
            enemy.Movement.TeleportTo(pathIndex);
            enemy.Movement.ResetActionProgress();
            EnemyTileEffectApplier.ApplyForcedStop(enemy);
        }
    }

    // 헬 (1000) — 보스를 뺀 적 전체 즉사
    public class HelSkill : SynergySkill
    {
        public const float MaxStacks = 1000f;

        public HelSkill() : base(MaxStacks) { }
        public override int Id => SynergyEffects.HelId;

        protected override bool Cast(SynergyContext context)
        {
            foreach (EnemyController enemy in SynergyEffects.Alive(context.Enemies, includeBoss: false)) SynergyEffects.Execute(enemy);
            return true;
        }
    }

    // 수르트 (500) — 맵 위의 적 전체(보스 포함)에게 잃은 체력의 10% 고정 피해
    public class SurtrSkill : SynergySkill
    {
        public const float MaxStacks = 500f;
        public const float LostHpRate = 0.1f;

        public SurtrSkill() : base(MaxStacks) { }
        public override int Id => SynergyEffects.SurtrId;

        protected override bool Cast(SynergyContext context)
        {
            foreach (EnemyController enemy in SynergyEffects.Alive(context.Enemies))
                SynergyEffects.FixedDamage(enemy, (enemy.MaxHp - enemy.CurrentHp) * LostHpRate);
            return true;
        }
    }

    // 프레이 (500) — 판 위의 모든 타워(버프·디버프 포함)가 다음 행동 5번 동안 AttackCount +1
    public class FreySkill : SynergySkill
    {
        public const float MaxStacks = 500f;
        public const string Source = "프레이의 가호";
        public const int Actions = SynergyEffects.EffectTurns;

        public FreySkill() : base(MaxStacks) { }
        public override int Id => SynergyEffects.FreyId;

        protected override bool Cast(SynergyContext context)
        {
            if (context.Towers == null) return false;
            foreach (TowerController tower in context.Towers)
            {
                if (tower == null || tower.Definition == null || TowerId.IsSynergy(tower.Definition.Id)) continue;
                TimedTowerBuff.Apply(tower.State, Source, Actions, StatModifier.Flat(Source, StatType.AttackCount, 1f));
            }
            return true;
        }
    }

    // 발드르 (500) — 다 차면 무력화 1회 (최대 2회, 2회면 더 쌓지 않음). 버튼으로 쓰면 그 뒤 처음 발동하는 보스 기믹 한 번을 막음
    public class BaldrSkill : SynergySkill
    {
        public const float MaxStacks = 500f;
        public const int MaxCharges = 2;

        public BaldrSkill() : base(MaxStacks) { }
        public override int Id => SynergyEffects.BaldrId;
        public int Charges { get; private set; }
        public int Pending { get; private set; }
        public override bool IsReady => Charges > 0;

        public override void Fill() => Charges = MaxCharges;

        public override void Tick(SynergyContext context, int tierSum)
        {
            if (Charges >= MaxCharges || !Stack.Gain(tierSum)) return;
            Stack.Consume();
            Charges++;
        }

        public override bool TryUse(SynergyContext context)
        {
            if (!IsReady) return false;
            Charges--;
            Pending++;
            return true;
        }

        protected override bool Cast(SynergyContext context) => false;

        public bool TryBlockMechanic()
        {
            if (Pending <= 0) return false;
            Pending--;
            return true;
        }
    }

    // 스카디 (250) — 맵 위의 적 전체(보스 포함)가 5번 행동하는 동안 최소·최대 이동력 −5
    public class SkadiSkill : SynergySkill
    {
        public const float MaxStacks = 250f;
        public const string Key = "스카디의 혹한";
        public const int Actions = SynergyEffects.EffectTurns;
        public const float SpeedCut = 5f;

        public SkadiSkill() : base(MaxStacks) { }
        public override int Id => SynergyEffects.SkadiId;

        protected override bool Cast(SynergyContext context)
        {
            foreach (EnemyController enemy in SynergyEffects.Alive(context.Enemies))
            {
                if (enemy.Status == null) continue;
                enemy.Status.Add(new TimedModifierEffect(Key, Actions, SpeedCut, $"최소·최대 이동력 −{SpeedCut:0}",
                    StatModifier.Flat(Key, StatType.SpeedMin, -SpeedCut), StatModifier.Flat(Key, StatType.SpeedMax, -SpeedCut)));
            }
            return true;
        }
    }

    // 이미르 (1000) — 맵 위의 적 전체(보스 포함)가 다음 행동 5번 동안 행동 불가
    public class YmirSkill : SynergySkill
    {
        public const float MaxStacks = 1000f;
        public const int Actions = SynergyEffects.EffectTurns;

        public YmirSkill() : base(MaxStacks) { }
        public override int Id => SynergyEffects.YmirId;

        protected override bool Cast(SynergyContext context)
        {
            foreach (EnemyController enemy in SynergyEffects.Alive(context.Enemies))
                if (enemy.Status != null) enemy.Status.Add(new YmirFrozenEffect(Actions));
            return true;
        }
    }

    // 이미르의 동결 — 행동 N번 동안 0 / 0 (저항력과 기존 행동 불가와 무관하게 걸림)
    public class YmirFrozenEffect : StatusEffect
    {
        public const string FrozenKey = "이미르의 동결";

        public YmirFrozenEffect(int actions) : base(actions) { }

        public override string Key => FrozenKey;
        public override MoveRange OnDecideMove(EnemyController enemy, MoveRange range) => new MoveRange(0, 0, range.Action);
        public override string Describe() => $"이미르의 동결: 행동 불가 ({RemainingActions}회 행동 남음)";
    }

    // 울르 (750) — 판 위의 모든 타워가 다음 행동 5번 동안, 그 타워의 공격 피해마다 50% 추가타
    public class UllrSkill : SynergySkill
    {
        public const float MaxStacks = 750f;
        public const string Source = "울르의 축복";
        public const int Actions = SynergyEffects.EffectTurns;
        public const float FollowUpRate = 0.5f;

        public UllrSkill() : base(MaxStacks) { }
        public override int Id => SynergyEffects.UllrId;

        protected override bool Cast(SynergyContext context)
        {
            if (context.Towers == null) return false;
            foreach (TowerController tower in context.Towers)
                if (tower != null && tower.Definition != null && !TowerId.IsSynergy(tower.Definition.Id))
                    TimedTowerBuff.Apply(tower.State, Source, Actions);
            return true;
        }

        public static bool TryFollowUp(EnemyController enemy, DamageInfo info, float damage)
        {
            TowerController attacker = info.Attacker;
            if (enemy == null || !enemy.IsAlive || damage <= 0f || attacker == null || attacker.State == null) return false;
            if (info.Source != DamageSource.Basic && info.Source != DamageSource.Skill && info.Source != DamageSource.Ground) return false;
            if (TimedTowerBuff.GetRemaining(attacker.State, Source) <= 0) return false;

            enemy.TakeDamage(new DamageInfo
            {
                Attacker = attacker,
                Source = DamageSource.Relay,
                Emblem = info.Emblem,
                Attribute = info.Attribute,
                BaseDamage = damage * FollowUpRate,
                ForceFixed = true,
            });
            return true;
        }
    }

    // 브륀힐드 (1000) — 적 수 한도로 인한 게임 오버를 한 번 막음 (보스 제한 턴은 막지 않음)
    public class BrynhildSkill : SynergySkill
    {
        public const float MaxStacks = 1000f;

        public BrynhildSkill() : base(MaxStacks) { }
        public override int Id => SynergyEffects.BrynhildId;

        protected override bool Cast(SynergyContext context)
        {
            EmblemSharedState.Current.AddLimitShield();
            return true;
        }
    }

    // 브록 & 에이트리 (750) — 판 위 일반 타워의 티어 합 × 200골드
    public class BrokkrSkill : SynergySkill
    {
        public const float MaxStacks = 750f;
        public const int GoldPerTier = 200;

        public BrokkrSkill() : base(MaxStacks) { }
        public override int Id => SynergyEffects.BrokkrId;

        public static int GoldAmount(IReadOnlyList<TowerController> towers)
        {
            int tiers = 0;
            if (towers != null)
                foreach (TowerController tower in towers)
                    if (tower != null && tower.Definition != null && !TowerId.IsSynergy(tower.Definition.Id)) tiers += tower.Definition.Tier;
            return tiers * GoldPerTier;
        }

        protected override bool Cast(SynergyContext context)
        {
            int gold = GoldAmount(context.Towers);
            if (gold > 0 && CurrencyManager.Instance != null) CurrencyManager.Instance.AddGold(gold);
            return true;
        }
    }

    // 뇨르드 (250) — 플레이어 턴에 고른 길 타일 하나가 5턴 동안 멈춤 칸: 이동하던 적은 그 칸을 넘지 못하고 그 칸에 섬
    public class NjordSkill : SynergySkill
    {
        public const float MaxStacks = 250f;
        public const int Turns = SynergyEffects.EffectTurns;

        public NjordSkill() : base(MaxStacks) { }
        public override int Id => SynergyEffects.NjordId;

        public override void Tick(SynergyContext context, int tierSum)
        {
            base.Tick(context, tierSum);
            EmblemSharedState.Current.CountDownStopTiles();
        }

        protected override bool Cast(SynergyContext context)
        {
            if (context == null || !context.IsPlayerTurn || context.Grid == null) return false;
            if (context.PathA < 0 || context.PathA >= context.Grid.PathCount) return false;
            EmblemSharedState.Current.AddStopTile(context.PathA, Turns);
            return true;
        }
    }

    // 프레이야 (500) — 5턴 동안 처치 골드 2배
    public class FreyjaSkill : SynergySkill
    {
        public const float MaxStacks = 500f;
        public const int Turns = SynergyEffects.EffectTurns;

        public FreyjaSkill() : base(MaxStacks) { }
        public override int Id => SynergyEffects.FreyjaId;

        public override void Tick(SynergyContext context, int tierSum)
        {
            base.Tick(context, tierSum);
            EmblemSharedState.Current.CountDownGoldDouble();
        }

        protected override bool Cast(SynergyContext context)
        {
            EmblemSharedState.Current.StartGoldDouble(Turns);
            return true;
        }
    }
}
