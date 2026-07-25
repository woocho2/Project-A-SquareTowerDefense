using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StormSniperSynergy : SynergyBase
{
    private TowerController m_stormsniper;
    private TowerData m_sniperData;
    private Button m_btnStromSniper;
    private Image m_imgStromSniperBow;
    private Image m_imgStromSniperWind;

    public StormSniperSynergy(TowerManager manager, TowerData data, Button btn, Image img1, Image img2) : base(manager)
    {
        m_sniperData         = data;
        m_btnStromSniper     = btn;
        m_imgStromSniperBow  = img1;
        m_imgStromSniperWind = img2;

        Color inactiveColor = new Color(0.3f, 0.3f, 0.3f, 1f);
        if (m_btnStromSniper     != null) m_btnStromSniper.image.color = inactiveColor;
        if (m_imgStromSniperBow  != null) m_imgStromSniperBow.color    = inactiveColor;
        if (m_imgStromSniperWind != null) m_imgStromSniperWind.color   = inactiveColor;

        if (m_imgStromSniperBow  != null) m_imgStromSniperBow.gameObject.SetActive(false);
        if (m_imgStromSniperWind != null) m_imgStromSniperWind.gameObject.SetActive(false);

        if (m_btnStromSniper != null)
        {
            m_btnStromSniper.onClick.AddListener(() =>
            {
                bool isVisible = m_imgStromSniperBow.gameObject.activeSelf;
                bool isOpening = !isVisible;

                if (m_imgStromSniperBow  != null) m_imgStromSniperBow.gameObject.SetActive(isOpening);
                if (m_imgStromSniperWind != null) m_imgStromSniperWind.gameObject.SetActive(isOpening);
                m_manager.HandleSynergyButtonToggle(m_btnStromSniper, isOpening);
            });
        }
    }

    public override bool CheckCondition(Dictionary<Vector3Int, TowerManager.GridTowerInfo> towersOnGrid)
    {
        HashSet<int> highTierVariants = m_manager.GetActiveVariants();

        bool hasBow  = highTierVariants.Contains(2);
        bool hasWind = highTierVariants.Contains(9);

        Color activeColor   = Color.white;
        Color inactiveColor = new Color(0.3f, 0.3f, 0.3f, 1f);

        if (m_imgStromSniperBow  != null) m_imgStromSniperBow.color  = hasBow  ? activeColor : inactiveColor;
        if (m_imgStromSniperWind != null) m_imgStromSniperWind.color = hasWind ? activeColor : inactiveColor;

        bool isSynergyActive = hasBow && hasWind;

        if (m_btnStromSniper != null && m_btnStromSniper.image != null) m_btnStromSniper.image.color = isSynergyActive ? activeColor : inactiveColor;

        return isSynergyActive;
    }

    public override void Activate()
    {
        m_stormsniper = m_manager.CreateSynergyTower(m_sniperData);
        IsActive = true;
    }

    public override void Deactivate()
    {
        if (m_stormsniper != null)
        {
            m_stormsniper = m_manager.DestroySynergyTower(m_stormsniper);
        }
        IsActive = false;
    }
}
