using System.Collections.Generic;
using UnityEngine;

namespace SquareTD
{
    // 최종 보스 아홉 종의 기믹 실행과 적 턴 기반 만료
    public sealed class BossMechanicService
    {
        public int CurrentTurn { get; private set; }
        public System.Func<bool> Blocker { get; set; }
        private readonly List<EnemyController> m_summons = new List<EnemyController>();

        public void BeginTurn() => CurrentTurn++;

        public static bool CanTarget(TowerController tower, BossMechanicKind kind)
        {
            if (tower == null || tower.State == null || tower.Definition == null) return false;
            if (TowerId.IsSynergy(tower.Definition.Id) || tower.State.BossEffects.BlocksProgression) return false;
            if (kind == BossMechanicKind.Fafnir && tower.Definition.Tier <= 1) return false;
            return kind == BossMechanicKind.Loki || kind == BossMechanicKind.Ymir || kind == BossMechanicKind.Fafnir;
        }

        private static List<TowerController> Towers()
        {
            var towers = new List<TowerController>();
            if (BoardManager.Instance == null || BoardManager.Instance.Slots == null) return towers;
            TowerSlots slots = BoardManager.Instance.Slots;
            for (int i = 0; i < slots.Count; i++)
                if (slots.Get(i) != null) towers.Add(slots.Get(i));
            return towers;
        }

        public void Trigger(EnemyController boss, int dice, int helTarget)
        {
            if (boss == null || !boss.IsAlive || dice <= 0 || boss.Definition.Kind != EnemyKind.FinalBoss) return;
            BossMechanicKind kind = boss.BossMechanic;
            if (kind != BossMechanicKind.None && Blocker != null && Blocker())
            {
                Debug.Log($"[Boss] {boss.DisplayName} 기믹 무력화");
                return;
            }
            switch (kind)
            {
                case BossMechanicKind.Loki:
                case BossMechanicKind.Ymir:
                case BossMechanicKind.Fafnir:
                    ApplySingle(kind, dice);
                    break;
                case BossMechanicKind.ElfChief:
                    if (EnemyManager.Instance != null)
                        foreach (EnemyController enemy in new List<EnemyController>(EnemyManager.Instance.Registry.Alive))
                            if (enemy.IsAlive) enemy.Heal(enemy.MaxHp * dice * 0.1f);
                    break;
                case BossMechanicKind.HighSpirit:
                    if (EnemyManager.Instance != null)
                        foreach (EnemyController enemy in new List<EnemyController>(EnemyManager.Instance.Registry.Alive))
                            if (enemy.IsAlive) enemy.Status.Add(new BossSpiritBlessing(dice, CurrentTurn));
                    break;
                case BossMechanicKind.Jormungandr:
                    if (EnemyManager.Instance != null)
                        m_summons.AddRange(EnemyManager.Instance.SpawnAdditionalNormals(boss.SpawnWave, dice));
                    break;
                case BossMechanicKind.Fenrir:
                    foreach (TowerController tower in Towers()) tower.State.BossEffects.AddFenrir(dice, CurrentTurn);
                    break;
                case BossMechanicKind.Surtr:
                    foreach (TowerController tower in Towers()) tower.State.BossEffects.AddSurtr(dice, CurrentTurn);
                    break;
                case BossMechanicKind.Hel:
                    bool destroy = boss.AddDeathCount(dice, helTarget);
                    Debug.Log($"[Boss] 헬 죽음 카운트 {(destroy ? helTarget : boss.DeathCount)}/{helTarget}");
                    if (destroy && TowerManager.Instance != null) TowerManager.Instance.DestroyAllForBoss();
                    break;
            }
            if (kind != BossMechanicKind.None) Debug.Log($"[Boss] {boss.DisplayName} 기믹 · 눈금 {dice}");
        }

        private void ApplySingle(BossMechanicKind kind, int dice)
        {
            TowerManager manager = TowerManager.Instance;
            if (manager == null) return;
            var candidates = new List<TowerController>();
            foreach (TowerController tower in Towers())
                if (CanTarget(tower, kind) && !manager.IsMergingTower(tower)) candidates.Add(tower);
            if (candidates.Count == 0) return;
            TowerController target = candidates[Random.Range(0, candidates.Count)];
            TowerDefinition original = target.Definition;
            TowerDefinition changed = null;
            if (kind == BossMechanicKind.Loki)
            {
                changed = manager.PickBossTransformation(target);
                if (changed == null) return;
            }
            if (kind == BossMechanicKind.Fafnir && !manager.Database.TryGet(original.Id - 1000, out changed)) return;

            List<Zone> owned = BoardManager.Instance.Zones.GetByOwner(target);
            var cells = new int[owned.Count];
            for (int i = 0; i < cells.Length; i++) cells[i] = owned[i].CellIndex;
            target.State.BossEffects.TryApplySingle(kind, dice, CurrentTurn, original);
            target.State.BossEffects.OriginalZoneCells = cells;
            if (changed != null) manager.TryChangeForBoss(target, changed, kind == BossMechanicKind.Fafnir ? cells : null);
            if (kind == BossMechanicKind.Ymir) target.GetComponent<TowerVisual>()?.SetBossFrozen(true);
            Debug.Log($"[Boss] {kind} → {target.name}, {dice}턴");
            manager.RefreshAfterBossEffects();
        }

        public List<EnemyController> TakeSummons()
        {
            var result = new List<EnemyController>(m_summons);
            m_summons.Clear();
            return result;
        }

        public void EndTurn()
        {
            bool unlocked = false;
            List<TowerController> towers = Towers();
            if (BoardManager.Instance != null && BoardManager.Instance.Bench != null) towers.AddRange(BoardManager.Instance.GetTowers(TowerPlace.Bench));
            foreach (TowerController tower in towers)
            {
                TowerBossEffects effects = tower.State.BossEffects;
                bool locked = effects.BlocksProgression;
                int[] cells = effects.OriginalZoneCells;
                TowerDefinition restore = effects.EndTurn(CurrentTurn);
                if (restore != null && TowerManager.Instance != null) TowerManager.Instance.TryChangeForBoss(tower, restore, cells);
                if (locked && !effects.BlocksProgression)
                {
                    tower.GetComponent<TowerVisual>()?.SetBossFrozen(false);
                    unlocked = true;
                }
            }
            if (EnemyManager.Instance != null)
                foreach (EnemyController enemy in new List<EnemyController>(EnemyManager.Instance.Registry.Alive))
                    if (enemy.IsAlive && enemy.Status.Find(BossSpiritBlessing.Source) is BossSpiritBlessing blessing)
                        blessing.EndTurn(enemy, CurrentTurn);
            if (unlocked && TowerManager.Instance != null) TowerManager.Instance.RefreshAfterBossEffects();
        }
    }
}
