/// <summary>
/// 버프/디버프 종류를 화면에 표시할 이름으로 바꿉니다.
/// </summary>
public static class BuffTargetNames
{
    public static string GetBuffTargetName(BuffTarget buffTarget)
    {
        return buffTarget switch
        {
            BuffTarget.Sword => "공격력",
            BuffTarget.Bow => "사거리",
            BuffTarget.Shield => "방어막",
            BuffTarget.Spear => "치명타 확률",
            BuffTarget.Axe => "치명타 피해",
            BuffTarget.Hammer => "방어 관통",
            BuffTarget.Fire => "공격 횟수",
            BuffTarget.Ice => "스플래시/추가 타격",
            BuffTarget.Electricity => "추가 공격",
            BuffTarget.Wind => "행동력",
            BuffTarget.Earth => "티어 강화",
            BuffTarget.Light => "체인 공격",
            BuffTarget.Darkness => "다크니스 강화",
            _ => "버프"
        };
    }

    public static string GetDebuffTargetName(DebuffTarget debuffTarget)
    {
        return debuffTarget switch
        {
            DebuffTarget.Sword => "저주",
            DebuffTarget.Bow => "고정 피해",
            DebuffTarget.Shield => "속박",
            DebuffTarget.Spear => "투창",
            DebuffTarget.Axe => "취약",
            DebuffTarget.Hammer => "방어 파괴",
            DebuffTarget.Fire => "화상",
            DebuffTarget.Ice => "빙결",
            DebuffTarget.Electricity => "감전",
            DebuffTarget.Wind => "바람",
            DebuffTarget.Earth => "대지",
            DebuffTarget.Light => "빛",
            DebuffTarget.Darkness => "암흑",
            _ => "디버프"
        };
    }

    /// <summary>적에게 걸린 디버프 목록에 쓰는 이름입니다. "문양 (효과)" 형식으로 표시합니다.</summary>
    public static string GetEnemyDebuffName(DebuffTarget debuffTarget)
    {
        return debuffTarget switch
        {
            DebuffTarget.Fire => "불 (화상)",
            DebuffTarget.Sword => "검 (저주)",
            DebuffTarget.Axe => "도끼 (취약)",
            DebuffTarget.Ice => "얼음 (빙결)",
            DebuffTarget.Electricity => "전기 (감전)",
            DebuffTarget.Wind => "바람 (돌풍)",
            DebuffTarget.Shield => "방패 (속박)",
            DebuffTarget.Spear => "창 (처형)",
            DebuffTarget.Bow => "활 (약점)",
            DebuffTarget.Earth => "대지 (지진)",
            DebuffTarget.Light => "빛 (섬광)",
            DebuffTarget.Darkness => "어둠 (암흑)",
            _ => "특수"
        };
    }
}
