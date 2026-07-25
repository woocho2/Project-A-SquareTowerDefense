using UnityEngine;

public class EnemyUIController : MonoBehaviour
{
    [SerializeField] HPBarSlider m_HPBarSlider;

    private void Awake()
    {
        if (m_HPBarSlider == null)
        {
            m_HPBarSlider = GetComponentInChildren<HPBarSlider>();
        }        
    }


    public void SetHPBar(float curHP, float maxHP)
    {
        if (m_HPBarSlider != null)
        {
            m_HPBarSlider.SetHP(curHP, maxHP);
        }
    }
}
