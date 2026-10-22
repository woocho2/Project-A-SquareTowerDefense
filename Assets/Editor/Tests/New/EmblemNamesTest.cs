using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace SquareTD.Tests
{
    public class EmblemNamesTest
    {
        [Test]
        public void 문양_이름()
        {
            Assert.AreEqual("검", EmblemNames.GetEmblemName(TowerEmblem.Sword));
            Assert.AreEqual("불", EmblemNames.GetEmblemName(TowerEmblem.Fire));
            Assert.AreEqual("어둠", EmblemNames.GetEmblemName(TowerEmblem.Darkness));
        }

        [Test]
        public void 공격_방식_이름()
        {
            Assert.AreEqual("광역", EmblemNames.GetColorName(TowerColor.Red));
            Assert.AreEqual("단일", EmblemNames.GetColorName(TowerColor.Blue));
            Assert.AreEqual("버프", EmblemNames.GetColorName(TowerColor.Yellow));
            Assert.AreEqual("디버프", EmblemNames.GetColorName(TowerColor.Purple));
        }

        [Test]
        public void 티어_이름()
        {
            Assert.AreEqual("브론즈", EmblemNames.GetTierName(1));
            Assert.AreEqual("미스릴", EmblemNames.GetTierName(4));
            Assert.AreEqual("다이아", EmblemNames.GetTierName(5));
        }

        [Test]
        public void 컨셉_이름_문양과_컬러로()
        {
            Assert.AreEqual("히어로", EmblemNames.GetConceptName(TowerEmblem.Sword, TowerColor.Red));
            Assert.AreEqual("잭 프로스트", EmblemNames.GetConceptName(TowerEmblem.Ice, TowerColor.Blue));
            Assert.AreEqual("블랙스미스", EmblemNames.GetConceptName(TowerEmblem.Hammer, TowerColor.Yellow));
            Assert.AreEqual("레이스", EmblemNames.GetConceptName(TowerEmblem.Darkness, TowerColor.Purple));
        }

        [Test]
        public void 컨셉_이름_ID로()
        {
            Assert.AreEqual("화염술사", EmblemNames.GetConceptName(2107));
            Assert.AreEqual("가디언", EmblemNames.GetConceptName(5305));
        }

        [Test]
        public void CSV의_Name_메모와_컨셉_이름이_모두_같음()
        {
            var csv = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Resources/TowerData.csv");
            Assert.IsNotNull(csv);

            string[] lines = csv.text.Split('\n');
            string[] header = lines[0].Trim().Split(',');
            int idColumn = System.Array.IndexOf(header, "ID");
            int nameColumn = System.Array.IndexOf(header, "Name");

            int checkedCount = 0;
            for (int i = 1; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (line.Length == 0) continue;

                string[] cells = line.Split(',');
                int id = int.Parse(cells[idColumn]);
                Assert.AreEqual(cells[nameColumn], EmblemNames.GetConceptName(id), $"{id}의 이름");
                checkedCount++;
            }
            Assert.AreEqual(280, checkedCount);
        }
    }
}
