using System;
using DG.Tweening;
using UnityEngine;

namespace SquareTD
{
    // 검객(단일 검) 기본 공격 연출 — 발도술 "슥삭" (그림 + 셰이더 + 파티클 버전)
    //   ① 타워 주위에 칼날이 번쩍 (별빛 FX_Flare)
    //   ② 순식간에 적까지 빛줄기(FX_SlashLine)가 그어짐 (발도)
    //   ③ 적 위에 반달 궤적(FX_SlashTrail)이 가로로 "슥" → 잠깐 뒤 엇갈려 "삭" + 불티 + 작은 별빛
    //   빛줄기가 닿는 순간 onArrived. 타워·규칙은 모름
    [DisallowMultipleComponent]
    public sealed class SwordTargetAttackEffect : MonoBehaviour, IShotEffect
    {
        private static readonly Color s_blade = new Color(0.72f, 0.88f, 1f);

        [SerializeField] private float m_glintSize = 0.6f;         // 칸에 대한 비율
        [SerializeField] private float m_streakDuration = 0.05f;
        [SerializeField] private float m_secondCutDelay = 0.08f;
        [SerializeField] private float m_cutDraw = 0.05f;
        [SerializeField] private float m_glow = 2.5f;

        private readonly System.Random m_random = new System.Random();     // 게임 쪽 Random을 건드리지 않게
        private EffectQuadPool m_flarePool;
        private EffectQuadPool m_linePool;
        private EffectQuadPool m_arcPool;
        private EffectParticles m_sparks;

        private void Awake()
        {
            m_flarePool = new EffectQuadPool(transform, "EffectSprite", "Swordsman Glint", 26, EffectTextures.Flare, true);
            m_linePool = new EffectQuadPool(transform, "EffectSprite", "Swordsman Streak", 24, EffectTextures.SlashLine, true);
            m_arcPool = new EffectQuadPool(transform, "EffectSprite", "Swordsman Cut", 25, EffectTextures.SlashTrail, true, ArcMesh.Get(100f, 0.25f));
            m_sparks = new EffectParticles(transform, "Swordsman Sparks", EffectTextures.Spark, true, 3f, 27, true, 1f, false);
        }

        private void OnDestroy()
        {
            m_flarePool?.Dispose(); m_linePool?.Dispose(); m_arcPool?.Dispose();
        }

        // 게임의 기본 공격 한 발 (TargetAction → ProjectileManager): hold만큼 번쩍인 뒤 발도, 빛줄기가 닿으면 onArrived
        public void FireShot(Vector3 from, Vector3 to, float speed, float hold, float sizeWorld, Action onArrived)
        {
            if (!m_flarePool.IsReady || !m_linePool.IsReady || !m_arcPool.IsReady)
            {
                onArrived?.Invoke();
                return;
            }

            float cell = Mathf.Max(0.5f, sizeWorld);
            Vector3 toTarget = to - from;
            float angle = Mathf.Atan2(toTarget.y, toTarget.x) * Mathf.Rad2Deg;
            float drawAt = Mathf.Max(hold, 0.1f);
            float cutAt = drawAt + m_streakDuration;

            // ① 칼날 번쩍 (발도 직전까지 남음)
            TexturedFx.Pop(m_flarePool, from, 45f, 0f, cell * m_glintSize, s_blade, m_glow, 0f, Mathf.Min(0.12f, drawAt), drawAt * 0.6f);
            // ② 발도 빛줄기
            TexturedFx.Swipe(m_linePool, (from + to) * 0.5f, angle, new Vector3(toTarget.magnitude, cell * 0.22f, 1f), s_blade, m_glow,
                drawAt, m_streakDuration, 0f, 0.12f);
            DOVirtual.DelayedCall(cutAt, () => onArrived?.Invoke()).SetLink(gameObject, LinkBehaviour.KillOnDisable);   // 닿는 순간 피해

            // ③ 슥 → 삭
            float radius = cell * 0.55f;
            float first = 90f + Range(-20f, 20f);                                  // 궤적이 거의 가로로 지나감
            PlayCut(to, first, radius, false, cutAt, cell);
            PlayCut(to, first + Range(100f, 125f), radius * 0.9f, true, cutAt + m_secondCutDelay, cell);
            DOVirtual.DelayedCall(cutAt + m_secondCutDelay + m_cutDraw, () =>
            {
                TexturedFx.Pop(m_flarePool, to, Range(0f, 45f), cell * 0.2f, cell * 0.6f, Color.white, 3f, 0f, 0.05f, 0.14f);
                ScreenShake.Play(0.02f, 0.06f);
            }).SetLink(gameObject, LinkBehaviour.KillOnDisable);
        }

        private void PlayCut(Vector3 center, float angle, float radius, bool mirror, float delay, float cell)
        {
            float radians = angle * Mathf.Deg2Rad;
            Vector3 pivot = center - new Vector3(Mathf.Cos(radians), Mathf.Sin(radians), 0f) * radius;
            TexturedFx.Swipe(m_arcPool, pivot, angle, new Vector3(radius, mirror ? -radius : radius, 1f), s_blade, m_glow,
                delay, m_cutDraw, 0.02f, 0.12f);
            DOVirtual.DelayedCall(delay + m_cutDraw * 0.6f, () =>
                    m_sparks.Burst(center, 6, s_blade, cell * 2f, cell * 5f, cell * 0.08f, cell * 0.14f, 0.18f, 0.3f,
                        new Vector2(Mathf.Cos((angle + (mirror ? -90f : 90f)) * Mathf.Deg2Rad), Mathf.Sin((angle + (mirror ? -90f : 90f)) * Mathf.Deg2Rad)), 70f))
                .SetLink(gameObject, LinkBehaviour.KillOnDisable);
        }

        private float Range(float min, float max) => Mathf.Lerp(min, max, (float)m_random.NextDouble());
    }
}
