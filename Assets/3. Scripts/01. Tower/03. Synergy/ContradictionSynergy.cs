using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ContradictionSynergy : SynergyBase
{
    private TowerController m_contradiction;
    private TowerData m_contradictionData;

    private Button m_btnCombineContradiction;
    private Button m_btnUndoContradiction;
    private Button m_btnContradiction;
    private Image m_imgContradictionShield;
    private Image m_imgContradictionSpear;

    private List<TowerData> m_consumedMaterials = new List<TowerData>();

    public ContradictionSynergy(TowerManager manager, TowerData data, Button btn1, Button btn2, Button btn3, Image img1, Image img2) : base(manager)
    {
        m_contradictionData       = data;
        m_btnCombineContradiction = btn1;
        m_btnUndoContradiction    = btn2;
        m_btnContradiction        = btn3;
        m_imgContradictionShield  = img1;
        m_imgContradictionSpear   = img2;

        if (m_btnCombineContradiction != null) m_btnCombineContradiction.gameObject.SetActive(false);
        if (m_btnUndoContradiction    != null) m_btnUndoContradiction.gameObject.SetActive(false);

        Color inactiveColor = new Color(0.3f, 0.3f, 0.3f, 1f);
        if (m_btnContradiction       != null) m_btnContradiction.image.color = inactiveColor;
        if (m_imgContradictionShield != null) m_imgContradictionShield.color = inactiveColor;
        if (m_imgContradictionSpear  != null) m_imgContradictionSpear.color  = inactiveColor;

        if (m_imgContradictionShield != null) m_imgContradictionShield.gameObject.SetActive(false);
        if (m_imgContradictionSpear  != null) m_imgContradictionSpear.gameObject.SetActive(false);

        if (m_btnContradiction != null)
        {
            m_btnContradiction.onClick.AddListener(() =>
            {
                bool isVisible = m_imgContradictionShield.gameObject.activeSelf;
                bool isOpening = !isVisible;

                if (m_imgContradictionShield  != null) m_imgContradictionShield.gameObject.SetActive(isOpening);
                if (m_imgContradictionSpear   != null) m_imgContradictionSpear.gameObject.SetActive(isOpening);                
                m_manager.HandleSynergyButtonToggle(m_btnContradiction, isOpening);
            });
        }
    }

    public override bool CheckCondition(Dictionary<Vector3Int, TowerManager.GridTowerInfo> towersOnGrid)
    {
        if (m_consumedMaterials != null && m_consumedMaterials.Count > 0)
        {
            int targetTier    = m_contradictionData.towerID / 1000;
            int targetVariant = m_contradictionData.towerID % 100;

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

        bool hasShield = highTierVariants.Contains(3);
        bool hasSpear  = highTierVariants.Contains(4);

        Color activeColor = Color.white;
        Color inactiveColor = new Color(0.3f, 0.3f, 0.3f, 1f);

        if (m_imgContradictionShield  != null) m_imgContradictionShield.color = hasShield ? activeColor : inactiveColor;
        if (m_imgContradictionSpear   != null) m_imgContradictionSpear.color  = hasSpear  ? activeColor : inactiveColor;

        if (m_btnCombineContradiction != null) m_btnCombineContradiction.interactable = (hasSpear && hasShield);

        bool isSynergyActive = hasShield && hasSpear;

        if (m_btnContradiction != null && m_btnContradiction.image != null) m_btnContradiction.image.color = isSynergyActive ? activeColor : inactiveColor;

        return isSynergyActive;
    }

    public override void Activate()
    {
        if (m_btnCombineContradiction != null && m_btnUndoContradiction != null)
        {
            m_btnCombineContradiction.gameObject.SetActive(true);
            m_btnUndoContradiction.gameObject.SetActive(false);
            m_btnCombineContradiction.onClick.RemoveAllListeners();
            m_btnCombineContradiction.onClick.AddListener(() =>
            {
                bool success = m_manager.CombineSynergyTower(m_contradictionData, 4, new List<int> { 3, 4 }, out m_consumedMaterials);

                if (success)
                {
                    m_btnCombineContradiction.gameObject.SetActive(false);
                    m_btnUndoContradiction.gameObject.SetActive(true);
                }
            });

            m_btnUndoContradiction.onClick.RemoveAllListeners();
            m_btnUndoContradiction.onClick.AddListener(() =>
            {
                bool undo = m_manager.UndoSynergyTower(m_contradictionData, m_consumedMaterials);

                if (undo)
                {
                    m_consumedMaterials.Clear();
                    m_btnCombineContradiction.gameObject.SetActive(true);
                    m_btnUndoContradiction.gameObject.SetActive(false);
                }
            });
        }
        IsActive = true;
    }

    public override void Deactivate()
    {
        if (m_btnCombineContradiction != null && m_btnUndoContradiction != null)
        {
            m_btnCombineContradiction.gameObject.SetActive(false);
            m_btnUndoContradiction.gameObject.SetActive(false);
        }
        IsActive = false;
    }
}
