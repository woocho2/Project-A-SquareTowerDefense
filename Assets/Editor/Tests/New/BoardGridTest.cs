using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace SquareTD.Tests
{
    public class BoardGridTest
    {
        // 테스트용 길 5칸: 0번 (0,0) → 1번 (1,0) → 2번 (2,1) → 3번 (1,1) → 4번 (2,-1) → 다시 0번
        private static BoardGrid MakeGrid()
        {
            var path = new List<Vector3Int>
            {
                new Vector3Int(0, 0, 0),
                new Vector3Int(1, 0, 0),
                new Vector3Int(2, 1, 0),
                new Vector3Int(1, 1, 0),
                new Vector3Int(2, -1, 0),
            };
            var spawn = new List<Vector3Int>
            {
                new Vector3Int(0, 1, 0),
                new Vector3Int(3, 0, 0),
                new Vector3Int(1, -1, 0)
            };

            return new BoardGrid(path, spawn);   // path가 인자 → 생성자의 pathCells가 됨
        }

        [Test]
        public void PathCount는_5()
        {
            BoardGrid grid = MakeGrid();
            Assert.AreEqual(5, grid.PathCount);
        }

        [Test]
        public void GetPathCell_2는_newVector3Int_2_1_0()
        {
            BoardGrid grid = MakeGrid();
            Assert.AreEqual(new Vector3Int(2, 1, 0), grid.GetPathCell(2));
        }

        [Test]
        public void WrapPathIndex_5는_0()
        {
            BoardGrid grid = MakeGrid();
            Assert.AreEqual(0, grid.WrapPathIndex(5));
        }

        [Test]
        public void WrapPathIndex_m1는_4()
        {
            BoardGrid grid = MakeGrid();
            Assert.AreEqual(4, grid.WrapPathIndex(-1));
        }

        [Test]
        public void WrapPathIndex_m6는_4()
        {
            BoardGrid grid = MakeGrid();
            Assert.AreEqual(4, grid.WrapPathIndex(-6));
        }

        [Test]
        public void GetPathCell_7은_newVector3Int_2_1_0()
        {
            BoardGrid grid = MakeGrid();
            Assert.AreEqual(new Vector3Int(2, 1, 0), grid.GetPathCell(7));
        }

        [Test]
        public void TryGetPathIndex_newVector3Int_1_1_0_은_true이고_3()
        {
            BoardGrid grid = MakeGrid();

            bool found = grid.TryGetPathIndex(new Vector3Int(1, 1, 0), out int i); 

            Assert.IsTrue(found);
            Assert.AreEqual(3, i);
        }

        [Test]
        public void TryGetPathIndex_newVector3Int_3_0_0_out_int_i는_false()
        {
            BoardGrid grid = MakeGrid();

            bool found = grid.TryGetPathIndex(new Vector3Int(3, 0, 0), out int i);

            Assert.IsFalse(found);
        }

        [Test]
        public void SpawnCount는3()
        {
            BoardGrid grid = MakeGrid();
            Assert.AreEqual(3,grid.SpawnCount);
        }

        [Test]
        public void GetSpawnCell_1의값은_3_0_0()
        {
            BoardGrid grid = MakeGrid();
            Assert.AreEqual(new Vector3Int(3,0,0),grid.GetSpawnCell(1));
        }

        [Test]
        public void TryGetSpawnIndex_1_m1_0_의값은_true_2()
        {
            BoardGrid grid = MakeGrid();
            bool found = grid.TryGetSpawnIndex(new Vector3Int (1,-1,0), out int i);
            Assert.IsTrue(found);
            Assert.AreEqual(2, i);
        }

        [Test]
        public void TryGetSpawnIndex_0_0_0_의값은_false()
        {
            BoardGrid grid = MakeGrid();
            bool found = grid.TryGetSpawnIndex(new Vector3Int(0, 0, 0), out int i);
            Assert.IsFalse(found);
        }

        [Test]
        public void BoardGrid_GetTileDistance_1_1_0과m1_m2_0의값은_3()
        {
            Assert.AreEqual(3, BoardGrid.GetTileDistance(new Vector3Int(1,1,0), new Vector3Int(-1,-2,0)));
        }

        [Test]
        public void GetBifrostPartner_1_값은_3()
        {
            BoardGrid grid = MakeGrid();
            Assert.AreEqual(3, grid.GetBifrostPartner(1));
        }

        [Test]
        public void GetBifrostPartner_4_값은_1()
        {
            BoardGrid grid = MakeGrid();
            Assert.AreEqual(1, grid.GetBifrostPartner(4));
        }
    }
}
