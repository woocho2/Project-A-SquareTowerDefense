using System.Collections.Generic;
using UnityEngine;

public class DebuffAction : TowerAttackAction
{
    private DebuffZone m_activeZone;
    private TowerController m_ownerTower;
    private int m_sourceID;

    public DebuffAction(TowerData data) : base(data) { }

    public void BindZone(DebuffZone zone, Transform towerTransform)
    {
        if (zone == null || towerTransform == null) return;

        if (m_activeZone != null) m_activeZone.OnPlacedPathIndexChanged -= HandleZoneIndexChanged;
        m_activeZone = zone;
        m_ownerTower = towerTransform.GetComponent<TowerController>();
        m_sourceID = towerTransform.GetInstanceID();
        m_activeZone.OnPlacedPathIndexChanged += HandleZoneIndexChanged;
        // 장판은 타워와 소유 관계만 유지하고, 위치는 독립적으로 고정합니다.
        m_activeZone.transform.SetParent(null, true);

        // 장판의 시각 연출은 TileSatelliteOrbiter가 전담합니다.
        // DebuffZone은 오비터를 직접 보관하지 않고, 배치/드래그만 담당합니다.
        TileSatelliteOrbiter orbiter = m_activeZone.GetComponent<TileSatelliteOrbiter>();
        if (orbiter != null)
        {
            orbiter.SetEffectType(TileSatelliteEffectType.Debuff);
            orbiter.SetDebuffType(m_data.debuffTarget);
        }

        // 최초에는 가장 가까운 패스 타일에 놓고, 이후에는 모든 패스 타일로 옮길 수 있습니다.
        // 장판을 놓는 즉시 HandleZoneIndexChanged가 타일 정보 패널용 디버프 표시를 등록합니다.
        // 실제 적 스택은 ExecuteAction에서 ApplicationVersion이 1 이상이 된 뒤부터 적용됩니다.
        int towerSpawnIndex = m_ownerTower != null ? m_ownerTower.SpawnIndex : -1;
        if (TileManager.Instance == null ||
            !TileManager.Instance.TryGetClosestPathIndexToTower(towerSpawnIndex, out int pathIndex) ||
            !m_activeZone.SetPlacedPathIndex(pathIndex))
        {
            m_activeZone.gameObject.SetActive(false);
            Debug.LogWarning("[DebuffZone] 배치할 패스 타일을 찾지 못했습니다. 디버프 존을 비활성화합니다.");
        }
    }

    private void HandleZoneIndexChanged(int pathIndex)
    {
        if (TileManager.Instance == null || m_ownerTower == null) return;

        TowerStats stats = m_ownerTower.GetFinalStats();
        // 플레이어 턴에는 위치와 UI 표시만 갱신합니다. 새 스택은 Action 완료 시 기록합니다.
        RegisterZonePreview(pathIndex, stats);
    }

    private void RegisterZonePreview(int centerPathIndex, TowerStats stats)
    {
        if (TileManager.Instance == null || m_activeZone == null) return;
        TileManager.Instance.RegisterPathDebuffZone(
            GetAffectedPathIndices(centerPathIndex, stats), m_sourceID, m_data.debuffTarget,
            Mathf.Clamp(m_data.Tier, 1, 5), GetEffectAbilityValue(stats), stats.Duration,
            centerPathIndex, m_ownerTower != null ? m_ownerTower.CreationOrder : int.MaxValue,
            GetEffectPower(stats), GetEffectStackGain(stats), GetEffectStackThreshold(stats));
    }

    // 미리보기와 실제 적용이 같은 인덱스 계산을 사용합니다.
    // 장판 크기는 SplashRadius를 따릅니다: 1 → 1x1, 2 → 3x3, 3 → 5x5. (0도 1x1)
    private List<int> GetAffectedPathIndices(int centerPathIndex, TowerStats stats)
    {
        int tileRadius = Mathf.Max(0, stats.ProjectileRadius - 1);
        return TileManager.Instance.GetPathIndicesInSquare(centerPathIndex, tileRadius);
    }

    private float GetEffectAbilityValue(TowerStats stats) =>
        m_data.debuffTarget == DebuffTarget.Fire ? stats.AbilityValue : stats.AttackPower;

    // 새 틀(스킬 1 + 스킬 2)로 옮긴 문양만 아래 값을 넘깁니다. 나머지 문양은 0을 넘겨 예전 방식을 유지합니다.
    // Power = 스킬 1의 세기, AttackCount = 한 번 행동에 쌓는 스택 수, 문턱 = 60 ÷ 컬러 강화 레벨.
    private bool UsesStackSkillRule => EnemyDebuffRule.Get(m_data.debuffTarget) != null;
    private float GetEffectPower(TowerStats stats) => UsesStackSkillRule ? stats.AttackPower : 0f;
    private int GetEffectStackGain(TowerStats stats) => UsesStackSkillRule ? Mathf.Max(1, stats.AttackCount) : 0;
    private int GetEffectStackThreshold(TowerStats stats) => UsesStackSkillRule ? GetStackSkillThreshold(stats.Level) : 0;

    public override bool ExecuteAction(Transform towerTransform, TowerStats currentStats)
    {
        if (m_activeZone == null) return false;
        if (TileManager.Instance == null || !m_activeZone.TryGetPlacedPathIndex(out int pathIndex)) return false;

        List<int> affectedIndices = GetAffectedPathIndices(pathIndex, currentStats);
        if (affectedIndices.Count == 0) return false;

        // Action이 끝날 때만 새 적용 버전을 기록합니다. 적은 이동 후 이 기록을 읽습니다.
        TileManager.Instance.SetPathDebuffEffects(
            affectedIndices, m_sourceID, m_data.debuffTarget,
            Mathf.Clamp(m_data.Tier, 1, 5), GetEffectAbilityValue(currentStats),
            currentStats.Duration, pathIndex,
            m_ownerTower != null ? m_ownerTower.CreationOrder : int.MaxValue,
            GetEffectPower(currentStats), GetEffectStackGain(currentStats), GetEffectStackThreshold(currentStats));
        return true;
    }
}
