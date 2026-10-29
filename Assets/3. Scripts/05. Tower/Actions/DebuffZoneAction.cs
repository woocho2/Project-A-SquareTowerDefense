using System.Collections;

namespace SquareTD
{
    // 디버프 타워의 행동: 존에 스택을 쌓고 밟은 적에게 디버프
    public class DebuffZoneAction : TowerAction
    {
        private readonly ZoneRegistry m_zones;

        public DebuffZoneAction(BoardGrid grid, PathOccupancy occupancy, ZoneRegistry zones)
            : base(grid, occupancy, null)
        {
            m_zones = zones;
        }

        public override bool CanAct(TowerController tower, TowerFinalStats stats) => true;

        public override IEnumerator Act(TowerController tower, TowerFinalStats stats, AttackHandle handle)
        {
            foreach (Zone zone in m_zones.GetByOwner(tower))
            {
                if (!zone.IsPlaced) continue;

                ZoneStage before = zone.Stage;

                zone.AddStack(stats.AttackCount, stats.Stack);

                if (zone.Stage != before) m_zones.NotifyChanged(zone);
            }
            yield break;
        }

        public override void OnRemoved(TowerController tower) => m_zones.RemoveByOwner(tower);

        public static void Step(Zone zone, EnemyController enemy, ZoneRegistry zones)
        {
            TowerFinalStats stats = zone.Owner.Stats;
            ZoneStage before = zone.Stage;
            zone.AddStack(stats.AttackCount, stats.Stack);
            if (zone.Stage != before) zones.NotifyChanged(zone);

            DebuffRule rule = SkillRegistry.GetDebuffRule(zone.Emblem);
            rule.OnStep(zone, enemy);
            enemy.Status.Add(rule.CreateEffect(zone, enemy));
        }
    }
}
