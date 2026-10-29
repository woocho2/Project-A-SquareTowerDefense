using System;
using DG.Tweening;
using UnityEngine;

namespace SquareTD
{
    // 블레이드 댄서(단일 검) 기본 공격 연출
    [DisallowMultipleComponent]
    public sealed class SwordTargetAttackEffect : MonoBehaviour, IShotEffect
    {
        private static readonly Color s_blade = new Color(0.72f, 0.88f, 1f);
        private static readonly Color s_halo = new Color(0.3f, 0.6f, 1f);

        [SerializeField] private float m_glintSize = 0.6f;
        [SerializeField] private float m_streakDuration = 0.05f;
        [Header("초승달 참격")]
        [SerializeField] private float m_crescentRadius = 0.85f;
        [SerializeField] private float m_crescentDraw = 0.07f;
        [SerializeField] private float m_crescentHold = 0.05f;
        [SerializeField] private float m_crescentFade = 0.18f;
        [SerializeField] private float m_echoDelay = 0.04f;
        [SerializeField] private float m_glow = 3f;

        private readonly System.Random m_random = new System.Random();
        private EffectQuadPool m_flarePool;
        private EffectQuadPool m_linePool;
        private EffectQuadPool m_haloPool;
        private EffectQuadPool m_bladePool;
        private EffectParticles m_sparks;

        private void Awake()
        {
            m_flarePool = new EffectQuadPool(transform, "EffectSprite", "Swordsman Glint", 26, EffectTextures.Flare, true);
            m_linePool = new EffectQuadPool(transform, "EffectSprite", "Swordsman Streak", 24, EffectTextures.SlashLine, true);
            m_haloPool = new EffectQuadPool(transform, "EffectSprite", "Swordsman Crescent Halo", 24, EffectTextures.SlashLine, true, ArcMesh.GetCrescent(165f, 0.5f));
            m_bladePool = new EffectQuadPool(transform, "EffectSprite", "Swordsman Crescent", 25, EffectTextures.SlashLine, true, ArcMesh.GetCrescent(155f, 0.32f));
            m_sparks = new EffectParticles(transform, "Swordsman Sparks", EffectTextures.Spark, true, 3f, 27, true, 1f, false);
        }

        private void OnDestroy()
        {
            m_flarePool?.Dispose(); m_linePool?.Dispose(); m_haloPool?.Dispose(); m_bladePool?.Dispose();
        }

        public void FireShot(Vector3 from, Vector3 to, float speed, float hold, float sizeWorld, Action onArrived)
        {
            if (!m_flarePool.IsReady || !m_linePool.IsReady || !m_bladePool.IsReady)
            {
                onArrived?.Invoke();
                return;
            }

            float cell = Mathf.Max(0.5f, sizeWorld);
            Vector3 toTarget = to - from;
            float attackAngle = Mathf.Atan2(toTarget.y, toTarget.x) * Mathf.Rad2Deg;
            float drawAt = Mathf.Max(hold, 0.1f);
            float cutAt = drawAt + m_streakDuration;

            TexturedFx.Pop(m_flarePool, from, 45f, 0f, cell * m_glintSize, s_blade, m_glow, 0f, Mathf.Min(0.12f, drawAt), drawAt * 0.6f);
            TexturedFx.Swipe(m_linePool, (from + to) * 0.5f, attackAngle, new Vector3(toTarget.magnitude, cell * 0.18f, 1f), s_blade, m_glow,
                drawAt, m_streakDuration, 0f, 0.1f);
            DOVirtual.DelayedCall(cutAt, () => onArrived?.Invoke()).SetLink(gameObject, LinkBehaviour.KillOnDisable);

            float angle = attackAngle + Range(-35f, 35f);
            bool mirror = Range(0f, 1f) < 0.5f;
            float radius = cell * m_crescentRadius;
            PlayCrescent(m_haloPool, to, angle, radius * 1.06f, mirror, s_halo, m_glow * 0.6f, cutAt);
            PlayCrescent(m_bladePool, to, angle, radius, mirror, s_blade, m_glow, cutAt);
            PlayCrescent(m_bladePool, to, angle - (mirror ? -12f : 12f), radius * 0.82f, mirror, s_halo, m_glow * 0.5f, cutAt + m_echoDelay);

            float hitAt = cutAt + m_crescentDraw * 0.5f;
            DOVirtual.DelayedCall(hitAt, () =>
            {
                TexturedFx.Pop(m_flarePool, to, Range(0f, 45f), cell * 0.25f, cell * 0.9f, Color.white, 3.5f, 0f, 0.05f, 0.16f);
                float tangent = (angle + (mirror ? -90f : 90f)) * Mathf.Deg2Rad;
                m_sparks.Burst(to, 10, s_blade, cell * 2.5f, cell * 6f, cell * 0.08f, cell * 0.15f, 0.18f, 0.32f,
                    new Vector2(Mathf.Cos(tangent), Mathf.Sin(tangent)), 80f);
                ScreenShake.Play(0.03f, 0.07f);
            }).SetLink(gameObject, LinkBehaviour.KillOnDisable);
        }

        private void PlayCrescent(EffectQuadPool pool, Vector3 center, float angle, float radius, bool mirror, Color color, float intensity, float delay)
        {
            float radians = angle * Mathf.Deg2Rad;
            Vector3 pivot = center - new Vector3(Mathf.Cos(radians), Mathf.Sin(radians), 0f) * (radius * 0.8f);
            TexturedFx.Swipe(pool, pivot, angle, new Vector3(radius, mirror ? -radius : radius, 1f), color, intensity,
                delay, m_crescentDraw, m_crescentHold, m_crescentFade);
        }

        private float Range(float min, float max) => Mathf.Lerp(min, max, (float)m_random.NextDouble());
    }
}
