using System.Collections.Generic;
using UnityEngine;

namespace SquareTD
{
    // 물 광역 — 쓰나미 (맵 전체, × (1 + 젖음 × 0.01), 맞은 적은 (100 - 저항력)% 확률로 -3 ~ +3칸 밀려나 그 칸의 타일 효과를 받음)
    public class WaterSplashSkill : AttackSkill
    {
        public const int PushRange = 3;

        public WaterSplashSkill(BoardGrid grid, PathOccupancy occupancy)
            : base(grid, occupancy) { }

        protected override List<ITarget> GetArea(TowerController owner, ITarget mainTarget) => SkillArea.WholeMap(m_grid, m_occupancy);

        public static float WetMultiplier(ITarget target) => 1f + WetEffect.Get(target) * 0.01f;

        protected override float ModifyOutgoingDamage(TowerController owner, ITarget target) => WetMultiplier(target);

        protected override void OnHitTarget(TowerController owner, ITarget target, DamageInfo hit)
        {
            EnemyController enemy = target.Body;
            bool pushes = enemy != null && enemy.IsAlive && Chance.Roll(enemy.Resistance.ResistWeak(1f));
            DamageInfo wave = hit;
            wave.ChangesMovement = pushes;
            ProjectileHitResolver.HitSingle(target, wave);
            if (!pushes || !enemy.IsAlive || enemy.Movement == null) return;

            enemy.Movement.TeleportTo(enemy.PathIndex + Random.Range(-PushRange, PushRange + 1));
            EnemyTileEffectApplier.ApplyForcedStop(enemy);
        }
    }
}
