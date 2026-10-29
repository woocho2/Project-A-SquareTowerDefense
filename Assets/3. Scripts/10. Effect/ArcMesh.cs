using System.Collections.Generic;
using UnityEngine;

namespace SquareTD
{
    // 반달·초승달 모양 메쉬를 코드로 만듦
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

        private static readonly Dictionary<(int, int), Mesh> s_crescentCache = new Dictionary<(int, int), Mesh>();

        public static Mesh GetCrescent(float arcDegrees, float thickness, int segments = 40)
        {
            var key = (Mathf.RoundToInt(arcDegrees), Mathf.RoundToInt(thickness * 100f));
            if (s_crescentCache.TryGetValue(key, out Mesh cached) && cached != null) return cached;

            var vertices = new List<Vector3>();
            var uvs = new List<Vector2>();
            var colors = new List<Color>();
            var triangles = new List<int>();
            float half = arcDegrees * 0.5f * Mathf.Deg2Rad;

            for (int i = 0; i <= segments; i++)
            {
                float t = i / (float)segments;
                float angle = Mathf.Lerp(-half, half, t);
                float width = thickness * Mathf.Pow(Mathf.Sin(t * Mathf.PI), 0.8f);
                Vector3 direction = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
                vertices.Add(direction * (1f - width));
                vertices.Add(direction);
                uvs.Add(new Vector2(t, 0f));
                uvs.Add(new Vector2(t, 0.5f));
                colors.Add(Color.white);
                colors.Add(Color.white);
                if (i == 0) continue;
                int b = i * 2;
                triangles.Add(b - 2); triangles.Add(b + 1); triangles.Add(b - 1);
                triangles.Add(b - 2); triangles.Add(b); triangles.Add(b + 1);
            }

            var mesh = new Mesh { name = $"Crescent {key.Item1}° × {thickness:0.##}" };
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            mesh.SetColors(colors);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            s_crescentCache[key] = mesh;
            return mesh;
        }
    }
}
