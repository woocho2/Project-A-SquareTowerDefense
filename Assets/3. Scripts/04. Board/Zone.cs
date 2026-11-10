namespace SquareTD
{
    // 버프존·디버프존 하나 (주인, 자리). 존은 스킬 1이 걸리는 자리일 뿐임
    public class Zone
    {
        public TowerController Owner { get; }
        public bool IsBuff { get; }
        public TowerEmblem Emblem => Owner.Definition.Emblem;

        public int CellIndex { get; private set; } = -1;
        public bool IsPlaced => CellIndex >= 0;

        public Zone(TowerController owner, bool isBuff)
        {
            Owner = owner;
            IsBuff = isBuff;
        }

        public void PlaceAt(int cellIndex) => CellIndex = cellIndex;
    }
}
