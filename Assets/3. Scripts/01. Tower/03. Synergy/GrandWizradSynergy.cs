using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GrandWizardSynergy : SynergyBase
{
    private TowerController m_grandwizard;
    private TowerData m_wizardData;
    private Button m_btnGrandWizard;
    private Image m_imgGrandWizardFire;
    private Image m_imgGrandWizardIce;
    private Image m_imgGrandWizardElectricity;
    private Image m_imgGrandWizardWind;

    public GrandWizardSynergy(TowerManager manager, TowerData data, Button btn, Image img1, Image img2, Image img3, Image img4) : base(manager)
    {
        m_wizardData                = data;
        m_btnGrandWizard            = btn;
        m_imgGrandWizardFire        = img1;
        m_imgGrandWizardIce         = img2;
        m_imgGrandWizardElectricity = img3;
        m_imgGrandWizardWind        = img4;

        Color inactiveColor = new Color(0.3f, 0.3f, 0.3f, 1f);
        if (m_btnGrandWizard            != null) m_btnGrandWizard.image.color      = inactiveColor;
        if (m_imgGrandWizardFire        != null) m_imgGrandWizardFire.color        = inactiveColor;
        if (m_imgGrandWizardIce         != null) m_imgGrandWizardIce.color         = inactiveColor;
        if (m_imgGrandWizardElectricity != null) m_imgGrandWizardElectricity.color = inactiveColor;
        if (m_imgGrandWizardWind        != null) m_imgGrandWizardWind.color        = inactiveColor;

        if (m_imgGrandWizardFire        != null) m_imgGrandWizardFire.gameObject.SetActive(false);
        if (m_imgGrandWizardIce         != null) m_imgGrandWizardIce.gameObject.SetActive(false);
        if (m_imgGrandWizardElectricity != null) m_imgGrandWizardElectricity.gameObject.SetActive(false);
        if (m_imgGrandWizardWind        != null) m_imgGrandWizardWind.gameObject.SetActive(false);

        if (m_btnGrandWizard != null)
        {
            m_btnGrandWizard.onClick.AddListener(() =>
            {
                bool isVisible = m_imgGrandWizardFire.gameObject.activeSelf;
                bool isOpening = !isVisible;

                if (m_imgGrandWizardFire        != null) m_imgGrandWizardFire.gameObject.SetActive(isOpening);
                if (m_imgGrandWizardIce         != null) m_imgGrandWizardIce.gameObject.SetActive(isOpening);
                if (m_imgGrandWizardElectricity != null) m_imgGrandWizardElectricity.gameObject.SetActive(isOpening);
                if (m_imgGrandWizardWind        != null) m_imgGrandWizardWind.gameObject.SetActive(isOpening);
                m_manager.HandleSynergyButtonToggle(m_btnGrandWizard, isOpening);
            });
        }
    }

    public override bool CheckCondition(Dictionary<Vector3Int, TowerManager.GridTowerInfo> towersOnGrid)
    {
        HashSet<int> highTierVariants = m_manager.GetActiveVariants();

        bool hasFire        = highTierVariants.Contains(6);
        bool hasIce         = highTierVariants.Contains(7);
        bool hasElectricity = highTierVariants.Contains(8);
        bool hasWind        = highTierVariants.Contains(9);

        Color activeColor = Color.white;
        Color inactiveColor = new Color(0.3f, 0.3f, 0.3f, 1f);

        if (m_imgGrandWizardFire        != null) m_imgGrandWizardFire.color        = hasFire ? activeColor : inactiveColor;
        if (m_imgGrandWizardIce         != null) m_imgGrandWizardIce.color         = hasIce ? activeColor : inactiveColor;
        if (m_imgGrandWizardElectricity != null) m_imgGrandWizardElectricity.color = hasElectricity ? activeColor : inactiveColor;
        if (m_imgGrandWizardWind        != null) m_imgGrandWizardWind.color        = hasWind ? activeColor : inactiveColor;

        bool isSynergyActive = hasFire && hasIce && hasElectricity && hasWind;

        if (m_btnGrandWizard != null && m_btnGrandWizard.image != null) m_btnGrandWizard.image.color = isSynergyActive ? activeColor : inactiveColor;

        return isSynergyActive;
    }

    public override void Activate()
    {
        m_grandwizard = m_manager.CreateSynergyTower(m_wizardData);
        IsActive = true;
    }

    public override void Deactivate()
    {
        if (m_grandwizard != null)
        {
            m_grandwizard = m_manager.DestroySynergyTower(m_grandwizard);
        }
        IsActive = false;
    }
}
