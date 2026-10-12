using System.Collections.Generic;
using NUnit.Framework;

public class EnemyCubeOrientationTests
{
    [Test]
    public void RequestedRightThenUpSequenceKeepsTheFacesRotation()
    {
        var cube = EnemyCubeOrientation.Identity;
        Assert.AreEqual(EnemyCubeFace.Front, cube.VisibleFace);
        cube = cube.Rolled(EnemyCubeRollDirection.Right);
        Assert.AreEqual(EnemyCubeFace.Left, cube.VisibleFace);
        Assert.AreEqual(0, cube.CounterClockwiseQuarterTurns);
        cube = cube.Rolled(EnemyCubeRollDirection.Up);
        Assert.AreEqual(EnemyCubeFace.Bottom, cube.VisibleFace);
        Assert.AreEqual(1, cube.CounterClockwiseQuarterTurns);
        cube = cube.Rolled(EnemyCubeRollDirection.Up).Rolled(EnemyCubeRollDirection.Up);
        Assert.AreEqual(EnemyCubeFace.Top, cube.VisibleFace);
        Assert.AreEqual(1, cube.CounterClockwiseQuarterTurns);
    }

    [TestCase(EnemyCubeRollDirection.Right)]
    [TestCase(EnemyCubeRollDirection.Left)]
    [TestCase(EnemyCubeRollDirection.Up)]
    [TestCase(EnemyCubeRollDirection.Down)]
    public void FourRollsRestoreTheEntireOrientation(EnemyCubeRollDirection direction)
    {
        var cube = EnemyCubeOrientation.Identity;
        for (int i = 0; i < 4; i++) cube = cube.Rolled(direction);
        Assert.AreEqual(EnemyCubeOrientation.Identity, cube);
    }

    [Test]
    public void All24OrientationsAreReversibleAndHave24DistinctRenderedPoses()
    {
        var seen = new HashSet<EnemyCubeOrientation> { EnemyCubeOrientation.Identity };
        var queue = new Queue<EnemyCubeOrientation>();
        var renderedPoses = new HashSet<string>();
        queue.Enqueue(EnemyCubeOrientation.Identity);
        var directions = new[] { EnemyCubeRollDirection.Right, EnemyCubeRollDirection.Left, EnemyCubeRollDirection.Up, EnemyCubeRollDirection.Down };
        var inverses = new[] { EnemyCubeRollDirection.Left, EnemyCubeRollDirection.Right, EnemyCubeRollDirection.Down, EnemyCubeRollDirection.Up };
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            renderedPoses.Add(current.VisibleFace + ":" + current.CounterClockwiseQuarterTurns);
            for (int i = 0; i < directions.Length; i++)
            {
                var next = current.Rolled(directions[i]);
                Assert.AreEqual(current, next.Rolled(inverses[i]));
                if (seen.Add(next)) queue.Enqueue(next);
            }
        }
        Assert.AreEqual(24, seen.Count);
        Assert.AreEqual(24, renderedPoses.Count);
    }
}
