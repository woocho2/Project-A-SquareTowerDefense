using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace SquareTD
{
    // 화염술사 기본 공격 — 타격 범위의 길 타일마다 같은 크기의 불기둥이 솟음
    [DisallowMultipleComponent]
    public sealed class FireSplashAttackEffect : MonoBehaviour, IShotEffect
    {
        private const float FireBaseUv = 0.19f;
        private static readonly Color s_fire = new Color(1f, 0.36f, 0.025f);
        private static readonly Color s_core = new Color(1f, 0.84f, 0.28f);
        private static readonly Color s_deep = new Color(0.65f, 0.065f, 0.012f);

        [SerializeField, Min(0.01f)] private float m_anticipation = 0.45f;
        [SerializeField, Min(0.01f)] private float m_riseDuration = 0.07f;
        [SerializeField, Min(0f)] private float m_sustainDuration = 0.1f;
        [SerializeField, Min(0.01f)] private float m_fadeDuration = 0.28f;
        [SerializeField, Min(0.1f)] private float m_pillarHeight = 2.1f;

        private EffectQuadPool m_pool;
        private EffectParticles m_embers;

        private void Awake()
        {
            m_pool = new EffectQuadPool(transform, "FirePillar", "Pyromancer Eruption", 25, "FX_FireEruption_Atlas_v2");
            m_embers = new EffectParticles(transform, "Eruption Embers", EffectTextures.Spark,
                true, 1.5f, 30, true, 0.5f, false, true);
        }

        private void OnDestroy() => m_pool?.Dispose();

        public void FireShot(Vector3 from, Vector3 to, float speed, float hold, float sizeWorld, Action onArrived)
        {
            if (!m_pool.IsReady)
            {
                onArrived?.Invoke();
                return;
            }

            float cell = EffectSpace.CellWorldSize();
            float diameter = cell;
            float height = cell * m_pillarHeight;
            List<Vector3> centers = GetTileCenters(to, sizeWorld, cell);
            var grounds = new EffectQuad[centers.Count];
            var blasts = new EffectQuad[centers.Count];
            var jets = new EffectQuad[centers.Count];
            for (int i = 0; i < centers.Count; i++)
            {
                grounds[i] = Prepare(centers[i], 1f, 0, new Color(0.15f, 0.025f, 0.012f), s_fire);
                grounds[i].SetSorting(SortingLayer.NameToID("MapTile"), 25);
                blasts[i] = Prepare(centers[i], 2f, 4 + i * 4, s_fire, s_core);
                jets[i] = Prepare(centers[i], 0f, 2 + i * 4, s_deep, s_fire);
            }
            float grown = 0f;
            float fade = 1f;
            float anticipation = Mathf.Max(hold, m_anticipation);
            Sequence sequence = DOTween.Sequence().SetLink(grounds[0].GameObject, LinkBehaviour.KillOnDisable);
            sequence.AppendInterval(anticipation);
            sequence.Append(DOTween.To(() => grown, value => grown = value, 1f, m_riseDuration).SetEase(Ease.OutExpo));
            sequence.AppendCallback(() =>
            {
                int embers = Mathf.Clamp(40 / centers.Count, 3, 14);
                foreach (Vector3 center in centers)
                    m_embers.Burst(center, embers, s_core, cell * 2f, cell * 4.5f,
                        cell * 0.035f, cell * 0.085f, 0.2f, 0.4f, Vector2.up, 85f, diameter * 0.3f);
                ScreenShake.Play(0.045f, 0.12f);
                onArrived?.Invoke();
            });
            sequence.AppendInterval(m_sustainDuration);
            sequence.Append(DOTween.To(() => fade, value => fade = value, 0f, m_fadeDuration).SetEase(Ease.InQuad));
            sequence.OnUpdate(() =>
            {
                float time = sequence.Elapsed();
                float charge = Mathf.Clamp01(time / anticipation);
                float eruption = Mathf.Max(0f, time - anticipation);
                float shock = Mathf.Clamp01(eruption / 0.2f);
                for (int i = 0; i < jets.Length; i++)
                {
                    grounds[i].Transform.localScale = new Vector3(diameter * (0.95f + charge * 0.12f), diameter * 0.65f, 1f);
                    grounds[i].SetFloat(EffectQuad.ProgressId, charge);
                    grounds[i].SetFloat(EffectQuad.AlphaId, (0.3f + charge * 0.7f) * (1f - grown * 0.55f) * fade);

                    blasts[i].Transform.localScale = new Vector3(diameter * (0.65f + shock * 0.55f), diameter * (0.4f + shock * 0.33f), 1f);
                    blasts[i].SetFloat(EffectQuad.ProgressId, shock);
                    blasts[i].SetFloat(EffectQuad.AlphaId, grown * (1f - shock));

                    float width = diameter * 1.3f;
                    float jetHeight = height;
                    jets[i].Transform.position = centers[i] + Vector3.up * (jetHeight * (0.5f - FireBaseUv));
                    jets[i].Transform.localScale = new Vector3(width, jetHeight, 1f);
                    jets[i].SetFloat(EffectQuad.AlphaId, Mathf.Clamp01(grown * 6f) * fade);
                    jets[i].SetFloat(EffectQuad.ProgressId, eruption);
                    jets[i].SetFloat(EffectQuad.DissolveId, 1f - fade);
                }
            });
            sequence.OnKill(() =>
            {
                foreach (EffectQuad jet in jets) m_pool.Release(jet);
                foreach (EffectQuad blast in blasts) m_pool.Release(blast);
                foreach (EffectQuad ground in grounds) m_pool.Release(ground);
            });
        }

        private EffectQuad Prepare(Vector3 center, float shape, int layer, Color outer, Color inner)
        {
            EffectQuad quad = m_pool.Get();
            quad.Transform.position = center;
            quad.Transform.localScale = Vector3.zero;
            quad.SetSorting(SortingLayer.NameToID("Projectile"), 25 + layer);
            quad.SetColor(outer);
            quad.SetColorById(EffectQuad.Color2Id, inner);
            quad.SetFloat(EffectQuad.ShapeId, shape);
            quad.SetFloat(EffectQuad.PhaseId, layer * 2.4f);
            quad.SetFloat(EffectQuad.AlphaId, 0f);
            return quad;
        }

        private static List<Vector3> GetTileCenters(Vector3 position, float sizeWorld, float cell)
        {
            var centers = new List<Vector3>();
            BoardManager board = BoardManager.Instance;
            if (board != null && board.Grid != null && board.TryGetPoint(position, out BoardPoint point) && point.IsPath)
            {
                int radius = Mathf.Max(0, Mathf.RoundToInt((sizeWorld / cell - 1f) * 0.5f));
                Vector3Int impactCell = board.Grid.GetPathCell(point.PathIndex);
                foreach (int index in board.Grid.GetPathIndicesInSquare(impactCell, radius))
                    centers.Add(board.GetWorldPosition(board.Grid.GetPathCell(index)));
            }
            if (centers.Count == 0) centers.Add(position);
            centers.Sort((a, b) => b.y.CompareTo(a.y));
            return centers;
        }
    }
}
