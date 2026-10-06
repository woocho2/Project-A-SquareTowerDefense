using UnityEngine;

/// <summary>
/// 버프 타워의 공통 틀입니다. 버프를 받는 타워 한 기가 문양 하나에 대해 갖는 상태이며, 문양별 버프는 이 클래스를 상속해서 만듭니다.
///   스킬 1: Action 턴마다 범위 안 아군에게 Power 만큼의 버프를 걸고, AttackCount 만큼 스택을 쌓습니다.
///   스킬 2: 스택이 문턱(60 ÷ 컬러 강화 레벨)에 닿으면 스킬 2 단계가 됩니다. 한 번 되면 스킬 1로 돌아가지 않습니다(영구).
///           그 뒤로는 버프 타워가 행동할 때마다 스킬 1 대신 Power × AbilityValue 만큼의 버프를 받습니다.
/// 들어온 버프는 받은 타워가 Duration 번 행동하는 동안 유지됩니다 (턴이 아니라 행동 횟수).
/// 스킬 1의 효과가 꺼져도 쌓인 스택 수는 스킬 2 단계가 될 때까지 남습니다.
/// 스킬 2 단계가 된 뒤로는 스킬 1의 효과를 받지 않고 스택도 쌓이지 않습니다.
/// 스킬 1이든 스킬 2든 버프는 버프 타워가 행동할 때 들어옵니다.
/// TowerController는 문양을 모른 채 정해진 시점에 아래 함수만 호출합니다.
/// 새 문양을 추가할 때는 이 클래스를 상속한 파일 하나를 만들고 Create에 한 줄을 등록합니다.
/// 여기에 등록되지 않은 문양은 TowerController의 예전 방식(티어 고정표)으로 동작합니다.
/// </summary>
public abstract class TowerBuffStatus
{
    protected int m_stack;
    // 스킬 2 단계가 되는 스택 수입니다. 스택이 문턱의 몇 분의 몇까지 찼는지 계산할 때 씁니다.
    protected int m_stackThreshold = 1;
    protected int m_skill1RemainingTurns;
    protected float m_skill1Power;
    // 스킬 2 단계가 되었는지 여부입니다. 한 번 true가 되면 버프가 지워질 때까지 유지됩니다.
    protected bool m_skill2Unlocked;
    protected int m_skill2RemainingTurns;
    protected float m_skill2Value;

    public int Stack => m_stack;
    public bool IsSkill1Active => m_skill1RemainingTurns > 0;
    /// <summary>스킬 2의 효과가 지금 켜져 있는지 여부입니다. 스킬 2 단계여도 버프 타워가 오래 행동하지 않으면 꺼져 있습니다.</summary>
    public bool IsSkill2Active => m_skill2Unlocked && m_skill2RemainingTurns > 0;
    /// <summary>스킬 2 단계가 되었는지 여부입니다. 효과가 지금 켜져 있는지와는 별개입니다.</summary>
    public bool IsSkill2Unlocked => m_skill2Unlocked;

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
    /// 스킬 2 단계가 아니면 스킬 1을 켜고 스택을 쌓으며, 문턱에 닿으면 스택을 비우고 스킬 2 단계가 됩니다.
    /// 스킬 2 단계면 스킬 1 대신 스킬 2의 효과를 Duration 턴 동안 받습니다. 스택은 쌓이지 않습니다.
    /// </summary>
    public void OnApplied(TowerTileBuffEffect effect)
    {
        int durationTurns = Mathf.Max(1, effect.StackLifetime);

        if (!m_skill2Unlocked)
        {
            m_stackThreshold = Mathf.Max(1, effect.StackThreshold);
            m_stack += Mathf.Max(1, effect.StackGain);
            if (m_stack < m_stackThreshold)
            {
                m_skill1RemainingTurns = Mathf.Max(m_skill1RemainingTurns, durationTurns);
                m_skill1Power = Mathf.Max(m_skill1Power, effect.Power);
                return;
            }

            m_stack = 0;
            m_skill1RemainingTurns = 0;
            m_skill1Power = 0f;
            m_skill2Unlocked = true;
        }

        m_skill2RemainingTurns = Mathf.Max(m_skill2RemainingTurns, durationTurns);
        m_skill2Value = effect.Power * effect.AbilityValue;
    }

    /// <summary>
    /// 문턱을 stageCount 단계로 나눴을 때 지금 스택이 몇 단계까지 찼는지 반환합니다 (0 ~ stageCount - 1).
    /// 문턱에 견주어 계산하므로, 컬러 강화로 문턱이 줄어도 같은 단계에 더 빨리 도달합니다.
    /// </summary>
    protected int GetStackStage(int stageCount)
    {
        int stage = m_stack * stageCount / Mathf.Max(1, m_stackThreshold);
        return Mathf.Clamp(stage, 0, stageCount - 1);
    }

    /// <summary>
    /// 켜져 있는 스킬의 세기를 버프 타워의 지금 스탯에 맞춥니다.
    /// 버프는 버프 타워가 있어야 유지되므로, 버프 타워를 티어 강화·컬러 강화하면 이미 받은 버프도 함께 오릅니다.
    /// </summary>
    public void SyncWithSource(float power, float abilityValue)
    {
        if (IsSkill1Active) m_skill1Power = power;
        if (IsSkill2Active) m_skill2Value = power * abilityValue;
    }

    /// <summary>
    /// 이 버프를 받은 타워가 한 번 행동할 때마다 호출됩니다. 스킬 1과 스킬 2의 남은 횟수를 줄입니다.
    /// 버프의 유지는 턴이 아니라 받은 타워의 행동 횟수로 셉니다 (Duration = 그 타워가 버프를 받은 채 행동하는 횟수).
    /// 스택 수와 스킬 2 단계는 줄지 않습니다.
    /// </summary>
    public void OnOwnerActed()
    {
        if (m_skill1RemainingTurns > 0 && --m_skill1RemainingTurns <= 0) m_skill1Power = 0f;
        if (m_skill2RemainingTurns > 0 && --m_skill2RemainingTurns <= 0) m_skill2Value = 0f;
    }

    /// <summary>최종 스탯을 계산할 때 호출됩니다. 켜져 있는 스킬 1 또는 스킬 2의 효과를 스탯에 반영합니다.</summary>
    public abstract void ModifyFinalStats(ref TowerStats finalStats);
}
