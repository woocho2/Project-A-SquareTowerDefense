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
}