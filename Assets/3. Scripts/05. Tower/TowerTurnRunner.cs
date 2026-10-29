using System;
using System.Collections;
using UnityEngine;

namespace SquareTD
{
    // 적 턴에 타워들을 차례로 행동시킴
    public class TowerTurnRunner
    {
        private readonly TowerSlots m_slots;
        private readonly float m_safetySeconds;
        private readonly ZoneRegistry m_zones;
        private readonly Func<IEnumerator> m_waitForVisuals;

        public static bool LogActions = false;

        public TowerTurnRunner(TowerSlots slots, float safetySeconds, ZoneRegistry zones = null, Func<IEnumerator> waitForVisuals = null)
        {
            m_slots = slots;
            m_safetySeconds = safetySeconds;
            m_zones = zones;
            m_waitForVisuals = waitForVisuals;
        }

        public IEnumerator RunSupportTowers()
        {
            for (int i = 0; i < m_slots.Count; i++)
            {
                TowerController tower = m_slots.Get(i);
                if (tower == null || tower.IsAttackTower) continue;
                if (!tower.TickAction()) continue;

                if (LogActions) Debug.Log($"[TowerTurnRunner] ② {tower.name} 행동");
                yield return tower.Act(new AttackHandle());
            }
        }

        public void RunEnemyTurnStart()
        {
            if (m_zones != null) BuffZoneAction.RunZoneTurns(m_zones, m_slots);

            for (int i = 0; i < m_slots.Count; i++)
            {
                TowerController tower = m_slots.Get(i);
                if (tower == null || !tower.IsAttackTower) continue;

                if (m_zones != null) BuffZoneAction.RefreshReceived(tower, m_zones);

                tower.OnEnemyTurnStart();
            }
        }

        public IEnumerator RunAttackTowers()
        {
            for (int i = 0; i < m_slots.Count; i++)
            {
                TowerController tower = m_slots.Get(i);
                if (tower == null || !tower.IsAttackTower) continue;

                if (!tower.TickAction()) continue;

                if (LogActions) Debug.Log($"[TowerTurnRunner] ④ {tower.name} 행동");
                var handle = new AttackHandle();
                yield return tower.Act(handle);
                yield return WaitForHandle(tower, handle);
                yield return WaitForVisuals();

                while (tower.HasPendingSkill)
                {
                    if (tower.WillFireNextSkill()) yield return SkillCutIn.Play(tower);
                    if (!tower.FireNextPendingSkill()) break;
                    yield return WaitForVisuals();
                }
            }
        }

        private IEnumerator WaitForVisuals()
        {
            if (m_waitForVisuals != null) yield return m_waitForVisuals();
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
