namespace SquareTD
{
    public interface ITurnState
    {
        string Name { get; }
        bool CanPlayerAct { get; }
        bool CanConsumeStack { get; }
        void Enter();
        void Exit();
    }

    public class IdleState : ITurnState
    {
        public string Name => "대기";
        public bool CanPlayerAct => false;
        public bool CanConsumeStack => false;
        public void Enter() { }
        public void Exit() { }
    }

    public class PlayerTurnState : ITurnState
    {
        public string Name => "플레이어 턴";
        public bool CanPlayerAct => true;
        public bool CanConsumeStack => true;
        public void Enter() { }
        public void Exit() { }
    }

    public class EnemyTurnState : ITurnState
    {
        public string Name => "적 턴";
        public bool CanPlayerAct => false;
        public bool CanConsumeStack => true;
        public void Enter() { }
        public void Exit() { }
    }

    public class GameOverState : ITurnState
    {
        public string Name => "게임 오버";
        public bool CanPlayerAct => false;
        public bool CanConsumeStack => false;
        public void Enter() { }
        public void Exit() { }
    }

    public class GameClearState : ITurnState
    {
        public string Name => "게임 클리어";
        public bool CanPlayerAct => false;
        public bool CanConsumeStack => false;
        public void Enter() { }
        public void Exit() { }
    }
}