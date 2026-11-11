using System;
using DG.Tweening;
using UnityEngine;

namespace SquareTD
{
    // 바이킹(광역 도끼) 기본 공격 — 대도끼 내려찍기
    [DisallowMultipleComponent]
    public sealed class AxeSplashAttackEffect : MonoBehaviour, IShotEffect
    {
        private static readonly Color s_blood = new Color(0.85f, 0.05f, 0.07f);
        private static readonly Color s_hot = new Color(1f, 0.6f, 0.55f);
        private static readonly Color s_rock = new Color(0.22f, 0.16f, 0.16f);
        private static readonly Color s_dust = new Color(0.45f, 0.38f, 0.36f, 0.7f);
        private static readonly Vector3 s_impactDir = new Vector3(0.5f, -0.866f, 0f);

        [Header("내려찍기")]
        [SerializeField, Min(0.01f)] private float m_windUp = 0.1f;
        [SerializeField, Min(0.01f)] private float m_chopDuration = 0.1f;
        [Tooltip("궤적의 반지름 (칸). 궤적은 표적 바로 위에서 시작해 표적에 꽂힘")]
        [SerializeField] private float m_arcRadius = 0.9f;
        [SerializeField] private float m_arcFade = 0.16f;
        [Header("균열")]
        [SerializeField] private int m_crackCount = 7;
        [SerializeField] private float m_crackHold = 0.25f;
        [SerializeField] private float m_crackFade = 0.35f;
        [Header("흔들림")]
        [SerializeField] private float m_shakeStrength = 0.07f;
        [SerializeField] private float m_shakeDuration = 0.14f;

        private readonly System.Random m_random = new System.Random();
        private EffectQuadPool m_arcPool;
        private EffectQuadPool m_arcCorePool;
        private EffectQuadPool m_crackPool;
        private EffectQuadPool m_ringPool;
        private EffectQuadPool m_flarePool;
        private EffectParticles m_sparks;
        private EffectParticles m_rocks;
        private EffectParticles m_dust;

        private void Awake()
        {
            m_arcPool = new EffectQuadPool(transform, "EffectSprite", "Viking Chop", 26, EffectTextures.SlashTrail, true, ArcMesh.Get(120f, 0.45f));
            m_arcCorePool = new EffectQuadPool(transform, "EffectSprite", "Viking Chop Core", 27, EffectTextures.SlashTrail, true, ArcMesh.Get(120f, 0.16f));
            m_crackPool = new EffectQuadPool(transform, "EffectSprite", "Viking Crack", 22, EffectTextures.SlashLine, true);
            m_ringPool = new EffectQuadPool(transform, "EffectSprite", "Viking Shock", 23, EffectTextures.Ring, true);
            m_flarePool = new EffectQuadPool(transform, "EffectSprite", "Viking Flash", 28, EffectTextures.Flare, true);
            m_sparks = new EffectParticles(transform, "Viking Sparks", EffectTextures.Spark, true, 2.5f, 29, true, 1.2f, false);
            m_rocks = new EffectParticles(transform, "Viking Rocks", EffectTextures.IceShard, false, 1f, 25, false, 2.5f, false);
            m_dust = new EffectParticles(transform, "Viking Dust", EffectTextures.Smoke, false, 1f, 21, false, -0.05f, false);
        }

        private void OnDestroy()
        {
            m_arcPool?.Dispose(); m_arcCorePool?.Dispose(); m_crackPool?.Dispose(); m_ringPool?.Dispose(); m_flarePool?.Dispose();
        }

        public void FireShot(Vector3 from, Vector3 to, float speed, float hold, float sizeWorld, Action onArrived)
        {
            if (!m_arcPool.IsReady)
            {
                onArrived?.Invoke();
                return;
            }

            float cell = EffectSpace.CellWorldSize();
            float area = Mathf.Max(cell, sizeWorld);
            Vector3 center = GetTileCenter(to);
            float delay = Mathf.Max(hold, m_windUp);
            float radius = cell * m_arcRadius;
            Vector3 pivot = center - s_impactDir * radius;

            Chop(m_arcPool, pivot, radius, s_blood, 2.5f, delay, m_arcFade);
            Chop(m_arcCorePool, pivot, radius, s_hot, 3.5f, delay, m_arcFade * 0.8f);

            DOVirtual.DelayedCall(delay + m_chopDuration, () =>
            {
                Impact(center, area, cell);
                onArrived?.Invoke();
            }).SetLink(gameObject, LinkBehaviour.KillOnDisable);
        }

