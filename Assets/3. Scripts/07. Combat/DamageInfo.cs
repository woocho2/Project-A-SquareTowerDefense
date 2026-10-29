namespace SquareTD
{
    // 피해 한 번의 정보와 처치 정보
    public enum DamageSource { None = 0, Basic = 1, Skill = 2, Dot = 3, Ground = 4, Tile = 5, Relay = 6 }

    public struct DamageInfo
    {
        public TowerController Attacker;
        public DamageSource Source;
        public TowerEmblem Emblem;
        public AttackAttribute Attribute;
        public float BaseDamage;
        public bool IsCritical;
        public float CritMultiplier;
        public float ArmorPenetration;
        public int Distance;
        public bool ChangesMovement;
        public bool ForceFixed;
        public float StunChance;
    }

    public struct KillInfo
    {
        public EnemyController Enemy;
        public DamageInfo Damage;
        public int SpawnWave;
        public EnemyKind Kind;
    }
}
