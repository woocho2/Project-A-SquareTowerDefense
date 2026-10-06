using UnityEngine;

/// <summary>
/// 불 디버프.
///   스킬 1 (화상): 걸려 있는 동안 매 턴 최대 체력의 Power% 피해, 받는 치유 40% 감소. 장판을 벗어나면 Duration 턴 뒤에 꺼집니다.
///   스킬 2 (연소): 화상 스택이 문턱에 닿으면 발동. 영구히 매 턴 최대 체력의 Power × AbilityValue % 피해,
///                  받는 치유 (40 + AbilityValue)% 감소 (최대 80%).
/// </summary>
public class FireDebuffRule : EnemyDebuffRule
{
    private const float BurnHealReductionPercent = 40f;
    private const float MaxHealReductionPercent = 80f;

    public override void OnTurn(EnemyDebuffState state, EnemyHealthController health)
    {
        float damagePercent;
        if (state.Triggered) damagePercent = state.Power * state.AbilityValue;
        else if (IsSkill1Active(state)) damagePercent = state.Power;
        else return;

        health.ApplyDamage(health.MaxHP * damagePercent / 100f);
    }

    public override float GetHealReceivedMultiplier(EnemyDebuffState state)
    {
        if (state.Triggered) return 1f - GetIgnitedHealReductionPercent(state) / 100f;
        return IsSkill1Active(state) ? 1f - BurnHealReductionPercent / 100f : 1f;
    }

    // 연소의 치유 감소를 합(40 + AbilityValue)으로 할지 곱(40 × AbilityValue)으로 할지는 실제 스탯을 넣을 때 정합니다.
    // 지금은 합으로 계산합니다. 어느 쪽이든 최대 80%입니다.
    private static float GetIgnitedHealReductionPercent(EnemyDebuffState state)
    {
        return Mathf.Clamp(BurnHealReductionPercent + state.AbilityValue, BurnHealReductionPercent, MaxHealReductionPercent);
    }

    public override string Describe(EnemyDebuffState state)
    {
        if (state.Triggered)
        {
            return $"티어 {state.Tier} · [연소] 매 턴 최대 체력 {state.Power * state.AbilityValue:0.#}% 피해, " +
                $"받는 치유 {GetIgnitedHealReductionPercent(state):0.#}% 감소";
        }

        string burn = IsSkill1Active(state)
            ? $"화상 매 턴 {state.Power:0.#}% 피해, 받는 치유 {BurnHealReductionPercent:0.#}% 감소 · 지속 {state.RemainingDuration}턴"
            : "화상 꺼짐";
        return $"티어 {state.Tier} · {burn} · 스택 {state.Stack}/{state.StackThreshold}";
    }
}
