using System.Collections.Generic;
using UnityEngine;

namespace SquareTD
{
    // 반달(부채꼴 띠) 메쉬를 코드로 만듦 — 베기 궤적 그림을 휘어서 입히는 판 (블렌더 없이)
    //   가운데(0, 0)를 중심으로 반지름 1인 원을 따라 arcDegrees만큼 (반시계 방향, 아래 → 위).
    //   u = 0(시작, 꼬리) → 1(끝, 머리), v = 0(안쪽) → 1(바깥쪽). 크기·방향은 Transform으로
    //   thickness: 띠의 두께 (반지름 1에 대한 비율). 띠는 바깥으로 갈수록 살짝 굵어지게 하지 않고 고르게
    public static class ArcMesh
    {
        private static readonly Dictionary<(int, int), Mesh> s_cache = new Dictionary<(int, int), Mesh>();

        public static Mesh Get(float arcDegrees, float thickness, int segments = 32)
        {
            var key = (Mathf.RoundToInt(arcDegrees), Mathf.RoundToInt(thickness * 100f));
            if (s_cache.TryGetValue(key, out Mesh cached) && cached != null) return cached;

            var vertices = new List<Vector3>();
            var uvs = new List<Vector2>();
            var colors = new List<Color>();
            var triangles = new List<int>();
            float half = arcDegrees * 0.5f * Mathf.Deg2Rad;
            float inner = 1f - thickness * 0.5f;
            float outer = 1f + thickness * 0.5f;

            for (int i = 0; i <= segments; i++)
            {
                float t = i / (float)segments;
                float angle = Mathf.Lerp(-half, half, t);
                Vector3 direction = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
                vertices.Add(direction * inner);
                vertices.Add(direction * outer);
                uvs.Add(new Vector2(t, 0f));
                uvs.Add(new Vector2(t, 1f));
                colors.Add(Color.white);
                colors.Add(Color.white);
                if (i == 0) continue;
                int b = i * 2;
                triangles.Add(b - 2); triangles.Add(b + 1); triangles.Add(b - 1);
                triangles.Add(b - 2); triangles.Add(b); triangles.Add(b + 1);
            }

            var mesh = new Mesh { name = $"Arc {key.Item1}° × {thickness:0.##}" };
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            mesh.SetColors(colors);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            s_cache[key] = mesh;
            return mesh;
        }
    }
}
