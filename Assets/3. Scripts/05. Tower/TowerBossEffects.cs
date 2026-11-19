using System;
using System.Collections.Generic;

namespace SquareTD
{
    // 타워에 걸린 보스 기믹과 적 턴별 독립 만료
    public sealed class TowerBossEffects
    {
        public const string FenrirSource = "펜리르";
        public const string SurtrSource = "수르트";

        private sealed class Entry
        {
            public int Until;
            public float Value;
        }

        private readonly List<Entry> m_fenrir = new List<Entry>();
        private readonly List<Entry> m_surtr = new List<Entry>();
        private int m_singleUntil;
        public BossMechanicKind Single { get; private set; }
        public TowerDefinition OriginalDefinition { get; private set; }
        public int[] OriginalZoneCells { get; set; }
        public bool IsFrozen => Single == BossMechanicKind.Ymir;
        public bool BlocksProgression => Single != BossMechanicKind.None;
        public int ActionPenalty => Math.Min(5, m_fenrir.Count);
        public int AttackCountPenalty => Math.Min(3, m_surtr.Count);
        public float PowerReduction
        {
            get
            {
                float largest = 0f;
                foreach (Entry entry in m_surtr) largest = Math.Max(largest, entry.Value);
                return largest;
            }
        }

        public event Action Changed;

        public bool TryApplySingle(BossMechanicKind kind, int turns, int currentTurn, TowerDefinition original)
        {
            if (Single != BossMechanicKind.None || turns <= 0) return false;
            if (kind != BossMechanicKind.Loki && kind != BossMechanicKind.Ymir && kind != BossMechanicKind.Fafnir) return false;
            Single = kind;
            OriginalDefinition = original;
            m_singleUntil = currentTurn + turns;
            Changed?.Invoke();
            return true;
        }

        public void AddFenrir(int turns, int currentTurn)
        {
            if (turns <= 0) return;
            m_fenrir.Add(new Entry { Until = currentTurn + turns });
            Changed?.Invoke();
        }

        public void AddSurtr(int dice, int currentTurn)
        {
            if (dice <= 0) return;
            m_surtr.Add(new Entry { Until = currentTurn + dice, Value = dice * 0.05f });
            Changed?.Invoke();
        }

        public TowerDefinition EndTurn(int currentTurn)
        {
            bool changed = m_fenrir.RemoveAll(entry => entry.Until <= currentTurn) > 0;
            changed |= m_surtr.RemoveAll(entry => entry.Until <= currentTurn) > 0;
            TowerDefinition restore = null;
            if (Single != BossMechanicKind.None && m_singleUntil <= currentTurn)
            {
                if (Single != BossMechanicKind.Ymir) restore = OriginalDefinition;
                Single = BossMechanicKind.None;
                OriginalDefinition = null;
                OriginalZoneCells = null;
                changed = true;
            }
            if (changed) Changed?.Invoke();
            return restore;
        }

        public void AddModifiersTo(StatModifierList list)
        {
            if (ActionPenalty > 0) list.Add(StatModifier.Flat(FenrirSource, StatType.MaxAction, ActionPenalty));
            if (PowerReduction > 0f) list.Add(StatModifier.Percent(SurtrSource, StatType.Power, -PowerReduction));
            if (AttackCountPenalty > 0) list.Add(StatModifier.Flat(SurtrSource, StatType.AttackCount, -AttackCountPenalty));
        }
    }
}
