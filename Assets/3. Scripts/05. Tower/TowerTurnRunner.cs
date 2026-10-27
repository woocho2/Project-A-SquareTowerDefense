using System;
using System.Collections;
using UnityEngine;

namespace SquareTD
{
    public class TowerTurnRunner
    {
        private readonly TowerSlots m_slots;
        private readonly float m_safetySeconds;
        private readonly ZoneRegistry m_zones;
        private readonly Func<IEnumerator> m_waitForVisuals;

        public static bool LogActions = false;      // "② / ④ 누구 행동" 로그 켜기·끄기 (2026-10-27 사용자 요청으로 끔, [Skill] 로그는 그대로)

        // waitForVisuals: 공격 타워 한 기가 끝날 때마다 "연출이 끝날 때까지 기다리기" (TurnManager가 EffectActivity.WaitUntilIdle을 넘김)
        //   타워 쪽은 연출을 모르게 밖에서 받음. 테스트처럼 안 넘기면 기다리지 않음
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

                if (!tower.TickAction()) continue;                  // 광역의 스킬 2는 착탄마다 셈 (TowerController.OnSplashShotLanded)

                if (LogActions) Debug.Log($"[TowerTurnRunner] ④ {tower.name} 행동");
                var handle = new AttackHandle();
                yield return tower.Act(handle);
                yield return WaitForHandle(tower, handle);
                yield return WaitForVisuals();                  // 기본 공격의 명중 연출이 끝난 뒤에

                while (tower.FireNextPendingSkill())            // 이번 행동에서 찬 스킬 2를 하나씩: 쏘고 → 연출이 끝나면 다음
                {
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
