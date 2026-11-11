using System;
using DG.Tweening;
using UnityEngine;

namespace SquareTD
{
    // 소드 마스터(광역 검) 기본 공격 연출
    [DisallowMultipleComponent]
    public sealed class SwordSplashAttackEffect : MonoBehaviour, IShotEffect
    {
        private static readonly Color s_violet = new Color(0.45f, 0.34f, 1f);
        private static readonly Color s_deep = new Color(0.24f, 0.15f, 0.72f);
        private static readonly Color s_white = new Color(0.94f, 0.93f, 1f);

        [Header("떠 있는 검기")]
        [SerializeField] private float m_qiSize = 1f;
        [SerializeField] private float m_appearDuration = 0.14f;
        [SerializeField] private float m_bobHeight = 0.06f;
        [SerializeField] private float m_glow = 3f;
        [Header("날아감")]
        [Tooltip("쏘기 전에 뒤로 살짝 당기는 시간")]
        [SerializeField] private float m_windUp = 0.06f;
        [Tooltip("1보다 작을수록 빨리 날아감")]
        [SerializeField] private float m_flightScale = 0.7f;
        [SerializeField] private float m_ghostInterval = 0.02f;
        [SerializeField] private float m_ghostLife = 0.12f;
        [Header("맞을 때")]
        [SerializeField] private float m_shakeStrength = 0.05f;
        [SerializeField] private float m_shakeDuration = 0.12f;

        private EffectQuadPool m_qiPool;
        private EffectQuadPool m_corePool;
        private EffectQuadPool m_ghostPool;
        private EffectQuadPool m_glowPool;
        private EffectQuadPool m_ringPool;
        private EffectParticles m_sparks;
        private EffectParticles m_motes;

        private void Awake()
        {
            m_qiPool = new EffectQuadPool(transform, "EffectSprite", "Sword Saint Qi", 24, EffectTextures.SwordQi, true);
            m_corePool = new EffectQuadPool(transform, "EffectSprite", "Sword Saint Qi Core", 25, EffectTextures.SwordQi, true);
            m_ghostPool = new EffectQuadPool(transform, "EffectSprite", "Sword Saint Qi Ghost", 23, EffectTextures.SwordQi, true);
            m_glowPool = new EffectQuadPool(transform, "EffectSprite", "Sword Saint Glow", 22, EffectTextures.Glow, true);
            m_ringPool = new EffectQuadPool(transform, "EffectSprite", "Sword Saint Ring", 23, EffectTextures.Ring, true);
            m_sparks = new EffectParticles(transform, "Sword Saint Sparks", EffectTextures.Spark, true, 3f, 28, true, 1.2f, false);
            m_motes = new EffectParticles(transform, "Sword Saint Motes", EffectTextures.Glow, true, 3f, 26, false, 0f, false);
        }

        private void OnDestroy()
        {
            m_qiPool?.Dispose(); m_corePool?.Dispose(); m_ghostPool?.Dispose(); m_glowPool?.Dispose(); m_ringPool?.Dispose();
        }

