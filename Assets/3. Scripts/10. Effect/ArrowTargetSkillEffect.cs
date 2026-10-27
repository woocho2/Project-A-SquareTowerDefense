using DG.Tweening;
using UnityEngine;

namespace SquareTD
{
    // 헌터(단일 활) 스킬 2 은화살 연출 — 최대 체력을 꿰뚫는 한 발
    //   ① 타워에 은빛이 모이며 은화살이 적을 겨눔 (뒤로 크게 당김)
    //   ② 순식간에 쏘아져 적을 꿰뚫고 지나감: 굵은 은빛 줄기가 적 뒤까지 길게 남음
    //   ③ 맞은 자리에 은빛 십자 번쩍 + 퍼지는 고리 + 약한 흔들림
    //   타워·규칙은 모름. 타워 위치·맞을 위치·칸 크기만 받아서 그림
    [DisallowMultipleComponent]
    public sealed class ArrowTargetSkillEffect : MonoBehaviour
    {
        private static readonly Color s_silver = new Color(0.8f, 0.9f, 1f);

        [SerializeField] private float m_chargeDuration = 0.3f;
        [SerializeField] private float m_arrowLength = 0.9f;       // 칸에 대한 비율
        [SerializeField] private float m_flight = 0.07f;
        [SerializeField] private float m_pierceLength = 1.6f;      // 적 뒤로 꿰뚫고 나가는 길이 (칸)

        private EffectQuadPool m_pool;
        private EffectQuadPool m_streakPool;

        private void Awake()
        {
            m_pool = new EffectQuadPool(transform, "SwordQi", "Silver Arrow", 26);
            m_streakPool = new EffectQuadPool(transform, "SlashMark", "Silver Streak", 25);
        }

        private void OnDestroy()
        {
            m_pool?.Dispose();
            m_streakPool?.Dispose();
        }

        // owner: 헌터 위치 / target: 맞을 적의 위치 / cellWorldSize: 칸 한 변
        public void Play(Vector3 owner, Vector3 target, float cellWorldSize)
        {
            if (!m_pool.IsReady || !m_streakPool.IsReady) return;

            float cell = Mathf.Max(0.5f, cellWorldSize);
            float length = cell * m_arrowLength;
            Vector3 toTarget = target - owner;
            Vector3 forward = toTarget.sqrMagnitude > 0.0001f ? toTarget.normalized : Vector3.right;
            float angle = Mathf.Atan2(forward.y, forward.x) * Mathf.Rad2Deg;
            Vector3 drawn = owner - forward * (length * 0.3f);
            Vector3 beyond = target + forward * (cell * m_pierceLength);
            float impactAt = m_chargeDuration + m_flight * 0.6f;

            PlayChargeGlow(owner, cell);

            EffectQuad arrow = m_pool.Get();
            arrow.Transform.position = owner;
            arrow.Transform.rotation = Quaternion.Euler(0f, 0f, angle);
            arrow.Transform.localScale = Vector3.zero;
            arrow.SetColor(s_silver);
            arrow.SetFloat(EffectQuad.ShapeId, 8f);
            arrow.SetFloat(EffectQuad.AlphaId, 1f);

            float alpha = 1f;
            Sequence sequence = DOTween.Sequence().SetLink(arrow.GameObject, LinkBehaviour.KillOnDisable);
            sequence.Append(arrow.Transform.DOScale(new Vector3(length, length * 0.5f, 1f), 0.12f).SetEase(Ease.OutBack));
            sequence.Append(arrow.Transform.DOMove(drawn, m_chargeDuration - 0.12f).SetEase(Ease.OutCubic));     // 크게 당김
            sequence.AppendCallback(() =>
            {
                SlashStroke.Play(m_streakPool, (drawn + beyond) * 0.5f, angle, (beyond - drawn).magnitude, cell * 0.5f, 0.16f, s_silver,
                    0f, m_flight, 0.12f, 0.35f);                                    // 꿰뚫는 은빛 줄기
            });
            sequence.Append(arrow.Transform.DOMove(beyond, m_flight).SetEase(Ease.Linear));
            sequence.Join(DOTween.To(() => alpha, value => { alpha = value; arrow.SetFloat(EffectQuad.AlphaId, value); },
                0f, m_flight * 1.5f));
            sequence.InsertCallback(impactAt, () => PlayPierce(target, cell));
            sequence.OnKill(() => m_pool.Release(arrow));
        }

        private void PlayChargeGlow(Vector3 owner, float cell)
        {
            EffectQuad glow = m_pool.Get();
            glow.Transform.position = owner;
            glow.Transform.localScale = Vector3.zero;
            glow.SetColor(s_silver);
            glow.SetFloat(EffectQuad.ShapeId, 2f);
            glow.SetFloat(EffectQuad.AlphaId, 0.7f);
            glow.SetSortingOffset(-2);

            float alpha = 0.7f;
            Sequence sequence = DOTween.Sequence().SetLink(glow.GameObject, LinkBehaviour.KillOnDisable);
            sequence.Append(glow.Transform.DOScale(new Vector3(cell * 1.4f, cell * 1.4f, 1f), m_chargeDuration).SetEase(Ease.InQuad));
            sequence.Append(DOTween.To(() => alpha, value => { alpha = value; glow.SetFloat(EffectQuad.AlphaId, value); },
                0f, 0.15f));
            sequence.OnKill(() => m_pool.Release(glow));
        }

        // 꿰뚫린 자리: 큰 은빛 십자 번쩍 + 퍼지는 고리 + 약한 흔들림
        private void PlayPierce(Vector3 target, float cell)
        {
            ArrowFx.PlaySpark(m_pool, target, cell * 1.5f, s_silver);

            EffectQuad ring = m_pool.Get();
            ring.Transform.position = target;
            ring.Transform.localScale = Vector3.zero;
            ring.SetColor(s_silver);
            ring.SetFloat(EffectQuad.ShapeId, 6f);
            ring.SetFloat(EffectQuad.AlphaId, 1f);

            float alpha = 1f;
            Sequence sequence = DOTween.Sequence().SetLink(ring.GameObject, LinkBehaviour.KillOnDisable);
            sequence.Append(ring.Transform.DOScale(new Vector3(cell * 1.3f, cell * 1.3f, 1f), 0.25f).SetEase(Ease.OutCubic));
            sequence.Join(DOTween.To(() => alpha, value => { alpha = value; ring.SetFloat(EffectQuad.AlphaId, value); },
                0f, 0.25f).SetEase(Ease.InQuad));
            sequence.OnKill(() => m_pool.Release(ring));

            ScreenShake.Play(0.06f, 0.1f);
        }
    }
}
