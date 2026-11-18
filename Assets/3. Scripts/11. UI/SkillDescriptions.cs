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
            if (SynergyRecipes.TryGet(definition.Id, out SynergyRecipes.Recipe recipe)) return recipe.Name + "의 권능";
            int emblem = (int)definition.Emblem, color = (int)definition.Color - 1;
            if (emblem < 1 || emblem >= s_names.GetLength(0) || color < 0 || color > 3) return "스킬";
            return s_names[emblem, color];
        }

        public static bool IsPassive(TowerDefinition definition) =>
            definition.Color == TowerColor.Yellow || definition.Color == TowerColor.Purple;

        public static string GetTitle(TowerDefinition definition)
        {
            string[] names = GetName(definition).Split('→');
            string kind = definition.Color == TowerColor.Yellow ? "스킬 버프" : definition.Color == TowerColor.Purple ? "스킬 디버프" : "스킬";
            return names[names.Length - 1].Trim() + " / " + kind;
        }

        public static string GetDescription(TowerDefinition definition, TowerFinalStats stats) =>
            TowerDescription.Plain(TowerDescriptionBuilder.Build(definition, stats).Skill);
        internal static string GetSynergyDescription(int id) => id switch
        {
            SynergyEffects.OdinId => $"체력이 {OdinSkill.HpRate * 100:0}% 이하인 적을 처형합니다. 보스는 {OdinSkill.BossHpRate * 100:0}% 이하일 때 처형합니다.",
            SynergyEffects.ThorId => $"무작위 길 {ThorSkill.StormCount}곳에 {ThorSkill.StormTurns}턴 동안 번개 폭풍을 만듭니다. 매 적 턴 최대 체력의 {ThorSkill.DamageRate * 100:0}% 고정 피해를 줍니다.",
            SynergyEffects.LokiId => "플레이어 턴에 길 두 칸을 선택합니다. 두 칸의 적을 서로 바꾸고 행동 카운트를 초기화합니다.",
            SynergyEffects.HelId => "보스를 제외한 모든 적을 즉시 처형합니다.",
            SynergyEffects.SurtrId => $"보스를 포함한 모든 적에게 잃은 체력의 {SurtrSkill.LostHpRate * 100:0}% 고정 피해를 줍니다.",
            SynergyEffects.FreyId => $"판 위 모든 일반 타워의 공격 횟수를 다음 {FreySkill.Actions}번 행동 동안 1 늘립니다.",
            SynergyEffects.BaldrId => $"스택이 차면 권능을 1회 저장합니다. 최대 {BaldrSkill.MaxCharges}회 저장하며 사용 후 다음 보스 기믹을 1회 막습니다.",
            SynergyEffects.SkadiId => $"모든 적의 최소·최대 이동력을 다음 {SkadiSkill.Actions}번 행동 동안 {SkadiSkill.SpeedCut:0} 줄입니다.",
            SynergyEffects.FreyjaId => $"{FreyjaSkill.Turns}턴 동안 적 처치 골드가 2배가 됩니다.",
            SynergyEffects.YmirId => $"보스를 포함한 모든 적이 다음 {YmirSkill.Actions}번 행동 동안 움직일 수 없게 합니다.",
            SynergyEffects.UllrId => $"판 위 모든 일반 타워가 다음 {UllrSkill.Actions}번 행동 동안 공격 피해의 {UllrSkill.FollowUpRate * 100:0}%만큼 고정 추가타를 줍니다.",
            SynergyEffects.BrynhildId => "적 수 한도로 발생하는 게임 오버를 1회 막습니다. 보스 처치 제한 턴은 막지 못합니다.",
            SynergyEffects.BrokkrId => $"판 위 일반 타워의 티어 합에 {BrokkrSkill.GoldPerTier}를 곱한 만큼 골드를 얻습니다.",
            SynergyEffects.NjordId => $"플레이어 턴에 길 한 칸을 선택합니다. {NjordSkill.Turns}턴 동안 적이 그 칸을 지나치지 못하고 멈춥니다.",
            _ => "타워 아래의 스킬 버튼으로 권능을 사용합니다."
        };

        public static string GetZoneSkillName(Zone zone)
        {
            TowerColor color = zone.IsBuff ? TowerColor.Yellow : TowerColor.Purple;
            int emblem = (int)zone.Emblem;
            if (emblem < 1 || emblem >= s_names.GetLength(0)) return zone.IsBuff ? "버프존" : "디버프존";
            return s_names[emblem, (int)color - 1].Split('→')[0].Trim();
        }
    }
}
