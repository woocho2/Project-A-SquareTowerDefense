namespace SquareTD
{
    // 맵·길에 깔리는 것 (21번 #27, 수업 29) — 활의 화살비, (수업 40) 어둠의 블랙홀
    //   디버프 단계(적 1턴의 ⑤)마다 OnTurn → 남은 턴 −1 → 0이면 사라짐. 적의 행동이 아니라 턴으로 셈
    //   같은 것이 다시 깔려도 덮어쓰지 않고 따로 남음 (각자 피해와 남은 턴을 가짐, 20번 4-1장)
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

        public abstract void OnTurn();                          // ⑤ 그 턴의 일 (피해 등)
        public virtual bool Covers(int pathIndex) => true;      // 이 길 칸에 깔려 있나 (맵 전체면 늘 true)
        public abstract string Describe();                      // 설명 한 줄 (타일 카드, Click Debug 창)

        public void CountDown() => RemainingTurns--;
    }
}
