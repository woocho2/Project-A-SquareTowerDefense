namespace SquareTD
{
    // 타워가 받는 시간 버프 (21번 #41, 수업 28) — 광폭화, (수업 36) 폭풍·순풍·질풍, (수업 39) 광채·광휘
    //   수정자는 State.Modifiers에 출처 이름으로 넣고, 남은 행동 수는 State.TimedBuffs[출처]에 둠
    //   다시 걸리면 겹치지 않고 처음부터 (같은 출처를 떼고 새로)
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

        // 그 타워의 행동이 끝날 때마다 (TowerController.Act). 0이 되면 뗌
        //   행동이 끝난 뒤에 세서, 걸린 다음 행동부터 정확히 n번 (스킬 2는 행동이 끝난 뒤에 쏘므로)
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
