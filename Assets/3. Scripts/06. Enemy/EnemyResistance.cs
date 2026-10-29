using UnityEngine;

namespace SquareTD
{
    // 적의 저항력 (약한 CC 감소, 강한 CC 확률)
    public class EnemyResistance
    {
        public const float Max = 100f;

        public float Base { get; }
        public float Current { get; private set; }

        public EnemyResistance(float basePercent)
        {
            Base = Mathf.Clamp(basePercent, 0f, Max);
            Current = Base;
        }

        public bool TryStrongCC() => TryStrongCC(Random.value * Max);

        public bool TryStrongCC(float roll)
        {
            if (roll < Current) return false;
            Current = Mathf.Min(Current + Base, Max);

            return true;
        }

        public void OnActed()
        {
            Current = Mathf.Max(Base, Current - Base * 0.5f);
        }

        public float ResistWeak(float value) => SkillFormula.Resist(value, Base);

        public void Reset() => Current = Base;
    }
}
