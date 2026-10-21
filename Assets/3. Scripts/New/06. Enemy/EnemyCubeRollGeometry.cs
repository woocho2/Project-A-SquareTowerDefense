using System;

namespace SquareTD
{
    public static class EnemyCubeRollGeometry
    {
        public struct Frame
        {
            public float Travel;
            public float DepartingWidth;
            public float ArrivingWidth;
            public float DepartingOffset;
            public float ArrivingOffset;
        }

        public static Frame Sample(float progress)
        {
            progress = Math.Max(0f, Math.Min(1f, progress));
            double angle = progress * Math.PI * 0.5;
            float cosine = (float)Math.Cos(angle);
            float sine = (float)Math.Sin(angle);
            return new Frame
            {
                Travel = 0.5f * (1f - cosine + sine),
                DepartingWidth = cosine,
                ArrivingWidth = sine,
                DepartingOffset = 0.5f * sine,
                ArrivingOffset = -0.5f * cosine
            };
        }
    }
}
