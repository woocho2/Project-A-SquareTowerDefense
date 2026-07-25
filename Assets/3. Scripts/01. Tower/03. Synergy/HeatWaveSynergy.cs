using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

public class HeatWaveSynergy : SynergyBase
{
    private Tilemap m_pathTilemap;
    private Color m_heatColor;
    private List<Vector3Int> m_pathCells = new List<Vector3Int>();
    private Button m_btnHeatWave;
    private Image m_imgHeatWaveFire;
    private Image m_imgHeatWaveElectricity;
    private Image m_imgHeatWaveWind;

    public HeatWaveSynergy(TowerManager manager, Tilemap pathTilemap, Button btn, Image img1, Image img2, Image img3) : base(manager)
    {
        m_pathTilemap            = pathTilemap;
        m_btnHeatWave            = btn;
        m_imgHeatWaveFire        = img1;
        m_imgHeatWaveElectricity = img2;
        m_imgHeatWaveWind        = img3;
        ColorUtility.TryParseHtmlString("#FF6200", out m_heatColor);

        Color inactiveColor = new Color(0.3f, 0.3f, 0.3f, 1f);
        if (m_btnHeatWave            != null) m_btnHeatWave.image.color      = inactiveColor;
        if (m_imgHeatWaveFire        != null) m_imgHeatWaveFire.color        = inactiveColor;
        if (m_imgHeatWaveElectricity != null) m_imgHeatWaveElectricity.color = inactiveColor;
        if (m_imgHeatWaveWind        != null) m_imgHeatWaveWind.color        = inactiveColor;

        if (m_imgHeatWaveFire        != null) m_imgHeatWaveFire.gameObject.SetActive(false);
        if (m_imgHeatWaveElectricity != null) m_imgHeatWaveElectricity.gameObject.SetActive(false);
        if (m_imgHeatWaveWind        != null) m_imgHeatWaveWind.gameObject.SetActive(false);

        if (m_btnHeatWave != null)
        {
            m_btnHeatWave.onClick.AddListener(() =>
            {
                bool isVisible = m_imgHeatWaveFire.gameObject.activeSelf;
                bool isOpening = !isVisible;

                if (m_imgHeatWaveFire        != null) m_imgHeatWaveFire.gameObject.SetActive(isOpening);
                if (m_imgHeatWaveElectricity != null) m_imgHeatWaveElectricity.gameObject.SetActive(isOpening);
                if (m_imgHeatWaveWind        != null) m_imgHeatWaveWind.gameObject.SetActive(isOpening);
                m_manager.HandleSynergyButtonToggle(m_btnHeatWave, isOpening);
            });
        }
    }

    public override bool CheckCondition(Dictionary<Vector3Int, TowerManager.GridTowerInfo> towersOnGrid)
    {
        HashSet<int> highTierVariants = m_manager.GetActiveVariants();

        bool hasFire        = highTierVariants.Contains(6);
        bool hasElectricity = highTierVariants.Contains(8);
        bool hasWind        = highTierVariants.Contains(9);

        Color activeColor = Color.white;
        Color inactiveColor = new Color(0.3f, 0.3f, 0.3f, 1f);

        if (m_imgHeatWaveFire        != null) m_imgHeatWaveFire.color        = hasFire        ? activeColor : inactiveColor;
        if (m_imgHeatWaveElectricity != null) m_imgHeatWaveElectricity.color = hasElectricity ? activeColor : inactiveColor;
        if (m_imgHeatWaveWind        != null) m_imgHeatWaveWind.color        = hasWind        ? activeColor : inactiveColor;

        bool isSynergyActive = hasFire && hasElectricity && hasWind;

        if (m_btnHeatWave != null && m_btnHeatWave.image != null) m_btnHeatWave.image.color = isSynergyActive ? activeColor : inactiveColor;

        return isSynergyActive;
    }

    public override void Activate()
    {
        m_manager.IsHeatWaveActive = true;

        m_pathCells.Clear();
        BoundsInt bounds = m_pathTilemap.cellBounds;

        // 맵 전체를 순회하며 타일이 존재하는(적이 지나가는) 경로를 찾음
        foreach (var pos in bounds.allPositionsWithin)
        {
            if (m_pathTilemap.HasTile(pos))
            {
                m_pathCells.Add(pos);

                // 타일의 색상을 변경하기 위해서는 TileFlags 설정을 None으로 해제해야 함
                m_pathTilemap.SetTileFlags(pos, TileFlags.None);
                m_pathTilemap.SetColor(pos, m_heatColor);              
            }
        }

        IsActive = true;
    }

    public override void Deactivate()
    {
        m_manager.IsHeatWaveActive = false;

        if (m_pathTilemap != null)
        {
            // 색상이 변경되었던 타일들을 원래 색상(기본값인 흰색)으로 복구
            foreach (var pos in m_pathCells)
            {
                m_pathTilemap.SetColor(pos, Color.white);
            }
            m_pathCells.Clear();
        }
        IsActive = false;
    }
}