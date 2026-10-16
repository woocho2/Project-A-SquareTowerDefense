using System;
using System.Collections.Generic;
public partial class TileManager
{
    private readonly Dictionary<int, Vector3Int> m_pathCellsByIndex = new();
    private readonly Dictionary<Vector3Int, List<int>> m_pathIndicesByCell = new();
    private readonly Dictionary<int, List<EnemyHealthController>> m_enemiesByPathIndex = new();
    private readonly Dictionary<EnemyHealthController, int> m_pathIndexByEnemy = new();
    private readonly Dictionary<EnemyHealthController, long> m_enemyArrivalOrder = new();
    private readonly Dictionary<Vector3Int, TextMeshPro> m_enemyCountLabels = new();
    private long m_nextEnemyArrivalOrder;
    private TMP_FontAsset m_enemyCountFont;
    private float m_enemyCountFontSize = 2.5f;
    private Color m_enemyCountColor = Color.white;
    private Vector2 m_enemyCountOffset = new(0.27f, -0.3f);
    private Tilemap tilemap = new();
    private GameObject gameObject = new();
    private Transform transform = new();
    public event Action EnemyCountChanged;
    public void AddPath(int index, Vector3Int cell) {
        m_pathCellsByIndex[index] = cell;
        if (!m_pathIndicesByCell.TryGetValue(cell, out var visits)) m_pathIndicesByCell[cell] = visits = new();
        visits.Add(index);
    }
    public TextMeshPro Label(Vector3Int cell) => m_enemyCountLabels[cell];
    public bool SetEnemyAtPathIndex(EnemyHealthController enemy, int index, out int previousIndex)
    {
        previousIndex = -1;
        if (enemy == null || !m_pathCellsByIndex.ContainsKey(index)) return false;

        bool wasRegistered = m_pathIndexByEnemy.TryGetValue(enemy, out int currentIndex);
        if (wasRegistered && currentIndex == index)
        {
            previousIndex = index;
            return true;
        }

        Vector3Int destinationCell = m_pathCellsByIndex[index];
        Vector3Int previousCell = default;
        if (wasRegistered)
        {
            previousIndex = currentIndex;
            previousCell = m_pathCellsByIndex[currentIndex];
            RemoveEnemyOccupancy(enemy, currentIndex);
        }
        // 같은 물리 타일의 다른 방문 인덱스로 바뀌는 경우 도착 순서는 유지합니다.
        if (!wasRegistered || previousCell != destinationCell)
            m_enemyArrivalOrder[enemy] = m_nextEnemyArrivalOrder++;
        if (!m_enemiesByPathIndex.TryGetValue(index, out List<EnemyHealthController> occupants))
        {
            occupants = new List<EnemyHealthController>();
            m_enemiesByPathIndex.Add(index, occupants);
        }

        occupants.Add(enemy);
        m_pathIndexByEnemy[enemy] = index;
        if (wasRegistered && previousCell != destinationCell) RefreshEnemyStack(previousCell);
        RefreshEnemyStack(destinationCell);

        EnemyCountChanged?.Invoke();
        return true;
    }

    public bool RemoveEnemyFromPath(EnemyHealthController enemy, out int previousIndex)
    {
        previousIndex = -1;
        if (ReferenceEquals(enemy, null)) return false;

        if (!m_pathIndexByEnemy.TryGetValue(enemy, out previousIndex)) return false;

        m_pathIndexByEnemy.Remove(enemy);
        m_enemyArrivalOrder.Remove(enemy);
        RemoveEnemyOccupancy(enemy, previousIndex);
        RefreshEnemyStack(m_pathCellsByIndex[previousIndex]);
        EnemyCountChanged?.Invoke();
        return true;
    }

    private void RemoveEnemyOccupancy(EnemyHealthController enemy, int index)
    {
        if (m_enemiesByPathIndex.TryGetValue(index, out List<EnemyHealthController> occupants))
        {
            occupants.Remove(enemy);
            if (occupants.Count == 0) m_enemiesByPathIndex.Remove(index);
        }
    }

    // 소환 초기화 중에도 수와 표시 순서가 필요하므로 HP 초기화 여부는 거르지 않습니다.
    public List<EnemyHealthController> GetEnemyOccupantsAtPathIndex(int index)
    {
        List<EnemyHealthController> result = new List<EnemyHealthController>();
        if (!m_enemiesByPathIndex.TryGetValue(index, out List<EnemyHealthController> occupants)) return result;
        foreach (EnemyHealthController enemy in occupants)
        {
            if (enemy != null && enemy.gameObject.activeInHierarchy) result.Add(enemy);
        }
        return result;
    }

    // 경로가 같은 좌표를 여러 번 지나도 그 좌표의 모든 방문 인덱스를 합친다.
    public List<EnemyHealthController> GetEnemyOccupantsAtPathCell(Vector3Int cell)
    {
        List<EnemyHealthController> result = new List<EnemyHealthController>();
        if (!m_pathIndicesByCell.TryGetValue(cell, out List<int> visits)) return result;
        foreach (int index in visits)
        {
            result.AddRange(GetEnemyOccupantsAtPathIndex(index));
        }
        result.Sort((left, right) => m_enemyArrivalOrder[left].CompareTo(m_enemyArrivalOrder[right]));
        return result;
    }

