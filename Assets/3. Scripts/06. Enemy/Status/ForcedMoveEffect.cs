namespace SquareTD
{
    // 다음 행동은 정해진 칸만큼 이동 (급류)
    public class ForcedMoveEffect : StatusEffect
    {
        public const string ForcedKey = "밀려남";

        private readonly int m_steps;

        public ForcedMoveEffect(int steps) : base(1)
        {
            m_steps = steps;
        }

        public override string Key => ForcedKey;
        public override float Strength => System.Math.Abs(m_steps);

        public override MoveRange OnDecideMove(EnemyController enemy, MoveRange range)
        {
            if (range.Min == 0 && range.Max == 0) return range;
            return new MoveRange(m_steps, m_steps, range.Action);
        }

        public override string Describe() => $"급류: 다음 행동은 {m_steps}칸";
    }
}
