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
            { TowerEmblem.Hammer, new HammerBuffRule(CurrentGold) },
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
            { TowerEmblem.Fire, new FireDebuffRule() },
            { TowerEmblem.Water, new WaterDebuffRule() },
            { TowerEmblem.Earth, new EarthDebuffRule() },
            { TowerEmblem.Wind, new WindDebuffRule() },
            { TowerEmblem.Ice, new IceDebuffRule() },
            { TowerEmblem.Electricity, new ElectricDebuffRule() },
            { TowerEmblem.Light, new LightDebuffRule() },
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

        private static readonly Dictionary<(TowerColor, TowerEmblem), Func<BoardGrid, PathOccupancy, AttackSkill>> s_attackSkills =
            new Dictionary<(TowerColor, TowerEmblem), Func<BoardGrid, PathOccupancy, AttackSkill>>
            {
                { (TowerColor.Red, TowerEmblem.Sword), (grid, occupancy) => new SwordSplashSkill(grid, occupancy) },
                { (TowerColor.Blue, TowerEmblem.Sword), (grid, occupancy) => new SwordTargetSkill(grid, occupancy) },
                { (TowerColor.Red, TowerEmblem.Axe), (grid, occupancy) => new AxeSplashSkill(grid, occupancy) },
                { (TowerColor.Blue, TowerEmblem.Axe), (grid, occupancy) => new AxeTargetSkill(grid, occupancy) },
                { (TowerColor.Red, TowerEmblem.Bow), (grid, occupancy) => new ArrowSplashSkill(grid, occupancy, CurrentGrounds()) },
                { (TowerColor.Blue, TowerEmblem.Bow), (grid, occupancy) => new ArrowTargetSkill(grid, occupancy) },
                { (TowerColor.Red, TowerEmblem.Spear), (grid, occupancy) => new SpearSplashSkill(grid, occupancy, CurrentSlots()) },
                { (TowerColor.Blue, TowerEmblem.Spear), (grid, occupancy) => new SpearTargetSkill(grid, occupancy) },
                { (TowerColor.Red, TowerEmblem.Shield), (grid, occupancy) => new ShieldSplashSkill(grid, occupancy) },
                { (TowerColor.Blue, TowerEmblem.Shield), (grid, occupancy) => new ShieldTargetSkill(grid, occupancy) },
                { (TowerColor.Red, TowerEmblem.Hammer), (grid, occupancy) => new HammerSplashSkill(grid, occupancy) },
                { (TowerColor.Blue, TowerEmblem.Hammer), (grid, occupancy) => new HammerTargetSkill(grid, occupancy) },
                { (TowerColor.Red, TowerEmblem.Fire), (grid, occupancy) => new FireSplashSkill(grid, occupancy) },
                { (TowerColor.Blue, TowerEmblem.Fire), (grid, occupancy) => new FireTargetSkill(grid, occupancy) },
                { (TowerColor.Red, TowerEmblem.Water), (grid, occupancy) => new WaterSplashSkill(grid, occupancy) },
                { (TowerColor.Blue, TowerEmblem.Water), (grid, occupancy) => new WaterTargetSkill(grid, occupancy) },
                { (TowerColor.Red, TowerEmblem.Earth), (grid, occupancy) => new EarthSplashSkill(grid, occupancy) },
                { (TowerColor.Blue, TowerEmblem.Earth), (grid, occupancy) => new EarthTargetSkill(grid, occupancy) },
                { (TowerColor.Red, TowerEmblem.Wind), (grid, occupancy) => new WindSplashSkill(grid, occupancy) },
                { (TowerColor.Blue, TowerEmblem.Wind), (grid, occupancy) => new WindTargetSkill(grid, occupancy) },
                { (TowerColor.Red, TowerEmblem.Ice), (grid, occupancy) => new IceSplashSkill(grid, occupancy) },
                { (TowerColor.Blue, TowerEmblem.Ice), (grid, occupancy) => new IceTargetSkill(grid, occupancy) },
                { (TowerColor.Red, TowerEmblem.Electricity), (grid, occupancy) => new ElectricSplashSkill(grid, occupancy) },
                { (TowerColor.Blue, TowerEmblem.Electricity), (grid, occupancy) => new ElectricTargetSkill(grid, occupancy) },
                { (TowerColor.Red, TowerEmblem.Light), (grid, occupancy) => new LightSplashSkill(grid, occupancy) },
                { (TowerColor.Blue, TowerEmblem.Light), (grid, occupancy) => new LightTargetSkill(grid, occupancy) },
                { (TowerColor.Red, TowerEmblem.Darkness), (grid, occupancy) => new DarknessSplashSkill(grid, occupancy, CurrentGrounds()) },
                { (TowerColor.Blue, TowerEmblem.Darkness), (grid, occupancy) => new DarknessTargetSkill(grid, occupancy) }
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
                                                    BoardGrid grid, PathOccupancy occupancy)
        {
            if (s_attackSkills.TryGetValue((color, emblem), out var create)) return create(grid, occupancy);
            return null;
        }
    }
}
