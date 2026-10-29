using System;
using DG.Tweening;
using UnityEngine;

namespace SquareTD
{
    // 버서커(단일 도끼) 기본 공격 연출
    [DisallowMultipleComponent]
    public sealed class AxeTargetAttackEffect : MonoBehaviour, IShotEffect
    {
        private static readonly Color s_axeColor = new Color(1f, 0.5f, 0.32f);
        private static readonly Color s_hitColor = new Color(1f, 0.35f, 0.25f);

        [SerializeField] private float m_axeSize = 0.42f;
        [SerializeField] private float m_windUpDuration = 0.08f;
        [SerializeField] private float m_throwSpeed = 10f;
        [SerializeField] private float m_spinPerSecond = 1440f;
        [SerializeField] private float m_ghostInterval = 0.03f;
        [SerializeField] private float m_hitStop = 0.07f;
        [SerializeField] private float m_shakeStrength = 0.045f;
        [SerializeField] private float m_stuckDuration = 0.3f;

        private readonly System.Random m_random = new System.Random();
        private EffectQuadPool m_shapePool;
        private EffectQuadPool m_slashPool;

        private void Awake()
        {
            m_shapePool = new EffectQuadPool(transform, "SwordQi", "Berserker Axe", 24);
            m_slashPool = new EffectQuadPool(transform, "SlashMark", "Berserker Hit", 25);
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

            float size = Mathf.Max(0.5f, sizeWorld);
            float side = to.x >= from.x ? 1f : -1f;
            Vector3 toTarget = to - from;
            Vector3 forward = toTarget.sqrMagnitude > 0.0001f ? toTarget.normalized : Vector3.right;
            float throwAngle = Mathf.Atan2(forward.y, forward.x) * Mathf.Rad2Deg;
            float flight = Mathf.Clamp(toTarget.magnitude / Mathf.Max(0.01f, m_throwSpeed), 0.1f, 0.4f);
            float groundY = to.y - size * 0.18f;
            Vector3 landing = new Vector3(to.x + forward.x * size * 0.45f, groundY + m_axeSize * 0.3f, to.z);

            EffectQuad axe = m_shapePool.Get();
            axe.Transform.position = from;
            axe.Transform.rotation = Quaternion.identity;
            axe.Transform.localScale = Vector3.zero;
            axe.SetColor(s_axeColor);
            axe.SetFloat(EffectQuad.ShapeId, 5f);
            axe.SetFloat(EffectQuad.AlphaId, 1f);

            float throwAt = Mathf.Max(hold, 0.1f + m_windUpDuration);
            float alpha = 1f;
            Sequence sequence = DOTween.Sequence().SetLink(axe.GameObject, LinkBehaviour.KillOnDisable);
            sequence.Append(axe.Transform.DOScale(new Vector3(side * m_axeSize, m_axeSize, 1f), 0.1f).SetEase(Ease.OutBack));
            sequence.AppendInterval(throwAt - 0.1f - m_windUpDuration);
            sequence.Append(axe.Transform.DORotate(new Vector3(0f, 0f, side * 35f), m_windUpDuration).SetEase(Ease.OutQuad));
            sequence.Append(axe.Transform.DOJump(to, Mathf.Min(0.35f, toTarget.magnitude * 0.12f), 1, flight).SetEase(Ease.Linear));
            sequence.Join(axe.Transform.DORotate(new Vector3(0f, 0f, -side * m_spinPerSecond * flight), flight, RotateMode.LocalAxisAdd)
                .SetEase(Ease.Linear));
            for (float t = m_ghostInterval; t < flight; t += m_ghostInterval)
                sequence.InsertCallback(throwAt + t, () => AxeImpact.PlayGhost(m_shapePool, axe.Transform, s_axeColor, 0.22f, 0.1f));

            sequence.AppendCallback(() =>
            {
                PlayHitFlash(to, size);
                SlashStroke.Play(m_slashPool, to, throwAngle + 90f + Range(-25f, 25f), size * 0.75f, size * 0.32f, 0.2f, s_hitColor,
                    0f, 0.03f, 0.04f, 0.14f);
                AxeImpact.PlaySmall(m_shapePool, m_random, to, size * 0.5f, 0f);
                ScreenShake.Play(m_shakeStrength, 0.08f);
                onArrived?.Invoke();
            });
            sequence.Append(axe.Transform.DOPunchScale(new Vector3(side * 0.25f, 0.25f, 0f) * m_axeSize, m_hitStop, 6, 0.5f));
            sequence.Append(axe.Transform.DOJump(landing, size * 0.25f, 1, 0.16f));
            sequence.Join(axe.Transform.DORotate(new Vector3(0f, 0f, -side * 125f), 0.16f).SetEase(Ease.OutQuad));
            sequence.AppendCallback(() => axe.SetFloat(EffectQuad.ClipBelowId, groundY));
            sequence.Append(axe.Transform.DOMoveY(landing.y - m_axeSize * 0.1f, 0.05f).SetEase(Ease.OutQuad));
            sequence.Join(axe.Transform.DOPunchRotation(new Vector3(0f, 0f, side * 8f), 0.12f, 8, 0.5f));
            sequence.AppendInterval(m_stuckDuration);
            sequence.Append(DOTween.To(() => alpha, value => { alpha = value; axe.SetFloat(EffectQuad.AlphaId, value); },
                0f, 0.15f).SetEase(Ease.InQuad));
            sequence.OnKill(() => m_shapePool.Release(axe));
        }

        private void PlayHitFlash(Vector3 position, float size)
        {
            EffectQuad flash = m_shapePool.Get();
            flash.Transform.position = position;
            flash.Transform.localScale = new Vector3(size * 0.75f, size * 0.75f, 1f);
            flash.SetColor(Color.white);
            flash.SetFloat(EffectQuad.ShapeId, 2f);
            flash.SetFloat(EffectQuad.AlphaId, 0.9f);

            float alpha = 0.9f;
            DOTween.To(() => alpha, value => { alpha = value; flash.SetFloat(EffectQuad.AlphaId, value); }, 0f, 0.12f)
                .SetEase(Ease.InQuad)
                .SetLink(flash.GameObject, LinkBehaviour.KillOnDisable)
                .OnKill(() => m_shapePool.Release(flash));
        }

        private float Range(float min, float max) => Mathf.Lerp(min, max, (float)m_random.NextDouble());
    }
}
