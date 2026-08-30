using UnityEngine;
using System.Collections;

// 가독성을 높이기 위한 타워 ID 상수 매핑 (기획된 실제 ID로 숫자 변경 필요)
public static class BlackTowerID
{
    public const int SWORD = 2001;
    public const int BOW = 2002;
    public const int SHIELD = 2003;
    public const int SPEAR = 2004;
    public const int AXE = 2005;
    public const int HAMMER = 2006;
    public const int FIRE = 2007;
    public const int ICE = 2008;
    public const int ELECTRICITY = 2009;
    public const int WIND = 2010;
    public const int EARTH = 2011;
    public const int LIGHT = 2012;
    public const int DARKNESS = 2013;
}

public class DebuffAction : TowerAttackAction
{
    private DebuffZone m_activeZone;

    public DebuffAction(TowerData data) : base(data) { }

    // TowerController에서 타워 초기화 시 호출하여 장판 스폰
    public void SpawnZone(DebuffZone prefab, Vector3 spawnPos, TowerStats currentStats)
    {
        if (prefab == null) return;

        m_activeZone = Object.Instantiate(prefab, spawnPos, Quaternion.identity);
        m_activeZone.transform.localScale = new Vector3(currentStats.Range, currentStats.Range, 1f);

        // 장판의 센서 이벤트 구독
        m_activeZone.OnEnemyEnterEvent += HandleEnemyEnter;
        m_activeZone.OnEnemyExitEvent += HandleEnemyExit;
    }

    // 장판을 밟는 즉시 발동하는 패시브/스택형 효과
    private void HandleEnemyEnter(EnemyController enemy)
    {
        switch (m_data.towerID)
        {
            case BlackTowerID.SWORD:
                enemy.ApplyStack(DebuffType.Curse, 1); break;
            case BlackTowerID.BOW:
                enemy.ApplyStack(DebuffType.Bleeding, 1); break;
            case BlackTowerID.SPEAR:
                enemy.ApplyStack(DebuffType.Javelin, 1); break;
            case BlackTowerID.AXE:
                enemy.ApplyStack(DebuffType.Vulnerable, 1); break;
            case BlackTowerID.ICE:
                enemy.ApplyStack(DebuffType.Slow, 1); break;
            case BlackTowerID.ELECTRICITY:
                enemy.ApplyStack(DebuffType.Shock, 1); break;
            case BlackTowerID.HAMMER:
                enemy.ApplyStatModifier(StatType.Armor, -m_data.abilityValue); break;
        }
    }

    // 장판 이탈 시 상태 복구
    private void HandleEnemyExit(EnemyController enemy)
    {
        if (m_data.towerID == BlackTowerID.HAMMER)
        {
            enemy.RemoveStatModifier(StatType.Armor, m_data.abilityValue);
        }
    }

    // 타워 공격 주기(쿨타임)마다 발동되는 액티브/광역 효과
    public override bool ExecuteAction(Transform towerTransform, TowerStats currentStats)
    {
        if (m_activeZone == null) return false;

        // Shield는 적이 장판에 없어도 길을 막아야 하므로 예외 처리
        if (m_activeZone.EnemiesInZone.Count == 0 && m_data.towerID != BlackTowerID.SHIELD)
            return false;

        switch (m_data.towerID)
        {
            case BlackTowerID.SHIELD:
                towerTransform.GetComponent<MonoBehaviour>().StartCoroutine(ShieldBlockRoutine(currentStats.AbilityValue));
                break;
            case BlackTowerID.FIRE:
                foreach (var enemy in m_activeZone.EnemiesInZone)
                    enemy.TakeDamage(currentStats.AttackPower);
                break;
            case BlackTowerID.WIND:
                foreach (var enemy in m_activeZone.EnemiesInZone)
                    enemy.ApplyDebuff(DebuffTarget.Push, currentStats.AbilityValue, 0.1f);
                break;
            case BlackTowerID.EARTH:
                foreach (var enemy in m_activeZone.EnemiesInZone)
                {
                    enemy.ApplyDebuff(DebuffTarget.Stun, 0f, 1.5f); // 임시 속박(Stun) 처리
                    enemy.TakeDamage(enemy.MaxHP * (currentStats.AbilityValue / 100f));
                }
                break;
            case BlackTowerID.LIGHT:
                for (int i = m_activeZone.EnemiesInZone.Count - 1; i >= 0; i--)
                {
                    var enemy = m_activeZone.EnemiesInZone[i];
                    if (enemy.currentHP / enemy.MaxHP <= (currentStats.AbilityValue / 100f))
                    {
                        enemy.ExecuteDeath();
                    }
                }
                break;
            case BlackTowerID.DARKNESS:
                foreach (var enemy in m_activeZone.EnemiesInZone)
                {
                    enemy.PullToPosition(m_activeZone.transform.position, currentStats.AbilityValue);
                    enemy.TakeDamage(enemy.MaxHP * 0.005f); // 0.5% 데미지
                }
                break;
        }
        return true; // 액션 실행 완료 (쿨타임 리셋)
    }

    private IEnumerator ShieldBlockRoutine(float duration)
    {
        m_activeZone.ToggleObstacle(true);
        yield return new WaitForSeconds(duration);
        m_activeZone.ToggleObstacle(false);
    }
}