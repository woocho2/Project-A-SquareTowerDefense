using System;
using System.Collections.Generic;

public static class OrientationCheck
{
    static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
    public static void Main()
    {
        var cube = EnemyCubeOrientation.Identity;
        Check(cube.VisibleFace == EnemyCubeFace.Front && cube.CounterClockwiseQuarterTurns == 0, "Initial front pose failed.");
        cube = cube.Rolled(EnemyCubeRollDirection.Right);
        Check(cube.VisibleFace == EnemyCubeFace.Left && cube.CounterClockwiseQuarterTurns == 0, "Right one tile must show the left profile upright.");
        cube = cube.Rolled(EnemyCubeRollDirection.Up);
        Check(cube.VisibleFace == EnemyCubeFace.Bottom && cube.CounterClockwiseQuarterTurns == 1, "Right then up must show the bottom face pointing left.");
        cube = cube.Rolled(EnemyCubeRollDirection.Up).Rolled(EnemyCubeRollDirection.Up);
        Check(cube.VisibleFace == EnemyCubeFace.Top && cube.CounterClockwiseQuarterTurns == 1, "Two further upward rolls must show the top face pointing left.");
        Console.WriteLine("PASS: requested R -> U -> U -> U sequence: Left/0, Bottom/90, Right/180, Top/90.");

        var directions = new[] { EnemyCubeRollDirection.Right, EnemyCubeRollDirection.Left, EnemyCubeRollDirection.Up, EnemyCubeRollDirection.Down };
        var inverse = new[] { EnemyCubeRollDirection.Left, EnemyCubeRollDirection.Right, EnemyCubeRollDirection.Down, EnemyCubeRollDirection.Up };
        var states = new HashSet<EnemyCubeOrientation> { EnemyCubeOrientation.Identity };
        var poses = new HashSet<string>();
        var queue = new Queue<EnemyCubeOrientation>(); queue.Enqueue(EnemyCubeOrientation.Identity);
        int transitions = 0;
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            poses.Add(current.VisibleFace + ":" + current.CounterClockwiseQuarterTurns);
            for (int i = 0; i < directions.Length; i++)
            {
                var next = current.Rolled(directions[i]);
                Check(current.Equals(next.Rolled(inverse[i])), "Reverse move did not undo the roll.");
                var cycle = current;
                for (int n = 0; n < 4; n++) cycle = cycle.Rolled(directions[i]);
                Check(current.Equals(cycle), "Four rolls did not restore the full pose.");
                transitions++;
                if (states.Add(next)) queue.Enqueue(next);
            }
        }
        Check(states.Count == 24 && poses.Count == 24, "Expected exactly 24 cube orientations and rendered poses.");
        Check(!EnemyCubeOrientation.Identity.Rolled(EnemyCubeRollDirection.Right).Rolled(EnemyCubeRollDirection.Up)
            .Equals(EnemyCubeOrientation.Identity.Rolled(EnemyCubeRollDirection.Up).Rolled(EnemyCubeRollDirection.Right)), "Turning history was lost.");
        Console.WriteLine("PASS: all 24 cube orientations, 96 reversible transitions, and four-roll cycles checked using the production C# source.");
        Console.WriteLine("PASS: different orders of turning preserve different cube poses.");

        cube = EnemyCubeOrientation.Identity;
        string trace = "const cubeRollPoses = [";
        var example = new[] { EnemyCubeRollDirection.Right, EnemyCubeRollDirection.Up, EnemyCubeRollDirection.Up, EnemyCubeRollDirection.Up };
        for (int i = 0; i <= example.Length; i++)
        {
            if (i > 0) { cube = cube.Rolled(example[i - 1]); trace += ","; }
            trace += "{\"face\":\"" + cube.VisibleFace + "\",\"quarterTurns\":" + cube.CounterClockwiseQuarterTurns + "}";
        }
        System.IO.File.WriteAllText("outputs/cube-enemy-roll/trace.js", trace + "];\n");

        float previous = -1f;
        for (int i = 0; i <= 1000; i++)
        {
            var frame = EnemyCubeRollGeometry.Sample(i / 1000f);
            Check(Math.Abs(frame.DepartingOffset - frame.DepartingWidth * 0.5f
                - frame.ArrivingOffset - frame.ArrivingWidth * 0.5f) < 0.00001f, "Faces separated at their shared edge.");
            Check(Math.Abs(frame.Travel + (frame.DepartingWidth - frame.ArrivingWidth) * 0.5f - 0.5f) < 0.00001f,
                "Normalized cube did not keep its rolling pivot.");
            Check(frame.DepartingWidth + frame.ArrivingWidth >= 0.99999f, "Cube collapsed into a thin card.");
            Check(frame.Travel >= previous, "Roll moved backwards during its animation.");
            previous = frame.Travel;
        }
        var middle = EnemyCubeRollGeometry.Sample(0.5f);
        Check(Math.Abs(middle.DepartingWidth - 0.70710678f) < 0.00001f
            && Math.Abs(middle.ArrivingWidth - 0.70710678f) < 0.00001f, "Mid-roll must show two equal faces.");
        Console.WriteLine("PASS: 1001 samples keep the shared edge, normalized pivot and full cube silhouette; two faces visible at 45 degrees.");
        var previousFrame = EnemyCubeRollGeometry.Sample(0f);
        for (int i = 1; i <= 1000; i++)
        {
            var frame = EnemyCubeRollGeometry.Sample(i / 1000f);
            Check(frame.Travel > previousFrame.Travel && frame.Travel - previousFrame.Travel < 0.002f,
                "Continuous movement held or jumped between samples.");
            Check(frame.DepartingWidth < previousFrame.DepartingWidth && frame.ArrivingWidth > previousFrame.ArrivingWidth,
                "Face deformation held between samples.");
            previousFrame = frame;
        }
        Console.WriteLine("PASS: all 1000 intervals move and deform continuously without held poses or large jumps.");

        var samples = new System.Text.StringBuilder("const cubeRollSamples = [");
        for (int i = 0; i <= 1024; i++)
        {
            if (i > 0) samples.Append(',');
            var frame = EnemyCubeRollGeometry.Sample(i / 1024f);
            samples.Append('[');
            float[] values = { frame.Travel, frame.DepartingWidth, frame.ArrivingWidth, frame.DepartingOffset, frame.ArrivingOffset };
            for (int n = 0; n < values.Length; n++)
            {
                if (n > 0) samples.Append(',');
                samples.Append(values[n].ToString("R", System.Globalization.CultureInfo.InvariantCulture));
            }
            samples.Append(']');
        }
        System.IO.File.WriteAllText("outputs/cube-enemy-roll/geometry.js", samples.Append("];\n").ToString());
    }
}
