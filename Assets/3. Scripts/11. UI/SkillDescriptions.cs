using UnityEngine;

namespace SquareTD
{
    // 타워 카드의 스킬 이름·설명
    public static class SkillDescriptions
    {
        private static readonly string[,] s_names =
        {
            { "", "", "", "" },
            { "보이드 슬래시", "블레이드 스톰", "블레싱 → 글로리", "커스 → 둠" },
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

        public static bool IsPassive(TowerDefinition definition) =>
            definition.Color == TowerColor.Yellow || definition.Color == TowerColor.Purple;

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
                    return $"기본 공격이 적에게 {stats.Duration}번 적중하면 그 순간 맞은 적에게 검기 폭풍.\n검기 10번에 {damage / SwordTargetSkill.StormHits:0.##}씩, 폭발 막타 {damage:0.##} (합계 2배). 치명은 한 번만 판정해 11번 모두에";
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
                case (TowerEmblem.Hammer, TowerColor.Red):
                    return $"기본 공격이 적에게 {stats.Duration}번 적중하면 착탄 범위의 적에게 {damage:0.##} 피해.\n맞은 적의 최대 이동력 −1, {stats.ColorLevel * 2}번 행동 동안";
                case (TowerEmblem.Hammer, TowerColor.Blue):
                    return $"기본 공격이 적에게 {stats.Duration}번 적중하면 그 적에게 {damage:0.##} 피해.\n50% 확률로 이동력 1 / 1 고정, {stats.ColorLevel * 2}번 행동 동안";
                case (TowerEmblem.Hammer, TowerColor.Yellow):
                    return $"제련: 추가 피해 +(스택만큼, 최대 {zone:0.##}%) × 지금 골드\n세공: 추가 피해 +{zone:0.##}% × 지금 골드, 존 위 타워가 행동할 때 10%로 젬 (사이클 번호)개\n타격 타워는 공격 횟수 +1 / +2";
                case (TowerEmblem.Hammer, TowerColor.Purple):
                    return $"원석: 처치 골드 +(스택만큼, 최대 {zone:0.##}%)\n금맥: 처치 골드 +{zone:0.##}%, 죽을 때 10%로 젬 1개\n타격 타워가 처치하면 골드 +5% / +10%";
                case (TowerEmblem.Fire, TowerColor.Red):
                    return $"기본 공격이 적에게 {stats.Duration}번 적중할 때마다 맵 전체의 적에게 {damage:0.##} 피해.\n화상인 적 +50%, 연소된 적 +100%";
                case (TowerEmblem.Fire, TowerColor.Blue):
                    return $"기본 공격이 적에게 {stats.Duration}번 적중하면 그 적에게 {damage:0.##} 피해, 60% 확률로 화상.\n화상인 적에게 +10%, 연소된 적에게 +20% (기본 공격도)";
                case (TowerEmblem.Fire, TowerColor.Yellow):
                    return $"불씨: 공격력 +(스택만큼, 최대 {zone:0.##}%)\n불길: 공격력 +{zone:0.##}%, 치명 확률 +10%, 치명 피해 +30%, 공격 횟수 +1. {stats.ColorLevel * 5}턴 뒤 불씨로\n불 단일은 공격 횟수 +1 / +2, 불 광역은 StackValue +20%";
                case (TowerEmblem.Fire, TowerColor.Purple):
                    return $"화상: 매 턴 최대 체력의 (스택만큼, 최대 {zone:0.##}%) 피해, 받는 치유도 같은 값만큼 감소\n연소: 매 턴 {zone:0.##}%, 같은 칸의 적에게 화상이 번짐";
                case (TowerEmblem.Water, TowerColor.Red):
                    return $"기본 공격이 적에게 {stats.Duration}번 적중할 때마다 맵 전체의 적에게 {damage:0.##} 피해.\n다음 행동 한 번 이동력 −2 (저항 확률). 물결 존 ×1.25, 해류 존 ×1.5";
                case (TowerEmblem.Water, TowerColor.Blue):
                    return $"기본 공격이 적에게 {stats.Duration}번 적중하면 그 적에게 {damage:0.##} 피해 후 밀어냄.\n다음 행동은 뒤로 1칸 (젖음이면 2칸)";
                case (TowerEmblem.Water, TowerColor.Yellow):
                    return $"물결: 스택이 쌓이는 양 +(스택만큼, 최대 {zone:0.##}%)\n해류: 쌓이는 양 +{zone:0.##}%, 스택을 써도 절반에서 다시";
                case (TowerEmblem.Water, TowerColor.Purple):
                    return $"젖음: 이동력을 바꾼 공격에 받는 피해 +(스택만큼, 최대 {zone:0.##}%), 일반 공격은 절반\n침수: +{zone:0.##}%, 맵 버프를 50%만 받음";
                case (TowerEmblem.Earth, TowerColor.Red):
                    return $"기본 공격이 적에게 {stats.Duration}번 적중할 때마다 맵 전체의 적에게 {damage:0.##} 피해.\n쓸 때마다 AbilityValue +1% (성장 존 1.5%, 만개 존 2%). 진흙·석화인 적은 방어력만큼 더 셈";
                case (TowerEmblem.Earth, TowerColor.Blue):
                    return $"기본 공격이 적에게 {stats.Duration}번 적중하면 그 적에게 {damage:0.##} 피해.\n쓸 때마다 Power +1%. 진흙인 적의 최소 이동력 −1, 석화면 −2";
                case (TowerEmblem.Earth, TowerColor.Yellow):
                    return $"성장: AbilityValue +(스택만큼, 최대 {zone:0.##}%)\n만개: AbilityValue +{zone:0.##}%, 컬러 레벨 +1 (최대 6)";
                case (TowerEmblem.Earth, TowerColor.Purple):
                    return $"진흙: 턴마다 (스택만큼, 최대 {zone:0.##}%) 확률로 속박, 방어력 +20%\n석화: 턴마다 {zone:0.##}%로 속박, 방어력 +10%, 행동 주기 +2";
                case (TowerEmblem.Wind, TowerColor.Red):
                    return $"기본 공격이 적에게 {stats.Duration}번 적중하면 착탄 범위에 {damage * CriticalRoll.Multiplier(stats.CriticalDamage):0.##} 피해를 적 수만큼 나눠 줌.\n반드시 치명. 역풍인 적 2배, 난기류 3배. 역풍·난기류를 맞히면 1 / 2 당김";
                case (TowerEmblem.Wind, TowerColor.Blue):
                    return $"기본 공격이 적에게 {stats.Duration}번 적중하면 {damage:0.##}% 확률로 폭풍 (피해 없음).\n{stats.ColorLevel * 2}번 행동 동안 매 턴 행동 카운트 +2, 공격 횟수 +1";
                case (TowerEmblem.Wind, TowerColor.Yellow):
                    return $"순풍: (스택만큼, 최대 {zone:0.##}%) 확률로 터져 행동 카운트 +2, 공격 횟수 +1\n질풍: {zone:0.##}%로 터져 행동 카운트 +2, 공격 횟수 +2. 존에 서면 Action −1\n바람 광역은 치명 피해 +15% / +30%, 바람 단일은 공격 횟수 +1 / +2";
                case (TowerEmblem.Wind, TowerColor.Purple):
                    return $"역풍: 이동할 때 (스택만큼, 최대 {zone:0.##}%) 확률로 −최대 ~ +최대로 굴림\n난기류: {zone:0.##}%, 이동한 칸마다 현재 체력 4%\n바람 단일의 치명 확률 +10% / +20%";
                case (TowerEmblem.Ice, TowerColor.Red):
                    return $"기본 공격이 적에게 {stats.Duration}번 적중할 때마다 맵 전체의 적에게 {damage:0.##} 피해.\n동상·저체온증인 적을 빙결 (풀릴 때 받은 피해의 25%)";
                case (TowerEmblem.Ice, TowerColor.Blue):
                    return $"기본 공격이 적에게 {stats.Duration}번 적중하면 그 적에게 {damage:0.##} × (1 + 줄어든 이동력 × 0.25) 피해";
                case (TowerEmblem.Ice, TowerColor.Yellow):
                    return $"냉기: 맞은 적이 (스택만큼, 최대 {zone:0.##}%) 확률로 최대 이동력 −1\n한기: {zone:0.##}%로 최소·최대 이동력 −1, 빙결된 적에게 +25%\n얼음 타워는 Mastery +1 / +2";
                case (TowerEmblem.Ice, TowerColor.Purple):
                    return $"동상: 턴마다 (스택만큼, 최대 {zone:0.##}%) 확률로 빙결\n저체온증: 턴마다 {zone:0.##}%로 빙결, 최소·최대 이동력 −1";
                case (TowerEmblem.Electricity, TowerColor.Red):
                    return $"기본 공격이 적에게 {stats.Duration}번 적중할 때마다 맵 전체의 적에게 {damage:0.##} 피해, 전하 {ElectricSplashSkill.ChargePerLevel * stats.ColorLevel}.\n기본 공격도 전하 1. 충전 존 2배, 과충전 존 4배";
                case (TowerEmblem.Electricity, TowerColor.Blue):
                    return $"기본 공격이 적에게 {stats.Duration}번 적중하면 {1 + stats.ColorLevel * stats.Mastery}명에게 연쇄.\n{damage:0.##} × (1 + 전하 × 1%) 피해 후 전하를 터뜨림 (감전이면 전하 2배)";
                case (TowerEmblem.Electricity, TowerColor.Yellow):
                    return $"충전: 치명 확률·치명 피해 +(스택만큼, 최대 {zone:0.##}%)\n과충전: +{zone:0.##}%, 맞힌 적에게 전하 (광역 2, 단일 5). {stats.ColorLevel * 5}턴 뒤 방전\n방전: 5턴 동안 아무 효과 없음";
                case (TowerEmblem.Electricity, TowerColor.Purple):
                    return $"누전: 밟을 때 전하 +(스택만큼, 최대 {zone:0.##})\n감전: 매 턴 전하 +{zone:0.##}, 과부하가 전하를 2배로 셈";
                case (TowerEmblem.Light, TowerColor.Red):
                    return $"기본 공격이 적에게 {stats.Duration}번 적중할 때마다 맵 전체의 적에게 {damage:0.##} 피해, 프리즘 2스택.\n프리즘은 다음 기본 공격에 터져 최대 체력의 5% × 스택. 광채 존 3, 광휘 존 4스택";
                case (TowerEmblem.Light, TowerColor.Blue):
                    return $"맞힐 때마다 그 적에게 도깨비불 (그 턴만), 스택당 피해 +2% (더미 4%).\n{stats.Duration}번 적중하면 그 적에게 {damage:0.##} 피해";
                case (TowerEmblem.Light, TowerColor.Yellow):
                    return $"광채: (스택만큼, 최대 {zone:0.##}%) 확률로 터져 공격력 +10%, 공격 횟수·Mastery +1, 치명 +5%, 치명 피해 +10%, Action −1\n광휘: {zone:0.##}%로 터짐, 여기에 AbilityValue +20%\n빛 단일은 공격 횟수 +1 / +2";
                case (TowerEmblem.Light, TowerColor.Purple):
                    return $"잔영: 적의 더미를 세움 (본체 최대 체력의 스택만큼, 최대 {zone:0.##}%), 더미가 받은 피해 × 2를 본체에게\n잔상: 더미 {zone:0.##}%, 피해 × 4";
                case (TowerEmblem.Darkness, TowerColor.Red):
                    return $"기본 공격이 적에게 {stats.Duration}번 적중하면 블랙홀 ({stats.ColorLevel * 2}턴).\n매 턴 그 구역에 {damage:0.##} 피해, 맵 전체를 끌어당김. 막타면 카운트 +5";
                case (TowerEmblem.Darkness, TowerColor.Blue):
                    return $"기본 공격이 적에게 {stats.Duration}번 적중하면 그 적에게 {damage:0.##} × (1 + 카운트 × 1%) 피해.\n지금 카운트 {EmblemSharedState.Current.DarknessCount:0}";
                case (TowerEmblem.Darkness, TowerColor.Yellow):
                    return $"카운트 100 ~ 1000의 단계 효과를 닿은 데까지 (식의 값: 포식은 스택만큼, 폭식은 {zone:0.##}%)\n폭식은 사거리 +1. 지금 카운트 {EmblemSharedState.Current.DarknessCount:0}";
                case (TowerEmblem.Darkness, TowerColor.Purple):
                    return $"잠식: 죽으면 카운트 +(스택만큼, 최대 {zone:0.##})\n침식: 죽으면 카운트 +{zone:0.##}, 받는 피해 +10%";
            }

