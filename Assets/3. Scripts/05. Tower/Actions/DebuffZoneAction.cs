using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SquareTD
{
    // 디버프 타워의 행동: 스킬 카운트가 차면 적 전체에게 스킬 2. 스킬 1은 존을 밟은 적에게 (Step)
    public class DebuffZoneAction : TowerAction
    {
        private readonly ZoneRegistry m_zones;
        private readonly Func<IEnumerable<EnemyController>> m_enemies;

        public DebuffZoneAction(BoardGrid grid, PathOccupancy occupancy, ZoneRegistry zones, Func<IEnumerable<EnemyController>> enemies = null)
            : base(grid, occupancy, null)
        {
            m_zones = zones;
            m_enemies = enemies ?? AliveEnemies;
        }

        private static IEnumerable<EnemyController> AliveEnemies() =>
            EnemyManager.Instance != null ? EnemyManager.Instance.Registry.Alive : Array.Empty<EnemyController>();

        public override bool CanAct(TowerController tower, TowerFinalStats stats) => true;

        public override IEnumerator Act(TowerController tower, TowerFinalStats stats, AttackHandle handle)
        {
            int fires = SupportSkillCount.Advance(tower);
            for (int i = 0; i < fires; i++) FireSkill2(tower);
            yield break;
        }

        public void FireSkill2(TowerController tower)
        {
            SupportCast cast = SupportSkillCount.TakeSkill2Cast(tower);
            DebuffRule rule = SkillRegistry.GetDebuffRule(tower.Definition.Emblem);

            int count = 0;
            var visualPositions = new List<Vector3>();
            foreach (EnemyController enemy in new List<EnemyController>(m_enemies()))
            {
                if (enemy == null || !enemy.IsAlive) continue;
                if (Application.isPlaying) visualPositions.Add(enemy.transform.position);
                Apply(rule, cast, enemy, -1);
                count++;
            }
            rule.OnSkill2(cast);
            SkillEffectTable.PlayDebuffSkill(tower, visualPositions);
            Debug.Log($"[Debuff] {tower.name} 스킬 2 발동 → 적 {count}마리, 값 {cast.Amount:0.##}, {cast.Duration}번 행동 동안");
        }

        public override void OnRemoved(TowerController tower) => m_zones.RemoveByOwner(tower);

        public static void Step(Zone zone, EnemyController enemy)
        {
            if (enemy != null) SkillEffectTable.PlaySupportContact(zone.Owner, enemy.transform.position, true);
            Apply(SkillRegistry.GetDebuffRule(zone.Emblem), SupportCast.Skill1(zone.Owner), enemy, zone.CellIndex);
        }

        public static void Apply(DebuffRule rule, SupportCast cast, EnemyController enemy, int zoneCell)
        {
            rule.OnApply(cast, enemy);
            StatusEffect effect = rule.CreateEffect(cast, enemy);
            if (effect == null || !enemy.IsAlive) return;
            if (effect is EmblemDebuffEffect debuff) debuff.ZoneCell = zoneCell;
            enemy.Status.Add(effect);
        }
    }
}
