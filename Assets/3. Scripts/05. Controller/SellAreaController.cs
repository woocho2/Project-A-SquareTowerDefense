using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class SellAreaController : MonoBehaviour
{
    [SerializeField] private bool m_hideOnStart = true;

    [SerializeField] private Color m_highlightColor = Color.red;
    private Color m_originalColor;

    private Collider2D m_collider2D;
    private Camera m_mainCamera;
    private Image m_uiIamage;
    private TextMeshProUGUI m_uiText;

    private void Awake()
    {
        m_uiIamage = GetComponent<Image>();
        m_collider2D = GetComponent<Collider2D>();
        m_uiText = GetComponentInChildren<TextMeshProUGUI>();

        m_mainCamera = Camera.main;

        if (m_uiIamage != null)
        {
            m_originalColor = m_uiIamage.color;
        }

        if (m_hideOnStart)
        {
            ToggleSellArea(false);
        }
    }

    private void OnEnable()
    {
        DragObjectOnGround.OnTowerDragStateChanged += ToggleSellArea;
    }

    private void OnDisable()
    {
        DragObjectOnGround.OnTowerDragStateChanged -= ToggleSellArea;
    }

    private void Update()
    {
        if (m_uiIamage.enabled && Mouse.current != null && m_mainCamera != null)
        {
            Vector2 mouseScreenPosition = Mouse.current.position.ReadValue();
            Vector3 worldPosition = m_mainCamera.ScreenToWorldPoint(new Vector3(mouseScreenPosition.x, mouseScreenPosition.y, Mathf.Abs(m_mainCamera.transform.position.z)));

            if (m_collider2D.OverlapPoint(worldPosition))
            {
                m_uiIamage.color = m_highlightColor;
            }
            else
            {
                m_uiIamage.color = m_originalColor;
            }
        }
    }

    private void ToggleSellArea(bool isDragging)
    {
        if (m_uiIamage   != null) m_uiIamage.enabled   = isDragging;
        if (m_uiText     != null) m_uiText.enabled     = isDragging;
        if (m_collider2D != null) m_collider2D.enabled = isDragging;
    }
}