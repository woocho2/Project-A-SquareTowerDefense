#if false // Synergy system temporarily disabled
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class KnightOfFireSynergy : SynergyBase
{
    private TowerController m_fireofknight;
    private TowerData m_knightData;
    private Button m_btnKnightOfFire;
    private Image m_imgKnightOfFireSword;
    private Image m_imgKnightOfFireShield;
    private Image m_imgKnightOfFireFire;

    public KnightOfFireSynergy(TowerManager manager, TowerData data, Button btn, Image img1, Image img2, Image img3) : base(manager)
    {
        m_knightData            = data;
        m_btnKnightOfFire       = btn;
        m_imgKnightOfFireSword  = img1;
        m_imgKnightOfFireShield = img2;
        m_imgKnightOfFireFire   = img3;

        Color inactiveColor = new Color(0.3f, 0.3f, 0.3f, 1f);
        if (m_btnKnightOfFire        != null) m_btnKnightOfFire.image.color = inactiveColor;    
        if (m_imgKnightOfFireSword   != null) m_imgKnightOfFireSword.color  = inactiveColor;
        if (m_imgKnightOfFireShield  != null) m_imgKnightOfFireShield.color = inactiveColor;
        if (m_imgKnightOfFireFire    != null) m_imgKnightOfFireFire.color   = inactiveColor;

        if (m_imgKnightOfFireSword  != null) m_imgKnightOfFireSword.gameObject.SetActive(false);
        if (m_imgKnightOfFireShield != null) m_imgKnightOfFireShield.gameObject.SetActive(false);
        if (m_imgKnightOfFireFire   != null) m_imgKnightOfFireFire.gameObject.SetActive(false);

        if (m_btnKnightOfFire != null)
        {
            m_btnKnightOfFire.onClick.AddListener(() =>
            {
                bool isVisible = m_imgKnightOfFireSword.gameObject.activeSelf;
                bool isOpening = !isVisible;

                if (m_imgKnightOfFireSword  != null) m_imgKnightOfFireSword.gameObject.SetActive(isOpening);
                if (m_imgKnightOfFireShield != null) m_imgKnightOfFireShield.gameObject.SetActive(isOpening);
                if (m_imgKnightOfFireFire   != null) m_imgKnightOfFireFire.gameObject.SetActive(isOpening);
                m_manager.HandleSynergyButtonToggle(m_btnKnightOfFire, isOpening);
            });
        }
    }

    public override bool CheckCondition(Dictionary<Vector3Int, TowerManager.GridTowerInfo> towersOnGrid)
    {
        HashSet<int> highTierVariants = m_manager.GetActiveVariants();

        bool hasSword  = highTierVariants.Contains(1);
        bool hasShield = highTierVariants.Contains(3);
        bool hasFire   = highTierVariants.Contains(6);

        Color activeColor = Color.white;
        Color inactiveColor = new Color(0.3f, 0.3f, 0.3f, 1f);

        if (m_imgKnightOfFireSword   != null) m_imgKnightOfFireSword.color   = hasSword  ? activeColor : inactiveColor;
        if (m_imgKnightOfFireShield  != null) m_imgKnightOfFireShield.color  = hasShield ? activeColor : inactiveColor;
        if (m_imgKnightOfFireFire    != null) m_imgKnightOfFireFire.color    = hasFire   ? activeColor : inactiveColor;

        bool isSynergyActive = hasSword && hasShield && hasFire;

        if (m_btnKnightOfFire != null && m_btnKnightOfFire.image != null) m_btnKnightOfFire.image.color = isSynergyActive ? activeColor : inactiveColor;

        return isSynergyActive;
    }

    public override void Activate()
    {
        m_fireofknight = m_manager.CreateSynergyTower(m_knightData);
        IsActive = true;
    }

    public override void Deactivate()
    {
        if (m_fireofknight != null)
        {
            m_fireofknight = m_manager.DestroySynergyTower(m_fireofknight);
        }
        IsActive = false;
    }
}

#endif
