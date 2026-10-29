using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace SquareTD
{
    // 워로드(광역 창) 옛 스킬 2(줄 돌격) 연출
    [DisallowMultipleComponent]
    public sealed class SpearSplashSkillEffect : MonoBehaviour
    {
        private const float CavalryShape = 11f;

        [SerializeField] private float m_riderSize = 2.2f;
        [SerializeField] private float m_lanceLength = 2.6f;
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

        public void Play(Camera camera, Vector3 owner, Vector3 center, IReadOnlyList<Vector3> hitPositions, float cellWorldSize)
        {
            if (camera == null || !m_pool.IsReady || !m_streakPool.IsReady) return;

            float cell = Mathf.Max(0.5f, cellWorldSize);
            float height = camera.orthographic ? camera.orthographicSize * 2f : 10f;
            float width = height * camera.aspect;
            Vector3 view = camera.transform.position;
            float riderSize = cell * m_riderSize;
            float lanceLength = cell * m_lanceLength;

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
            bool mirror = forward.x < -0.5f;
            float bodyAngle = mirror ? 0f : travel;
            float lanceAngle = mirror ? 180f : travel;
            Vector3 lanceOffset = Orient(new Vector3(riderSize * 0.12f + lanceLength * 0.25f, riderSize * 0.14f, 0f), mirror, bodyAngle);
            float tipAhead = Vector3.Dot(lanceOffset, forward) + lanceLength * SpearFx.TipOffset;
            float total = (end - start).magnitude;

            Vector3 lineMiddle = (start + end) * 0.5f;
            SlashStroke.Play(m_streakPool, lineMiddle, travel, total, cell * 0.25f, 0.08f, SpearFx.Crimson,
                0f, m_markDuration, m_chargeDuration, 0.2f, 0.3f);

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
            sequence.AppendCallback(() => ScreenShake.Play(0.05f, m_chargeDuration));
            sequence.Append(DOTween.To(() => progress, value =>
            {
                progress = value;
                Vector3 position = Vector3.LerpUnclamped(start, end, value);
                rider.Transform.position = position;
                lance.Transform.position = position + lanceOffset;
            }, 1f, m_chargeDuration).SetEase(Ease.Linear));
            for (float t = 0f; t < m_chargeDuration; t += m_dustInterval)
                sequence.InsertCallback(m_markDuration + t, () =>
                    ArrowFx.PlayDust(m_pool, rider.Transform.position + Orient(new Vector3(-riderSize * 0.15f, -riderSize * 0.36f, 0f), mirror, bodyAngle),
                        riderSize * 0.35f));
            sequence.OnKill(() =>
            {
                gallop.Kill();
                m_pool.Release(rider);
                m_pool.Release(lance);
            });

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

        private static Vector3 Orient(Vector3 local, bool mirror, float bodyAngle)
            => mirror ? new Vector3(-local.x, local.y, 0f) : Quaternion.Euler(0f, 0f, bodyAngle) * local;
    }
}
