using UnityEngine;

namespace SquareTD
{
    // 적의 저항력 (약한 CC는 기본 저항력만큼 감소, 강한 CC는 지금 저항력 확률로 막음. 걸리면 +기본, 막으면 −기본). 방패 디버프가 깎는 만큼(Cut)은 뺌
    public class EnemyResistance
    {
        public const float Max = 100f;

        private readonly float m_base;
        private float m_current;
        private float m_bossMultiplier = 1f;
        public float Base => Mathf.Min(Max, m_base * m_bossMultiplier);
        public float Cut { get; private set; }
        public float Effective => Mathf.Max(0f, Base - Cut);
        public float Current => Mathf.Min(Max, m_current * m_bossMultiplier);

        public EnemyResistance(float basePercent)
        {
            m_base = Mathf.Clamp(basePercent, 0f, Max);
            m_current = m_base;
        }

        public bool TryStrongCC() => TryStrongCC(Random.value * Max);

        public bool TryStrongCC(float roll)
        {
            if (roll < Mathf.Max(0f, Current - Cut))
            {
                m_current = Mathf.Max(m_base, m_current - m_base);
                return false;
            }
            m_current = Mathf.Min(m_current + m_base, Max);
            return true;
        }

        public float ResistWeak(float value) => SkillFormula.Resist(value, Effective);

        public void AddCut(float points) => Cut += points;
        public void RemoveCut(float points) => Cut = Mathf.Max(0f, Cut - points);

        public void Reset()
        {
            m_current = m_base;
            Cut = 0f;
        }

        public void SetBossMultiplier(float multiplier) => m_bossMultiplier = Mathf.Max(0f, multiplier);
    }
}
