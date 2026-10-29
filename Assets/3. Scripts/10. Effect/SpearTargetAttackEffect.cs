using System;
using DG.Tweening;
using UnityEngine;

namespace SquareTD
{
    // 창술사(단일 창) 기본 공격 연출
    [DisallowMultipleComponent]
    public sealed class SpearTargetAttackEffect : MonoBehaviour, IShotEffect
    {
        [SerializeField] private float m_spearLength = 0.9f;
        [SerializeField] private float m_dashDuration = 0.08f;
        [SerializeField] private float m_drawBack = 0.05f;
        [SerializeField] private float m_thrustDuration = 0.05f;
        [SerializeField] private float m_holdAfterThrust = 0.06f;

        private EffectQuadPool m_pool;
        private EffectQuadPool m_streakPool;

        private void Awake()
        {
            m_pool = new EffectQuadPool(transform, "SwordQi", "Lancer Spear", 24);
            m_streakPool = new EffectQuadPool(transform, "SlashMark", "Lancer Pierce", 23);
        }

        private void OnDestroy()
        {
            m_pool?.Dispose();
            m_streakPool?.Dispose();
        }

        public void FireShot(Vector3 from, Vector3 to, float speed, float hold, float sizeWorld, Action onArrived)
        {
            if (!m_pool.IsReady || !m_streakPool.IsReady)
            {
                onArrived?.Invoke();
                return;
            }

            float cell = Mathf.Max(0.5f, sizeWorld);
            float length = cell * m_spearLength;
            Vector3 toTarget = to - from;
            Vector3 forward = toTarget.sqrMagnitude > 0.0001f ? toTarget.normalized : Vector3.right;
            float angle = Mathf.Atan2(forward.y, forward.x) * Mathf.Rad2Deg;
            Vector3 ready = SpearFx.CenterForTip(to, forward, length, -0.35f);
            Vector3 pulled = ready - forward * (length * 0.15f);
            Vector3 thrust = SpearFx.CenterForTip(to, forward, length, 0.2f);
            float dashAt = Mathf.Max(hold, 0.1f);
            float hitAt = dashAt + m_dashDuration + m_drawBack + m_thrustDuration;

            EffectQuad spear = SpearFx.MakeSpear(m_pool, from, angle, SpearFx.Steel);
            float alpha = 1f;
            Sequence sequence = DOTween.Sequence().SetLink(spear.GameObject, LinkBehaviour.KillOnDisable);
            sequence.Append(spear.Transform.DOScale(SpearFx.Scale(length), 0.1f).SetEase(Ease.OutBack));
            sequence.AppendInterval(dashAt - 0.1f);
            sequence.Append(spear.Transform.DOMove(ready, m_dashDuration).SetEase(Ease.OutQuad));
            sequence.Append(spear.Transform.DOMove(pulled, m_drawBack).SetEase(Ease.OutQuad));
            sequence.Append(spear.Transform.DOMove(thrust, m_thrustDuration).SetEase(Ease.InQuad));
            sequence.InsertCallback(hitAt - m_thrustDuration * 0.5f,
                () => AxeImpact.PlayGhostShape(m_pool, spear.Transform, SpearFx.Steel, 9f, 0.35f, 0.1f));
            sequence.AppendCallback(() =>
            {
                SpearFx.PlaySpark(m_pool, to, cell * 0.5f, Color.white, angle);
                SlashStroke.Play(m_streakPool, to + forward * (cell * 0.3f), angle, cell * 0.9f, cell * 0.25f, 0.14f, SpearFx.Steel,
                    0f, 0.04f, 0.03f, 0.12f);
                onArrived?.Invoke();
            });
            sequence.AppendInterval(m_holdAfterThrust);
            sequence.Append(spear.Transform.DOMove(pulled - forward * (length * 0.3f), 0.1f).SetEase(Ease.InQuad));
            sequence.Join(DOTween.To(() => alpha, value => { alpha = value; spear.SetFloat(EffectQuad.AlphaId, value); },
                0f, 0.1f).SetEase(Ease.InQuad));
            sequence.OnKill(() => m_pool.Release(spear));
        }
    }
}
