using System;
using DG.Tweening;
using UnityEngine;

namespace SquareTD
{
    // 화염술사(광역 불) 기본 공격 연출 — 화염구가 천천히 날아가 닿으면 큰 불길을 사방으로 퍼뜨리며 사라짐
    [DisallowMultipleComponent]
    public sealed class FireSplashAttackEffect : MonoBehaviour, IShotEffect
    {
        private static readonly Color s_fire = new Color(1f, 0.42f, 0.08f);
        private static readonly Color s_core = new Color(1f, 0.88f, 0.55f);
        private static readonly Color s_deep = new Color(0.85f, 0.18f, 0.04f);
        private static readonly Color s_smoke = new Color(0.18f, 0.11f, 0.08f, 0.7f);

        [SerializeField] private float m_ballSize = 0.75f;
        [SerializeField] private float m_appearDuration = 0.15f;
        [SerializeField] private float m_slowness = 0.25f;
        [SerializeField] private float m_minFlight = 0.5f;
        [SerializeField] private float m_flicker = 0.12f;
        [SerializeField] private float m_spreadDuration = 0.4f;
        [SerializeField] private int m_spreadFlames = 28;

        private readonly System.Random m_random = new System.Random();
        private EffectQuadPool m_glowPool;
        private EffectQuadPool m_ringPool;
        private EffectQuadPool m_flarePool;
        private EffectParticles m_flames;
        private EffectParticles m_embers;
        private EffectParticles m_smoke;

        private void Awake()
        {
            m_glowPool = new EffectQuadPool(transform, "EffectSprite", "Fireball Glow", 26, EffectTextures.Glow, true);
            m_ringPool = new EffectQuadPool(transform, "EffectSprite", "Fireball Ring", 25, EffectTextures.Ring, true);
            m_flarePool = new EffectQuadPool(transform, "EffectSprite", "Fireball Flare", 28, EffectTextures.Flare, true);
            m_smoke = new EffectParticles(transform, "Fireball Smoke", EffectTextures.Smoke, false, 1f, 23, false, -0.05f, true);
            m_flames = new EffectParticles(transform, "Fireball Flames", EffectTextures.Glow, true, 2f, 26, false, -0.3f, true);
            m_embers = new EffectParticles(transform, "Fireball Embers", EffectTextures.Spark, true, 2.5f, 27, true, 0.6f, false);
        }

        private void OnDestroy()
        {
            m_glowPool?.Dispose(); m_ringPool?.Dispose(); m_flarePool?.Dispose();
        }

        public void FireShot(Vector3 from, Vector3 to, float speed, float hold, float sizeWorld, Action onArrived)
        {
            if (!m_glowPool.IsReady)
            {
                onArrived?.Invoke();
                return;
            }

            float cell = EffectSpace.CellWorldSize();
            float ball = cell * m_ballSize;
            Vector3 toTarget = to - from;
            Vector2 back = -(Vector2)toTarget.normalized;
            float flight = Mathf.Max(m_minFlight, toTarget.magnitude / Mathf.Max(0.01f, speed * m_slowness));
            float launchAt = Mathf.Max(hold, m_appearDuration);

            EffectQuad glow = Prepare(m_glowPool, from, s_fire, 2.2f);
            EffectQuad core = Prepare(m_glowPool, from, s_core, 3f);
            float grown = 0f;

            Sequence sequence = DOTween.Sequence().SetLink(glow.GameObject, LinkBehaviour.KillOnDisable);
            sequence.Append(DOTween.To(() => grown, value => grown = value, 1f, m_appearDuration).SetEase(Ease.OutBack));
            sequence.AppendInterval(launchAt - m_appearDuration);
            sequence.Append(glow.Transform.DOMove(to, flight).SetEase(Ease.InOutSine));
            sequence.OnUpdate(() =>
            {
                float flicker = 1f + m_flicker * Mathf.Sin(Time.time * 37f) * Mathf.Sin(Time.time * 23f);
                Vector3 at = glow.Transform.position;
                core.Transform.position = at;
                SetSize(glow, ball * 1.5f * grown * flicker);
                SetSize(core, ball * 0.6f * grown);
                if (Range(0f, 1f) < 0.7f)
                    m_flames.Burst(at, 1, Range(0f, 1f) < 0.5f ? s_fire : s_deep, cell * 0.3f, cell * 0.8f, ball * 0.4f, ball * 0.7f,
                        0.2f, 0.4f, back, 70f, ball * 0.25f);
                if (Range(0f, 1f) < 0.2f)
                    m_embers.Burst(at, 1, s_core, cell * 0.5f, cell * 1.5f, cell * 0.04f, cell * 0.07f, 0.25f, 0.4f, back, 90f);
            });
            sequence.OnComplete(() =>
            {
                PlaySpread(to, Mathf.Max(cell, sizeWorld), cell);
                onArrived?.Invoke();
            });
            sequence.OnKill(() =>
            {
                m_glowPool.Release(glow);
                m_glowPool.Release(core);
            });
        }

