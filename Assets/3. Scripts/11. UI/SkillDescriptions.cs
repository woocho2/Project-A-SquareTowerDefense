namespace SquareTD
{
    // 타워 카드의 스킬 이름·설명 (20번 4-3 ~ 4-16장)
    public static class SkillDescriptions
    {
        private static readonly string[,] s_names =
        {
            { "", "", "", "" },
            { "보이드 슬래시", "블레이드 스톰", "브레이브 소드 → 글로리 소드", "위더링 커스 → 둠 레퀴엠" },
            { "휠윈드", "블러드 레이지", "워 송 → 웰컴 투 발할라", "데스 센텐스 → 엑스큐션" },
            { "애로우 레인", "헤비 볼트", "엘븐 사이트 → 스피릿 보우", "락 온 → 핀 포인트" },
            { "랠리 배너", "헤드헌트", "원 포인트 → 원 스트라이크", "래서레이트 → 헤머리지" },
            { "콘커션", "리바운드", "프로텍트 → 이지스", "서프레스 → 락다운" },
            { "저지먼트", "아머 크러시", "길딩 → 골든 터치", "크랙 → 잭팟" },
            { "메테오", "브레스", "피닉스 페더 → 라이징 피닉스", "이그나이트 → 인페르노" },
            { "쓰나미", "아쿠아 프리즌", "웰스프링 → 오버플로우", "미스트 → 세이렌 송" },
            { "어스퀘이크", "소잉", "버드 → 블로섬", "루트 드레인 → 라이프 하베스트" },
            { "윈드 슬래쉬", "템페스트", "테일윈드 → 게일 포스", "윈드 시어 → 터뷸런스" },
            { "블리자드", "아이시클 레인", "프로스트 아머 → 앱솔루트 제로", "프로스트바이트 → 딥 프리즈" },
            { "라이트닝 볼텍스", "오버로드", "펄스 → 하이퍼 펄스", "스태틱 필드 → 스태틱 스톰" },
            { "프리즘", "하이페리온", "헤일로 → 어센션", "쉬머 → 배니시" },
            { "블랙홀", "심연", "디바우어 → 글러트니", "미아즈마 → 다크 고스펠" },
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
            float skill1 = SkillFormula.SupportSkill1(stats.Power);
            float skill2 = SkillFormula.SupportSkill2(stats.Power, stats.AbilityValue);
            int duration = SkillFormula.EffectDuration(stats.ColorLevel, definition.Tier);
            string count = $"스킬 카운트 {stats.SkillCount:0.#}마다";
            string support = $"스킬 1 {skill1:0.##}, 스킬 2 {skill2:0.##} ({duration}번 행동 동안)";

            switch ((definition.Emblem, definition.Color))
            {
                case (TowerEmblem.Sword, TowerColor.Red): return $"{count} 맵 전체의 적에게 {damage:0.##} 피해. 베기의 기본형";
                case (TowerEmblem.Sword, TowerColor.Blue): return $"{count} 그 적에게 검기 10번 (합계 {damage:0.##}) + 잃은 체력의 {damage:0.##}% 폭발";
                case (TowerEmblem.Sword, TowerColor.Yellow): return $"공격력 +%. {support}\n글로리 소드 최종 피해 +10%, 베기는 치명타 확률 +10%";
                case (TowerEmblem.Sword, TowerColor.Purple): return $"최대 체력 −%. {support}\n둠 레퀴엠 매 턴 최대 체력 1%, 베기 피해의 20% 고정";

                case (TowerEmblem.Axe, TowerColor.Red): return $"{count} 맵 전체의 적에게 {damage:0.##} 피해. 사거리 안의 적은 반드시 치명타";
                case (TowerEmblem.Axe, TowerColor.Blue): return $"{count} {duration}번 행동 동안 치명타 확률 100%, 공격력 +{damage:0.##}%, 행동 간격 절반";
                case (TowerEmblem.Axe, TowerColor.Yellow): return $"치명타 피해 +%. {support}\n발할라 최대 Action −1, 베기는 치명타 피해 +20%";
                case (TowerEmblem.Axe, TowerColor.Purple): return $"즉사 기준 %. {support}\n엑스큐션 받는 피해 +10%, 베기에 맞으면 기준 +10%";

                case (TowerEmblem.Bow, TowerColor.Red): return $"{count} {duration}턴 동안 매 턴 맵 전체에 {damage * 0.2f:0.##} 피해 (겹쳐 깔림)";
                case (TowerEmblem.Bow, TowerColor.Blue): return $"{count} 그 적의 최대 체력의 {damage:0.##}% 피해 (저항으로 줄지 않음)";
                case (TowerEmblem.Bow, TowerColor.Yellow): return $"거리 비례 추가 피해 +%. {support}\n스피릿 보우 사거리 +1, 찌르기는 항상 최대치";
                case (TowerEmblem.Bow, TowerColor.Purple): return $"받는 최종 피해 +%. {support}\n핀 포인트 받는 치명타 확률 +10%, 찌르기 피해의 20% 추가타";

                case (TowerEmblem.Spear, TowerColor.Red): return $"{count} 사거리 안 아군 공격 타워의 공격력·치명타 확률 +{damage:0.##}% ({duration}번 행동 동안)";
                case (TowerEmblem.Spear, TowerColor.Blue): return $"{count} 창 {duration}개를 체력이 높은 적부터, 하나에 {damage * 0.2f:0.##}";
                case (TowerEmblem.Spear, TowerColor.Yellow): return $"AttackCount 1, 공격력 × 원래 AttackCount, 공격력 +%. {support}\n원 스트라이크 치명타 확률·방어 관통 +10%, 찌르기는 받는 치유 −40%";
                case (TowerEmblem.Spear, TowerColor.Purple): return $"받은 피해의 %가 출혈. {support}\n헤머리지 이동력 −1, 찌르기의 출혈 2배";

                case (TowerEmblem.Shield, TowerColor.Red): return $"{count} 착탄 범위의 적에게 {damage:0.##} 피해, 1행동 기절";
                case (TowerEmblem.Shield, TowerColor.Blue): return $"{count} 방패가 {duration}번 튕김, 한 번에 {damage * 0.2f:0.##}";
                case (TowerEmblem.Shield, TowerColor.Yellow): return $"최종 피해 +(값 × 한도)%. {support}\n이지스 동안 한도 +50%, 타격은 공격력 +(한도 × 0.2)%";
                case (TowerEmblem.Shield, TowerColor.Purple): return $"저항력 −%p. {support}\n락다운 최대 이동력 −2, 타격에 받는 최종 피해 +5%";

                case (TowerEmblem.Hammer, TowerColor.Red): return $"{count} 착탄 범위의 적에게 {damage:0.##} 피해, 맞은편 칸으로 날려 보냄";
                case (TowerEmblem.Hammer, TowerColor.Blue): return $"{count} 8번 공격 (합계 {damage:0.##}), 마지막에 1행동 방어력 0";
                case (TowerEmblem.Hammer, TowerColor.Yellow): return $"추가 피해 +(값 × 현재 골드)%. {support}\n골든 터치 골드 +50%, 타격 처치 50%로 골드 +100%";
                case (TowerEmblem.Hammer, TowerColor.Purple): return $"방어력 −%. {support}\n잭팟인 적이 죽으면 젬 +1, 타격 처치 50%로 +1";

                case (TowerEmblem.Fire, TowerColor.Red): return $"{count} 맵 전체의 적에게 {damage:0.##} × (1 + 화상 × 1%) 피해";
                case (TowerEmblem.Fire, TowerColor.Blue): return $"{count} {duration}턴 동안 체력이 가장 많은 적에게 매 턴 {damage:0.##} × (1 + 화상 × 1%), 화상 +1. 그동안 기본 공격 없음";
                case (TowerEmblem.Fire, TowerColor.Yellow): return $"공격 횟수 ×2, 공격력 +(값 − 50)%. {support}\n라이징 피닉스 공격력 +10%, 불은 치명타 피해 +20%";
                case (TowerEmblem.Fire, TowerColor.Purple): return $"화상 스택 +값. {support}\n인페르노 화상 피해 2배, 불 타워가 치면 화상 +(Tier × 레벨)";

                case (TowerEmblem.Water, TowerColor.Red): return $"{count} 맵 전체의 적에게 {damage:0.##} × (1 + 젖음 × 1%) 피해, −3 ~ +3칸 밀어냄";
                case (TowerEmblem.Water, TowerColor.Blue): return $"{count} {duration}턴 동안 가둠 (0 / 0), 매 턴 {damage:0.##} × (1 + 젖음 × 1%)";
                case (TowerEmblem.Water, TowerColor.Yellow): return $"차지 쌓이는 양 +%. {support}\n오버플로우 차지 배율 1.5배, 물은 잃은 체력 비례 최대 +10%";
                case (TowerEmblem.Water, TowerColor.Purple): return $"젖음 스택 +값. {support}\n세이렌 송 받는 피해 +10%, 물에게 +10% 더 (다음 행동 이동력 +2)";

                case (TowerEmblem.Earth, TowerColor.Red): return $"{count} 맵 전체의 적에게 {damage:0.##} 피해, 다음 행동 한 번 행동 불가. 쓸 때마다 AbilityValue +성장%";
                case (TowerEmblem.Earth, TowerColor.Blue): return $"{count} 그 적에게 {damage:0.##} 피해. 쓸 때마다 무작위 능력치 +성장";
                case (TowerEmblem.Earth, TowerColor.Yellow): return $"AbilityValue +%. {support}\n블로섬 SkillCount −Tier, 대지 타워는 공격마다 성장 +1";
                case (TowerEmblem.Earth, TowerColor.Purple): return $"밟을 때마다 성장 +1, 최대 체력·방어력 −(값 × 성장)%. {support}\n라이프 하베스트 적응형 능력치 −1, 대지 타워 추가 피해";

                case (TowerEmblem.Wind, TowerColor.Red): return $"{count} 착탄 범위에 {damage:0.##} × 치명타 배율을 적 수로 나눠 줌 (반드시 치명타)";
                case (TowerEmblem.Wind, TowerColor.Blue): return $"{count} {damage:0.##}% 확률로 {duration}번 행동 동안 AttackCount +2 (가속이면 +4)";
                case (TowerEmblem.Wind, TowerColor.Yellow): return $"최대 Action −1, AttackCount +1, 치명타 교환. {support}\n게일 포스 Power +10%, 바람은 밀어내기";
                case (TowerEmblem.Wind, TowerColor.Purple): return $"다음 이동 +5, 이동한 칸 × 값 피해, 아군 가속 +값. {support}\n터뷸런스 최대 Action +3, 바람 타워 스킬 카운트 +2";

                case (TowerEmblem.Ice, TowerColor.Red): return $"{count} 맵 전체의 적에게 {damage:0.##} 피해. 빙결인 적 +150%, 한기인 적은 빙결";
                case (TowerEmblem.Ice, TowerColor.Blue): return $"{count} 그 적에게 {damage:0.##} 피해. 한기면 치명타, 빙결이면 치명타 + 치명타 피해 +300%";
                case (TowerEmblem.Ice, TowerColor.Yellow): return $"최대 Action +4, 공격력·치명타 +%. {support}\n앱솔루트 제로 Mastery +1, 얼음은 Action +2만";
                case (TowerEmblem.Ice, TowerColor.Purple): return $"한기 스택 +값. {support}\n딥 프리즈는 한기를 모두 없애고 빙결, 한기·빙결인 적 받는 피해 +50%";

                case (TowerEmblem.Electricity, TowerColor.Red): return $"{count} 맵 전체의 적에게 {damage:0.##} 피해, 전하 +(Tier × 레벨 × 최대 Charge)";
                case (TowerEmblem.Electricity, TowerColor.Blue): return $"{count} 맵 전체의 적에게 {damage:0.##} × (1 + 전하 × 1%), 전하를 터뜨림";
                case (TowerEmblem.Electricity, TowerColor.Yellow): return $"AttackCount ×3, 공격력·치명타 (계산식 − 100)%, 타격마다 전하. {support}\n하이퍼 펄스 스킬 카운트 +1, 전기는 전하 2배";
                case (TowerEmblem.Electricity, TowerColor.Purple): return $"전하 +값. {support}\n스태틱 스톰 발동 때 전하 2배, 전기 타워가 치면 연쇄";

                case (TowerEmblem.Light, TowerColor.Red): return $"{count} 아군 공격 타워의 공격력·치명타 +{damage:0.##}% ({duration}번 행동). 초월 웨이브 2배 + AttackCount·Mastery +1";
                case (TowerEmblem.Light, TowerColor.Blue): return $"{count} {duration}턴 동안 매 턴 {damage * 0.1f:0.##}부터 2배씩. 초월 웨이브는 맵 전체 {damage:0.##}부터 4배씩";
                case (TowerEmblem.Light, TowerColor.Yellow): return $"추가 피해 +%, 확률로 초월 카운트. {support}\n어센션 카운트 +1, 초월 웨이브면 다음 웨이브도 초월\n초월 카운트 {EmblemSharedState.Current.TranscendCount:0} / {EmblemSharedState.TranscendGoal:0}";
                case (TowerEmblem.Light, TowerColor.Purple): return $"실명 스택 (뒤로 걸음). {support}\n초월 웨이브의 배니시는 모든 적 즉사, 최대 체력 −10%, 빛은 최종 피해 +20%";

                case (TowerEmblem.Darkness, TowerColor.Red): return $"{count} {duration}턴 블랙홀, 매 턴 {damage * 0.3f:0.##} 피해와 1칸 끌어당김. 막타면 침식 카운트 +5";
                case (TowerEmblem.Darkness, TowerColor.Blue): return $"{count} 그 적에게 {damage:0.##} × (1 + 침식 카운트 × 1%)\n지금 침식 카운트 {EmblemSharedState.Current.DarknessCount:0}";
                case (TowerEmblem.Darkness, TowerColor.Yellow): return $"침식 카운트 +값, 카운트 단계표. {support}\n글러트니 타격마다 0.5% 즉사 (+50), 어둠은 1%\n지금 침식 카운트 {EmblemSharedState.Current.DarknessCount:0}";
                case (TowerEmblem.Darkness, TowerColor.Purple): return $"침식 스택과 같은 양의 카운트 +값. {support}\n다크 고스펠 침식인 적 추가 피해 +10%, 어둠 타워가 치면 그 타일 침식 +1";
            }

            switch (definition.Color)
            {
                case TowerColor.Red: return $"{count} 맵 전체의 적에게 {damage:0.##} 피해";
                case TowerColor.Blue: return $"{count} 그 순간 맞은 적에게 {damage:0.##} 피해";
                case TowerColor.Yellow: return $"존 위의 공격 타워에게 버프. {support}";
                case TowerColor.Purple: return $"존을 밟은 적에게 디버프. {support}";
                default: return "시너지 타워 (효과 미정)";
            }
        }

        public static string GetZoneSkillName(Zone zone)
        {
            TowerColor color = zone.IsBuff ? TowerColor.Yellow : TowerColor.Purple;
            int emblem = (int)zone.Emblem;
            if (emblem < 1 || emblem >= s_names.GetLength(0)) return zone.IsBuff ? "버프존" : "디버프존";
            return s_names[emblem, (int)color - 1].Split('→')[0].Trim();
        }
    }
}
