using NUnit.Framework;
using UnityEngine;

namespace SquareTD.Tests
{
    public class SatelliteLayoutTest
    {
        private const float Tolerance = 0.001f;
        private const float Sin60 = 0.8660254f;

        private static void AssertNear(Vector3 expected, Vector3 actual)
        {
            Assert.AreEqual(expected.x, actual.x, Tolerance, $"x: 기대 {expected}, 실제 {actual}");
            Assert.AreEqual(expected.y, actual.y, Tolerance, $"y: 기대 {expected}, 실제 {actual}");
            Assert.AreEqual(expected.z, actual.z, Tolerance, $"z: 기대 {expected}, 실제 {actual}");
        }

        [Test]
        public void 한_개면_12시()
        {
            AssertNear(new Vector3(0f, 1f, 0f), SatelliteLayout.GetOffset(0, 1, 1f));
        }

        [Test]
        public void 두_개면_12시와_6시()
        {
            AssertNear(new Vector3(0f, 1f, 0f), SatelliteLayout.GetOffset(0, 2, 1f));
            AssertNear(new Vector3(0f, -1f, 0f), SatelliteLayout.GetOffset(1, 2, 1f));
        }

        [Test]
        public void 네_개면_12시_3시_6시_9시_시계_방향()
        {
            AssertNear(new Vector3(0f, 1f, 0f), SatelliteLayout.GetOffset(0, 4, 1f));
            AssertNear(new Vector3(1f, 0f, 0f), SatelliteLayout.GetOffset(1, 4, 1f));
            AssertNear(new Vector3(0f, -1f, 0f), SatelliteLayout.GetOffset(2, 4, 1f));
            AssertNear(new Vector3(-1f, 0f, 0f), SatelliteLayout.GetOffset(3, 4, 1f));
        }

        [Test]
        public void 세_개면_12시_4시_8시()
        {
            AssertNear(new Vector3(0f, 1f, 0f), SatelliteLayout.GetOffset(0, 3, 1f));
            AssertNear(new Vector3(Sin60, -0.5f, 0f), SatelliteLayout.GetOffset(1, 3, 1f));
            AssertNear(new Vector3(-Sin60, -0.5f, 0f), SatelliteLayout.GetOffset(2, 3, 1f));
        }

        [Test]
        public void 반지름만큼_떨어짐()
        {
            AssertNear(new Vector3(0.9f, 0f, 0f), SatelliteLayout.GetOffset(1, 4, 0.9f));
        }

        [Test]
        public void 한_자리에_한_발이면_벌리지_않음()
        {
            AssertNear(Vector3.zero, SatelliteLayout.GetSpread(0, 4, 0, 1, 0.12f));
        }

        [Test]
        public void 위쪽_12시_자리의_두_발은_가로로_벌어짐()
        {
            AssertNear(new Vector3(-0.06f, 0f, 0f), SatelliteLayout.GetSpread(0, 4, 0, 2, 0.12f));
            AssertNear(new Vector3(0.06f, 0f, 0f), SatelliteLayout.GetSpread(0, 4, 1, 2, 0.12f));
        }

        [Test]
        public void 오른쪽_3시_자리의_두_발은_세로로_벌어짐()
        {
            AssertNear(new Vector3(0f, 0.06f, 0f), SatelliteLayout.GetSpread(1, 4, 0, 2, 0.12f));
            AssertNear(new Vector3(0f, -0.06f, 0f), SatelliteLayout.GetSpread(1, 4, 1, 2, 0.12f));
        }

        [Test]
        public void 세_발이면_가운데는_자리_그대로()
        {
            AssertNear(Vector3.zero, SatelliteLayout.GetSpread(0, 4, 1, 3, 0.12f));
        }
    }
}
