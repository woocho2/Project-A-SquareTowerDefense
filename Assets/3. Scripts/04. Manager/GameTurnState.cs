/// <summary>
/// 게임 흐름 상태입니다. 상태가 바뀔 때 필요한 UI와 입력 처리를 각 상태가 책임집니다.
/// </summary>
public interface IGameTurnState
{
    TurnState Type { get; }
    bool CanPerformPlayerAction { get; }

    void Enter(GameManager gameManager);
    void Exit(GameManager gameManager);
}

public abstract class GameTurnStateBase : IGameTurnState
{
    public abstract TurnState Type { get; }
    public virtual bool CanPerformPlayerAction => false;

    public virtual void Enter(GameManager gameManager) { }
    public virtual void Exit(GameManager gameManager) { }
}

public sealed class IdleTurnState : GameTurnStateBase
{
    public override TurnState Type => TurnState.None;

    public override void Enter(GameManager gameManager)
    {
        UIManager.Instance?.SetPlayerActionUI(false);
    }
}

public sealed class PlayerTurnState : GameTurnStateBase
{
    public override TurnState Type => TurnState.PlayerTurn;
    public override bool CanPerformPlayerAction => true;

    public override void Enter(GameManager gameManager)
    {
        CameraController.SmoothToPlayerTurnProjectionSize(gameManager);

        // 타워/적 정보 패널은 계속 남기고, 생성·강화·합성 등 행동 UI만 표시합니다.
        UIManager.Instance?.SetPlayerActionUI(true);
    }
}

public sealed class EnemyTurnState : GameTurnStateBase
{
    public override TurnState Type => TurnState.EnemyTurn;

    public override void Enter(GameManager gameManager)
    {
        // 턴 전환 순간 진행 중이던 드래그를 취소해 적 턴에 행동이 이어지는 일을 막습니다.
        DragObjectOnGround.CancelAllInteractions();
        DebuffZone.CancelAllInteractions();

        // 정보 확인 UI는 숨기지 않습니다.
        UIManager.Instance?.SetPlayerActionUI(false);
    }
}

public sealed class GameOverTurnState : GameTurnStateBase
{
    public override TurnState Type => TurnState.GameOver;

    public override void Enter(GameManager gameManager)
    {
        DragObjectOnGround.CancelAllInteractions();
        DebuffZone.CancelAllInteractions();
        UIManager.Instance?.SetPlayerActionUI(false);
    }
}

public sealed class GameClearTurnState : GameTurnStateBase
{
    public override TurnState Type => TurnState.GameClear;

    public override void Enter(GameManager gameManager)
    {
        DragObjectOnGround.CancelAllInteractions();
        DebuffZone.CancelAllInteractions();
        UIManager.Instance?.SetPlayerActionUI(false);
    }
}
