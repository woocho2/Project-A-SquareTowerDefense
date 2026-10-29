namespace SquareTD
{
    // 맵·길에 깔리는 장판의 베이스 (화살비, 블랙홀)
    public abstract class GroundEffect
    {
        public TowerController Owner { get; }
        public TowerEmblem Emblem { get; }
        public int RemainingTurns { get; private set; }
        public bool IsExpired => RemainingTurns <= 0;

        protected GroundEffect(TowerController owner, TowerEmblem emblem, int turns)
        {
            Owner = owner;
            Emblem = emblem;
            RemainingTurns = turns;
        }

        public abstract void OnTurn();
        public virtual bool Covers(int pathIndex) => true;

        public virtual int PullCenter => -1;
        public virtual MoveRange ModifyMove(EnemyController enemy, MoveRange range) => range;
        public abstract string Describe();

        public void CountDown() => RemainingTurns--;
    }
}
