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
            { TowerEmblem.Axe, new AxeBuffRule() },
            { TowerEmblem.Bow, new BowBuffRule() },
            { TowerEmblem.Spear, new SpearBuffRule() },
            { TowerEmblem.Shield, new ShieldBuffRule(CurrentLimit) },
            { TowerEmblem.Hammer, new HammerBuffRule(CurrentGold, CurrentCycle, AddGem) },
            // [E] 불 버프 규칙 — 위 줄들과 같은 모양 { 문양, new 규칙() } (FireBuffRule)
            { TowerEmblem.Water, new WaterBuffRule() },
            // [E] 대지 버프 규칙 — 위 줄들과 같은 모양 (EarthBuffRule)
            { TowerEmblem.Wind, new WindBuffRule() },
            { TowerEmblem.Ice, new IceBuffRule() },
            { TowerEmblem.Electricity, new ElectricBuffRule() },
            // [E] 빛 버프 규칙 — 위 줄들과 같은 모양 (LightBuffRule)
            { TowerEmblem.Darkness, new DarknessBuffRule(CurrentDarkness) },
        };

        private static readonly Dictionary<TowerEmblem, DebuffRule> s_debuffRules = new Dictionary<TowerEmblem, DebuffRule>
        {
            { TowerEmblem.Sword, new SwordDebuffRule() },
            { TowerEmblem.Axe, new AxeDebuffRule() },
            { TowerEmblem.Bow, new BowDebuffRule() },
            { TowerEmblem.Spear, new SpearDebuffRule() },
            { TowerEmblem.Shield, new ShieldDebuffRule() },
            // [D] 해머 디버프 규칙 — 위 줄들과 같은 모양 (HammerDebuffRule)
            { TowerEmblem.Fire, new FireDebuffRule(CurrentOccupancy) },
            // [D] 물 디버프 규칙 — 위 줄들과 같은 모양 (WaterDebuffRule)
            { TowerEmblem.Earth, new EarthDebuffRule() },
            // [E] 바람 디버프 규칙 — 위 줄들과 같은 모양 (WindDebuffRule)
            // [D] 얼음 디버프 규칙 — 위 줄들과 같은 모양 (IceDebuffRule)
            // [E] 전기 디버프 규칙 — 위 줄들과 같은 모양 (ElectricDebuffRule)
            { TowerEmblem.Light, new LightDebuffRule(CurrentOccupancy, PathCellPosition) },
            // [E] 어둠 디버프 규칙 — 위 줄들과 같은 모양 (DarknessDebuffRule)
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
                { (TowerColor.Red, TowerEmblem.Axe), (grid, occupancy, stackModes) => new AxeSplashSkill(grid, occupancy, stackModes) },
                { (TowerColor.Blue, TowerEmblem.Axe), (grid, occupancy, stackModes) => new AxeTargetSkill(grid, occupancy, stackModes) },
                { (TowerColor.Red, TowerEmblem.Bow), (grid, occupancy, stackModes) => new ArrowSplashSkill(grid, occupancy, stackModes, CurrentGrounds()) },
                { (TowerColor.Blue, TowerEmblem.Bow), (grid, occupancy, stackModes) => new ArrowTargetSkill(grid, occupancy, stackModes) },
                { (TowerColor.Red, TowerEmblem.Spear), (grid, occupancy, stackModes) => new SpearSplashSkill(grid, occupancy, stackModes, CurrentSlots()) },
                { (TowerColor.Blue, TowerEmblem.Spear), (grid, occupancy, stackModes) => new SpearTargetSkill(grid, occupancy, stackModes) },
                { (TowerColor.Red, TowerEmblem.Shield), (grid, occupancy, stackModes) => new ShieldSplashSkill(grid, occupancy, stackModes) },
                { (TowerColor.Blue, TowerEmblem.Shield), (grid, occupancy, stackModes) => new ShieldTargetSkill(grid, occupancy, stackModes) },
                // [A] 해머 광역 — 바로 위 방패 광역 줄과 같은 모양 (Red, Hammer → HammerSplashSkill)
                { (TowerColor.Blue, TowerEmblem.Hammer), (grid, occupancy, stackModes) => new HammerTargetSkill(grid, occupancy, stackModes) },
                { (TowerColor.Red, TowerEmblem.Fire), (grid, occupancy, stackModes) => new FireSplashSkill(grid, occupancy, stackModes) },
                // [A] 불 단일 — 바로 위 불 광역 줄과 같은 모양 (Blue, Fire → FireTargetSkill)
                // [A] 물 광역 — 바로 아래 물 단일 줄과 같은 모양 (Red, Water → WaterSplashSkill)
                { (TowerColor.Blue, TowerEmblem.Water), (grid, occupancy, stackModes) => new WaterTargetSkill(grid, occupancy, stackModes) },
                // [A] 대지 광역 — 바로 아래 대지 단일 줄과 같은 모양 (Red, Earth → EarthSplashSkill)
                { (TowerColor.Blue, TowerEmblem.Earth), (grid, occupancy, stackModes) => new EarthTargetSkill(grid, occupancy, stackModes) },
                { (TowerColor.Red, TowerEmblem.Wind), (grid, occupancy, stackModes) => new WindSplashSkill(grid, occupancy, stackModes) },
                // [A] 바람 단일 — 바로 위 바람 광역 줄과 같은 모양 (Blue, Wind → WindTargetSkill)
                // [A] 얼음 광역 — 바로 아래 얼음 단일 줄과 같은 모양 (Red, Ice → IceSplashSkill)
                { (TowerColor.Blue, TowerEmblem.Ice), (grid, occupancy, stackModes) => new IceTargetSkill(grid, occupancy, stackModes) },
                { (TowerColor.Red, TowerEmblem.Electricity), (grid, occupancy, stackModes) => new ElectricSplashSkill(grid, occupancy, stackModes) },
                // [A] 전기 단일 — 바로 위 전기 광역 줄과 같은 모양 (Blue, Electricity → ElectricTargetSkill)
                // [A] 빛 광역 — 바로 아래 빛 단일 줄과 같은 모양 (Red, Light → LightSplashSkill)
                { (TowerColor.Blue, TowerEmblem.Light), (grid, occupancy, stackModes) => new LightTargetSkill(grid, occupancy, stackModes) },
                { (TowerColor.Red, TowerEmblem.Darkness), (grid, occupancy, stackModes) => new DarknessSplashSkill(grid, occupancy, stackModes, CurrentGrounds()) },
                // [A] 어둠 단일 — 바로 위 어둠 광역 줄과 같은 모양이되 괄호 끝의 CurrentGrounds()는 없음 (Blue, Darkness → DarknessTargetSkill)
            };

        // 판에 달린 것들: 게임에서는 BoardManager의 것. 테스트(판이 없음)에서는 null
        private static GroundEffectRegistry CurrentGrounds() => BoardManager.Instance != null ? BoardManager.Instance.Grounds : null;   // 화살비가 장판을 깔 곳
        private static TowerSlots CurrentSlots() => BoardManager.Instance != null ? BoardManager.Instance.Slots : null;                // 깃창이 아군을 찾을 곳
        private static int CurrentLimit() => GameSession.Instance != null ? GameSession.Instance.CurrentLimit : 20;                       // 방패 버프 (게임 밖이면 사이클 1의 노말 한도)
        private static int CurrentGold() => CurrencyManager.Instance != null ? CurrencyManager.Instance.Gold : 0;                         // 해머 버프의 골드 비례
        private static int CurrentCycle() => WaveManager.Instance != null && WaveManager.Instance.CurrentWave > 0 ? WaveManager.Instance.Cycle : 1;   // 세공의 젬 수
        private static void AddGem(int amount) { if (CurrencyManager.Instance != null) CurrencyManager.Instance.AddGem(amount); }
        private static PathOccupancy CurrentOccupancy() => BoardManager.Instance != null ? BoardManager.Instance.Occupancy : null;          // 연소가 번질 곳, 더미가 설 곳
        private static UnityEngine.Vector3 PathCellPosition(int pathIndex) =>                                                              // 더미를 세울 자리
            BoardManager.Instance != null ? BoardManager.Instance.GetWorldPosition(BoardManager.Instance.Grid.GetPathCell(pathIndex)) : UnityEngine.Vector3.zero;
        private static float CurrentDarkness() => EmblemSharedState.Current.DarknessCount;                                                // 어둠 버프의 단계

        public static AttackSkill CreateAttackSkill(TowerColor color, TowerEmblem emblem,
                                                    BoardGrid grid, PathOccupancy occupancy, StackModeSettings stackModes)
        {
            if (s_attackSkills.TryGetValue((color, emblem), out var create)) return create(grid, occupancy, stackModes);
            return null;
        }
    }
}
