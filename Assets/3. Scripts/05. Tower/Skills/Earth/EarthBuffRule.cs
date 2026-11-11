using System.Collections.Generic;

namespace SquareTD
{
    // 대지 버프 — 버드 · 블로섬 (AbilityValue, 블로섬 SkillCount -Tier, 대지 타워는 공격마다 성장 +1)
    public class EarthBuffRule : BuffRule
    {
        public override List<StatModifier> GetModifiers(SupportCast cast, TowerController receiver, string source)
        {
            var modifiers = new List<StatModifier> { StatModifier.Percent(source, StatType.AbilityValue, cast.Rate) };
            if (cast.IsSkill2) modifiers.Add(StatModifier.Flat(source, StatType.Duration, -cast.Tier));
            return modifiers;
        }

        public override void OnReceiverActed(SupportCast cast, TowerController receiver)
        {
            if (IsSynergy(cast, receiver)) Growth.Add(receiver, 1f);
        }
    }

    // 대지의 키워드 — 성장: 아군 타워 자신이 가지는 스택 (사라지지 않고 티어 강화로 이어받음)
    public static class Growth
    {
        public const string Key = "성장";

        public static float Get(TowerController tower) => tower != null && tower.State != null ? tower.State.GetCounter(Key) : 0f;

        public static void Add(TowerController tower, float amount)
        {
            if (tower == null || tower.State == null || amount <= 0f) return;
            tower.State.AddCounter(Key, amount);
        }

        public static void AddToOthers(TowerSlots slots, TowerController except, float amount)
        {
            if (slots == null) return;
            for (int i = 0; i < slots.Count; i++)
            {
                TowerController tower = slots.Get(i);
                if (tower != except) Add(tower, amount);
            }
        }
    }
}
