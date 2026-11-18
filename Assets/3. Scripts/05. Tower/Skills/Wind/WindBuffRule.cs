using System.Collections.Generic;
using UnityEngine;

namespace SquareTD
{
    // 바람 버프 — 테일윈드 · 게일 포스 (최대 Action -1, AttackCount +1, 치명타 확률·피해 교환, 게일 포스 Power +10%,
    //                                    바람 타워는 행동마다 한 번 맞은 적을 ±1칸 밀고 행동력 카운트 -1)
    public class WindBuffRule : BuffRule
    {
        public const float BronzeCritRate = -0.06f;
        public const float BronzeCritDamage = -0.2f;
        public const float GalePower = 0.1f;
        public const string PushReadyKey = "게일 포스 밀어내기";

        public override List<StatModifier> GetModifiers(SupportCast cast, TowerController receiver, string source)
        {
            var modifiers = new List<StatModifier>
            {
                StatModifier.Flat(source, StatType.MaxAction, -1f),
                StatModifier.Flat(source, StatType.AttackCount, 1f),
                StatModifier.Flat(source, StatType.CritRate, cast.Penalty(BronzeCritRate)),
                StatModifier.Flat(source, StatType.CritDamage, cast.Penalty(BronzeCritDamage)),
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

    // 바람의 키워드 — 가속: 아군 타워의 스택. 20이 차면 다음 행동 한 번 최대 Action -1 (그동안 더 쌓이지 않음, 넘친 것은 버림)
    public static class Acceleration
    {
        public const string Key = "가속";
        public const string ReadyKey = "가속 버프";
        public const float Threshold = 20f;

        public static float Get(TowerController tower) => tower != null && tower.State != null ? tower.State.GetCounter(Key) : 0f;
        public static bool IsAccelerated(TowerController tower) => tower != null && tower.State != null && tower.State.GetCounter(ReadyKey) > 0f;

        public static void Add(TowerController tower, float amount)
        {
            if (tower == null || tower.State == null || amount <= 0f || IsAccelerated(tower)) return;
            tower.State.AddCounter(Key, amount);
            if (Get(tower) < Threshold) return;

            tower.State.Counters[Key] = 0f;
            tower.State.Counters[ReadyKey] = 1f;
            tower.State.PersistentSources.Add(Key);
            tower.State.Modifiers.Add(StatModifier.Flat(Key, StatType.MaxAction, -1f));
        }

        public static void Consume(TowerController tower)
        {
            if (!IsAccelerated(tower)) return;
            tower.State.Counters[ReadyKey] = 0f;
            tower.State.PersistentSources.Remove(Key);
            tower.State.Modifiers.RemoveBySource(Key);
        }

        public static void AddToAll(float amount)
        {
            if (BoardManager.Instance == null) return;
            TowerSlots slots = BoardManager.Instance.Slots;
            for (int i = 0; i < slots.Count; i++) Add(slots.Get(i), amount);
        }
    }
}
