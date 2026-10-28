using System.Collections.Generic;

namespace SquareTD
{
    // 해머 광역 — 100T 해머 (20번 4-8장)
    //   착탄 범위(Mastery) 안의 적에게 Power × AbilityValue (스택을 쓰면 ×), 맞은 적의 최대 이동력 −1
    //   그 적이 (컬러 강화 레벨 × 2)번 행동하는 동안. 정수형이라 저항할 수 없음. 겹치지 않음 (다시 맞으면 유지만 처음부터)
    //   이동력을 바꾼 공격이라 젖음·침수(물)의 피해 증가를 전부 받음 (ChangesMovement)
    public class HammerSplashSkill : AttackSkill
    {
        public const string Key = "100T 해머";

        public HammerSplashSkill(BoardGrid grid, PathOccupancy occupancy, StackModeSettings stackModes)
            : base(grid, occupancy, stackModes) { }

        protected override List<ITarget> GetArea(TowerController owner, ITarget mainTarget)
        {
            if (mainTarget == null) return new List<ITarget>();
            return SkillArea.Impact(m_grid, m_occupancy, mainTarget.PathIndex, owner.Stats.Mastery);
        }

        protected override void OnHitTarget(TowerController owner, ITarget target, DamageInfo hit)
        {
            DamageInfo smash = hit;
            smash.ChangesMovement = true;
            // [B] 피해 주기 — ShieldSplashSkill.OnHitTarget의 HitSingle 줄과 같은 모양 (넘길 것은 바로 위에서 만든 smash)
            if (target.Body != null) ApplySlow(owner, target.Body);
        }

        // 최대 이동력 −1, 레벨 × 2 행동. 같은 키라 다시 걸리면 유지만 처음부터
        public static void ApplySlow(TowerController owner, EnemyController enemy)
        {
            if (!enemy.IsAlive) return;
            enemy.Status.Add(new TimedModifierEffect(Key, owner.Stats.ColorLevel * 2, 1f, "최대 이동력 −1",
                StatModifier.Flat(Key, StatType.SpeedMax, -1f)));
        }
    }
}