        private void PlaySpread(Vector3 center, float area, float cell)
        {
            float reach = area * 0.6f;
            TexturedFx.Pop(m_glowPool, center, 0f, cell * 0.5f, area * 1.6f, s_core, 2.6f, 0f, 0.16f, 0.35f);
            TexturedFx.Pop(m_flarePool, center, Range(0f, 90f), cell * 0.3f, area * 1.1f, Color.white, 3f, 0f, 0.1f, 0.2f);
            TexturedFx.Pop(m_ringPool, center, 0f, cell * 0.3f, area * 1.5f, s_fire, 2.4f, 0f, m_spreadDuration * 0.8f, 0.3f, Ease.OutCubic);
            TexturedFx.Pop(m_ringPool, center, 0f, cell * 0.2f, area * 1.1f, s_deep, 2f, 0.07f, m_spreadDuration * 0.8f, 0.3f, Ease.OutCubic);

            for (int i = 0; i < m_spreadFlames; i++)
            {
                float angle = (i + Range(-0.3f, 0.3f)) / m_spreadFlames * 360f;
                float radians = angle * Mathf.Deg2Rad;
                Vector2 direction = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
                float run = reach / m_spreadDuration * Range(0.7f, 1.2f);
                m_flames.Burst(center, 1, i % 3 == 0 ? s_deep : s_fire, run, run, cell * 0.5f, cell * 0.9f,
                    m_spreadDuration * 0.8f, m_spreadDuration * 1.2f, direction, 6f, cell * 0.1f);
            }
            m_flames.Burst(center, 10, s_core, cell * 0.3f, cell * 1f, cell * 0.5f, cell * 0.8f, 0.25f, 0.4f, Vector2.up, 360f, area * 0.2f);
            m_embers.Burst(center, 18, s_core, cell * 3f, cell * 7f, cell * 0.05f, cell * 0.1f, 0.3f, 0.6f, Vector2.up, 360f);
            m_smoke.Burst(center, 5, s_smoke, cell * 0.4f, cell * 1f, cell * 0.7f, cell * 1.2f, 0.6f, 1f, Vector2.up, 360f, area * 0.25f);
            ScreenShake.Play(0.04f, 0.12f);
        }

        private static EffectQuad Prepare(EffectQuadPool pool, Vector3 position, Color color, float intensity)
        {
            EffectQuad quad = pool.Get();
            quad.Transform.position = position;
            quad.Transform.localScale = Vector3.zero;
            quad.SetColor(color);
            quad.SetFloat(EffectQuad.IntensityId, intensity);
            return quad;
        }

        private static void SetSize(EffectQuad quad, float size) => quad.Transform.localScale = new Vector3(size, size, 1f);

        private float Range(float min, float max) => Mathf.Lerp(min, max, (float)m_random.NextDouble());
    }
}
