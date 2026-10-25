using System;
using System.Collections.Generic;

namespace SquareTD
{
    // 버프·디버프 한 줄: (출처, 스탯, 값, % 인지 정수인지)
    public readonly struct StatModifier
    {
        public readonly string Source;      // 카드에 보일 이름이자 지울 때 쓰는 열쇠 (예: "공격력 타일", "기사의 검 (12번 타워)")
        public readonly StatType Stat;
        public readonly float Value;        // %면 0.5 = +50%, 정수면 그 수만큼 더함. 디버프는 음수
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

    // 한 대상(타워·적)이 받는 수정자 전부. 20번 3장: 최종값 = 기본값 × (1 + %의 합) + 정수의 합. 곱하지 않고 더함
    public class StatModifierList
    {
        private readonly List<StatModifier> m_modifiers = new List<StatModifier>();

        public IReadOnlyList<StatModifier> All => m_modifiers;      // 카드의 "어디서 얼마" 표시용
        public event Action Changed;                                // 더하거나 지우면 → 타워가 스탯을 다시 계산

        public void Add(StatModifier modifier)
        {
            m_modifiers.Add(modifier);
            Changed?.Invoke();
        }

        public int RemoveBySource(string source)
        {
            int removed = m_modifiers.RemoveAll(m => m.Source == source);
            if (removed > 0) Changed?.Invoke();
            return removed;
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
