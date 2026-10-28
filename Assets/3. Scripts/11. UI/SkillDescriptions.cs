using UnityEngine;

namespace SquareTD
{
    // 타워 카드의 스킬 칸 글: 스킬 이름, 액티브/패시브, 설명 (2026-10-28)
    //   이름은 20번 기획서 [문양별 이름]. 공격 타워(광역·단일)의 스킬 2는 액티브, 버프·디버프 타워의 존 효과는 패시브
    //   설명은 지금 검만 자세히. 다른 문양은 컬러별 공통 문장 — 문양 수업(28 ~ 40)에서 그 문양의 줄을 채움
    public static class SkillDescriptions
    {
        // [문양, 컬러] — 컬러 순서: 광역, 단일, 버프, 디버프
        private static readonly string[,] s_names =
        {
            { "", "", "", "" },                                                  // 0 None
            { "보이드 슬래시", "블레이드 템페스트", "블레싱 → 글로리", "커스 → 둠" },
            { "대회전", "광폭화", "출정가 → 발할라의 찬가", "사형 선고 → 사형 집행" },
            { "화살비", "은화살", "요정의 활 → 정령의 활", "락 온 → 핀 포인트" },
            { "용맹한 깃창", "뽑아찢기", "카우치 → 랜스 차지", "출혈 → 과다출혈" },
            { "배쉬", "아머 브레이크", "프로텍트 → 이지스", "서프레스 → 락다운" },
            { "100T 해머", "망치질", "제련 → 세공", "원석 → 금맥" },
            { "메테오", "화염구", "불씨 → 불길", "화상 → 연소" },
            { "쓰나미", "급류", "물결 → 해류", "젖음 → 침수" },
            { "대지의 분노", "씨뿌리기", "성장 → 만개", "진흙 → 석화" },
            { "칼바람", "폭풍", "순풍 → 질풍", "역풍 → 난기류" },
            { "블리자드", "스노우볼", "냉기 → 한기", "동상 → 저체온증" },
            { "라이트닝 볼텍스", "과부하", "충전 → 과충전 → 방전", "누전 → 감전" },
            { "프리즘", "도깨비불", "광채 → 광휘", "잔영 → 잔상" },
            { "블랙홀", "심연", "포식 → 폭식", "잠식 → 침식" },
        };

        public static string GetName(TowerDefinition definition)
        {
            int emblem = (int)definition.Emblem, color = (int)definition.Color - 1;
            if (emblem < 1 || emblem >= s_names.GetLength(0) || color < 0 || color > 3) return "스킬";
            return s_names[emblem, color];
        }

        // 버프·디버프 타워의 존 효과는 따로 누르지 않아도 늘 걸리므로 패시브
        public static bool IsPassive(TowerDefinition definition) =>
            definition.Color == TowerColor.Yellow || definition.Color == TowerColor.Purple;

        // 카드 스킬 칸의 제목: "보이드 슬래시 · 액티브"
        public static string GetTitle(TowerDefinition definition) =>
            $"{GetName(definition)} · {(IsPassive(definition) ? "패시브" : "액티브")}";

