using DG.Tweening;
using UnityEngine;

namespace SquareTD
{
    // 블레이드 댄서(단일 검) 스킬 2 블레이드 스톰 연출 — 초승달 검기가 적을 베며 쌓이고, 쌓인 검기가 돌며 폭풍이 되어 펑 터짐
    [DisallowMultipleComponent]
    public sealed class SwordTargetSkillEffect : MonoBehaviour
    {
        public const int QiCount = SwordTargetSkill.StormHits;
        private static readonly Color s_blade = new Color(1f, 0.97f, 1f);
        private static readonly Color s_qi = new Color(0.62f, 0.86f, 1f);

        [SerializeField] private float m_slashInterval = 0.07f;
        [SerializeField] private float m_slashDraw = 0.04f;
        [SerializeField] private float m_pileHold = 0.15f;
        [SerializeField] private float m_gatherDuration = 0.4f;
        [SerializeField] private float m_stormDuration = 0.4f;
        [SerializeField] private float m_burstDuration = 0.3f;
        [SerializeField] private float m_ringRadius = 0.5f;
        [SerializeField] private float m_burstRadius = 2.2f;
        [SerializeField] private float m_stormSpin = 1260f;
        [SerializeField] private float m_glow = 2.2f;
        [SerializeField] private int m_burstSparks = 36;

        private readonly System.Random m_random = new System.Random();
        private EffectQuadPool m_arcPool;
        private EffectQuadPool m_flarePool;
        private EffectQuadPool m_glowPool;
        private EffectParticles m_sparks;

        private void Awake()
        {
            m_arcPool = new EffectQuadPool(transform, "EffectSprite", "Storm Qi", 24, EffectTextures.SlashTrail, true, ArcMesh.Get(120f, 0.28f));
            m_flarePool = new EffectQuadPool(transform, "EffectSprite", "Storm Flare", 27, EffectTextures.Flare, true);
            m_glowPool = new EffectQuadPool(transform, "EffectSprite", "Storm Glow", 22, EffectTextures.Glow, true);
            m_sparks = new EffectParticles(transform, "Storm Sparks", EffectTextures.Spark, true, 2.5f, 28, true, 0.8f, false);
        }

        private void OnDestroy()
        {
            m_arcPool?.Dispose(); m_flarePool?.Dispose(); m_glowPool?.Dispose();
        }

        public void Play(Vector3 target, float cellWorldSize)
        {
            if (!m_arcPool.IsReady) return;

            float size = Mathf.Max(0.5f, cellWorldSize);
            float gatherAt = (QiCount - 1) * m_slashInterval + m_slashDraw + m_pileHold;
            float stormAt = gatherAt + m_gatherDuration;
            float burstAt = stormAt + m_stormDuration;

            for (int i = 0; i < QiCount; i++)
            {
                PlayQi(target, size, i * m_slashInterval, gatherAt, burstAt, i % 2 == 0 ? s_blade : s_qi);
            }

            TexturedFx.Pop(m_glowPool, target, 0f, size * 0.2f, size * 1.3f, s_qi, 1.2f, gatherAt, m_gatherDuration + m_stormDuration, 0.12f);
            DOVirtual.DelayedCall(stormAt, () => ScreenShake.Play(0.02f, m_stormDuration)).SetLink(gameObject, LinkBehaviour.KillOnDisable);
            for (float at = stormAt; at < burstAt; at += 0.1f)
            {
                DOVirtual.DelayedCall(at, () => m_sparks.Burst(target, 4, s_qi, size * 1.5f, size * 3f, size * 0.05f, size * 0.09f, 0.15f, 0.25f))
                    .SetLink(gameObject, LinkBehaviour.KillOnDisable);
            }

            DOVirtual.DelayedCall(burstAt - 0.05f, () => ImpactFx.Slow(0.3f, 0.1f)).SetLink(gameObject, LinkBehaviour.KillOnDisable);
            DOVirtual.DelayedCall(burstAt, () =>
            {
                ImpactFx.Frame(new Color(0.75f, 0.9f, 1f, 0.55f), 0.05f);
                ImpactFx.Punch(0.03f, 0.3f);
                ScreenShake.Play(0.07f, 0.2f);
                TexturedFx.Pop(m_glowPool, target, 0f, size * 0.6f, size * 2.8f, s_qi, 1.8f, 0f, 0.22f, 0.4f);
                TexturedFx.Pop(m_flarePool, target, 0f, size * 0.4f, size * 2.2f, Color.white, 3f, 0f, 0.12f, 0.3f);
                TexturedFx.Pop(m_flarePool, target, 45f, size * 0.3f, size * 1.6f, s_blade, 2.5f, 0.03f, 0.12f, 0.3f);
                m_sparks.Burst(target, m_burstSparks, s_blade, size * 3f, size * 9f, size * 0.08f, size * 0.16f, 0.25f, 0.5f);
            }).SetLink(gameObject, LinkBehaviour.KillOnDisable);
        }

