using System;
using System.Collections.Generic;

namespace SquareTD
{
    // (컬러, 문양) → 스킬, 문양 → 버프·디버프 규칙 등록표
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
            { TowerEmblem.Fire, new FireBuffRule() },
            { TowerEmblem.Water, new WaterBuffRule() },
            { TowerEmblem.Earth, new EarthBuffRule() },
            { TowerEmblem.Wind, new WindBuffRule() },
            { TowerEmblem.Ice, new IceBuffRule() },
            { TowerEmblem.Electricity, new ElectricBuffRule() },
            { TowerEmblem.Light, new LightBuffRule() },
            { TowerEmblem.Darkness, new DarknessBuffRule(CurrentDarkness) },
        };

        private static readonly Dictionary<TowerEmblem, DebuffRule> s_debuffRules = new Dictionary<TowerEmblem, DebuffRule>
        {
            { TowerEmblem.Sword, new SwordDebuffRule() },
            { TowerEmblem.Axe, new AxeDebuffRule() },
            { TowerEmblem.Bow, new BowDebuffRule() },
            { TowerEmblem.Spear, new SpearDebuffRule() },
            { TowerEmblem.Shield, new ShieldDebuffRule() },
            { TowerEmblem.Hammer, new HammerDebuffRule() },
            { TowerEmblem.Fire, new FireDebuffRule(CurrentOccupancy) },
            { TowerEmblem.Water, new WaterDebuffRule() },
            { TowerEmblem.Earth, new EarthDebuffRule() },
            { TowerEmblem.Wind, new WindDebuffRule() },
            { TowerEmblem.Ice, new IceDebuffRule() },
            { TowerEmblem.Electricity, new ElectricDebuffRule() },
            { TowerEmblem.Light, new LightDebuffRule(CurrentOccupancy, PathCellPosition) },
            { TowerEmblem.Darkness, new DarknessDebuffRule() },
        };

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
                { (TowerColor.Red, TowerEmblem.Hammer), (grid, occupancy, stackModes) => new HammerSplashSkill(grid, occupancy, stackModes) },
                { (TowerColor.Blue, TowerEmblem.Hammer), (grid, occupancy, stackModes) => new HammerTargetSkill(grid, occupancy, stackModes) },
                { (TowerColor.Red, TowerEmblem.Fire), (grid, occupancy, stackModes) => new FireSplashSkill(grid, occupancy, stackModes) },
                { (TowerColor.Blue, TowerEmblem.Fire), (grid, occupancy, stackModes) => new FireTargetSkill(grid, occupancy, stackModes) },
                { (TowerColor.Red, TowerEmblem.Water), (grid, occupancy, stackModes) => new WaterSplashSkill(grid, occupancy, stackModes) },
                { (TowerColor.Blue, TowerEmblem.Water), (grid, occupancy, stackModes) => new WaterTargetSkill(grid, occupancy, stackModes) },
                { (TowerColor.Red, TowerEmblem.Earth), (grid, occupancy, stackModes) => new EarthSplashSkill(grid, occupancy, stackModes) },
                { (TowerColor.Blue, TowerEmblem.Earth), (grid, occupancy, stackModes) => new EarthTargetSkill(grid, occupancy, stackModes) },
                { (TowerColor.Red, TowerEmblem.Wind), (grid, occupancy, stackModes) => new WindSplashSkill(grid, occupancy, stackModes) },
                { (TowerColor.Blue, TowerEmblem.Wind), (grid, occupancy, stackModes) => new WindTargetSkill(grid, occupancy, stackModes) },
                { (TowerColor.Red, TowerEmblem.Ice), (grid, occupancy, stackModes) => new IceSplashSkill(grid, occupancy, stackModes) },
                { (TowerColor.Blue, TowerEmblem.Ice), (grid, occupancy, stackModes) => new IceTargetSkill(grid, occupancy, stackModes) },
                { (TowerColor.Red, TowerEmblem.Electricity), (grid, occupancy, stackModes) => new ElectricSplashSkill(grid, occupancy, stackModes) },
                { (TowerColor.Blue, TowerEmblem.Electricity), (grid, occupancy, stackModes) => new ElectricTargetSkill(grid, occupancy, stackModes) },
                { (TowerColor.Red, TowerEmblem.Light), (grid, occupancy, stackModes) => new LightSplashSkill(grid, occupancy, stackModes) },
                { (TowerColor.Blue, TowerEmblem.Light), (grid, occupancy, stackModes) => new LightTargetSkill(grid, occupancy, stackModes) },
                { (TowerColor.Red, TowerEmblem.Darkness), (grid, occupancy, stackModes) => new DarknessSplashSkill(grid, occupancy, stackModes, CurrentGrounds()) },
                { (TowerColor.Blue, TowerEmblem.Darkness), (grid, occupancy, stackModes) => new DarknessTargetSkill(grid, occupancy, stackModes) }
            };

        private static GroundEffectRegistry CurrentGrounds() => BoardManager.Instance != null ? BoardManager.Instance.Grounds : null;
        private static TowerSlots CurrentSlots() => BoardManager.Instance != null ? BoardManager.Instance.Slots : null;
        private static int CurrentLimit() => GameSession.Instance != null ? GameSession.Instance.CurrentLimit : 20;
        private static int CurrentGold() => CurrencyManager.Instance != null ? CurrencyManager.Instance.Gold : 0;
        private static int CurrentCycle() => WaveManager.Instance != null && WaveManager.Instance.CurrentWave > 0 ? WaveManager.Instance.Cycle : 1;
        private static void AddGem(int amount) { if (CurrencyManager.Instance != null) CurrencyManager.Instance.AddGem(amount); }
        private static PathOccupancy CurrentOccupancy() => BoardManager.Instance != null ? BoardManager.Instance.Occupancy : null;
        private static UnityEngine.Vector3 PathCellPosition(int pathIndex) =>
            BoardManager.Instance != null ? BoardManager.Instance.GetWorldPosition(BoardManager.Instance.Grid.GetPathCell(pathIndex)) : UnityEngine.Vector3.zero;
        private static float CurrentDarkness() => EmblemSharedState.Current.DarknessCount;

        public static AttackSkill CreateAttackSkill(TowerColor color, TowerEmblem emblem,
                                                    BoardGrid grid, PathOccupancy occupancy, StackModeSettings stackModes)
        {
            if (s_attackSkills.TryGetValue((color, emblem), out var create)) return create(grid, occupancy, stackModes);
            return null;
        }
    }
}
