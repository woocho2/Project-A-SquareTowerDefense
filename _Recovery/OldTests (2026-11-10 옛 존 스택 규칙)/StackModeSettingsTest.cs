using NUnit.Framework;

namespace SquareTD.Tests
{
    public class StackModeSettingsTest
    {
        private static TowerDefinition Def(int id) =>
            new TowerDefinition(id, 10f, 2, 3, 1, 0f, 0f, 1, 60, 5, 2f, 5f, TargetPriority.Closest);

        private static TowerStack FullStack()
        {
            var stack = new TowerStack(10);
            stack.Add(10f);
            return stack;
        }

        // ── 자동 / 수동 ──

        [Test]
        public void 기본은_전체_자동()
        {
            var settings = new StackModeSettings();
            Assert.IsFalse(settings.IsManual(TowerColor.Red, TowerEmblem.Fire));
        }

        [Test]
        public void 전체_수동으로_바꿀_수_있음()
        {
            var settings = new StackModeSettings { ManualByDefault = true };
            Assert.IsTrue(settings.IsManual(TowerColor.Red, TowerEmblem.Fire));
            Assert.IsTrue(settings.IsManual(TowerColor.Blue, TowerEmblem.Sword));
        }

        [Test]
        public void 전체_자동에_불_광역만_수동()
        {
            // 20번 4-1장 예
            var settings = new StackModeSettings();
            settings.SetException(TowerColor.Red, TowerEmblem.Fire, true);

            Assert.IsTrue(settings.IsManual(TowerColor.Red, TowerEmblem.Fire));
            Assert.IsFalse(settings.IsManual(TowerColor.Blue, TowerEmblem.Fire), "예외는 컬러 + 문양 둘 다 맞아야 함");
            Assert.IsFalse(settings.IsManual(TowerColor.Red, TowerEmblem.Sword));
        }

        [Test]
        public void 전체_수동에_한_종류만_자동()
        {
            var settings = new StackModeSettings { ManualByDefault = true };
            settings.SetException(TowerColor.Blue, TowerEmblem.Bow, false);

            Assert.IsFalse(settings.IsManual(TowerColor.Blue, TowerEmblem.Bow));
            Assert.IsTrue(settings.IsManual(TowerColor.Red, TowerEmblem.Bow));
        }

        [Test]
        public void 예외를_다시_넣으면_덮어씀()
        {
            var settings = new StackModeSettings();
            settings.SetException(TowerColor.Red, TowerEmblem.Fire, true);
            settings.SetException(TowerColor.Red, TowerEmblem.Fire, false);
            Assert.IsFalse(settings.IsManual(TowerColor.Red, TowerEmblem.Fire));
        }

        [Test]
        public void 예외를_지우면_전체_설정을_따름()
        {
            var settings = new StackModeSettings();
            settings.SetException(TowerColor.Red, TowerEmblem.Fire, true);
            settings.ClearException(TowerColor.Red, TowerEmblem.Fire);
            Assert.IsFalse(settings.IsManual(TowerColor.Red, TowerEmblem.Fire));
        }

        [Test]
        public void 정의로도_물어볼_수_있음()
        {
            var settings = new StackModeSettings();
            settings.SetException(TowerColor.Red, TowerEmblem.Fire, true);
            Assert.IsTrue(settings.IsManual(Def(TowerId.Make(3, TowerColor.Red, TowerEmblem.Fire))), "티어가 달라도 같은 종류");
        }

        // ── 스택을 쓸지 ──

        [Test]
        public void 자동이면_다_찼을_때_씀()
        {
            var settings = new StackModeSettings();
            Assert.IsTrue(settings.ShouldUseStack(Def(1107), FullStack()));
        }

        [Test]
        public void 안_찼으면_안_씀()
        {
            var settings = new StackModeSettings();
            var stack = new TowerStack(10);
            stack.Add(9f);
            Assert.IsFalse(settings.ShouldUseStack(Def(1107), stack));
        }

        [Test]
        public void 수동인데_누르지_않았으면_안_씀()
        {
            var settings = new StackModeSettings { ManualByDefault = true };
            Assert.IsFalse(settings.ShouldUseStack(Def(1107), FullStack()), "기본 피해로 나가고 스택은 다 찬 채로 남음");
        }

        [Test]
        public void 수동이고_눌렀으면_씀()
        {
            var settings = new StackModeSettings { ManualByDefault = true };
            TowerStack stack = FullStack();
            stack.RequestManualConsume();
            Assert.IsTrue(settings.ShouldUseStack(Def(1107), stack));
        }
    }
}
