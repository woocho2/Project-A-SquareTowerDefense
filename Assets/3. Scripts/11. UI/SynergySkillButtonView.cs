using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SquareTD
{
    // 월드 공간 버튼의 프레임·충전 바·준비 표시. 모양과 크기는 UI 프리팹에서 조절함.
    [RequireComponent(typeof(Canvas))]
    public sealed class SynergySkillButtonView : MonoBehaviour
    {
        [SerializeField] private Button m_button;
        [SerializeField] private Image m_fill;
        [SerializeField] private Image m_readyGlow;
        [SerializeField] private TextMeshProUGUI m_label;
        [SerializeField] private Color m_chargingColor = new Color(0.24f, 0.39f, 0.68f, 1f);
        [SerializeField] private Color m_readyColor = new Color(0.95f, 0.77f, 0.26f, 1f);

        private Tween m_pulse;
        private Tween m_click;
        private bool m_wasReady;

        public Button ActionButton => m_button;
        public float FillAmount => m_fill.fillAmount;
        public string Label => m_label.text;

        public void Render(float ratio, bool ready, bool interactable, bool selecting, string label)
        {
            m_button.interactable = interactable;
            m_fill.fillAmount = ready ? 1f : Mathf.Clamp01(ratio);
            m_fill.color = ready ? m_readyColor : m_chargingColor;
            m_label.text = label;
            m_label.color = ready ? new Color(0.07f, 0.10f, 0.22f, 1f) : Color.white;
            if (ready != m_wasReady)
            {
                m_wasReady = ready;
                m_pulse?.Kill();
                m_readyGlow.color = new Color(1f, 0.89f, 0.44f, ready ? 0.08f : 0f);
                if (ready && Application.isPlaying)
                    m_pulse = m_readyGlow.DOFade(0.28f, 0.7f).SetLoops(-1, LoopType.Yoyo).SetUpdate(true).SetLink(gameObject);
            }
            if (selecting) m_label.color = Color.white;
        }

        public void PlayUsed()
        {
            m_click?.Kill();
            m_button.transform.localScale = Vector3.one;
            m_click = m_button.transform.DOPunchScale(Vector3.one * 0.07f, 0.2f, 3, 0.5f).SetUpdate(true).SetLink(gameObject);
        }

        private void OnDisable()
        {
            m_pulse?.Kill();
            m_click?.Kill();
            m_wasReady = false;
            if (m_readyGlow != null) m_readyGlow.color = new Color(1f, 0.89f, 0.44f, 0f);
            if (m_button != null) m_button.transform.localScale = Vector3.one;
        }
    }
}
