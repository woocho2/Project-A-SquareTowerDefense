using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SquareTD
{
    // 기본 효과와 스킬 효과를 구분하고 선택한 수치의 계산을 표시
    public sealed class SkillDescriptionView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI m_basicTitle, m_basicText, m_skillTitle, m_skillText, m_activation, m_hint;
        [SerializeField] private GameObject m_details;
        [SerializeField] private TextMeshProUGUI m_valueTitle, m_valueText;
        [SerializeField] private ScrollRect m_valueScroll;
        [SerializeField] private Button m_close;
        private TowerDescription m_document;
        private string m_openKey;
        private bool m_bound;

        public TowerDescription Document => m_document;
        public bool IsValueOpen => m_details != null && m_details.activeInHierarchy;
        public string ValueText => m_valueText != null ? m_valueText.text : "";

        private void Bind()
        {
            if (m_bound) return;
            if (m_close != null) m_close.onClick.AddListener(HideValue);
            m_bound = true;
        }

        public void SetDocument(TowerDescription document)
        {
            Bind();
            if (m_document == null || m_document.TowerId != document.TowerId) HideValue();
            m_document = document;
            if (m_basicTitle != null) m_basicTitle.text = document.BasicTitle;
            if (m_basicText != null) m_basicText.text = document.Basic;
            if (m_skillTitle != null) m_skillTitle.text = document.SkillTitle;
            if (m_skillText != null) m_skillText.text = document.Skill;
            if (m_activation != null) m_activation.text = document.Activation;
            if (m_hint != null) m_hint.gameObject.SetActive(document.Values.Count > 0);
            if (m_openKey == null) return;
            if (!document.Values.TryGetValue(m_openKey, out TowerDescription.Value value)) HideValue();
            else SetValue(value);
        }

        public void ShowValue(string key)
        {
            if (m_document == null || !m_document.Values.TryGetValue(key, out TowerDescription.Value value) || m_details == null) return;
            Bind();
            m_openKey = key;
            m_details.SetActive(true);
            m_details.transform.SetAsLastSibling();
            SetValue(value);
            CanvasGroup group = m_details.GetComponent<CanvasGroup>();
            if (group == null) group = m_details.AddComponent<CanvasGroup>();
            group.DOKill(); group.alpha = 0;
            group.DOFade(1, .12f).SetUpdate(true);
        }

        private void SetValue(TowerDescription.Value value)
        {
            if (m_valueTitle != null) m_valueTitle.text = value.Title;
            if (m_valueText != null) m_valueText.text = value.Explanation;
            if (m_valueScroll != null) { Canvas.ForceUpdateCanvases(); m_valueScroll.verticalNormalizedPosition = 1; }
        }

        public void HideValue()
        {
            m_openKey = null;
            if (m_details == null) return;
            CanvasGroup group = m_details.GetComponent<CanvasGroup>();
            if (group != null) group.DOKill();
            m_details.SetActive(false);
        }

        public void PreparePage()
        {
            HideValue();
            ScrollRect effects = m_basicText != null ? m_basicText.GetComponentInParent<ScrollRect>(true) : null;
            if (effects != null) { effects.content.anchoredPosition = new Vector2(effects.content.anchoredPosition.x, 0); effects.verticalNormalizedPosition = 1; }
        }

        private void OnDisable() => HideValue();
        private void OnDestroy() { if (m_close != null) m_close.onClick.RemoveListener(HideValue); }
    }
}
