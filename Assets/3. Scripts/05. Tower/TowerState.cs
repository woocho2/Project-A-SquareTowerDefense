using System.Collections.Generic;

namespace SquareTD
{
    // 타워의 바뀌는 상태 (남은 행동, 우선순위, 수정자, 차지, 스킬 카운트, 받은 버프, 시간 버프)
    public class TowerState
    {
        public int RemainingAction { get; set; }
        public int AttackCellIndex { get; set; } = -1;
        public TargetPriority TargetPriority { get; set; }
        public StatModifierList Modifiers { get; } = new StatModifierList();
        public TowerStack Stack { get; }
        public float SkillCount { get; set; }

        public Dictionary<TowerEmblem, SupportBuffEntry> SupportBuffs { get; } = new Dictionary<TowerEmblem, SupportBuffEntry>();

        public Dictionary<string, int> TimedBuffs { get; } = new Dictionary<string, int>();

        public HashSet<string> PersistentSources { get; } = new HashSet<string>();

        public Dictionary<string, float> Counters { get; } = new Dictionary<string, float>();

        public TowerState(TowerDefinition definition)
        {
            RemainingAction = definition.Action;
            TargetPriority = definition.TargetPriority;
            Stack = new TowerStack(definition.Stack);
        }

        public float GetCounter(string key) => Counters.TryGetValue(key, out float value) ? value : 0f;
        public void AddCounter(string key, float amount) => Counters[key] = GetCounter(key) + amount;

        public void CopyInheritedFrom(TowerState old)
        {
            TargetPriority = old.TargetPriority;
            AttackCellIndex = old.AttackCellIndex;
            Stack.CopyFrom(old.Stack);
            SkillCount = old.SkillCount;

            foreach (var pair in old.Counters) Counters[pair.Key] = pair.Value;

            foreach (var pair in old.SupportBuffs)
            {
                SupportBuffs[pair.Key] = pair.Value.Clone();
                CopyModifiers(old, global::SquareTD.SupportBuffs.SourceOf(pair.Key));
            }
            foreach (var pair in old.TimedBuffs)
            {
                TimedBuffs[pair.Key] = pair.Value;
                CopyModifiers(old, pair.Key);
            }
            foreach (string source in old.PersistentSources)
            {
                PersistentSources.Add(source);
                CopyModifiers(old, source);
            }
        }

        private void CopyModifiers(TowerState old, string source)
        {
            foreach (StatModifier modifier in old.Modifiers.All)
            {
                if (modifier.Source == source) Modifiers.Add(modifier);
            }
        }
    }
}
