using System;
using System.Collections.Generic;

namespace SquareTD
{
    //   (컬러, 문양) → 그 스킬을 만드는 함수. 표에 없으면 null → 부르는 쪽(TowerFactory)이 정함
    //   switch 대신 표 하나: 문양이 늘어도 한 줄씩 더하기만 하고, 어느 문양이 비었는지 한눈에 보임
    //   문양 → 버프 규칙·디버프 규칙. 규칙은 상태가 없어 문양마다 하나를 만들어 두고 같이 씀. 수업 41: 타워 ID → 시너지
    public static class SkillRegistry
    {
        private static readonly Dictionary<TowerEmblem, BuffRule> s_buffRules = new Dictionary<TowerEmblem, BuffRule>
        {
            { TowerEmblem.Sword, new SwordBuffRule() },
        };

        private static readonly Dictionary<TowerEmblem, DebuffRule> s_debuffRules = new Dictionary<TowerEmblem, DebuffRule>
        {
            { TowerEmblem.Sword, new SwordDebuffRule() },            
        };

        // 표에 아직 없는 문양은 시험 규칙 (문양이 다 들어오는 수업 40에서 지움)
        private static readonly BuffRule s_testBuffRule = new TestBuffRule();
        private static readonly DebuffRule s_testDebuffRule = new TestDebuffRule();

        public static BuffRule GetBuffRule(TowerEmblem emblem)
        {
            if (s_buffRules.TryGetValue(emblem, out BuffRule rule)) return rule;
            return s_testBuffRule;
        }

        public static DebuffRule GetDebuffRule(TowerEmblem emblem)
        {
            if (s_debuffRules.TryGetValue(emblem, out DebuffRule rule)) return rule;
            return s_testDebuffRule;
        }

        // Func<받는 것들…, 돌려주는 것> = "이것들을 받아 AttackSkill을 만들어 주는 함수"를 담는 변수 형
        //   (grid, occupancy, stackModes) => new …  는 그 자리에서 만든 이름 없는 함수 (람다)
        private static readonly Dictionary<(TowerColor, TowerEmblem), Func<BoardGrid, PathOccupancy, StackModeSettings, AttackSkill>> s_attackSkills =
            new Dictionary<(TowerColor, TowerEmblem), Func<BoardGrid, PathOccupancy, StackModeSettings, AttackSkill>>
            {
                { (TowerColor.Red, TowerEmblem.Sword), (grid, occupancy, stackModes) => new SwordSplashSkill(grid, occupancy, stackModes) },
                { (TowerColor.Blue, TowerEmblem.Sword), (grid, occupancy, stackModes) => new SwordTargetSkill(grid, occupancy, stackModes) },                
            };

        public static AttackSkill CreateAttackSkill(TowerColor color, TowerEmblem emblem,
                                                    BoardGrid grid, PathOccupancy occupancy, StackModeSettings stackModes)
        {
            if (s_attackSkills.TryGetValue((color, emblem), out var create)) return create(grid, occupancy, stackModes);
            return null;
        }
    }
}
