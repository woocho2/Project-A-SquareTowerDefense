using UnityEngine;
using UnityEngine.UI;

public class HPBarSlider : MonoBehaviour
{
    [SerializeField] private Color m_targetColor;

    [SerializeField] Image m_gauge;


    private void Awake()
    {
        if (m_gauge == null)
        {
            m_gauge = GetComponentInChildren<Image>();
        }
    }

    public void SetHP(float curHP, float maxHP)
    {
        if (m_gauge != null)
        {
            m_gauge.color = m_targetColor;
        }

        if (maxHP <= 0)
        {
            Debug.LogWarning("MaxHP must be greater than 0.");
            return;
        }
        float fillAmount = (float)curHP / maxHP;

        if (fillAmount > 0.66f)
        {
            m_targetColor = Color.green;
        }
        else if (fillAmount > 0.33f && fillAmount <= 0.66f)
        {
            m_targetColor = Color.yellow;
        }
        else
        {
            m_targetColor = Color.red;
        }

        if (m_gauge != null)
        {
            m_gauge.color = m_targetColor;
            m_gauge.fillAmount = fillAmount;
        }

        m_gauge.fillAmount = Mathf.Clamp01(fillAmount);
    }
}

