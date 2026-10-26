using System.Collections.Generic;

namespace SquareTD
{
    public class StackModeSettings
    {
        public bool ManualByDefault { get; set; }           // false = 전체 자동 (기본)

        private readonly Dictionary<(TowerColor, TowerEmblem), bool> m_exceptions = new Dictionary<(TowerColor, TowerEmblem), bool>();

        public void SetException(TowerColor color, TowerEmblem emblem, bool manual)
        {
            m_exceptions[(color, emblem)] = manual;
        }

        public void ClearException(TowerColor color, TowerEmblem emblem) => m_exceptions.Remove((color, emblem));

        public bool IsManual(TowerColor color, TowerEmblem emblem)
        {
            if (m_exceptions.TryGetValue((color, emblem), out bool manual)) return manual;
            return ManualByDefault;
        }

        public bool IsManual(TowerDefinition definition) => IsManual(definition.Color, definition.Emblem);

        // 스킬 2를 쏠 때 스택을 쓸지: 다 찼고, (자동이거나 수동 요청이 있으면)
        //   수동인데 누르지 않았으면 스킬 2는 기본 피해로 나가고 스택은 다 찬 채로 남음
        public bool ShouldUseStack(TowerDefinition definition, TowerStack stack)
        {
            return stack.IsFull && (!IsManual(definition) || stack.ManualRequested);
        }
    }
}
