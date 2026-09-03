#if false // Synergy system temporarily disabled
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FrostBerserkerSynergy : SynergyBase
{
    private TowerController m_frostberserker;
    private TowerData m_berserkerData;
    private Button m_btnFrostBerserker;
    private Image m_imgFrostBerserkerShield;
    private Image m_imgFrostBerserkerAxe;
    private Image m_imgFrostBerserkerIce;

    public FrostBerserkerSynergy(TowerManager manager, TowerData data, Button btn, Image img1, Image img2, Image img3) : base(manager)
    {
        m_berserkerData           = data;
        m_btnFrostBerserker       = btn;
        m_imgFrostBerserkerShield = img1;
        m_imgFrostBerserkerAxe    = img2;
        m_imgFrostBerserkerIce    = img3;

        Color inactiveColor = new Color(0.3f, 0.3f, 0.3f, 1f);
        if (m_btnFrostBerserker       != null) m_btnFrostBerserker.image.color = inactiveColor;
        if (m_imgFrostBerserkerShield != null) m_imgFrostBerserkerShield.color = inactiveColor;
        if (m_imgFrostBerserkerAxe    != null) m_imgFrostBerserkerAxe.color    = inactiveColor;
        if (m_imgFrostBerserkerIce    != null) m_imgFrostBerserkerIce.color    = inactiveColor;

        if (m_imgFrostBerserkerShield != null) m_imgFrostBerserkerShield.gameObject.SetActive(false);
        if (m_imgFrostBerserkerAxe    != null) m_imgFrostBerserkerAxe.gameObject.SetActive(false);
        if (m_imgFrostBerserkerIce    != null) m_imgFrostBerserkerIce.gameObject.SetActive(false);

        if (m_btnFrostBerserker != null)
        {
            m_btnFrostBerserker.onClick.AddListener(() =>
            {
                bool isVisible = m_imgFrostBerserkerShield.gameObject.activeSelf;
                bool isOpening = !isVisible;

                if (m_imgFrostBerserkerShield != null) m_imgFrostBerserkerShield.gameObject.SetActive(isOpening);
                if (m_imgFrostBerserkerAxe    != null) m_imgFrostBerserkerAxe.gameObject.SetActive(isOpening);
                if (m_imgFrostBerserkerIce    != null) m_imgFrostBerserkerIce.gameObject.SetActive(isOpening);

                m_manager.HandleSynergyButtonToggle(m_btnFrostBerserker, isOpening);
            });
        }
    }

    public override bool CheckCondition(Dictionary<Vector3Int, TowerManager.GridTowerInfo> towersOnGrid)
    {
        HashSet<int> highTierVariants = m_manager.GetActiveVariants();

        bool hasShield = highTierVariants.Contains(3);
        bool hasAxe    = highTierVariants.Contains(5);
        bool hasIce    = highTierVariants.Contains(7);

        Color activeColor = Color.white;
        Color inactiveColor = new Color(0.3f, 0.3f, 0.3f, 1f);

        if (m_imgFrostBerserkerShield != null) m_imgFrostBerserkerShield.color = hasShield ? activeColor : inactiveColor;
        if (m_imgFrostBerserkerAxe    != null) m_imgFrostBerserkerAxe.color    = hasAxe    ? activeColor : inactiveColor;
        if (m_imgFrostBerserkerIce    != null) m_imgFrostBerserkerIce.color    = hasIce    ? activeColor : inactiveColor;

        bool isSynergyActive = hasShield && hasAxe && hasIce;

        if (m_btnFrostBerserker != null && m_btnFrostBerserker.image != null) m_btnFrostBerserker.image.color = isSynergyActive ? activeColor : inactiveColor;

        return isSynergyActive;
    }

    public override void Activate()
    {
        m_frostberserker = m_manager.CreateSynergyTower(m_berserkerData);
        IsActive = true;
    }

    public override void Deactivate()
    {
        if (m_frostberserker != null)
        {
            m_frostberserker = m_manager.DestroySynergyTower(m_frostberserker);
        }
        IsActive = false;
    }
}

#endif
