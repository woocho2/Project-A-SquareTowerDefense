using System.Collections.Generic;
using UnityEngine;

namespace SquareTD
{
    // 시너지 타워의 효과 (적 턴마다)
    public static class SynergyEffects
    {
        public const int OdinId = 6001;
        public const int ThorId = 6002;
        public const int LokiId = 6003;
        public const int HelId = 6004;
        public const float HelDrainRate = 0.02f;
        public const int SurtrId = 6005;
        public const float SurtrLostHpRate = 0.1f;
        public const int FreyId = 6006;
        public const string FreySource = "프레이의 가호";
        public const int FreyActions = 1;
        public const float PercentPerTier = 0.5f;

        public static float TierValue(int tierSum) => tierSum * PercentPerTier;

        public static float TierRate(int tierSum) => TierValue(tierSum) * SkillFormula.PercentRate;

        public static int OdinExecute(IReadOnlyList<EnemyController> enemies, int tierSum)
        {
            float threshold = TierRate(tierSum);
            if (threshold <= 0f || enemies == null) return 0;

            var targets = new List<EnemyController>();
            foreach (EnemyController enemy in enemies)
                if (enemy != null && enemy.IsAlive && enemy.CurrentHp <= enemy.MaxHp * threshold) targets.Add(enemy);

            foreach (EnemyController enemy in targets) enemy.Execute(new DamageInfo { Source = DamageSource.Dot });
            return targets.Count;
        }

        public static int PickThorStormIndex(int pathCount) => pathCount > 0 ? UnityEngine.Random.Range(0, pathCount) : -1;

        public static int ThorStorm(PathOccupancy occupancy, int pathIndex, int tierSum)
        {
            float rate = TierRate(tierSum);
            if (rate <= 0f || occupancy == null || pathIndex < 0) return 0;

            var targets = new List<ITarget>(occupancy.GetAt(pathIndex));
            int hits = 0;
            foreach (ITarget target in targets)
            {
                if (target == null || !target.IsAlive) continue;
                target.TakeDamage(new DamageInfo { BaseDamage = target.MaxHp * rate, Source = DamageSource.Dot, ForceFixed = true });
                hits++;
            }
            return hits;
        }

        public static List<EnemyController> LokiCandidates(IReadOnlyList<EnemyController> enemies)
        {
            var candidates = new List<EnemyController>();
            if (enemies == null) return candidates;
            foreach (EnemyController enemy in enemies)
                if (enemy != null && enemy.IsAlive && !enemy.IsBoss) candidates.Add(enemy);
            return candidates;
        }

        public static int NearestPathIndex(BoardGrid grid, int slotIndex)
        {
            Vector3Int towerCell = grid.GetSpawnCell(slotIndex);
            int best = -1, bestDistance = int.MaxValue;
            for (int i = 0; i < grid.PathCount; i++)
            {
                int distance = BoardGrid.GetTileDistance(grid.GetPathCell(i), towerCell);
                if (distance >= bestDistance) continue;
                best = i;
                bestDistance = distance;
            }
            return best;
        }

        public static bool LokiPull(IReadOnlyList<EnemyController> enemies, IReadOnlyList<TowerController> attackTowers, BoardGrid grid)
        {
            List<EnemyController> candidates = LokiCandidates(enemies);
            if (candidates.Count == 0 || attackTowers == null || attackTowers.Count == 0 || grid == null) return false;

            EnemyController enemy = candidates[UnityEngine.Random.Range(0, candidates.Count)];
            TowerController tower = attackTowers[UnityEngine.Random.Range(0, attackTowers.Count)];
            if (enemy.Movement == null || tower == null) return false;

            enemy.Movement.TeleportTo(NearestPathIndex(grid, tower.SlotIndex));
            return true;
        }

        public static bool HelDrain(EnemyController enemy)
        {
            if (enemy == null || !enemy.IsAlive) return false;
            enemy.TakeDamage(new DamageInfo { BaseDamage = enemy.MaxHp * HelDrainRate, Source = DamageSource.Dot, ForceFixed = true });
            return true;
        }

