using UnityEngine;

/// <summary>타워 선택 하이라이트의 표시만 담당합니다. 입력 판정은 BoardInputController가 합니다.</summary>
public class TowerSelectionVisual : MonoBehaviour
{
    [SerializeField] private GameObject m_highlight;

    private void Awake()
    {
        if (m_highlight != null) m_highlight.SetActive(false);
    }

    public void SetHighlighted(bool highlighted)
    {
        if (m_highlight != null) m_highlight.SetActive(highlighted);
    }
}
