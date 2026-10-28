namespace SquareTD
{    
    public enum ZoneStage
    {
        None = 0,
        Skill1 = 1,
        Skill2 = 2,
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

        public void AddStack(float amount, int maxStack)
        {
            if (Stage == ZoneStage.Skill2) return;            

            if (amount <= 0f) return;
            Stack = System.Math.Min(Stack + amount, maxStack);

            if (Stack >= maxStack) Stage = ZoneStage.Skill2;            
        }

        // 스택을 타워가 쌓는 문양(방패, 수업 31): 타워의 스택을 그대로 옮겨 적음. 한 번 스킬 2가 되면 그대로
        public void SetStack(float stack, int maxStack)
        {
            if (Stage == ZoneStage.Skill2) return;

            Stack = System.Math.Min(stack, maxStack);
            if (Stack >= maxStack) Stage = ZoneStage.Skill2;
        }

        public void ResetIfSkill1()
        {
            if (Stage == ZoneStage.Skill1) Stack = 0f;
        }
    }
}
