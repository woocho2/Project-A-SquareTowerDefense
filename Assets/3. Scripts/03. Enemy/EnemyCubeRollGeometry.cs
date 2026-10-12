using System;

/// <summary>모서리를 축으로 90도 구르는 정육면체를 위에서 정사영한 값입니다. 길이 단위는 한 변입니다.</summary>
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
        // 이동과 면 변형을 매 프레임 갱신합니다. 시간을 단계로 잘라서 멈추지 않습니다.
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
