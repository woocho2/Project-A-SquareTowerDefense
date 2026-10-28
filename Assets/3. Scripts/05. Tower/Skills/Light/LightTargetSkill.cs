using System.Collections.Generic;

namespace SquareTD
{
    // 빛 단일 — 도깨비불 (20번 4-15장)
    //   이 타워의 공격이 적에게 맞을 때마다 그 적에게 도깨비불 1 (이 타워가 따로 셈, 그 턴 안에서만 — 다음 적 턴 ⓪에 비움)
    //   공격의 피해 × (1 + 그 적의 도깨비불 × 2%). 더미면 스택당 4%. 공격 횟수 8 · 발 수 4 = 32발이면 마지막 발 +64%
    //   스킬 2: 그 적에게 Power × AbilityValue (스택을 쓰면 ×), 이 피해에도 도깨비불이 붙음
    //   탄은 쏠 때 피해를 정하므로 도깨비불도 쏠 때 쌓음 (TargetAction이 MakeBasicHit에서 ModifyBasicDamage를 부름)
    public class LightTargetSkill : AttackSkill
    {
        public const float RatePerMark = 0.02f;
        public const float DummyRatePerMark = 0.04f;

        private readonly Dictionary<ITarget, int> m_marks = new Dictionary<ITarget, int>();

        public LightTargetSkill(BoardGrid grid, PathOccupancy occupancy, StackModeSettings stackModes)
            : base(grid, occupancy, stackModes) { }

        public int GetMarks(ITarget target) => target != null && m_marks.TryGetValue(target, out int marks) ? marks : 0;

        // 한 번 맞을 때마다 1 쌓고, 쌓인 수만큼의 배율을 돌려줌
        private float AddMark(ITarget target)
        {
            int marks = GetMarks(target) + 1;
            m_marks[target] = marks;
            return 1f + marks * (target.IsDummy ? DummyRatePerMark : RatePerMark);
        }

        public override float ModifyBasicDamage(TowerController owner, ITarget target) => AddMark(target);
        protected override float ModifyOutgoingDamage(TowerController owner, ITarget target) => AddMark(target);

        public override void OnEnemyTurnStart(TowerController owner) => m_marks.Clear();

        protected override List<ITarget> GetArea(TowerController owner, ITarget mainTarget) => SkillArea.Single(mainTarget);

        protected override void OnHitTarget(TowerController owner, ITarget target, DamageInfo hit)
        {
            // [B] 피해 주기 — SwordSplashSkill.OnHitTarget과 똑같은 한 줄
        }
    }
}
