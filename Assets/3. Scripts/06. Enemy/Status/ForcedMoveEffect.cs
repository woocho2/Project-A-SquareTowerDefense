namespace SquareTD
{
    // 다음 행동 한 번은 주사위 없이 정해진 칸만큼 (21번 #137, 수업 34 급류) — −1이면 뒤로 한 칸, 젖음이면 −2
    //   다시 맞아도 겹치지 않음 (다음 행동 한 번만). 행동 불가(0 / 0)면 그대로 둠
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
