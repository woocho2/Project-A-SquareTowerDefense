/// <summary>
/// 불 버프.
///   스킬 1 (예열): 스택이 쌓일수록 세집니다. 문턱을 5단계로 나눠, 문턱의 1/5을 채울 때마다 공격력 +(Power × 0.4)%씩 오릅니다.
///                  1단계 0.4배, 2단계 0.8배, 3단계 1.2배, 4단계 1.6배. 5단계(문턱)에서 과열 단계가 됩니다.
///   스킬 2 (과열): 예열 스택이 문턱에 닿으면 과열 단계가 됩니다 (영구).
///   그 뒤로는 불 버프 타워가 행동할 때마다 예열 대신 과열을 받습니다: 공격력 +(Power × AbilityValue × 2)%, 공격 횟수 +1.
/// </summary>
public class FireBuffStatus : TowerBuffStatus
{
    private const int PreheatStageCount = 5;
    private const float PreheatPowerRatioPerStage = 0.4f;
    private const float OverheatMultiplier = 2f;

    public override void ModifyFinalStats(ref TowerStats finalStats)
    {
        if (IsSkill2Active)
        {
            finalStats.AttackPower *= 1f + m_skill2Value * OverheatMultiplier / 100f;
            finalStats.AttackCount += 1;
        }
        else if (IsSkill1Active)
        {
            float preheatPercent = m_skill1Power * PreheatPowerRatioPerStage * GetStackStage(PreheatStageCount);
            finalStats.AttackPower *= 1f + preheatPercent / 100f;
        }
    }
}
