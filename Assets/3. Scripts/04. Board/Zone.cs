namespace SquareTD
{
    // 버프존·디버프존 하나 (주인, 자리, 스택, 단계)
    public enum ZoneStage
    {
        None = 0,
        Skill1 = 1,
        Skill2 = 2,
        Discharge = 3,
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

        public int StageTurnsLeft { get; private set; }

        public void StartStageTimer(int turns) => StageTurnsLeft = System.Math.Max(1, turns);

        public bool TickStageTimer()
        {
            if (StageTurnsLeft <= 0) return false;
            StageTurnsLeft--;
            return StageTurnsLeft == 0;
        }

        public void ResetToSkill1()
        {
            Stage = ZoneStage.Skill1;
            Stack = 0f;
            StageTurnsLeft = 0;
        }

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
