using System;
using DG.Tweening;
using UnityEngine;

namespace SquareTD
{
    // 블레이드 댄서(단일 검) 기본 공격 연출 — 발도 + 초승달 참격 (그림 + 셰이더 + 파티클 버전)
    //   ① 타워 주위에 칼날이 번쩍 (별빛 FX_Flare)
    //   ② 순식간에 적까지 빛줄기(FX_SlashLine)가 그어짐 (발도)
    //   ③ 적 위로 큰 초승달 참격이 휙 (2026-10-28 사용자: "애니메이션처럼 벨 때 초승달이 나가게")
    //      초승달 메쉬(ArcMesh.GetCrescent)에 FX_SlashLine을 입혀 바깥 날이 가장 밝고 안쪽으로 번짐
    //      푸른 빛무리 한 겹 + 하얀 날 한 겹 + 살짝 늦게 따라오는 잔상 한 겹. 볼록한 쪽이 공격 방향을 향함
    //   ④ 베는 순간 번쩍 + 불티가 날 방향으로 튀고 화면이 살짝 흔들림
    //   빛줄기가 닿는 순간 onArrived. 타워·규칙은 모름
    [DisallowMultipleComponent]
    public sealed class SwordTargetAttackEffect : MonoBehaviour, IShotEffect
    {
        private static readonly Color s_blade = new Color(0.72f, 0.88f, 1f);
        private static readonly Color s_halo = new Color(0.3f, 0.6f, 1f);

        [SerializeField] private float m_glintSize = 0.6f;         // 칸에 대한 비율
        [SerializeField] private float m_streakDuration = 0.05f;
        [Header("초승달 참격")]
        [SerializeField] private float m_crescentRadius = 0.85f;   // 칸에 대한 비율 (초승달 바깥 둘레의 반지름)
        [SerializeField] private float m_crescentDraw = 0.07f;      // 휙 그어지는 시간
        [SerializeField] private float m_crescentHold = 0.05f;
        [SerializeField] private float m_crescentFade = 0.18f;      // 꼬리부터 지워지는 시간
        [SerializeField] private float m_echoDelay = 0.04f;         // 잔상이 따라오는 간격
        [SerializeField] private float m_glow = 3f;

        private readonly System.Random m_random = new System.Random();     // 게임 쪽 Random을 건드리지 않게
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

        // 게임의 기본 공격 한 발 (TargetAction → ProjectileManager): hold만큼 번쩍인 뒤 발도, 빛줄기가 닿으면 onArrived
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

            // ① 칼날 번쩍 (발도 직전까지 남음)
            TexturedFx.Pop(m_flarePool, from, 45f, 0f, cell * m_glintSize, s_blade, m_glow, 0f, Mathf.Min(0.12f, drawAt), drawAt * 0.6f);
            // ② 발도 빛줄기
            TexturedFx.Swipe(m_linePool, (from + to) * 0.5f, attackAngle, new Vector3(toTarget.magnitude, cell * 0.18f, 1f), s_blade, m_glow,
                drawAt, m_streakDuration, 0f, 0.1f);
            DOVirtual.DelayedCall(cutAt, () => onArrived?.Invoke()).SetLink(gameObject, LinkBehaviour.KillOnDisable);   // 닿는 순간 피해

            // ③ 초승달 참격: 볼록한 쪽이 공격 방향(± 비스듬히), 위·아래로 휘는 방향은 무작위
            float angle = attackAngle + Range(-35f, 35f);
            bool mirror = Range(0f, 1f) < 0.5f;
            float radius = cell * m_crescentRadius;
            PlayCrescent(m_haloPool, to, angle, radius * 1.06f, mirror, s_halo, m_glow * 0.6f, cutAt);
            PlayCrescent(m_bladePool, to, angle, radius, mirror, s_blade, m_glow, cutAt);
            PlayCrescent(m_bladePool, to, angle - (mirror ? -12f : 12f), radius * 0.82f, mirror, s_halo, m_glow * 0.5f, cutAt + m_echoDelay);   // 잔상

            // ④ 베는 순간
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

        // 초승달 하나가 center를 지나가게: 메쉬의 원 중심을 볼록한 반대쪽으로 비켜 둠 (바깥 날이 center를 지남)
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
