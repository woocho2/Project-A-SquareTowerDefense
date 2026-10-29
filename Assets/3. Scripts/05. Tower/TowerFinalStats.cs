using System.Collections.Generic;

namespace SquareTD
{
    // 계산이 끝난 타워의 최종 스탯과 출처
    public readonly struct StatSource
    {
        public readonly string Source;
        public readonly StatType Stat;
        public readonly float Amount;

        public StatSource(string source, StatType stat, float amount)
        {
            Source = source;
            Stat = stat;
            Amount = amount;
        }
    }

    public class TowerFinalStats
    {
        public static readonly int StatCount = System.Enum.GetValues(typeof(StatType)).Length;

        private readonly float[] m_values;
        private readonly float[] m_baseValues;

        public int ColorLevel { get; }
        public IReadOnlyList<StatSource> Sources { get; }

        public TowerFinalStats(float[] values, float[] baseValues, int colorLevel, IReadOnlyList<StatSource> sources)
        {
            m_values = (float[])values.Clone();
            m_baseValues = (float[])baseValues.Clone();
            ColorLevel = colorLevel;
            Sources = sources;
        }

        public float Get(StatType stat) => m_values[(int)stat];
        public float GetBase(StatType stat) => m_baseValues[(int)stat];

        public float Power => Get(StatType.Power);
        public int Range => (int)Get(StatType.Range);
        public int Action => (int)Get(StatType.MaxAction);
        public int AttackCount => (int)Get(StatType.AttackCount);
        public float CriticalRate => Get(StatType.CritRate);
        public float CriticalDamage => Get(StatType.CritDamage);
        public int Mastery => (int)Get(StatType.Mastery);
        public int Stack => (int)Get(StatType.MaxStack);
        public int Duration => (int)Get(StatType.Duration);
        public float AbilityValue => Get(StatType.AbilityValue);
        public float StackValue => Get(StatType.StackValue);
        public float FinalDamageMultiplier => 1f + Get(StatType.FinalDamage) + Get(StatType.BonusDamage);
        public float ArmorPenetration => Get(StatType.ArmorPenetration);
        public bool ForcesFixedDamage => Get(StatType.ForceFixedDamage) > 0f;

        public float GetDistanceMultiplier(int distance)
        {
            float perTile = Get(StatType.DistanceDamage);
            if (perTile <= 0f) return 1f;

            float pierceBonus = Get(StatType.DistanceDamageBonus);
            int tiles = pierceBonus > 0f ? Range : UnityEngine.Mathf.Clamp(distance, 0, Range);
            return 1f + perTile * tiles + pierceBonus;
        }
    }
}
