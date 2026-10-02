/// <summary>
/// 불 디버프.
///   스킬 1 (화상): 걸려 있는 동안 매 턴 최대 체력의 Power% 피해. 장판을 벗어나면 Duration 턴 뒤에 꺼집니다.
///   스킬 2 (연소): 화상 스택이 문턱에 닿으면 발동. 영구히 매 턴 최대 체력의 Power × AbilityValue % 피해, 받는 치유 40% 감소.
/// </summary>
public class FireDebuffRule : EnemyDebuffRule
{
    private const float IgnitedHealReceivedMultiplier = 0.6f;

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
        return state.Triggered ? IgnitedHealReceivedMultiplier : 1f;
    }

    public override string Describe(EnemyDebuffState state)
    {
        if (state.Triggered)
        {
            return $"티어 {state.Tier} · [연소] 매 턴 최대 체력 {state.Power * state.AbilityValue:0.#}% 피해, 받는 치유 40% 감소";
        }

        string burn = IsSkill1Active(state)
            ? $"화상 매 턴 {state.Power:0.#}% 피해 · 지속 {state.RemainingDuration}턴"
            : "화상 꺼짐";
        return $"티어 {state.Tier} · {burn} · 스택 {state.Stack}/{state.StackThreshold}";
    }
}
