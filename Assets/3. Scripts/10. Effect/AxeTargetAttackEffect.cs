using System;
using DG.Tweening;
using UnityEngine;

namespace SquareTD
{
    // 버서커(단일 도끼) 기본 공격 연출 — 작은 도끼 던지기 (2026-10-27 사용자: 작은 도끼를 던지듯, 타격감은 또렷하게)
    //   ① 타워 주위에 작은 도끼가 나타나 기다림
    //   ② 살짝 젖혔다가 빠르게 빙글빙글 돌며 낮게 날아감 (옅은 잔상)
    //   ③ 적에 박혀 잠깐 멈춤(히트스톱) + 하얀 번쩍 · 짧은 베기 자국 · 파편 · 아주 약한 화면 흔들림
    //   ④ 튕겨 나가 적 발밑 땅에 꽂힘 (땅 아래로 들어간 부분은 지워짐) → 흐려지며 사라짐
    //   타워·규칙은 모름. 위치와 칸 크기만 받아서 그림
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

        private readonly System.Random m_random = new System.Random();     // 게임 쪽 Random을 건드리지 않게
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

        // 게임의 기본 공격 한 발 (TargetAction → ProjectileManager): from에서 hold만큼 기다렸다 던짐, 맞는 순간 onArrived
        //   sizeWorld = 칸 한 변의 월드 길이
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
            float groundY = to.y - size * 0.18f;                                  // 적 발밑
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
            sequence.Append(axe.Transform.DORotate(new Vector3(0f, 0f, side * 35f), m_windUpDuration).SetEase(Ease.OutQuad));   // 살짝 젖힘
            sequence.Append(axe.Transform.DOJump(to, Mathf.Min(0.35f, toTarget.magnitude * 0.12f), 1, flight).SetEase(Ease.Linear));
            sequence.Join(axe.Transform.DORotate(new Vector3(0f, 0f, -side * m_spinPerSecond * flight), flight, RotateMode.LocalAxisAdd)
                .SetEase(Ease.Linear));
            for (float t = m_ghostInterval; t < flight; t += m_ghostInterval)
                sequence.InsertCallback(throwAt + t, () => AxeImpact.PlayGhost(m_shapePool, axe.Transform, s_axeColor, 0.22f, 0.1f));

            // 맞는 순간: 박혀서 멈칫 + 번쩍 + 베기 자국 + 파편 + 약한 흔들림
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
            // 튕겨 나가 발밑 땅에 꽂힘
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

        // 맞은 적이 하얗게 번쩍 (적 위에 겹치는 짧은 빛)
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
