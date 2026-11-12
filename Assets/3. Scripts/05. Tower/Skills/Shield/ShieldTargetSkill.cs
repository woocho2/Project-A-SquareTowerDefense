using System.Collections.Generic;
using UnityEngine;

namespace SquareTD
{
    // 방패 단일 — 리바운드 (맞은 적에서 방패가 맵 전체의 다른 적에게 (Tier × 레벨)번 튕김, 한 번에 Power × AbilityValue × 0.2.
    //                       다음은 방패에서 가장 가까운 적, 바로 직전 적은 빼고. 튕길 적이 없으면 남은 튕김의 피해를 합쳐 한 번에)
    public class ShieldTargetSkill : AttackSkill
    {
        public const float BounceRatio = 0.2f;

        public ShieldTargetSkill(BoardGrid grid, PathOccupancy occupancy)
            : base(grid, occupancy) { }

        public static int BounceCount(TowerController owner) => SkillFormula.EffectDuration(owner);

        protected override bool CastInstead(TowerController owner, ITarget mainTarget, bool useStack)
        {
            if (mainTarget == null) return false;

            TowerFinalStats stats = owner.Stats;
            float damage = SkillFormula.AttackSkill(stats.Power, ChargedAbility(stats, useStack)) * BounceRatio;
            int bounces = BounceCount(owner);
            ITarget previous = mainTarget;
            int shieldAt = mainTarget.PathIndex;
            var visualPath = new List<Vector3>();
            if (Application.isPlaying && mainTarget.Transform != null) visualPath.Add(mainTarget.Transform.position);

            for (int i = 0; i < bounces; i++)
            {
                ITarget next = NearestExcept(shieldAt, previous);
                if (next == null)
                {
                    if (previous.IsAlive)
                    {
                        if (Application.isPlaying && previous.Transform != null) visualPath.Add(previous.Transform.position);
                        Hit(owner, stats, previous, damage * (bounces - i));
                    }
                    break;
                }

                if (Application.isPlaying && next.Transform != null) visualPath.Add(next.Transform.position);
                Hit(owner, stats, next, damage);
                previous = next;
                shieldAt = next.PathIndex;
            }
            SkillEffectTable.PlayRebound(owner, visualPath);
            return true;
        }

        private void Hit(TowerController owner, TowerFinalStats stats, ITarget target, float damage) =>
            ProjectileHitResolver.HitSingle(target, MakeSkillHit(owner, stats, target, damage, false));

        private ITarget NearestExcept(int pathIndex, ITarget excluded)
        {
            Vector3Int from = m_grid.GetPathCell(pathIndex);
            ITarget best = null;
            int bestDistance = int.MaxValue;

            foreach (ITarget target in SkillArea.WholeMap(m_grid, m_occupancy))
            {
                if (target == excluded || !target.IsAlive) continue;
                int distance = BoardGrid.GetTileDistance(from, m_grid.GetPathCell(target.PathIndex));
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = target;
                }
            }
            return best;
        }

        protected override List<ITarget> GetArea(TowerController owner, ITarget mainTarget) => new List<ITarget>();
        protected override void OnHitTarget(TowerController owner, ITarget target, DamageInfo hit) { }
    }
}
