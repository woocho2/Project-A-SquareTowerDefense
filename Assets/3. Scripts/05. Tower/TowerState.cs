namespace SquareTD
{
    // 타워의 바뀌는 상태 (남은 행동, 우선순위, 수정자, 스택, 적중 수, 시간 버프)
    public class TowerState
    {
        public int RemainingAction { get; set; }
        public int AttackCellIndex { get; set; } = -1;
        public TargetPriority TargetPriority { get; set; }
        public StatModifierList Modifiers { get; } = new StatModifierList();
        public TowerStack Stack { get; }
        public int SkillHitCount { get; set; }

        public System.Collections.Generic.Dictionary<Zone, string> ReceivedZoneBuffs { get; } = new System.Collections.Generic.Dictionary<Zone, string>();

        public System.Collections.Generic.Dictionary<string, int> TimedBuffs { get; } = new System.Collections.Generic.Dictionary<string, int>();

        public System.Collections.Generic.HashSet<string> PersistentSources { get; } = new System.Collections.Generic.HashSet<string>();

        public TowerState(TowerDefinition definition)
        {
            RemainingAction = definition.Action;
            TargetPriority = definition.TargetPriority;
            Stack = new TowerStack(definition.Stack);
        }

        public void CopyInheritedFrom(TowerState old)
        {
            TargetPriority = old.TargetPriority;
            AttackCellIndex = old.AttackCellIndex;
            Stack.CopyFrom(old.Stack);
            SkillHitCount = old.SkillHitCount;

            foreach (var pair in old.ReceivedZoneBuffs)
            {
                ReceivedZoneBuffs[pair.Key] = pair.Value;
                foreach (StatModifier modifier in old.Modifiers.All)
                {
                    if (modifier.Source == pair.Value) Modifiers.Add(modifier);
                }
            }
            foreach (var pair in old.TimedBuffs)
            {
                TimedBuffs[pair.Key] = pair.Value;
                foreach (StatModifier modifier in old.Modifiers.All)
                {
                    if (modifier.Source == pair.Key) Modifiers.Add(modifier);
                }
            }
            foreach (string source in old.PersistentSources)
            {
                PersistentSources.Add(source);
                foreach (StatModifier modifier in old.Modifiers.All)
                {
                    if (modifier.Source == source) Modifiers.Add(modifier);
                }
            }
        }
    }
}
