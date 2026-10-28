using System;
using System.Collections.Generic;

namespace SquareTD
{
    // 판 위에 깔린 장판 목록 (21번 #28, 수업 29). BoardManager가 하나 가짐
    public class GroundEffectRegistry
    {
        private readonly List<GroundEffect> m_effects = new List<GroundEffect>();

        public IReadOnlyList<GroundEffect> All => m_effects;
        public event Action Changed;

        public void Add(GroundEffect effect)
        {
            m_effects.Add(effect);
            Changed?.Invoke();
        }

        public List<GroundEffect> GetAt(int pathIndex) => m_effects.FindAll(effect => effect.Covers(pathIndex));

        // ⑤: 깔린 순서대로 그 턴의 일 → 남은 턴 −1 → 끝난 것을 지움
        public void RunTurn()
        {
            if (m_effects.Count == 0) return;

            foreach (GroundEffect effect in new List<GroundEffect>(m_effects))     // 도는 중에 새로 깔려도 되게 복사
            {
                effect.OnTurn();
                effect.CountDown();
            }
            m_effects.RemoveAll(effect => effect.IsExpired);
            Changed?.Invoke();
        }

        public void Clear()
        {
            m_effects.Clear();
            Changed?.Invoke();
        }
    }
}
