using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace SquareTD
{
    // 자동 합성 연출: 재료가 별 꼬리를 끌며 날아감 → 남는 타워에 빛이 모임 → 번쩍이며 다음 티어로, "TIER UP!"
    [DisallowMultipleComponent]
    public sealed class TowerMergeEffect : MonoBehaviour
    {
        private static readonly Color s_star = new Color(1f, 0.9f, 0.55f);
        private static readonly Color s_smoke = new Color(0.92f, 0.92f, 0.98f);
        private static readonly Color s_light = new Color(1f, 0.86f, 0.45f);
        private static readonly Color s_text = new Color(1f, 0.84f, 0.3f);

        [SerializeField] private float m_flyDuration = 0.75f;
        [SerializeField] private float m_trailInterval = 0.03f;
        [SerializeField] private float m_chargeDuration = 0.5f;
        [SerializeField] private float m_popDuration = 0.3f;
        [SerializeField] private int m_burstCount = 12;
        [SerializeField] private string m_tierUpText = "TIER UP!";
        [SerializeField] private float m_textDuration = 1f;

        private EffectQuadPool m_pool;
        private EffectQuadPool m_glowPool;

        public float FlyDuration => m_flyDuration;
        public float ChargeDuration => m_chargeDuration;

        private void Awake()
        {
            m_pool = new EffectQuadPool(transform, "SwordQi", "Tower Merge", 40);
            m_glowPool = new EffectQuadPool(transform, "SwordQi", "Tower Merge Glow", 41, additive: true);
        }

        private void OnDestroy()
        {
            m_pool?.Dispose();
            m_glowPool?.Dispose();
        }

        public void PlayCharge(TowerController keep)
        {
            if (SimulationRunner.SuppressCosmetics) return;
            if (keep == null) return;
            float cell = EffectSpace.CellWorldSize();
            Vector3 center = keep.transform.position;

            PlayGlow(center, cell * 0.5f, cell * 1.5f, 0f, 0.9f, m_chargeDuration, 0.2f);
            PlayBeam(center, cell, m_chargeDuration);
            keep.transform.DOShakePosition(m_chargeDuration, cell * 0.04f, 30, 90f, false, true).SetLink(keep.gameObject);
            for (int i = 0; i < 6; i++)
            {
                Vector3 from = center + (Vector3)(Random.insideUnitCircle.normalized * cell * 1.1f);
                PlayStarTo(from, center, cell * 0.16f, m_chargeDuration * Random.Range(0.6f, 1f));
            }
        }

        public void PlayGather(IReadOnlyList<TowerController> materials, Vector3 target)
        {
            if (SimulationRunner.SuppressCosmetics) return;
            float cell = EffectSpace.CellWorldSize();
            foreach (TowerController material in materials)
            {
                if (material == null) continue;
                Transform tower = material.transform;
                PlaySmoke(tower.position, cell * 0.9f);

                Sequence sequence = DOTween.Sequence().SetLink(material.gameObject);
                sequence.Append(tower.DOMove(target, m_flyDuration).SetEase(Ease.InCubic));
                sequence.Join(tower.DOScale(tower.localScale * 0.25f, m_flyDuration).SetEase(Ease.InQuad));
                sequence.Join(FadeRenderers(material.gameObject, 0f, m_flyDuration));

                float elapsed = 0f;
                DOTween.To(() => elapsed, value =>
                {
                    float before = elapsed;
                    elapsed = value;
                    if (Mathf.FloorToInt(before / m_trailInterval) != Mathf.FloorToInt(value / m_trailInterval) && tower != null)
                        PlayStar(tower.position + (Vector3)(Random.insideUnitCircle * cell * 0.12f), cell * 0.18f, 0.25f, Vector3.zero);
                }, m_flyDuration, m_flyDuration).SetLink(material.gameObject);
            }
        }

        public void PlayPop(TowerController result)
        {
            if (SimulationRunner.SuppressCosmetics) return;
            if (result == null) return;
            float cell = EffectSpace.CellWorldSize();
            Transform tower = result.transform;
            Vector3 scale = tower.localScale;

            tower.localScale = scale * 0.6f;
            tower.DOScale(scale, m_popDuration).SetEase(Ease.OutBack).SetLink(result.gameObject);

            PlayGlow(tower.position, cell * 1.2f, cell * 2.6f, 1f, 0f, 0.45f, 0f);
            PlaySmoke(tower.position, cell * 1.4f);
            for (int i = 0; i < m_burstCount; i++)
            {
                float angle = 360f * i / m_burstCount * Mathf.Deg2Rad;
                Vector3 direction = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
                PlayStar(tower.position, cell * 0.22f, 0.45f, direction * cell * 0.9f);
            }
            PlayTierUpText(tower.position + Vector3.up * cell * 0.6f, cell);
        }

        private void PlayTierUpText(Vector3 position, float cell)
        {
            var go = new GameObject("Tier Up Text");
            go.transform.SetParent(transform, false);
            go.transform.position = position;
            go.transform.localScale = Vector3.zero;

            TextMeshPro text = go.AddComponent<TextMeshPro>();
            text.text = m_tierUpText;
            text.fontSize = cell * 4.5f;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.color = s_text;
            text.outlineWidth = 0.25f;
            text.outlineColor = new Color32(70, 40, 0, 255);
            text.sortingOrder = 60;
            text.rectTransform.sizeDelta = new Vector2(cell * 4f, cell);

            float alpha = 1f;
            Sequence sequence = DOTween.Sequence().SetLink(go);
            sequence.Append(go.transform.DOScale(Vector3.one, 0.25f).SetEase(Ease.OutBack));
            sequence.Join(go.transform.DOMove(position + Vector3.up * cell * 0.6f, m_textDuration).SetEase(Ease.OutCubic));
            sequence.Insert(m_textDuration - 0.3f, DOTween.To(() => alpha, value => { alpha = value; text.alpha = value; }, 0f, 0.3f));
            sequence.OnComplete(() => Destroy(go));
            EffectActivity.HoldFor(m_textDuration);
        }

        private void PlayGlow(Vector3 position, float fromSize, float toSize, float fromAlpha, float toAlpha, float duration, float tail)
        {
            if (!m_glowPool.IsReady) return;

            EffectQuad glow = m_glowPool.Get();
            glow.Transform.position = position;
            glow.Transform.localScale = new Vector3(fromSize, fromSize, 1f);
            glow.SetColor(s_light);
            glow.SetFloat(EffectQuad.ShapeId, 2f);
            glow.SetFloat(EffectQuad.AlphaId, fromAlpha);

            float alpha = fromAlpha;
            Sequence sequence = DOTween.Sequence().SetLink(glow.GameObject, LinkBehaviour.KillOnDisable);
            sequence.Append(glow.Transform.DOScale(new Vector3(toSize, toSize, 1f), duration).SetEase(Ease.OutCubic));
            sequence.Join(DOTween.To(() => alpha, value => { alpha = value; glow.SetFloat(EffectQuad.AlphaId, value); }, toAlpha, duration));
            if (tail > 0f)
                sequence.Append(DOTween.To(() => alpha, value => { alpha = value; glow.SetFloat(EffectQuad.AlphaId, value); }, 0f, tail));
            sequence.OnKill(() => m_glowPool.Release(glow));
        }

        private void PlayBeam(Vector3 position, float cell, float duration)
        {
            if (!m_glowPool.IsReady) return;

            EffectQuad beam = m_glowPool.Get();
            beam.Transform.position = position;
            beam.Transform.localScale = new Vector3(cell * 0.45f, 0f, 1f);
            beam.SetColor(s_light);
            beam.SetFloat(EffectQuad.ShapeId, 2f);
            beam.SetFloat(EffectQuad.AlphaId, 0f);

            float alpha = 0f;
            Sequence sequence = DOTween.Sequence().SetLink(beam.GameObject, LinkBehaviour.KillOnDisable);
            sequence.Append(beam.Transform.DOScale(new Vector3(cell * 0.45f, cell * 3.2f, 1f), duration).SetEase(Ease.OutCubic));
            sequence.Join(DOTween.To(() => alpha, value => { alpha = value; beam.SetFloat(EffectQuad.AlphaId, value); }, 0.85f, duration));
            sequence.Append(beam.Transform.DOScale(new Vector3(cell * 0.9f, cell * 3.6f, 1f), 0.3f));
            sequence.Join(DOTween.To(() => alpha, value => { alpha = value; beam.SetFloat(EffectQuad.AlphaId, value); }, 0f, 0.3f).SetEase(Ease.InQuad));
            sequence.OnKill(() => m_glowPool.Release(beam));
        }

        private void PlayStarTo(Vector3 from, Vector3 to, float size, float duration)
        {
            if (!m_pool.IsReady) return;

            EffectQuad star = m_pool.Get();
            star.Transform.position = from;
            star.Transform.rotation = Quaternion.Euler(0f, 0f, 45f);
            star.Transform.localScale = new Vector3(size, size, 1f);
            star.SetColor(s_star);
            star.SetFloat(EffectQuad.ShapeId, 1f);
            star.SetFloat(EffectQuad.AlphaId, 1f);

            Sequence sequence = DOTween.Sequence().SetLink(star.GameObject, LinkBehaviour.KillOnDisable);
            sequence.Append(star.Transform.DOMove(to, duration).SetEase(Ease.InCubic));
            sequence.Join(star.Transform.DOScale(new Vector3(size * 0.4f, size * 0.4f, 1f), duration));
            sequence.OnKill(() => m_pool.Release(star));
        }

        private Tween FadeRenderers(GameObject root, float to, float duration)
        {
            SpriteRenderer[] renderers = root.GetComponentsInChildren<SpriteRenderer>();
            float alpha = 1f;
            return DOTween.To(() => alpha, value =>
            {
                alpha = value;
                foreach (SpriteRenderer renderer in renderers)
                {
                    if (renderer == null) continue;
                    Color color = renderer.color;
                    color.a = value;
                    renderer.color = color;
                }
            }, to, duration).SetEase(Ease.InQuad);
        }

        private void PlayStar(Vector3 position, float size, float life, Vector3 drift)
        {
            if (!m_pool.IsReady) return;

            EffectQuad star = m_pool.Get();
            star.Transform.position = position;
            star.Transform.rotation = Quaternion.Euler(0f, 0f, 45f);
            star.Transform.localScale = new Vector3(size, size, 1f);
            star.SetColor(s_star);
            star.SetFloat(EffectQuad.ShapeId, 1f);
            star.SetFloat(EffectQuad.AlphaId, 1f);

            float alpha = 1f;
            Sequence sequence = DOTween.Sequence().SetLink(star.GameObject, LinkBehaviour.KillOnDisable);
            sequence.Append(star.Transform.DOMove(position + drift, life).SetEase(Ease.OutCubic));
            sequence.Join(star.Transform.DOScale(Vector3.zero, life).SetEase(Ease.InQuad));
            sequence.Join(DOTween.To(() => alpha, value => { alpha = value; star.SetFloat(EffectQuad.AlphaId, value); }, 0f, life));
            sequence.OnKill(() => m_pool.Release(star));
        }

        private void PlaySmoke(Vector3 position, float size)
        {
            if (!m_pool.IsReady) return;

            EffectQuad smoke = m_pool.Get();
            smoke.Transform.position = position;
            smoke.Transform.localScale = new Vector3(size * 0.4f, size * 0.4f, 1f);
            smoke.SetColor(s_smoke);
            smoke.SetFloat(EffectQuad.ShapeId, 2f);
            smoke.SetFloat(EffectQuad.AlphaId, 0.6f);
            smoke.SetSortingOffset(-5);

            float alpha = 0.6f;
            Sequence sequence = DOTween.Sequence().SetLink(smoke.GameObject, LinkBehaviour.KillOnDisable);
            sequence.Append(smoke.Transform.DOScale(new Vector3(size, size, 1f), 0.35f).SetEase(Ease.OutCubic));
            sequence.Join(DOTween.To(() => alpha, value => { alpha = value; smoke.SetFloat(EffectQuad.AlphaId, value); }, 0f, 0.35f).SetEase(Ease.InQuad));
            sequence.OnKill(() => m_pool.Release(smoke));
        }
    }
}
