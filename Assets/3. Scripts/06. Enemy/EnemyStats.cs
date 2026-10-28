using UnityEngine;

namespace SquareTD
{
    // 한 번 행동할 때의 이동 범위와 행동 주기
    public readonly struct MoveRange
    {
        public readonly int Min;
        public readonly int Max;
        public readonly int Action;

        public MoveRange(int min, int max, int action)
        {
            Min = min;
            Max = max;
            Action = action;
        }
    }

    public class EnemyStats
    {
        private readonly float[] m_base = new float[TowerFinalStats.StatCount];

        public StatModifierList Modifiers { get; } = new StatModifierList();

        public EnemyStats(EnemyDefinition definition, int spawnWave)
        {
            m_base[(int)StatType.Defense] = definition.Defense * WaveTable.GetDefenseMultiplier(spawnWave, definition.Kind);
            m_base[(int)StatType.MaxAction] = definition.Action;
            m_base[(int)StatType.SpeedMax] = definition.SpeedMax;
            m_base[(int)StatType.SpeedMin] = definition.SpeedMin;
            m_base[(int)StatType.DamageTaken] = 1f;
            m_base[(int)StatType.HealingTaken] = 1f;
            m_base[(int)StatType.MapBuffRate] = 1f;
        }

        public float GetBase(StatType stat) => m_base[(int)stat];

        public float Get(StatType stat) => Modifiers.Apply(stat, GetBase(stat));

        public MoveRange GetMoveRange()
        {
            int min = Mathf.RoundToInt(Get(StatType.SpeedMin));
            int max = Mathf.RoundToInt(Get(StatType.SpeedMax));
            int action = Mathf.RoundToInt(Get(StatType.MaxAction));

            int shortage = Mathf.Max(0, 1 - min);
            
            min = Mathf.Max(1, min);
            max = Mathf.Max(min, max);
            action = Mathf.Max(1, action + shortage);

            return new MoveRange(min, max, action);
        }
    }
}
