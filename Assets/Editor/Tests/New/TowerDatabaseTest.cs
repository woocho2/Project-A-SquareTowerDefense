using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace SquareTD.Tests
{
    public class TowerDatabaseTest
    {
        private const string Header =
            "ID,Name,Power,Range,Action,AttackCount,CriticalRate,CriticalDamage,Mastery,Stack,Duration,AbilityValue,ProjectileSpeed,TargetPriority";

        private static TowerDatabase Load(params string[] lines)
        {
            var database = new TowerDatabase();
            database.Load(string.Join("\n", lines));
            return database;
        }

        [Test]
        public void 두_줄을_읽음()
        {
            TowerDatabase database = Load(Header,
                "1101,히어로,1.4,1,3,1,0.05,0.7,1,60,5,1.5,5,Closest",
                "2107,화염술사,2.5,2,4,2,0.1,0.5,1,60,6,2,4,Boss");
            Assert.AreEqual(2, database.Count);
        }

        [Test]
        public void 값이_열_이름대로_들어감()
        {
            TowerDatabase database = Load(Header, "2107,화염술사,2.5,2,4,2,0.1,0.5,1,60,6,2,4,Boss");
            TowerDefinition fire = database.Get(2107);

            Assert.AreEqual(2.5f, fire.Power, 0.0001f);
            Assert.AreEqual(2, fire.Range);
            Assert.AreEqual(4, fire.Action);
            Assert.AreEqual(2, fire.AttackCount);
            Assert.AreEqual(0.1f, fire.CriticalRate, 0.0001f);
            Assert.AreEqual(0.5f, fire.CriticalDamage, 0.0001f);
            Assert.AreEqual(1, fire.Mastery);
            Assert.AreEqual(60, fire.Stack);
            Assert.AreEqual(6, fire.Duration);
            Assert.AreEqual(2f, fire.AbilityValue, 0.0001f);
            Assert.AreEqual(4f, fire.ProjectileSpeed, 0.0001f);
            Assert.AreEqual(TargetPriority.Boss, fire.TargetPriority);
        }

        [Test]
        public void 티어_컬러_문양_속성은_ID에서_나옴()
        {
            TowerDatabase database = Load(Header, "2107,화염술사,2.5,2,4,2,0.1,0.5,1,60,6,2,4,Closest",
                                                  "1201,검객,1,1,3,1,0,0,1,60,5,1,5,Closest");
            TowerDefinition fire = database.Get(2107);
            Assert.AreEqual(2, fire.Tier);
            Assert.AreEqual(TowerColor.Red, fire.Color);
            Assert.AreEqual(TowerEmblem.Fire, fire.Emblem);
            Assert.AreEqual(AttackAttribute.None, fire.Attribute);
            Assert.AreEqual(AttackAttribute.Slash, database.Get(1201).Attribute);
        }

        [Test]
        public void 열_순서가_바뀌어도_읽음()
        {
            TowerDatabase database = Load(
                "TargetPriority,Power,ID,Stack,Mastery,Range,Action,AttackCount,CriticalRate,CriticalDamage,Duration,AbilityValue,ProjectileSpeed",
                "Closest,2.5,2107,60,1,2,4,2,0.1,0.5,6,2,4");
            Assert.AreEqual(2.5f, database.Get(2107).Power, 0.0001f);
            Assert.AreEqual(60, database.Get(2107).Stack);
        }

        [Test]
        public void 코드가_안_읽는_메모_열이_있어도_됨()
        {
            TowerDatabase database = Load(Header + ",Memo",
                "2107,화염술사,2.5,2,4,2,0.1,0.5,1,60,6,2,4,Closest,나중에 수치 조정");
            Assert.AreEqual(1, database.Count);
        }

        [Test]
        public void 윈도우_줄바꿈도_읽음()
        {
            var database = new TowerDatabase();
            database.Load(Header + "\r\n" + "2107,화염술사,2.5,2,4,2,0.1,0.5,1,60,6,2,4,Closest\r\n");
            Assert.AreEqual(TargetPriority.Closest, database.Get(2107).TargetPriority);
        }

        [Test]
        public void 숫자가_아닌_칸이_있는_줄만_건너뜀()
        {
            LogAssert.Expect(LogType.Error, new Regex("3번째 줄을 읽지 못함: Power의 값 'abc'이 숫자가 아님"));
            TowerDatabase database = Load(Header,
                "1101,히어로,1.4,1,3,1,0.05,0.7,1,60,5,1.5,5,Closest",
                "1102,바이킹,abc,1,5,1,0.03,1.05,1,60,5,1.5,5,Closest",
                "1103,레인저,0.98,4,3,1,0.055,0.49,1,60,5,1.5,5,Closest");
            Assert.AreEqual(2, database.Count);
            Assert.IsFalse(database.TryGet(1102, out _));
        }

        [Test]
        public void 정수_칸에_소수가_있으면_건너뜀()
        {
            LogAssert.Expect(LogType.Error, new Regex("Range의 값 '1.5'이 숫자가 아님"));
            TowerDatabase database = Load(Header, "1101,히어로,1.4,1.5,3,1,0.05,0.7,1,60,5,1.5,5,Closest");
            Assert.AreEqual(0, database.Count);
        }

        [Test]
        public void 없는_우선순위_이름이면_건너뜀()
        {
            LogAssert.Expect(LogType.Error, new Regex("TargetPriority의 값 'Nearest'이 TargetPriority에 없음"));
            TowerDatabase database = Load(Header, "1101,히어로,1.4,1,3,1,0.05,0.7,1,60,5,1.5,5,Nearest");
            Assert.AreEqual(0, database.Count);
        }

        [Test]
        public void 같은_ID가_두_번이면_처음_것만()
        {
            LogAssert.Expect(LogType.Error, new Regex("3번째 줄: ID 1101가 두 번 나옴"));
            TowerDatabase database = Load(Header,
                "1101,히어로,1.4,1,3,1,0.05,0.7,1,60,5,1.5,5,Closest",
                "1101,히어로,9.9,1,3,1,0.05,0.7,1,60,5,1.5,5,Closest");
            Assert.AreEqual(1, database.Count);
            Assert.AreEqual(1.4f, database.Get(1101).Power, 0.0001f);
        }

        [Test]
        public void 열이_빠지면_그_줄을_건너뜀()
        {
            LogAssert.Expect(LogType.Error, new Regex("Stack 열이 없음"));
            TowerDatabase database = Load(
                "ID,Name,Power,Range,Action,AttackCount,CriticalRate,CriticalDamage,Mastery,Duration,AbilityValue,ProjectileSpeed,TargetPriority",
                "1101,히어로,1.4,1,3,1,0.05,0.7,1,5,1.5,5,Closest");
            Assert.AreEqual(0, database.Count);
        }

        [Test]
        public void 없는_ID는_TryGet이_false()
        {
            TowerDatabase database = Load(Header, "1101,히어로,1.4,1,3,1,0.05,0.7,1,60,5,1.5,5,Closest");
            Assert.IsFalse(database.TryGet(9999, out TowerDefinition missing));
            Assert.IsNull(missing);
        }

        [Test]
        public void 진짜_TowerData_csv_280종()
        {
            var csv = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Resources/TowerData.csv");
            Assert.IsNotNull(csv, "Assets/Resources/TowerData.csv가 없음");

            var database = new TowerDatabase();
            database.Load(csv.text);

            Assert.AreEqual(280, database.Count);
            Assert.AreEqual(TowerEmblem.Fire, database.Get(2107).Emblem);
            Assert.AreEqual(1.4f, database.Get(1101).Power, 0.0001f);
            for (int tier = 1; tier <= 5; tier++)
            {
                for (int color = 1; color <= 4; color++)
                {
                    for (int emblem = 1; emblem <= 14; emblem++)
                    {
                        int id = TowerId.Make(tier, (TowerColor)color, (TowerEmblem)emblem);
                        Assert.IsTrue(database.TryGet(id, out _), $"{id}가 없음");
                    }
                }
            }
        }
    }
}
