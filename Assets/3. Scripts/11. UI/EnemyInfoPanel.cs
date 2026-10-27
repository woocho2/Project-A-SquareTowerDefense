using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SquareTD
{
    public class EnemyInfoPanel : MonoBehaviour
    {
        [Header("Views")]
        [SerializeField] private GameObject m_viewEnemyList;
        [SerializeField] private GameObject m_viewEnemyDetail;

        [Header("List View Components")]
        [SerializeField] private TextMeshProUGUI m_txtListTitle;
        [SerializeField] private RectTransform m_listContent;
        [SerializeField] private ScrollRect m_listScrollRect;
        [SerializeField] private TextMeshProUGUI m_txtNoEnemies;
        [SerializeField] private GameObject m_enemyListItemPrefab;

        [Header("Detail View Components")]
        [SerializeField] private Button m_btnBackToList;
        [SerializeField] private TextMeshProUGUI m_txtDetailTitle;
        [SerializeField] private TextMeshProUGUI m_txtHP;
        [SerializeField] private TextMeshProUGUI m_txtDefend;
        [SerializeField] private TextMeshProUGUI m_txtAction;
        [SerializeField] private TextMeshProUGUI m_txtSpeed;
        [SerializeField] private TextMeshProUGUI m_txtResistance;
        [SerializeField] private TextMeshProUGUI m_txtHitCount;
        [SerializeField] private Slider m_hpSlider;                     // 체력 막대 (없으면 글자만)
        [SerializeField] private TextMeshProUGUI m_txtEnemyType;        // 일반 / 중간 보스 / 최종 보스
        [SerializeField] private TextMeshProUGUI m_txtDamageTaken;      // 받는 피해 증감 (%)
        [SerializeField] private TextMeshProUGUI[] m_unusedValueTexts = new TextMeshProUGUI[0];   // 아직 쓰는 시스템이 없는 칸 → "-"
        [SerializeField] private bool m_compactValues;                  // 칸 이름 글자가 따로 있는 레이아웃이면 값만 씀 ("방어력:" 없이)

        [Header("Buff & Debuff Rects")]
        [SerializeField] private RectTransform m_buffContent;
        [SerializeField] private RectTransform m_debuffContent;
        [SerializeField] private ScrollRect m_buffScrollRect;
        [SerializeField] private ScrollRect m_debuffScrollRect;
        [SerializeField] private GameObject m_buffEntryPrefab;

        [Header("Detail Flip (앞: 스탯 / 뒤: 버프·디버프 목록)")]
        [SerializeField] private GameObject m_detailFront;
        [SerializeField] private GameObject m_detailBack;
        [SerializeField] private Button m_btnFlipDetail;                // 비어 있으면 상세 화면 가운데 줄에 작은 버튼을 만듦
        [SerializeField] private Vector2 m_flipButtonPosition = new Vector2(0f, 0.5f);
        [SerializeField] private Vector2 m_flipButtonSize = new Vector2(46f, 35f);
        [SerializeField, Min(0.05f)] private float m_flipDuration = 0.2f;

        private const float ListStartY = -45f;
        private const float ListSpacing = 82f;

        private readonly List<EnemyListItemUI> m_listItems = new List<EnemyListItemUI>();
        private readonly List<GameObject> m_spawnedBuffEntries = new List<GameObject>();
        private readonly List<GameObject> m_spawnedDebuffEntries = new List<GameObject>();

        private int m_currentPathIndex = -1;
        private ITarget m_selectedTarget;
        private TMP_FontAsset m_cachedFont;
        private bool m_subscribed;
        private bool m_showingBack;
        private TextMeshProUGUI m_txtFlipDetail;
        private Sequence m_flipTween;

        public int CurrentPathIndex => m_currentPathIndex;
        public ITarget SelectedTarget => m_selectedTarget;

        private void Awake()
        {
            InitializeComponents();
        }

        private void Start()
        {
            if (m_btnBackToList != null)
            {
                m_btnBackToList.onClick.RemoveAllListeners();
                m_btnBackToList.onClick.AddListener(BackToList);
            }
            if (m_btnFlipDetail != null) m_btnFlipDetail.onClick.AddListener(FlipDetail);
        }

        private void OnDestroy()
        {
            if (m_btnFlipDetail != null) m_btnFlipDetail.onClick.RemoveListener(FlipDetail);
            m_flipTween?.Kill();
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void Subscribe()
        {
            if (m_subscribed || BoardManager.Instance == null || EnemyManager.Instance == null || TurnManager.Instance == null) return;

            BoardManager.Instance.Occupancy.CellChanged += HandleCellChanged;
            EnemyManager.Instance.EnemyDied += HandleEnemyDied;
            TurnManager.Instance.EnemyTurnEnded += HandleEnemyTurnEnded;
            m_subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!m_subscribed) return;

            if (BoardManager.Instance != null) BoardManager.Instance.Occupancy.CellChanged -= HandleCellChanged;
            if (EnemyManager.Instance != null) EnemyManager.Instance.EnemyDied -= HandleEnemyDied;
            if (TurnManager.Instance != null) TurnManager.Instance.EnemyTurnEnded -= HandleEnemyTurnEnded;
            m_subscribed = false;
        }

        private void HandleCellChanged(int index)
        {
            if (index != m_currentPathIndex) return;

            if (IsDetailVisible)
            {
                if (m_selectedTarget == null || !m_selectedTarget.IsAlive || m_selectedTarget.PathIndex != m_currentPathIndex) BackToList();
                return;
            }

            RefreshListEntries();
        }

        private void HandleEnemyDied(KillInfo kill)
        {
            if (IsDetailVisible && (m_selectedTarget == null || !m_selectedTarget.IsAlive)) BackToList();
        }

        private void HandleEnemyTurnEnded(int turn)
        {
            if (IsDetailVisible) UpdateStatsText();
            else RefreshListDisplays();
        }

        private bool IsDetailVisible => m_viewEnemyDetail != null && m_viewEnemyDetail.activeSelf;

        public void InitializeComponents()
        {
            FindExistingScrollRects();

            TextMeshProUGUI anyText = GetComponentInChildren<TextMeshProUGUI>(true);
            if (anyText != null && anyText.font != null) m_cachedFont = anyText.font;

            if (m_buffEntryPrefab == null)
            {
                m_buffEntryPrefab = Resources.Load<GameObject>("BuffPrefab");
                if (m_buffEntryPrefab == null)
                {
                    TileinfoPanel tileInfoPanel = FindFirstObjectByType<TileinfoPanel>(FindObjectsInactive.Include);
                    if (tileInfoPanel != null && tileInfoPanel.BuffEntryPrefab != null) m_buffEntryPrefab = tileInfoPanel.BuffEntryPrefab;
                }
            }

            EnsureViewHierarchy();

            foreach (TextMeshProUGUI text in m_unusedValueTexts)
            {
                if (text != null) text.text = "-";
            }

            EnsureFlipButton();
            SetDetailFace(false);
        }

        // ── 상세 화면 뒤집기 (스탯 ↔ 버프·디버프 목록) ──

        private void EnsureFlipButton()
        {
            if (m_detailFront == null || m_detailBack == null || m_viewEnemyDetail == null) return;

            if (m_btnFlipDetail == null)
            {
                var obj = new GameObject("Btn_FlipDetail", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
                obj.transform.SetParent(m_viewEnemyDetail.transform, false);
                RectTransform rect = obj.GetComponent<RectTransform>();
                rect.anchoredPosition = m_flipButtonPosition;
                rect.sizeDelta = m_flipButtonSize;

                Image image = obj.GetComponent<Image>();
                Image sample = FindSampleImage();
                if (sample != null)
                {
                    image.sprite = sample.sprite;
                    image.type = sample.type;
                    image.color = sample.color;
                }
                else image.color = new Color(0.25f, 0.35f, 0.45f, 0.95f);

                m_btnFlipDetail = obj.GetComponent<Button>();
                m_txtFlipDetail = CreateText(obj.transform, "Txt_FlipDetail", "", Vector2.zero, m_flipButtonSize,
                    14, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
            }
            else if (m_txtFlipDetail == null)
            {
                m_txtFlipDetail = m_btnFlipDetail.GetComponentInChildren<TextMeshProUGUI>(true);
            }
        }

        private Image FindSampleImage()
        {
            foreach (Image image in m_viewEnemyDetail.GetComponentsInChildren<Image>(true))
            {
                if (image.name.EndsWith("_BG") && image.sprite != null) return image;
            }
            return null;
        }

        public void FlipDetail()
        {
            if (m_detailFront == null || m_detailBack == null) return;

            m_flipTween?.Kill(true);
            GameObject from = m_showingBack ? m_detailBack : m_detailFront;
            GameObject to = m_showingBack ? m_detailFront : m_detailBack;
            m_showingBack = !m_showingBack;
            UpdateFlipLabel();

            float half = m_flipDuration * 0.5f;
            m_flipTween = DOTween.Sequence()
                .Append(from.transform.DOScaleX(0f, half).SetEase(Ease.InSine))
                .AppendCallback(() =>
                {
                    from.SetActive(false);
                    from.transform.localScale = Vector3.one;
                    to.transform.localScale = new Vector3(0f, 1f, 1f);
                    to.SetActive(true);
                })
                .Append(to.transform.DOScaleX(1f, half).SetEase(Ease.OutSine))
                .SetUpdate(true)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable);
        }

        private void SetDetailFace(bool showBack)
        {
            m_flipTween?.Kill();
            m_showingBack = showBack;
            if (m_detailFront != null)
            {
                m_detailFront.transform.localScale = Vector3.one;
                m_detailFront.SetActive(!showBack);
            }
            if (m_detailBack != null)
            {
                m_detailBack.transform.localScale = Vector3.one;
                m_detailBack.SetActive(showBack);
            }
            UpdateFlipLabel();
        }

        private void UpdateFlipLabel()
        {
            if (m_txtFlipDetail != null) m_txtFlipDetail.text = m_showingBack ? "스탯" : "버프";
        }

        private void FindExistingScrollRects()
        {
            if (m_buffScrollRect == null)
            {
                Transform buffRect = transform.Find("Group_BuffRect/Img_A_BuffScrollRect_BG 128*128")
                    ?? transform.Find("Group_BuffRect/BuffScrollRect")
                    ?? transform.Find("Group_BuffRect");
                if (buffRect != null) m_buffScrollRect = buffRect.GetComponentInChildren<ScrollRect>(true);
            }
            if (m_buffContent == null && m_buffScrollRect != null) m_buffContent = m_buffScrollRect.content;

            if (m_debuffScrollRect == null)
            {
                Transform debuffRect = transform.Find("Group_DebuffRect/Img_A_DebuffScrollRect_BG 128*128")
                    ?? transform.Find("Group_DebuffRect/DebuffScrollRect")
                    ?? transform.Find("Group_DebuffRect");
                if (debuffRect != null) m_debuffScrollRect = debuffRect.GetComponentInChildren<ScrollRect>(true);
            }
            if (m_debuffContent == null && m_debuffScrollRect != null) m_debuffContent = m_debuffScrollRect.content;
        }

        private void EnsureViewHierarchy()
        {
            // 인스펙터에 연결한 화면이 있으면 그것을 씀. 없을 때만 찾거나 만듦
            if (m_viewEnemyList == null)
            {
                Transform listTr = transform.Find("View_EnemyList");
                m_viewEnemyList = listTr != null ? listTr.gameObject : CreateEnemyListViewObject();
            }

            if (m_viewEnemyDetail == null)
            {
                Transform detailTr = transform.Find("View_EnemyDetail");
                m_viewEnemyDetail = detailTr != null ? detailTr.gameObject : CreateEnemyDetailViewObject();
            }

            if (m_btnBackToList == null)
            {
                foreach (Button btn in m_viewEnemyDetail.GetComponentsInChildren<Button>(true))
                {
                    if (btn.name.Contains("Back") || btn.name.Contains("List"))
                    {
                        m_btnBackToList = btn;
                        break;
                    }
                }
            }

            foreach (TextMeshProUGUI txt in GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                switch (txt.name)
                {
                    case "Txt_EnemyListTitle": m_txtListTitle ??= txt; break;
                    case "Txt_NoEnemies": m_txtNoEnemies ??= txt; break;
                    case "Txt_EnemyDetailName": m_txtDetailTitle ??= txt; break;
                    case "Txt_EnemyHP": m_txtHP ??= txt; break;
                    case "Txt_EnemyDefend": m_txtDefend ??= txt; break;
                    case "Txt_EnemyAction": m_txtAction ??= txt; break;
                    case "Txt_EnemySpeed": m_txtSpeed ??= txt; break;
                    case "Txt_EnemyResistance": m_txtResistance ??= txt; break;
                    case "Txt_EnemyHitCount": m_txtHitCount ??= txt; break;
                }
            }
        }

        private GameObject CreateEnemyListViewObject()
        {
            var listView = new GameObject("View_EnemyList", typeof(RectTransform));
            listView.transform.SetParent(transform, false);

            RectTransform listRect = listView.GetComponent<RectTransform>();
            listRect.anchorMin = new Vector2(0.5f, 0.5f);
            listRect.anchorMax = new Vector2(0.5f, 0.5f);
            listRect.anchoredPosition = new Vector2(0f, -20f);
            listRect.sizeDelta = new Vector2(420f, 900f);

            m_txtListTitle = CreateText(listView.transform, "Txt_EnemyListTitle", "타일 위 적 목록",
                new Vector2(0f, 380f), new Vector2(350f, 40f), 20, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);

            Transform existingBuffRect = transform.Find("Group_BuffRect");
            if (existingBuffRect != null)
            {
                GameObject scrollObj = Instantiate(existingBuffRect.gameObject, listView.transform, false);
                scrollObj.name = "EnemyListScrollRect";
                RectTransform scrollRect = scrollObj.GetComponent<RectTransform>();
                scrollRect.anchoredPosition = new Vector2(0f, -20f);
                scrollRect.sizeDelta = new Vector2(360f, 720f);

                ScrollRect sr = scrollObj.GetComponentInChildren<ScrollRect>(true);
                if (sr != null)
                {
                    m_listScrollRect = sr;
                    m_listContent = sr.content;
                    for (int i = m_listContent.childCount - 1; i >= 0; i--) Destroy(m_listContent.GetChild(i).gameObject);
                }
            }

            m_txtNoEnemies = CreateText(listView.transform, "Txt_NoEnemies", "현재 이 타일 위에 적이 없습니다.",
                Vector2.zero, new Vector2(320f, 60f), 16, FontStyles.Normal, TextAlignmentOptions.Center, new Color(0.8f, 0.8f, 0.8f, 0.9f));

            return listView;
        }

        private GameObject CreateEnemyDetailViewObject()
        {
            var detailView = new GameObject("View_EnemyDetail", typeof(RectTransform));
            detailView.transform.SetParent(transform, false);

            RectTransform detailRect = detailView.GetComponent<RectTransform>();
            detailRect.anchorMin = new Vector2(0.5f, 0.5f);
            detailRect.anchorMax = new Vector2(0.5f, 0.5f);
            detailRect.anchoredPosition = new Vector2(0f, -20f);
            detailRect.sizeDelta = new Vector2(420f, 900f);

            Transform sampleBtnImg = transform.Find("Group_ChangeTilePanel/Img_C_ChangeTilePanel_BG 128*128");

            var backBtnObj = new GameObject("Btn_BackToList", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            backBtnObj.transform.SetParent(detailView.transform, false);
            RectTransform backBtnRect = backBtnObj.GetComponent<RectTransform>();
            backBtnRect.anchoredPosition = new Vector2(-120f, 385f);
            backBtnRect.sizeDelta = new Vector2(100f, 36f);
            Image backBtnImg = backBtnObj.GetComponent<Image>();
            CopyImageStyle(sampleBtnImg, backBtnImg);
            backBtnImg.color = new Color(0.25f, 0.35f, 0.45f, 0.95f);
            m_btnBackToList = backBtnObj.GetComponent<Button>();
            CreateText(backBtnObj.transform, "Txt_Back", "← 목록", Vector2.zero, new Vector2(100f, 36f),
                14, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);

            m_txtDetailTitle = CreateText(detailView.transform, "Txt_EnemyDetailName", "적 정보",
                new Vector2(35f, 385f), new Vector2(230f, 36f), 19, FontStyles.Bold, TextAlignmentOptions.Left, Color.white);

            var statsGroup = new GameObject("Group_Stats", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            statsGroup.transform.SetParent(detailView.transform, false);
            RectTransform statsRect = statsGroup.GetComponent<RectTransform>();
            statsRect.anchoredPosition = new Vector2(0f, 265f);
            statsRect.sizeDelta = new Vector2(350f, 190f);
            Image statsBg = statsGroup.GetComponent<Image>();
            CopyImageStyle(sampleBtnImg, statsBg);
            statsBg.color = new Color(0.12f, 0.14f, 0.18f, 0.85f);

            m_txtHP = CreateStatRow(statsGroup.transform, "Txt_EnemyHP", "체력: - / -", 62f, new Color(1f, 0.45f, 0.45f));
            m_txtDefend = CreateStatRow(statsGroup.transform, "Txt_EnemyDefend", "방어력: -", 37f, new Color(0.95f, 0.8f, 0.35f));
            m_txtAction = CreateStatRow(statsGroup.transform, "Txt_EnemyAction", "행동력: -", 12f, new Color(0.45f, 0.8f, 1f));
            m_txtSpeed = CreateStatRow(statsGroup.transform, "Txt_EnemySpeed", "이동력: -", -13f, new Color(0.55f, 1f, 0.55f));
            m_txtResistance = CreateStatRow(statsGroup.transform, "Txt_EnemyResistance", "저항력: -", -38f, new Color(0.85f, 0.65f, 1f));
            m_txtHitCount = CreateStatRow(statsGroup.transform, "Txt_EnemyHitCount", "적중 수: -", -63f, new Color(0.9f, 0.9f, 0.9f));

            RepositionBuffDebuffGroups(detailView.transform);
            return detailView;
        }

        private TextMeshProUGUI CreateStatRow(Transform parent, string objectName, string defaultText, float posY, Color color)
        {
            return CreateText(parent, objectName, defaultText, new Vector2(0f, posY), new Vector2(320f, 24f),
                15, FontStyles.Bold, TextAlignmentOptions.Left, color);
        }

        private TextMeshProUGUI CreateText(Transform parent, string objectName, string text, Vector2 position, Vector2 size,
            float fontSize, FontStyles style, TextAlignmentOptions alignment, Color color)
        {
            var obj = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
            obj.transform.SetParent(parent, false);
            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            TextMeshProUGUI tmp = obj.GetComponent<TextMeshProUGUI>();
            if (m_cachedFont != null) tmp.font = m_cachedFont;
            tmp.fontSize = fontSize;
            tmp.fontStyle = style;
            tmp.alignment = alignment;
            tmp.color = color;
            tmp.text = text;
            return tmp;
        }

        private static void CopyImageStyle(Transform sample, Image target)
        {
            if (sample != null && sample.TryGetComponent(out Image sampleImage))
            {
                target.sprite = sampleImage.sprite;
                target.type = sampleImage.type;
            }
        }

        private void RepositionBuffDebuffGroups(Transform detailParent)
        {
            MoveGroup("Group_Buff", detailParent, new Vector2(0f, 145f), new Vector2(350f, 30f));
            MoveGroup("Group_BuffRect", detailParent, new Vector2(0f, 75f), new Vector2(350f, 110f));
            MoveGroup("Group_Debuff", detailParent, new Vector2(0f, -5f), new Vector2(350f, 30f));
            MoveGroup("Group_DebuffRect", detailParent, new Vector2(0f, -190f), new Vector2(350f, 320f));
        }

        private void MoveGroup(string groupName, Transform parent, Vector2 position, Vector2 size)
        {
            Transform group = transform.Find(groupName);
            if (group == null) return;

            group.SetParent(parent, false);
            RectTransform rect = group.GetComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        public void OpenForTile(int pathIndex)
        {
            m_currentPathIndex = pathIndex;
            m_selectedTarget = null;

            gameObject.SetActive(true);
            Subscribe();

            RefreshListEntries();
            ShowListView();
        }

        public void ShowListView()
        {
            if (m_viewEnemyList != null) m_viewEnemyList.SetActive(true);
            if (m_viewEnemyDetail != null) m_viewEnemyDetail.SetActive(false);
        }

        public void ShowDetailView()
        {
            if (m_viewEnemyList != null) m_viewEnemyList.SetActive(false);
            if (m_viewEnemyDetail != null) m_viewEnemyDetail.SetActive(true);
            SetDetailFace(false);                                       // 다른 적을 고르면 앞면(스탯)부터
        }

        public void RefreshListEntries()
        {
            IReadOnlyList<ITarget> targets = m_currentPathIndex >= 0
                ? BoardManager.Instance.Occupancy.GetAt(m_currentPathIndex)
                : new List<ITarget>();

            if (m_txtListTitle != null) m_txtListTitle.text = $"타일 위 적 목록 ({targets.Count}마리)";
            if (m_txtNoEnemies != null) m_txtNoEnemies.gameObject.SetActive(targets.Count == 0);

            RectTransform content = m_listContent != null ? m_listContent : m_listScrollRect != null ? m_listScrollRect.content : null;
            if (content == null)
            {
                Debug.LogWarning("[EnemyInfoPanel] 목록 Content가 연결되지 않음");
                return;
            }

            int shown = 0;
            for (int i = 0; i < targets.Count; i++)
            {
                if (!targets[i].IsAlive) continue;

                EnemyListItemUI item = GetOrCreateListItem(shown, content);
                PlaceListItem(item, shown, GetEntryWidth(content));
                item.Init(targets[i], SelectTarget);
                shown++;
            }

            for (int i = shown; i < m_listItems.Count; i++)
            {
                m_listItems[i].gameObject.SetActive(false);
            }

            float totalHeight = Mathf.Max(content.rect.height, Mathf.Abs(ListStartY) + shown * ListSpacing + 20f);
            content.sizeDelta = new Vector2(content.sizeDelta.x, totalHeight);
            if (m_listScrollRect != null) m_listScrollRect.verticalNormalizedPosition = 1f;
        }

        private void RefreshListDisplays()
        {
            foreach (EnemyListItemUI item in m_listItems)
            {
                if (item.gameObject.activeSelf) item.RefreshDisplay();
            }
        }

        private EnemyListItemUI GetOrCreateListItem(int index, RectTransform content)
        {
            if (index < m_listItems.Count) return m_listItems[index];

            GameObject entryObj = m_enemyListItemPrefab != null
                ? Instantiate(m_enemyListItemPrefab, content)
                : CreateDefaultListItemObject(content, index);

            if (!entryObj.TryGetComponent(out EnemyListItemUI item)) item = entryObj.AddComponent<EnemyListItemUI>();
            m_listItems.Add(item);
            return item;
        }

        private static void PlaceListItem(EnemyListItemUI item, int index, float width)
        {
            RectTransform rect = item.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, ListStartY - index * ListSpacing);
            rect.sizeDelta = new Vector2(width, 74f);
        }

        // 목록 칸의 너비에 맞춤 (옛 레이아웃은 330 고정이라 좁은 목록에서 잘렸음)
        private static float GetEntryWidth(RectTransform content)
        {
            Transform viewport = content.parent;
            float width = viewport is RectTransform viewportRect ? viewportRect.rect.width : content.rect.width;
            return width > 20f ? width - 10f : 330f;
        }

        private GameObject CreateDefaultListItemObject(RectTransform parent, int index)
        {
            var root = new GameObject($"EnemyItem_{index}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            root.transform.SetParent(parent, false);

            Image bgImg = root.GetComponent<Image>();
            CopyImageStyle(transform.Find("Group_ChangeTilePanel/Img_C_ChangeTilePanel_BG 128*128"), bgImg);
            bgImg.color = new Color(0.18f, 0.22f, 0.28f, 0.95f);

            var frame = new GameObject("Frame", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            frame.transform.SetParent(root.transform, false);
            RectTransform frameRect = frame.GetComponent<RectTransform>();
            frameRect.anchorMin = Vector2.zero;
            frameRect.anchorMax = Vector2.one;
            frameRect.sizeDelta = Vector2.zero;
            Image frameImg = frame.GetComponent<Image>();
            Transform sampleFrame = transform.Find("Group_ChangeTilePanel/Img_ChangeTilePanel_Frame 128*128");
            if (sampleFrame != null && sampleFrame.TryGetComponent(out Image fImg))
            {
                frameImg.sprite = fImg.sprite;
                frameImg.type = fImg.type;
                frameImg.color = fImg.color;
            }

            var iconObj = new GameObject("Img_Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            iconObj.transform.SetParent(root.transform, false);
            RectTransform iconRect = iconObj.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0f, 0.5f);
            iconRect.anchorMax = new Vector2(0f, 0.5f);
            iconRect.anchoredPosition = new Vector2(40f, 0f);
            iconRect.sizeDelta = new Vector2(50f, 50f);
            iconObj.GetComponent<Image>().preserveAspect = true;

            TextMeshProUGUI nameTxt = CreateText(root.transform, "Txt_Name", "", new Vector2(80f, 12f), new Vector2(-120f, 26f),
                17, FontStyles.Bold, TextAlignmentOptions.Left, Color.white);
            StretchHorizontally(nameTxt.rectTransform);

            TextMeshProUGUI hpTxt = CreateText(root.transform, "Txt_HP", "", new Vector2(80f, -14f), new Vector2(-120f, 22f),
                14, FontStyles.Normal, TextAlignmentOptions.Left, new Color(1f, 0.6f, 0.6f));
            StretchHorizontally(hpTxt.rectTransform);

            TextMeshProUGUI arrowTxt = CreateText(root.transform, "Txt_Arrow", "▶", new Vector2(-15f, 0f), new Vector2(25f, 30f),
                18, FontStyles.Normal, TextAlignmentOptions.Center, new Color(0.8f, 0.8f, 0.8f));
            arrowTxt.rectTransform.anchorMin = new Vector2(1f, 0.5f);
            arrowTxt.rectTransform.anchorMax = new Vector2(1f, 0.5f);
            arrowTxt.rectTransform.pivot = new Vector2(1f, 0.5f);

            return root;
        }

        private static void StretchHorizontally(RectTransform rect)
        {
            Vector2 position = rect.anchoredPosition;
            Vector2 size = rect.sizeDelta;
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(1f, 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        public void SelectTarget(ITarget target)
        {
            if (target == null || !target.IsAlive) return;

            m_selectedTarget = target;
            ShowDetailView();

            UpdateStatsText();
            RefreshBuffEntries();
            RefreshDebuffEntries();
        }

        private void UpdateStatsText()
        {
            if (m_selectedTarget == null) return;

            EnemyController body = m_selectedTarget.Body;
            EnemyDefinition definition = body != null ? body.Definition : null;

            string title = EnemyListItemUI.GetDisplayName(m_selectedTarget);
            if (body != null && body.IsBoss) title += $" 남은 턴 {EnemyManager.Instance.BossTimer.GetRemaining(body)}";
            if (m_txtDetailTitle != null) m_txtDetailTitle.text = title;

            int hp = Mathf.CeilToInt(m_selectedTarget.CurrentHp);
            int maxHp = Mathf.CeilToInt(m_selectedTarget.MaxHp);
            if (m_txtHP != null) m_txtHP.text = m_compactValues ? $"{hp} / {maxHp}" : $"체력: {hp} / {maxHp}";
            if (m_hpSlider != null)
            {
                m_hpSlider.minValue = 0f;
                m_hpSlider.maxValue = Mathf.Max(1f, m_selectedTarget.MaxHp);
                m_hpSlider.value = m_selectedTarget.CurrentHp;
            }

            if (definition == null) return;

            if (m_txtEnemyType != null) m_txtEnemyType.text = GetKindName(definition.Kind);

            EnemyStats stats = body.Stats;
            float defense = stats.Get(StatType.Defense);
            float baseDefense = stats.GetBase(StatType.Defense);
            string defenseText = Mathf.Approximately(defense, baseDefense) ? $"{defense:0.#}" : $"{defense:0.#} (기본 {baseDefense:0.#})";
            if (m_txtDefend != null) m_txtDefend.text = m_compactValues ? defenseText : $"방어력: {defenseText}";

            MoveRange range = stats.GetMoveRange();
            int left = Mathf.Max(0, body.Movement.ActionCounter);
            if (m_txtAction != null)
            {
                string cycle = range.Action == definition.Action ? $"{range.Action}턴 주기" : $"{range.Action}턴 주기 (기본 {definition.Action})";
                m_txtAction.text = m_compactValues ? $"{left}/{range.Action}" : $"행동력: {cycle}, 다음 행동까지 {left}";
            }
            bool speedChanged = range.Min != definition.SpeedMin || range.Max != definition.SpeedMax;
            if (m_txtSpeed != null)
            {
                if (m_compactValues) m_txtSpeed.text = $"{range.Min}~{range.Max}";
                else m_txtSpeed.text = speedChanged ? $"이동력: 주사위 {range.Min} ~ {range.Max}칸 (기본 {definition.SpeedMin} ~ {definition.SpeedMax})"
                                                    : $"이동력: 주사위 {range.Min} ~ {range.Max}칸";
            }
            if (m_txtResistance != null)
            {
                EnemyResistance resistance = body.Resistance;
                string text = Mathf.Approximately(resistance.Current, resistance.Base)
                    ? $"{resistance.Current:0.#}%"
                    : $"{resistance.Current:0.#}% (기본 {resistance.Base:0.#}%)";          // 강한 CC로 올라 있으면 기본도 함께 (20번 3장)
                m_txtResistance.text = m_compactValues ? text : $"저항력: {text}";
            }
            if (m_txtDamageTaken != null)
            {
                float extra = (stats.Get(StatType.DamageTaken) - 1f) * 100f;
                m_txtDamageTaken.text = Mathf.Approximately(extra, 0f) ? "0%" : $"{extra:+0.#;-0.#}%";
            }
            if (m_txtHitCount != null)
            {
                string hits = GetHitCountText(body.HitCounter);
                m_txtHitCount.text = m_compactValues ? hits : $"적중 수: {hits}";
            }
        }

        // 단일 타워 문양별로 이 적이 맞은 횟수 (20번 4장: 적이 몇 회 맞았는지는 적 쪽에서 보여 줌). 예: "검 3, 활 1"
        private static string GetHitCountText(EnemyHitCounter counter)
        {
            var parts = new List<string>();
            foreach (TowerEmblem emblem in System.Enum.GetValues(typeof(TowerEmblem)))
            {
                if (emblem == TowerEmblem.None) continue;
                int count = counter.Get(emblem);
                if (count > 0) parts.Add($"{EmblemNames.GetEmblemName(emblem)} {count}");
            }
            return parts.Count > 0 ? string.Join(", ", parts) : "-";
        }

        private static string GetKindName(EnemyKind kind)
        {
            switch (kind)
            {
                case EnemyKind.MiddleBoss: return "중간 보스";
                case EnemyKind.FinalBoss: return "최종 보스";
                default: return "일반";
            }
        }

        private void RefreshBuffEntries()
        {
            ClearEntries(m_spawnedBuffEntries);

            RectTransform content = m_buffContent != null ? m_buffContent : m_buffScrollRect != null ? m_buffScrollRect.content : null;
            if (content == null) return;

            var descriptions = new List<string>();
            BoardManager board = BoardManager.Instance;
            PathTileType tile = board.Tiles.GetPathTile(m_selectedTarget.PathIndex);
            if (SpecialTileText.IsStandingBuff(tile)) descriptions.Add(SpecialTileText.Describe(tile, m_selectedTarget.PathIndex, board.Grid));
            // 수업 21: 큐브 윗면의 효과 (EnemyCubeFaces) — 면의 수치가 정해지면 (지금은 전부 0)
            if (descriptions.Count == 0) descriptions.Add("적용 중인 버프 없음\n현재 적에게 적용된 버프가 없습니다.");

            CreateEntries(content, descriptions, m_spawnedBuffEntries);
            if (m_buffScrollRect != null) m_buffScrollRect.verticalNormalizedPosition = 1f;
        }

        private void RefreshDebuffEntries()
        {
            ClearEntries(m_spawnedDebuffEntries);

            RectTransform content = m_debuffContent != null ? m_debuffContent : m_debuffScrollRect != null ? m_debuffScrollRect.content : null;
            if (content == null) return;

            var descriptions = new List<string>();
            descriptions.AddRange(StatusTextFormatter.DescribeAll(m_selectedTarget.Status));
            if (descriptions.Count == 0) descriptions.Add("적용 중인 디버프 없음\n현재 적에게 적용된 디버프가 없습니다.");

            CreateEntries(content, descriptions, m_spawnedDebuffEntries);
            if (m_debuffScrollRect != null) m_debuffScrollRect.verticalNormalizedPosition = 1f;
        }

        private void CreateEntries(RectTransform content, List<string> descriptions, List<GameObject> spawnedList)
        {
            const float startY = -40f;
            const float spacing = 75f;

            for (int i = 0; i < descriptions.Count; i++)
            {
                GameObject entry = m_buffEntryPrefab != null ? Instantiate(m_buffEntryPrefab, content) : CreateDefaultInfoEntry(content, i);
                entry.SetActive(true);

                RectTransform rect = entry.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 1f);
                rect.anchorMax = new Vector2(0.5f, 1f);
                rect.pivot = new Vector2(0.5f, 1f);
                rect.anchoredPosition = new Vector2(0f, startY - i * spacing);
                rect.sizeDelta = new Vector2(GetEntryWidth(content), 70f);

                TextMeshProUGUI txt = entry.GetComponentInChildren<TextMeshProUGUI>(true);
                if (txt != null) txt.text = descriptions[i];

                spawnedList.Add(entry);
            }

            float totalHeight = Mathf.Max(content.rect.height, Mathf.Abs(startY) + descriptions.Count * spacing + 20f);
            content.sizeDelta = new Vector2(content.sizeDelta.x, totalHeight);
        }

        private GameObject CreateDefaultInfoEntry(RectTransform parent, int index)
        {
            var root = new GameObject($"InfoEntry_{index}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            root.transform.SetParent(parent, false);

            Image bg = root.GetComponent<Image>();
            CopyImageStyle(transform.Find("Group_ChangeTilePanel/Img_C_ChangeTilePanel_BG 128*128"), bg);
            bg.color = new Color(0.18f, 0.22f, 0.28f, 0.95f);

            TextMeshProUGUI txt = CreateText(root.transform, "Txt_Desc", "", Vector2.zero, new Vector2(-20f, -10f),
                14, FontStyles.Normal, TextAlignmentOptions.Center, Color.white);
            txt.rectTransform.anchorMin = Vector2.zero;
            txt.rectTransform.anchorMax = Vector2.one;
            return root;
        }

        private static void ClearEntries(List<GameObject> entries)
        {
            foreach (GameObject entry in entries)
            {
                if (entry != null) Destroy(entry);
            }
            entries.Clear();
        }

        public void BackToList()
        {
            m_selectedTarget = null;
            RefreshListEntries();
            ShowListView();
        }

        public void Hide()
        {
            m_selectedTarget = null;
            ClearEntries(m_spawnedBuffEntries);
            ClearEntries(m_spawnedDebuffEntries);
            gameObject.SetActive(false);
        }
    }
}
