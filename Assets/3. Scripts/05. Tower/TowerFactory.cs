using UnityEngine;
using System.Collections.Generic;

namespace SquareTD
{
    // 타워를 만들고 바꾸고 지우는 곳 (행동·스킬 연결)
    public class TowerFactory
    {
        private readonly TowerController m_prefab;
        private readonly Transform m_parent;
        private readonly BoardManager m_board;
        private readonly TowerDatabase m_database;
        private readonly ColorUpgradeState m_colorLevels;
        private readonly System.Action<GameObject> m_destroy;

        public TowerFactory(TowerController prefab, Transform parent, BoardManager board,
                            TowerDatabase database, ColorUpgradeState colorLevels, System.Action<GameObject> destroy = null)
        {
            m_prefab = prefab;
            m_parent = parent;
            m_board = board;
            m_database = database;
            m_colorLevels = colorLevels;
            m_destroy = destroy ?? (go => Object.Destroy(go));
        }

        private TowerDefinition FindTier1(TowerDefinition definition)
        {
            int tier1Id = TowerId.Make(1, definition.Color, definition.Emblem);

            if (m_database.TryGet(tier1Id, out TowerDefinition tier1)) return tier1;
            return null;
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
                m_destroy(tower.gameObject);
                return null;
            }

            tower.Init(definition, slotIndex, CreateAction(definition.Color), FindTier1(definition), m_colorLevels, CreateSkill(definition));
            m_board.Zones.EnsureZonesFor(tower);
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

                case TowerColor.Yellow:
                    return new BuffZoneAction(m_board.Grid, m_board.Occupancy, m_board.Zones, m_board.Slots);

                case TowerColor.Purple:
                    return new DebuffZoneAction(m_board.Grid, m_board.Occupancy, m_board.Zones);

                default:
                    return null;
            }
        }

        private AttackSkill CreateSkill(TowerDefinition definition)
        {
            if (definition.Color != TowerColor.Red && definition.Color != TowerColor.Blue) return null;

            AttackSkill skill = SkillRegistry.CreateAttackSkill(definition.Color, definition.Emblem, m_board.Grid, m_board.Occupancy);
            if (skill != null) return skill;
            return new DamageOnlySkill(m_board.Grid, m_board.Occupancy);
        }

        public void Destroy(TowerController tower)
        {
            m_board.Slots.Remove(tower.SlotIndex);
            m_destroy(tower.gameObject);
        }

        public void ChangeDefinition(TowerController tower, TowerDefinition definition, IReadOnlyList<int> zoneCells = null)
        {
            tower.ChangeDefinition(definition, FindTier1(definition), CreateAction(definition.Color), m_colorLevels, CreateSkill(definition));
            m_board.Zones.EnsureZonesFor(tower);
            if (zoneCells == null) return;
            List<Zone> zones = m_board.Zones.GetByOwner(tower);
            for (int i = 0; i < zones.Count && i < zoneCells.Count; i++)
                if (zoneCells[i] >= 0) m_board.Zones.TryPlace(zones[i], zoneCells[i]);
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
            created.Init(newDefinition, slot, CreateAction(newDefinition.Color), FindTier1(newDefinition), m_colorLevels, CreateSkill(newDefinition));

            if (inheritState) created.State.CopyInheritedFrom(old.State);
            m_board.Zones.EnsureZonesFor(created);

            old.OnRemoved();
            m_board.Slots.Remove(slot);
            m_board.Slots.TryPlace(created, slot);
            m_destroy(old.gameObject);
            return created;
        }
    }
}
