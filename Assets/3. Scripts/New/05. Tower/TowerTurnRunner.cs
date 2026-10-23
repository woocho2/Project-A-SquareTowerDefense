using System.Collections;
using UnityEngine;

namespace SquareTD
{
    public class TowerTurnRunner
    {
        private readonly TowerSlots m_slots;
        private readonly float m_safetySeconds;

        public TowerTurnRunner(TowerSlots slots, float safetySeconds)
        {
            m_slots = slots;
            m_safetySeconds = safetySeconds;
        }

        public void RunSupportTowers()
        {
            for (int i = 0; i < m_slots.Count; i++)
            {
                TowerController tower = m_slots.Get(i);
                if (tower == null || tower.IsAttackTower) continue;

                if (tower.TickAction()) Debug.Log($"[TowerTurnRunner] ② {tower.name} 행동");
            }
        }

        public IEnumerator RunAttackTowers()
        {
            for (int i = 0; i < m_slots.Count; i++)
            {
                TowerController tower = m_slots.Get(i);
                if (tower == null || !tower.IsAttackTower) continue;
                if (!tower.TickAction()) continue;

                Debug.Log($"[TowerTurnRunner] ④ {tower.name} 행동");
                var handle = new AttackHandle();
                yield return tower.Act(handle);
                yield return WaitForHandle(tower, handle);
            }            
        }

        private IEnumerator WaitForHandle(TowerController tower, AttackHandle handle)
        {
            float elapsed = 0f;
            while (!handle.IsDone)
            {
                if (elapsed >= m_safetySeconds)
                {
                    Debug.LogWarning($"[TowerTurnRunner] {tower.name}의 탄 {handle.PendingShots}발이 {m_safetySeconds}초 안에 닿지 않아 끝냄");
                    handle.ForceComplete();
                    yield break;
                }

                elapsed += Time.deltaTime;
                yield return null;
            }
        }
    }
}
