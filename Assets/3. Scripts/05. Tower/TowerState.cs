namespace SquareTD
{
    public class TowerState
    {
        public int RemainingAction { get; set; }
        public int AttackCellIndex { get; set; } = -1;
        public TargetPriority TargetPriority { get; set; }
        public StatModifierList Modifiers { get; } = new StatModifierList();
        public TowerStack Stack { get; }
        public int SkillHitCount { get; set; }      // 공격 타워: 스킬 2까지 센 적중 수 (광역·단일 공통, 수업 28)

        // 받고 있는 존 버프: 존 → 그 버프 수정자의 출처 이름. ⓪에서 존을 벗어났으면 이 출처로 뗌 (BuffZoneAction.RefreshReceived)
        public System.Collections.Generic.Dictionary<Zone, string> ReceivedZoneBuffs { get; } = new System.Collections.Generic.Dictionary<Zone, string>();

        // 스킬이 자기에게 건 시간 버프: 출처 → 남은 행동 수 (TimedTowerBuff, 수업 28 광폭화)
        public System.Collections.Generic.Dictionary<string, int> TimedBuffs { get; } = new System.Collections.Generic.Dictionary<string, int>();

        // 타워가 판 위에 있는 동안 계속 남는 자기 강화의 출처 (대지의 분노·씨뿌리기의 성장, 수업 35). 티어 강화해도 이어받음
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

            // 받던 존 버프도 이어받음 — 같은 자리라 새로 존에 올라온 것으로 치지 않음 (20번 8장 [티어 강화])
            foreach (var pair in old.ReceivedZoneBuffs)
            {
                ReceivedZoneBuffs[pair.Key] = pair.Value;
                foreach (StatModifier modifier in old.Modifiers.All)
                {
                    if (modifier.Source == pair.Value) Modifiers.Add(modifier);
                }
            }
            // 스킬이 자기에게 건 시간 버프(광폭화 …)도 남은 행동 수와 함께 (20번 8장 [티어 강화])
            foreach (var pair in old.TimedBuffs)
            {
                TimedBuffs[pair.Key] = pair.Value;
                foreach (StatModifier modifier in old.Modifiers.All)
                {
                    if (modifier.Source == pair.Key) Modifiers.Add(modifier);
                }
            }
            // 쌓인 자기 강화(대지 성장)도 그대로 (20번 4-11장)
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
