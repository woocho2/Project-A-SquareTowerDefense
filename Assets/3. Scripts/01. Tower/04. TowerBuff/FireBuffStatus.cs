/// <summary>
/// 불 버프.
///   스킬 1 (예열): 켜져 있는 동안 공격력 +Power%.
///   스킬 2 (과열): 예열 스택이 문턱에 닿으면 발동. Duration 턴 동안 공격력 +(Power × AbilityValue)%, 공격 횟수 +1.
/// </summary>
public class FireBuffStatus : TowerBuffStatus
{
    public override void ModifyFinalStats(ref TowerStats finalStats)
    {
        if (IsSkill1Active)
        {
            finalStats.AttackPower *= 1f + m_skill1Power / 100f;
        }

        if (IsSkill2Active)
        {
            finalStats.AttackPower *= 1f + m_skill2Value / 100f;
            finalStats.AttackCount += 1;
        }
    }
}
