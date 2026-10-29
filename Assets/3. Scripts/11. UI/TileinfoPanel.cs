using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SquareTD
{
    // 타일 정보 카드
    public class TileinfoPanel : MonoBehaviour
    {
        [Header("머리 (앞·뒤 공통)")]
        [SerializeField] TextMeshProUGUI m_title;

        [Header("앞면: 버프·디버프 목록")]
        [SerializeField] GameObject m_front;
        [SerializeField] ScrollRect m_list;
        [SerializeField] RectTransform m_listContent;
        [Tooltip("목록 한 줄의 틀 (꺼 둔 채로 둠. 이것을 복사해서 씀)")]
        [SerializeField] Button m_entryTemplate;
        [SerializeField] float m_entryHeight = 58f;
        [SerializeField] float m_entrySpacing = 6f;
        [SerializeField] Color m_buffColor = new Color(1f, 0.92f, 0.65f);
        [SerializeField] Color m_debuffColor = new Color(0.85f, 0.72f, 1f);

        [Header("뒷면: 고른 효과의 자세한 설명")]
        [SerializeField] GameObject m_back;
        [SerializeField] TextMeshProUGUI m_detailTitle;
        [SerializeField] TextMeshProUGUI m_detailText;
        [Tooltip("누르면 앞면으로 (뒷면 바탕)")]
        [SerializeField] Graphic m_backArea;

        private struct Entry
        {
            public string Title;
            public string Summary;
            public string Detail;
            public bool IsDebuff;
        }

        private readonly List<GameObject> m_spawned = new List<GameObject>();
        private bool m_subscribed;

        public GameObject BuffEntryPrefab => m_entryTemplate != null ? m_entryTemplate.gameObject : null;

        private void Start()
        {
            if (m_backArea != null)
            {
                m_backArea.raycastTarget = true;
                Button button = m_backArea.GetComponent<Button>();
                if (button == null) button = m_backArea.gameObject.AddComponent<Button>();
                button.targetGraphic = m_backArea;
                button.onClick.AddListener(FlipToFront);
            }
        }

        private void OnEnable()
        {
            ShowFront();
            if (UIManager.Instance == null) return;

            if (!m_subscribed)
            {
                UIManager.Instance.SelectedTileChanged += Refresh;
                if (BoardManager.Instance != null) BoardManager.Instance.Zones.ZoneChanged += HandleZoneChanged;
                if (TurnManager.Instance != null) TurnManager.Instance.EnemyTurnEnded += HandleEnemyTurnEnded;
                m_subscribed = true;
            }
            if (UIManager.Instance.HasSelectedTile) Refresh();
        }

        private void OnDisable()
        {
            if (!m_subscribed) return;
            if (UIManager.Instance != null) UIManager.Instance.SelectedTileChanged -= Refresh;
            if (BoardManager.Instance != null) BoardManager.Instance.Zones.ZoneChanged -= HandleZoneChanged;
            if (TurnManager.Instance != null) TurnManager.Instance.EnemyTurnEnded -= HandleEnemyTurnEnded;
            m_subscribed = false;
        }

        private void HandleZoneChanged(Zone zone) => Refresh();
        private void HandleEnemyTurnEnded(int turn) => Refresh();

        private void Refresh()
        {
            if (UIManager.Instance == null || BoardManager.Instance == null) return;
            int pathIndex = UIManager.Instance.SelectedPathIndex;
            int spawnIndex = UIManager.Instance.SelectedSpawnIndex;

            var entries = new List<Entry>();
            if (spawnIndex >= 0) CollectSpawn(spawnIndex, entries);
            else if (pathIndex >= 0) CollectPath(pathIndex, entries);

            if (m_title != null) m_title.text = spawnIndex >= 0 ? $"스폰 {spawnIndex}번 칸" : pathIndex >= 0 ? $"길 {pathIndex}번 칸" : "-";
            BuildList(entries);
        }

        private static void CollectSpawn(int spawnIndex, List<Entry> entries)
        {
            BoardManager board = BoardManager.Instance;
            AddTile(SpecialTileText.Describe(board.Tiles.GetSpawnTile(spawnIndex)), entries);
            foreach (Zone zone in board.Zones.GetAtSpawn(spawnIndex)) entries.Add(ZoneEntry(zone));
        }

        private static void CollectPath(int pathIndex, List<Entry> entries)
        {
            BoardManager board = BoardManager.Instance;
            AddTile(SpecialTileText.Describe(board.Tiles.GetPathTile(pathIndex), pathIndex, board.Grid), entries);
            foreach (Zone zone in board.Zones.GetAtPath(pathIndex)) entries.Add(ZoneEntry(zone));
            foreach (GroundEffect ground in board.Grounds.GetAt(pathIndex)) entries.Add(GroundEntry(ground));
        }

        private static Entry GroundEntry(GroundEffect ground)
        {
            string owner = ground.Owner != null ? ground.Owner.name : "-";
            string description = ground.Describe();
            return new Entry
            {
                Title = description.Split(':')[0],
                Summary = $"{ground.RemainingTurns}턴 남음",
                Detail = $"{description}\n쏜 타워: {owner}",
                IsDebuff = true,
            };
        }

        private static void AddTile(string description, List<Entry> entries)
        {
            if (string.IsNullOrEmpty(description)) return;
            string[] lines = description.Split(new[] { '\n' }, 2);
            entries.Add(new Entry
            {
                Title = lines[0],
                Summary = lines.Length > 1 ? lines[1] : "",
                Detail = description,
                IsDebuff = false,
            });
        }

        private static Entry ZoneEntry(Zone zone)
        {
            string emblem = EmblemNames.GetEmblemName(zone.Emblem);
            string kind = zone.IsBuff ? "버프존" : "디버프존";
            string stageName = SkillDescriptions.GetZoneStageName(zone);
            int maxStack = zone.Owner != null ? zone.Owner.Stats.Stack : 0;
            string progress = zone.Stage == ZoneStage.Skill2 ? "스킬 2 (다 참)" : $"스킬 1 · 스택 {zone.Stack:0.#} / {maxStack}";
            string owner = zone.Owner != null ? zone.Owner.name : "-";
            string rule = zone.Owner != null ? SkillDescriptions.GetDescription(zone.Owner.Definition, zone.Owner.Stats) : "";

            return new Entry
            {
                Title = $"{emblem} {kind} · {stageName}",
                Summary = progress,
                Detail = $"{emblem} {kind} · {stageName}\n{progress}\n존 주인: {owner}\n\n{rule}",
                IsDebuff = !zone.IsBuff,
            };
        }

        private void BuildList(List<Entry> entries)
        {
            foreach (GameObject spawned in m_spawned)
            {
                if (spawned != null) Destroy(spawned);
            }
            m_spawned.Clear();
            if (m_entryTemplate == null || m_listContent == null) return;

            if (entries.Count == 0)
            {
                entries.Add(new Entry { Title = "효과 없음", Summary = "이 칸에는 특수 타일과 존이 없습니다.", Detail = "이 칸에는 특수 타일과 존이 없습니다." });
            }

            for (int i = 0; i < entries.Count; i++)
            {
                Entry entry = entries[i];
                Button button = Instantiate(m_entryTemplate, m_listContent);
                button.name = $"Entry_{i + 1}";
                button.gameObject.SetActive(true);

                var rect = (RectTransform)button.transform;
                rect.anchoredPosition = new Vector2(0f, -m_entrySpacing - i * (m_entryHeight + m_entrySpacing));
                rect.sizeDelta = new Vector2(rect.sizeDelta.x, m_entryHeight);

                if (button.targetGraphic != null) button.targetGraphic.color = entry.IsDebuff ? m_debuffColor : m_buffColor;
                TextMeshProUGUI text = button.GetComponentInChildren<TextMeshProUGUI>(true);
                if (text != null) text.text = string.IsNullOrEmpty(entry.Summary) ? $"<b>{entry.Title}</b>" : $"<b>{entry.Title}</b>\n<size=80%>{entry.Summary}</size>";

                button.onClick.AddListener(() => ShowDetail(entry));
                m_spawned.Add(button.gameObject);
            }

            m_listContent.sizeDelta = new Vector2(m_listContent.sizeDelta.x, entries.Count * (m_entryHeight + m_entrySpacing) + m_entrySpacing);
            if (m_list != null) m_list.verticalNormalizedPosition = 1f;
        }

        private void ShowDetail(Entry entry)
        {
            if (m_detailTitle != null) m_detailTitle.text = entry.Title;
            if (m_detailText != null) m_detailText.text = entry.Detail;
            if (m_front == null || m_back == null) return;
            CardFlip.Flip(transform, () =>
            {
                m_front.SetActive(false);
                m_back.SetActive(true);
            });
        }

        private void FlipToFront()
        {
            if (m_front == null || m_back == null || m_front.activeSelf) return;
            CardFlip.Flip(transform, () =>
            {
                m_back.SetActive(false);
                m_front.SetActive(true);
            });
        }

        private void ShowFront()
        {
            DG.Tweening.DOTween.Kill(transform, true);
            transform.localRotation = Quaternion.identity;
            if (m_front != null) m_front.SetActive(true);
            if (m_back != null) m_back.SetActive(false);
        }
    }
}
