namespace SquareTD
{    
    public enum ZoneStage
    {
        None = 0,
        Skill1 = 1,
        Skill2 = 2,
        Discharge = 3,      // 전기 방전: 아무 효과 없이 5턴 쉬는 단계 (수업 38)
    }

    public class Zone
    {
        public TowerController Owner { get; }
        public bool IsBuff { get; }
        public TowerEmblem Emblem => Owner.Definition.Emblem;

        public int CellIndex { get; private set; } = -1;
        public bool IsPlaced => CellIndex >= 0;

        public float Stack { get; private set; }
        public ZoneStage Stage { get; private set; } = ZoneStage.Skill1;

        public Zone(TowerController owner, bool isBuff)
        {
            Owner = owner;
            IsBuff = isBuff;
        }

        public void PlaceAt(int cellIndex) => CellIndex = cellIndex;

        // 단계가 도는 문양 (불의 불길, 전기의 과충전·방전, 수업 33·38): 그 단계가 남은 턴. 0이면 아직 시작하지 않음
        public int StageTurnsLeft { get; private set; }

        public void StartStageTimer(int turns) => StageTurnsLeft = System.Math.Max(1, turns);

        // 한 턴 줄임. 이번에 0이 되었으면 true
        public bool TickStageTimer()
        {
            if (StageTurnsLeft <= 0) return false;
            StageTurnsLeft--;
            return StageTurnsLeft == 0;
        }

        // 불길이 끝남 → 0스택의 불씨 / 방전이 끝남 → 0스택의 충전
        public void ResetToSkill1()
        {
            Stage = ZoneStage.Skill1;
            Stack = 0f;
            StageTurnsLeft = 0;
        }

        // 과충전이 끝남 → 방전 (스택도 쌓지 않음)
        public void StartDischarge(int turns)
        {
            Stage = ZoneStage.Discharge;
            Stack = 0f;
            StartStageTimer(turns);
        }

        public void AddStack(float amount, int maxStack)
        {
            if (Stage == ZoneStage.Skill2 || Stage == ZoneStage.Discharge) return;

            if (amount <= 0f) return;
            Stack = System.Math.Min(Stack + amount, maxStack);

            if (Stack >= maxStack) Stage = ZoneStage.Skill2;            
        }

        // 스택을 타워가 쌓는 문양(방패, 수업 31): 타워의 스택을 그대로 옮겨 적음. 한 번 스킬 2가 되면 그대로
        public void SetStack(float stack, int maxStack)
        {
            if (Stage == ZoneStage.Skill2 || Stage == ZoneStage.Discharge) return;

            Stack = System.Math.Min(stack, maxStack);
            if (Stack >= maxStack) Stage = ZoneStage.Skill2;
        }

        public void ResetIfSkill1()
        {
            if (Stage == ZoneStage.Skill1) Stack = 0f;
        }
    }
}
