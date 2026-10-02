using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 선택한 타일의 고정 맵 효과와 버프/디버프를 항목 프리팹으로 표시합니다.
/// 패널을 켜고 끄는 것은 UIManager가 하고, 표시 내용은 UIManager의 타일 선택 이벤트를 구독해 갱신합니다.
/// </summary>
public class TileinfoPanel : MonoBehaviour
{
    [Header("버프 목록")]
    [SerializeField] ScrollRect m_buffScrollRect;
    [SerializeField] RectTransform m_buffContent;
    [SerializeField] GameObject m_buffEntryPrefab;
    [SerializeField] float m_buffStartY = 150f;

    [Header("디버프 목록")]
    [SerializeField] ScrollRect m_debuffScrollRect;
    [SerializeField] RectTransform m_debuffContent;
    [SerializeField] GameObject m_debuffEntryPrefab;
    [SerializeField] float m_debuffStartY = 105f;

    [Header("항목 배치")]
    [SerializeField] float m_entrySpacing = 75f;

    private readonly List<GameObject> m_spawnedBuffEntries = new List<GameObject>();
    private readonly List<GameObject> m_spawnedDebuffEntries = new List<GameObject>();

    public GameObject BuffEntryPrefab => m_buffEntryPrefab;

    private void OnEnable()
    {
        // 패널이 켜진 채로 씬이 시작되면 UIManager.Awake보다 먼저 불릴 수 있습니다.
        // 이 경우 UIManager가 시작할 때 패널을 껐다가 다시 켜므로 그때 구독합니다.
        if (UIManager.Instance == null) return;

        UIManager.Instance.SelectedTileChanged += UpdateTileInfo;

        // 패널이 꺼져 있는 동안 선택된 타일은 UIManager에서 가져옵니다.
        if (UIManager.Instance.HasSelectedTile)
        {
            UpdateTileInfo(UIManager.Instance.SelectedTileCell, UIManager.Instance.SelectedTileIsTowerSpawn);
        }
    }

