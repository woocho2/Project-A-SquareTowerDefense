using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace SquareTD
{
    // 레인저(광역 활) 스킬 2 화살비 연출
    [DisallowMultipleComponent]
    public sealed class ArrowSplashSkillEffect : MonoBehaviour
    {
        private static readonly Color s_sky = new Color(0.05f, 0.05f, 0.1f);

        [SerializeField] private int m_arrowCount = 70;
        [SerializeField] private float m_arrowLength = 0.6f;
        [SerializeField] private float m_volleyDuration = 0.35f;
        [SerializeField] private int m_volleyWaves = 3;
        [SerializeField] private float m_volleyWaveGap = 0.12f;
        [SerializeField] private float m_rainSpread = 1f;
        [SerializeField] private float m_fallDuration = 0.2f;
        [SerializeField] private float m_slant = 16f;
        [SerializeField] private float m_stayDuration = 0.6f;
        [SerializeField, Range(0f, 1f)] private float m_skyDarkness = 0.35f;

        private readonly System.Random m_random = new System.Random();
        private EffectQuadPool m_pool;

        private void Awake() => m_pool = new EffectQuadPool(transform, "SwordQi", "Arrow Rain", 30);

        private void OnDestroy() => m_pool?.Dispose();

        public void PlayVolley(Camera camera, Vector3 owner, float cellWorldSize)
        {
            if (!m_pool.IsReady) return;

            float cell = Mathf.Max(0.5f, cellWorldSize);
            float length = cell * m_arrowLength;
            float height = camera != null && camera.orthographic ? camera.orthographicSize * 2f : 10f;
            for (int wave = 0; wave < m_volleyWaves; wave++)
            {
                for (int i = 0; i < 5; i++) PlayVolleyArrow(owner, length, i, wave * m_volleyWaveGap, height);
            }
        }

        public void PlayRain(Camera camera, IReadOnlyList<Vector3> hitPositions, float cellWorldSize)
        {
            if (camera == null || !m_pool.IsReady) return;

            float cell = Mathf.Max(0.5f, cellWorldSize);
            float length = cell * m_arrowLength;
            float height = camera.orthographic ? camera.orthographicSize * 2f : 10f;
            float width = height * camera.aspect;
            Vector3 center = new Vector3(camera.transform.position.x, camera.transform.position.y, 0f);
            float rainAt = 0.2f;

            PlaySkyDim(center, width, height, rainAt + m_rainSpread + m_fallDuration + m_stayDuration);

            for (int i = 0; i < m_arrowCount; i++)
            {
                Vector3 land = center + new Vector3(Range(-0.5f, 0.5f) * width, Range(-0.5f, 0.5f) * height, 0f);
                ArrowFx.PlayFallingArrow(m_pool, land, length, rainAt + Range(0f, m_rainSpread), m_slant + Range(-5f, 5f),
                    cell * 2.5f, m_fallDuration, m_stayDuration, ArrowFx.Wood, i % 3 == 0);
            }

            if (hitPositions == null) return;
            foreach (Vector3 hit in hitPositions)
            {
                for (int k = 0; k < 2; k++)
                {
                    Vector3 land = hit + new Vector3(Range(-0.2f, 0.2f), Range(-0.15f, 0.15f), 0f) * cell;
                    ArrowFx.PlayFallingArrow(m_pool, land, length, rainAt + Range(0f, m_rainSpread * 0.4f), m_slant + Range(-5f, 5f),
                        cell * 2.5f, m_fallDuration, m_stayDuration, ArrowFx.Wood, true);
                }
            }
        }

        private void PlayVolleyArrow(Vector3 owner, float length, int index, float delay, float travel)
        {
            float angle = 90f + (index - 2) * 10f + Range(-3f, 3f);
            float radians = angle * Mathf.Deg2Rad;
            Vector3 direction = new Vector3(Mathf.Cos(radians), Mathf.Sin(radians), 0f);

            EffectQuad arrow = m_pool.Get();
            arrow.Transform.position = owner;
            arrow.Transform.rotation = Quaternion.Euler(0f, 0f, angle);
            arrow.Transform.localScale = new Vector3(length, length * 0.5f, 1f);
            arrow.SetColor(ArrowFx.Wood);
            arrow.SetFloat(EffectQuad.ShapeId, 8f);
            arrow.SetFloat(EffectQuad.AlphaId, 1f);

            float alpha = 1f;
            Sequence sequence = DOTween.Sequence().SetLink(arrow.GameObject, LinkBehaviour.KillOnDisable);
            arrow.Transform.localScale = Vector3.zero;
            sequence.AppendInterval(delay + index * 0.025f);
            sequence.AppendCallback(() => arrow.Transform.localScale = new Vector3(length, length * 0.5f, 1f));
            sequence.Append(arrow.Transform.DOMove(owner + direction * Mathf.Max(length * 6f, travel), m_volleyDuration).SetEase(Ease.InQuad));
            sequence.Join(DOTween.To(() => alpha, value => { alpha = value; arrow.SetFloat(EffectQuad.AlphaId, value); },
                0f, m_volleyDuration).SetEase(Ease.InCubic));
            sequence.OnKill(() => m_pool.Release(arrow));
        }

        private void PlaySkyDim(Vector3 center, float width, float height, float duration)
        {
            EffectQuad sky = m_pool.Get();
            sky.Transform.position = center;
            sky.Transform.localScale = new Vector3(width * 1.8f, height * 1.8f, 1f);
            sky.SetColor(s_sky);
            sky.SetFloat(EffectQuad.ShapeId, 2f);
            sky.SetFloat(EffectQuad.AlphaId, 0f);
            sky.SetSortingOffset(-20);

            float alpha = 0f;
            Sequence sequence = DOTween.Sequence().SetLink(sky.GameObject, LinkBehaviour.KillOnDisable);
            sequence.Append(DOTween.To(() => alpha, value => { alpha = value; sky.SetFloat(EffectQuad.AlphaId, value); },
                m_skyDarkness, 0.25f).SetEase(Ease.OutQuad));
            sequence.AppendInterval(Mathf.Max(0f, duration - 0.25f));
            sequence.Append(DOTween.To(() => alpha, value => { alpha = value; sky.SetFloat(EffectQuad.AlphaId, value); },
                0f, 0.4f).SetEase(Ease.InQuad));
            sequence.OnKill(() => m_pool.Release(sky));
        }

        private float Range(float min, float max) => Mathf.Lerp(min, max, (float)m_random.NextDouble());
    }
}
