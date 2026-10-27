using System;
using UnityEngine;

namespace SquareTD
{
    [Serializable]
    public struct SatelliteSettings
    {
        public float Interval;        // 위성 하나 띄우고 다음 것까지 (발사도 같은 간격)
        public float LaunchDelay;     // 다 띄운 뒤 첫 발사까지
        public float Radius;          // 타워 중심에서 위성까지
        public float GroupSpacing;    // 한 자리에 여러 발일 때 옆으로 벌리는 간격

        public static SatelliteSettings Default => new SatelliteSettings
        {
            Interval = 0.3f, LaunchDelay = 0.5f, Radius = 0.9f, GroupSpacing = 0.12f
        };
    }

    public static class SatelliteLayout
    {
        public static Vector3 GetOffset(int index, int count, float radius)
        {
            float angle = 360f * index / count * Mathf.Deg2Rad;
            return new Vector3(Mathf.Sin(angle), Mathf.Cos(angle), 0f) * radius;
        }

        public static Vector3 GetSpread(int index, int count, int shotIndex, int shotCount, float spacing)
        {
            float angle = 360f * index / count * Mathf.Deg2Rad;
            Vector3 sideways = new Vector3(Mathf.Cos(angle), -Mathf.Sin(angle), 0f);
            float amount = (shotIndex - (shotCount - 1) * 0.5f) * spacing;
            return sideways * amount;
        }
    }
}
