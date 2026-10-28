using System.Collections.Generic;
using UnityEngine;

namespace SquareTD
{
    // 화살비 (21번 #115, 20번 4-5장): 쏘아 올린 화살이 ⑤마다 맵 전체에 떨어짐
    //   피해는 발동할 때 정해 둠 (Power × AbilityValue × 0.4, 스택을 썼으면 그만큼 더 큼) → 다시 발동해도 따로 남음
    //   치명은 없음. 방어는 받음
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
                if (Application.isPlaying && target.Transform != null) hitPositions.Add(target.Transform.position);   // 피해 전에 (죽으면 풀로 돌아가 자리가 바뀜)

                ProjectileHitResolver.HitSingle(target, new DamageInfo
                {
                    Attacker = Owner != null ? Owner : null,    // 그사이 팔린 타워면 null
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
