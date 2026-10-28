namespace SquareTD
{
    // 이동력 1 / 1 고정 (21번 #138, 수업 32 망치질) — 한 번에 한 칸씩만 움직임. 그 적이 n번 행동하는 동안
    //   다른 이동력 감소(100T 해머)보다 우선: 스탯이 얼마든 다음 이동은 1 / 1. 행동 불가(0 / 0)는 그대로 둠
    public class SpeedLockEffect : StatusEffect
    {
        public const string LockKey = "이동력 고정";

        public SpeedLockEffect(int durationActions) : base(durationActions) { }

        public override string Key => LockKey;
        public override float Strength => 1f;

        public override MoveRange OnDecideMove(EnemyController enemy, MoveRange range)
        {
            if (range.Min == 0 && range.Max == 0) return range;     // 기절·빙결 중
            return new MoveRange(1, 1, range.Action);
        }

        public override string Describe() => $"망치질: 이동력 1 / 1 고정 ({RemainingActions}회 행동 남음)";
    }
}
