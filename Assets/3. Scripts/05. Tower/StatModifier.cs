using System;
using System.Collections.Generic;

namespace SquareTD
{
    // 스탯 수정자 하나와 그 목록 (출처, 스탯, 값)
    public readonly struct StatModifier
    {
        public readonly string Source;
        public readonly StatType Stat;
        public readonly float Value;
        public readonly bool IsPercent;

        private StatModifier(string source, StatType stat, float value, bool isPercent)
        {
            Source = source;
            Stat = stat;
            Value = value;
            IsPercent = isPercent;
        }

        public static StatModifier Percent(string source, StatType stat, float value) => new StatModifier(source, stat, value, true);
        public static StatModifier Flat(string source, StatType stat, float value) => new StatModifier(source, stat, value, false);
    }

    public class StatModifierList
    {
        private readonly List<StatModifier> m_modifiers = new List<StatModifier>();
        private int m_batch;
        private bool m_dirty;

        public IReadOnlyList<StatModifier> All => m_modifiers;
        public event Action Changed;

        public void BeginBatch() => m_batch++;

        public void EndBatch()
        {
            if (--m_batch > 0 || !m_dirty) return;
            m_dirty = false;
            Changed?.Invoke();
        }

        private void Notify()
        {
            if (m_batch > 0) m_dirty = true;
            else Changed?.Invoke();
        }

        public void Add(StatModifier modifier)
        {
            m_modifiers.Add(modifier);
            Notify();
        }

        public int RemoveBySource(string source)
        {
            int removed = 0;
            for (int i = m_modifiers.Count - 1; i >= 0; i--)
            {
                if (m_modifiers[i].Source != source) continue;
                m_modifiers.RemoveAt(i);
                removed++;
            }
            if (removed > 0) Notify();
            return removed;
        }

        public void SetSource(string source, IEnumerable<StatModifier> modifiers)
        {
            BeginBatch();
            RemoveBySource(source);
            if (modifiers != null) foreach (StatModifier modifier in modifiers) Add(modifier);
            EndBatch();
        }

        public (float percent, float flat) Sum(StatType stat)
        {
            float percent = 0f;
            float flat = 0f;

            foreach (StatModifier modifier in m_modifiers)
            {
                if (modifier.Stat != stat) continue;

                if (modifier.IsPercent) percent += modifier.Value;
                else flat += modifier.Value;
            }

            return (percent, flat);
        }

        public float Apply(StatType stat, float baseValue)
        {
            (float percent, float flat) = Sum(stat);

            return baseValue * (1+percent) + flat;
        }
    }
}
