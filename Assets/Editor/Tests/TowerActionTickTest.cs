using NUnit.Framework;
using UnityEngine;

namespace SquareTD.Tests
{
    // 타워의 행동력: 턴마다 1씩, ActionGainPerTurn(블러드 레이지 등)이 있으면 그만큼 더 줄어듦
    public class TowerActionTickTest
    {
        private GameObject m_go;

        private TowerController MakeTower(int id, int action)
        {
            m_go = new GameObject("TestTower");
            var tower = m_go.AddComponent<TowerController>();
            var definition = new TowerDefinition(id, 10f, 2, action, 1, 0f, 0f, 1, 60, 5, 2f, 5f, TargetPriority.Closest);
            tower.Init(definition, 0, null);
            return tower;
        }

        private static void GainOneMore(TowerController tower) =>
            tower.State.Modifiers.Add(StatModifier.Flat("시험", StatType.ActionGainPerTurn, 1f));

        [TearDown]
        public void CleanUp()
        {
            if (m_go != null) Object.DestroyImmediate(m_go);
        }

        [Test]
        public void 따로_더하는_것이_없으면_한_턴에_1씩()
        {
            TowerController tower = MakeTower(1101, 6);
            for (int turn = 1; turn <= 5; turn++) Assert.IsFalse(tower.TickAction(), $"{turn}턴");
            Assert.IsTrue(tower.TickAction(), "6턴째에 행동");
        }

        [Test]
        public void 행동_카운트가_1_더해지면_한_턴에_2씩()
        {
            TowerController tower = MakeTower(1101, 6);
            GainOneMore(tower);

            Assert.IsFalse(tower.TickAction());      // 6 → 4
            Assert.IsFalse(tower.TickAction());      // 4 → 2
            Assert.IsTrue(tower.TickAction());       // 2 → 0 → 행동, 다시 6
            Assert.AreEqual(6, tower.State.RemainingAction);
        }

        [Test]
        public void 남은_행동력이_1이어도_음수로_내려가지_않음()
        {
            TowerController tower = MakeTower(1101, 3);
            GainOneMore(tower);

            Assert.IsFalse(tower.TickAction());      // 3 → 1
            Assert.IsTrue(tower.TickAction());       // 1 → 0 (−1이 아님) → 행동
            Assert.AreEqual(3, tower.State.RemainingAction);
        }
    }
}