    private void RefreshEnemyStack(Vector3Int cell)
    {
        List<EnemyHealthController> enemies = GetEnemyOccupantsAtPathCell(cell);
        for (int i = 0; i < enemies.Count; i++)
        {
            EnemyMoveController movement = enemies[i].Movement;
            if (movement != null) movement.SetStackRenderOrder(Mathf.Max(-20000, 20000 - i * 2), i == 0);
        }

        if (enemies.Count == 0)
        {
            if (m_enemyCountLabels.TryGetValue(cell, out TextMeshPro emptyLabel) && emptyLabel != null)
                emptyLabel.gameObject.SetActive(false);
            return;
        }

        if (!m_enemyCountLabels.TryGetValue(cell, out TextMeshPro label) || label == null)
        {
            var labelObject = new GameObject($"EnemyCount_{cell.x}_{cell.y}", typeof(TextMeshPro));
            labelObject.layer = gameObject.layer;
            labelObject.transform.SetParent(transform, false);
            label = labelObject.GetComponent<TextMeshPro>();
            label.font = m_enemyCountFont != null ? m_enemyCountFont : TMP_Settings.defaultFontAsset;
            label.fontSize = m_enemyCountFontSize;
            label.fontStyle = FontStyles.Bold;
            label.color = m_enemyCountColor;
            label.outlineColor = Color.black;
            label.outlineWidth = 0.2f;
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.renderer.sortingLayerID = SortingLayer.NameToID("Enemy");
            label.renderer.sortingOrder = 30000;
            Vector3 size = tilemap.cellSize;
            label.rectTransform.sizeDelta = new Vector2(size.x * 0.5f, size.y * 0.3f);
            label.transform.position = tilemap.GetCellCenterWorld(cell)
                + new Vector3(size.x * m_enemyCountOffset.x, size.y * m_enemyCountOffset.y, -2f);
            m_enemyCountLabels[cell] = label;
        }
        label.SetText("x{0}", enemies.Count);
        label.gameObject.SetActive(true);
    }

    /// <summary>패스에 등록된 모든 적을 복사해 반환합니다. 순회 중 적이 죽거나 이동해도 안전합니다.</summary>
    public List<EnemyHealthController> GetAllEnemiesOnPath()
    {
        List<EnemyHealthController> result = new List<EnemyHealthController>(m_pathIndexByEnemy.Count);
        foreach (EnemyHealthController enemy in m_pathIndexByEnemy.Keys)
        {
            if (enemy != null && enemy.gameObject.activeInHierarchy) result.Add(enemy);
        }
        return result;
    }

    private static bool IsLivingEnemy(EnemyHealthController enemy)
    {
        return enemy != null && enemy.gameObject.activeInHierarchy && enemy.CurrentHP > 0f;
    }

    /// <summary>특정 패스 인덱스를 점유한 살아 있는 적 목록입니다. 공격·스플래시 대상 판정에 사용합니다.</summary>
    public List<EnemyHealthController> GetLivingEnemiesAtPathIndex(int index)
    {
        List<EnemyHealthController> result = new List<EnemyHealthController>();
        AddLivingEnemiesAtPathIndex(index, result);
        return result;
    }

    /// <summary>경로가 같은 좌표를 여러 번 지나도 그 좌표의 살아 있는 적을 모두 반환합니다.</summary>
    public List<EnemyHealthController> GetLivingEnemiesAtPathCell(Vector3Int cell)
    {
        List<EnemyHealthController> result = new List<EnemyHealthController>();
        if (!m_pathIndicesByCell.TryGetValue(cell, out List<int> visits)) return result;
        foreach (int index in visits)
        {
            AddLivingEnemiesAtPathIndex(index, result);
        }
        return result;
    }

    /// <summary>
    /// 모든 패스 타일의 살아 있는 적을 반환합니다. 체인 공격처럼 경로 전체 후보가 필요할 때 사용합니다.
    /// 호출자는 반환 리스트를 장기 보관하지 않아야 합니다.
    /// </summary>
    public List<EnemyHealthController> GetAllLivingEnemies()
    {
        List<EnemyHealthController> result = new List<EnemyHealthController>(m_pathIndexByEnemy.Count);
        foreach (List<EnemyHealthController> occupants in m_enemiesByPathIndex.Values)
        {
            foreach (EnemyHealthController enemy in occupants)
            {
                if (IsLivingEnemy(enemy)) result.Add(enemy);
            }
        }
        return result;
    }

    private void AddLivingEnemiesAtPathIndex(int index, List<EnemyHealthController> result)
    {
        if (!m_enemiesByPathIndex.TryGetValue(index, out List<EnemyHealthController> occupants)) return;
        foreach (EnemyHealthController enemy in occupants)
        {
            if (IsLivingEnemy(enemy)) result.Add(enemy);
        }
    }


}