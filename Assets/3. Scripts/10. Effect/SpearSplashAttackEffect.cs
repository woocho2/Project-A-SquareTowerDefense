using System;
using DG.Tweening;
using UnityEngine;

namespace SquareTD
{
    // 워로드(광역 창) 기본 공격 연출 — 깃발 창으로 내리꽂아 찌르기 (2026-10-27 사용자: 던지기 → 깃발 창으로 찌르기)
    //   ① 타워 주위에 깃발 창이 곧게 서서 나타남 (깃발 펄럭임)
    //   ② 착탄점 위로 뛰어올라 창끝을 비스듬히 아래로 겨눔
    //   ③ 쾅 내리꽂음: 창끝이 땅에 박혀 지워지고, 찌른 방향으로 흙먼지 충격이 밀려 나감 + 약한 흔들림
    //   ④ 꽂힌 깃발이 잠깐 펄럭이다 흐려짐
    //   찌르는 순간 onArrived. 타워·규칙은 모름. 위치와 착탄 범위 크기만 받아서 그림
    [DisallowMultipleComponent]
    public sealed class SpearSplashAttackEffect : MonoBehaviour, IShotEffect
    {
        private static readonly Color s_shock = new Color(0.72f, 0.62f, 0.5f);

        [SerializeField] private float m_spearLength = 1.2f;       // 칸에 대한 비율
        [SerializeField] private float m_leapDuration = 0.18f;
        [SerializeField] private float m_aimAngle = 55f;           // 아래로 겨누는 각 (수평에서)
        [SerializeField] private float m_thrustDuration = 0.07f;
        [SerializeField] private float m_plantedDuration = 0.45f;

        private EffectQuadPool m_pool;
        private EffectQuadPool m_shockPool;

        private void Awake()
        {
            m_pool = new EffectQuadPool(transform, "SwordQi", "Warlord Banner", 24);
            m_shockPool = new EffectQuadPool(transform, "SlashMark", "Warlord Shock", 22);
        }

        private void OnDestroy()
        {
            m_pool?.Dispose();
            m_shockPool?.Dispose();
        }

        // 게임의 기본 공격 한 발 (SplashAction → ProjectileManager): hold만큼 세운 채 기다렸다 뛰어올라 내리꽂음, 찌르는 순간 onArrived
        //   sizeWorld = 착탄 범위 한 변의 월드 길이
        public void FireShot(Vector3 from, Vector3 to, float speed, float hold, float sizeWorld, Action onArrived)
        {
            if (!m_pool.IsReady || !m_shockPool.IsReady)
            {
                onArrived?.Invoke();
                return;
            }

            float cell = EffectSpace.CellWorldSize();
            float length = cell * m_spearLength;
            float area = Mathf.Max(cell, sizeWorld);
            float side = to.x >= from.x ? 1f : -1f;
            float strikeAngle = side > 0f ? -m_aimAngle : -180f + m_aimAngle;      // 앞쪽 아래로 겨눔
            float radians = strikeAngle * Mathf.Deg2Rad;
            Vector3 strikeForward = new Vector3(Mathf.Cos(radians), Mathf.Sin(radians), 0f);
            Vector3 aimed = SpearFx.CenterForTip(to, strikeForward, length, -0.25f);   // 창끝이 땅 바로 위
            Vector3 planted = SpearFx.CenterForTip(to, strikeForward, length, 0.18f);  // 창끝이 땅에 박힌 자리
            float leapAt = Mathf.Max(hold, 0.12f);

            EffectQuad banner = SpearFx.MakeSpear(m_pool, from, 90f, SpearFx.Steel, SpearFx.BannerShape);
            banner.SetColorById(EffectQuad.Color2Id, SpearFx.Banner);
            float alpha = 1f;

            Sequence sequence = DOTween.Sequence().SetLink(banner.GameObject, LinkBehaviour.KillOnDisable);
            sequence.Append(banner.Transform.DOScale(SpearFx.BannerScale(length * 0.8f, 90f), 0.12f).SetEase(Ease.OutBack));
            sequence.AppendInterval(leapAt - 0.12f);
            // 뛰어올라 겨눔: 높이 올랐다가 착탄점 위에서 창끝을 아래로
            sequence.Append(banner.Transform.DOJump(aimed, cell * 1.2f, 1, m_leapDuration).SetEase(Ease.OutQuad));
            sequence.Join(banner.Transform.DORotate(new Vector3(0f, 0f, strikeAngle), m_leapDuration).SetEase(Ease.InOutQuad));
            sequence.Join(banner.Transform.DOScale(SpearFx.BannerScale(length, strikeAngle), m_leapDuration));
            sequence.Append(banner.Transform.DOMove(planted, m_thrustDuration).SetEase(Ease.InQuad));          // 쾅
            sequence.InsertCallback(leapAt + m_leapDuration + m_thrustDuration * 0.5f,
                () => AxeImpact.PlayGhostShape(m_pool, banner.Transform, SpearFx.Steel, SpearFx.BannerShape, 0.3f, 0.1f));
            sequence.AppendCallback(() =>
            {
                banner.SetFloat(EffectQuad.ClipBelowId, to.y);                   // 땅 아래는 안 보임
                PlayShock(to, side > 0f ? 0f : 180f, area);
                onArrived?.Invoke();
            });
            sequence.Append(banner.Transform.DOPunchRotation(new Vector3(0f, 0f, side * 6f), 0.18f, 8, 0.5f));
            sequence.AppendInterval(m_plantedDuration);
            sequence.Append(DOTween.To(() => alpha, value => { alpha = value; banner.SetFloat(EffectQuad.AlphaId, value); },
                0f, 0.25f).SetEase(Ease.InQuad));
            sequence.OnKill(() => m_pool.Release(banner));
        }

        // 찌른 방향으로 흙먼지 충격이 곧게 밀려 나감 (빛 없이) + 양옆으로 짧게 + 약한 흔들림
        private void PlayShock(Vector3 point, float aim, float area)
        {
            float radians = aim * Mathf.Deg2Rad;
            Vector3 forward = new Vector3(Mathf.Cos(radians), Mathf.Sin(radians), 0f);
            float length = area * 0.9f;
            SlashStroke.Play(m_shockPool, point + forward * (length * 0.5f), aim, length, area * 0.35f, 0.3f, s_shock,
                0f, 0.1f, 0.12f, 0.3f, 0f);
            SlashStroke.Play(m_shockPool, point, aim + 90f, area * 0.6f, area * 0.22f, 0.25f, s_shock,
                0.03f, 0.08f, 0.1f, 0.25f, 0f);
            ArrowFx.PlayDust(m_pool, point, area * 0.7f);
            ScreenShake.Play(0.04f, 0.1f);
        }
    }
}
