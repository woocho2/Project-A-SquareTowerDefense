namespace SquareTD
{
    public class TowerState
    {
        public int RemainingAction { get; set; }
        public int AttackCellIndex { get; set; } = -1;
        public TargetPriority TargetPriority { get; set; }

        public TowerState(TowerDefinition definition)
        {
            RemainingAction = definition.Action;
            TargetPriority = definition.TargetPriority;
        }

        public void CopyInheritedFrom(TowerState old)
        {
            TargetPriority = old.TargetPriority;
            AttackCellIndex = old.AttackCellIndex;
            // 수업 23: 스킬 2용 스택, 광역의 스킬 주기 카운트 / 수업 31: 시간 버프 / 수업 35: 대지 성장
        }
    }
}
