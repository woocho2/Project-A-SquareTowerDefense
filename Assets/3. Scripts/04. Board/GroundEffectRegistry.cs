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

        // 다음 이동을 정할 때: 끌어당기는 장판(블랙홀)이 있으면 인덱스가 가장 가까운 하나를 따름 (20번 4-16장, 고리를 돌아 재지 않음)
        public MoveRange DecideMove(EnemyController enemy, MoveRange range)
        {
            GroundEffect nearest = null;
            int best = int.MaxValue;
            foreach (GroundEffect effect in m_effects)
            {
                if (effect.PullCenter < 0) continue;
                int distance = Math.Abs(effect.PullCenter - enemy.PathIndex);
                if (distance < best)
                {
                    best = distance;
                    nearest = effect;
                }
            }
            return nearest != null ? nearest.ModifyMove(enemy, range) : range;
        }

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
