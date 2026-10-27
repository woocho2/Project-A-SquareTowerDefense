using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace SquareTD
{
    // 워로드(광역 창) 스킬 2 선두 지휘 연출 — 말 탄 워로드가 그 줄을 돌격하며 다 꿰뚫음 (2026-10-27 사용자)
    //   공격 대상이 선 칸의 가로줄·세로줄 중 적이 더 많은 쪽 (같으면 가로줄). 줄은 화면 끝에서 끝까지
    //   ① 고른 줄에 붉은 표시선
    //   ② 화면 밖에서 말 탄 워로드가 깃발 창을 앞으로 겨누고 줄을 따라 질주 (다리가 달리고 흙먼지가 일고 화면이 울림)
    //   ③ 창끝이 지나가는 순간 줄 위 적마다 꿰뚫리는 반짝, 지나간 자리에 굵은 찌르기 줄기
    //   타워·규칙은 모름. 카메라·타워 위치·중심 위치·맞은 위치들만 받아서 그림 (줄 고르기는 위치로 셈)
    [DisallowMultipleComponent]
    public sealed class SpearSplashSkillEffect : MonoBehaviour
    {
        private const float CavalryShape = 11f;

        [SerializeField] private float m_riderSize = 2.2f;         // 칸에 대한 비율
        [SerializeField] private float m_lanceLength = 2.6f;       // 칸에 대한 비율
        [SerializeField] private float m_markDuration = 0.2f;
        [SerializeField] private float m_chargeDuration = 0.6f;
        [SerializeField] private float m_gallopsPerSecond = 9f;
        [SerializeField] private float m_dustInterval = 0.05f;

        private EffectQuadPool m_pool;
        private EffectQuadPool m_streakPool;

        private void Awake()
        {
            m_pool = new EffectQuadPool(transform, "SwordQi", "Vanguard Charge", 30);
            m_streakPool = new EffectQuadPool(transform, "SlashMark", "Vanguard Streak", 29);
        }

        private void OnDestroy()
        {
            m_pool?.Dispose();
            m_streakPool?.Dispose();
        }

        // camera: 화면 크기 / owner: 워로드 위치 / center: 공격 대상의 위치 / hitPositions: 맞은 적들 / cellWorldSize: 칸 한 변
        public void Play(Camera camera, Vector3 owner, Vector3 center, IReadOnlyList<Vector3> hitPositions, float cellWorldSize)
        {
            if (camera == null || !m_pool.IsReady || !m_streakPool.IsReady) return;

            float cell = Mathf.Max(0.5f, cellWorldSize);
            float height = camera.orthographic ? camera.orthographicSize * 2f : 10f;
            float width = height * camera.aspect;
            Vector3 view = camera.transform.position;
            float riderSize = cell * m_riderSize;
            float lanceLength = cell * m_lanceLength;

            // 줄 고르기: 같은 가로줄(y가 같은)과 세로줄(x가 같은)의 적 수
            int row = 0, column = 0;
            if (hitPositions != null)
            {
                foreach (Vector3 hit in hitPositions)
                {
                    if (Mathf.Abs(hit.y - center.y) < cell * 0.5f) row++;
                    if (Mathf.Abs(hit.x - center.x) < cell * 0.5f) column++;
                }
            }
            bool horizontal = row >= column;

            // 줄의 시작 · 끝 (화면 밖에서 밖으로): 타워 쪽에서 반대쪽으로 돌격
            Vector3 start, end;
            float margin = riderSize + lanceLength;
            if (horizontal)
            {
                float sign = owner.x <= center.x ? 1f : -1f;
                start = new Vector3(view.x - sign * (width * 0.5f + margin), center.y, center.z);
                end = new Vector3(view.x + sign * (width * 0.5f + margin), center.y, center.z);
            }
            else
            {
                float sign = owner.y <= center.y ? 1f : -1f;
                start = new Vector3(center.x, view.y - sign * (height * 0.5f + margin), center.z);
                end = new Vector3(center.x, view.y + sign * (height * 0.5f + margin), center.z);
            }
            Vector3 forward = (end - start).normalized;
            float travel = Mathf.Atan2(forward.y, forward.x) * Mathf.Rad2Deg;
            bool mirror = forward.x < -0.5f;                                       // 왼쪽으로 달릴 땐 뒤집어서 말이 똑바로 서게
            float bodyAngle = mirror ? 0f : travel;
            float lanceAngle = mirror ? 180f : travel;
            Vector3 lanceOffset = Orient(new Vector3(riderSize * 0.12f + lanceLength * 0.25f, riderSize * 0.14f, 0f), mirror, bodyAngle);
            float tipAhead = Vector3.Dot(lanceOffset, forward) + lanceLength * SpearFx.TipOffset;    // 말 가운데에서 창끝까지 (진행 방향)
            float total = (end - start).magnitude;

            // ① 표시선 (화면 안쪽 부분만 보여도 됨)
            Vector3 lineMiddle = (start + end) * 0.5f;
            SlashStroke.Play(m_streakPool, lineMiddle, travel, total, cell * 0.25f, 0.08f, SpearFx.Crimson,
                0f, m_markDuration, m_chargeDuration, 0.2f, 0.3f);

            // ② 말 탄 워로드와 깃발 창
            EffectQuad rider = m_pool.Get();
            rider.Transform.position = start;
            rider.Transform.rotation = Quaternion.Euler(0f, 0f, bodyAngle);
            rider.Transform.localScale = new Vector3(mirror ? -riderSize : riderSize, riderSize, 1f);
            rider.SetColor(SpearFx.Steel);
            rider.SetColorById(EffectQuad.Color2Id, SpearFx.Banner);
            rider.SetFloat(EffectQuad.ShapeId, CavalryShape);
            rider.SetFloat(EffectQuad.AlphaId, 1f);

            EffectQuad lance = SpearFx.MakeSpear(m_pool, start + lanceOffset, lanceAngle, SpearFx.Steel, SpearFx.BannerShape);
            lance.SetColorById(EffectQuad.Color2Id, SpearFx.Banner);
            lance.Transform.localScale = SpearFx.BannerScale(lanceLength, lanceAngle);

            float progress = 0f;
            float phase = 0f;
            Tween gallop = DOTween.To(() => phase, value => { phase = value; rider.SetFloat(EffectQuad.PhaseId, value); },
                    Mathf.PI * 2f * m_gallopsPerSecond * (m_markDuration + m_chargeDuration), m_markDuration + m_chargeDuration)
                .SetEase(Ease.Linear).SetLink(rider.GameObject, LinkBehaviour.KillOnDisable);
            Sequence sequence = DOTween.Sequence().SetLink(rider.GameObject, LinkBehaviour.KillOnDisable);
            sequence.AppendInterval(m_markDuration);
            sequence.AppendCallback(() => ScreenShake.Play(0.05f, m_chargeDuration));                      // 말발굽 울림
            sequence.Append(DOTween.To(() => progress, value =>
            {
                progress = value;
                Vector3 position = Vector3.LerpUnclamped(start, end, value);
                rider.Transform.position = position;
                lance.Transform.position = position + lanceOffset;
            }, 1f, m_chargeDuration).SetEase(Ease.Linear));
            for (float t = 0f; t < m_chargeDuration; t += m_dustInterval)                                    // 발밑 흙먼지
                sequence.InsertCallback(m_markDuration + t, () =>
                    ArrowFx.PlayDust(m_pool, rider.Transform.position + Orient(new Vector3(-riderSize * 0.15f, -riderSize * 0.36f, 0f), mirror, bodyAngle),
                        riderSize * 0.35f));
            sequence.OnKill(() =>
            {
                gallop.Kill();
                m_pool.Release(rider);
                m_pool.Release(lance);
            });

            // ③ 지나간 자리의 굵은 찌르기 줄기 + 줄 위 적마다 꿰뚫리는 반짝 (창끝이 닿는 시각에)
            SlashStroke.Play(m_streakPool, lineMiddle, travel, total, cell * 0.8f, 0.2f, SpearFx.Steel,
                m_markDuration, m_chargeDuration, 0.1f, 0.35f, 0.6f);
            if (hitPositions == null) return;
            foreach (Vector3 hit in hitPositions)
            {
                bool onLine = horizontal ? Mathf.Abs(hit.y - center.y) < cell * 0.5f : Mathf.Abs(hit.x - center.x) < cell * 0.5f;
                if (!onLine) continue;
                float along = Vector3.Dot(hit - start, forward);
                float passAt = m_markDuration + Mathf.Clamp01((along - tipAhead) / total) * m_chargeDuration;
                SpearFx.PlaySpark(m_pool, hit, cell * 0.8f, SpearFx.Crimson, travel, passAt);
            }
        }

        // 말 기준(오른쪽을 볼 때)의 자리를 지금 방향으로: 왼쪽이면 좌우만 뒤집고, 위·아래면 돌림
        private static Vector3 Orient(Vector3 local, bool mirror, float bodyAngle)
            => mirror ? new Vector3(-local.x, local.y, 0f) : Quaternion.Euler(0f, 0f, bodyAngle) * local;
    }
}
