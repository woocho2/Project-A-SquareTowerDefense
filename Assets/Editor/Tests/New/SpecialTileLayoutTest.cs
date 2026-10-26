using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace SquareTD.Tests
{
    public class SpecialTileLayoutTest
    {
        // 첫 맵 크기: 길 40칸, 스폰 20칸 (모양은 상관없어 한 줄로 늘어놓음)
        private static BoardGrid MakeGrid(int pathCount = 40, int spawnCount = 20)
        {
            var path = new List<Vector3Int>();
            for (int i = 0; i < pathCount; i++) path.Add(new Vector3Int(i, 0, 0));
            var spawn = new List<Vector3Int>();
            for (int i = 0; i < spawnCount; i++) spawn.Add(new Vector3Int(i, 1, 0));
            return new BoardGrid(path, spawn);
        }

        private static SpecialTileLayout Generate(BoardGrid grid, SpecialTileCounts counts, int seed)
        {
            var layout = new SpecialTileLayout();
            layout.Generate(grid, counts, new System.Random(seed));
            return layout;
        }

        private static int CountPath(SpecialTileLayout layout, BoardGrid grid, PathTileType type)
        {
            int count = 0;
            for (int i = 0; i < grid.PathCount; i++) if (layout.GetPathTile(i) == type) count++;
            return count;
        }

        private static int CountSpawn(SpecialTileLayout layout, BoardGrid grid, SpawnTileType type)
        {
            int count = 0;
            for (int i = 0; i < grid.SpawnCount; i++) if (layout.GetSpawnTile(i) == type) count++;
            return count;
        }

        [Test]
        public void 섞어도_원소를_잃거나_더하지_않음()
        {
            var list = new List<int> { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            SpecialTileLayout.Shuffle(list, new System.Random(1));

            var sorted = new List<int>(list);
            sorted.Sort();
            CollectionAssert.AreEqual(new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 }, sorted);
        }

        [Test]
        public void 섞으면_순서가_바뀜()
        {
            var list = new List<int> { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            SpecialTileLayout.Shuffle(list, new System.Random(1));
            CollectionAssert.AreNotEqual(new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 }, list, "Shuffle이 아무것도 안 함");
        }

        [Test]
        public void 섞으면_어느_원소든_어느_자리든_갈_수_있음()
        {
            // random.Next(i)로 쓰면 (자기 자신 빼고 고르면) 원소가 제자리에 남는 일이 없음 → 이 테스트가 잡음
            var seen = new bool[3, 3];
            for (int seed = 0; seed < 300; seed++)
            {
                var list = new List<int> { 0, 1, 2 };
                SpecialTileLayout.Shuffle(list, new System.Random(seed));
                for (int position = 0; position < 3; position++) seen[list[position], position] = true;
            }

            for (int value = 0; value < 3; value++)
                for (int position = 0; position < 3; position++)
                    Assert.IsTrue(seen[value, position], $"{value}가 {position}번 자리에 한 번도 안 옴");
        }

        [Test]
        public void 길_타일이_개수대로_깔림()
        {
            BoardGrid grid = MakeGrid();
            SpecialTileLayout layout = Generate(grid, new SpecialTileCounts(), 7);

            Assert.AreEqual(2, CountPath(layout, grid, PathTileType.Bifrost));
            Assert.AreEqual(2, CountPath(layout, grid, PathTileType.Defense));
            Assert.AreEqual(2, CountPath(layout, grid, PathTileType.Speed));
            Assert.AreEqual(2, CountPath(layout, grid, PathTileType.ActionCount));
            Assert.AreEqual(2, CountPath(layout, grid, PathTileType.Heal));
            Assert.AreEqual(1, CountPath(layout, grid, PathTileType.Purify));
            Assert.AreEqual(40 - 11, CountPath(layout, grid, PathTileType.None));
        }

        [Test]
        public void 스폰_타일이_개수대로_깔림()
        {
            BoardGrid grid = MakeGrid();
            SpecialTileLayout layout = Generate(grid, new SpecialTileCounts(), 7);

            Assert.AreEqual(2, CountSpawn(layout, grid, SpawnTileType.Power));
            Assert.AreEqual(2, CountSpawn(layout, grid, SpawnTileType.ActionCount));
            Assert.AreEqual(2, CountSpawn(layout, grid, SpawnTileType.AttackCount));
            Assert.AreEqual(1, CountSpawn(layout, grid, SpawnTileType.Mastery));
            Assert.AreEqual(20 - 7, CountSpawn(layout, grid, SpawnTileType.None));
        }

        [Test]
        public void 비프로스트_두_칸은_맞은편()
        {
            BoardGrid grid = MakeGrid();
            for (int seed = 0; seed < 20; seed++)
            {
                SpecialTileLayout layout = Generate(grid, new SpecialTileCounts(), seed);
                var bifrosts = new List<int>();
                for (int i = 0; i < grid.PathCount; i++) if (layout.GetPathTile(i) == PathTileType.Bifrost) bifrosts.Add(i);

                Assert.AreEqual(2, bifrosts.Count, $"시드 {seed}");
                Assert.AreEqual(bifrosts[1], grid.GetBifrostPartner(bifrosts[0]), $"시드 {seed}");
            }
        }

        [Test]
        public void 비프로스트를_끄면_안_깔림()
        {
            BoardGrid grid = MakeGrid();
            SpecialTileLayout layout = Generate(grid, new SpecialTileCounts { Bifrost = false }, 7);

            Assert.AreEqual(0, CountPath(layout, grid, PathTileType.Bifrost));
            Assert.AreEqual(2, CountPath(layout, grid, PathTileType.Defense));
        }

        [Test]
        public void 비프로스트_첫_칸은_어디든_될_수_있음()
        {
            BoardGrid grid = MakeGrid(10, 5);
            var seen = new bool[10];
            for (int seed = 0; seed < 300; seed++)
            {
                SpecialTileLayout layout = Generate(grid, new SpecialTileCounts(), seed);
                for (int i = 0; i < 10; i++) if (layout.GetPathTile(i) == PathTileType.Bifrost) seen[i] = true;
            }
            for (int i = 0; i < 10; i++) Assert.IsTrue(seen[i], $"{i}번 칸에 비프로스트가 한 번도 안 깔림 (0번과 마지막 칸도 포함해야 함)");
        }

        [Test]
        public void 같은_시드면_같은_배치()
        {
            BoardGrid grid = MakeGrid();
            SpecialTileLayout a = Generate(grid, new SpecialTileCounts(), 42);
            SpecialTileLayout b = Generate(grid, new SpecialTileCounts(), 42);

            for (int i = 0; i < grid.PathCount; i++) Assert.AreEqual(a.GetPathTile(i), b.GetPathTile(i));
            for (int i = 0; i < grid.SpawnCount; i++) Assert.AreEqual(a.GetSpawnTile(i), b.GetSpawnTile(i));
        }

        [Test]
        public void 시드가_다르면_배치가_달라짐()
        {
            BoardGrid grid = MakeGrid();
            SpecialTileLayout first = Generate(grid, new SpecialTileCounts(), 1);

            bool anyDifferent = false;
            for (int seed = 2; seed < 10 && !anyDifferent; seed++)
            {
                SpecialTileLayout other = Generate(grid, new SpecialTileCounts(), seed);
                for (int i = 0; i < grid.PathCount; i++)
                    if (first.GetPathTile(i) != other.GetPathTile(i)) anyDifferent = true;
            }
            Assert.IsTrue(anyDifferent, "판마다 배치가 같음 (섞지 않았거나 random을 안 씀)");
        }

        [Test]
        public void 다시_깔면_지난_배치가_남지_않음()
        {
            BoardGrid grid = MakeGrid();
            var layout = new SpecialTileLayout();
            layout.Generate(grid, new SpecialTileCounts(), new System.Random(1));
            layout.Generate(grid, new SpecialTileCounts(), new System.Random(2));

            Assert.AreEqual(2, CountPath(layout, grid, PathTileType.Defense));
            Assert.AreEqual(2, CountPath(layout, grid, PathTileType.Bifrost));
        }

        [Test]
        public void 칸보다_많으면_있는_칸까지만()
        {
            BoardGrid grid = MakeGrid(6, 2);
            SpecialTileLayout layout = Generate(grid, new SpecialTileCounts(), 3);     // 경고 로그가 뜸 (테스트 실패는 아님)

            Assert.AreEqual(2, CountPath(layout, grid, PathTileType.Bifrost));
            Assert.AreEqual(0, CountPath(layout, grid, PathTileType.None), "남은 4칸이 다 채워져야 함");
            Assert.AreEqual(0, CountSpawn(layout, grid, SpawnTileType.None));
        }

        [Test]
        public void 깔고_나면_LayoutGenerated()
        {
            var layout = new SpecialTileLayout();
            int count = 0;
            layout.LayoutGenerated += () => count++;
            layout.Generate(MakeGrid(), new SpecialTileCounts(), new System.Random(1));
            Assert.AreEqual(1, count);
        }

        [Test]
        public void 판_밖의_번호는_None()
        {
            SpecialTileLayout layout = Generate(MakeGrid(), new SpecialTileCounts(), 1);
            Assert.AreEqual(PathTileType.None, layout.GetPathTile(-1));
            Assert.AreEqual(PathTileType.None, layout.GetPathTile(40));
            Assert.AreEqual(SpawnTileType.None, layout.GetSpawnTile(20));
        }
    }
}
