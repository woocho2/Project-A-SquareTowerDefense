using UnityEngine;

namespace SquareTD
{
    public class TowerFactory
    {
        private readonly TowerController m_prefab;
        private readonly Transform m_parent;
        private readonly BoardManager m_board;

        public TowerFactory(TowerController prefab, Transform parent, BoardManager board)
        {
            m_prefab = prefab;
            m_parent = parent;
            m_board = board;
        }

        public TowerController Create(TowerDefinition definition, int slotIndex)
        {
            if (m_prefab == null)
            {
                Debug.LogError("[TowerFactory] 타워 프리팹이 비어 있음");
                return null;
            }

            Vector3 position = m_board.GetWorldPosition(m_board.Grid.GetSpawnCell(slotIndex));
            TowerController tower = Object.Instantiate(m_prefab, position, Quaternion.identity, m_parent);

            if (!m_board.Slots.TryPlace(tower, slotIndex))
            {
                Debug.LogError($"[TowerFactory] {slotIndex}번 칸에 이미 타워가 있어 놓지 못함");
                Object.Destroy(tower.gameObject);
                return null;
            }

            tower.Init(definition, slotIndex, CreateAction(definition.Color));
            return tower;
        }

        private TowerAction CreateAction(TowerColor color)
        {
            switch (color)
            {
                case TowerColor.Red:
                    return new SplashAction(m_board.Grid, m_board.Occupancy, ProjectileManager.Instance);

                case TowerColor.Blue:
                    return new TargetAction(m_board.Grid, m_board.Occupancy, ProjectileManager.Instance);

                default:
                    return null;
            }
        }

        public void Destroy(TowerController tower)
        {
            m_board.Slots.Remove(tower.SlotIndex);
            Object.Destroy(tower.gameObject);
        }

        public TowerController Replace(TowerController old, TowerDefinition newDefinition, bool inheritState)
        {
            if (m_prefab == null)
            {
                Debug.LogError("[TowerFactory] 타워 프리팹이 비어 있음");
                return null;
            }

            int slot = old.SlotIndex;
            TowerController created = Object.Instantiate(m_prefab, old.transform.position, Quaternion.identity, m_parent);
            created.Init(newDefinition, slot, CreateAction(newDefinition.Color));
            if (inheritState) created.State.CopyInheritedFrom(old.State);

            old.OnRemoved();
            m_board.Slots.Remove(slot);
            m_board.Slots.TryPlace(created, slot);
            Object.Destroy(old.gameObject);
            return created;
        }
    }
}
