using System;

namespace SquareTD
{
    // 기본 공격의 발사 간격
    [Serializable]
    public struct ShotTiming
    {
        public float VolleyInterval;
        public float ShotGap;

        public static ShotTiming Default => new ShotTiming { VolleyInterval = 0.3f, ShotGap = 0.06f };
    }
}
