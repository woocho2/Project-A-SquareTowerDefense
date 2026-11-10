using System.Collections.Generic;
using UnityEngine;

namespace SquareTD
{
    // 바람 버프 — 테일윈드 · 게일 포스 (최대 Action -1, AttackCount +1, 치명타 확률·피해 교환, 게일 포스 Power +10%,
    //                                    바람 타워는 행동마다 한 번 맞은 적을 ±1칸 밀고 행동력 카운트 -1)
    public class WindBuffRule : BuffRule
    {
        public const float CritRateOffset = 5f;
        public const float CritDamageOffset = 50f;
        public const float GalePower = 0.1f;
        public const string PushReadyKey = "게일 포스 밀어내기";

        public override List<StatModifier> GetModifiers(SupportCast cast, TowerController receiver, string source)
        {
            var modifiers = new List<StatModifier>
            {
                StatModifier.Flat(source, StatType.MaxAction, -1f),
                StatModifier.Flat(source, StatType.AttackCount, 1f),
                StatModifier.Flat(source, StatType.CritRate, (cast.Amount * 0.1f - CritRateOffset) * SkillFormula.PercentRate),
                StatModifier.Flat(source, StatType.CritDamage, (cast.Amount - CritDamageOffset) * SkillFormula.PercentRate),
            };
            if (cast.IsSkill2) modifiers.Add(StatModifier.Percent(source, StatType.Power, GalePower));
            return modifiers;
        }

        public override void OnReceiverActed(SupportCast cast, TowerController receiver)
        {
            if (IsSynergy(cast, receiver)) receiver.State.Counters[PushReadyKey] = 1f;
        }

        public override void OnReceiverHit(SupportCast cast, TowerController receiver, ITarget target)
        {
            if (!IsSynergy(cast, receiver) || receiver.State.GetCounter(PushReadyKey) <= 0f) return;
            EnemyController enemy = target?.Body;
            if (enemy == null || !enemy.IsAlive || enemy.Movement == null) return;

            receiver.State.Counters[PushReadyKey] = 0f;
            if (BoardManager.Instance != null) enemy.Movement.TeleportTo(enemy.PathIndex + (Random.value < 0.5f ? 1 : -1));
            enemy.Movement.AddActionProgress(1);
        }
    }

    // 바람의 키워드 — 가속: 아군 타워 자신이 가지는 스택. 가진 동안 최대 Action -1 (컬러 무관, 사라지지 않음)
    public static class Acceleration
    {
        public const string Key = "가속";

        public static float Get(TowerController tower) => tower != null && tower.State != null ? tower.State.GetCounter(Key) : 0f;
        public static bool IsAccelerated(TowerController tower) => Get(tower) > 0f;

        public static void Add(TowerController tower, float amount)
        {
            if (tower == null || tower.State == null || amount <= 0f) return;
            bool first = !IsAccelerated(tower);
            tower.State.AddCounter(Key, amount);
            if (!first) return;

            tower.State.PersistentSources.Add(Key);
            tower.State.Modifiers.Add(StatModifier.Flat(Key, StatType.MaxAction, -1f));
        }

        public static void AddToAll(float amount)
        {
            if (BoardManager.Instance == null) return;
            TowerSlots slots = BoardManager.Instance.Slots;
            for (int i = 0; i < slots.Count; i++) Add(slots.Get(i), amount);
        }
    }
}
