namespace SquareTD
{
    // 로딩 화면에 표시할 타워의 이름과 실제 기본·스킬 효과
    public sealed class LoadingTowerProfile
    {
        public string Name { get; private set; }
        public string Category { get; private set; }
        public string Introduction { get; private set; }
        public TowerDescription Description { get; private set; }

        public static LoadingTowerProfile Create(TowerDefinition definition)
        {
            var state = new TowerState(definition);
            TowerFinalStats stats = TowerStatCalculator.Calculate(definition, definition, state, 1);
            string introduction;
            switch (definition.Color)
            {
                case TowerColor.Red: introduction = "여러 적을 함께 공격하며 전장의 흐름을 바꾸는 광역 타워입니다."; break;
                case TowerColor.Blue: introduction = "한 적에게 공격을 집중하는 단일 타워입니다."; break;
                case TowerColor.Yellow: introduction = "존 위의 아군을 강화하고, 스킬로 아군 전체를 돕는 버프 타워입니다."; break;
                default: introduction = "존 위의 적을 약화하고, 스킬로 적 전체를 견제하는 디버프 타워입니다."; break;
            }
            return new LoadingTowerProfile
            {
                Name = EmblemNames.GetConceptName(definition.Id),
                Category = EmblemNames.GetEmblemName(definition.Emblem) + "  /  " + EmblemNames.GetColorName(definition.Color),
                Introduction = introduction,
                Description = TowerDescriptionBuilder.Build(definition, stats, state, new TowerDescriptionContext(20, 0, 0, false))
            };
        }
    }
}
