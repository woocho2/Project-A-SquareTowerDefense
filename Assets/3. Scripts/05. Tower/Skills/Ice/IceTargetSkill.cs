using System.Collections.Generic;
using UnityEngine;

namespace SquareTD
{
    // 얼음 단일 — 스노우볼 (20번 4-13장)
    //   그 적에게 Power × AbilityValue × (1 + 감소된 이동력 × 0.25) (스택을 쓰면 ×)
    //   감소된 이동력 = 최소가 줄어든 양 + 최대가 줄어든 양. 원본(기본 이동력)과 지금을 견줘 실제로 줄어든 만큼만 (늘어난 쪽은 0)
    //   예: 저체온증(−1 / −1) 2 → 1.5배, 한기(−1 / −1)까지 4 → 2배. 얼음이 깎지 않았어도 셈 (100T 해머, 진흙 …)
    public class IceTargetSkill : AttackSkill
    {
        public const float PerSpeed = 0.25f;

        public IceTargetSkill(BoardGrid grid, PathOccupancy occupancy, StackModeSettings stackModes)
            : base(grid, occupancy, stackModes) { }

        protected override List<ITarget> GetArea(TowerController owner, ITarget mainTarget) => SkillArea.Single(mainTarget);

        protected override float ModifyOutgoingDamage(TowerController owner, ITarget target)
        {
            if (target.Stats == null) return 1f;
            return 1f + ReducedSpeed(target.Stats) * PerSpeed;
        }

        // 이동력은 1 아래로 안 내려가므로(3장) 실제 이동 범위로 견줌
        public static int ReducedSpeed(EnemyStats stats)
        {
            MoveRange now = stats.GetMoveRange();
            int baseMin = Mathf.RoundToInt(stats.GetBase(StatType.SpeedMin));
            int baseMax = Mathf.RoundToInt(stats.GetBase(StatType.SpeedMax));
            return Mathf.Max(0, baseMin - now.Min) + Mathf.Max(0, baseMax - now.Max);
        }

        protected override void OnHitTarget(TowerController owner, ITarget target, DamageInfo hit)
        {
            // [C] 피해 주기 — SwordSplashSkill.OnHitTarget과 똑같은 한 줄
        }
    }
}
