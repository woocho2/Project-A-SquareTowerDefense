using System.Collections.Generic;
using UnityEngine;

namespace SquareTD
{
    // 블랙홀 장판: 적을 끌어당기고 매 턴 피해
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
            }
            SkillEffectTable.PlayBlackHoleTurn(this, hitPositions);
        }

        public static void GetPull(ZoneStage darkness, out int lower, out int higher, out float backChance)
        {
            switch (darkness)
            {
                case ZoneStage.Skill2: lower = 4; higher = 5; backChance = 1f; break;
                case ZoneStage.Skill1: lower = 3; higher = 4; backChance = 0.8f; break;
                default: lower = 2; higher = 3; backChance = 0.6f; break;
            }
        }

        public override MoveRange ModifyMove(EnemyController enemy, MoveRange range)
        {
            if (enemy.PathIndex == m_center) return range;

            GetPull(EmblemDebuffEffect.StageOn(enemy, TowerEmblem.Darkness), out int lower, out int higher, out float backChance);

            if (enemy.PathIndex < m_center) return new MoveRange(range.Min + lower, range.Max + lower, range.Action);

            var faster = new MoveRange(range.Min + higher, range.Max + higher, range.Action);
            if (!Chance.Roll(enemy.Resistance.ResistWeak(backChance))) return faster;
            return new MoveRange(-faster.Max, -faster.Min, faster.Action);
        }

        public override string Describe() => $"블랙홀: 매 턴 {Damage:0.##} 피해, 맵 전체를 {m_center}번 칸으로 끌어당김, {RemainingTurns}턴 남음";
    }
}
