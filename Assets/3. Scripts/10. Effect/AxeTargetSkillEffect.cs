using DG.Tweening;
using UnityEngine;

namespace SquareTD
{
    // 버서커(단일 도끼) 스킬 2 연출
    [DisallowMultipleComponent]
    public sealed class AxeTargetSkillEffect : MonoBehaviour
    {
        private static readonly Color s_rage = new Color(1f, 0.25f, 0.15f);
        private static readonly Color s_blade = new Color(1f, 0.55f, 0.35f);

        [SerializeField] private float m_axeSize = 2f;
        [SerializeField] private float m_appearDuration = 0.18f;
        [SerializeField] private float m_chargeDuration = 0.35f;
        [SerializeField] private float m_slamDuration = 0.08f;
        [SerializeField] private float m_hitStop = 0.18f;
        [SerializeField] private float m_rageDuration = 2.5f;

        private readonly System.Random m_random = new System.Random();
        private EffectQuadPool m_shapePool;
        private EffectQuadPool m_slashPool;

        private void Awake()
        {
            m_shapePool = new EffectQuadPool(transform, "SwordQi", "Cleave Shape", 26);
            m_slashPool = new EffectQuadPool(transform, "SlashMark", "Cleave Slash", 25);
        }

        private void OnDestroy()
        {
            m_shapePool?.Dispose();
            m_slashPool?.Dispose();
        }

        public void Play(Vector3 owner, Vector3 target, float cellWorldSize)
        {
            if (!m_shapePool.IsReady || !m_slashPool.IsReady) return;

            float size = Mathf.Max(0.5f, cellWorldSize);
            float scale = size * m_axeSize;
            Vector3 embed = target + new Vector3(0.25f * scale, 0.28f * scale, 0f);
            Vector3 top = embed + Vector3.up * (size * 1.4f);
            Vector3 higher = top + Vector3.up * (size * 0.35f);
            float impactAt = m_appearDuration + m_chargeDuration + m_slamDuration;

            EffectQuad axe = m_shapePool.Get();
            axe.Transform.position = top;
            axe.Transform.rotation = Quaternion.Euler(0f, 0f, 180f);
            axe.Transform.localScale = Vector3.zero;
            axe.SetColor(s_blade);
            axe.SetFloat(EffectQuad.ShapeId, 5f);
            axe.SetFloat(EffectQuad.AlphaId, 1f);

            Sequence sequence = DOTween.Sequence().SetLink(axe.GameObject, LinkBehaviour.KillOnDisable);
            sequence.Append(axe.Transform.DOScale(new Vector3(scale, scale, 1f), m_appearDuration).SetEase(Ease.OutBack));
            sequence.Append(axe.Transform.DOMove(higher, m_chargeDuration).SetEase(Ease.OutCubic));
            sequence.Join(axe.Transform.DOShakeRotation(m_chargeDuration, new Vector3(0f, 0f, 6f), 20, 90f, true));
            sequence.Append(axe.Transform.DOMove(embed, m_slamDuration).SetEase(Ease.InExpo));
            for (int k = 1; k <= 4; k++)
                sequence.InsertCallback(m_appearDuration + m_chargeDuration + m_slamDuration * k / 5f,
                    () => AxeImpact.PlayGhost(m_shapePool, axe.Transform, s_blade, 0.4f, 0.2f));
            sequence.AppendCallback(() =>
            {
                SlashStroke.Play(m_slashPool, target, 90f, size * 2.4f, size * 0.8f, 0.32f, s_blade, 0f, 0.05f, 0.15f, 0.4f);
                AxeImpact.Play(m_shapePool, m_slashPool, m_random, target, size * 1.9f, 7, 0f, 1f);
            });
            sequence.Append(axe.Transform.DOPunchScale(new Vector3(0.12f, -0.12f, 0f) * scale, m_hitStop, 10, 0.4f));
            sequence.Append(axe.Transform.DOScale(Vector3.zero, 0.2f).SetEase(Ease.InBack));
            sequence.OnKill(() => m_shapePool.Release(axe));

            PlayRage(owner, size, impactAt);
        }

        private void PlayRage(Vector3 owner, float size, float startAt)
        {
            EffectQuad rage = m_shapePool.Get();
            rage.Transform.position = owner;
            rage.Transform.localScale = Vector3.zero;
            rage.SetColor(s_rage);
            rage.SetFloat(EffectQuad.ShapeId, 2f);
            rage.SetFloat(EffectQuad.AlphaId, 0.75f);
            rage.SetSortingOffset(-5);

            float alpha = 0.75f;
            float pulse = 0.3f;
            int pulses = Mathf.Max(2, Mathf.RoundToInt(m_rageDuration / pulse));
            if (pulses % 2 == 1) pulses++;
            Sequence sequence = DOTween.Sequence().SetLink(rage.GameObject, LinkBehaviour.KillOnDisable);
            sequence.AppendInterval(startAt);
            sequence.Append(rage.Transform.DOScale(new Vector3(size * 1.8f, size * 1.8f, 1f), 0.15f).SetEase(Ease.OutBack));
            sequence.Append(rage.Transform.DOScale(new Vector3(size * 2.1f, size * 2.1f, 1f), pulse)
                .SetEase(Ease.InOutSine).SetLoops(pulses, LoopType.Yoyo));
            sequence.Append(DOTween.To(() => alpha, value => { alpha = value; rage.SetFloat(EffectQuad.AlphaId, value); },
                0f, 0.4f).SetEase(Ease.InQuad));
            sequence.OnKill(() => m_shapePool.Release(rage));
        }
    }
}
