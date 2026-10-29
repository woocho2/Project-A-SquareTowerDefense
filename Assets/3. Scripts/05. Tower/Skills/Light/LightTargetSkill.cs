using System.Collections.Generic;

namespace SquareTD
{
    // 빛 단일 — 도깨비불
    public class LightTargetSkill : AttackSkill
    {
        public const float RatePerMark = 0.02f;
        public const float DummyRatePerMark = 0.04f;

        private readonly Dictionary<ITarget, int> m_marks = new Dictionary<ITarget, int>();

        public LightTargetSkill(BoardGrid grid, PathOccupancy occupancy, StackModeSettings stackModes)
            : base(grid, occupancy, stackModes) { }

        public int GetMarks(ITarget target) => target != null && m_marks.TryGetValue(target, out int marks) ? marks : 0;

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
            ProjectileHitResolver.HitSingle(target, hit);
        }
    }
}
