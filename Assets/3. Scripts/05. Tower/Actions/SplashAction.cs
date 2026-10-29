using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SquareTD
{
    // 광역 타워의 기본 공격: 착탄 범위의 적 전부에게
    public class SplashAction : TowerAction
    {
        public SplashAction(BoardGrid grid, PathOccupancy occupancy, IProjectileLauncher launcher) : base(grid, occupancy, launcher) { }

        public static int GetAreaRadius(int mastery) => mastery - 1;
        public static int GetAreaSide(int mastery) => GetAreaRadius(mastery) * 2 + 1;

        public override bool CanAct(TowerController tower, TowerFinalStats stats)
        {
            return CollectTargetsInRange(tower, stats.Range).Count > 0;
        }

        public override IEnumerator Act(TowerController tower, TowerFinalStats stats, AttackHandle handle)
        {
            int volleyCount = stats.AttackCount;
            ShotTiming timing = tower.ShotTiming;
            int lookId = ProjectileCatalog.GetLookId(tower.Definition.Id);
            float speed = tower.Definition.ProjectileSpeed;
            int radius = GetAreaRadius(stats.Mastery);
            float effectScale = GetAreaSide(stats.Mastery);
            TargetSelector selector = TargetSelector.For(tower.State.TargetPriority);
            Vector3Int towerCell = m_grid.GetSpawnCell(tower.SlotIndex);

            for (int s = 0; s < volleyCount; s++)
            {
                List<ITarget> inRange = CollectTargetsInRange(tower, stats.Range);
                if (inRange.Count == 0) yield break;

                ITarget aimed = selector.Select(TargetSelector.PreferAttackCell(inRange, tower.State.AttackCellIndex), towerCell, m_grid);
                Vector3Int impactCell = m_grid.GetPathCell(aimed.PathIndex);
                bool isCritical = CriticalRoll.Roll(CriticalRoll.RateAgainst(tower, stats, aimed));
                Vector3 from = tower.transform.position;

                handle.AddShot();
                m_launcher.Fire(lookId, from, aimed.Transform.position, speed, 0f, effectScale, () =>
                                {
                                    foreach (ITarget target in CollectTargetsInSquare(impactCell, radius))
                                    {
                                        tower.CountSkillHit(target);
                                        ProjectileHitResolver.HitSingle(target, MakeBasicHit(tower, stats, target, isCritical));
                                        tower.OnBasicHitLanded(target);
                                    }
                                    handle.CompleteShot();
                                });

                if (s < volleyCount - 1 && timing.VolleyInterval > 0f)
                {
                    yield return new WaitForSeconds(timing.VolleyInterval);
                }
            }
        }
    }
}
