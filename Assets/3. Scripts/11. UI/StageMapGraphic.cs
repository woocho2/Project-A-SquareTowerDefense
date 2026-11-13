using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SquareTD
{
    // 스테이지 카드의 판 미리보기 — 길 칸·타워 칸·길 방향 화살표·시작 칸을 월드 색으로 그림
    public class StageMapGraphic : MaskableGraphic
    {
        [SerializeField] float m_gap = 0.1f;
        [SerializeField] float m_arrowSize = 0.28f;
        [SerializeField] float m_frameWidth = 0.35f;
        [SerializeField] float m_cornerRadius = 0.22f;
        [SerializeField] int m_cornerSegments = 5;

        private StageDefinition m_stage;
        private StageDefinition m_layout;

        public void SetStage(StageDefinition stage, StageDefinition fallbackLayout = null)
        {
            m_stage = stage;
            m_layout = stage != null && stage.HasLayout ? stage : fallbackLayout;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (m_stage == null || m_layout == null || !m_layout.HasLayout) return;

            Rect rect = GetPixelAdjustedRect();
            Vector2Int size = m_layout.BoardSize;
            float columns = size.x + m_frameWidth * 2f;
            float rows = size.y + m_frameWidth * 2f;
            float cell = Mathf.Min(rect.width / columns, rect.height / rows);
            Vector2 boardSize = new Vector2(size.x * cell, size.y * cell);
            Vector2 origin = rect.center - boardSize * 0.5f;

            float frame = cell * m_frameWidth;
            float radius = cell * m_cornerRadius;
            Color accent = m_stage.AccentColor;
            Color shade = Color.Lerp(m_stage.PathColor, Color.black, 0.35f);
            AddRoundedRect(vh, new Rect(origin - Vector2.one * frame, boardSize + Vector2.one * frame * 2f), radius * 1.6f, accent);
            AddRoundedRect(vh, new Rect(origin - Vector2.one * frame * 0.45f, boardSize + Vector2.one * frame * 0.9f), radius * 1.2f, shade);

            float gap = cell * m_gap;
            foreach (Vector2Int spawn in m_layout.SpawnCells)
            {
                AddRoundedRect(vh, CellRect(origin, cell, gap, spawn), radius, m_stage.SpawnColor);
            }

            List<Vector2Int> path = m_layout.PathCells;
            for (int i = 0; i < path.Count; i++)
            {
                Rect cellRect = CellRect(origin, cell, gap, path[i]);
                if (i == 0)
                {
                    float border = cell * 0.1f;
                    AddRoundedRect(vh, cellRect, radius, accent);
                    AddRoundedRect(vh, new Rect(cellRect.position + Vector2.one * border, cellRect.size - Vector2.one * border * 2f), Mathf.Max(0f, radius - border), m_stage.PathColor);
                }
                else
                {
                    AddRoundedRect(vh, cellRect, radius, m_stage.PathColor);
                }

                Vector2Int step = path[(i + 1) % path.Count] - path[i];
                AddArrow(vh, cellRect.center, new Vector2(step.x, step.y).normalized, cell * m_arrowSize, accent);
            }
        }

        private Rect CellRect(Vector2 origin, float cell, float gap, Vector2Int position)
        {
            Vector2Int local = position - m_layout.BoardMin;
            return new Rect(origin.x + local.x * cell + gap * 0.5f, origin.y + local.y * cell + gap * 0.5f, cell - gap, cell - gap);
        }

        private void AddRoundedRect(VertexHelper vh, Rect rect, float radius, Color tint)
        {
            Color32 c = tint * color;
            radius = Mathf.Clamp(radius, 0f, Mathf.Min(rect.width, rect.height) * 0.5f);
            int segments = Mathf.Max(1, m_cornerSegments);

            int center = vh.currentVertCount;
            vh.AddVert(rect.center, c, Vector2.zero);

            Vector2[] corners =
            {
                new Vector2(rect.xMax - radius, rect.yMax - radius),
                new Vector2(rect.xMin + radius, rect.yMax - radius),
                new Vector2(rect.xMin + radius, rect.yMin + radius),
                new Vector2(rect.xMax - radius, rect.yMin + radius),
            };

            int first = vh.currentVertCount;
            for (int corner = 0; corner < 4; corner++)
            {
                for (int s = 0; s <= segments; s++)
                {
                    float angle = (corner * 90f + 90f * s / segments) * Mathf.Deg2Rad;
                    vh.AddVert(corners[corner] + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius, c, Vector2.zero);
                }
            }

            int count = vh.currentVertCount - first;
            for (int i = 0; i < count; i++)
            {
                vh.AddTriangle(center, first + i, first + (i + 1) % count);
            }
        }

        private void AddArrow(VertexHelper vh, Vector2 center, Vector2 direction, float size, Color tint)
        {
            if (direction == Vector2.zero) return;
            Color32 c = tint * color;
            Vector2 side = new Vector2(-direction.y, direction.x);
            int start = vh.currentVertCount;
            vh.AddVert(center + direction * size * 0.55f, c, Vector2.zero);
            vh.AddVert(center - direction * size * 0.45f + side * size * 0.5f, c, Vector2.zero);
            vh.AddVert(center - direction * size * 0.45f - side * size * 0.5f, c, Vector2.zero);
            vh.AddTriangle(start, start + 1, start + 2);
        }
    }
}
