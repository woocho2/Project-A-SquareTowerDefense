using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 패스 타일 클릭 시 해당 타일 위 적 목록과 선택된 적의 세부 스탯, 버프/디버프를 표시하는 패널 컨트롤러입니다.
/// </summary>
public class EnemyInfoPanelUI : MonoBehaviour
{
    #region Inspector Fields

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

    [Header("Buff & Debuff Rects")]
    [SerializeField] private RectTransform m_buffContent;
    [SerializeField] private RectTransform m_debuffContent;
    [SerializeField] private ScrollRect m_buffScrollRect;
    [SerializeField] private ScrollRect m_debuffScrollRect;
    [SerializeField] private GameObject m_buffEntryPrefab;

    #endregion

    #region Private Fields

    private readonly List<GameObject> m_spawnedListEntries = new List<GameObject>();
    private readonly List<GameObject> m_spawnedBuffEntries = new List<GameObject>();
    private readonly List<GameObject> m_spawnedDebuffEntries = new List<GameObject>();

    private Vector3Int m_currentPathCell;
    private int m_currentPathIndex = -1;
    private EnemyHealthController m_selectedEnemy;
    private TMP_FontAsset m_cachedFont;

    public Vector3Int CurrentPathCell => m_currentPathCell;
    public int CurrentPathIndex => m_currentPathIndex;
    public EnemyHealthController SelectedEnemy => m_selectedEnemy;

    #endregion

