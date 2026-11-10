using UnityEngine;

namespace SquareTD
{
    // 적의 저항력 (약한 CC 감소, 강한 CC 확률). 방패 디버프가 깎는 만큼(Cut)은 기본 저항력에서 뺌
    public class EnemyResistance
    {
        public const float Max = 100f;

        public float Base { get; }
        public float Cut { get; private set; }
        public float Effective => Mathf.Max(0f, Base - Cut);
        public float Current { get; private set; }

        public EnemyResistance(float basePercent)
        {
            Base = Mathf.Clamp(basePercent, 0f, Max);
            Current = Base;
        }

        public bool TryStrongCC() => TryStrongCC(Random.value * Max);

        public bool TryStrongCC(float roll)
        {
            if (roll < Mathf.Max(0f, Current - Cut)) return false;
            Current = Mathf.Min(Current + Base, Max);

            return true;
        }

        public void OnActed()
        {
            Current = Mathf.Max(Base, Current - Base * 0.5f);
        }

        public float ResistWeak(float value) => SkillFormula.Resist(value, Effective);

        public void AddCut(float points) => Cut += points;
        public void RemoveCut(float points) => Cut = Mathf.Max(0f, Cut - points);

        public void Reset()
        {
            Current = Base;
            Cut = 0f;
        }
    }
}
