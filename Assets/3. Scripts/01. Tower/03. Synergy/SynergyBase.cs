#if false // Synergy system temporarily disabled
using System.Collections.Generic;
using UnityEngine;

public abstract class SynergyBase
{
    public bool IsActive { get; protected set; }
    protected TowerManager m_manager;

    public SynergyBase(TowerManager manager)
    {
        m_manager = manager;
        IsActive = false;
    }

    public abstract bool CheckCondition(Dictionary<Vector3Int, TowerManager.GridTowerInfo> towersOnGrid);

    public abstract void Activate();
    public abstract void Deactivate();
}
#endif
