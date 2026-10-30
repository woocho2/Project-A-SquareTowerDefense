using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace SquareTD
{
    // 블리자드(광역 얼음) 스킬 2 연출 — 눈보라가 화면을 비스듬히 휩쓸고 적마다 서리가 터져 얼어붙음
    [DisallowMultipleComponent]
    public sealed class IceSplashSkillEffect : MonoBehaviour
    {
        private static readonly Color s_sky = new Color(0.6f, 0.8f, 1f);
        private static readonly Color s_frost = new Color(0.55f, 0.85f, 1f);
        private static readonly Color s_ice = new Color(0.85f, 0.96f, 1f);
        private static readonly Color s_snow = new Color(0.92f, 0.97f, 1f, 0.85f);

        [SerializeField] private float m_stormDuration = 1.1f;
        [SerializeField] private float m_windAngle = -18f;
        [SerializeField] private float m_tick = 0.03f;
        [SerializeField] private int m_streaksPerTick = 4;
        [SerializeField] private int m_flakesPerTick = 5;
        [SerializeField] private float m_frostAt = 0.35f;
        [SerializeField] private float m_frostSpread = 0.5f;
        [SerializeField, Range(0f, 1f)] private float m_skyAlpha = 0.22f;

        private readonly System.Random m_random = new System.Random();
        private EffectQuadPool m_tintPool;
        private EffectQuadPool m_glowPool;
        private EffectQuadPool m_ringPool;
        private EffectQuadPool m_crystalPool;
        private EffectQuadPool m_flarePool;
        private EffectParticles m_streaks;
        private EffectParticles m_flakes;
        private EffectParticles m_shards;
        private EffectParticles m_puffs;

        private void Awake()
        {
            m_tintPool = new EffectQuadPool(transform, "EffectSprite", "Blizzard Sky", 18);
            m_glowPool = new EffectQuadPool(transform, "EffectSprite", "Blizzard Glow", 26, EffectTextures.Glow, true);
            m_ringPool = new EffectQuadPool(transform, "EffectSprite", "Blizzard Frost Ring", 27, EffectTextures.Ring, true);
            m_crystalPool = new EffectQuadPool(transform, "EffectSprite", "Blizzard Crystal", 28, EffectTextures.Snowflake, true);
            m_flarePool = new EffectQuadPool(transform, "EffectSprite", "Blizzard Flare", 29, EffectTextures.Flare, true);
            m_puffs = new EffectParticles(transform, "Blizzard Puffs", EffectTextures.Smoke, false, 1f, 24, false, -0.05f, true);
            m_streaks = new EffectParticles(transform, "Blizzard Wind", EffectTextures.Spark, true, 1.6f, 25, true, 0f, false);
            m_flakes = new EffectParticles(transform, "Blizzard Snow", EffectTextures.Snowflake, true, 1.4f, 25, false, 0.05f, true);
            m_shards = new EffectParticles(transform, "Blizzard Shards", EffectTextures.IceShard, true, 1.8f, 27, false, 1.2f, false);
        }

        private void OnDestroy()
        {
            m_tintPool?.Dispose(); m_glowPool?.Dispose(); m_ringPool?.Dispose();
            m_crystalPool?.Dispose(); m_flarePool?.Dispose();
        }

        public void Play(Camera camera, IReadOnlyList<Vector3> hitPositions, float cellWorldSize)
        {
            if (camera == null || !m_glowPool.IsReady) return;

            float cell = Mathf.Max(0.5f, cellWorldSize);
            float height = camera.orthographic ? camera.orthographicSize * 2f : 10f;
            float width = height * camera.aspect;
            Vector3 view = new Vector3(camera.transform.position.x, camera.transform.position.y, 0f);

            PlayTint(view, width, height, m_stormDuration + 0.3f);
            ScreenShake.Play(0.012f, m_stormDuration);
            for (float at = 0f; at < m_stormDuration; at += m_tick)
            {
                float envelope = Mathf.Sin(Mathf.PI * at / m_stormDuration);
                DOVirtual.DelayedCall(at, () => EmitStorm(view, width, height, cell, envelope))
                    .SetLink(gameObject, LinkBehaviour.KillOnDisable);
            }

            foreach (Vector3 hit in hitPositions)
            {
                Vector3 target = hit;
                DOVirtual.DelayedCall(m_frostAt + Range(0f, m_frostSpread), () => PlayFrost(target, cell))
                    .SetLink(gameObject, LinkBehaviour.KillOnDisable);
            }

            DOVirtual.DelayedCall(m_frostAt + m_frostSpread + 0.1f, () =>
            {
                ImpactFx.Frame(new Color(0.8f, 0.92f, 1f, 0.3f), 0.06f);
                ScreenShake.Play(0.03f, 0.15f);
            }).SetLink(gameObject, LinkBehaviour.KillOnDisable);
        }

        private void EmitStorm(Vector3 view, float width, float height, float cell, float envelope)
        {
            float radians = m_windAngle * Mathf.Deg2Rad;
            Vector2 wind = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
            Vector3 across = new Vector3(-wind.y, wind.x, 0f);
            Vector3 upwind = view - (Vector3)wind * (width * 0.6f);
            float span = Mathf.Max(width, height) * 0.75f;

            int streaks = Mathf.RoundToInt(m_streaksPerTick * envelope + 0.5f);
            for (int i = 0; i < streaks; i++)
            {
                float speed = width * Range(1.6f, 2.2f);
                m_streaks.Burst(upwind + across * Range(-span, span), 1, s_ice, speed, speed, cell * 0.06f, cell * 0.12f,
                    width * 1.3f / speed, width * 1.3f / speed, wind, 6f);
            }

            int flakes = Mathf.RoundToInt(m_flakesPerTick * envelope + 0.5f);
            for (int i = 0; i < flakes; i++)
            {
                float speed = width * Range(0.9f, 1.3f);
                m_flakes.Burst(upwind + across * Range(-span, span), 1, s_snow, speed, speed, cell * 0.12f, cell * 0.25f,
                    width * 1.3f / speed, width * 1.3f / speed, wind, 10f);
            }
        }

        private void PlayFrost(Vector3 target, float cell)
        {
            TexturedFx.Pop(m_glowPool, target, 0f, cell * 0.3f, cell * 1.6f, s_frost, 1.6f, 0f, 0.2f, 0.35f);
            TexturedFx.Pop(m_ringPool, target, 0f, cell * 0.2f, cell * 1.8f, s_frost, 1.8f, 0f, 0.3f, 0.3f, Ease.OutCubic);
            TexturedFx.Pop(m_crystalPool, target, Range(0f, 60f), cell * 0.2f, cell * 1.1f, s_ice, 2.2f, 0f, 0.25f, 0.4f, Ease.OutBack);
            TexturedFx.Pop(m_flarePool, target, 45f, cell * 0.2f, cell * 1.2f, Color.white, 2.5f, 0.02f, 0.1f, 0.2f);
            m_shards.Burst(target, 8, s_ice, cell * 2f, cell * 5f, cell * 0.15f, cell * 0.3f, 0.3f, 0.55f, Vector2.up, 300f);
            m_puffs.Burst(target, 3, s_snow, cell * 0.3f, cell * 0.8f, cell * 0.5f, cell * 0.9f, 0.4f, 0.7f, Vector2.up, 360f, cell * 0.15f);
        }

        private void PlayTint(Vector3 center, float width, float height, float duration)
        {
            EffectQuad sky = m_tintPool.Get();
            sky.Transform.position = center;
            sky.Transform.localScale = new Vector3(width * 1.8f, height * 1.8f, 1f);
            sky.SetColor(s_sky);
            sky.SetFloat(EffectQuad.AlphaId, 0f);

            float alpha = 0f;
            Sequence sequence = DOTween.Sequence().SetLink(sky.GameObject, LinkBehaviour.KillOnDisable);
            sequence.Append(DOTween.To(() => alpha, value => { alpha = value; sky.SetFloat(EffectQuad.AlphaId, value); },
                m_skyAlpha, 0.25f).SetEase(Ease.OutQuad));
            sequence.AppendInterval(Mathf.Max(0f, duration - 0.25f));
            sequence.Append(DOTween.To(() => alpha, value => { alpha = value; sky.SetFloat(EffectQuad.AlphaId, value); },
                0f, 0.4f).SetEase(Ease.InQuad));
            sequence.OnKill(() => m_tintPool.Release(sky));
        }

        private float Range(float min, float max) => Mathf.Lerp(min, max, (float)m_random.NextDouble());
    }
}