    #region Unity Lifecycle

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
    }

    private void Update()
    {
        if (m_viewEnemyDetail != null && m_viewEnemyDetail.activeSelf)
        {
            if (m_selectedEnemy == null || !m_selectedEnemy.gameObject.activeInHierarchy || m_selectedEnemy.CurrentHP <= 0f)
            {
                // 선택된 적이 사망하거나 풀로 반환된 경우 목록으로 복귀
                m_selectedEnemy = null;
                RefreshListEntries();
                ShowListView();
            }
            else
            {
                UpdateStatsText();
            }
        }
    }

    #endregion

    #region Initialization and Dynamic Layout Setup

    public void InitializeComponents()
    {
        // 1. 기존 타일 인포 패널 구조에서 스크롤 컨텐츠 및 프리팹 참조
        FindExistingScrollRects();

        // 2. 폰트 에셋 캐싱
        TextMeshProUGUI anyText = GetComponentInChildren<TextMeshProUGUI>(true);
        if (anyText != null && anyText.font != null)
        {
            m_cachedFont = anyText.font;
        }

        // 3. BuffEntryPrefab 연결 (BuffPrefab 프리팹 리소스 또는 부모 UIManager 참조)
        if (m_buffEntryPrefab == null)
        {
            m_buffEntryPrefab = Resources.Load<GameObject>("BuffPrefab");
            if (m_buffEntryPrefab == null)
            {
                // BuffPrefab이 Resources에 없으면 씬에서 TileInfoPanel 쪽 프리팹을 찾거나 복제
                if (UIManager.Instance != null && UIManager.Instance.TileBuffEntryPrefab != null)
                {
                    m_buffEntryPrefab = UIManager.Instance.TileBuffEntryPrefab;
                }
            }
        }

        // 4. View_EnemyList / View_EnemyDetail 계층 보장
        EnsureViewHierarchy();
    }

    private void FindExistingScrollRects()
    {
        if (m_buffScrollRect == null)
        {
            Transform buffRectTransform = transform.Find("Group_BuffRect/Img_A_BuffScrollRect_BG 128*128")
                ?? transform.Find("Group_BuffRect/BuffScrollRect")
                ?? transform.Find("Group_BuffRect");
            if (buffRectTransform != null) m_buffScrollRect = buffRectTransform.GetComponentInChildren<ScrollRect>(true);
        }

        if (m_buffContent == null && m_buffScrollRect != null)
        {
            m_buffContent = m_buffScrollRect.content;
        }

        if (m_debuffScrollRect == null)
        {
            Transform debuffRectTransform = transform.Find("Group_DebuffRect/Img_A_DebuffScrollRect_BG 128*128")
                ?? transform.Find("Group_DebuffRect/DebuffScrollRect")
                ?? transform.Find("Group_DebuffRect");
            if (debuffRectTransform != null) m_debuffScrollRect = debuffRectTransform.GetComponentInChildren<ScrollRect>(true);
        }

        if (m_debuffContent == null && m_debuffScrollRect != null)
        {
            m_debuffContent = m_debuffScrollRect.content;
        }
    }

    private void EnsureViewHierarchy()
    {
        Transform listTr = transform.Find("View_EnemyList");
        if (listTr != null)
        {
            m_viewEnemyList = listTr.gameObject;
        }
        else
        {
            m_viewEnemyList = CreateEnemyListViewObject();
        }

        Transform detailTr = transform.Find("View_EnemyDetail");
        if (detailTr != null)
        {
            m_viewEnemyDetail = detailTr.gameObject;
        }
        else
        {
            m_viewEnemyDetail = CreateEnemyDetailViewObject();
        }

        // 바인딩 확인
        if (m_btnBackToList == null)
        {
            Button[] buttons = m_viewEnemyDetail.GetComponentsInChildren<Button>(true);
            foreach (Button btn in buttons)
            {
                if (btn.name.Contains("Back") || btn.name.Contains("List"))
                {
                    m_btnBackToList = btn;
                    break;
                }
            }
        }

        TextMeshProUGUI[] texts = GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (TextMeshProUGUI txt in texts)
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
            }
        }
    }

    private GameObject CreateEnemyListViewObject()
    {
        GameObject listView = new GameObject("View_EnemyList", typeof(RectTransform));
        listView.transform.SetParent(transform, false);

        RectTransform listRect = listView.GetComponent<RectTransform>();
        listRect.anchorMin = new Vector2(0.5f, 0.5f);
        listRect.anchorMax = new Vector2(0.5f, 0.5f);
        listRect.anchoredPosition = new Vector2(0f, -20f);
        listRect.sizeDelta = new Vector2(420f, 900f);

        // 1. Title Bar
        GameObject titleObj = new GameObject("Txt_EnemyListTitle", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleObj.transform.SetParent(listView.transform, false);
        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchoredPosition = new Vector2(0f, 380f);
        titleRect.sizeDelta = new Vector2(350f, 40f);
        TextMeshProUGUI titleText = titleObj.GetComponent<TextMeshProUGUI>();
        if (m_cachedFont != null) titleText.font = m_cachedFont;
        titleText.fontSize = 20;
        titleText.fontStyle = FontStyles.Bold;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.color = Color.white;
        titleText.text = "타일 위 적 목록";
        m_txtListTitle = titleText;

        // 2. ScrollRect (Group_BuffRect 복제 활용)
        Transform existingBuffRect = transform.Find("Group_BuffRect");
        if (existingBuffRect != null)
        {
            GameObject scrollObj = Instantiate(existingBuffRect.gameObject, listView.transform, false);
            scrollObj.name = "EnemyListScrollRect";
            RectTransform sRect = scrollObj.GetComponent<RectTransform>();
            sRect.anchoredPosition = new Vector2(0f, -20f);
            sRect.sizeDelta = new Vector2(360f, 720f);

            ScrollRect sr = scrollObj.GetComponentInChildren<ScrollRect>(true);
            if (sr != null)
            {
                m_listScrollRect = sr;
                m_listContent = sr.content;
                // 이전 복제 잔여물 정리
                for (int i = m_listContent.childCount - 1; i >= 0; i--)
                {
                    Destroy(m_listContent.GetChild(i).gameObject);
                }
            }
        }

        // 3. 안내 문구 (적 없을 때)
        GameObject noEnemyObj = new GameObject("Txt_NoEnemies", typeof(RectTransform), typeof(TextMeshProUGUI));
        noEnemyObj.transform.SetParent(listView.transform, false);
        RectTransform noEnemyRect = noEnemyObj.GetComponent<RectTransform>();
        noEnemyRect.anchoredPosition = new Vector2(0f, 0f);
        noEnemyRect.sizeDelta = new Vector2(320f, 60f);
        TextMeshProUGUI noEnemyText = noEnemyObj.GetComponent<TextMeshProUGUI>();
        if (m_cachedFont != null) noEnemyText.font = m_cachedFont;
        noEnemyText.fontSize = 16;
        noEnemyText.alignment = TextAlignmentOptions.Center;
        noEnemyText.color = new Color(0.8f, 0.8f, 0.8f, 0.9f);
        noEnemyText.text = "현재 이 타일 위에 적이 없습니다.";
        m_txtNoEnemies = noEnemyText;

        return listView;
    }

    private GameObject CreateEnemyDetailViewObject()
    {
        GameObject detailView = new GameObject("View_EnemyDetail", typeof(RectTransform));
        detailView.transform.SetParent(transform, false);

        RectTransform detailRect = detailView.GetComponent<RectTransform>();
        detailRect.anchorMin = new Vector2(0.5f, 0.5f);
        detailRect.anchorMax = new Vector2(0.5f, 0.5f);
        detailRect.anchoredPosition = new Vector2(0f, -20f);
        detailRect.sizeDelta = new Vector2(420f, 900f);

        // 1. Header (뒤로가기 버튼 + 적 이름)
        // 뒤로가기 버튼
        GameObject backBtnObj = new GameObject("Btn_BackToList", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        backBtnObj.transform.SetParent(detailView.transform, false);
        RectTransform backBtnRect = backBtnObj.GetComponent<RectTransform>();
        backBtnRect.anchoredPosition = new Vector2(-120f, 385f);
        backBtnRect.sizeDelta = new Vector2(100f, 36f);

        Image backBtnImg = backBtnObj.GetComponent<Image>();
        Transform sampleBtnImg = transform.Find("Group_ChangeTilePanel/Img_A_ChangeTilePanel_BG 128*128");
        if (sampleBtnImg != null && sampleBtnImg.TryGetComponent(out Image sImg))
        {
            backBtnImg.sprite = sImg.sprite;
            backBtnImg.type = sImg.type;
        }
        backBtnImg.color = new Color(0.25f, 0.35f, 0.45f, 0.95f);
        m_btnBackToList = backBtnObj.GetComponent<Button>();

        GameObject backBtnTxtObj = new GameObject("Txt_Back", typeof(RectTransform), typeof(TextMeshProUGUI));
        backBtnTxtObj.transform.SetParent(backBtnObj.transform, false);
        RectTransform backBtnTxtRect = backBtnTxtObj.GetComponent<RectTransform>();
        backBtnTxtRect.anchoredPosition = Vector2.zero;
        backBtnTxtRect.sizeDelta = new Vector2(100f, 36f);
        TextMeshProUGUI backBtnTxt = backBtnTxtObj.GetComponent<TextMeshProUGUI>();
        if (m_cachedFont != null) backBtnTxt.font = m_cachedFont;
        backBtnTxt.fontSize = 14;
        backBtnTxt.fontStyle = FontStyles.Bold;
        backBtnTxt.alignment = TextAlignmentOptions.Center;
        backBtnTxt.color = Color.white;
        backBtnTxt.text = "← 목록";

        // 적 이름 타이틀
        GameObject nameObj = new GameObject("Txt_EnemyDetailName", typeof(RectTransform), typeof(TextMeshProUGUI));
        nameObj.transform.SetParent(detailView.transform, false);
        RectTransform nameRect = nameObj.GetComponent<RectTransform>();
        nameRect.anchoredPosition = new Vector2(35f, 385f);
        nameRect.sizeDelta = new Vector2(230f, 36f);
        TextMeshProUGUI nameTxt = nameObj.GetComponent<TextMeshProUGUI>();
        if (m_cachedFont != null) nameTxt.font = m_cachedFont;
        nameTxt.fontSize = 19;
        nameTxt.fontStyle = FontStyles.Bold;
        nameTxt.alignment = TextAlignmentOptions.Left;
        nameTxt.color = Color.white;
        nameTxt.text = "적 정보";
        m_txtDetailTitle = nameTxt;

        // 2. 세부 스탯 그룹 (Group_Stats)
        GameObject statsGroup = new GameObject("Group_Stats", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        statsGroup.transform.SetParent(detailView.transform, false);
        RectTransform statsRect = statsGroup.GetComponent<RectTransform>();
        statsRect.anchoredPosition = new Vector2(0f, 265f);
        statsRect.sizeDelta = new Vector2(350f, 170f);

        Image statsBg = statsGroup.GetComponent<Image>();
        if (sampleBtnImg != null && sampleBtnImg.TryGetComponent(out Image sampleImg))
        {
            statsBg.sprite = sampleImg.sprite;
            statsBg.type = sampleImg.type;
        }
        statsBg.color = new Color(0.12f, 0.14f, 0.18f, 0.85f);

        // 스탯 항목 텍스트 생성 헬퍼
        m_txtHP = CreateStatRow(statsGroup.transform, "Txt_EnemyHP", "체력: - / -", 50f, new Color(1f, 0.45f, 0.45f));
        m_txtDefend = CreateStatRow(statsGroup.transform, "Txt_EnemyDefend", "방어력: -", 25f, new Color(0.95f, 0.8f, 0.35f));
        m_txtAction = CreateStatRow(statsGroup.transform, "Txt_EnemyAction", "행동력: -", 0f, new Color(0.45f, 0.8f, 1f));
        m_txtSpeed = CreateStatRow(statsGroup.transform, "Txt_EnemySpeed", "이동력: -", -25f, new Color(0.55f, 1f, 0.55f));
        m_txtResistance = CreateStatRow(statsGroup.transform, "Txt_EnemyResistance", "저항력: -", -50f, new Color(0.85f, 0.65f, 1f));

        // 3. 기존의 Group_Buff, Group_BuffRect, Group_Debuff, Group_DebuffRect를 DetailView 안으로 통합 재배치
        RepositionBuffDebuffGroups(detailView.transform);

        return detailView;
    }

    private TextMeshProUGUI CreateStatRow(Transform parent, string objectName, string defaultText, float posY, Color color)
    {
        GameObject rowObj = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
        rowObj.transform.SetParent(parent, false);

        RectTransform rect = rowObj.GetComponent<RectTransform>();
        rect.anchoredPosition = new Vector2(0f, posY);
        rect.sizeDelta = new Vector2(320f, 24f);

        TextMeshProUGUI txt = rowObj.GetComponent<TextMeshProUGUI>();
        if (m_cachedFont != null) txt.font = m_cachedFont;
        txt.fontSize = 15;
        txt.fontStyle = FontStyles.Bold;
        txt.alignment = TextAlignmentOptions.Left;
        txt.color = color;
        txt.text = defaultText;

        return txt;
    }

    private void RepositionBuffDebuffGroups(Transform detailParent)
    {
        Transform buffTitle = transform.Find("Group_Buff");
        if (buffTitle != null)
        {
            buffTitle.SetParent(detailParent, false);
            RectTransform r = buffTitle.GetComponent<RectTransform>();
            r.anchoredPosition = new Vector2(0f, 155f);
            r.sizeDelta = new Vector2(350f, 30f);
        }

        Transform buffRect = transform.Find("Group_BuffRect");
        if (buffRect != null)
        {
            buffRect.SetParent(detailParent, false);
            RectTransform r = buffRect.GetComponent<RectTransform>();
            r.anchoredPosition = new Vector2(0f, 80f);
            r.sizeDelta = new Vector2(350f, 110f);
        }

        Transform debuffTitle = transform.Find("Group_Debuff");
        if (debuffTitle != null)
        {
            debuffTitle.SetParent(detailParent, false);
            RectTransform r = debuffTitle.GetComponent<RectTransform>();
            r.anchoredPosition = new Vector2(0f, 0f);
            r.sizeDelta = new Vector2(350f, 30f);
        }

        Transform debuffRect = transform.Find("Group_DebuffRect");
        if (debuffRect != null)
        {
            debuffRect.SetParent(detailParent, false);
            RectTransform r = debuffRect.GetComponent<RectTransform>();
            r.anchoredPosition = new Vector2(0f, -190f);
            r.sizeDelta = new Vector2(350f, 330f);
        }
    }

    #endregion

    #region Panel Open and View Switching

    /// <summary>
    /// 패스 타일을 클릭했을 때 적 목록을 열고 해당 타일의 적들을 표시합니다.
    /// </summary>
    public void OpenForTile(Vector3Int pathCell, int pathIndex)
    {
        m_currentPathCell = pathCell;
        m_currentPathIndex = pathIndex;
        m_selectedEnemy = null;

        gameObject.SetActive(true);

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
    }

    #endregion

    #region Enemy List Handling

    public void RefreshListEntries()
    {
        ClearEntries(m_spawnedListEntries);

        List<EnemyHealthController> enemies = TilePath.Instance != null
            ? TilePath.Instance.GetEnemiesAtIndex(m_currentPathIndex)
            : new List<EnemyHealthController>();

        if (m_txtListTitle != null)
        {
            m_txtListTitle.text = $"타일 위 적 목록 ({enemies.Count}마리)";
        }

        if (enemies.Count == 0)
        {
            if (m_txtNoEnemies != null)
            {
                m_txtNoEnemies.gameObject.SetActive(true);
                m_txtNoEnemies.text = "현재 이 타일 위에 적이 없습니다.";
            }
            return;
        }

        if (m_txtNoEnemies != null)
        {
            m_txtNoEnemies.gameObject.SetActive(false);
        }

        RectTransform content = m_listContent;
        if (content == null && m_listScrollRect != null) content = m_listScrollRect.content;

        if (content == null)
        {
            Debug.LogWarning("[EnemyInfoPanelUI] List Content RectTransform이 연결되지 않았습니다.");
            return;
        }

        float startY = -45f;
        float spacing = 82f;

        for (int i = 0; i < enemies.Count; i++)
        {
            EnemyHealthController enemy = enemies[i];
            if (enemy == null || !enemy.gameObject.activeInHierarchy || enemy.CurrentHP <= 0f) continue;

            GameObject entryObj = CreateListItem(enemy, content, i, startY, spacing);
            if (entryObj != null)
            {
                m_spawnedListEntries.Add(entryObj);
            }
        }

        float totalHeight = Mathf.Max(content.rect.height, Mathf.Abs(startY) + (m_spawnedListEntries.Count * spacing) + 20f);
        content.sizeDelta = new Vector2(content.sizeDelta.x, totalHeight);
        if (m_listScrollRect != null) m_listScrollRect.verticalNormalizedPosition = 1f;
    }

    private GameObject CreateListItem(EnemyHealthController enemy, RectTransform content, int index, float startY, float spacing)
    {
        GameObject entryObj = null;

        if (m_enemyListItemPrefab != null)
        {
            entryObj = Instantiate(m_enemyListItemPrefab, content);
        }
        else
        {
            // 전용 리스트 아이템 UI 프리팹 동적 구성 (아이콘, 이름, 체력, 화살표)
            entryObj = CreateDefaultListItemObject(content, index);
        }

        RectTransform rect = entryObj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, startY - (index * spacing));
        rect.sizeDelta = new Vector2(330f, 74f);

        if (!entryObj.TryGetComponent(out EnemyListItemUI itemUI))
        {
            itemUI = entryObj.AddComponent<EnemyListItemUI>();
        }

        itemUI.Init(enemy, SelectEnemy);
        return entryObj;
    }

    private GameObject CreateDefaultListItemObject(RectTransform parent, int index)
    {
        GameObject root = new GameObject($"EnemyItem_{index}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        root.transform.SetParent(parent, false);

        Image bgImg = root.GetComponent<Image>();
        Transform sampleBtnImg = transform.Find("Group_ChangeTilePanel/Img_A_ChangeTilePanel_BG 128*128");
        if (sampleBtnImg != null && sampleBtnImg.TryGetComponent(out Image sImg))
        {
            bgImg.sprite = sImg.sprite;
            bgImg.type = sImg.type;
        }
        bgImg.color = new Color(0.18f, 0.22f, 0.28f, 0.95f);

        // Frame
        GameObject frame = new GameObject("Frame", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
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

        // Icon
        GameObject iconObj = new GameObject("Img_Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        iconObj.transform.SetParent(root.transform, false);
        RectTransform iconRect = iconObj.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0f, 0.5f);
        iconRect.anchorMax = new Vector2(0f, 0.5f);
        iconRect.pivot = new Vector2(0.5f, 0.5f);
        iconRect.anchoredPosition = new Vector2(40f, 0f);
        iconRect.sizeDelta = new Vector2(50f, 50f);
        Image iconImg = iconObj.GetComponent<Image>();
        iconImg.preserveAspect = true;

        // Name
        GameObject nameObj = new GameObject("Txt_Name", typeof(RectTransform), typeof(TextMeshProUGUI));
        nameObj.transform.SetParent(root.transform, false);
        RectTransform nameRect = nameObj.GetComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0f, 0.5f);
        nameRect.anchorMax = new Vector2(1f, 0.5f);
        nameRect.pivot = new Vector2(0f, 0.5f);
        nameRect.anchoredPosition = new Vector2(80f, 12f);
        nameRect.sizeDelta = new Vector2(-120f, 26f);
        TextMeshProUGUI nameTxt = nameObj.GetComponent<TextMeshProUGUI>();
        if (m_cachedFont != null) nameTxt.font = m_cachedFont;
        nameTxt.fontSize = 17;
        nameTxt.fontStyle = FontStyles.Bold;
        nameTxt.color = Color.white;

        // HP Text
        GameObject hpObj = new GameObject("Txt_HP", typeof(RectTransform), typeof(TextMeshProUGUI));
        hpObj.transform.SetParent(root.transform, false);
        RectTransform hpRect = hpObj.GetComponent<RectTransform>();
        hpRect.anchorMin = new Vector2(0f, 0.5f);
        hpRect.anchorMax = new Vector2(1f, 0.5f);
        hpRect.pivot = new Vector2(0f, 0.5f);
        hpRect.anchoredPosition = new Vector2(80f, -14f);
        hpRect.sizeDelta = new Vector2(-120f, 22f);
        TextMeshProUGUI hpTxt = hpObj.GetComponent<TextMeshProUGUI>();
        if (m_cachedFont != null) hpTxt.font = m_cachedFont;
        hpTxt.fontSize = 14;
        hpTxt.color = new Color(1f, 0.6f, 0.6f);

        // Arrow
        GameObject arrowObj = new GameObject("Txt_Arrow", typeof(RectTransform), typeof(TextMeshProUGUI));
        arrowObj.transform.SetParent(root.transform, false);
        RectTransform arrowRect = arrowObj.GetComponent<RectTransform>();
        arrowRect.anchorMin = new Vector2(1f, 0.5f);
        arrowRect.anchorMax = new Vector2(1f, 0.5f);
        arrowRect.pivot = new Vector2(1f, 0.5f);
        arrowRect.anchoredPosition = new Vector2(-15f, 0f);
        arrowRect.sizeDelta = new Vector2(25f, 30f);
        TextMeshProUGUI arrowTxt = arrowObj.GetComponent<TextMeshProUGUI>();
        if (m_cachedFont != null) arrowTxt.font = m_cachedFont;
        arrowTxt.fontSize = 18;
        arrowTxt.alignment = TextAlignmentOptions.Center;
        arrowTxt.color = new Color(0.8f, 0.8f, 0.8f);
        arrowTxt.text = "▶";

        return root;
    }

    #endregion

    #region Enemy Detail Handling

    public void SelectEnemy(EnemyHealthController enemy)
    {
        if (enemy == null || !enemy.gameObject.activeInHierarchy || enemy.CurrentHP <= 0f) return;

        m_selectedEnemy = enemy;
        ShowDetailView();

        UpdateStatsText();
        RefreshBuffEntries();
        RefreshDebuffEntries();
    }

    private void UpdateStatsText()
    {
        if (m_selectedEnemy == null) return;

        string enemyName = m_selectedEnemy.EnemyData != null
            ? m_selectedEnemy.EnemyData.DisplayName
            : m_selectedEnemy.gameObject.name;

        if (m_selectedEnemy.IsBoss) enemyName = $"[BOSS] {enemyName}";
        if (m_txtDetailTitle != null) m_txtDetailTitle.text = enemyName;

        // 체력
        if (m_txtHP != null)
        {
            int curHP = Mathf.CeilToInt(m_selectedEnemy.CurrentHP);
            int maxHP = Mathf.CeilToInt(m_selectedEnemy.MaxHP);
            m_txtHP.text = $"체력: {curHP} / {maxHP}";
        }

        // 방어력
        if (m_txtDefend != null)
        {
            float finalDefend = m_selectedEnemy.FinalDefend;
            float baseDefend = m_selectedEnemy.BaseDefend;
            if (Mathf.Approximately(finalDefend, baseDefend))
            {
                m_txtDefend.text = $"방어력: {finalDefend:0.#}";
            }
            else
            {
                m_txtDefend.text = $"방어력: {finalDefend:0.#} (기본 {baseDefend:0.#})";
            }
        }

        // 행동력 및 이동력
        if (m_selectedEnemy.TryGetComponent(out EnemyMoveController movement))
        {
            if (m_txtAction != null)
            {
                m_txtAction.text = $"행동력: {movement.ActionInterval}턴 주기 ({movement.CurrentActionCounter}/{movement.ActionInterval})";
            }

            if (m_txtSpeed != null)
            {
                int currentMax = movement.CurrentDiceMaxSpeed;
                int baseMax = movement.BaseDiceMaxSpeed;
                if (currentMax == baseMax)
                {
                    m_txtSpeed.text = $"이동력: 주사위 1 ~ {currentMax}칸";
                }
                else
                {
                    m_txtSpeed.text = $"이동력: 주사위 1 ~ {currentMax}칸 (기본 {baseMax})";
                }
            }
        }

        // 저항력
        if (m_txtResistance != null)
        {
            float res = m_selectedEnemy.EnemyData != null ? m_selectedEnemy.EnemyData.Resistance : 0f;
            m_txtResistance.text = $"저항력: {res:0.#}%";
        }
    }

    private void RefreshBuffEntries()
    {
        ClearEntries(m_spawnedBuffEntries);

        RectTransform content = m_buffContent;
        if (content == null && m_buffScrollRect != null) content = m_buffScrollRect.content;
        if (content == null) return;

        List<string> buffDescriptions = new List<string>();

        // 1. 현재 패스 타일 버프 확인
        if (TileManager.Instance != null)
        {
            SpecialTileType tileType = TileManager.Instance.GetTileTypeAt(m_currentPathCell);
            switch (tileType)
            {
                case SpecialTileType.SpeedTile:
                    buffDescriptions.Add("스피드 타일 효과\n행동주기 1 감소, 이동 주사위 최댓값 +2 증가");
                    break;
                case SpecialTileType.DefendTile:
                    buffDescriptions.Add("방어 타일 효과\n방어력 20% 증가");
                    break;
                case SpecialTileType.HealTile:
                    buffDescriptions.Add("치유 타일 효과\n체력 20% 즉시 회복");
                    break;
            }
        }

        if (buffDescriptions.Count == 0)
        {
            buffDescriptions.Add("적용 중인 버프 없음\n현재 적에게 적용된 버프가 없습니다.");
        }

        CreateEntries(content, buffDescriptions, m_spawnedBuffEntries);
        if (m_buffScrollRect != null) m_buffScrollRect.verticalNormalizedPosition = 1f;
    }

    private void RefreshDebuffEntries()
    {
        ClearEntries(m_spawnedDebuffEntries);

        RectTransform content = m_debuffContent;
        if (content == null && m_debuffScrollRect != null) content = m_debuffScrollRect.content;
        if (content == null) return;

        List<string> debuffDescriptions = new List<string>();

        if (m_selectedEnemy != null && m_selectedEnemy.TryGetComponent(out EnemyDebuffController debuffController))
        {
            debuffDescriptions.AddRange(debuffController.GetActiveDebuffDescriptions());
        }

        if (debuffDescriptions.Count == 0)
        {
            debuffDescriptions.Add("적용 중인 디버프 없음\n현재 적에게 적용된 디버프가 없습니다.");
        }

        CreateEntries(content, debuffDescriptions, m_spawnedDebuffEntries);
        if (m_debuffScrollRect != null) m_debuffScrollRect.verticalNormalizedPosition = 1f;
    }

    private void CreateEntries(RectTransform content, List<string> descriptions, List<GameObject> spawnedList)
    {
        float startY = -40f;
        float spacing = 75f;

        for (int i = 0; i < descriptions.Count; i++)
        {
            GameObject entry = null;
            if (m_buffEntryPrefab != null)
            {
                entry = Instantiate(m_buffEntryPrefab, content);
            }
            else
            {
                entry = CreateDefaultInfoEntry(content, i);
            }

            entry.SetActive(true);

            RectTransform rect = entry.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, startY - (i * spacing));
            rect.sizeDelta = new Vector2(330f, 70f);

            TextMeshProUGUI txt = entry.GetComponentInChildren<TextMeshProUGUI>(true);
            if (txt != null)
            {
                txt.text = descriptions[i];
            }

            spawnedList.Add(entry);
        }

        float totalHeight = Mathf.Max(content.rect.height, Mathf.Abs(startY) + (descriptions.Count * spacing) + 20f);
        content.sizeDelta = new Vector2(content.sizeDelta.x, totalHeight);
    }

    private GameObject CreateDefaultInfoEntry(RectTransform parent, int index)
    {
        GameObject root = new GameObject($"InfoEntry_{index}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        root.transform.SetParent(parent, false);

        Image bg = root.GetComponent<Image>();
        Transform sampleBtnImg = transform.Find("Group_ChangeTilePanel/Img_A_ChangeTilePanel_BG 128*128");
        if (sampleBtnImg != null && sampleBtnImg.TryGetComponent(out Image sImg))
        {
            bg.sprite = sImg.sprite;
            bg.type = sImg.type;
        }
        bg.color = new Color(0.18f, 0.22f, 0.28f, 0.95f);

        GameObject txtObj = new GameObject("Txt_Desc", typeof(RectTransform), typeof(TextMeshProUGUI));
        txtObj.transform.SetParent(root.transform, false);
        RectTransform txtRect = txtObj.GetComponent<RectTransform>();
        txtRect.anchorMin = Vector2.zero;
        txtRect.anchorMax = Vector2.one;
        txtRect.sizeDelta = new Vector2(-20f, -10f);

        TextMeshProUGUI txt = txtObj.GetComponent<TextMeshProUGUI>();
        if (m_cachedFont != null) txt.font = m_cachedFont;
        txt.fontSize = 14;
        txt.alignment = TextAlignmentOptions.Center;
        txt.color = Color.white;

        return root;
    }

    private void ClearEntries(List<GameObject> entries)
    {
        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i] != null)
            {
                Destroy(entries[i]);
            }
        }
        entries.Clear();
    }

    public void BackToList()
    {
        m_selectedEnemy = null;
        RefreshListEntries();
        ShowListView();
    }

    public void Hide()
    {
        m_selectedEnemy = null;
        ClearEntries(m_spawnedListEntries);
        ClearEntries(m_spawnedBuffEntries);
        ClearEntries(m_spawnedDebuffEntries);
        gameObject.SetActive(false);
    }

    #endregion
}
