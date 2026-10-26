namespace SquareTD
{
    public class TowerState
    {
        public int RemainingAction { get; set; }
        public int AttackCellIndex { get; set; } = -1;
        public TargetPriority TargetPriority { get; set; }
        public StatModifierList Modifiers { get; } = new StatModifierList();
        public TowerStack Stack { get; }
        public int SkillTurnCount { get; set; }

        // 받고 있는 존 버프: 존 → 그 버프 수정자의 출처 이름. ⓪에서 존을 벗어났으면 이 출처로 뗌 (BuffZoneAction.RefreshReceived)
        public System.Collections.Generic.Dictionary<Zone, string> ReceivedZoneBuffs { get; } = new System.Collections.Generic.Dictionary<Zone, string>();

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
            SkillTurnCount = old.SkillTurnCount;

            // 받던 존 버프도 이어받음 — 같은 자리라 새로 존에 올라온 것으로 치지 않음 (20번 8장 [티어 강화])
            foreach (var pair in old.ReceivedZoneBuffs)
            {
                ReceivedZoneBuffs[pair.Key] = pair.Value;
                foreach (StatModifier modifier in old.Modifiers.All)
                {
                    if (modifier.Source == pair.Value) Modifiers.Add(modifier);
                }
            }
            // 수업 31: 시간 버프 / 수업 35: 대지 성장
        }
    }
}