        private void PlayQi(Vector3 target, float size, float slashAt, float gatherAt, float burstAt, Color color)
        {
            float angle = Range(0f, 360f);
            float slashRadius = size * Range(0.42f, 0.6f);
            float flip = Range(0f, 1f) < 0.5f ? -1f : 1f;
            Vector3 offset = new Vector3(Range(-0.12f, 0.12f), Range(-0.12f, 0.12f), 0f) * size;

            EffectQuad arc = m_arcPool.Get();
            arc.SetColor(color);
            arc.SetFloat(EffectQuad.IntensityId, m_glow);
            arc.SetFloat(EffectQuad.HeadFollowId, 1f);
            arc.SetFloat(EffectQuad.ProgressId, 0f);
            arc.SetFloat(EffectQuad.TailId, 0f);
            arc.SetFloat(EffectQuad.AlphaId, 1f);

            void Place(float a, float radius, float toRing)
            {
                float radians = a * Mathf.Deg2Rad;
                Vector3 cross = target + offset * (1f - toRing);
                Vector3 pivot = cross - new Vector3(Mathf.Cos(radians), Mathf.Sin(radians), 0f) * radius;
                arc.Transform.position = Vector3.Lerp(pivot, target, toRing);
                arc.Transform.rotation = Quaternion.Euler(0f, 0f, a);
                arc.Transform.localScale = new Vector3(radius, radius * flip, 1f);
            }

            Place(angle, slashRadius, 0f);
            arc.Transform.localScale = Vector3.zero;

            float progress = 0f;
            Sequence sequence = DOTween.Sequence().SetLink(arc.GameObject, LinkBehaviour.KillOnDisable);
            sequence.AppendInterval(slashAt);
            sequence.AppendCallback(() => Place(angle, slashRadius, 0f));
            sequence.Append(DOTween.To(() => progress, value => { progress = value; arc.SetFloat(EffectQuad.ProgressId, value); }, 1f, m_slashDraw)
                .SetEase(Ease.OutExpo));
            sequence.AppendCallback(() =>
            {
                TexturedFx.Pop(m_flarePool, target + offset, Range(0f, 90f), size * 0.1f, size * 0.5f, color, 2f, 0f, 0.06f, 0.14f);
                m_sparks.Burst(target + offset, 3, color, size * 1.5f, size * 3f, size * 0.05f, size * 0.09f, 0.12f, 0.2f);
            });
            sequence.AppendInterval(Mathf.Max(0f, gatherAt - slashAt - m_slashDraw));

            float elapsed = 0f;
            float swirl = burstAt - gatherAt;
            sequence.Append(DOTween.To(() => elapsed, value =>
            {
                float step = value - elapsed;
                elapsed = value;
                float t = Mathf.Clamp01(elapsed / m_gatherDuration);
                float eased = 1f - (1f - t) * (1f - t) * (1f - t);
                angle -= Mathf.Lerp(90f, m_stormSpin, t) * step;
                Place(angle, Mathf.Lerp(slashRadius, m_ringRadius * size, eased), eased);
            }, swirl, swirl).SetEase(Ease.Linear));

            float spread = 0f;
            sequence.Append(DOTween.To(() => spread, value =>
            {
                spread = value;
                angle -= 12f;
                Place(angle, Mathf.Lerp(m_ringRadius, m_burstRadius, value) * size, 1f);
                arc.SetFloat(EffectQuad.AlphaId, 1f - value);
            }, 1f, m_burstDuration).SetEase(Ease.OutCubic));
            sequence.OnKill(() => m_arcPool.Release(arc));
        }

        private float Range(float min, float max) => Mathf.Lerp(min, max, (float)m_random.NextDouble());
    }
}
