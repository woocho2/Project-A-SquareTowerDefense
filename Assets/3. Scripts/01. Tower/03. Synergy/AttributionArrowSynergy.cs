#if false // Synergy system temporarily disabled
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class AttributionArrowSynergy : SynergyBase
{
    private Button m_btnAttribution;
    private Image m_imgAttributionBow;
    private Image m_imgAttributionFire;
    private Image m_imgAttributionIce;

    public AttributionArrowSynergy(TowerManager manager, Button btn, Image img1, Image img2, Image img3) : base(manager)
    {
        m_btnAttribution     = btn;
        m_imgAttributionBow  = img1;
        m_imgAttributionFire = img2;
        m_imgAttributionIce  = img3;

        Color inactiveColor = new Color(0.3f, 0.3f, 0.3f, 1f);
        if (m_btnAttribution     != null) m_btnAttribution.image.color = inactiveColor;
        if (m_imgAttributionBow  != null) m_imgAttributionBow.color    = inactiveColor;
        if (m_imgAttributionFire != null) m_imgAttributionFire.color   = inactiveColor;
        if (m_imgAttributionIce  != null) m_imgAttributionIce.color    = inactiveColor;

        if (m_imgAttributionBow  != null) m_imgAttributionBow.gameObject.SetActive(false);
        if (m_imgAttributionFire != null) m_imgAttributionFire.gameObject.SetActive(false);
        if (m_imgAttributionIce  != null) m_imgAttributionIce.gameObject.SetActive(false);

        if (m_btnAttribution != null)
        {
            m_btnAttribution.onClick.AddListener(() =>
            {
                bool isVisible = m_imgAttributionBow.gameObject.activeSelf;
                bool isOpening = !isVisible;

                if (m_imgAttributionBow  != null) m_imgAttributionBow.gameObject.SetActive(isOpening);
                if (m_imgAttributionFire != null) m_imgAttributionFire.gameObject.SetActive(isOpening);
                if (m_imgAttributionIce  != null) m_imgAttributionIce.gameObject.SetActive(isOpening);
                m_manager.HandleSynergyButtonToggle(m_btnAttribution, isOpening);
            });
        }
    }

    public override bool CheckCondition(Dictionary<Vector3Int, TowerManager.GridTowerInfo> towersOnGrid)
    {
        HashSet<int> highTierVariants = m_manager.GetActiveVariants();

        bool hasBow  = highTierVariants.Contains(2);
        bool hasFire = highTierVariants.Contains(6);
        bool hasIce  = highTierVariants.Contains(7);

        Color activeColor = Color.white;
        Color inactiveColor = new Color(0.3f, 0.3f, 0.3f, 1f);

        if (m_imgAttributionBow  != null) m_imgAttributionBow.color  = hasBow  ? activeColor : inactiveColor;
        if (m_imgAttributionFire != null) m_imgAttributionFire.color = hasFire ? activeColor : inactiveColor;
        if (m_imgAttributionIce  != null) m_imgAttributionIce.color  = hasIce  ? activeColor : inactiveColor;

        bool isSynergyActive = hasBow && hasFire && hasIce;

        if (m_btnAttribution != null && m_btnAttribution.image != null) m_btnAttribution.image.color = isSynergyActive ? activeColor : inactiveColor;
 
        return isSynergyActive;
    }

    public override void Activate()
    {
        m_manager.IsAttributionArrowActive = true;
        IsActive = true;
    }

    public override void Deactivate()
    {
        m_manager.IsAttributionArrowActive = false;
        IsActive = false;
    }
}
#endif
