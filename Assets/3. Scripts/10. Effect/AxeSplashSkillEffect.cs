using DG.Tweening;
using UnityEngine;

namespace SquareTD
{
    // 바이킹(광역 도끼) 스킬 2 강타 연출 — 빛 없이 땅이 크게 터짐 (바이킹 기본 공격과 같은 말투)
    //   ① 타워 위에 거대한 도끼가 나타나 크게 뒤로 젖힘 (힘 모으기)
    //   ② 잔상을 남기며 높이 던져져 천천히 돌며 날아감
    //   ③ 착탄점에 박힘 (땅 아래는 지워짐) → 큰 그을린 자국 · 지그재그 금 10줄 · 파편 · 흙먼지 고리 · 강한 흔들림
    //   ④ 한 박자 뒤 여진: 더 넓게 금이 한 번 더 퍼지고 약하게 한 번 더 흔들림
    //   ⑤ 박힌 도끼가 남았다가 흐려지며 사라짐
    //   타워·규칙은 모름. 타워 위치·착탄점·착탄 범위 크기만 받아서 그림
    [DisallowMultipleComponent]
    public sealed class AxeSplashSkillEffect : MonoBehaviour
    {
        private static readonly Color s_axeColor = new Color(0.95f, 0.6f, 0.36f);

        [SerializeField] private float m_axeScale = 1.6f;          // 착탄 범위 한 변에 대한 배율 (최소 1.6칸)
        [SerializeField] private float m_appearDuration = 0.2f;
        [SerializeField] private float m_windUpDuration = 0.3f;
        [SerializeField] private float m_flight = 0.45f;
        [SerializeField] private float m_embedDuration = 0.5f;
        [SerializeField] private float m_aftershockDelay = 0.14f;

        private readonly System.Random m_random = new System.Random();     // 게임 쪽 Random을 건드리지 않게
        private EffectQuadPool m_shapePool;
        private EffectQuadPool m_slashPool;

        private void Awake()
        {
            m_shapePool = new EffectQuadPool(transform, "SwordQi", "Smash Shape", 28);
            m_slashPool = new EffectQuadPool(transform, "SlashMark", "Smash Crack", 27);
        }

        private void OnDestroy()
        {
            m_shapePool?.Dispose();
            m_slashPool?.Dispose();
        }

        // owner: 바이킹 위치 / center: 착탄점 / areaWorldSize: 착탄 범위 한 변의 월드 길이 / cellWorldSize: 칸 한 변
        public void Play(Vector3 owner, Vector3 center, float areaWorldSize, float cellWorldSize)
        {
            if (!m_shapePool.IsReady || !m_slashPool.IsReady) return;

            float cell = Mathf.Max(0.5f, cellWorldSize);
            float area = Mathf.Max(cell, areaWorldSize);
            float scale = Mathf.Max(cell * 1.6f, area * m_axeScale * 0.6f);
            float side = center.x >= owner.x ? 1f : -1f;
            Vector3 start = owner + Vector3.up * (cell * 0.6f);
            Vector3 embed = center + Vector3.up * (scale * 0.3f);
            float jumpPower = Mathf.Max(cell * 2.5f, (center - start).magnitude * 0.5f);
            float launchAt = m_appearDuration + m_windUpDuration;
            float alpha = 1f;

            EffectQuad axe = m_shapePool.Get();
            axe.Transform.position = start;
            axe.Transform.rotation = Quaternion.identity;
            axe.Transform.localScale = Vector3.zero;
            axe.SetColor(s_axeColor);
            axe.SetFloat(EffectQuad.ShapeId, 5f);
            axe.SetFloat(EffectQuad.AlphaId, 1f);

            Sequence sequence = DOTween.Sequence().SetLink(axe.GameObject, LinkBehaviour.KillOnDisable);
            sequence.Append(axe.Transform.DOScale(new Vector3(side * scale, scale, 1f), m_appearDuration).SetEase(Ease.OutBack));
            sequence.Append(axe.Transform.DORotate(new Vector3(0f, 0f, side * 60f), m_windUpDuration).SetEase(Ease.OutCubic));   // 크게 젖힘
            sequence.Join(axe.Transform.DOMove(start - new Vector3(side * cell * 0.3f, -cell * 0.15f, 0f), m_windUpDuration).SetEase(Ease.OutCubic));
            sequence.Join(axe.Transform.DOPunchScale(new Vector3(side * 0.06f, 0.06f, 0f) * scale, m_windUpDuration, 14, 0.5f));   // 부들부들
            sequence.Append(axe.Transform.DOJump(embed, jumpPower, 1, m_flight));
            sequence.Join(axe.Transform.DORotate(new Vector3(0f, 0f, -side * (360f + 170f)), m_flight, RotateMode.LocalAxisAdd)
                .SetEase(Ease.InQuad));                                       // 날이 땅을 향한 채 끝남
            for (float t = 0.04f; t < m_flight; t += 0.04f)
                sequence.InsertCallback(launchAt + t, () => AxeImpact.PlayGhost(m_shapePool, axe.Transform, s_axeColor, 0.3f, 0.18f));
            sequence.AppendCallback(() =>
            {
                axe.SetFloat(EffectQuad.ClipBelowId, center.y);               // 땅 아래는 안 보임
                AxeImpact.PlayGround(m_shapePool, m_slashPool, m_random, center, area * 1.1f, 10, 0f, 1f);
                AxeImpact.PlayDustRing(m_shapePool, center, area, 0f);
                // 여진: 더 넓게 한 번 더 갈라지고 약하게 흔들림
                AxeImpact.PlayGround(m_shapePool, m_slashPool, m_random, center, area * 1.7f, 7, m_aftershockDelay, 0.25f);
            });
            sequence.Append(axe.Transform.DOMoveY(embed.y - scale * 0.15f, 0.1f).SetEase(Ease.OutQuad));   // 깊이 파고듦
            sequence.Join(axe.Transform.DOPunchRotation(new Vector3(0f, 0f, side * 7f), 0.25f, 10, 0.5f));
            sequence.AppendInterval(m_embedDuration);
            sequence.Append(DOTween.To(() => alpha, value => { alpha = value; axe.SetFloat(EffectQuad.AlphaId, value); },
                0f, 0.3f).SetEase(Ease.InQuad));
            sequence.OnKill(() => m_shapePool.Release(axe));
        }
    }
}
