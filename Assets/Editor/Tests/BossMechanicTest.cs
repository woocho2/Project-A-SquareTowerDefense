using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace SquareTD.Tests
{
    public class BossMechanicTest
    {
        private readonly List<Object> m_objects = new List<Object>();
        private const float Eps = 0.001f;

        private sealed class PurifyTestDebuff : StatusEffect
        {
            public override string Key => "정화할 영구 디버프";
            public override bool IsPermanent => true;
            public PurifyTestDebuff() : base(1) { }
            public override string Describe() => Key;
        }

        private T Track<T>(T item) where T : Object { m_objects.Add(item); return item; }
        private static TowerDefinition Def(int id = 1101, int attacks = 2) =>
            new TowerDefinition(id, 100f, 2, 4, attacks, 0f, 0f, 1, 60, 5, 2f, 5f, TargetPriority.Closest);

        [TearDown]
        public void Cleanup()
        {
            for (int i = m_objects.Count - 1; i >= 0; i--)
                if (m_objects[i] != null) Object.DestroyImmediate(m_objects[i]);
            m_objects.Clear();
        }

        private TowerController Tower(int id = 1101, int slot = 0)
        {
            var go = Track(new GameObject("BossTestTower"));
            var tower = go.AddComponent<TowerController>();
            tower.Init(Def(id), slot, null);
            return tower;
        }

        private EnemyController Enemy(BossMechanicKind kind, EnemyKind enemyKind = EnemyKind.FinalBoss)
        {
            var definition = Track(ScriptableObject.CreateInstance<EnemyDefinition>());
            var so = new SerializedObject(definition);
            so.FindProperty("<Kind>k__BackingField").enumValueIndex = (int)enemyKind;
            so.FindProperty("<MaxHP>k__BackingField").floatValue = 1000;
            so.FindProperty("<Defense>k__BackingField").floatValue = 100;
            so.FindProperty("<Action>k__BackingField").intValue = 3;
            so.FindProperty("<BaseResistance>k__BackingField").floatValue = 40;
            so.ApplyModifiedPropertiesWithoutUndo();
            var enemy = Track(new GameObject("BossTestEnemy")).AddComponent<EnemyController>();
            enemy.Setup(definition, null, 10, "TestBoss", kind);
            return enemy;
        }

        [Test]
        public void 펜리르_중첩은_각자_만료되고_추가_Action은_최대_다섯()
        {
            var effects = new TowerBossEffects();
            effects.AddFenrir(2, 0);
            effects.AddFenrir(4, 1);
            Assert.AreEqual(2, effects.ActionPenalty);
            effects.EndTurn(2);
            Assert.AreEqual(1, effects.ActionPenalty);
            effects.EndTurn(5);
            Assert.AreEqual(0, effects.ActionPenalty);
            for (int i = 0; i < 8; i++) effects.AddFenrir(3, 5);
            Assert.AreEqual(5, effects.ActionPenalty);
            effects.EndTurn(8);
            Assert.AreEqual(0, effects.ActionPenalty);
        }

        [Test]
        public void 수르트_공격력은_활성_최댓값이고_공격횟수는_세번까지_중첩()
        {
            var effects = new TowerBossEffects();
            effects.AddSurtr(8, 0);
            effects.AddSurtr(3, 6);
            effects.AddSurtr(2, 6);
            effects.AddSurtr(2, 6);
            Assert.AreEqual(0.4f, effects.PowerReduction, Eps);
            Assert.AreEqual(3, effects.AttackCountPenalty);
            effects.EndTurn(8);
            Assert.AreEqual(0.15f, effects.PowerReduction, Eps);
            Assert.AreEqual(1, effects.AttackCountPenalty);
            effects.EndTurn(9);
            Assert.AreEqual(0f, effects.PowerReduction);
            Assert.AreEqual(0, effects.AttackCountPenalty);
        }

        [TestCase(1101)]
        [TestCase(1201)]
        [TestCase(1301)]
        [TestCase(1401)]
        public void 전체_타워에_펜리르와_수르트가_적용되고_공격횟수는_최소_하나(int id)
        {
            TowerDefinition def = Def(id, 1);
            var state = new TowerState(def);
            TowerFinalStats before = TowerStatCalculator.Calculate(def, def, state, 1);
            state.BossEffects.AddFenrir(5, 0);
            for (int i = 0; i < 3; i++) state.BossEffects.AddSurtr(4, 0);
            TowerFinalStats after = TowerStatCalculator.Calculate(def, def, state, 1);
            Assert.AreEqual(before.Action + 1, after.Action);
            Assert.AreEqual(before.Power * 0.8f, after.Power, Eps);
            Assert.AreEqual(1, after.AttackCount);
        }

        [Test]
        public void 합성_결과에는_펜리르와_수르트의_기존_타이머가_없음()
        {
            var old = new TowerState(Def());
            old.BossEffects.AddFenrir(8, 1);
            old.BossEffects.AddSurtr(8, 1);
            var merged = new TowerState(Def(2101));
            merged.CopyInheritedFrom(old);
            Assert.AreEqual(0, merged.BossEffects.ActionPenalty);
            Assert.AreEqual(0, merged.BossEffects.AttackCountPenalty);
            old.BossEffects.EndTurn(9);
            merged.BossEffects.AddFenrir(3, 9);
            Assert.AreEqual(1, merged.BossEffects.ActionPenalty);
        }

        [TestCase(BossMechanicKind.Loki)]
        [TestCase(BossMechanicKind.Ymir)]
        [TestCase(BossMechanicKind.Fafnir)]
        public void 단일_기믹은_재적용으로_연장하거나_다른_단일_기믹과_중첩하지_않음(BossMechanicKind kind)
        {
            var effects = new TowerBossEffects();
            TowerDefinition original = Def(2101);
            Assert.IsTrue(effects.TryApplySingle(kind, 3, 1, original));
            Assert.IsFalse(effects.TryApplySingle(BossMechanicKind.Ymir, 8, 2, original));
            Assert.IsNull(effects.EndTurn(3));
            TowerDefinition restored = effects.EndTurn(4);
            Assert.AreEqual(kind == BossMechanicKind.Ymir ? null : original, restored);
            Assert.IsFalse(effects.BlocksProgression);
        }

        [Test]
        public void 이미르_빙결_중에는_Action카운트와_턴시작_공격을_진행하지_않음()
        {
            TowerController tower = Tower();
            int remaining = tower.State.RemainingAction;
            tower.State.BossEffects.TryApplySingle(BossMechanicKind.Ymir, 2, 0, tower.Definition);
            for (int i = 0; i < 3; i++) Assert.IsFalse(tower.TickAction());
            Assert.AreEqual(remaining, tower.State.RemainingAction);
            tower.State.BossEffects.EndTurn(2);
            tower.TickAction();
            Assert.AreEqual(remaining - 1, tower.State.RemainingAction);
        }

        [TestCase(BossMechanicKind.Loki)]
        [TestCase(BossMechanicKind.Ymir)]
        [TestCase(BossMechanicKind.Fafnir)]
        public void 단일_기믹_중인_타워는_자동합성에서_빠짐(BossMechanicKind kind)
        {
            var slots = new TowerSlots(3);
            for (int i = 0; i < 3; i++) slots.TryPlace(Tower(2101, i), i);
            slots.Get(0).State.BossEffects.TryApplySingle(kind, 3, 0, slots.Get(0).Definition);
            Assert.IsFalse(TowerProgression.TryFindMergeGroup(slots, null, out _, out _));
            slots.Get(0).State.BossEffects.EndTurn(3);
            Assert.IsTrue(TowerProgression.TryFindMergeGroup(slots, null, out _, out _));
        }

        [Test]
        public void 파프니르는_일티어와_시너지와_다른_단일_기믹의_대상을_제외함()
        {
            Assert.IsFalse(BossMechanicService.CanTarget(Tower(1101), BossMechanicKind.Fafnir));
            Assert.IsFalse(BossMechanicService.CanTarget(Tower(6001), BossMechanicKind.Fafnir));
            TowerController target = Tower(2101);
            Assert.IsTrue(BossMechanicService.CanTarget(target, BossMechanicKind.Fafnir));
            target.State.BossEffects.TryApplySingle(BossMechanicKind.Loki, 3, 0, target.Definition);
            Assert.IsFalse(BossMechanicService.CanTarget(target, BossMechanicKind.Fafnir));
            Assert.IsFalse(BossMechanicService.CanTarget(target, BossMechanicKind.Ymir));
        }

        [TestCase(1, BossMechanicKind.Loki)]
        [TestCase(2, BossMechanicKind.ElfChief)]
        [TestCase(3, BossMechanicKind.HighSpirit)]
        [TestCase(4, BossMechanicKind.Jormungandr)]
        [TestCase(5, BossMechanicKind.Ymir)]
        [TestCase(6, BossMechanicKind.Fafnir)]
        [TestCase(7, BossMechanicKind.Fenrir)]
        [TestCase(8, BossMechanicKind.Surtr)]
        [TestCase(9, BossMechanicKind.Hel)]
        public void 스테이지별_최종보스_기믹을_고름(int number, BossMechanicKind kind)
        {
            var stage = Track(ScriptableObject.CreateInstance<StageDefinition>());
            var so = new SerializedObject(stage);
            so.FindProperty("<Number>k__BackingField").intValue = number;
            so.ApplyModifiedPropertiesWithoutUndo();
            Assert.AreEqual(kind, stage.GetBossMechanic(1));
        }

        [Test]
        public void 라그나로크는_사이클별_원래_보스_기믹을_사용함()
        {
            var stage = Track(ScriptableObject.CreateInstance<StageDefinition>());
            var cycles = new StageDefinition[4];
            int[] numbers = { 4, 7, 9, 8 };
            for (int i = 0; i < 4; i++)
            {
                cycles[i] = Track(ScriptableObject.CreateInstance<StageDefinition>());
                typeof(StageDefinition).GetProperty("Number").SetValue(cycles[i], numbers[i]);
            }
            typeof(StageDefinition).GetProperty("RagnarokCycles").SetValue(stage, cycles);
            for (int i = 0; i < 4; i++) Assert.AreEqual((BossMechanicKind)numbers[i], stage.GetBossMechanic(i + 1));
        }

        [Test]
        public void 상급정령_강화는_행동수가_아닌_적턴으로_끝나고_스탯이_복구됨()
        {
            EnemyController enemy = Enemy(BossMechanicKind.HighSpirit);
            float hp = enemy.MaxHp;
            float defense = enemy.Stats.Get(StatType.Defense);
            var blessing = new BossSpiritBlessing(2, 1);
            enemy.Status.Add(blessing);
            Assert.AreEqual(hp * 1.5f, enemy.MaxHp, Eps);
            Assert.AreEqual(defense * 1.5f, enemy.Stats.Get(StatType.Defense), Eps);
            Assert.AreEqual(60f, enemy.Resistance.Base, Eps);
            for (int i = 0; i < 5; i++) enemy.Status.OnActed();
            Assert.AreEqual(2, blessing.RemainingActions);
            blessing.EndTurn(enemy, 1);
            Assert.AreEqual(2, blessing.RemainingActions);
            blessing.EndTurn(enemy, 2);
            Assert.AreEqual(1, blessing.RemainingActions);
            blessing.EndTurn(enemy, 3);
            Assert.AreEqual(hp, enemy.MaxHp, Eps);
            Assert.AreEqual(defense, enemy.Stats.Get(StatType.Defense), Eps);
            Assert.AreEqual(40f, enemy.Resistance.Base, Eps);
        }

        [Test]
        public void 정령_저항력_상한과_버프해제는_원래_저항력_누적을_보존함()
        {
            var resistance = new EnemyResistance(80f);
            resistance.SetBossMultiplier(1.5f);
            Assert.AreEqual(100f, resistance.Base);
            Assert.AreEqual(100f, resistance.Current);
            resistance.SetBossMultiplier(1f);
            Assert.AreEqual(80f, resistance.Base);
            Assert.AreEqual(80f, resistance.Current);
        }

        [Test]
        public void 정화_타일은_디버프만_제거하고_정령의_강화는_유지함()
        {
            EnemyController enemy = Enemy(BossMechanicKind.HighSpirit);
            float hp = enemy.MaxHp;
            enemy.Status.Add(new BossSpiritBlessing(3, 0));
            enemy.Status.Add(new PurifyTestDebuff());
            new PurifyTileEffect().OnStopped(enemy, null, null);
            Assert.IsTrue(enemy.Status.Has(BossSpiritBlessing.Source));
            Assert.IsFalse(enemy.Status.Has("정화할 영구 디버프"));
            Assert.AreEqual(hp * 1.5f, enemy.MaxHp, Eps);
            Assert.AreEqual(60f, enemy.Resistance.Current, Eps);
        }

        [TestCase(70)]
        [TestCase(50)]
        public void 헬은_목표이상에서_파괴를_발동하고_카운트를_초기화함(int target)
        {
            EnemyController enemy = Enemy(BossMechanicKind.Hel);
            int total = 0;
            while (total + 8 < target) { Assert.IsFalse(enemy.AddDeathCount(8, target)); total += 8; }
            Assert.AreEqual(total, enemy.DeathCount);
            Assert.IsTrue(enemy.AddDeathCount(8, target));
            Assert.AreEqual(0, enemy.DeathCount);
            Assert.IsFalse(enemy.AddDeathCount(3, target));
            Assert.AreEqual(3, enemy.DeathCount);
            enemy.Setup(enemy.Definition, null, 20, "Respawn", BossMechanicKind.Hel);
            Assert.AreEqual(0, enemy.DeathCount);
        }

        [Test]
        public void 헬의_목표는_노말_칠십_하드_오십()
        {
            var go = Track(new GameObject("TestSession"));
            go.SetActive(false);
            var session = go.AddComponent<GameSession>();
            Assert.AreEqual(70, session.HelDeathTarget);
            typeof(GameSession).GetField("m_mode", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(session, GameMode.Hard);
            Assert.AreEqual(50, session.HelDeathTarget);
        }
    }
}
