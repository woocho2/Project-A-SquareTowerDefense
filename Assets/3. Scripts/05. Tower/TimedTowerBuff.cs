namespace SquareTD
{
    // 타워가 받는 시간 버프 (남은 행동 수만큼 유지)
    public static class TimedTowerBuff
    {
        public static void Apply(TowerState state, string source, int actions, params StatModifier[] modifiers)
        {
            Remove(state, source);
            foreach (StatModifier modifier in modifiers) state.Modifiers.Add(modifier);
            state.TimedBuffs[source] = actions;
        }

        public static void Remove(TowerState state, string source)
        {
            state.Modifiers.RemoveBySource(source);
            state.TimedBuffs.Remove(source);
        }

        public static void CountDown(TowerState state)
        {
            var sources = new System.Collections.Generic.List<string>(state.TimedBuffs.Keys);
            foreach (string source in sources)
            {
                int left = state.TimedBuffs[source] - 1;
                if (left > 0) state.TimedBuffs[source] = left;
                else Remove(state, source);
            }
        }

        public static int GetRemaining(TowerState state, string source) =>
            state.TimedBuffs.TryGetValue(source, out int left) ? left : 0;
    }
}
