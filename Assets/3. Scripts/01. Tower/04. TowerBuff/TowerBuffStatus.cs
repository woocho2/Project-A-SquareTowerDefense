using UnityEngine;

/// <summary>
/// 버프 타워의 공통 틀입니다. 버프를 받는 타워 한 기가 문양 하나에 대해 갖는 상태이며, 문양별 버프는 이 클래스를 상속해서 만듭니다.
///   스킬 1: Action 턴마다 범위 안 아군에게 Duration 턴 동안 Power 만큼의 버프를 걸고, AttackCount 만큼 스택을 쌓습니다.
///   스킬 2: 스택이 문턱(60 ÷ 컬러 강화 레벨)에 닿으면 Duration 턴 동안 Power × AbilityValue 만큼의 버프가 됩니다.
/// 스킬 1의 효과는 Duration 턴 뒤에 꺼지지만, 쌓인 스택 수는 스킬 2가 발동할 때까지 남습니다.
/// TowerController는 문양을 모른 채 정해진 시점에 아래 함수만 호출합니다.
/// 새 문양을 추가할 때는 이 클래스를 상속한 파일 하나를 만들고 Create에 한 줄을 등록합니다.
/// 여기에 등록되지 않은 문양은 TowerController의 예전 방식(티어 고정표)으로 동작합니다.
/// </summary>
public abstract class TowerBuffStatus
{
    protected int m_stack;
    protected int m_skill1RemainingTurns;
    protected float m_skill1Power;
    protected int m_skill2RemainingTurns;
    protected float m_skill2Value;

    public int Stack => m_stack;
    public bool IsSkill1Active => m_skill1RemainingTurns > 0;
    public bool IsSkill2Active => m_skill2RemainingTurns > 0;

    /// <summary>문양에 맞는 상태를 만듭니다. 아직 이 틀로 옮기지 않은 문양은 null입니다.</summary>
    public static TowerBuffStatus Create(BuffTarget target)
    {
        switch (target)
        {
            case BuffTarget.Fire: return new FireBuffStatus();
            default: return null;
        }
    }

    /// <summary>
    /// 버프 타워의 한 번 행동이 이 타워에 적용될 때 호출됩니다.
    /// 스킬 1을 켜고 스택을 쌓으며, 문턱에 닿으면 스택을 비우고 스킬 2를 발동합니다.
    /// </summary>
    public void OnApplied(TowerTileBuffEffect effect)
    {
        int durationTurns = Mathf.Max(1, effect.StackLifetime);
        m_skill1RemainingTurns = Mathf.Max(m_skill1RemainingTurns, durationTurns);
        m_skill1Power = Mathf.Max(m_skill1Power, effect.Power);

        m_stack += Mathf.Max(1, effect.StackGain);
        if (m_stack < Mathf.Max(1, effect.StackThreshold)) return;

        m_stack = 0;
        m_skill2RemainingTurns = Mathf.Max(m_skill2RemainingTurns, durationTurns);
        m_skill2Value = Mathf.Max(m_skill2Value, effect.Power * effect.AbilityValue);
    }

    /// <summary>적 턴이 시작될 때 한 번 호출됩니다. 스킬 1과 스킬 2의 남은 턴을 줄입니다. 스택 수는 줄지 않습니다.</summary>
    public void OnEnemyTurnStart()
    {
        if (m_skill1RemainingTurns > 0 && --m_skill1RemainingTurns <= 0) m_skill1Power = 0f;
        if (m_skill2RemainingTurns > 0 && --m_skill2RemainingTurns <= 0) m_skill2Value = 0f;
    }

    /// <summary>최종 스탯을 계산할 때 호출됩니다. 켜져 있는 스킬 1과 스킬 2의 효과를 스탯에 반영합니다.</summary>
    public abstract void ModifyFinalStats(ref TowerStats finalStats);
}
