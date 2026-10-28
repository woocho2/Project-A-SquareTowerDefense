using System.Collections.Generic;

namespace SquareTD
{
    // 불 단일 — 화염구 (20번 4-9장)
    //   그 적에게 Power × AbilityValue (스택을 쓰면 ×). 60% × (1 − 저항력%) 확률로 화상 — 그 적이 (레벨 × 2)번 행동하는 동안
    //   이 화상은 스택 1로 고정: 세기가 아니라 "화상이 붙어 있다"는 상태 (메테오와 불 단일의 추가 피해를 켜는 용도)
    //   불 단일의 맞물림: 화상인 적에게 +10%, 연소된 적에게 +20% — 기본 공격과 화염구 모두
    public class FireTargetSkill : AttackSkill
    {
        public const float BurnChance = 0.6f;
        public const float BurnBonus = 0.1f;
        public const float BlazeBonus = 0.2f;

        public FireTargetSkill(BoardGrid grid, PathOccupancy occupancy, StackModeSettings stackModes)
            : base(grid, occupancy, stackModes) { }

        protected override List<ITarget> GetArea(TowerController owner, ITarget mainTarget) => SkillArea.Single(mainTarget);

        protected override float ModifyOutgoingDamage(TowerController owner, ITarget target) => BurnMultiplier(target);
        public override float ModifyBasicDamage(TowerController owner, ITarget target) => BurnMultiplier(target);

        private static float BurnMultiplier(ITarget target)
        {
            ZoneStage burn = EmblemDebuffEffect.StageOn(target, TowerEmblem.Fire);
            if (burn == ZoneStage.Skill2) return 1f + BlazeBonus;
            if (burn == ZoneStage.Skill1) return 1f + BurnBonus;
            return 1f;
        }

        protected override void OnHitTarget(TowerController owner, ITarget target, DamageInfo hit)
        {
            ProjectileHitResolver.HitSingle(target, hit);

            EnemyController enemy = target.Body;
            if (enemy == null || !enemy.IsAlive) return;
            if (Chance.Roll(enemy.Resistance.ResistWeak(BurnChance))) enemy.Status.Add(FireDebuffEffect.Fireball(owner));
        }
    }
}
