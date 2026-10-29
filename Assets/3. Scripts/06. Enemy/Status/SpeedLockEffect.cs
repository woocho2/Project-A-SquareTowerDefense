namespace SquareTD
{
    // 이동력 1 / 1 고정 (망치질)
    public class SpeedLockEffect : StatusEffect
    {
        public const string LockKey = "이동력 고정";

        public SpeedLockEffect(int durationActions) : base(durationActions) { }

        public override string Key => LockKey;
        public override float Strength => 1f;

        public override MoveRange OnDecideMove(EnemyController enemy, MoveRange range)
        {
            if (range.Min == 0 && range.Max == 0) return range;
            return new MoveRange(1, 1, range.Action);
        }

        public override string Describe() => $"망치질: 이동력 1 / 1 고정 ({RemainingActions}회 행동 남음)";
    }
}
