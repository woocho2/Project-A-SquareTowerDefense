using System;
using DG.Tweening;
using UnityEngine;

namespace SquareTD
{
    // 소서러(광역 얼음) 기본 공격 연출 — 거대한 눈덩이가 구르며 포물선으로 날아가 닿으면 펑 터짐
    [DisallowMultipleComponent]
    public sealed class IceSplashAttackEffect : MonoBehaviour, IShotEffect
    {
        private static readonly Color s_snow = new Color(0.95f, 0.98f, 1f);
        private static readonly Color s_shadow = new Color(0.62f, 0.76f, 0.92f);
        private static readonly Color s_frost = new Color(0.55f, 0.85f, 1f);
        private static readonly Color s_puff = new Color(0.94f, 0.97f, 1f, 0.9f);

        [SerializeField] private float m_ballSize = 1.1f;
        [SerializeField] private float m_appearDuration = 0.18f;
        [SerializeField] private float m_slowness = 0.45f;
        [SerializeField] private float m_minFlight = 0.4f;
        [SerializeField] private float m_arcHeight = 1.4f;
        [SerializeField] private float m_spin = 540f;
        [SerializeField] private float m_squashDuration = 0.06f;
        [SerializeField] private int m_burstPuffs = 12;

        private readonly System.Random m_random = new System.Random();
        private EffectQuadPool m_ballPool;
        private EffectQuadPool m_glowPool;
        private EffectQuadPool m_ringPool;
        private EffectQuadPool m_crystalPool;
        private EffectParticles m_puffs;
        private EffectParticles m_flakes;
        private EffectParticles m_shards;

        private void Awake()
        {
            m_ballPool = new EffectQuadPool(transform, "EffectSprite", "Snowball", 26, EffectTextures.Snowball);
            m_glowPool = new EffectQuadPool(transform, "EffectSprite", "Snowball Glow", 25, EffectTextures.Glow, true);
            m_ringPool = new EffectQuadPool(transform, "EffectSprite", "Snowball Ring", 27, EffectTextures.Ring, true);
            m_crystalPool = new EffectQuadPool(transform, "EffectSprite", "Snowball Crystal", 28, EffectTextures.Snowflake);
            m_puffs = new EffectParticles(transform, "Snowball Puffs", EffectTextures.Smoke, false, 1f, 27, false, 0.15f, true);
            m_flakes = new EffectParticles(transform, "Snowball Flakes", EffectTextures.Snowflake, false, 1f, 28, false, 0.3f, true);
            m_shards = new EffectParticles(transform, "Snowball Shards", EffectTextures.IceShard, false, 1f, 28, false, 1.4f, false);
        }

        private void OnDestroy()
        {
            m_ballPool?.Dispose(); m_glowPool?.Dispose(); m_ringPool?.Dispose(); m_crystalPool?.Dispose();
        }

