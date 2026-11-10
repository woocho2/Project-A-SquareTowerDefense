using System.Collections.Generic;
using UnityEngine;

namespace SquareTD
{
    // 블랙홀 장판: 범위 안의 적을 매 턴 블랙홀 쪽으로 1칸 끌어당기고 피해 (끌려간 칸의 타일 효과를 받음)
    public class BlackHoleGround : GroundEffect
    {
        private readonly BoardGrid m_grid;
        private readonly PathOccupancy m_occupancy;
        private readonly int m_center;
        private readonly int m_radius;

        public float Damage { get; }
        public int Radius => m_radius;

        public BlackHoleGround(TowerController owner, BoardGrid grid, PathOccupancy occupancy, int centerPathIndex, int mastery, float damage, int turns)
            : base(owner, TowerEmblem.Darkness, turns)
        {
            m_grid = grid;
            m_occupancy = occupancy;
            m_center = centerPathIndex;
            m_radius = SplashAction.GetAreaRadius(mastery);
            Damage = damage;
        }

        public override int PullCenter => m_center;

        public override bool Covers(int pathIndex) =>
            BoardGrid.GetTileDistance(m_grid.GetPathCell(m_center), m_grid.GetPathCell(pathIndex)) <= m_radius;

        public override void OnTurn()
        {
            var hitPositions = new List<Vector3>();
            foreach (ITarget target in SkillArea.Impact(m_grid, m_occupancy, m_center, m_radius + 1))
            {
                if (!target.IsAlive) continue;
                if (Application.isPlaying && target.Transform != null) hitPositions.Add(target.Transform.position);
                ProjectileHitResolver.HitSingle(target, new DamageInfo
                {
                    Attacker = Owner != null ? Owner : null,
                    Source = DamageSource.Ground,
                    Emblem = TowerEmblem.Darkness,
                    BaseDamage = Damage,
                });
                Pull(target.Body);
            }
            SkillEffectTable.PlayBlackHoleTurn(this, hitPositions);
        }

        private void Pull(EnemyController enemy)
        {
            if (enemy == null || !enemy.IsAlive || enemy.Movement == null || enemy.PathIndex == m_center) return;
            int step = enemy.PathIndex > m_center ? -1 : 1;
            enemy.Movement.TeleportTo(enemy.PathIndex + step);
            EnemyTileEffectApplier.ApplyForcedStop(enemy);
        }

        public override MoveRange ModifyMove(EnemyController enemy, MoveRange range) => range;

        public override string Describe() => $"블랙홀: 매 턴 {Damage:0.##} 피해, 범위 안의 적을 {m_center}번 칸 쪽으로 1칸씩 끌어당김, {RemainingTurns}턴 남음";
    }
}
