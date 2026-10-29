using System;
using DG.Tweening;
using UnityEngine;

namespace SquareTD
{
    // 다크메이지(광역 어둠) 기본 공격 연출 — 둘레 공간을 휘게 하는 작은 검은 구체가 날아가 닿으면 확 커졌다가 한 점으로 소멸
    [DisallowMultipleComponent]
    public sealed class DarknessSplashAttackEffect : MonoBehaviour, IShotEffect
    {
        private static readonly int s_horizonId = Shader.PropertyToID("_Horizon");
        private static readonly int s_strengthId = Shader.PropertyToID("_Strength");
        private static readonly int s_swirlId = Shader.PropertyToID("_Swirl");
        private static readonly int s_rimColorId = Shader.PropertyToID("_RimColor");

        private static readonly Color s_rim = new Color(0.75f, 0.3f, 1f);
        private static readonly Color s_purple = new Color(0.55f, 0.15f, 1f);
        private static readonly Color s_magenta = new Color(1f, 0.3f, 0.85f);

        [SerializeField] private float m_orbSize = 1.1f;
        [SerializeField] private float m_horizon = 0.22f;
        [SerializeField] private float m_strength = 0.8f;
        [SerializeField] private float m_swirl = 2.5f;
        [SerializeField] private float m_appearDuration = 0.15f;
        [SerializeField] private float m_slowness = 0.6f;
        [SerializeField] private float m_minFlight = 0.25f;
        [SerializeField] private float m_swellDuration = 0.12f;
        [SerializeField] private float m_swellHold = 0.08f;
        [SerializeField] private float m_vanishDuration = 0.2f;
        [SerializeField] private float m_swellStrength = 1.6f;

        private readonly System.Random m_random = new System.Random();
        private EffectQuadPool m_warpPool;
        private EffectQuadPool m_vortexPool;
        private EffectQuadPool m_glowPool;
        private EffectQuadPool m_ringPool;
        private EffectQuadPool m_flarePool;
        private EffectParticles m_motes;

        private void Awake()
        {
            m_warpPool = new EffectQuadPool(transform, "SpaceWarp", "Dark Orb", 25);
            m_vortexPool = new EffectQuadPool(transform, "EffectSprite", "Dark Orb Vortex", 26, EffectTextures.BlackHoleVortex);
            m_glowPool = new EffectQuadPool(transform, "EffectSprite", "Dark Orb Glow", 24, EffectTextures.Glow, true);
            m_ringPool = new EffectQuadPool(transform, "EffectSprite", "Dark Orb Ring", 26, EffectTextures.Ring, true);
            m_flarePool = new EffectQuadPool(transform, "EffectSprite", "Dark Orb Flare", 27, EffectTextures.Flare, true);
            m_motes = new EffectParticles(transform, "Dark Orb Motes", EffectTextures.Spark, true, 2.2f, 26, true, 0f, false);
        }

        private void OnDestroy()
        {
            m_warpPool?.Dispose(); m_glowPool?.Dispose(); m_ringPool?.Dispose(); m_flarePool?.Dispose();
            m_vortexPool?.Dispose();
        }

