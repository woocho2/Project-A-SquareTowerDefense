namespace SquareTD
{
    // 문양·컬러·티어 이름과 56종 타워의 직업 이름 표
    public static class EmblemNames
    {
        private static readonly string[] s_emblemNames =
            { "", "검", "도끼", "활", "창", "방패", "해머", "불", "물", "대지", "바람", "얼음", "전기", "빛", "어둠" };

        private static readonly string[] s_colorNames = { "", "광역", "단일", "버프", "디버프" };

        private static readonly string[] s_tierNames = { "", "브론즈", "실버", "골드", "미스릴", "다이아" };

        private static readonly string[,] s_conceptNames =
        {
            { "", "",              "",              "",              ""              },
            { "", "소드 마스터",   "블레이드 댄서",  "발키리",        "데스 나이트"   },
            { "", "바이킹",        "버서커",        "스칼드",        "익스큐셔너"    },
            { "", "레인저",        "헌터",          "하이엘프",      "스나이퍼"      },
            { "", "워로드",        "스카이 랜서",   "뱅가드",        "임페일러"      },
            { "", "센티넬",        "실드메이든",    "가디언",        "워든"          },
            { "", "팔라딘",        "브레이커",      "골드스미스",    "프로스펙터"    },
            { "", "플레임위자드",  "드레이크",      "피닉스",        "이프리트"      },
            { "", "파도잡이",      "운디네",        "나이아드",      "세이렌"        },
            { "", "노움",          "드루이드",      "드라이어드",    "하베스터"      },
            { "", "에어로맨서",    "실프",          "제피로스",      "하피"          },
            { "", "소서러",        "잭 프로스트",   "이르비스",      "유키온나"      },
            { "", "키린",          "볼트 거너",     "일렉트릭 점퍼", "스톰위치"      },
            { "", "아크메이지",    "세라프",        "비숍",          "인퀴지터"      },
            { "", "다크메이지",    "벨리알",        "헬레틱",        "폴스 프로펫"   },
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