        private void Chop(EffectQuadPool pool, Vector3 pivot, float radius, Color color, float intensity, float delay, float fade)
        {
            EffectQuad arc = pool.Get();
            arc.Transform.position = pivot;
            arc.Transform.localScale = new Vector3(radius, -radius, 1f);
            arc.SetColor(color);
            arc.SetFloat(EffectQuad.IntensityId, intensity);
            arc.SetFloat(EffectQuad.HeadFollowId, 1f);
            arc.SetFloat(EffectQuad.ProgressId, 0f);
            arc.SetFloat(EffectQuad.TailId, 0f);

            float progress = 0f, erased = 0f;
            Sequence sequence = DOTween.Sequence().SetLink(arc.GameObject, LinkBehaviour.KillOnDisable);
            sequence.AppendInterval(delay);
            sequence.Append(DOTween.To(() => progress, v => { progress = v; arc.SetFloat(EffectQuad.ProgressId, v); }, 1f, m_chopDuration)
                .SetEase(Ease.InCubic));
            sequence.AppendInterval(0.03f);
            sequence.Append(DOTween.To(() => erased, v => { erased = v; arc.SetFloat(EffectQuad.TailId, v); }, 1f, fade).SetEase(Ease.InQuad));
            sequence.OnKill(() => pool.Release(arc));
        }

        private void Impact(Vector3 center, float area, float cell)
        {
            for (int i = 0; i < m_crackCount; i++)
            {
                float angle = i * 360f / m_crackCount + Range(-18f, 18f);
                float length = area * Range(0.35f, 0.6f);
                float radians = angle * Mathf.Deg2Rad;
                Vector3 middle = center + new Vector3(Mathf.Cos(radians), Mathf.Sin(radians), 0f) * (length * 0.5f);
                TexturedFx.Swipe(m_crackPool, middle, angle, new Vector3(length, cell * Range(0.08f, 0.14f), 1f), s_blood, 2.5f,
                    Range(0f, 0.03f), 0.06f, m_crackHold, m_crackFade);
            }

            TexturedFx.Pop(m_ringPool, center, 0f, cell * 0.2f, area * 1.4f, s_blood, 2.2f, 0f, 0.3f, 0.3f, Ease.OutCubic);
            TexturedFx.Pop(m_ringPool, center, 0f, cell * 0.1f, area * 0.8f, s_hot, 2.5f, 0f, 0.16f, 0.18f, Ease.OutCubic);
            TexturedFx.Pop(m_flarePool, center, Range(0f, 45f), cell * 0.3f, area * 0.9f, s_hot, 3f, 0f, 0.07f, 0.14f);

            m_sparks.Burst(center, 12, s_blood, cell * 3f, cell * 7f, cell * 0.05f, cell * 0.1f, 0.2f, 0.4f, Vector2.up, 150f);
            m_rocks.Burst(center, 8, s_rock, cell * 2f, cell * 4.5f, cell * 0.08f, cell * 0.16f, 0.4f, 0.6f, Vector2.up, 120f, cell * 0.15f);
            m_dust.Burst(center, 6, s_dust, cell * 0.4f, cell * 1f, cell * 0.4f, cell * 0.7f, 0.5f, 0.8f, default, 360f, cell * 0.2f);

            ScreenShake.Play(m_shakeStrength, m_shakeDuration);
            ImpactFx.Punch(0.015f, 0.16f);
        }

        private float Range(float min, float max) => Mathf.Lerp(min, max, (float)m_random.NextDouble());

        private static Vector3 GetTileCenter(Vector3 position)
        {
            BoardManager board = BoardManager.Instance;
            if (board != null && board.Grid != null && board.TryGetPoint(position, out BoardPoint point) && point.IsPath)
                return board.GetWorldPosition(board.Grid.GetPathCell(point.PathIndex));
            return position;
        }
    }
}
