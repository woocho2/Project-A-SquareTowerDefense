using System;
using DG.Tweening;
using UnityEngine;

namespace SquareTD
{
    // 소드 마스터(광역 검) 기본 공격 연출
    [DisallowMultipleComponent]
    public sealed class SwordSplashAttackEffect : MonoBehaviour, IShotEffect
    {
        private static readonly Color s_gold = new Color(1f, 0.78f, 0.38f);
        private static readonly Color s_white = new Color(1f, 0.95f, 0.85f);

        [SerializeField] private float m_qiSize = 0.9f;
        [SerializeField] private float m_appearDuration = 0.16f;
        [SerializeField] private float m_bobHeight = 0.07f;
        [SerializeField] private float m_glow = 2.5f;
        [SerializeField] private float m_slashDraw = 0.08f;
        [SerializeField] private float m_slashTail = 0.16f;

        private readonly System.Random m_random = new System.Random();
        private EffectQuadPool m_qiPool;
        private EffectQuadPool m_arcPool;
        private EffectQuadPool m_glowPool;
        private EffectQuadPool m_flarePool;
        private EffectQuadPool m_ringPool;
        private EffectParticles m_sparks;

        private void Awake()
        {
            m_qiPool = new EffectQuadPool(transform, "EffectSprite", "Sword Saint Qi", 24, EffectTextures.SwordQi, true);
            m_arcPool = new EffectQuadPool(transform, "EffectSprite", "Sword Saint Arc", 25, EffectTextures.SlashTrail, true, ArcMesh.Get(110f, 0.35f));
            m_glowPool = new EffectQuadPool(transform, "EffectSprite", "Sword Saint Glow", 23, EffectTextures.Glow, true);
            m_flarePool = new EffectQuadPool(transform, "EffectSprite", "Sword Saint Flare", 26, EffectTextures.Flare, true);
            m_ringPool = new EffectQuadPool(transform, "EffectSprite", "Sword Saint Ring", 23, EffectTextures.Ring, true);
            m_sparks = new EffectParticles(transform, "Sword Saint Sparks", EffectTextures.Spark, true, 3f, 27, true, 1.2f, false);
        }

        private void OnDestroy()
        {
            m_qiPool?.Dispose(); m_arcPool?.Dispose(); m_glowPool?.Dispose(); m_flarePool?.Dispose(); m_ringPool?.Dispose();
        }

        public void FireShot(Vector3 from, Vector3 to, float speed, float hold, float sizeWorld, Action onArrived)
        {
            if (!m_qiPool.IsReady || !m_arcPool.IsReady)
            {
                onArrived?.Invoke();
                return;
            }

            float cell = EffectSpace.CellWorldSize();
            float length = cell * m_qiSize;
            Vector3 fullScale = new Vector3(length, length * 0.5f, 1f);
            Vector3 toTarget = to - from;
            float angle = Mathf.Atan2(toTarget.y, toTarget.x) * Mathf.Rad2Deg;
            float flight = Mathf.Max(0.15f, toTarget.magnitude / Mathf.Max(0.01f, speed));
            float launchAt = Mathf.Max(hold, m_appearDuration);

            EffectQuad qi = m_qiPool.Get();
            qi.Transform.position = from;
            qi.Transform.rotation = Quaternion.Euler(0f, 0f, 90f);
            qi.Transform.localScale = Vector3.zero;
            qi.SetColor(s_gold);
            qi.SetFloat(EffectQuad.IntensityId, m_glow);
            Tween bob = null;

            Sequence sequence = DOTween.Sequence().SetLink(qi.GameObject, LinkBehaviour.KillOnDisable);
            sequence.Append(qi.Transform.DOScale(fullScale, m_appearDuration).SetEase(Ease.OutBack));
            sequence.AppendCallback(() => bob = qi.Transform.DOMoveY(from.y + m_bobHeight, 0.45f)
                .SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetLink(qi.GameObject, LinkBehaviour.KillOnDisable));
            sequence.AppendInterval(launchAt - m_appearDuration);
            sequence.AppendCallback(() =>
            {
                bob?.Kill();
                qi.Transform.rotation = Quaternion.Euler(0f, 0f, angle);
            });
            sequence.Append(qi.Transform.DOScale(new Vector3(length * 1.8f, length * 0.4f, 1f), 0.08f));
            sequence.Join(qi.Transform.DOMove(to, flight).SetEase(Ease.InQuad));
            for (float t = 0.03f; t < flight; t += 0.03f)
                sequence.InsertCallback(launchAt + t, () =>
                    m_sparks.Burst(qi.Transform.position, 2, s_gold, 0.5f, 1.5f, cell * 0.08f, cell * 0.14f, 0.2f, 0.35f));
            sequence.OnComplete(() =>
            {
                m_qiPool.Release(qi);
                PlayCross(to, Mathf.Max(cell, sizeWorld), cell);
                onArrived?.Invoke();
            });
            sequence.OnKill(() =>
            {
                bob?.Kill();
                m_qiPool.Release(qi);
            });
        }

        private void PlayCross(Vector3 center, float area, float cell)
        {
            float radius = area * 0.55f;
            float first = 45f + Range(-15f, 15f);
            PlayArc(center, first, radius, false, 0f);
            PlayArc(center, first + 90f, radius, true, 0.06f);

            TexturedFx.Pop(m_flarePool, center, Range(0f, 45f), area * 0.3f, area * 1.1f, s_white, 3f, 0.08f, 0.12f, 0.18f);
            TexturedFx.Pop(m_glowPool, center, 0f, area * 0.5f, area * 1.4f, s_gold, 1.6f, 0.06f, 0.16f, 0.22f);
            TexturedFx.Pop(m_ringPool, center, 0f, area * 0.2f, area * 1.3f, s_gold, 2f, 0.08f, 0.3f, 0.3f, Ease.OutCubic);
            DOVirtual.DelayedCall(0.08f, () =>
            {
                m_sparks.Burst(center, 16, s_gold, cell * 3f, cell * 8f, cell * 0.1f, cell * 0.2f, 0.25f, 0.5f);
                ScreenShake.Play(0.035f, 0.1f);
                ImpactFx.Punch(0.012f, 0.15f);
            }).SetLink(gameObject, LinkBehaviour.KillOnDisable);
        }

        private void PlayArc(Vector3 center, float angle, float radius, bool mirror, float delay)
        {
            float radians = angle * Mathf.Deg2Rad;
            Vector3 pivot = center - new Vector3(Mathf.Cos(radians), Mathf.Sin(radians), 0f) * radius;
            Vector3 scale = new Vector3(radius, mirror ? -radius : radius, 1f);
            TexturedFx.Swipe(m_arcPool, pivot, angle, scale, s_gold, m_glow, delay, m_slashDraw, 0.04f, m_slashTail);
        }

        private float Range(float min, float max) => Mathf.Lerp(min, max, (float)m_random.NextDouble());
    }
}
