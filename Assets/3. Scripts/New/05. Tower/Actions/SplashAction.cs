using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;

namespace SquareTD
{
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
            int satelliteCount = stats.AttackCount;
            SatelliteSettings satellite = tower.Satellite;
            float hold = (satelliteCount - 1) * satellite.Interval + satellite.LaunchDelay;
            int lookId = ProjectileCatalog.GetLookId(tower.Definition.Id);
            float speed = tower.Definition.ProjectileSpeed;
            int radius = GetAreaRadius(stats.Mastery);
            float effectScale = GetAreaSide(stats.Mastery);
            TargetSelector selector = TargetSelector.For(tower.State.TargetPriority);
            Vector3Int towerCell = m_grid.GetSpawnCell(tower.SlotIndex);

            for (int s = 0; s < satelliteCount; s++)
            {
                List<ITarget> inRange = CollectTargetsInRange(tower, stats.Range);
                if (inRange.Count == 0) yield break;

                ITarget aimed = selector.Select(TargetSelector.PreferAttackCell(inRange, tower.State.AttackCellIndex), towerCell, m_grid);
                Vector3Int impactCell = m_grid.GetPathCell(aimed.PathIndex);
                bool isCritical = CriticalRoll.Roll(stats.CriticalRate);
                Vector3 from = tower.transform.position + SatelliteLayout.GetOffset(s, satelliteCount, satellite.Radius);

                handle.AddShot();
                m_launcher.Fire(lookId, from, aimed.Transform.position, speed, hold, effectScale, () =>
                                {
                                    foreach (ITarget target in CollectTargetsInSquare(impactCell, radius))
                                    {
                                        ProjectileHitResolver.HitSingle(target, MakeBasicHit(tower, stats, target, isCritical));
                                    }
                                    handle.CompleteShot();
                                });

                if (s < satelliteCount - 1 && satellite.Interval > 0f)
                {
                    yield return new WaitForSeconds(satellite.Interval);
                }
            }
        }
    }
}
