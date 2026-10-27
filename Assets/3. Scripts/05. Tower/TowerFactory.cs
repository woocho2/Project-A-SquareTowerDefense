using UnityEngine;

namespace SquareTD
{
    public class TowerFactory
    {
        private readonly TowerController m_prefab;
        private readonly Transform m_parent;
        private readonly BoardManager m_board;
        private readonly TowerDatabase m_database;
        private readonly ColorUpgradeState m_colorLevels;
        private readonly StackModeSettings m_stackModes;

        public TowerFactory(TowerController prefab, Transform parent, BoardManager board,
                            TowerDatabase database, ColorUpgradeState colorLevels, StackModeSettings stackModes)
        {
            m_prefab = prefab;
            m_parent = parent;
            m_board = board;
            m_database = database;
            m_colorLevels = colorLevels;
            m_stackModes = stackModes;
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
                Object.Destroy(tower.gameObject);
                return null;
            }

            tower.Init(definition, slotIndex, CreateAction(definition.Color), FindTier1(definition), m_colorLevels, CreateSkill(definition));
            tower.SetSpawnTile(m_board.Tiles.GetSpawnTile(slotIndex));
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

        // SkillRegistry에서 (컬러, 문양) → 그 문양의 스킬. 표에 아직 없는 문양만 시험 스킬 (문양이 다 들어오는 수업 40에서 지움)
        private AttackSkill CreateSkill(TowerDefinition definition)
        {
            if (definition.Color != TowerColor.Red && definition.Color != TowerColor.Blue) return null;

            AttackSkill skill = SkillRegistry.CreateAttackSkill(definition.Color, definition.Emblem, m_board.Grid, m_board.Occupancy, m_stackModes);
            if (skill != null) return skill;
            return new DamageOnlySkill(m_board.Grid, m_board.Occupancy, m_stackModes);
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
            created.Init(newDefinition, slot, CreateAction(newDefinition.Color), FindTier1(newDefinition), m_colorLevels, CreateSkill(newDefinition));

            if (inheritState) created.State.CopyInheritedFrom(old.State);
            created.SetSpawnTile(m_board.Tiles.GetSpawnTile(slot));
            m_board.Zones.EnsureZonesFor(created);                   // 티어 강화·합성한 버프·디버프 타워는 존을 처음부터 다시 고름 (옛 존은 old.OnRemoved에서 지움)


            old.OnRemoved();
            m_board.Slots.Remove(slot);
            m_board.Slots.TryPlace(created, slot);
            Object.Destroy(old.gameObject);
            return created;
        }
    }
}