        public static string GetDescription(TowerDefinition definition, TowerFinalStats stats)
        {
            float damage = SkillFormula.AttackSkill(stats.Power, stats.AbilityValue);
            float zone = SkillFormula.SupportSkill2(stats.Power, stats.AbilityValue, stats.Stack, stats.StackValue) * 100f;

            switch ((definition.Emblem, definition.Color))
            {
                case (TowerEmblem.Sword, TowerColor.Red):
                    return $"기본 공격이 적에게 {stats.Duration}번 적중할 때마다 맵 전체의 적에게 {damage:0.##} 피해.\n덧붙는 효과 없는 베기의 기본형";
                case (TowerEmblem.Sword, TowerColor.Blue):
                    return $"기본 공격이 적에게 {stats.Duration}번 적중하면 그 순간 맞은 적에게 {damage:0.##} 피해를 24번에 나눠 줌, 마지막 일격은 2.4배.\n치명은 한 번만 판정해 24번 모두에";
                case (TowerEmblem.Sword, TowerColor.Yellow):
                    return $"블레싱: 공격력 +(스택만큼, 최대 {zone:0.##}%)\n글로리: 공격력 +{zone:0.##}%, 최종 피해 +10%\n베기 타워는 치명 확률 +5% / +10%";
                case (TowerEmblem.Sword, TowerColor.Purple):
                    return $"커스: 매 턴 최대 체력의 (스택만큼, 최대 {zone:0.##}%) 피해\n둠: 매 턴 {zone:0.##}% 피해, 최대 체력 −20%\n베기에 맞으면 피해의 10% / 20%가 고정 피해";
                case (TowerEmblem.Axe, TowerColor.Red):
                    return $"기본 공격이 적에게 {stats.Duration}번 적중할 때마다 사거리 안의 모든 적에게 {damage * CriticalRoll.Multiplier(stats.CriticalDamage):0.##} 피해.\n반드시 치명, 사거리 끝 칸은 × 1.5";
                case (TowerEmblem.Axe, TowerColor.Blue):
                    return $"기본 공격이 적에게 {stats.Duration}번 적중하면 {stats.ColorLevel * 2}번 행동 동안 광폭화.\n치명 확률 100%, 공격력 +{damage:0.##}%, 행동 간격 절반";
                case (TowerEmblem.Axe, TowerColor.Yellow):
                    return $"출정가: 치명 피해 +(스택만큼, 최대 {zone:0.##}%)\n발할라의 찬가: 치명 피해 +{zone:0.##}%, Action −1, Mastery +1\n베기 타워는 치명 피해 +25% / +50%. 판 위에 있으면 적 수 한도 감소";
                case (TowerEmblem.Axe, TowerColor.Purple):
                    return $"사형 선고: 체력 (스택만큼, 최대 {zone:0.##}%) 미만이면 즉사, 적은 최대 체력 +20%·최대 이동력 +1\n사형 집행: 체력 {zone:0.##}% 미만이면 즉사\n베기에 맞으면 기준 +5% / +10%";
                case (TowerEmblem.Bow, TowerColor.Red):
                    return $"기본 공격이 적에게 {stats.Duration}번 적중하면 하늘로 화살을 쏘아 올림.\n{stats.ColorLevel * 2}턴 동안 매 턴 맵 전체에 {damage * ArrowRainGround.TurnRatio:0.##} 피해 (겹쳐 깔림)";
                case (TowerEmblem.Bow, TowerColor.Blue):
                    return $"기본 공격이 적에게 {stats.Duration}번 적중하면 그 순간 맞은 적에게 최대 체력의 {damage:0.##}% 피해.\n저항으로 줄지 않음";
                case (TowerEmblem.Bow, TowerColor.Yellow):
                    return $"요정의 활: 날아간 칸마다 추가 피해 +(스택만큼, 최대 {zone:0.##}%)\n정령의 활: 칸마다 +{zone:0.##}%, 사거리 +1\n찌르기 타워는 거리와 무관하게 최대치 + 5% / 10%";
                case (TowerEmblem.Bow, TowerColor.Purple):
                    return $"락 온: 받는 최종 피해 +(스택만큼, 최대 {zone:0.##}%)\n핀 포인트: 받는 최종 피해 +{zone:0.##}%, 받는 치명 피해 +50%\n찌르기 타워의 치명 확률 +5% / +10%";
                case (TowerEmblem.Spear, TowerColor.Red):
                    return $"기본 공격이 적에게 {stats.Duration}번 적중하면 깃창을 세움.\n사거리 안 아군 공격 타워(자기 포함)의 공격력·치명 확률 +{damage:0.##}%, {stats.ColorLevel * 2}번 행동 동안";
                case (TowerEmblem.Spear, TowerColor.Blue):
                    return $"기본 공격이 맞을 때마다 적에게 창이 꽂힘.\n{stats.Duration}번 적중하면 창이 꽂힌 적 전부에게 창 수 × {damage:0.##} 피해 후 다 뽑음";
                case (TowerEmblem.Spear, TowerColor.Yellow):
                    return $"카우치: 돌격 준비, Action +2 (좋은 효과 없음)\n랜스 차지: 방어 관통 20%, 치명 확률·치명 피해 +{zone:0.##}%\n찌르기 타워는 랜스 차지의 방어 관통 +10%";
                case (TowerEmblem.Spear, TowerColor.Purple):
                    return $"출혈: 받는 치유 −(스택만큼, 최대 {zone:0.##}%)\n과다출혈: 받는 치유 −{zone:0.##}%, 이동한 칸마다 최대 체력 2%\n찌르기에 맞으면 잃은 체력만큼 피해 최대 +10% / +20%";
                case (TowerEmblem.Shield, TowerColor.Red):
                    return $"기본 공격이 적에게 {stats.Duration}번 적중하면 착탄 범위의 적에게 {damage:0.##} 피해.\n맞은 적을 20% 확률로 1행동 기절";
                case (TowerEmblem.Shield, TowerColor.Blue):
                    return $"기본 공격이 적에게 {stats.Duration}번 적중하면 그 적에게 {damage:0.##} 피해.\n방어력 −20%, {stats.ColorLevel * 2}번 행동 동안";
                case (TowerEmblem.Shield, TowerColor.Yellow):
                    return $"스택을 가디언 자신이 쌓음. 존 위 타워의 최종 피해 +(식 × 적 수 한도)%\n이지스: 스택이 다 차면 펼쳐 적 수 한도 증가 (티어가 높을수록 많이)\n타격 타워는 추가 피해 (한도 × 0.1)% / (한도 × 0.2)%";
                case (TowerEmblem.Shield, TowerColor.Purple):
                    return $"서프레스: 방어력 −(스택만큼, 최대 {zone:0.##}%)\n락다운: 방어력 −{zone:0.##}%, 행동 주기 +1\n타격에 맞으면 기절 확률 +5% / +10%";
            }

            // 아직 문양 수업 전: 컬러별 공통 문장
            switch (definition.Color)
            {
                case TowerColor.Red: return $"기본 공격이 적에게 {stats.Duration}번 적중할 때마다 발동. 피해 {damage:0.##}\n(문양 효과는 그 문양의 수업에서)";
                case TowerColor.Blue: return $"기본 공격이 적에게 {stats.Duration}번 적중할 때마다 그 순간 맞은 적에게 발동. 피해 {damage:0.##}\n(문양 효과는 그 문양의 수업에서)";
                case TowerColor.Yellow: return $"존 위의 공격 타워에게 버프. 스택이 다 차면 스킬 2 ({zone:0.##}%)\n(문양 효과는 그 문양의 수업에서)";
                case TowerColor.Purple: return $"존을 밟은 적에게 디버프. 스택이 다 차면 스킬 2 ({zone:0.##}%)\n(문양 효과는 그 문양의 수업에서)";
                default: return "시너지 타워 (효과 미정)";
            }
        }

        // 버프존·디버프존의 지금 단계 이름: 스킬 1이면 "블레싱", 스킬 2면 "글로리" (이름 표의 "→" 앞뒤)
        public static string GetZoneStageName(Zone zone)
        {
            TowerColor color = zone.IsBuff ? TowerColor.Yellow : TowerColor.Purple;
            int emblem = (int)zone.Emblem;
            if (emblem < 1 || emblem >= s_names.GetLength(0)) return zone.IsBuff ? "버프존" : "디버프존";
            string[] stages = s_names[emblem, (int)color - 1].Split('→');
            int index = zone.Stage == ZoneStage.Skill2 ? Mathf.Min(1, stages.Length - 1) : 0;
            return stages[index].Trim();
        }
    }
}