        public void FireShot(Vector3 from, Vector3 to, float speed, float hold, float sizeWorld, Action onArrived)
        {
            if (!m_warpPool.IsReady)
            {
                onArrived?.Invoke();
                return;
            }

            float cell = EffectSpace.CellWorldSize();
            float orb = cell * m_orbSize;
            float area = Mathf.Max(cell, sizeWorld);
            float flight = Mathf.Max(m_minFlight, (to - from).magnitude / Mathf.Max(0.01f, speed * m_slowness));
            float launchAt = Mathf.Max(hold, m_appearDuration);

            EffectQuad warp = m_warpPool.Get();
            warp.Transform.position = from;
            warp.Transform.localScale = Vector3.zero;
            warp.SetFloat(s_horizonId, m_horizon);
            warp.SetFloat(s_strengthId, m_strength);
            warp.SetFloat(s_swirlId, m_swirl);
            warp.SetColorById(s_rimColorId, s_rim);

            EffectQuad glow = m_glowPool.Get();
            glow.Transform.position = from;
            glow.Transform.localScale = Vector3.zero;
            glow.SetColor(new Color(s_purple.r, s_purple.g, s_purple.b, 0.35f));
            glow.SetFloat(EffectQuad.IntensityId, 0.6f);

            EffectQuad vortex = m_vortexPool.Get();
            vortex.Transform.position = from;
            vortex.Transform.localScale = Vector3.zero;
            vortex.SetColor(s_purple);
            float turn = Range(0f, 360f);

            float size = 0f;
            float strength = m_strength;
            Sequence sequence = DOTween.Sequence().SetLink(warp.GameObject, LinkBehaviour.KillOnDisable);
            sequence.Append(DOTween.To(() => size, value => size = value, orb, m_appearDuration).SetEase(Ease.OutBack));
            sequence.AppendInterval(launchAt - m_appearDuration);
            sequence.Append(warp.Transform.DOMove(to, flight).SetEase(Ease.InQuad));
            sequence.AppendCallback(() =>
            {
                onArrived?.Invoke();
                TexturedFx.Pop(m_flarePool, to, Range(0f, 90f), cell * 0.15f, area * 0.5f, s_magenta, 1.2f, 0f, 0.08f, 0.15f);
                ScreenShake.Play(0.03f, 0.1f);
            });
            sequence.Append(DOTween.To(() => size, value => size = value, area * 1.4f, m_swellDuration).SetEase(Ease.OutExpo));
            sequence.Join(DOTween.To(() => strength, value => strength = value, m_swellStrength, m_swellDuration));
            sequence.AppendInterval(m_swellHold);
            sequence.Append(DOTween.To(() => size, value => size = value, 0f, m_vanishDuration).SetEase(Ease.InBack));
            sequence.AppendCallback(() =>
            {
                TexturedFx.Pop(m_flarePool, to, 0f, cell * 0.1f, cell * 0.7f, s_rim, 1.2f, 0f, 0.06f, 0.12f);
                TexturedFx.Pop(m_ringPool, to, 0f, cell * 0.15f, area * 0.55f, s_purple, 1f, 0f, 0.13f, 0.16f, Ease.OutCubic);
                m_motes.Burst(to, 6, s_magenta, cell * 1.5f, cell * 3f, cell * 0.04f, cell * 0.07f, 0.15f, 0.25f);
            });
            sequence.OnUpdate(() =>
            {
                Vector3 at = warp.Transform.position;
                float shown = Mathf.Max(0f, size);
                warp.Transform.localScale = new Vector3(shown, shown, 1f);
                warp.SetFloat(s_strengthId, strength);
                glow.Transform.position = at;
                glow.Transform.localScale = new Vector3(shown * 1.15f, shown * 1.15f, 1f);
                turn -= 160f * Time.deltaTime;
                vortex.Transform.position = at;
                vortex.Transform.rotation = Quaternion.Euler(0f, 0f, turn);
                vortex.Transform.localScale = new Vector3(shown * 1.05f, shown * 1.05f, 1f);
                if (Range(0f, 1f) < Time.deltaTime * 12f)
                {
                    float radians = Range(0f, Mathf.PI * 2f);
                    Vector3 outside = new Vector3(Mathf.Cos(radians), Mathf.Sin(radians), 0f) * (shown * 0.55f);
                    m_motes.Burst(at + outside, 1, Range(0f, 1f) < 0.5f ? s_purple : s_magenta, shown * 1.5f, shown * 2.2f,
                        cell * 0.04f, cell * 0.07f, 0.2f, 0.25f, -outside, 20f);
                }
            });
            sequence.OnKill(() =>
            {
                m_warpPool.Release(warp);
                m_glowPool.Release(glow);
                m_vortexPool.Release(vortex);
            });
        }

        private float Range(float min, float max) => Mathf.Lerp(min, max, (float)m_random.NextDouble());
    }
}
