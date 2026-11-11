using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace SquareTD
{
    // 바이킹(광역 도끼) 스킬 2 휠윈드 연출 — 대도끼가 빠르게 한 바퀴 돈 뒤 맵 전체의 적이 베임
    [DisallowMultipleComponent]
    public sealed class AxeSplashSkillEffect : MonoBehaviour
    {
        private static readonly Color s_steel = new Color(0.92f, 0.9f, 0.9f);
        private static readonly Color s_blood = new Color(0.85f, 0.05f, 0.07f);
        private static readonly Color s_hot = new Color(1f, 0.6f, 0.55f);
        private static readonly Color s_crit = new Color(1f, 0.92f, 0.9f);

        [Header("대도끼")]
        [Tooltip("도끼 전체 길이 (칸)")]
        [SerializeField] private float m_axeLength = 3f;
        [SerializeField] private float m_appear = 0.2f;
        [Tooltip("돌기 전에 뒤로 젖히는 각도")]
        [SerializeField] private float m_backSwing = 30f;
        [SerializeField] private float m_windUp = 0.6f;
        [Header("한 바퀴")]
        [SerializeField] private float m_spinDuration = 0.45f;
        [Tooltip("시작 방향 (90 = 위). 시계 반대 방향으로 한 바퀴")]
        [SerializeField] private float m_startAngle = 90f;
        [SerializeField] private float m_trailDegrees = 140f;
        [SerializeField] private float m_ghostInterval = 0.025f;
        [SerializeField] private float m_ghostLife = 0.14f;
        [Header("타격")]
        [Tooltip("다 돈 뒤 적들이 맞기까지의 간격")]
        [SerializeField] private float m_hitDelay = 0.15f;
        [SerializeField] private float m_hitStagger = 0.08f;
        [SerializeField] private float m_finish = 0.8f;

        private readonly System.Random m_random = new System.Random();
        private EffectQuadPool m_axePool;
        private EffectQuadPool m_rimPool;
        private EffectQuadPool m_ghostPool;
        private EffectQuadPool m_trailPool;
        private EffectQuadPool m_cutPool;
        private EffectQuadPool m_ringPool;
        private EffectQuadPool m_flarePool;
        private EffectQuadPool m_glowPool;
        private EffectParticles m_sparks;
        private EffectParticles m_motes;
        private Sequence m_sequence;

        private void Awake()
        {
            m_axePool = new EffectQuadPool(transform, "EffectSprite", "Whirlwind Axe", 31, EffectTextures.GreatAxe, false, null, true);
            m_rimPool = new EffectQuadPool(transform, "EffectSprite", "Whirlwind Axe Rim", 30, EffectTextures.GreatAxe, true, null, true);
            m_ghostPool = new EffectQuadPool(transform, "EffectSprite", "Whirlwind Axe Ghost", 29, EffectTextures.GreatAxe, true, null, true);
            m_trailPool = new EffectQuadPool(transform, "EffectSprite", "Whirlwind Trail", 28, EffectTextures.SlashTrail, true, ArcMesh.Get(m_trailDegrees, 0.35f));
            m_cutPool = new EffectQuadPool(transform, "EffectSprite", "Whirlwind Cut", 32, EffectTextures.SlashLine, true);
            m_ringPool = new EffectQuadPool(transform, "EffectSprite", "Whirlwind Ring", 29, EffectTextures.Ring, true);
            m_flarePool = new EffectQuadPool(transform, "EffectSprite", "Whirlwind Flare", 33, EffectTextures.Flare, true);
            m_glowPool = new EffectQuadPool(transform, "EffectSprite", "Whirlwind Glow", 27, EffectTextures.Glow, true);
            m_sparks = new EffectParticles(transform, "Whirlwind Sparks", EffectTextures.Spark, true, 2.5f, 34, true, 1f, false);
            m_motes = new EffectParticles(transform, "Whirlwind Motes", EffectTextures.Glow, true, 2.5f, 29, false, 0f, false);
        }

        private void OnDisable()
        {
            if (m_sequence != null && m_sequence.IsActive()) m_sequence.Kill();
            m_sequence = null;
        }

        private void OnDestroy()
        {
            m_axePool?.Dispose(); m_rimPool?.Dispose(); m_ghostPool?.Dispose(); m_trailPool?.Dispose();
            m_cutPool?.Dispose(); m_ringPool?.Dispose(); m_flarePool?.Dispose(); m_glowPool?.Dispose();
        }

        public void Play(Camera camera, Vector3 origin, IReadOnlyList<Vector3> hitPoints, int range, float cellWorldSize)
        {
            if (!m_axePool.IsReady) return;
            if (m_sequence != null && m_sequence.IsActive()) m_sequence.Kill();

            float cell = Mathf.Max(0.01f, cellWorldSize);
            float length = cell * m_axeLength;
            var hits = new List<Hit>();
            if (hitPoints != null)
            {
                foreach (Vector3 point in hitPoints)
                {
                    Vector3 offset = point - origin;
                    float direction = Mathf.Atan2(offset.y, offset.x) * Mathf.Rad2Deg;
                    bool critical = Mathf.Max(Mathf.Abs(offset.x), Mathf.Abs(offset.y)) <= (range + 0.5f) * cell;
                    hits.Add(new Hit { Point = point, Direction = direction, Critical = critical });
                }
            }

            float spinStart = m_windUp;
            float spinEnd = spinStart + m_spinDuration;
            float hitAt = spinEnd + m_hitDelay;
            EffectActivity.HoldFor(hitAt + m_finish + 0.1f);

            EffectQuad rim = Prepare(m_rimPool, s_blood, 2.2f);
            EffectQuad axe = Prepare(m_axePool, s_steel, 1.1f);
            EffectQuad trail = m_trailPool.Get();
            float trailRadius = length * 0.8f;
            trail.Transform.position = origin;
            trail.Transform.localScale = new Vector3(trailRadius, trailRadius, 1f);
            trail.SetColor(s_blood);
            trail.SetFloat(EffectQuad.IntensityId, 2.5f);
            trail.SetFloat(EffectQuad.AlphaId, 0f);

            float angle = m_startAngle, size = 0f, trailAlpha = 0f, alpha = 1f;
            Sequence s = DOTween.Sequence().SetLink(gameObject, LinkBehaviour.KillOnDisable);
            m_sequence = s;

            s.InsertCallback(0f, () => PlayGather(origin, cell));
            s.Insert(0f, DOTween.To(() => size, v => size = v, 1f, m_appear).SetEase(Ease.OutBack));
            s.Insert(m_appear, DOTween.To(() => angle, v => angle = v, m_startAngle - m_backSwing, m_windUp - m_appear).SetEase(Ease.OutQuad));
            s.Insert(spinStart, DOTween.To(() => angle, v => angle = v, m_startAngle + 360f, m_spinDuration).SetEase(Ease.InQuad));
            s.Insert(spinStart, DOTween.To(() => trailAlpha, v => trailAlpha = v, 0.9f, m_spinDuration * 0.3f));
            for (float t = 0f; t < m_spinDuration; t += m_ghostInterval)
                s.InsertCallback(spinStart + t, () => Ghost(origin, angle, length));
            s.InsertCallback(spinEnd, () =>
            {
                TexturedFx.Pop(m_flarePool, HeadPosition(origin, angle, length), 0f, cell * 0.3f, cell * 1.6f, Color.white, 3.5f, 0f, 0.08f, 0.16f);
                ScreenShake.Play(0.04f, 0.1f);
            });
            s.Insert(spinEnd, DOTween.To(() => trailAlpha, v => trailAlpha = v, 0f, 0.2f));
            s.Insert(spinEnd + 0.05f, DOTween.To(() => alpha, v => alpha = v, 0f, 0.25f).SetEase(Ease.InQuad));
            s.InsertCallback(hitAt, () =>
            {
                PlayFinish(origin, cell);
                foreach (Hit hit in hits)
                {
                    Hit target = hit;
                    DOVirtual.DelayedCall(Range(0f, m_hitStagger), () => PlayHit(target, cell)).SetLink(gameObject, LinkBehaviour.KillOnDisable);
                }
            });
            s.AppendInterval(m_finish);

            s.OnUpdate(() =>
            {
                Place(axe, origin, angle, length * size, alpha);
                Place(rim, origin, angle, length * size * 1.04f, alpha);
                trail.Transform.rotation = Quaternion.Euler(0f, 0f, angle - m_trailDegrees * 0.5f);
                trail.SetFloat(EffectQuad.AlphaId, trailAlpha);
            });
            s.OnKill(() =>
            {
                m_axePool.Release(axe);
                m_rimPool.Release(rim);
                m_trailPool.Release(trail);
                m_sequence = null;
            });
        }

        private struct Hit
        {
            public Vector3 Point;
            public float Direction;
            public bool Critical;
        }

        private static EffectQuad Prepare(EffectQuadPool pool, Color color, float intensity)
        {
            EffectQuad quad = pool.Get();
            quad.Transform.localScale = Vector3.zero;
            quad.SetColor(color);
            quad.SetFloat(EffectQuad.IntensityId, intensity);
            return quad;
        }

        private static void Place(EffectQuad quad, Vector3 origin, float angle, float length, float alpha)
        {
            float radians = angle * Mathf.Deg2Rad;
            quad.Transform.SetPositionAndRotation(origin + new Vector3(Mathf.Cos(radians), Mathf.Sin(radians), 0f) * (length * 0.48f),
                Quaternion.Euler(0f, 0f, angle));
            quad.Transform.localScale = new Vector3(Mathf.Max(0.001f, length), Mathf.Max(0.001f, length * 0.5f), 1f);
            quad.SetFloat(EffectQuad.AlphaId, alpha);
        }

        private static Vector3 HeadPosition(Vector3 origin, float angle, float length)
        {
            float radians = angle * Mathf.Deg2Rad;
            return origin + new Vector3(Mathf.Cos(radians), Mathf.Sin(radians), 0f) * (length * 0.78f);
        }

        private void Ghost(Vector3 origin, float angle, float length)
        {
            EffectQuad ghost = m_ghostPool.Get();
            ghost.SetColor(s_blood);
            ghost.SetFloat(EffectQuad.IntensityId, 2f);
            float alpha = 0.5f;
            Place(ghost, origin, angle, length, alpha);
            DOTween.To(() => alpha, v => { alpha = v; ghost.SetFloat(EffectQuad.AlphaId, v); }, 0f, m_ghostLife)
                .SetEase(Ease.OutQuad)
                .SetLink(ghost.GameObject, LinkBehaviour.KillOnDisable)
                .OnKill(() => m_ghostPool.Release(ghost));
        }

        private void PlayGather(Vector3 origin, float cell)
        {
            m_motes.Gather(origin, 20, s_blood, cell * 2.2f, cell * 0.1f, m_windUp);
            TexturedFx.Pop(m_glowPool, origin, 0f, cell * 0.3f, cell * 2f, s_blood, 1.8f, 0f, m_windUp, 0.2f, Ease.InCubic);
        }

        private void PlayHit(Hit hit, float cell)
        {
            float angle = hit.Direction + 90f + Range(-12f, 12f);
            float length = cell * (hit.Critical ? 2.2f : 1.4f);
            Color color = hit.Critical ? s_crit : s_blood;
            TexturedFx.Swipe(m_cutPool, hit.Point, angle, new Vector3(length, cell * (hit.Critical ? 0.34f : 0.24f), 1f), color,
                hit.Critical ? 4f : 2.8f, 0f, 0.05f, 0.05f, 0.2f);
            TexturedFx.Pop(m_ringPool, hit.Point, 0f, cell * 0.2f, cell * (hit.Critical ? 1.6f : 1f), s_blood, 2f, 0f, 0.22f, 0.22f, Ease.OutCubic);
            float radians = (hit.Direction + 90f) * Mathf.Deg2Rad;
            var direction = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
            m_sparks.Burst(hit.Point, hit.Critical ? 10 : 5, hit.Critical ? s_hot : s_blood, cell * 2f, cell * 6f, cell * 0.05f, cell * 0.1f,
                0.2f, 0.4f, direction, 60f);
            if (hit.Critical)
                TexturedFx.Pop(m_flarePool, hit.Point, Range(0f, 45f), cell * 0.3f, cell * 1.4f, Color.white, 3.5f, 0f, 0.07f, 0.16f);
        }

        private void PlayFinish(Vector3 origin, float cell)
        {
            TexturedFx.Pop(m_ringPool, origin, 0f, cell * 0.3f, cell * 5f, s_blood, 2.2f, 0f, 0.5f, 0.45f, Ease.OutCubic);
            TexturedFx.Pop(m_ringPool, origin, 0f, cell * 0.2f, cell * 3f, s_hot, 2.5f, 0f, 0.3f, 0.3f, Ease.OutCubic);
            m_sparks.Burst(origin, 20, s_blood, cell * 3f, cell * 8f, cell * 0.06f, cell * 0.12f, 0.3f, 0.5f);
            ScreenShake.Play(0.08f, 0.2f);
            ImpactFx.Punch(0.02f, 0.2f);
        }

        private float Range(float min, float max) => Mathf.Lerp(min, max, (float)m_random.NextDouble());
    }
}
