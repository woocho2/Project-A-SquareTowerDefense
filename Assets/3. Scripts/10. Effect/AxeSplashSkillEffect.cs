using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace SquareTD
{
    // 도끼 대회전 — 원형 참격과 사거리 끝 칸의 도끼날 강조
    [DisallowMultipleComponent]
    public sealed class AxeSplashSkillEffect : MonoBehaviour
    {
        private static readonly Color s_edge = new Color(1f, 0.28f, 0.065f);
        private static readonly Color s_hot = new Color(1.35f, 1f, 0.55f);

        [SerializeField, Min(0.01f)] private float m_spinDuration = 0.28f;
        [SerializeField, Min(0f)] private float m_holdDuration = 0.05f;
        [SerializeField, Min(0.01f)] private float m_fadeDuration = 0.22f;

        private EffectQuadPool m_pool;
        private EffectParticles m_sparks;

        private void Awake()
        {
            m_pool = new EffectQuadPool(transform, "AxeWhirl", "Viking Whirl", 28, EffectTextures.SlashTrail);
            m_sparks = new EffectParticles(transform, "Whirl Edge Sparks", EffectTextures.Spark,
                true, 1.4f, 31, true, 0.5f, false, true);
        }

        private void OnDestroy() => m_pool?.Dispose();

        public void Play(Vector3 owner, Vector3 center, float areaWorldSize, float cellWorldSize)
        {
            if (!m_pool.IsReady) return;

            float cell = Mathf.Max(0.01f, cellWorldSize);
            float area = Mathf.Max(cell, areaWorldSize);
            int range = Mathf.Max(0, Mathf.RoundToInt((area / cell - 1f) * 0.5f));
            List<Vector3> edges = GetEdgeTileCenters(center, range);
            EffectQuad sweep = Prepare(center, area, 0f);
            EffectQuad floor = Prepare(center, area, 1f);
            floor.SetSorting(SortingLayer.NameToID("MapTile"), 23);
            var markers = new EffectQuad[edges.Count];
            var flashed = new bool[edges.Count];
            for (int i = 0; i < edges.Count; i++)
            {
                markers[i] = Prepare(edges[i], cell, 2f);
                markers[i].SetSorting(SortingLayer.NameToID("MapTile"), 26);
                markers[i].SetFloat(EffectQuad.AlphaId, 0.8f);
            }

            float progress = 0f;
            float fade = 1f;
            Sequence sequence = DOTween.Sequence().SetLink(sweep.GameObject, LinkBehaviour.KillOnDisable);
            sequence.Append(DOTween.To(() => progress, value => progress = value, 1f, m_spinDuration).SetEase(Ease.Linear));
            sequence.AppendInterval(m_holdDuration);
            sequence.Append(DOTween.To(() => fade, value => fade = value, 0f, m_fadeDuration).SetEase(Ease.InQuad));
            sequence.OnUpdate(() =>
            {
                sweep.SetFloat(EffectQuad.ProgressId, progress);
                sweep.SetFloat(EffectQuad.AlphaId, fade);
                floor.SetFloat(EffectQuad.ProgressId, progress);
                floor.SetFloat(EffectQuad.AlphaId, fade * 0.35f);
                for (int i = 0; i < markers.Length; i++)
                {
                    Vector3 direction = edges[i] - center;
                    float angle = Mathf.Repeat(Mathf.Atan2(direction.y, direction.x) / (Mathf.PI * 2f), 1f);
                    float passed = progress - angle;
                    float pulse = passed < 0f ? 0f : Mathf.Clamp01(1f - passed / 0.16f);
                    markers[i].SetFloat(EffectQuad.ProgressId, pulse);
                    markers[i].SetFloat(EffectQuad.AlphaId, fade * (0.65f + pulse * 0.35f));
                    if (!flashed[i] && passed >= 0f)
                    {
                        flashed[i] = true;
                        m_sparks.Burst(edges[i], 3, s_hot, cell, cell * 2.5f,
                            cell * 0.025f, cell * 0.06f, 0.12f, 0.24f, direction, 45f, cell * 0.12f);
                    }
                }
            });
            sequence.InsertCallback(m_spinDuration * 0.5f, () => ScreenShake.Play(0.045f, 0.14f));
            sequence.OnKill(() =>
            {
                foreach (EffectQuad marker in markers) m_pool.Release(marker);
                m_pool.Release(floor);
                m_pool.Release(sweep);
            });
        }

        private EffectQuad Prepare(Vector3 center, float size, float shape)
        {
            EffectQuad quad = m_pool.Get();
            quad.Transform.position = center;
            quad.Transform.localScale = new Vector3(size, size, 1f);
            quad.SetSorting(SortingLayer.NameToID("Projectile"), 28);
            quad.SetColor(s_edge);
            quad.SetColorById(EffectQuad.Color2Id, s_hot);
            quad.SetFloat(EffectQuad.ShapeId, shape);
            quad.SetFloat(EffectQuad.ProgressId, 0f);
            quad.SetFloat(EffectQuad.AlphaId, 0f);
            return quad;
        }

        private static List<Vector3> GetEdgeTileCenters(Vector3 position, int range)
        {
            var centers = new List<Vector3>();
            BoardManager board = BoardManager.Instance;
            if (board == null || board.Grid == null || !board.TryGetPoint(position, out BoardPoint point)) return centers;
            Vector3Int origin;
            if (point.IsSpawn) origin = board.Grid.GetSpawnCell(point.SpawnIndex);
            else if (point.IsPath) origin = board.Grid.GetPathCell(point.PathIndex);
            else return centers;

            foreach (int index in board.Grid.GetPathIndicesInSquare(origin, range))
            {
                Vector3Int cell = board.Grid.GetPathCell(index);
                if (BoardGrid.GetTileDistance(origin, cell) == range)
                    centers.Add(board.GetWorldPosition(cell));
            }
            return centers;
        }
    }
}