            switch (definition.Color)
            {
                case TowerColor.Red: return $"기본 공격이 적에게 {stats.Duration}번 적중할 때마다 발동. 피해 {damage:0.##}\n(문양 효과는 그 문양의 수업에서)";
                case TowerColor.Blue: return $"기본 공격이 적에게 {stats.Duration}번 적중할 때마다 그 순간 맞은 적에게 발동. 피해 {damage:0.##}\n(문양 효과는 그 문양의 수업에서)";
                case TowerColor.Yellow: return $"존 위의 공격 타워에게 버프. 스택이 다 차면 스킬 2 ({zone:0.##}%)\n(문양 효과는 그 문양의 수업에서)";
                case TowerColor.Purple: return $"존을 밟은 적에게 디버프. 스택이 다 차면 스킬 2 ({zone:0.##}%)\n(문양 효과는 그 문양의 수업에서)";
                default: return "시너지 타워 (효과 미정)";
            }
        }

        public static string GetZoneStageName(Zone zone)
        {
            TowerColor color = zone.IsBuff ? TowerColor.Yellow : TowerColor.Purple;
            int emblem = (int)zone.Emblem;
            if (emblem < 1 || emblem >= s_names.GetLength(0)) return zone.IsBuff ? "버프존" : "디버프존";
            string[] stages = s_names[emblem, (int)color - 1].Split('→');
            int index = zone.Stage == ZoneStage.Skill2 ? 1 : zone.Stage == ZoneStage.Discharge ? 2 : 0;
            return stages[Mathf.Min(index, stages.Length - 1)].Trim();
        }
    }
}
