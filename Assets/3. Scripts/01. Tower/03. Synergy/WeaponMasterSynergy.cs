using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WeaponMasterSynergy : SynergyBase
{
    private TowerController m_WeaponMaster;
    private TowerData m_gunData;
    private Button m_btnWeaponMaster;
    private Image m_imgWeaponMasterSword;
    private Image m_imgWeaponMasterBow;
    private Image m_imgWeaponMasterShield;
    private Image m_imgWeaponMasterSpear;
    private Image m_imgWeaponMasterAxe;

    public WeaponMasterSynergy(TowerManager manager, TowerData data, Button btn, Image img1, Image img2, Image img3, Image img4, Image img5) : base(manager)
    {
        m_gunData               = data;
        m_btnWeaponMaster       = btn;
        m_imgWeaponMasterSword  = img1;
        m_imgWeaponMasterBow    = img2;
        m_imgWeaponMasterShield = img3;
        m_imgWeaponMasterSpear  = img4;
        m_imgWeaponMasterAxe    = img5;

        Color inactiveColor = new Color(0.3f, 0.3f, 0.3f, 1f);
        if (m_btnWeaponMaster       != null) m_btnWeaponMaster.image.color = inactiveColor;
        if (m_imgWeaponMasterSword  != null) m_imgWeaponMasterSword.color  = inactiveColor;
        if (m_imgWeaponMasterBow    != null) m_imgWeaponMasterBow.color    = inactiveColor;
        if (m_imgWeaponMasterShield != null) m_imgWeaponMasterShield.color = inactiveColor;
        if (m_imgWeaponMasterSpear  != null) m_imgWeaponMasterSpear.color  = inactiveColor;
        if (m_imgWeaponMasterAxe    != null) m_imgWeaponMasterAxe.color    = inactiveColor;

        if (m_imgWeaponMasterSword  != null) m_imgWeaponMasterSword.gameObject.SetActive(false);
        if (m_imgWeaponMasterBow    != null) m_imgWeaponMasterBow.gameObject.SetActive(false);
        if (m_imgWeaponMasterShield != null) m_imgWeaponMasterShield.gameObject.SetActive(false);
        if (m_imgWeaponMasterSpear  != null) m_imgWeaponMasterSpear.gameObject.SetActive(false);
        if (m_imgWeaponMasterAxe    != null) m_imgWeaponMasterAxe.gameObject.SetActive(false);

        if (m_btnWeaponMaster != null)
        {
            m_btnWeaponMaster.onClick.AddListener(() =>
            {
                bool isVisible = m_imgWeaponMasterSword.gameObject.activeSelf;
                bool isOpening = !isVisible;

                if (m_imgWeaponMasterSword  != null) m_imgWeaponMasterSword.gameObject.SetActive(isOpening);
                if (m_imgWeaponMasterBow    != null) m_imgWeaponMasterBow.gameObject.SetActive(isOpening);
                if (m_imgWeaponMasterShield != null) m_imgWeaponMasterShield.gameObject.SetActive(isOpening);
                if (m_imgWeaponMasterSpear  != null) m_imgWeaponMasterSpear.gameObject.SetActive(isOpening);
                if (m_imgWeaponMasterAxe    != null) m_imgWeaponMasterAxe.gameObject.SetActive(isOpening);
                m_manager.HandleSynergyButtonToggle(m_btnWeaponMaster, isOpening);
            });
        }
    }

    public override bool CheckCondition(Dictionary<Vector3Int, TowerManager.GridTowerInfo> towersOnGrid)
    {
        HashSet<int> highTierVariants = m_manager.GetActiveVariants();

        bool hasSword  = highTierVariants.Contains(1);
        bool hasBow    = highTierVariants.Contains(2);
        bool hasShield = highTierVariants.Contains(3);
        bool hasSpear  = highTierVariants.Contains(4);
        bool hasAxe    = highTierVariants.Contains(5);

        Color activeColor = Color.white;
        Color inactiveColor = new Color(0.3f, 0.3f, 0.3f, 1f);

        if (m_imgWeaponMasterSword  != null) m_imgWeaponMasterSword.color  = hasSword  ? activeColor : inactiveColor;
        if (m_imgWeaponMasterBow    != null) m_imgWeaponMasterBow.color    = hasBow    ? activeColor : inactiveColor;
        if (m_imgWeaponMasterShield != null) m_imgWeaponMasterShield.color = hasShield ? activeColor : inactiveColor;
        if (m_imgWeaponMasterSpear  != null) m_imgWeaponMasterSpear.color  = hasSpear  ? activeColor : inactiveColor;
        if (m_imgWeaponMasterAxe    != null) m_imgWeaponMasterAxe.color    = hasAxe    ? activeColor : inactiveColor;

        bool isSynergyActive = hasSword && hasBow && hasShield && hasSpear && hasAxe;

        if (m_btnWeaponMaster != null && m_btnWeaponMaster.image != null) m_btnWeaponMaster.image.color = isSynergyActive ? activeColor : inactiveColor;

        return isSynergyActive;
    }

    public override void Activate()
    {
        m_WeaponMaster = m_manager.CreateSynergyTower(m_gunData);
        IsActive = true;
    }

    public override void Deactivate()
    {
        if (m_WeaponMaster != null)
        {
            m_WeaponMaster = m_manager.DestroySynergyTower(m_WeaponMaster);
        }
        IsActive = false;
    }
}
