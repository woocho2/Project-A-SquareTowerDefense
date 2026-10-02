using UnityEngine;

/// <summary>
/// 적 한 마리에게 걸린 디버프 한 종류의 상태입니다.
/// </summary>
public sealed class EnemyDebuffState
{
    public int Tier;
    public int Stack;
    public int TurnCount;
    public int ExpireTurns = -1;
    public bool Triggered;
    public int ZonePathIndex = -1;
    public float AbilityValue;
    public float Power;
    public float PendingDamage;
    public int StackThreshold = 1;
    public int RemainingDuration = -1;
    public bool RefreshedOnCurrentTile;
    public int? ActiveSourceID;
    public readonly System.Collections.Generic.Dictionary<int, int> AppliedVersionsBySource =
        new System.Collections.Generic.Dictionary<int, int>();
}

/// <summary>
/// 디버프 타워의 공통 틀입니다. 문양별 디버프는 이 클래스를 상속해서 만듭니다.
///   스킬 1: Action 턴마다 장판 위의 적에게 Duration 턴 동안 Power 만큼의 디버프를 걸고, AttackCount 만큼 스택을 쌓습니다.
///   스킬 2: 스택이 문턱(60 ÷ 컬러 강화 레벨)에 닿으면 영구히 지속되는 Power × AbilityValue 만큼의 디버프가 됩니다.
/// 스킬 1의 효과는 Duration 턴 뒤에 꺼지지만, 쌓인 스택 수는 스킬 2가 발동할 때까지 남습니다.
/// EnemyDebuffController는 문양을 모른 채 정해진 시점에 아래 함수만 호출합니다.
/// 새 문양을 추가할 때는 이 클래스를 상속한 파일 하나를 만들고 Get에 한 줄을 등록합니다.
/// 여기에 등록되지 않은 문양은 EnemyDebuffController의 예전 방식으로 동작합니다.
/// </summary>
public abstract class EnemyDebuffRule
{
    private static readonly EnemyDebuffRule s_fire = new FireDebuffRule();

    /// <summary>문양에 맞는 규칙을 반환합니다. 아직 이 틀로 옮기지 않은 문양은 null입니다.</summary>
    public static EnemyDebuffRule Get(DebuffTarget target)
    {
        switch (target)
        {
            case DebuffTarget.Fire: return s_fire;
            default: return null;
        }
    }

    /// <summary>
    /// 디버프 타워의 한 번 행동이 이 적에게 적용될 때 호출됩니다. 스택을 쌓고, 문턱에 닿으면 스킬 2를 발동합니다.
    /// </summary>
    public virtual void OnStackApplied(EnemyDebuffState state, int stackGain)
    {
        if (state.Triggered) return;

        state.Stack += Mathf.Max(1, stackGain);
        if (state.Stack >= Mathf.Max(1, state.StackThreshold))
        {
            state.Stack = 0;
            state.Triggered = true;
        }
    }

    /// <summary>스킬 1의 효과가 지금 켜져 있는지 여부입니다. 장판을 벗어나 Duration 턴이 지나면 꺼집니다.</summary>
    public bool IsSkill1Active(EnemyDebuffState state) => !state.Triggered && state.RemainingDuration > 0;

    /// <summary>매 적 턴의 디버프 경과 단계에서 한 번 호출됩니다. 지속 피해 같은 턴 효과를 처리합니다.</summary>
    public abstract void OnTurn(EnemyDebuffState state, EnemyHealthController health);

    /// <summary>이 디버프가 걸린 적이 받는 치유의 배율입니다. 영향이 없으면 1입니다.</summary>
    public virtual float GetHealReceivedMultiplier(EnemyDebuffState state) => 1f;

    /// <summary>적 정보 패널에 보여 줄 설명입니다.</summary>
    public abstract string Describe(EnemyDebuffState state);
}
