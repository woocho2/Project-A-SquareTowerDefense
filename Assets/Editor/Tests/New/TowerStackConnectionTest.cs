using System.Collections;
using NUnit.Framework;
using UnityEngine;

namespace SquareTD.Tests
{
    // 수업 23 2단계: 타워에 붙은 스택 (쌓기, 최대 맞추기)
    public class TowerStackConnectionTest
    {
        // 판 없이 행동만 하는 가짜 공격 방식
        private class FakeAction : TowerAction
        {
            public FakeAction() : base(null, null, null) { }
            public override bool CanAct(TowerController tower, TowerFinalStats stats) => true;
            public override IEnumerator Act(TowerController tower, TowerFinalStats stats, AttackHandle handle) { yield break; }
        }

        private GameObject m_go;

        private TowerController MakeTower(int id, int attackCount = 3, ColorUpgradeState colors = null)
        {
            m_go = new GameObject("TestTower");
            var tower = m_go.AddComponent<TowerController>();
            var definition = new TowerDefinition(id, 10f, 2, 3, attackCount, 0f, 0f, 1, 60, 5, 2f, 5f, TargetPriority.Closest);
            tower.Init(definition, 0, new FakeAction(), null, colors);
            return tower;
        }

        private static void RunAct(TowerController tower)
        {
            IEnumerator act = tower.Act(new AttackHandle());
            while (act.MoveNext()) { }
        }

        [TearDown]
        public void CleanUp()
        {
            if (m_go != null) Object.DestroyImmediate(m_go);
        }

        // ── 쌓기 ──

        [Test]
        public void 공격_타워는_행동할_때마다_AttackCount만큼_쌓임()
        {
            TowerController tower = MakeTower(1101, attackCount: 3);
            RunAct(tower);
            RunAct(tower);
            Assert.AreEqual(6f, tower.State.Stack.Current, 0.0001f);
        }

        [Test]
        public void 단일_타워도_쌓임()
        {
            TowerController tower = MakeTower(1201, attackCount: 2);
            RunAct(tower);
            Assert.AreEqual(2f, tower.State.Stack.Current, 0.0001f);
        }

        [Test]
        public void 버프_디버프_타워는_자기에게_쌓지_않음()
        {
            TowerController buff = MakeTower(1301);
            RunAct(buff);
            Assert.AreEqual(0f, buff.State.Stack.Current, "버프·디버프의 스택은 존이 가짐 (수업 26)");
            CleanUp();

            TowerController debuff = MakeTower(1401);
            RunAct(debuff);
            Assert.AreEqual(0f, debuff.State.Stack.Current);
        }

        [Test]
        public void 다_차면_더_쌓이지_않음()
        {
            TowerController tower = MakeTower(1101, attackCount: 25);
            RunAct(tower);
            RunAct(tower);
            RunAct(tower);
            Assert.AreEqual(60f, tower.State.Stack.Current, 0.0001f);
            Assert.IsTrue(tower.State.Stack.IsFull);
        }

        // ── 최대 맞추기 ──

        [Test]
        public void 브론즈는_CSV의_최대_그대로()
        {
            TowerController tower = MakeTower(1101);
            Assert.AreEqual(60, tower.State.Stack.Max);
        }

        [Test]
        public void 미스릴은_최대가_절반()
        {
            TowerController tower = MakeTower(4101);
            Assert.AreEqual(30, tower.State.Stack.Max, "티어 표: 미스릴 최대 스택 ×1/2 → Init에서 SetMax");
        }

        [Test]
        public void 컬러_레벨_4가_되면_넘치는_스택을_버림()
        {
            var colors = new ColorUpgradeState();
            TowerController tower = MakeTower(1101, attackCount: 50, colors: colors);
            RunAct(tower);
            Assert.AreEqual(50f, tower.State.Stack.Current, 0.0001f);

            colors.TryUpgrade(TowerColor.Red);      // 2
            colors.TryUpgrade(TowerColor.Red);      // 3
            colors.TryUpgrade(TowerColor.Red);      // 4: 최대 스택 ×1/2
            tower.RecalculateStats();               // 판에서는 TowerManager가 LevelChanged를 듣고 부름

            Assert.AreEqual(30, tower.State.Stack.Max);
            Assert.AreEqual(30f, tower.State.Stack.Current, 0.0001f, "50 / 60 → 30 / 30으로 바로 다 참");
            Assert.IsTrue(tower.State.Stack.IsFull);
        }

        [Test]
        public void 다른_수정자로_다시_계산해도_스택은_그대로()
        {
            TowerController tower = MakeTower(1101, attackCount: 10);
            RunAct(tower);
            tower.SetSpawnTile(SpawnTileType.Power);     // Changed → RecalculateStats

            Assert.AreEqual(10f, tower.State.Stack.Current, 0.0001f);
            Assert.AreEqual(60, tower.State.Stack.Max);
        }
    }
}
