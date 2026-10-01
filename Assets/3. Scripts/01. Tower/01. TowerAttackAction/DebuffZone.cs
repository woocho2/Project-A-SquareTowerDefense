using System;
using UnityEngine;

/// <summary>
/// 디버프타워 소유 장판이 놓인 패스 타일 인덱스를 관리합니다. 입력은 BoardInputController가 처리합니다.
/// 위치의 원본은 패스 인덱스이며, Transform은 그 타일 중앙을 보여주는 표시용입니다.
/// </summary>
public class DebuffZone : MonoBehaviour
{
    public event Action<int> OnPlacedPathIndexChanged;

    private TileSatelliteOrbiter m_orbiter;
    private int m_placedPathIndex = -1;
    private int m_previewPathIndex = -1;

    private void Awake()
    {
        m_orbiter = GetComponent<TileSatelliteOrbiter>();
    }

    public void SetHighlighted(bool highlighted)
    {
        if (m_orbiter != null) m_orbiter.SetHighlighted(highlighted);
    }

    public bool IsAtWorldPosition(Vector3 worldPosition)
    {
        return m_placedPathIndex >= 0 && TileManager.Instance != null &&
               TileManager.Instance.TryGetPathIndexAtWorldPosition(worldPosition, out int index) &&
               index == m_placedPathIndex;
    }

    /// <summary>드래그 중에는 보이는 위치만 옮기고, 놓인 인덱스와 타일 효과 기록은 바꾸지 않습니다.</summary>
    public bool TryPreviewAtWorldPosition(Vector3 worldPosition)
    {
        if (TileManager.Instance == null ||
            !TileManager.Instance.TryGetPathIndexAtWorldPosition(worldPosition, out int index) ||
            !MoveToPathIndex(index)) return false;

        m_previewPathIndex = index;
        return true;
    }

    /// <summary>손을 놓은 뒤 미리보기 중이던 패스 인덱스를 실제 위치로 확정합니다.</summary>
    public void CommitPreviewPosition()
    {
        int previewIndex = m_previewPathIndex;
        m_previewPathIndex = -1;

        if (previewIndex < 0 || !SetPlacedPathIndex(previewIndex)) MoveToPathIndex(m_placedPathIndex);
    }

    /// <summary>드래그가 취소되면 미리보기를 버리고 놓여 있던 타일로 되돌립니다.</summary>
    public void CancelPreview()
    {
        m_previewPathIndex = -1;
        MoveToPathIndex(m_placedPathIndex);
    }

    public bool TryGetPlacedPathIndex(out int index)
    {
        index = m_placedPathIndex;
        return index >= 0;
    }

    /// <summary>장판을 지정한 패스 타일에 놓습니다. 타워 교체 시 기존 장판의 위치를 넘길 때도 사용합니다.</summary>
    public bool SetPlacedPathIndex(int index)
    {
        if (!MoveToPathIndex(index)) return false;

        gameObject.SetActive(true);
        if (index == m_placedPathIndex) return true;

        m_placedPathIndex = index;
        OnPlacedPathIndexChanged?.Invoke(index);
        return true;
    }

    private bool MoveToPathIndex(int index)
    {
        if (TileManager.Instance == null ||
            !TileManager.Instance.TryGetPathWorldPosition(index, out Vector3 center)) return false;

        center.z = transform.position.z;
        transform.position = center;
        return true;
    }
}
