using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(EnemyHealthController))]
[RequireComponent(typeof(EnemyMovementController))]
public class EnemyDebuffController : MonoBehaviour
{
    private readonly List<DebuffBase> m_activeDebuffs = new List<DebuffBase>();
    private EnemyHealthController m_health;
    private EnemyMovementController m_movement;

#if false // Synergy system temporarily disabled
    private float m_heatwaveTickTimer = 0f;
    private const float HEATWAVE_INTERVAL = 1f;

#endif

    private void Awake()
    {
        m_health = GetComponent<EnemyHealthController>();
        m_movement = GetComponent<EnemyMovementController>();
    }

    private void Update()
    {
        if (m_health.CurrentHP <= 0f) return;

        // UpdateSynergyHeatWave(Time.deltaTime);

        for (int i = m_activeDebuffs.Count - 1; i >= 0; i--)
        {
            DebuffBase debuff = m_activeDebuffs[i];
            debuff.OnUpdate(m_health, m_movement, Time.deltaTime);

            if (debuff.Duration <= 0f)
            {
                debuff.OnRemove(m_health, m_movement);
                m_activeDebuffs.RemoveAt(i);
            }
        }
    }

    public void AddDebuff(DebuffBase newDebuff)
    {
        if (newDebuff == null || m_health.CurrentHP <= 0f) return;

        DebuffBase existing = m_activeDebuffs.Find(d => d.Target == newDebuff.Target);
        if (existing != null)
        {
            existing.OnRefresh(m_health, m_movement, newDebuff.Duration, newDebuff.Value);
        }
        else
        {
            newDebuff.OnApply(m_health, m_movement);
            m_activeDebuffs.Add(newDebuff);
        }
    }

    // 풀 반환 및 사망 시 호출되는 핵심 초기화 함수
    public void ClearAllDebuffs()
    {
        // 1. 디버프 컨트롤러에 실행 중인 모든 코루틴(DoT 데미지, 슬로우 등) 강제 종료
        StopAllCoroutines();

        // 2. 적용 중인 디버프 데이터 해제 및 리스트 비우기
        for (int i = 0; i < m_activeDebuffs.Count; i++)
        {
            m_activeDebuffs[i].OnRemove(m_health, m_movement);
        }
        m_activeDebuffs.Clear();

        // 3. 시너지 및 스탯 배율 초기화

        if (m_movement != null)
        {

            m_movement.SetSpeedMultiplier(1f);
        }
        if (m_health != null)
        {

            m_health.SetVulnerability(1f);
            m_health.SetDefendMultiplier(1f);
        }
    }

#if false // Synergy system temporarily disabled
    private void UpdateSynergyHeatWave(float deltaTime)
    {
        if (TowerManager.Instance == null) return;

        if (TowerManager.Instance.IsHeatWaveActive)
        {
            m_movement.SetSynergySlow(0.5f);
            m_health.SetSynergyWeak(1.5f);

            m_heatwaveTickTimer += deltaTime;
            if (m_heatwaveTickTimer >= HEATWAVE_INTERVAL)
            {
                m_heatwaveTickTimer -= HEATWAVE_INTERVAL;
                m_health.ApplyDamage(m_health.MaxHP * 0.01f, false);
            }
        }
        else
        {


    
        }
    }
#endif

}