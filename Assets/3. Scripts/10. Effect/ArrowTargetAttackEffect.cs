using System;
using DG.Tweening;
using UnityEngine;

namespace SquareTD
{
    // 헌터(단일 활) 기본 공격 연출 — 화살 한 대를 곧게 쏨
    //   ① 타워 주위에 화살이 적을 향해 메겨짐 → 쏘기 직전 뒤로 살짝 당겨짐
    //   ② 옅은 꼬리를 그으며 곧게 날아감
    //   ③ 적에 꽂혀 작은 반짝 → 잠깐 박혀 있다가 흐려짐
    //   맞는 순간 onArrived. 타워·규칙은 모름. 위치와 칸 크기만 받아서 그림
    [DisallowMultipleComponent]
    public sealed class ArrowTargetAttackEffect : MonoBehaviour, IShotEffect
    {
        private static readonly Color s_trail = new Color(1f, 0.95f, 0.85f);

        [SerializeField] private float m_arrowLength = 0.5f;       // 칸에 대한 비율
        [SerializeField] private float m_drawBack = 0.1f;          // 쏘기 직전 당기는 시간
        [SerializeField] private float m_arrowSpeed = 14f;
        [SerializeField] private float m_stuckDuration = 0.15f;

        private EffectQuadPool m_pool;
        private EffectQuadPool m_trailPool;

        private void Awake()
        {
            m_pool = new EffectQuadPool(transform, "SwordQi", "Hunter Arrow", 24);
            m_trailPool = new EffectQuadPool(transform, "SlashMark", "Hunter Trail", 23);
        }

        private void OnDestroy()
        {
            m_pool?.Dispose();
            m_trailPool?.Dispose();
        }

        // 게임의 기본 공격 한 발 (TargetAction → ProjectileManager): hold만큼 메긴 채 기다렸다 쏨, 맞는 순간 onArrived
        public void FireShot(Vector3 from, Vector3 to, float speed, float hold, float sizeWorld, Action onArrived)
        {
            if (!m_pool.IsReady || !m_trailPool.IsReady)
            {
                onArrived?.Invoke();
                return;
            }

            float cell = Mathf.Max(0.5f, sizeWorld);
            float length = cell * m_arrowLength;
            Vector3 toTarget = to - from;
            Vector3 forward = toTarget.sqrMagnitude > 0.0001f ? toTarget.normalized : Vector3.right;
            float angle = Mathf.Atan2(forward.y, forward.x) * Mathf.Rad2Deg;
            Vector3 stuck = to - forward * (length * 0.25f);                   // 촉이 적 몸에 박힌 자리
            float flight = Mathf.Clamp((stuck - from).magnitude / Mathf.Max(0.01f, m_arrowSpeed), 0.06f, 0.3f);
            float shootAt = Mathf.Max(hold, 0.1f + m_drawBack);

            EffectQuad arrow = m_pool.Get();
            arrow.Transform.position = from;
            arrow.Transform.rotation = Quaternion.Euler(0f, 0f, angle);
            arrow.Transform.localScale = Vector3.zero;
            arrow.SetColor(ArrowFx.Wood);
            arrow.SetFloat(EffectQuad.ShapeId, 8f);
            arrow.SetFloat(EffectQuad.AlphaId, 1f);

            float alpha = 1f;
            Sequence sequence = DOTween.Sequence().SetLink(arrow.GameObject, LinkBehaviour.KillOnDisable);
            sequence.Append(arrow.Transform.DOScale(new Vector3(length, length * 0.5f, 1f), 0.1f).SetEase(Ease.OutBack));
            sequence.AppendInterval(shootAt - 0.1f - m_drawBack);
            sequence.Append(arrow.Transform.DOMove(from - forward * (length * 0.2f), m_drawBack).SetEase(Ease.OutQuad));   // 당김
            sequence.AppendCallback(() =>
            {
                Vector3 start = arrow.Transform.position;
                SlashStroke.Play(m_trailPool, (start + stuck) * 0.5f, angle, (stuck - start).magnitude, length * 0.35f, 0.1f, s_trail,
                    0f, flight, 0f, 0.12f, 0.5f);                                  // 날아가는 동안 따라 그어지는 꼬리
            });
            sequence.Append(arrow.Transform.DOMove(stuck, flight).SetEase(Ease.Linear));
            sequence.AppendCallback(() =>
            {
                ArrowFx.PlaySpark(m_pool, to, cell * 0.4f, s_trail);
                onArrived?.Invoke();
            });
            sequence.Append(arrow.Transform.DOPunchRotation(new Vector3(0f, 0f, 8f), 0.1f, 8, 0.5f));
            sequence.AppendInterval(m_stuckDuration);
            sequence.Append(DOTween.To(() => alpha, value => { alpha = value; arrow.SetFloat(EffectQuad.AlphaId, value); },
                0f, 0.15f).SetEase(Ease.InQuad));
            sequence.OnKill(() => m_pool.Release(arrow));
        }
    }
}
