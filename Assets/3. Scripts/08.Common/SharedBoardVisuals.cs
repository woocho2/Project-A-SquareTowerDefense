using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[Serializable]
public struct BoardLayerColors
{
    [InspectorName("배경색")] public Color background;
    [InspectorName("장식색")] public Color decoration;
}

[Serializable]
public sealed class StageBoardPalette
{
    public StageRealm realm;
    public string displayName;
    [InspectorName("타워 스폰 타일")] public BoardLayerColors towerSpawn;
    [InspectorName("패스 타일")] public BoardLayerColors path;
    [InspectorName("화살표 색")] public Color arrow = Color.white;
}

[Serializable]
public sealed class TowerTierPalette
{
    [Range(1, 6)] public int tier = 1;
    public string displayName;
    [InspectorName("색상")] public BoardLayerColors colors;
}

[Serializable]
public sealed class TowerPaletteOverride
{
    public int towerID;
    public BoardLayerColors colors;
}

[Serializable]
public sealed class EnemyFacePalette
{
    public EnemyCubeFaceAsset state;
    [InspectorName("배경색")] public Color background = Color.white;
    [InspectorName("장식색")] public Color decoration = Color.white;
    [InspectorName("장비색")] public Color equipment = Color.white;
}

[Serializable]
public sealed class EnemyAppearancePalette
{
    public EnemyCubeAppearance appearance;
    public List<EnemyFacePalette> states = new List<EnemyFacePalette>();
}

/// <summary>타일·타워·큐브 적의 공용 이미지와 색상 원본입니다.</summary>
[CreateAssetMenu(menuName = "SquareTowerDefense/Shared Board Visuals", fileName = "SharedBoardVisuals")]
public sealed class SharedBoardVisuals : ScriptableObject
{
    [Header("공용 이미지 — 모두 같은 두 텍스처를 참조합니다")]
    [SerializeField] private Sprite m_background;
    [SerializeField] private Sprite m_decoration;
    [SerializeField] private Sprite m_arrow;
    [Header("타일맵 브러시")]
    [SerializeField] private Tile m_backgroundTile;
    [SerializeField] private Tile m_decorationTile;
    [SerializeField] private Tile m_arrowTile;
    [Header("스테이지별 타일 색상")]
    [SerializeField] private List<StageBoardPalette> m_stages = new List<StageBoardPalette>();
    [Header("타워 티어별 기본 색상")]
    [SerializeField] private List<TowerTierPalette> m_tiers = new List<TowerTierPalette>();
    [Header("개별 타워 색상 — 동일 ID가 있으면 티어 기본값보다 우선")]
    [SerializeField] private List<TowerPaletteOverride> m_towers = new List<TowerPaletteOverride>();
    [Header("몬스터 면 유형별 기본 색상")]
    [SerializeField] private List<EnemyFacePalette> m_enemyStates = new List<EnemyFacePalette>();
    [Header("몬스터 캐릭터별 색상 — 해당 캐릭터의 면 유형 설정을 우선 적용")]
    [SerializeField] private List<EnemyAppearancePalette> m_enemies = new List<EnemyAppearancePalette>();

    private static SharedBoardVisuals s_default;
    public static SharedBoardVisuals Default => s_default != null
        ? s_default : s_default = Resources.Load<SharedBoardVisuals>("SharedBoardVisuals");
    public static event Action Changed;
    public Sprite Background => m_background;
    public Sprite Decoration => m_decoration;
    public Sprite Arrow => m_arrow;
    public Tile BackgroundTile => m_backgroundTile;
    public Tile DecorationTile => m_decorationTile;
    public Tile ArrowTile => m_arrowTile;

    public StageBoardPalette GetStage(StageRealm realm)
    {
        return m_stages.Find(item => item != null && item.realm == realm);
    }

    public BoardLayerColors GetTower(int towerID)
    {
        TowerPaletteOverride custom = m_towers.Find(item => item != null && item.towerID == towerID);
        if (custom != null) return custom.colors;
        TowerTierPalette tier = m_tiers.Find(item => item != null && item.tier == towerID / 1000);
        return tier != null ? tier.colors : new BoardLayerColors { background = Color.white, decoration = Color.white };
    }

    public EnemyFacePalette GetEnemy(EnemyCubeAppearance appearance, EnemyCubeFaceAsset state)
    {
        EnemyAppearancePalette custom = m_enemies.Find(item => item != null && item.appearance == appearance);
        EnemyFacePalette colors = custom?.states.Find(item => item != null && item.state == state);
        return colors ?? m_enemyStates.Find(item => item != null && item.state == state);
    }

    public void NotifyChanged() => Changed?.Invoke();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { s_default = null; Changed = null; }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Tilemap changes are deferred out of the serialization/import callback.
        UnityEditor.EditorApplication.delayCall += () => { if (this != null) NotifyChanged(); };
    }
#endif
}
