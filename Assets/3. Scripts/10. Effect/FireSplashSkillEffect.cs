using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace SquareTD
{
    // 메테오(광역 불) 스킬 2 연출 — 하늘이 붉게 어두워지고 불덩이가 적마다 비스듬히 떨어져 터짐
    [DisallowMultipleComponent]
    public sealed class FireSplashSkillEffect : MonoBehaviour
    {
        private static readonly Color s_sky = new Color(0.22f, 0.03f, 0.01f);
        private static readonly Color s_fire = new Color(1f, 0.42f, 0.08f);
        private static readonly Color s_core = new Color(1f, 0.86f, 0.5f);
        private static readonly Color s_smoke = new Color(0.16f, 0.1f, 0.08f, 0.75f);

        [SerializeField] private int m_maxMeteors = 30;
        [SerializeField] private int m_extraMeteors = 6;
        [SerializeField] private float m_dimDuration = 0.25f;
        [SerializeField] private float m_spread = 0.7f;
        [SerializeField] private float m_fallDuration = 0.35f;
        [SerializeField] private float m_fallAngle = 62f;
        [SerializeField] private float m_meteorSize = 0.9f;
        [SerializeField] private float m_afterglow = 0.5f;
        [SerializeField, Range(0f, 1f)] private float m_skyDarkness = 0.45f;

        private readonly System.Random m_random = new System.Random();
        private EffectQuadPool m_tintPool;
        private EffectQuadPool m_trailPool;
        private EffectQuadPool m_glowPool;
        private EffectQuadPool m_meteorPool;
        private EffectQuadPool m_burstPool;
        private EffectQuadPool m_ringPool;
        private EffectQuadPool m_flarePool;
        private EffectParticles m_flames;
        private EffectParticles m_embers;
        private EffectParticles m_smoke;

        private void Awake()
        {
            m_tintPool = new EffectQuadPool(transform, "EffectSprite", "Meteor Sky", 18);
            m_trailPool = new EffectQuadPool(transform, "EffectSprite", "Meteor Trail", 25, EffectTextures.SlashTrail, true);
            m_glowPool = new EffectQuadPool(transform, "EffectSprite", "Meteor Glow", 26, EffectTextures.Glow, true);
            m_meteorPool = new EffectQuadPool(transform, "EffectSprite", "Meteor Rock", 27, EffectTextures.MeteorCore);
            m_burstPool = new EffectQuadPool(transform, "EffectSprite", "Meteor Fire Splash", 27, EffectTextures.MeteorBurst);
            m_ringPool = new EffectQuadPool(transform, "EffectSprite", "Meteor Shock", 27, EffectTextures.Ring, true);
            m_flarePool = new EffectQuadPool(transform, "EffectSprite", "Meteor Flare", 28, EffectTextures.Flare, true);
            m_smoke = new EffectParticles(transform, "Meteor Smoke", EffectTextures.Smoke, false, 1f, 23, false, -0.05f, true);
            m_flames = new EffectParticles(transform, "Meteor Flames", EffectTextures.Glow, true, 2f, 26, false, -0.3f, true);
            m_embers = new EffectParticles(transform, "Meteor Embers", EffectTextures.Spark, true, 2.5f, 27, true, 0.8f, false);
        }

        private void OnDestroy()
        {
            m_tintPool?.Dispose(); m_trailPool?.Dispose();
            m_glowPool?.Dispose(); m_ringPool?.Dispose(); m_flarePool?.Dispose();
            m_meteorPool?.Dispose(); m_burstPool?.Dispose();
        }

        public void Play(Camera camera, IReadOnlyList<Vector3> hitPositions, float cellWorldSize)
        {
            if (camera == null || !m_glowPool.IsReady) return;

            float cell = Mathf.Max(0.5f, cellWorldSize);
            float height = camera.orthographic ? camera.orthographicSize * 2f : 10f;
            float width = height * camera.aspect;
            Vector3 view = new Vector3(camera.transform.position.x, camera.transform.position.y, 0f);

            var lands = new List<Vector3>();
            for (int i = 0; i < hitPositions.Count && lands.Count < m_maxMeteors; i++) lands.Add(hitPositions[i]);
            int real = lands.Count;
            for (int i = 0; i < m_extraMeteors; i++)
                lands.Add(view + new Vector3(Range(-0.45f, 0.45f) * width, Range(-0.45f, 0.45f) * height, 0f));

            float lastImpact = m_dimDuration + m_fallDuration;
            for (int i = 0; i < lands.Count; i++)
            {
                bool big = i < real;
                float startAt = m_dimDuration + Range(0f, m_spread);
                PlayMeteor(lands[i], cell * (big ? 1f : Range(0.5f, 0.75f)), height, startAt, big);
                lastImpact = Mathf.Max(lastImpact, startAt + m_fallDuration);
            }

            PlayTint(view, width, height, lastImpact + m_afterglow);
            DOVirtual.DelayedCall(lastImpact, () =>
            {
                ImpactFx.Frame(new Color(1f, 0.55f, 0.25f, 0.22f), 0.05f);
                ScreenShake.Play(0.06f, 0.25f);
            }).SetLink(gameObject, LinkBehaviour.KillOnDisable);
        }

        private void PlayMeteor(Vector3 land, float size, float travel, float startAt, bool big)
        {
            float radians = m_fallAngle * Mathf.Deg2Rad;
            Vector3 back = new Vector3(Mathf.Cos(radians), Mathf.Sin(radians), 0f);
            Vector3 start = land + back * travel;
            float ball = size * m_meteorSize;

            EffectQuad glow = Prepare(m_glowPool, s_fire, 1.35f);
            EffectQuad core = Prepare(m_meteorPool, new Color(1f, 0.65f, 0.34f), 1f);
            core.Transform.rotation = Quaternion.Euler(0f, 0f, m_fallAngle);
            EffectQuad trail = Prepare(m_trailPool, s_fire, 1.4f);
            trail.Transform.rotation = Quaternion.Euler(0f, 0f, m_fallAngle + 180f);

            float t = 0f;
            Sequence sequence = DOTween.Sequence().SetLink(glow.GameObject, LinkBehaviour.KillOnDisable);
            sequence.AppendInterval(startAt);
            sequence.AppendCallback(() =>
            {
                glow.Transform.localScale = new Vector3(ball * 1.5f, ball * 1.5f, 1f);
                core.Transform.localScale = new Vector3(ball * 1.25f, ball * 1.25f, 1f);
                trail.Transform.localScale = new Vector3(ball * 3.7f, ball * 0.85f, 1f);
            });
            sequence.Append(DOTween.To(() => t, value =>
            {
                t = value;
                Vector3 at = Vector3.LerpUnclamped(start, land, value);
                glow.Transform.position = at;
                core.Transform.position = at;
                trail.Transform.position = at + back * (ball * 2f);
                if (Range(0f, 1f) < 0.35f)
                    m_flames.Burst(at, 1, s_fire, size * 0.2f, size * 0.6f, ball * 0.35f, ball * 0.6f, 0.15f, 0.3f, back, 50f, ball * 0.2f);
            }, 1f, m_fallDuration).SetEase(Ease.InQuad));
            sequence.AppendCallback(() => PlayImpact(land, size, big));
            sequence.OnKill(() =>
            {
                m_glowPool.Release(glow);
                m_meteorPool.Release(core);
                m_trailPool.Release(trail);
            });
        }

        private void PlayImpact(Vector3 land, float size, bool big)
        {
            float blast = big ? 1f : 0.65f;
            TexturedFx.Pop(m_burstPool, land, Range(0f, 360f), size * 0.5f, size * 2.3f * blast, s_fire, 1f, 0f, 0.16f, 0.45f, Ease.OutCubic);
            TexturedFx.Pop(m_glowPool, land, 0f, size * 0.4f, size * 1.8f * blast, s_core, 1.35f, 0f, 0.18f, 0.3f);
            TexturedFx.Pop(m_flarePool, land, Range(0f, 90f), size * 0.3f, size * 1.2f * blast, Color.white, 1.8f, 0f, 0.08f, 0.18f);
            TexturedFx.Pop(m_ringPool, land, 0f, size * 0.3f, size * 2.4f * blast, s_fire, 1.4f, 0f, 0.3f, 0.3f, Ease.OutCubic);
            m_flames.Burst(land, big ? 10 : 5, s_fire, size * 1f, size * 3f, size * 0.25f, size * 0.5f, 0.25f, 0.5f, Vector2.up, 200f, size * 0.15f);
            m_embers.Burst(land, big ? 14 : 5, s_core, size * 3f, size * 7f, size * 0.05f, size * 0.1f, 0.3f, 0.6f, Vector2.up, 160f);
            m_smoke.Burst(land, big ? 4 : 2, s_smoke, size * 0.3f, size * 0.8f, size * 0.6f, size * 1.1f, 0.6f, 1f, Vector2.up, 120f, size * 0.2f);
            if (big) ScreenShake.Play(0.025f, 0.1f);
        }

        private void PlayTint(Vector3 center, float width, float height, float duration)
        {
            EffectQuad sky = Prepare(m_tintPool, s_sky, 1f);
            sky.Transform.position = center;
            sky.Transform.localScale = new Vector3(width * 1.8f, height * 1.8f, 1f);
            sky.SetFloat(EffectQuad.AlphaId, 0f);

            float alpha = 0f;
            Sequence sequence = DOTween.Sequence().SetLink(sky.GameObject, LinkBehaviour.KillOnDisable);
            sequence.Append(DOTween.To(() => alpha, value => { alpha = value; sky.SetFloat(EffectQuad.AlphaId, value); },
                m_skyDarkness, m_dimDuration).SetEase(Ease.OutQuad));
            sequence.AppendInterval(Mathf.Max(0f, duration - m_dimDuration));
            sequence.Append(DOTween.To(() => alpha, value => { alpha = value; sky.SetFloat(EffectQuad.AlphaId, value); },
                0f, 0.4f).SetEase(Ease.InQuad));
            sequence.OnKill(() => m_tintPool.Release(sky));
        }

        private static EffectQuad Prepare(EffectQuadPool pool, Color color, float intensity)
        {
            EffectQuad quad = pool.Get();
            quad.Transform.localScale = Vector3.zero;
            quad.SetColor(color);
            quad.SetFloat(EffectQuad.IntensityId, intensity);
            return quad;
        }

        private float Range(float min, float max) => Mathf.Lerp(min, max, (float)m_random.NextDouble());
    }
}