        public void FireShot(Vector3 from, Vector3 to, float speed, float hold, float sizeWorld, Action onArrived)
        {
            if (!m_ballPool.IsReady)
            {
                onArrived?.Invoke();
                return;
            }

            float cell = EffectSpace.CellWorldSize();
            float ball = cell * m_ballSize;
            Vector3 toTarget = to - from;
            float side = toTarget.x >= 0f ? -1f : 1f;
            float flight = Mathf.Max(m_minFlight, toTarget.magnitude / Mathf.Max(0.01f, speed * m_slowness));
            float launchAt = Mathf.Max(hold, m_appearDuration);

            EffectQuad halo = Prepare(m_glowPool, from, new Color(s_frost.r, s_frost.g, s_frost.b, 0.25f));
            EffectQuad body = Prepare(m_ballPool, from, s_snow);
            body.SetSortingOffset(1);
            float grown = 0f;
            float turn = 0f;

            Sequence sequence = DOTween.Sequence().SetLink(body.GameObject, LinkBehaviour.KillOnDisable);
            sequence.Append(DOTween.To(() => grown, value => grown = value, 1f, m_appearDuration).SetEase(Ease.OutBack));
            sequence.AppendInterval(launchAt - m_appearDuration);
            sequence.Append(body.Transform.DOJump(to, cell * m_arcHeight, 1, flight).SetEase(Ease.Linear));
            sequence.Join(DOTween.To(() => turn, value => turn = value, side * m_spin * flight, flight).SetEase(Ease.Linear));
            sequence.OnUpdate(() =>
            {
                Vector3 at = body.Transform.position;
                body.Transform.rotation = Quaternion.Euler(0f, 0f, turn);
                halo.Transform.position = at;
                SetSize(body, ball * grown);
                SetSize(halo, ball * 1.3f * grown);
                if (Range(0f, 1f) < Time.deltaTime * 10f)
                    m_flakes.Burst(at, 1, s_frost, cell * 0.2f, cell * 0.6f, cell * 0.08f, cell * 0.14f, 0.3f, 0.5f, Vector2.zero, 360f, ball * 0.4f);
                if (Range(0f, 1f) < Time.deltaTime * 5f)
                    m_puffs.Burst(at, 1, s_puff, cell * 0.1f, cell * 0.3f, ball * 0.25f, ball * 0.4f, 0.25f, 0.4f, Vector2.zero, 360f, ball * 0.3f);
            });
            sequence.Append(DOTween.To(() => grown, value => grown = value, 0.8f, m_squashDuration).SetEase(Ease.OutQuad));
            sequence.OnComplete(() =>
            {
                PlayBurst(to, Mathf.Max(cell, sizeWorld), cell);
                onArrived?.Invoke();
            });
            sequence.OnKill(() =>
            {
                m_ballPool.Release(body);
                m_glowPool.Release(halo);
            });
        }

        private void PlayBurst(Vector3 center, float area, float cell)
        {
            TexturedFx.Pop(m_glowPool, center, 0f, cell * 0.35f, area * 0.9f, s_frost, 1f, 0f, 0.12f, 0.2f);
            TexturedFx.Pop(m_ringPool, center, 0f, cell * 0.3f, area * 1.1f, s_frost, 1.1f, 0f, 0.24f, 0.22f, Ease.OutCubic);
            TexturedFx.Pop(m_crystalPool, center, Range(0f, 60f), cell * 0.3f, area * 0.9f, s_frost, 1f, 0.02f, 0.18f, 0.22f, Ease.OutBack);
            TexturedFx.Pop(m_ballPool, center, Range(0f, 360f), cell * 0.2f, area * 0.5f, s_shadow, 1f, 0f, 0.06f, 0.16f);

            float reach = area * 0.55f;
            for (int i = 0; i < m_burstPuffs; i++)
            {
                float radians = (i + Range(-0.3f, 0.3f)) / m_burstPuffs * Mathf.PI * 2f;
                Vector2 direction = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
                float run = reach / 0.35f * Range(0.6f, 1.1f);
                m_puffs.Burst(center, 1, s_puff, run, run, cell * 0.6f, cell * 1f, 0.35f, 0.55f, direction, 10f, cell * 0.1f);
            }
            m_puffs.Burst(center, 4, s_puff, cell * 0.2f, cell * 0.6f, cell * 0.9f, cell * 1.3f, 0.4f, 0.6f, Vector2.up, 360f);
            m_flakes.Burst(center, 10, s_frost, cell * 2f, cell * 4f, cell * 0.1f, cell * 0.2f, 0.3f, 0.55f, Vector2.up, 360f);
            m_shards.Burst(center, 8, s_frost, cell * 3f, cell * 6f, cell * 0.15f, cell * 0.28f, 0.3f, 0.5f, Vector2.up, 300f);
            ScreenShake.Play(0.035f, 0.1f);
        }

        private static EffectQuad Prepare(EffectQuadPool pool, Vector3 position, Color color)
        {
            EffectQuad quad = pool.Get();
            quad.Transform.position = position;
            quad.Transform.localScale = Vector3.zero;
            quad.SetColor(color);
            return quad;
        }

        private static void SetSize(EffectQuad quad, float size) => quad.Transform.localScale = new Vector3(size, size, 1f);

        private float Range(float min, float max) => Mathf.Lerp(min, max, (float)m_random.NextDouble());
    }
}
