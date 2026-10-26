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
        Defense = 13,
        SpeedMin = 14,
        SpeedMax = 15,
        ActionGainPerTurn = 16,     // 매 턴 행동 카운트를 더 받음 (행동 카운트 타일, 바람의 순풍)
        DamageTaken = 17,           // 받는 피해 배율 (기본 1)
        FixedDamageRatio = 18,      // 피해 중 방어를 무시하는 비율 (검 디버프, 어둠 900)
    }

    // 길 위의 특수 타일 (20번 2장). 적에게 이로운 것만 (비프로스트만 예외)
    public enum PathTileType
    {
        None = 0,
        Defense = 1,        // 멈춰 있는 동안 방어력 +20%
        Speed = 2,          // 멈춰 있는 동안 이동력 최소·최대 +2
        ActionCount = 3,    // 멈춰 있는 동안 매 턴 행동 카운트 +2
        Heal = 4,           // 멈춰 있는 동안 매 턴 최대 체력의 5% 회복
        Purify = 5,         // 멈춘 순간 모든 디버프 제거
        Bifrost = 6,        // 멈춘 순간 맞은편 칸으로
    }

    // 타워 스폰 칸의 특수 타일 (20번 2장). 공격 타워만 받음
    public enum SpawnTileType
    {
        None = 0,
        Power = 1,          // 공격력 +30%
        ActionCount = 2,    // 매 턴 행동 카운트 +1
        AttackCount = 3,    // 공격 횟수 +1
        Mastery = 4,        // Mastery +1
    }
}