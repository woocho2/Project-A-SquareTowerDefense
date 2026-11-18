using UnityEngine;
using UnityEngine.UI;

namespace SquareTD
{
    // 활성 버튼과 토글의 실제 클릭에 붙이는 공통 UI 효과음
    public sealed class AudioButtonFeedback : MonoBehaviour
    {
        private Button m_button;
        private Toggle m_toggle;
        private void OnEnable()
        {
            m_button = GetComponent<Button>(); m_toggle = GetComponent<Toggle>();
            if (m_button != null) m_button.onClick.AddListener(Click);
            if (m_toggle != null) m_toggle.onValueChanged.AddListener(ToggleChanged);
        }
        private void Click()
        {
            if (SoundManager.Instance == null) return;
            string key = name.ToLowerInvariant();
            SoundManager.Instance.Cue(key.Contains("select") || key.Contains("start") || key.Contains("confirm") ? SoundCue.Confirm : key.Contains("back") || key.Contains("close") || key.Contains("resume") || key.Contains("menu") ? SoundCue.Back : SoundCue.Click);
        }
        private void ToggleChanged(bool value) => Click();
        private void OnDisable()
        {
            if (m_button != null) m_button.onClick.RemoveListener(Click);
            if (m_toggle != null) m_toggle.onValueChanged.RemoveListener(ToggleChanged);
        }
    }
}
