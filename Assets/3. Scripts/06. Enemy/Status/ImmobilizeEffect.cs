namespace SquareTD
{
    public class ImmobilizeEffect : StatusEffect
    {
        public const string ImmobilizeKey = "행동 불가";

        private readonly string m_name;

        public ImmobilizeEffect(string name) : base(1)
        {
            m_name = name;
        }

        public override string Key => ImmobilizeKey;
        public override bool IsStrongCC => true;

        public override MoveRange OnDecideMove(EnemyController enemy, MoveRange range)
        {
            return new MoveRange(0, 0, range.Action);
        }

        public override string Describe() => $"{m_name}: 다음 행동 한 번 0 / 0";
    }
}
