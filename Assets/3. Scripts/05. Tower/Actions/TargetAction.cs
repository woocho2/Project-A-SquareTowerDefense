using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace SquareTD
{
    // 단일 타워의 기본 공격: 고른 적에게 여러 발
    public class TargetAction : TowerAction
    {
        public TargetAction(BoardGrid grid, PathOccupancy occupancy, IProjectileLauncher launcher) : base(grid, occupancy, launcher) { }

        public static int GetShotsPerAttack(int mastery) => mastery * 2;

        public override bool CanAct(TowerController tower, TowerFinalStats stats)
        {
            return CollectTargetsInRange(tower, stats.Range).Count > 0;
        }

        public override IEnumerator Act(TowerController tower, TowerFinalStats stats, AttackHandle handle)
        {
            int volleyCount = stats.AttackCount;
            int shotsPerVolley = GetShotsPerAttack(stats.Mastery);
            ShotTiming timing = tower.ShotTiming;
            Vector3 from = tower.transform.position;
            int lookId = ProjectileCatalog.GetLookId(tower.Definition.Id);
            float speed = tower.Definition.ProjectileSpeed;
            TargetSelector selector = TargetSelector.For(tower.State.TargetPriority);
            Vector3Int towerCell = m_grid.GetSpawnCell(tower.SlotIndex);
            var incoming = new Dictionary<ITarget, float>();

            for (int s = 0; s < volleyCount; s++)
            {
                for (int j = 0; j < shotsPerVolley; j++)
                {
                    if (j > 0 && timing.ShotGap > 0f) yield return new WaitForSeconds(timing.ShotGap);

                    List<ITarget> inRange = CollectTargetsInRange(tower, stats.Range);
                    if (inRange.Count == 0) yield break;

                    ITarget shotTarget = PickTarget(selector, inRange, incoming, towerCell, tower.State.AttackCellIndex);
                    bool isCritical = CriticalRoll.Roll(CriticalRoll.RateAgainst(tower, stats, shotTarget));
                    DamageInfo hit = MakeBasicHit(tower, stats, shotTarget, isCritical);
                    float expected = shotTarget.PreviewDamage(hit);
                    AddIncoming(incoming, shotTarget, expected);

                    handle.AddShot();
                    m_launcher.Fire(lookId, from, shotTarget.Transform.position, speed, 0f, 1f, () =>
                    {
                        AddIncoming(incoming, shotTarget, -expected);
                        tower.CountSkillHit(shotTarget);
                        ProjectileHitResolver.HitSingle(shotTarget, hit);
                        tower.OnBasicHitLanded(shotTarget);
                        handle.CompleteShot();
                    });
                }

                if (s < volleyCount - 1 && timing.VolleyInterval > 0f)
                {
                    yield return new WaitForSeconds(timing.VolleyInterval);
                }
            }
        }

        private ITarget PickTarget(TargetSelector selector, List<ITarget> inRange, Dictionary<ITarget, float> incoming, Vector3Int towerCell, int attackCellIndex)
        {
            var survivors = new List<ITarget>();
            foreach (ITarget target in inRange)
            {
                incoming.TryGetValue(target, out float planned);
                if (target.CurrentHp - planned > 0f) survivors.Add(target);
            }

            if (survivors.Count == 0) survivors = inRange;
            IReadOnlyList<ITarget> candidates = TargetSelector.PreferAttackCell(survivors, attackCellIndex);
            return selector.Select(candidates, towerCell, m_grid);
        }

        private static void AddIncoming(Dictionary<ITarget, float> incoming, ITarget target, float amount)
        {
            incoming.TryGetValue(target, out float current);
            incoming[target] = current + amount;
        }
    }
}
