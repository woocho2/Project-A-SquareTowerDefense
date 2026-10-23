using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SquareTD
{
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
            if (UIManager.Instance == null) return;

            UIManager.Instance.SelectedTileChanged += UpdateTileInfo;
            // 수업 26: ZoneRegistry.ZoneChanged += 지금 보는 칸의 존이 바뀌면 UpdateTileInfo
            if (UIManager.Instance.HasSelectedTile) UpdateTileInfo();
        }

        private void OnDisable()
        {
            if (UIManager.Instance != null) UIManager.Instance.SelectedTileChanged -= UpdateTileInfo;
        }

        private void UpdateTileInfo()
        {
            ClearEntries(m_spawnedBuffEntries);
            ClearEntries(m_spawnedDebuffEntries);

            var buffDescriptions = new List<string>();
            var debuffDescriptions = new List<string>();

            int pathIndex = UIManager.Instance.SelectedPathIndex;
            int spawnIndex = UIManager.Instance.SelectedSpawnIndex;

            if (spawnIndex >= 0) AddTowerTileDescriptions(spawnIndex, buffDescriptions);
            else if (pathIndex >= 0) AddPathTileDescriptions(pathIndex, buffDescriptions, debuffDescriptions);

            if (buffDescriptions.Count == 0) buffDescriptions.Add("특수 효과 없음\n이 칸에는 맵 효과와 버프존이 없습니다.");
            if (debuffDescriptions.Count == 0 && spawnIndex < 0) debuffDescriptions.Add("디버프존 없음\n이 칸에는 디버프존이 없습니다.");

            RectTransform buffContent = m_buffScrollRect != null && m_buffScrollRect.content != null ? m_buffScrollRect.content : m_buffContent;
            RectTransform debuffContent = m_debuffScrollRect != null && m_debuffScrollRect.content != null ? m_debuffScrollRect.content : m_debuffContent;

            CreateEntries(m_buffEntryPrefab, buffContent, buffDescriptions, m_buffStartY, m_spawnedBuffEntries);
            CreateEntries(m_debuffEntryPrefab, debuffContent, debuffDescriptions, m_debuffStartY, m_spawnedDebuffEntries);

            if (m_buffScrollRect != null) m_buffScrollRect.verticalNormalizedPosition = 1f;
            if (m_debuffScrollRect != null) m_debuffScrollRect.verticalNormalizedPosition = 1f;
        }

        private void AddTowerTileDescriptions(int spawnIndex, List<string> buffDescriptions)
        {
            // 수업 22: 스폰 칸의 특수 타일 (공격력, 행동 카운트, 공격 횟수, 마스터리)
            //   SpawnTileEffect effect = BoardManager.Instance.SpecialTiles.GetSpawnEffect(spawnIndex);
            //   if (effect != null) buffDescriptions.Add(effect.Describe());
            // 수업 26: 이 칸의 버프존들 (문양, 단계, 스택)
            //   foreach (Zone zone in BoardManager.Instance.Zones.GetAtSpawn(spawnIndex)) buffDescriptions.Add(StatusTextFormatter.Describe(zone));
        }

        private void AddPathTileDescriptions(int pathIndex, List<string> buffDescriptions, List<string> debuffDescriptions)
        {
            // 수업 22: 길 칸의 특수 타일 (방어, 이동력, 행동 카운트, 회복, 정화, 비프로스트)
            //   PathTileEffect effect = BoardManager.Instance.SpecialTiles.GetPathEffect(pathIndex);
            //   if (effect != null) buffDescriptions.Add(effect.Describe());
            // 수업 26: 이 칸의 디버프존들
            //   foreach (Zone zone in BoardManager.Instance.Zones.GetAtPath(pathIndex)) debuffDescriptions.Add(StatusTextFormatter.Describe(zone));
            // 수업 29 · 40: 이 칸에 깔린 장판 (화살비, 블랙홀)
        }

        private void CreateEntries(GameObject entryPrefab, RectTransform content, List<string> descriptions, float startY, List<GameObject> spawnedEntries)
        {
            if (entryPrefab == null || content == null) return;

            for (int index = 0; index < descriptions.Count; index++)
            {
                GameObject entry = Instantiate(entryPrefab, content);
                entry.name = $"{entryPrefab.name}_{index + 1}";
                entry.SetActive(true);

                RectTransform entryRect = entry.GetComponent<RectTransform>();
                if (entryRect != null)
                    entryRect.anchoredPosition = new Vector2(entryRect.anchoredPosition.x, startY - m_entrySpacing * index);

                TextMeshProUGUI text = entry.GetComponentInChildren<TextMeshProUGUI>(true);
                if (text != null) text.text = descriptions[index];
                else Debug.LogWarning($"[TileinfoPanel] {entryPrefab.name} 프리팹에서 TextMeshProUGUI를 찾지 못함");

                spawnedEntries.Add(entry);
            }

            if (descriptions.Count > 0)
            {
                float requiredHeight = Mathf.Abs(startY - m_entrySpacing * (descriptions.Count - 1)) + m_entrySpacing;
                content.sizeDelta = new Vector2(content.sizeDelta.x, Mathf.Max(content.sizeDelta.y, requiredHeight));
            }
        }

        private static void ClearEntries(List<GameObject> entries)
        {
            foreach (GameObject entry in entries)
            {
                if (entry != null) Destroy(entry);
            }
            entries.Clear();
        }
    }
}