        public static int HelExecuteAll(IReadOnlyList<EnemyController> enemies)
        {
            var targets = LokiCandidates(enemies);
            foreach (EnemyController enemy in targets) enemy.Execute(new DamageInfo { Source = DamageSource.Dot });
            return targets.Count;
        }

        public static int FreyBless(IReadOnlyList<TowerController> towers)
        {
            if (towers == null) return 0;
            int count = 0;
            foreach (TowerController tower in towers)
            {
                if (tower == null || tower.Definition == null || TowerId.IsSynergy(tower.Definition.Id)) continue;
                TimedTowerBuff.Apply(tower.State, FreySource, FreyActions, StatModifier.Flat(FreySource, StatType.AttackCount, 1f));
                count++;
            }
            return count;
        }

        public static int SurtrBurn(IReadOnlyList<EnemyController> enemies)
        {
            const float rate = SurtrLostHpRate;
            if (enemies == null) return 0;

            var targets = new List<EnemyController>();
            foreach (EnemyController enemy in enemies) if (enemy != null && enemy.IsAlive) targets.Add(enemy);

            int hits = 0;
            foreach (EnemyController enemy in targets)
            {
                float lost = enemy.MaxHp - enemy.CurrentHp;
                if (lost <= 0f) continue;
                enemy.TakeDamage(new DamageInfo { BaseDamage = lost * rate, Source = DamageSource.Dot, ForceFixed = true });
                hits++;
            }
            return hits;
        }
    }

    // 시너지 스택 — 매 적 턴 n(0.5 × 티어 합)씩 쌓고 100이 되면 터지고 0으로 (헬의 데스 카운트, 수르트의 재앙의 검, 프레이의 가호)
    public class SynergyStack
    {
        public const float DefaultMax = 100f;

        public float MaxStacks { get; }
        public float Stacks { get; private set; }
        public float Ratio => Stacks / MaxStacks;

        public SynergyStack(float maxStacks = DefaultMax)
        {
            MaxStacks = Mathf.Max(1f, maxStacks);
        }

        public bool Gain(int tierSum)
        {
            Stacks = Mathf.Min(MaxStacks, Stacks + SynergyEffects.TierValue(tierSum));
            if (Stacks < MaxStacks) return false;

            Stacks = 0f;
            return true;
        }
    }

    // 수르트의 재앙의 검 — 다 차면 맵 위의 적 전체에게 잃은 체력의 10% 고정 피해
    public class SurtrSword
    {
        public SynergyStack Stack { get; } = new SynergyStack();

        public int Tick(IReadOnlyList<EnemyController> enemies, int tierSum) =>
            Stack.Gain(tierSum) ? SynergyEffects.SurtrBurn(enemies) : 0;
    }

    // 프레이의 가호 — 다 차면 판 위의 모든 타워가 다음 행동 한 번 동안 AttackCount +1
    public class FreyBlessing
    {
        public SynergyStack Stack { get; } = new SynergyStack();

        public int Tick(IReadOnlyList<TowerController> towers, int tierSum) =>
            Stack.Gain(tierSum) ? SynergyEffects.FreyBless(towers) : 0;
    }

    // 헬의 데스 카운트 — 매 적 턴 무작위 적 1기의 최대 체력 2%를 깎고, 1000이 차면 보스를 뺀 적 전체 즉사
    public class HelDeathCount
    {
        public const float MaxStacks = 1000f;

        public SynergyStack Stack { get; } = new SynergyStack(MaxStacks);

        public int Tick(IReadOnlyList<EnemyController> enemies, int tierSum)
        {
            var alive = new List<EnemyController>();
            if (enemies != null)
                foreach (EnemyController enemy in enemies) if (enemy != null && enemy.IsAlive) alive.Add(enemy);
            if (alive.Count > 0) SynergyEffects.HelDrain(alive[Random.Range(0, alive.Count)]);

            return Stack.Gain(tierSum) ? SynergyEffects.HelExecuteAll(enemies) : 0;
        }
    }
}
