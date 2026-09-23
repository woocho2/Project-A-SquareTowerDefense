using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// EnemyInfoPanel의 적 목록(View_EnemyList)에 표시되는 개별 적 항목 UI입니다.
/// </summary>
public class EnemyListItemUI : MonoBehaviour
{
    [SerializeField] private Image m_icon;
    [SerializeField] private TextMeshProUGUI m_txtName;
    [SerializeField] private TextMeshProUGUI m_txtHP;
    [SerializeField] private Button m_button;

    private EnemyHealthController m_enemy;
    private Action<EnemyHealthController> m_onSelected;

    private void Awake()
    {
        if (m_button == null) m_button = GetComponent<Button>();
        if (m_button != null)
        {
            m_button.onClick.RemoveAllListeners();
            m_button.onClick.AddListener(OnClickItem);
        }
    }

    public void Init(EnemyHealthController enemy, Action<EnemyHealthController> onSelected)
    {
        m_enemy = enemy;
        m_onSelected = onSelected;

        RefreshDisplay();
    }

    public void RefreshDisplay()
    {
        if (m_enemy == null || !m_enemy.gameObject.activeInHierarchy || m_enemy.CurrentHP <= 0f)
        {
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);

        // 이름 표기
        string enemyName = m_enemy.EnemyData != null
            ? m_enemy.EnemyData.DisplayName
            : m_enemy.gameObject.name;

        if (m_enemy.IsBoss)
        {
            enemyName = $"[BOSS] {enemyName}";
        }

        if (m_txtName != null)
        {
            m_txtName.text = enemyName;
        }

        // 체력 표기
        if (m_txtHP != null)
        {
            int curHP = Mathf.CeilToInt(m_enemy.CurrentHP);
            int maxHP = Mathf.CeilToInt(m_enemy.MaxHP);
            m_txtHP.text = $"체력: {curHP} / {maxHP}";
        }

        // 아이콘 스프라이트 표기
        if (m_icon != null)
        {
            if (m_enemy.TryGetComponent(out SpriteRenderer sr) && sr.sprite != null)
            {
                m_icon.sprite = sr.sprite;
                m_icon.enabled = true;
            }
            else
            {
                m_icon.enabled = false;
            }
        }
    }

    private void OnClickItem()
    {
        if (m_enemy != null && m_enemy.gameObject.activeInHierarchy && m_enemy.CurrentHP > 0f)
        {
            m_onSelected?.Invoke(m_enemy);
        }
    }
}
