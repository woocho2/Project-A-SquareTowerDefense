using NUnit.Framework;

public class EnemyCubeRollGeometryTests
{
    [Test]
    public void QuarterTurnHasTwoVisibleFacesInsteadOfCollapsingTheWholeCube()
    {
        var middle = EnemyCubeRollGeometry.Sample(0.5f);
        Assert.AreEqual(0.70710678f, middle.DepartingWidth, 0.00001f);
        Assert.AreEqual(0.70710678f, middle.ArrivingWidth, 0.00001f);
        Assert.AreEqual(0.5f, middle.Travel, 0.00001f);
        Assert.Greater(middle.DepartingWidth + middle.ArrivingWidth, 1f);
    }

    [Test]
    public void FacesShareAnEdgeAndQuarterTurnKeepsItsGroundPivot()
    {
        float previous = -1f;
        for (int i = 0; i <= 1000; i++)
        {
            var frame = EnemyCubeRollGeometry.Sample(i / 1000f);
            float departingEdge = frame.DepartingOffset - frame.DepartingWidth * 0.5f;
            float arrivingEdge = frame.ArrivingOffset + frame.ArrivingWidth * 0.5f;
            Assert.AreEqual(departingEdge, arrivingEdge, 0.00001f);
            Assert.AreEqual(0.5f, frame.Travel + 0.5f * (frame.DepartingWidth - frame.ArrivingWidth), 0.00001f);
            Assert.GreaterOrEqual(frame.DepartingWidth + frame.ArrivingWidth, 0.99999f);
            Assert.GreaterOrEqual(frame.Travel, previous);
            previous = frame.Travel;
        }
    }

    [Test]
    public void EndpointsLandOnTheNextFaceAtFullSize()
    {
        var start = EnemyCubeRollGeometry.Sample(0f);
        var end = EnemyCubeRollGeometry.Sample(1f);
        Assert.AreEqual(0f, start.Travel, 0.00001f);
        Assert.AreEqual(1f, start.DepartingWidth, 0.00001f);
        Assert.AreEqual(0f, start.ArrivingWidth, 0.00001f);
        Assert.AreEqual(1f, end.Travel, 0.00001f);
        Assert.AreEqual(0f, end.DepartingWidth, 0.00001f);
        Assert.AreEqual(1f, end.ArrivingWidth, 0.00001f);
        Assert.AreEqual(0f, end.ArrivingOffset, 0.00001f);
    }

    [Test]
    public void SlowingTheRollDoesNotHoldThenJumpBetweenPoses()
    {
        var previous = EnemyCubeRollGeometry.Sample(0f);
        for (int i = 1; i <= 1000; i++)
        {
            var frame = EnemyCubeRollGeometry.Sample(i / 1000f);
            Assert.Greater(frame.Travel, previous.Travel);
            Assert.Less(frame.Travel - previous.Travel, 0.002f);
            Assert.Less(frame.DepartingWidth, previous.DepartingWidth);
            Assert.Greater(frame.ArrivingWidth, previous.ArrivingWidth);
            previous = frame;
        }
    }
}
