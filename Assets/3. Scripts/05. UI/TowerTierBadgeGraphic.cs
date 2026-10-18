using UnityEngine;
using UnityEngine.UI;

/// <summary>티어 스프라이트가 없는 등급에도 표시할 수 있는 작은 보석 배지입니다.</summary>
[RequireComponent(typeof(CanvasRenderer))]
public sealed class TowerTierBadgeGraphic : MaskableGraphic
{
    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        Rect rect = GetPixelAdjustedRect();
        Vector2 center = rect.center;
        Vector2[] corners = { new Vector2(0, rect.height * .5f), new Vector2(rect.width * .5f, 0),
            new Vector2(0, -rect.height * .5f), new Vector2(-rect.width * .5f, 0) };
        Color edge = new Color(.30f, .19f, .12f, color.a);
        for (int i = 0; i < 4; i++) Triangle(mesh, center, center + corners[i], center + corners[(i + 1) % 4], edge);
        float[] shades = { 1.12f, .84f, .67f, 1.02f };
        for (int i = 0; i < 4; i++)
        {
            Color face = new Color(Mathf.Clamp01(color.r * shades[i]), Mathf.Clamp01(color.g * shades[i]),
                Mathf.Clamp01(color.b * shades[i]), color.a);
            Triangle(mesh, center, center + corners[i] * .85f, center + corners[(i + 1) % 4] * .85f, face);
        }
    }
    private static void Triangle(VertexHelper mesh, Vector2 a, Vector2 b, Vector2 c, Color color)
    {
        int start = mesh.currentVertCount;
        mesh.AddVert(a, color, Vector2.zero); mesh.AddVert(b, color, Vector2.zero); mesh.AddVert(c, color, Vector2.zero);
        mesh.AddTriangle(start, start + 1, start + 2);
    }
}
