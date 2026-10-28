using System.Collections.Generic;

namespace SquareTD
{
    // 창 단일 — 뽑아찢기 (20번 4-6장, 2026-10-28 기획 변경)
    //   창 꽂기: 이 타워의 기본 공격이 적에게 맞을 때마다 그 적에게 창 1개 (OnBasicHit). 타워마다 따로 → 스킬(타워마다 하나)이 들고 있음
    //   스킬 2: 이 타워의 창이 꽂힌 적 전부에게 창 수 × Power × AbilityValue (스택을 쓰면 ×). 치명 한 번. 맞은 적의 창은 다 뽑음(0)
    public class SpearTargetSkill : AttackSkill
    {
        // 적은 풀에서 다시 쓰이므로 소환된 웨이브도 함께 적어 둠. 웨이브가 다르면 같은 오브젝트여도 새 적 (창 0)
        private struct Spears
        {
            public int Count;
            public int SpawnWave;
        }

        private readonly Dictionary<ITarget, Spears> m_spears = new Dictionary<ITarget, Spears>();

        public SpearTargetSkill(BoardGrid grid, PathOccupancy occupancy, StackModeSettings stackModes)
            : base(grid, occupancy, stackModes) { }

        public override bool NeedsLivingTarget => false;     // 맵의 창 꽂힌 적을 다 뽑으므로 발동시킨 적이 죽었어도 씀

        public int GetSpears(ITarget target)
        {
            if (target == null || !m_spears.TryGetValue(target, out Spears spears)) return 0;
            return spears.SpawnWave == SpawnWaveOf(target) ? spears.Count : 0;
        }

        // 기본 공격이 맞은 뒤 (TowerController.OnBasicHitLanded). 이 탄에 죽은 적에게는 꽂지 않음
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

        // 꽂힌 창 수만큼 곱함
        protected override float ModifyOutgoingDamage(TowerController owner, ITarget target)
        {
            return GetSpears(target);
        }

        protected override void OnHitTarget(TowerController owner, ITarget target, DamageInfo hit)
        {
            m_spears.Remove(target);                        // 다 뽑음
            ProjectileHitResolver.HitSingle(target, hit);
        }

        private static int SpawnWaveOf(ITarget target) => target.Body != null ? target.Body.SpawnWave : 0;
    }
}
