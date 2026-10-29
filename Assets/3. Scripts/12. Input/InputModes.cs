using UnityEngine;

namespace SquareTD
{
    // 입력 모드들 (보통, 공격 칸 고르기, 존 자리 고르기 등)
    public readonly struct BoardPoint
    {
        public readonly Vector3 World;
        public readonly int PathIndex;
        public readonly int SpawnIndex;

        public BoardPoint(Vector3 world, int pathIndex, int spawnIndex)
        {
            World = world;
            PathIndex = pathIndex;
            SpawnIndex = spawnIndex;
        }

        public bool IsPath => PathIndex >= 0;
        public bool IsSpawn => SpawnIndex >= 0;
    }

    public interface IInputMode
    {
        void Enter();
        void Exit();
        void OnPointerDown(BoardPoint point);
        void OnPointerDrag(Vector3 world);
        void OnPointerUp(BoardPoint point);
        void OnCancel();
    }

    public class IdleMode : IInputMode
    {
        private const float DragStartDistance = 0.2f;

        private readonly BoardInputController m_input;
        private TowerController m_pressedTower;
        private Vector3 m_pressedWorld;

        public IdleMode(BoardInputController input)
        {
            m_input = input;
        }

        public void Enter() => m_pressedTower = null;
        public void Exit() => m_pressedTower = null;

        public void OnPointerDown(BoardPoint point)
        {
            m_pressedTower = point.IsSpawn ? BoardManager.Instance.Slots.Get(point.SpawnIndex) : null;
            m_pressedWorld = point.World;
            TowerManager.Instance.Select(m_pressedTower);
            m_input.NotifyClicked(point);
        }

        public void OnPointerDrag(Vector3 world)
        {
            if (m_pressedTower == null) return;
            if (!TurnManager.Instance.Current.CanPlayerAct) return;
            if (TowerId.IsSynergy(m_pressedTower.Definition.Id)) return;
            if (Vector3.Distance(world, m_pressedWorld) < DragStartDistance) return;

            m_input.ChangeMode(new DragTowerMode(m_input, m_pressedTower));
        }

        public void OnPointerUp(BoardPoint point) => m_pressedTower = null;
        public void OnCancel() => m_pressedTower = null;
    }

    public class PickAttackCellMode : IInputMode
    {
        private readonly BoardInputController m_input;
        private readonly TowerController m_tower;

        public PickAttackCellMode(BoardInputController input, TowerController tower)
        {
            m_input = input;
            m_tower = tower;
        }

        public void Enter() => Debug.Log($"[Input] {m_tower.name}의 공격할 칸을 고르세요 (사거리 안의 길 칸, 같은 칸을 다시 누르면 해제)");
        public void Exit() { }
        public void OnPointerDrag(Vector3 world) { }
        public void OnPointerUp(BoardPoint point) { }

        public void OnPointerDown(BoardPoint point)
        {
            if (!point.IsPath || !IsInRange(point.PathIndex))
            {
                Debug.Log("[Input] 사거리 안의 길 칸만 고를 수 있음");
                m_input.ChangeMode(new IdleMode(m_input));
                return;
            }

            bool same = m_tower.State.AttackCellIndex == point.PathIndex;
            m_tower.State.AttackCellIndex = same ? -1 : point.PathIndex;
            Debug.Log($"[Input] {m_tower.name} 공격 칸 {(same ? "해제" : $"{point.PathIndex}번")}");
            m_input.ChangeMode(new IdleMode(m_input));
        }

        public void OnCancel() => m_input.ChangeMode(new IdleMode(m_input));

        private bool IsInRange(int pathIndex)
        {
            BoardGrid grid = BoardManager.Instance.Grid;
            Vector3Int towerCell = grid.GetSpawnCell(m_tower.SlotIndex);
            return BoardGrid.GetTileDistance(towerCell, grid.GetPathCell(pathIndex)) <= m_tower.Stats.Range;
        }
    }

    public class PlaceZoneMode : IInputMode
    {
        private readonly BoardInputController m_input;
        private readonly TowerController m_tower;
        private readonly ZoneRegistry m_zones;

        public PlaceZoneMode(BoardInputController input, TowerController tower, ZoneRegistry zones)
        {
            m_input = input;
            m_tower = tower;
            m_zones = zones;
        }

        public void Enter()
        {
            if (NextUnplaced() == null)
            {
                Debug.Log($"[Input] {m_tower.name}: 고를 존이 없음");
                m_input.ChangeMode(new IdleMode(m_input));
                return;
            }
            LogRemaining();
        }

        public void Exit() { }
        public void OnPointerDrag(Vector3 world) { }
        public void OnPointerUp(BoardPoint point) { }
        public void OnCancel() => m_input.ChangeMode(new IdleMode(m_input));

        public void OnPointerDown(BoardPoint point)
        {
            Zone zone = NextUnplaced();
            if (zone == null)
            {
                m_input.ChangeMode(new IdleMode(m_input));
                return;
            }

            int cell = zone.IsBuff ? point.SpawnIndex : point.PathIndex;
            string reason = zone.IsBuff ? "버프존은 타워 스폰 칸에만" : "디버프존은 길 칸에만";
            if (cell < 0 || !m_zones.CanPlace(zone, cell, out reason))
            {
                Debug.Log($"[Input] 여기에는 놓을 수 없음: {reason}");
                return;
            }

            m_zones.TryPlace(zone, cell);
            Debug.Log($"[Input] {m_tower.name} 존 → {(zone.IsBuff ? "스폰" : "길")} {cell}번 칸 (확정)");

            if (NextUnplaced() == null) m_input.ChangeMode(new IdleMode(m_input));
            else LogRemaining();
        }

        private Zone NextUnplaced() =>
            m_zones.GetByOwner(m_tower).Find(zone => !zone.IsPlaced && m_zones.HasPlaceableCell(zone));

        private void LogRemaining()
        {
            int left = m_zones.GetByOwner(m_tower).FindAll(zone => !zone.IsPlaced).Count;
            bool isBuff = m_tower.Definition.Color == TowerColor.Yellow;
            Debug.Log($"[Input] {m_tower.name}의 존 자리를 고르세요 ({(isBuff ? "타워 스폰 칸" : "길 칸, 비프로스트 빼고")}), 남은 존 {left}개");
        }
    }

    public class DragTowerMode : IInputMode
    {
        private readonly BoardInputController m_input;
        private readonly TowerController m_tower;

        public DragTowerMode(BoardInputController input, TowerController tower)
        {
            m_input = input;
            m_tower = tower;
        }

        public void Enter() { }
        public void Exit() { }
        public void OnPointerDown(BoardPoint point) { }

        public void OnPointerDrag(Vector3 world)
        {
            m_tower.transform.position = world;
        }

        public void OnPointerUp(BoardPoint point)
        {
            bool moved = point.IsSpawn && TowerManager.Instance.TryMoveOrSwap(m_tower, point.SpawnIndex);
            if (!moved) ReturnToSlot();
            m_input.ChangeMode(new IdleMode(m_input));
        }

        public void OnCancel()
        {
            ReturnToSlot();
            m_input.ChangeMode(new IdleMode(m_input));
        }

        private void ReturnToSlot()
        {
            m_tower.transform.position = BoardManager.Instance.GetSpawnWorldPosition(m_tower.SlotIndex);
        }
    }
}