        public void FireShot(Vector3 from, Vector3 to, float speed, float hold, float sizeWorld, Action onArrived)
        {
            if (!m_qiPool.IsReady)
            {
                onArrived?.Invoke();
                return;
            }

            float cell = EffectSpace.CellWorldSize();
            float length = cell * m_qiSize;
            Vector3 fullScale = new Vector3(length, length * 0.5f, 1f);
            Vector3 toTarget = to - from;
            Vector3 direction = toTarget.sqrMagnitude > 0.0001f ? toTarget.normalized : Vector3.right;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            float flight = Mathf.Max(0.1f, toTarget.magnitude / Mathf.Max(0.01f, speed) * m_flightScale);
            float launchAt = Mathf.Max(hold, m_appearDuration);

            EffectQuad qi = m_qiPool.Get();
            EffectQuad core = m_corePool.Get();
            EffectQuad glow = m_glowPool.Get();
            qi.Transform.position = from;
            qi.Transform.rotation = Quaternion.Euler(0f, 0f, 90f);
            qi.Transform.localScale = Vector3.zero;
            qi.SetColor(s_violet);
            qi.SetFloat(EffectQuad.IntensityId, m_glow);
            core.SetColor(s_white);
            core.SetFloat(EffectQuad.IntensityId, m_glow * 1.3f);
            glow.SetColor(s_deep);
            glow.SetFloat(EffectQuad.IntensityId, 1.6f);
            Follow(qi, core, glow);
            m_motes.Gather(from, 10, s_violet, cell * 0.9f, cell * 0.08f, m_appearDuration + 0.06f);

            Tween bob = null;
            Sequence sequence = DOTween.Sequence().SetLink(qi.GameObject, LinkBehaviour.KillOnDisable);
            sequence.OnUpdate(() => Follow(qi, core, glow));
            sequence.Append(qi.Transform.DOScale(fullScale, m_appearDuration).SetEase(Ease.OutBack));
            sequence.AppendCallback(() => bob = qi.Transform.DOMoveY(from.y + m_bobHeight, 0.45f)
                .SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetLink(qi.GameObject, LinkBehaviour.KillOnDisable));
            sequence.AppendInterval(Mathf.Max(0f, launchAt - m_appearDuration));

            sequence.AppendCallback(() =>
            {
                bob?.Kill();
                qi.Transform.rotation = Quaternion.Euler(0f, 0f, angle);
            });
            sequence.Append(qi.Transform.DOMove(from - direction * cell * 0.12f, m_windUp).SetEase(Ease.OutQuad));
            sequence.Join(qi.Transform.DOScale(new Vector3(length * 0.8f, length * 0.65f, 1f), m_windUp).SetEase(Ease.OutQuad));

            float flyAt = launchAt + m_windUp;
            sequence.Append(qi.Transform.DOScale(new Vector3(length * 1.9f, length * 0.38f, 1f), 0.06f).SetEase(Ease.OutQuad));
            sequence.Join(qi.Transform.DOMove(to, flight).SetEase(Ease.InSine));
            for (float t = 0f; t < flight; t += m_ghostInterval)
                sequence.InsertCallback(flyAt + t, () => Ghost(qi));
            for (float t = 0.02f; t < flight; t += 0.03f)
                sequence.InsertCallback(flyAt + t, () =>
                    m_sparks.Burst(qi.Transform.position, 2, s_violet, cell * 0.5f, cell * 1.5f, cell * 0.06f, cell * 0.12f, 0.15f, 0.3f,
                        new Vector2(-direction.x, -direction.y), 60f));

            sequence.OnComplete(() =>
            {
                Release(qi, core, glow);
                PlaySplash(to, Mathf.Max(cell, sizeWorld));
                onArrived?.Invoke();
            });
            sequence.OnKill(() =>
            {
                bob?.Kill();
                Release(qi, core, glow);
            });
        }

        private static void Follow(EffectQuad qi, EffectQuad core, EffectQuad glow)
        {
            Transform source = qi.Transform;
            core.Transform.SetPositionAndRotation(source.position, source.rotation);
            core.Transform.localScale = new Vector3(source.localScale.x * 0.82f, source.localScale.y * 0.62f, 1f);
            glow.Transform.position = source.position;
            float size = Mathf.Max(source.localScale.x, source.localScale.y) * 1.5f;
            glow.Transform.localScale = new Vector3(size, size, 1f);
        }

        private void Release(EffectQuad qi, EffectQuad core, EffectQuad glow)
        {
            m_qiPool.Release(qi);
            m_corePool.Release(core);
            m_glowPool.Release(glow);
        }

        private void Ghost(EffectQuad qi)
        {
            EffectQuad ghost = m_ghostPool.Get();
            ghost.Transform.SetPositionAndRotation(qi.Transform.position, qi.Transform.rotation);
            ghost.Transform.localScale = qi.Transform.localScale;
            ghost.SetColor(s_violet);
            ghost.SetFloat(EffectQuad.IntensityId, m_glow * 0.7f);
            float alpha = 0.55f;
            ghost.SetFloat(EffectQuad.AlphaId, alpha);
            DOTween.To(() => alpha, v => { alpha = v; ghost.SetFloat(EffectQuad.AlphaId, v); }, 0f, m_ghostLife)
                .SetEase(Ease.OutQuad)
                .SetLink(ghost.GameObject, LinkBehaviour.KillOnDisable)
                .OnKill(() => m_ghostPool.Release(ghost));
        }

        private void PlaySplash(Vector3 center, float area)
        {
            TexturedFx.Pop(m_ringPool, center, 0f, area * 0.2f, area * 1.3f, s_violet, 2.2f, 0f, 0.28f, 0.28f, Ease.OutCubic);
            TexturedFx.Pop(m_ringPool, center, 0f, area * 0.1f, area * 0.8f, s_white, 2.5f, 0f, 0.16f, 0.18f, Ease.OutCubic);
            ScreenShake.Play(m_shakeStrength, m_shakeDuration);
        }
    }
}
