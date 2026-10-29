using System;
using DG.Tweening;
using UnityEngine;

namespace SquareTD
{
    // 워로드(광역 창) 기본 공격 연출
    [DisallowMultipleComponent]
    public sealed class SpearSplashAttackEffect : MonoBehaviour, IShotEffect
    {
        private static readonly Color s_shock = new Color(0.72f, 0.62f, 0.5f);

        [SerializeField] private float m_spearLength = 1.2f;
        [SerializeField] private float m_leapDuration = 0.18f;
        [SerializeField] private float m_aimAngle = 55f;
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
            float strikeAngle = side > 0f ? -m_aimAngle : -180f + m_aimAngle;
            float radians = strikeAngle * Mathf.Deg2Rad;
            Vector3 strikeForward = new Vector3(Mathf.Cos(radians), Mathf.Sin(radians), 0f);
            Vector3 aimed = SpearFx.CenterForTip(to, strikeForward, length, -0.25f);
            Vector3 planted = SpearFx.CenterForTip(to, strikeForward, length, 0.18f);
            float leapAt = Mathf.Max(hold, 0.12f);

            EffectQuad banner = SpearFx.MakeSpear(m_pool, from, 90f, SpearFx.Steel, SpearFx.BannerShape);
            banner.SetColorById(EffectQuad.Color2Id, SpearFx.Banner);
            float alpha = 1f;

            Sequence sequence = DOTween.Sequence().SetLink(banner.GameObject, LinkBehaviour.KillOnDisable);
            sequence.Append(banner.Transform.DOScale(SpearFx.BannerScale(length * 0.8f, 90f), 0.12f).SetEase(Ease.OutBack));
            sequence.AppendInterval(leapAt - 0.12f);
            sequence.Append(banner.Transform.DOJump(aimed, cell * 1.2f, 1, m_leapDuration).SetEase(Ease.OutQuad));
            sequence.Join(banner.Transform.DORotate(new Vector3(0f, 0f, strikeAngle), m_leapDuration).SetEase(Ease.InOutQuad));
            sequence.Join(banner.Transform.DOScale(SpearFx.BannerScale(length, strikeAngle), m_leapDuration));
            sequence.Append(banner.Transform.DOMove(planted, m_thrustDuration).SetEase(Ease.InQuad));
            sequence.InsertCallback(leapAt + m_leapDuration + m_thrustDuration * 0.5f,
                () => AxeImpact.PlayGhostShape(m_pool, banner.Transform, SpearFx.Steel, SpearFx.BannerShape, 0.3f, 0.1f));
            sequence.AppendCallback(() =>
            {
                banner.SetFloat(EffectQuad.ClipBelowId, to.y);
                PlayShock(to, side > 0f ? 0f : 180f, area);
                onArrived?.Invoke();
            });
            sequence.Append(banner.Transform.DOPunchRotation(new Vector3(0f, 0f, side * 6f), 0.18f, 8, 0.5f));
            sequence.AppendInterval(m_plantedDuration);
            sequence.Append(DOTween.To(() => alpha, value => { alpha = value; banner.SetFloat(EffectQuad.AlphaId, value); },
                0f, 0.25f).SetEase(Ease.InQuad));
            sequence.OnKill(() => m_pool.Release(banner));
        }

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
