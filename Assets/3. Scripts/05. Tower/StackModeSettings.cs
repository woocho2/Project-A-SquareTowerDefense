using System.Collections.Generic;

namespace SquareTD
{
    // 스택을 자동으로 쓸지 수동으로 쓸지 설정
    public class StackModeSettings
    {
        public bool ManualByDefault { get; set; }

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

        public bool ShouldUseStack(TowerDefinition definition, TowerStack stack)
        {
            return stack.IsFull && (!IsManual(definition) || stack.ManualRequested);
        }
    }
}
