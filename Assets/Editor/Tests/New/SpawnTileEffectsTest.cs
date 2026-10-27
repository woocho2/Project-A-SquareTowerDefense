using NUnit.Framework;
using UnityEngine;

namespace SquareTD.Tests
{
    public class SpawnTileEffectsTest
    {
        private GameObject m_go;

        // 1티어(티어 표 그대로)라 CSV 값이 곧 기본값: 위력 10, Action은 인자, 공격 횟수 1, Mastery 1
        private TowerController MakeTower(int id, int action)
        {
            m_go = new GameObject("TestTower");
            var tower = m_go.AddComponent<TowerController>();
            var definition = new TowerDefinition(id, 10f, 2, action, 1, 0f, 0f, 1, 60, 5, 2f, 5f, TargetPriority.Closest);
            tower.Init(definition, 0, null);
            return tower;
        }

        [TearDown]
        public void CleanUp()
        {
            if (m_go != null) Object.DestroyImmediate(m_go);
        }

        // ── 표와 Apply ──

        [Test]
        public void 평범한_칸은_수정자_없음()
        {
            Assert.AreEqual(0, SpawnTileEffects.GetModifiers(SpawnTileType.None).Length);
        }

        [Test]
        public void 칸에_놓이면_그_칸의_수정자가_붙음()
        {
            var list = new StatModifierList();
            SpawnTileEffects.Apply(list, SpawnTileType.Power);

            Assert.AreEqual(1, list.All.Count);
            Assert.AreEqual(SpawnTileEffects.Source, list.All[0].Source);
            Assert.AreEqual(13f, list.Apply(StatType.Power, 10f), 0.0001f);
        }

        [Test]
        public void 다른_칸으로_옮기면_지난_칸의_것은_사라짐()
        {
            var list = new StatModifierList();
            SpawnTileEffects.Apply(list, SpawnTileType.Power);
            SpawnTileEffects.Apply(list, SpawnTileType.AttackCount);

            Assert.AreEqual(1, list.All.Count);
            Assert.AreEqual(10f, list.Apply(StatType.Power, 10f), 0.0001f, "공격력 타일을 떠남");
            Assert.AreEqual(2f, list.Apply(StatType.AttackCount, 1f), 0.0001f);
        }

        [Test]
        public void 평범한_칸으로_옮기면_다_사라짐()
        {
            var list = new StatModifierList();
            SpawnTileEffects.Apply(list, SpawnTileType.Mastery);
            SpawnTileEffects.Apply(list, SpawnTileType.None);

            Assert.AreEqual(0, list.All.Count);
        }

        [Test]
        public void 같은_칸에_다시_걸어도_겹치지_않음()
        {
            var list = new StatModifierList();
            SpawnTileEffects.Apply(list, SpawnTileType.Power);
            SpawnTileEffects.Apply(list, SpawnTileType.Power);

            Assert.AreEqual(13f, list.Apply(StatType.Power, 10f), 0.0001f, "+30%가 두 번 붙으면 16");
        }

        [Test]
        public void 다른_출처의_수정자는_건드리지_않음()
        {
            var list = new StatModifierList();
            list.Add(StatModifier.Percent("검기", StatType.Power, 0.5f));
            SpawnTileEffects.Apply(list, SpawnTileType.Power);
            SpawnTileEffects.Apply(list, SpawnTileType.None);

            Assert.AreEqual(15f, list.Apply(StatType.Power, 10f), 0.0001f);
        }

        // ── 타워에 걸기 ──

        [Test]
        public void 공격력_타일_위의_공격_타워는_위력_30퍼센트()
        {
            TowerController tower = MakeTower(1101, 3);
            tower.SetSpawnTile(SpawnTileType.Power);

            Assert.AreEqual(13f, tower.Stats.Power, 0.0001f);
        }

        [Test]
        public void 공격_횟수와_마스터리_타일은_하나씩_더함()
        {
            TowerController tower = MakeTower(1201, 3);
            tower.SetSpawnTile(SpawnTileType.AttackCount);
            Assert.AreEqual(2, tower.Stats.AttackCount);

            tower.SetSpawnTile(SpawnTileType.Mastery);
            Assert.AreEqual(1, tower.Stats.AttackCount, "공격 횟수 타일을 떠남");
            Assert.AreEqual(2, tower.Stats.Mastery);
        }

        [Test]
        public void 버프_디버프_타워는_스폰_타일을_받지_않음()
        {
            TowerController buff = MakeTower(1301, 3);
            buff.SetSpawnTile(SpawnTileType.Power);
            Assert.AreEqual(10f, buff.Stats.Power, 0.0001f);
            CleanUp();

            TowerController debuff = MakeTower(1401, 3);
            debuff.SetSpawnTile(SpawnTileType.Mastery);
            Assert.AreEqual(1, debuff.Stats.Mastery);
        }

        // ── 행동 카운트 타일 ──

        [Test]
        public void 행동_카운트_타일이_없으면_한_턴에_1씩()
        {
            TowerController tower = MakeTower(1101, 6);
            for (int turn = 1; turn <= 5; turn++) Assert.IsFalse(tower.TickAction(), $"{turn}턴");
            Assert.IsTrue(tower.TickAction(), "6턴째에 행동");
        }

        [Test]
        public void 행동_카운트_타일_위에선_한_턴에_2씩()
        {
            // 기획 2장: Action 6이면 3턴마다 행동
            TowerController tower = MakeTower(1101, 6);
            tower.SetSpawnTile(SpawnTileType.ActionCount);

            Assert.IsFalse(tower.TickAction());      // 6 → 4
            Assert.IsFalse(tower.TickAction());      // 4 → 2
            Assert.IsTrue(tower.TickAction());       // 2 → 0 → 행동, 다시 6
            Assert.AreEqual(6, tower.State.RemainingAction);
        }

        [Test]
        public void 남은_행동력이_1이어도_음수로_내려가지_않음()
        {
            TowerController tower = MakeTower(1101, 3);
            tower.SetSpawnTile(SpawnTileType.ActionCount);

            Assert.IsFalse(tower.TickAction());      // 3 → 1
            Assert.IsTrue(tower.TickAction());       // 1 → 0 (−1이 아님) → 행동
            Assert.AreEqual(3, tower.State.RemainingAction);
        }

        [Test]
        public void 버프_타워는_행동_카운트_타일도_받지_않음()
        {
            TowerController tower = MakeTower(1301, 2);
            tower.SetSpawnTile(SpawnTileType.ActionCount);

            Assert.IsFalse(tower.TickAction());      // 2 → 1
            Assert.IsTrue(tower.TickAction());
        }
    }
}
