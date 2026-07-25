using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ThorSynergy : SynergyBase
{
    private TowerController m_thor;
    private TowerData m_thorData;

    private Button m_btnCombineThor;
    private Button m_btnUndoThor;
    private Button m_btnThor;
    private Image m_imgThorSword;
    private Image m_imgThorAxe;
    private Image m_imgThorElectricity;

    private List<TowerData> m_consumedMaterials = new List<TowerData>();

    public ThorSynergy(TowerManager manager, TowerData data, Button btn1, Button btn2, Button btn3, Image img1, Image img2, Image img3) : base(manager)
    {
        m_thorData           = data;
        m_btnCombineThor     = btn1;
        m_btnUndoThor        = btn2;
        m_btnThor            = btn3;
        m_imgThorSword       = img1;
        m_imgThorAxe         = img2;
        m_imgThorElectricity = img3;

        if (m_btnCombineThor != null) m_btnCombineThor.gameObject.SetActive(false);
        if (m_btnUndoThor    != null) m_btnUndoThor.gameObject.SetActive(false);

        Color inactiveColor = new Color(0.3f, 0.3f, 0.3f, 1f);
        if (m_btnThor            != null) m_btnThor.image.color      = inactiveColor;
        if (m_imgThorSword       != null) m_imgThorSword.color       = inactiveColor;
        if (m_imgThorAxe         != null) m_imgThorAxe.color         = inactiveColor;
        if (m_imgThorElectricity != null) m_imgThorElectricity.color = inactiveColor;

        if (m_imgThorSword       != null) m_imgThorSword.gameObject.SetActive(false);
        if (m_imgThorAxe         != null) m_imgThorAxe.gameObject.SetActive(false);
        if (m_imgThorElectricity != null) m_imgThorElectricity.gameObject.SetActive(false);

        if (m_btnThor != null)
        {
            m_btnThor.onClick.AddListener(() =>
            {
                bool isVisible = m_imgThorSword.gameObject.activeSelf;
                bool isOpening = !isVisible;

                if (m_imgThorSword       != null) m_imgThorSword.gameObject.SetActive(isOpening);
                if (m_imgThorAxe         != null) m_imgThorAxe.gameObject.SetActive(isOpening);
                if (m_imgThorElectricity != null) m_imgThorElectricity.gameObject.SetActive(isOpening);                
                m_manager.HandleSynergyButtonToggle(m_btnThor, isOpening);
            });
        }
    }

    public override bool CheckCondition(Dictionary<Vector3Int, TowerManager.GridTowerInfo> towersOnGrid)
    {
        if (m_consumedMaterials != null && m_consumedMaterials.Count > 0)
        {
            int targetTier = m_thorData.towerID / 1000;
            int targetVariant = m_thorData.towerID % 100;

            foreach (var kvp in towersOnGrid)
            {
                if (kvp.Value.Tier == targetTier && kvp.Value.Variant == targetVariant)
                {
                    return true;
                }
            }

            m_consumedMaterials.Clear();
            return false;
        }

        HashSet<int> highTierVariants = m_manager.GetActiveVariants();

        bool hasSword       = highTierVariants.Contains(1);
        bool hasAxe         = highTierVariants.Contains(5);
        bool hasElectricity = highTierVariants.Contains(8);

        Color activeColor = Color.white;
        Color inactiveColor = new Color(0.3f, 0.3f, 0.3f, 1f);

        if (m_imgThorSword       != null) m_imgThorSword.color       = hasSword       ? activeColor : inactiveColor;
        if (m_imgThorAxe         != null) m_imgThorAxe.color         = hasAxe         ? activeColor : inactiveColor;
        if (m_imgThorElectricity != null) m_imgThorElectricity.color = hasElectricity ? activeColor : inactiveColor;

        if (m_btnCombineThor != null) m_btnCombineThor.interactable = (hasSword && hasAxe && hasElectricity);

        bool isSynergyActive = hasSword && hasAxe && hasElectricity;

        if (m_btnThor != null && m_btnThor.image != null) m_btnThor.image.color = isSynergyActive ? activeColor : inactiveColor;

        return isSynergyActive;
    }

    public override void Activate()
    {
        if (m_btnCombineThor != null && m_btnUndoThor != null)
        {            
            m_btnCombineThor.gameObject.SetActive(true);
            m_btnUndoThor.gameObject.SetActive(false);
            m_btnCombineThor.onClick.RemoveAllListeners();
            m_btnCombineThor.onClick.AddListener(() =>
            {
                bool success = m_manager.CombineSynergyTower(m_thorData, 4, new List<int> { 1, 5, 8 }, out m_consumedMaterials);

                if (success)
                {
                    m_btnCombineThor.gameObject.SetActive(false);
                    m_btnUndoThor.gameObject.SetActive(true);
                }
            });

            m_btnUndoThor.onClick.RemoveAllListeners();
            m_btnUndoThor.onClick.AddListener(() =>
            {
                bool undo = m_manager.UndoSynergyTower(m_thorData, m_consumedMaterials);

                if (undo)
                {
                    m_consumedMaterials.Clear();
                    m_btnCombineThor.gameObject.SetActive(true);
                    m_btnUndoThor.gameObject.SetActive(false);
                }
            });
        }
        IsActive = true;
    }

    public override void Deactivate()
    {
        if (m_btnCombineThor != null && m_btnUndoThor != null)
        {
            m_btnCombineThor.gameObject.SetActive(false);
            m_btnUndoThor.gameObject.SetActive(false);
        }
        IsActive = false;
    }
}
