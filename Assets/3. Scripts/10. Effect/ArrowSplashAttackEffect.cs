using System;
using DG.Tweening;
using UnityEngine;

namespace SquareTD
{
    // 레인저(광역 활) 기본 공격 연출
    [DisallowMultipleComponent]
    public sealed class ArrowSplashAttackEffect : MonoBehaviour, IShotEffect
    {
        [SerializeField] private float m_arrowLength = 0.55f;
        [SerializeField] private float m_riseDuration = 0.14f;
        [SerializeField] private float m_rainDelay = 0.12f;
        [SerializeField] private float m_fallDuration = 0.16f;
        [SerializeField] private float m_arrowInterval = 0.03f;
        [SerializeField] private float m_slant = 18f;
        [SerializeField] private float m_stayDuration = 0.35f;

        private readonly System.Random m_random = new System.Random();
        private EffectQuadPool m_pool;

        private void Awake() => m_pool = new EffectQuadPool(transform, "SwordQi", "Ranger Arrow", 24);

        private void OnDestroy() => m_pool?.Dispose();

        public void FireShot(Vector3 from, Vector3 to, float speed, float hold, float sizeWorld, Action onArrived)
        {
            if (!m_pool.IsReady)
            {
                onArrived?.Invoke();
                return;
            }

            float cell = EffectSpace.CellWorldSize();
            float length = cell * m_arrowLength;
            float area = Mathf.Max(cell, sizeWorld);

            EffectQuad volley = m_pool.Get();
            volley.Transform.position = from;
            volley.Transform.rotation = Quaternion.Euler(0f, 0f, 90f);
            volley.Transform.localScale = Vector3.zero;
            volley.SetColor(ArrowFx.Wood);
            volley.SetFloat(EffectQuad.ShapeId, 8f);
            volley.SetFloat(EffectQuad.AlphaId, 1f);

            float shootAt = Mathf.Max(hold, 0.1f);
            float alpha = 1f;
            Sequence sequence = DOTween.Sequence().SetLink(volley.GameObject, LinkBehaviour.KillOnDisable);
            sequence.Append(volley.Transform.DOScale(new Vector3(length, length * 0.5f, 1f), 0.1f).SetEase(Ease.OutBack));
            sequence.AppendInterval(shootAt - 0.1f);
            sequence.Append(volley.Transform.DOMoveY(from.y + cell * 3f, m_riseDuration).SetEase(Ease.InQuad));
            sequence.Join(DOTween.To(() => alpha, value => { alpha = value; volley.SetFloat(EffectQuad.AlphaId, value); },
                0f, m_riseDuration).SetEase(Ease.InQuad));
            sequence.OnKill(() => m_pool.Release(volley));

            int side = Mathf.Max(1, Mathf.RoundToInt(area / cell));
            int count = Mathf.Clamp(4 + side * 2, 6, 14);
            float rainAt = shootAt + m_riseDuration + m_rainDelay;
            for (int i = 0; i < count; i++)
            {
                Vector3 land = to + new Vector3(Range(-0.45f, 0.45f) * area, Range(-0.45f, 0.45f) * area, 0f);
                ArrowFx.PlayFallingArrow(m_pool, land, length, rainAt + i * m_arrowInterval, m_slant + Range(-6f, 6f),
                    cell * 2.2f, m_fallDuration, m_stayDuration, ArrowFx.Wood, i % 2 == 0, i == 0 ? onArrived : null);
            }
        }

        private float Range(float min, float max) => Mathf.Lerp(min, max, (float)m_random.NextDouble());
    }
}
