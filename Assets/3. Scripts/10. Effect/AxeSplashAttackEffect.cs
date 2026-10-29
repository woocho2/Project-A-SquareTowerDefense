using System;
using DG.Tweening;
using UnityEngine;

namespace SquareTD
{
    // 바이킹(광역 도끼) 기본 공격 연출
    [DisallowMultipleComponent]
    public sealed class AxeSplashAttackEffect : MonoBehaviour, IShotEffect
    {
        private static readonly Color s_axeColor = new Color(0.95f, 0.62f, 0.38f);

        [SerializeField] private float m_axeSize = 0.85f;
        [SerializeField] private float m_appearDuration = 0.16f;
        [SerializeField] private float m_windUpDuration = 0.22f;
        [SerializeField] private float m_windUpAngle = 40f;
        [SerializeField] private float m_minFlight = 0.32f;
        [SerializeField] private float m_ghostInterval = 0.035f;
        [SerializeField] private float m_embedDuration = 0.25f;
        [SerializeField] private int m_cracks = 6;
        [SerializeField, Range(0f, 1f)] private float m_weight = 0.45f;

        private readonly System.Random m_random = new System.Random();
        private EffectQuadPool m_shapePool;
        private EffectQuadPool m_slashPool;

        private void Awake()
        {
            m_shapePool = new EffectQuadPool(transform, "SwordQi", "Viking Axe", 24);
            m_slashPool = new EffectQuadPool(transform, "SlashMark", "Viking Crack", 23);
        }

        private void OnDestroy()
        {
            m_shapePool?.Dispose();
            m_slashPool?.Dispose();
        }

        public void FireShot(Vector3 from, Vector3 to, float speed, float hold, float sizeWorld, Action onArrived)
        {
            if (!m_shapePool.IsReady || !m_slashPool.IsReady)
            {
                onArrived?.Invoke();
                return;
            }

            float side = to.x >= from.x ? 1f : -1f;
            Vector3 fullScale = new Vector3(side * m_axeSize, m_axeSize, 1f);
            Vector3 start = from + Vector3.up * 0.15f;

            EffectQuad axe = m_shapePool.Get();
            axe.Transform.position = start;
            axe.Transform.rotation = Quaternion.identity;
            axe.Transform.localScale = Vector3.zero;
            axe.SetColor(s_axeColor);
            axe.SetFloat(EffectQuad.ShapeId, 5f);
            axe.SetFloat(EffectQuad.AlphaId, 1f);

            Vector3 toTarget = to - start;
            float flight = Mathf.Max(m_minFlight, toTarget.magnitude / Mathf.Max(0.01f, speed));
            float jumpPower = Mathf.Max(1f, toTarget.magnitude * 0.35f);
            int turns = Mathf.Max(1, Mathf.RoundToInt(flight * 2f));
            float spin = -side * (360f * turns + 150f);
            float launchAt = Mathf.Max(hold, m_appearDuration + m_windUpDuration);
            float bobFor = launchAt - m_appearDuration - m_windUpDuration;

            Sequence sequence = DOTween.Sequence().SetLink(axe.GameObject, LinkBehaviour.KillOnDisable);
            sequence.Append(axe.Transform.DOScale(fullScale, m_appearDuration).SetEase(Ease.OutBack));
            if (bobFor > 0f)
                sequence.Append(axe.Transform.DOMoveY(start.y + 0.08f, bobFor).SetEase(Ease.InOutSine));
            sequence.Append(axe.Transform.DORotate(new Vector3(0f, 0f, side * m_windUpAngle), m_windUpDuration).SetEase(Ease.OutCubic));
            sequence.Join(axe.Transform.DOMove(start - new Vector3(side * 0.12f, -0.05f, 0f), m_windUpDuration).SetEase(Ease.OutCubic));
            Vector3 embed = to + Vector3.up * (m_axeSize * 0.3f);
            float alpha = 1f;
            sequence.Append(axe.Transform.DOJump(embed, jumpPower, 1, flight));
            sequence.Join(axe.Transform.DORotate(new Vector3(0f, 0f, spin), flight, RotateMode.LocalAxisAdd).SetEase(Ease.InSine));
            for (float t = m_ghostInterval; t < flight; t += m_ghostInterval)
                sequence.InsertCallback(launchAt + t, () => AxeImpact.PlayGhost(m_shapePool, axe.Transform, s_axeColor, 0.3f, 0.14f));
            sequence.AppendCallback(() =>
            {
                axe.SetFloat(EffectQuad.ClipBelowId, to.y);
                AxeImpact.PlayGround(m_shapePool, m_slashPool, m_random, to, sizeWorld, m_cracks, 0f, m_weight);
                onArrived?.Invoke();
            });
            sequence.Append(axe.Transform.DOMoveY(embed.y - m_axeSize * 0.12f, 0.08f).SetEase(Ease.OutQuad));
            sequence.Join(axe.Transform.DOPunchRotation(new Vector3(0f, 0f, side * 6f), 0.18f, 8, 0.5f));
            sequence.AppendInterval(m_embedDuration);
            sequence.Append(DOTween.To(() => alpha, value => { alpha = value; axe.SetFloat(EffectQuad.AlphaId, value); },
                0f, 0.2f).SetEase(Ease.InQuad));
            sequence.OnKill(() => m_shapePool.Release(axe));
        }
    }
}
