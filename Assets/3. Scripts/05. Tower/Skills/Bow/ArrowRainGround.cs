using System.Collections.Generic;
using UnityEngine;

namespace SquareTD
{
    // 화살비 장판: ⑤마다 맵 전체에 화살이 떨어짐
    public class ArrowRainGround : GroundEffect
    {
        public const float TurnRatio = 0.4f;

        private readonly BoardGrid m_grid;
        private readonly PathOccupancy m_occupancy;

        public float Damage { get; }

        public ArrowRainGround(TowerController owner, BoardGrid grid, PathOccupancy occupancy, float damage, int turns)
            : base(owner, TowerEmblem.Bow, turns)
        {
            m_grid = grid;
            m_occupancy = occupancy;
            Damage = damage;
        }

        public override void OnTurn()
        {
            var hitPositions = new List<Vector3>();
            foreach (ITarget target in SkillArea.WholeMap(m_grid, m_occupancy))
            {
                if (!target.IsAlive) continue;
                if (Application.isPlaying && target.Transform != null) hitPositions.Add(target.Transform.position);

                ProjectileHitResolver.HitSingle(target, new DamageInfo
                {
                    Attacker = Owner != null ? Owner : null,
                    Source = DamageSource.Ground,
                    Emblem = TowerEmblem.Bow,
                    Attribute = AttackAttribute.Pierce,
                    BaseDamage = Damage,
                });
            }
            SkillEffectTable.PlayArrowRain(Owner, hitPositions);
        }

        public override string Describe() => $"화살비: 매 턴 맵 전체에 {Damage:0.##} 피해, {RemainingTurns}턴 남음";
    }
}
