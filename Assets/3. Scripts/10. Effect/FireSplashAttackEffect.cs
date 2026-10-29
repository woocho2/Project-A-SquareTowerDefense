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
        [SerializeField] private int m_spreadFlames = 12;

        private readonly System.Random m_random = new System.Random();
        private EffectQuadPool m_glowPool;
        private EffectQuadPool m_ballPool;
        private EffectQuadPool m_burstPool;
        private EffectQuadPool m_ringPool;
        private EffectQuadPool m_flarePool;
        private EffectParticles m_flames;
        private EffectParticles m_embers;
        private EffectParticles m_smoke;

        private void Awake()
        {
            m_glowPool = new EffectQuadPool(transform, "EffectSprite", "Fireball Glow", 26, EffectTextures.Glow, true);
            m_ballPool = new EffectQuadPool(transform, "EffectSprite", "Fireball Artwork", 27, EffectTextures.Fireball);
            m_burstPool = new EffectQuadPool(transform, "EffectSprite", "Fireball Flame Splash", 27, EffectTextures.MeteorBurst);
            m_ringPool = new EffectQuadPool(transform, "EffectSprite", "Fireball Ring", 25, EffectTextures.Ring, true);
            m_flarePool = new EffectQuadPool(transform, "EffectSprite", "Fireball Flare", 28, EffectTextures.Flare, true);
            m_smoke = new EffectParticles(transform, "Fireball Smoke", EffectTextures.Smoke, false, 1f, 23, false, -0.05f, true);
            m_flames = new EffectParticles(transform, "Fireball Flames", EffectTextures.Fireball, false, 1f, 26, false, -0.3f, true);
            m_embers = new EffectParticles(transform, "Fireball Embers", EffectTextures.Spark, true, 1.4f, 28, true, 0.6f, false);
        }

        private void OnDestroy()
        {
            m_glowPool?.Dispose(); m_ringPool?.Dispose(); m_flarePool?.Dispose();
            m_ballPool?.Dispose(); m_burstPool?.Dispose();
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

            EffectQuad glow = Prepare(m_glowPool, from, new Color(s_fire.r, s_fire.g, s_fire.b, 0.5f), 0.8f);
            EffectQuad body = Prepare(m_ballPool, from, s_fire, 1f);
            body.Transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(toTarget.y, toTarget.x) * Mathf.Rad2Deg + 90f);
            EffectQuad core = Prepare(m_glowPool, from, new Color(s_core.r, s_core.g, s_core.b, 0.65f), 1.1f);
            core.SetSortingOffset(2);
            float grown = 0f;

            Sequence sequence = DOTween.Sequence().SetLink(glow.GameObject, LinkBehaviour.KillOnDisable);
            sequence.Append(DOTween.To(() => grown, value => grown = value, 1f, m_appearDuration).SetEase(Ease.OutBack));
            sequence.AppendInterval(launchAt - m_appearDuration);
            sequence.Append(glow.Transform.DOMove(to, flight).SetEase(Ease.InOutSine));
            sequence.OnUpdate(() =>
            {
                float flicker = 1f + m_flicker * Mathf.Sin(Time.time * 37f) * Mathf.Sin(Time.time * 23f);
                Vector3 at = glow.Transform.position;
                body.Transform.position = at;
                core.Transform.position = at;
                SetSize(body, ball * 1.35f * grown * (1f + (flicker - 1f) * 0.3f));
                SetSize(glow, ball * 1.4f * grown * flicker);
                SetSize(core, ball * 0.3f * grown);
                if (Range(0f, 1f) < Time.deltaTime * 12f)
                    m_flames.Burst(at, 1, Range(0f, 1f) < 0.5f ? s_fire : s_deep, cell * 0.3f, cell * 0.8f, ball * 0.3f, ball * 0.5f,
                        0.16f, 0.3f, back, 70f, ball * 0.15f);
                if (Range(0f, 1f) < Time.deltaTime * 6f)
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
                m_ballPool.Release(body);
            });
        }

        private void PlaySpread(Vector3 center, float area, float cell)
        {
            float reach = area * 0.6f;
            TexturedFx.Pop(m_burstPool, center, Range(0f, 360f), cell * 0.4f, area * 1.25f, s_fire, 1f, 0f, 0.22f, 0.25f, Ease.OutCubic);
            TexturedFx.Pop(m_glowPool, center, 0f, cell * 0.35f, area * 0.9f, s_core, 1.2f, 0f, 0.12f, 0.2f);
            TexturedFx.Pop(m_flarePool, center, Range(0f, 90f), cell * 0.2f, area * 0.55f, s_core, 1.2f, 0f, 0.08f, 0.15f);
            TexturedFx.Pop(m_ringPool, center, 0f, cell * 0.25f, area * 1.1f, s_fire, 1.1f, 0f, m_spreadDuration * 0.8f, 0.25f, Ease.OutCubic);

            for (int i = 0; i < m_spreadFlames; i++)
            {
                float angle = (i + Range(-0.3f, 0.3f)) / m_spreadFlames * 360f;
                float radians = angle * Mathf.Deg2Rad;
                Vector2 direction = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
                float run = reach / m_spreadDuration * Range(0.7f, 1.2f);
                m_flames.Burst(center, 1, i % 3 == 0 ? s_deep : s_fire, run, run, cell * 0.3f, cell * 0.55f,
                    m_spreadDuration * 0.8f, m_spreadDuration * 1.2f, direction, 6f, cell * 0.1f);
            }
            m_flames.Burst(center, 4, s_core, cell * 0.3f, cell * 1f, cell * 0.3f, cell * 0.5f, 0.2f, 0.35f, Vector2.up, 360f, area * 0.15f);
            m_embers.Burst(center, 8, s_core, cell * 2f, cell * 4f, cell * 0.04f, cell * 0.07f, 0.25f, 0.45f, Vector2.up, 360f);
            m_smoke.Burst(center, 3, s_smoke, cell * 0.3f, cell * 0.7f, cell * 0.5f, cell * 0.85f, 0.35f, 0.55f, Vector2.up, 360f, area * 0.15f);
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
