using System;
using DG.Tweening;
using UnityEngine;

namespace SquareTD
{
    // 도끼 광역 기본 공격 — 타격 구간을 가르는 묵직한 참격
    [DisallowMultipleComponent]
    public sealed class AxeSplashAttackEffect : MonoBehaviour, IShotEffect
    {
        private static readonly Color s_edge = new Color(1f, 0.42f, 0.12f);
        private static readonly Color s_hot = new Color(1.35f, 1.12f, 0.78f);

        [SerializeField, Min(0.01f)] private float m_windUpDuration = 0.08f;
        [SerializeField, Min(0.01f)] private float m_slashDuration = 0.09f;
        [SerializeField, Min(0f)] private float m_impactHold = 0.04f;
        [SerializeField, Min(0.01f)] private float m_fadeDuration = 0.22f;

        private EffectQuadPool m_pool;
        private EffectParticles m_sparks;

        private void Awake()
        {
            m_pool = new EffectQuadPool(transform, "AxeCleave", "Viking Cleave", 26, EffectTextures.SlashTrail);
            m_sparks = new EffectParticles(transform, "Cleave Sparks", EffectTextures.Spark,
                true, 1.5f, 30, true, 0.8f, false, true);
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
            float area = Mathf.Max(cell, sizeWorld);
            Vector3 center = GetTileCenter(to);
            EffectQuad rift = Prepare(center, area, 1f, 0);
            EffectQuad slash = Prepare(center, area, 0f, 1);
            EffectQuad flash = Prepare(center, area, 2f, 2);
            float drawn = 0f;
            float fade = 1f;
            float delay = Mathf.Max(hold, m_windUpDuration);
            float impactAt = delay + m_slashDuration;
            Sequence sequence = DOTween.Sequence().SetLink(slash.GameObject, LinkBehaviour.KillOnDisable);
            sequence.AppendInterval(delay);
            sequence.Append(DOTween.To(() => drawn, value => drawn = value, 1f, m_slashDuration).SetEase(Ease.OutCubic));
            sequence.AppendCallback(() =>
            {
                m_sparks.Burst(center, 12, s_hot, cell * 1.5f, cell * 4f,
                    cell * 0.025f, cell * 0.07f, 0.12f, 0.26f, new Vector2(1f, 1f), 70f, area * 0.18f);
                ScreenShake.Play(0.045f, 0.11f);
                onArrived?.Invoke();
            });
            sequence.AppendInterval(m_impactHold);
            sequence.Append(DOTween.To(() => fade, value => fade = value, 0f, m_fadeDuration).SetEase(Ease.InQuad));
            sequence.OnUpdate(() =>
            {
                slash.SetFloat(EffectQuad.ProgressId, drawn);
                slash.SetFloat(EffectQuad.AlphaId, fade);
                rift.SetFloat(EffectQuad.ProgressId, drawn);
                rift.SetFloat(EffectQuad.CoreId, drawn * fade);
                rift.SetFloat(EffectQuad.AlphaId, fade * 0.9f);
                float sinceImpact = sequence.Elapsed() - impactAt;
                flash.SetFloat(EffectQuad.AlphaId, sinceImpact < 0f ? 0f : Mathf.Clamp01(1f - sinceImpact / 0.075f));
            });
            sequence.OnKill(() =>
            {
                m_pool.Release(slash);
                m_pool.Release(flash);
                m_pool.Release(rift);
            });
        }

        private EffectQuad Prepare(Vector3 position, float size, float shape, int layer)
        {
            EffectQuad quad = m_pool.Get();
            quad.Transform.position = position;
            quad.Transform.localScale = new Vector3(size, size, 1f);
            quad.SetSortingOffset(layer);
            quad.SetColor(s_edge);
            quad.SetColorById(EffectQuad.Color2Id, s_hot);
            quad.SetFloat(EffectQuad.ShapeId, shape);
            quad.SetFloat(EffectQuad.ProgressId, 0f);
            quad.SetFloat(EffectQuad.AlphaId, 0f);
            return quad;
        }

        private static Vector3 GetTileCenter(Vector3 position)
        {
            BoardManager board = BoardManager.Instance;
            if (board != null && board.Grid != null && board.TryGetPoint(position, out BoardPoint point) && point.IsPath)
                return board.GetWorldPosition(board.Grid.GetPathCell(point.PathIndex));
            return position;
        }
    }
}
