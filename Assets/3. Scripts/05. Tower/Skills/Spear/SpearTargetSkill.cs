using System.Collections.Generic;

namespace SquareTD
{
    // 창 단일 — 뽑아찢기
    public class SpearTargetSkill : AttackSkill
    {
        private struct Spears
        {
            public int Count;
            public int SpawnWave;
        }

        private readonly Dictionary<ITarget, Spears> m_spears = new Dictionary<ITarget, Spears>();

        public SpearTargetSkill(BoardGrid grid, PathOccupancy occupancy, StackModeSettings stackModes)
            : base(grid, occupancy, stackModes) { }

        public override bool NeedsLivingTarget => false;

        public int GetSpears(ITarget target)
        {
            if (target == null || !m_spears.TryGetValue(target, out Spears spears)) return 0;
            return spears.SpawnWave == SpawnWaveOf(target) ? spears.Count : 0;
        }

        public override void OnBasicHit(TowerController owner, ITarget target)
        {
            if (target == null || !target.IsAlive) return;
            m_spears[target] = new Spears { Count = GetSpears(target) + 1, SpawnWave = SpawnWaveOf(target) };
        }

        protected override List<ITarget> GetArea(TowerController owner, ITarget mainTarget)
        {
            var area = new List<ITarget>();
            foreach (ITarget target in SkillArea.WholeMap(m_grid, m_occupancy))
            {
                if (target.IsAlive && GetSpears(target) > 0) area.Add(target);
            }
            return area;
        }

        protected override float ModifyOutgoingDamage(TowerController owner, ITarget target)
        {
            return GetSpears(target);
        }

        protected override void OnHitTarget(TowerController owner, ITarget target, DamageInfo hit)
        {
            m_spears.Remove(target);
            ProjectileHitResolver.HitSingle(target, hit);
        }

        private static int SpawnWaveOf(ITarget target) => target.Body != null ? target.Body.SpawnWave : 0;
    }
}
