namespace SquareTD
{
    public static class EmblemNames
    {
        private static readonly string[] s_emblemNames =
            { "", "검", "도끼", "활", "창", "방패", "해머", "불", "물", "대지", "바람", "얼음", "전기", "빛", "어둠" };

        private static readonly string[] s_colorNames = { "", "광역", "단일", "버프", "디버프" };

        private static readonly string[] s_tierNames = { "", "브론즈", "실버", "골드", "미스릴", "다이아" };

        private static readonly string[,] s_conceptNames =
        {
            //  None  레드(광역)      블루(단일)      옐로우(버프)     퍼플(디버프)
            { "", "",              "",              "",              ""              },  // None
            { "", "소드 마스터",   "블레이드 댄서", "발키리",        "데스 나이트"   },  // 검
            { "", "바이킹",        "버서커",        "스칼드",        "집행관"        },  // 도끼
            { "", "레인저",        "헌터",          "엘프",          "스나이퍼"      },  // 활
            { "", "워로드",        "창술사",        "랜서",          "임페일러"      },  // 창
            { "", "서전트",        "실드메이든",    "가디언",        "워든"          },  // 방패
            { "", "브레이커",      "메이슨",        "블랙스미스",    "드워프"        },  // 해머
            { "", "화염술사",      "샐러맨더",      "피닉스",        "이프리트"      },  // 불
            { "", "파도술사",      "운디네",        "나이아드",      "세이렌"        },  // 물
            { "", "드루이드",      "노움",          "오리에드",      "고르곤"        },  // 대지
            { "", "기상술사",      "실프",          "에리얼",        "하피"          },  // 바람
            { "", "소서러",        "잭 프로스트",   "글래셔",        "예티"          },  // 얼음
            { "", "스톰브링어",    "썬더버드",      "키린",          "라이트닝 울프" },  // 전기
            { "", "아크메이지",    "위습",          "비숍",          "루시퍼"        },  // 빛
            { "", "다크메이지",    "셰이드",        "헬레틱",        "레이스"        },  // 어둠
        };

        public static string GetEmblemName(TowerEmblem emblem) => s_emblemNames[(int)emblem];
        public static string GetColorName(TowerColor color) => s_colorNames[(int)color];
        public static string GetTierName(int tier) => s_tierNames[tier];

        public static string GetConceptName(TowerEmblem emblem, TowerColor color) => s_conceptNames[(int)emblem, (int)color];

        public static string GetConceptName(int towerId)
        {
            return GetConceptName(TowerId.GetEmblem(towerId), TowerId.GetColor(towerId));
        }
    }
}