    private void OnDisable()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.SelectedTileChanged -= UpdateTileInfo;
        }
    }

    private void UpdateTileInfo(Vector3Int cell, bool isTowerSpawnTile)
    {
        ClearEntries(m_spawnedBuffEntries);
        ClearEntries(m_spawnedDebuffEntries);

        List<string> buffDescriptions = new List<string>();
        List<string> debuffDescriptions = new List<string>();

        if (TileManager.Instance != null)
        {
            if (isTowerSpawnTile)
            {
                AddTowerTileDescriptions(cell, buffDescriptions);
            }
            else
            {
                AddPathTileDescriptions(cell, buffDescriptions, debuffDescriptions);
            }
        }

        // ScrollRect가 연결되어 있다면 외부 Rect가 아니라 실제 Content에 생성해야
        // Viewport 마스크와 스크롤바가 정상적으로 목록을 제어합니다.
        RectTransform buffContent = m_buffScrollRect != null && m_buffScrollRect.content != null
            ? m_buffScrollRect.content
            : m_buffContent;
        RectTransform debuffContent = m_debuffScrollRect != null && m_debuffScrollRect.content != null
            ? m_debuffScrollRect.content
            : m_debuffContent;

        CreateEntries(m_buffEntryPrefab, buffContent, buffDescriptions, m_buffStartY, m_spawnedBuffEntries);
        CreateEntries(m_debuffEntryPrefab, debuffContent, debuffDescriptions, m_debuffStartY, m_spawnedDebuffEntries);

        if (m_buffScrollRect != null) m_buffScrollRect.verticalNormalizedPosition = 1f;
        if (m_debuffScrollRect != null) m_debuffScrollRect.verticalNormalizedPosition = 1f;
    }

    private void AddTowerTileDescriptions(Vector3Int cell, List<string> buffDescriptions)
    {
        AddTowerMapTileDescription(TileManager.Instance.GetTowerTileBuffAt(cell), buffDescriptions);

        IReadOnlyList<TowerTileBuffEffect> effects = TileManager.Instance.GetTowerBuffEffectsAt(cell);
        for (int i = 0; i < effects.Count; i++)
        {
            TowerTileBuffEffect effect = effects[i];
            buffDescriptions.Add($"{BuffTargetNames.GetBuffTargetName(effect.Target)} 버프\n티어 {effect.Tier} · 지속 {effect.RemainingTurns}턴");
        }
    }

    private void AddPathTileDescriptions(Vector3Int cell, List<string> buffDescriptions, List<string> debuffDescriptions)
    {
        AddPathMapTileDescription(TileManager.Instance.GetTileTypeAt(cell), buffDescriptions);

        IReadOnlyList<PathTileDebuffEffect> effects = TileManager.Instance.GetPathDebuffEffectsAt(cell);
        for (int i = 0; i < effects.Count; i++)
        {
            PathTileDebuffEffect effect = effects[i];
            string state = effect.ApplicationVersion <= 0
                ? "배치 예정"
                : $"지속 {effect.Duration}턴";
            debuffDescriptions.Add($"{BuffTargetNames.GetDebuffTargetName(effect.Target)} 디버프\n티어 {effect.Tier} · {state}");
        }
    }

    private void AddTowerMapTileDescription(TowerTileBuffType type, List<string> descriptions)
    {
        switch (type)
        {
            case TowerTileBuffType.AttackPowerUp:
                descriptions.Add("공격력 타일\n공격력 50% 증가");
                break;
            case TowerTileBuffType.ActionCountUp:
                descriptions.Add("행동력 타일\n행동력 1 감소");
                break;
            case TowerTileBuffType.AttackCountUp:
                descriptions.Add("공격 횟수 타일\n공격 횟수 1 증가");
                break;
        }
    }

    private void AddPathMapTileDescription(PathTileBuffType type, List<string> descriptions)
    {
        switch (type)
        {
            case PathTileBuffType.DefendTile:
                descriptions.Add("방어 타일\n방어력 20% 증가");
                break;
            case PathTileBuffType.SpeedTile:
                descriptions.Add("속도 타일\n행동 주기 1 감소 · 이동 주사위 최대값 +2");
                break;
            case PathTileBuffType.HealTile:
                descriptions.Add("회복 타일\n매 턴 최대 체력의 5% 회복");
                break;
        }
    }

    private void CreateEntries(
        GameObject entryPrefab,
        RectTransform content,
        List<string> descriptions,
        float startY,
        List<GameObject> spawnedEntries)
    {
        if (entryPrefab == null || content == null) return;

        for (int index = 0; index < descriptions.Count; index++)
        {
            GameObject entry = Instantiate(entryPrefab, content);
            entry.name = $"{entryPrefab.name}_{index + 1}";
            entry.SetActive(true);

            RectTransform entryRect = entry.GetComponent<RectTransform>();
            if (entryRect != null)
            {
                entryRect.anchoredPosition = new Vector2(entryRect.anchoredPosition.x, startY - m_entrySpacing * index);
            }

            TextMeshProUGUI text = entry.GetComponentInChildren<TextMeshProUGUI>(true);
            if (text != null)
            {
                text.text = descriptions[index];
            }
            else
            {
                Debug.LogWarning($"TileinfoPanel : {entryPrefab.name} 프리팹에서 TextMeshProUGUI를 찾지 못했습니다.");
            }

            spawnedEntries.Add(entry);
        }

        // Content 높이를 늘려 ScrollRect가 항목 전체를 스크롤할 수 있게 합니다.
        if (descriptions.Count > 0)
        {
            float requiredHeight = Mathf.Abs(startY - m_entrySpacing * (descriptions.Count - 1)) + m_entrySpacing;
            content.sizeDelta = new Vector2(content.sizeDelta.x, Mathf.Max(content.sizeDelta.y, requiredHeight));
        }
    }

    private void ClearEntries(List<GameObject> entries)
    {
        foreach (GameObject entry in entries)
        {
            if (entry != null) Destroy(entry);
        }
        entries.Clear();
    }
}
