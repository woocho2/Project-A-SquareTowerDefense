namespace SquareTD
{
    public enum TowerEmblem
    {
        None = 00,
        Sword = 01,
        Axe = 02,
        Bow = 03,
        Spear = 04,
        Shield = 05,
        Hammer = 06,
        Fire = 07,
        Water = 08,
        Earth = 09,
        Wind = 10,
        Ice = 11,
        Electricity = 12,
        Light = 13,
        Darkness = 14
    }

    public enum TowerColor
    {
        None = 0,
        Red = 1,
        Blue = 2,
        Yellow = 3,
        Purple = 4
    }

    public enum AttackAttribute
    {
        None = 0,
        Slash = 1,
        Pierce = 2,
        Blunt = 3
    }

    public enum EnemyKind
    {
        None = 0,
        Normal = 1,
        MiddleBoss = 2,
        FinalBoss = 3,
    }

    public enum GameMode
    {
        None = 0,
        Story = 1,
        Normal = 2,
        Hard = 3
    }

    public enum TargetPriority
    {
        None = 0,
        Closest = 1,
        Farthest = 2,
        MostHP = 3,
        LeastHP = 4,
        MostCrowded = 5,
        Boss = 6
    }

    // 수정자가 바꾸는 스탯 (23번 4-3). 쓰는 수업에서 맨 뒤에 더함
    public enum StatType
    {
        None = 0,
        Power = 1,
        Range = 2,
        MaxAction = 3,
        AttackCount = 4,
        Mastery = 5,
        CritRate = 6,
        CritDamage = 7,
        AbilityValue = 8,
        Duration = 9,
        MaxStack = 10,
        StackValue = 11,
        ColorLevelBonus = 12,       // 대지의 만개: 쓸 컬러 레벨 +1 (최대 6)
    }
}