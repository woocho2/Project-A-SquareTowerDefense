using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SquareTD
{
    // 입장 문양 선택·선택 개수·실제 조합 순서에 맞춘 시너지 미리보기
    public class EmblemSelectPanel : MonoBehaviour
    {
        [Serializable]
        public sealed class SynergyRow
        {
            public GameObject Root;
            public Image Portrait;
            public TextMeshProUGUI Name, Ingredients, Match;
        }
        public const int NormalCount = 4;
        public const int HardCount = 6;
        [SerializeField] private Image[] img_emblems = new Image[14];
        [SerializeField] private Image[] img_slots = new Image[HardCount];
        [SerializeField] private Button[] m_choices = new Button[14], m_slotButtons = new Button[HardCount];
        [SerializeField] private Image[] m_borders = new Image[14];
        [SerializeField] private GameObject[] m_checks = new GameObject[14];
        [SerializeField] private TextMeshProUGUI[] m_slotNames = new TextMeshProUGUI[HardCount];
        [SerializeField] private TextMeshProUGUI txt_count, m_hint, m_stageTitle, m_synergyCount, m_emptySynergy, m_selectLabel;
        [SerializeField] private Button btn_emblemSelect, btn_back;
        [SerializeField] private SynergyCharacterCatalog m_synergyCatalog;
        [SerializeField] private SynergyRow[] m_synergyRows = Array.Empty<SynergyRow>();
        [SerializeField] private int[] m_stageSynergyCapacity = Array.Empty<int>();
        [SerializeField] private RectTransform m_window;
        private readonly List<TowerEmblem> m_selected = new List<TowerEmblem>();
        private List<SynergyRecipes.Recipe> m_preview = new List<SynergyRecipes.Recipe>();
        private StageDefinition m_stage;
        private GameMode m_mode;
        private bool m_initialized, m_starting;
        private static readonly Color Gold = new Color32(221, 190, 112, 255);
        private static readonly Color Border = new Color32(72, 85, 108, 255);
        public event Action BackRequested;
        public bool IsOpen => gameObject.activeSelf;
        public bool IsReady => m_selected.Count == RequiredCount;
        public bool IsStarting => m_starting;
        public int RequiredCount => GetCount(m_mode);
        public IReadOnlyList<TowerEmblem> Selected => m_selected;
        public IReadOnlyList<SynergyRecipes.Recipe> Preview => m_preview;
        public static int GetCount(GameMode mode) => mode == GameMode.Hard ? HardCount : NormalCount;
        private void Awake() => Initialize();
        private void Initialize()
        {
            if (m_initialized) return;
            m_initialized = true;
            for (int i = 0; i < m_choices.Length; i++)
            {
                TowerEmblem emblem = (TowerEmblem)(i + 1);
                if (m_choices[i] != null) m_choices[i].onClick.AddListener(() => ToggleEmblem(emblem));
            }
            for (int i = 0; i < m_slotButtons.Length; i++)
            {
                int index = i;
                if (m_slotButtons[i] != null) m_slotButtons[i].onClick.AddListener(() => RemoveAt(index));
            }
            if (btn_emblemSelect != null) btn_emblemSelect.onClick.AddListener(HandleStart);
            if (btn_back != null) btn_back.onClick.AddListener(HandleBack);
            Refresh();
        }
        public void Open(StageDefinition stage, GameMode mode)
        {
            Initialize(); m_stage = stage; m_starting = false;
            gameObject.SetActive(true); transform.SetAsLastSibling(); FitWindow(); SetMode(mode);
        }
        public void Close() { if (!m_starting) gameObject.SetActive(false); }
        public void SetStage(StageDefinition stage) { m_stage = stage; Refresh(); }
        public void SetMode(GameMode mode)
        {
            Initialize(); m_mode = mode;
            if (m_selected.Count > RequiredCount) m_selected.RemoveRange(RequiredCount, m_selected.Count - RequiredCount);
            Refresh();
        }
        public bool ToggleEmblem(TowerEmblem emblem)
        {
            Initialize();
            if (m_starting || (int)emblem < 1 || (int)emblem > 14) return false;
            int index = m_selected.IndexOf(emblem);
            if (index >= 0) { RemoveAt(index); return true; }
            if (m_selected.Count >= RequiredCount)
            {
                if (m_hint != null) m_hint.text = $"최대 {RequiredCount}개까지 선택할 수 있습니다. 선택한 문양을 눌러 교체해 주세요.";
                ShakeCount(); return false;
            }
            m_selected.Add(emblem); Refresh();
            RectTransform rect = img_emblems[(int)emblem - 1].rectTransform;
            rect.DOKill(true); rect.localScale = Vector3.one;
            rect.DOPunchScale(Vector3.one * .10f, .22f, 4, .5f).SetUpdate(true).SetLink(gameObject);
            return true;
        }
        public void RemoveAt(int index)
        {
            if (m_starting || index < 0 || index >= m_selected.Count) return;
            m_selected.RemoveAt(index); Refresh();
        }
        private void Refresh()
        {
            for (int i = 0; i < img_emblems.Length; i++)
            {
                bool selected = m_selected.Contains((TowerEmblem)(i + 1));
                if (img_emblems[i] != null) img_emblems[i].color = selected ? Gold : Color.white;
                if (i < m_borders.Length && m_borders[i] != null) m_borders[i].color = selected ? Gold : Border;
                if (i < m_checks.Length && m_checks[i] != null) m_checks[i].SetActive(selected);
                if (i < m_choices.Length && m_choices[i] != null) m_choices[i].interactable = !m_starting;
            }
            for (int i = 0; i < img_slots.Length; i++)
            {
                if (img_slots[i] == null) continue;
                bool filled = i < m_selected.Count;
                if (i < m_slotButtons.Length && m_slotButtons[i] != null)
                {
                    m_slotButtons[i].gameObject.SetActive(i < RequiredCount);
                    m_slotButtons[i].interactable = filled && !m_starting;
                }
                img_slots[i].sprite = filled ? img_emblems[(int)m_selected[i] - 1].sprite : null;
                img_slots[i].enabled = filled;
                img_slots[i].color = Gold;
                if (i < m_slotNames.Length && m_slotNames[i] != null) m_slotNames[i].text = filled ? EmblemNames.GetEmblemName(m_selected[i]) : "비어 있음";
            }
            if (txt_count != null) { txt_count.text = $"{m_selected.Count} / {RequiredCount} 선택"; txt_count.color = IsReady ? Gold : Color.white; }
            if (m_stageTitle != null) m_stageTitle.text = (m_stage != null ? m_stage.DisplayName : "스테이지") + (m_mode == GameMode.Hard ? "  ·  하드" : "  ·  노말");
            if (m_hint != null) m_hint.text = m_starting ? "로딩 화면으로 이동합니다" : IsReady ? "선택 완료 · SELECT를 눌러 입장하세요" : $"문양 {RequiredCount - m_selected.Count}개를 더 선택해 주세요";
            if (btn_emblemSelect != null) btn_emblemSelect.interactable = IsReady && !m_starting;
            if (btn_back != null) btn_back.interactable = !m_starting;
            if (m_selectLabel != null) m_selectLabel.text = m_starting ? "LOADING..." : "SELECT";
            RefreshSynergies();
        }
        private void RefreshSynergies()
        {
            int stageIndex = m_stage != null ? m_stage.Number - 1 : 0;
            int capacity = stageIndex >= 0 && stageIndex < m_stageSynergyCapacity.Length ? m_stageSynergyCapacity[stageIndex] : 6;
            m_preview = SynergyRecipes.Pick(m_selected, capacity);
            if (m_synergyCount != null) m_synergyCount.text = $"예상 시너지 타워  {m_preview.Count}기";
            if (m_emptySynergy != null)
            {
                m_emptySynergy.gameObject.SetActive(m_preview.Count == 0);
                m_emptySynergy.text = capacity == 0 ? "이 스테이지에는 시너지 칸이 없습니다." : "문양을 선택하면 함께 등장할\n시너지 타워를 확인할 수 있습니다.";
            }
            for (int i = 0; i < m_synergyRows.Length; i++)
            {
                SynergyRow row = m_synergyRows[i]; row.Root.SetActive(i < m_preview.Count);
                if (i >= m_preview.Count) continue;
                SynergyRecipes.Recipe recipe = m_preview[i];
                row.Name.text = recipe.Name;
                row.Portrait.sprite = m_synergyCatalog != null ? m_synergyCatalog.GetTowerCharacter(recipe.Id) : null;
                row.Portrait.enabled = row.Portrait.sprite != null;
                row.Match.text = $"{SynergyRecipes.Overlap(recipe, m_selected)} / 3";
                var ingredients = new List<string>();
                foreach (TowerEmblem emblem in recipe.Emblems)
                    ingredients.Add($"<color={(m_selected.Contains(emblem) ? "#DDBE70" : "#7D8A9F")}>{EmblemNames.GetEmblemName(emblem)}</color>");
                row.Ingredients.text = string.Join(" · ", ingredients);
            }
        }
        public bool TryStart()
        {
            if (m_starting || SceneFlow.PendingScene != null || m_stage == null) return false;
            if (!IsReady) { ShakeCount(); return false; }
            if (string.IsNullOrEmpty(m_stage.SceneName) || !Application.CanStreamedLevelBeLoaded(m_stage.SceneName))
            {
                if (m_hint != null) m_hint.text = "입장할 스테이지 씬을 확인해 주세요.";
                return false;
            }
            m_starting = true; Refresh();
            StartSettings.Set(m_mode, m_stage, m_selected);
            if (SceneFlow.LoadStage(m_stage)) return true;
            m_starting = false; Refresh(); return false;
        }
        private void HandleStart() => TryStart();
        private void OnRectTransformDimensionsChange() => FitWindow();
        private void FitWindow()
        {
            if (m_window == null) return;
            Rect area = ((RectTransform)transform).rect;
            float scale = Mathf.Min(area.width / 1640f, area.height / 1000f);
            if (scale > 0) m_window.localScale = Vector3.one * scale;
        }
        private void HandleBack() { if (m_starting) return; Close(); BackRequested?.Invoke(); }
        private void ShakeCount()
        {
            if (txt_count == null) return;
            txt_count.rectTransform.DOKill(true); txt_count.rectTransform.localScale = Vector3.one;
            txt_count.rectTransform.DOPunchScale(Vector3.one * .08f, .25f, 4, .5f).SetUpdate(true).SetLink(gameObject);
        }
        private void OnDestroy()
        {
            foreach (Button button in m_choices) if (button != null) button.onClick.RemoveAllListeners();
            foreach (Button button in m_slotButtons) if (button != null) button.onClick.RemoveAllListeners();
            if (btn_emblemSelect != null) btn_emblemSelect.onClick.RemoveListener(HandleStart);
            if (btn_back != null) btn_back.onClick.RemoveListener(HandleBack);
        }
    }
}
