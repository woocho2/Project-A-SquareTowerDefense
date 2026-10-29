namespace SquareTD
{
    // 게임 전체에서 쓰는 열거형 (문양, 컬러, 스탯, 타일 종류 등)
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
        ColorLevelBonus = 12,
        Defense = 13,
        SpeedMin = 14,
        SpeedMax = 15,
        ActionGainPerTurn = 16,
        DamageTaken = 17,
        FixedDamageRatio = 18,
        FinalDamage = 19,
        DistanceDamage = 20,
        DistanceDamageBonus = 21,
        ArmorPenetration = 22,
        HealingTaken = 23,
        BonusDamage = 24,
        StackGainRate = 25,
        StackKeepHalf = 26,
        MapBuffRate = 27,
        RewardBonus = 28,
        ForceFixedDamage = 29,
    }

    public enum PathTileType
    {
        None = 0,
        Defense = 1,
        Speed = 2,
        ActionCount = 3,
        Heal = 4,
        Purify = 5,
        Bifrost = 6,
    }

    public enum SpawnTileType
    {
        None = 0,
        Power = 1,
        ActionCount = 2,
        AttackCount = 3,
        Mastery = 4,
    }
}
