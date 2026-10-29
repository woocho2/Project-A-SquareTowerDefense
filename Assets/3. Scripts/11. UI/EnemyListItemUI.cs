using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SquareTD
{
    // 적 목록의 한 줄
    public class EnemyListItemUI : MonoBehaviour
    {
        [SerializeField] private Image m_icon;
        [SerializeField] private TextMeshProUGUI m_txtName;
        [SerializeField] private TextMeshProUGUI m_txtHP;
        [SerializeField] private Button m_button;

        private ITarget m_target;
        private Action<ITarget> m_onSelected;

        public ITarget Target => m_target;

        private void Awake()
        {
            if (m_button == null) m_button = GetComponent<Button>();
            if (m_button != null)
            {
                m_button.onClick.RemoveAllListeners();
                m_button.onClick.AddListener(OnClickItem);
            }
        }

        public void Init(ITarget target, Action<ITarget> onSelected)
        {
            m_target = target;
            m_onSelected = onSelected;
            RefreshDisplay();
        }

        public void RefreshDisplay()
        {
            if (m_target == null || !m_target.IsAlive)
            {
                gameObject.SetActive(false);
                return;
            }

            gameObject.SetActive(true);

            if (m_txtName != null) m_txtName.text = GetDisplayName(m_target);

            if (m_txtHP != null)
                m_txtHP.text = $"체력: {Mathf.CeilToInt(m_target.CurrentHp)} / {Mathf.CeilToInt(m_target.MaxHp)}";

            if (m_icon != null)
            {
                SpriteRenderer sr = m_target.Transform.GetComponentInChildren<SpriteRenderer>(true);
                m_icon.sprite = sr != null ? sr.sprite : null;
                m_icon.enabled = m_icon.sprite != null;
            }
        }

        public static string GetDisplayName(ITarget target)
        {
            EnemyController body = target.Body;
            string name = body != null && body.Definition != null ? body.Definition.DisplayName : target.Transform.name;

            if (target.IsDummy) name = $"[잔상] {name}";
            else if (target.IsBoss) name = $"[BOSS] {name}";
            return name;
        }

        private void OnClickItem()
        {
            if (m_target != null && m_target.IsAlive) m_onSelected?.Invoke(m_target);
        }
    }
}
