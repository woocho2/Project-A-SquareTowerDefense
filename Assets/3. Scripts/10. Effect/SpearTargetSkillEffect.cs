using DG.Tweening;
using UnityEngine;

namespace SquareTD
{
    // 창술사(단일 창) 스킬 2 뽑아찢기 연출
    [DisallowMultipleComponent]
    public sealed class SpearTargetSkillEffect : MonoBehaviour
    {
        [SerializeField] private int m_spearCount = 5;
        [SerializeField] private float m_spearLength = 1f;
        [SerializeField] private float m_radius = 1.5f;
        [SerializeField] private float m_appearDuration = 0.15f;
        [SerializeField] private float m_stabInterval = 0.07f;
        [SerializeField] private float m_stabDuration = 0.06f;
        [SerializeField] private float m_holdBeforeRip = 0.25f;
        [SerializeField] private float m_ripDuration = 0.14f;

        private EffectQuadPool m_pool;

        private void Awake() => m_pool = new EffectQuadPool(transform, "SwordQi", "Rend Spear", 26);

        private void OnDestroy() => m_pool?.Dispose();

        public void Play(Vector3 owner, Vector3 target, float cellWorldSize)
        {
            if (!m_pool.IsReady) return;

            float cell = Mathf.Max(0.5f, cellWorldSize);
            float length = cell * m_spearLength;
            int count = Mathf.Max(1, m_spearCount);
            float baseAngle = Mathf.Atan2(target.y - owner.y, target.x - owner.x) * Mathf.Rad2Deg + 180f;
            float lastStab = m_appearDuration + (count - 1) * m_stabInterval + m_stabDuration;
            float ripAt = lastStab + m_holdBeforeRip;

            for (int i = 0; i < count; i++)
            {
                float around = baseAngle + i * (360f / count);
                float radians = around * Mathf.Deg2Rad;
                Vector3 outward = new Vector3(Mathf.Cos(radians), Mathf.Sin(radians), 0f);
                Vector3 inward = -outward;
                float pointAngle = around + 180f;
                Vector3 aimed = target + outward * (cell * m_radius) - inward * (length * SpearFx.TipOffset);
                Vector3 stuck = SpearFx.CenterForTip(target, inward, length, 0.25f);
                Vector3 ripped = stuck + outward * (cell * 1.2f);
                float stabAt = m_appearDuration + i * m_stabInterval;

                EffectQuad spear = SpearFx.MakeSpear(m_pool, aimed, pointAngle, SpearFx.Steel);
                float alpha = 1f;
                Sequence sequence = DOTween.Sequence().SetLink(spear.GameObject, LinkBehaviour.KillOnDisable);
                sequence.Append(spear.Transform.DOScale(SpearFx.Scale(length), m_appearDuration).SetEase(Ease.OutBack));
                sequence.AppendInterval(stabAt - m_appearDuration);
                sequence.Append(spear.Transform.DOMove(stuck, m_stabDuration).SetEase(Ease.InQuad));
                sequence.AppendCallback(() => SpearFx.PlaySpark(m_pool, target, cell * 0.45f, Color.white, pointAngle));
                sequence.AppendInterval(ripAt - stabAt - m_stabDuration);
                sequence.Append(spear.Transform.DOMove(ripped, m_ripDuration).SetEase(Ease.OutCubic));
                sequence.Join(spear.Transform.DORotate(new Vector3(0f, 0f, pointAngle + (i % 2 == 0 ? 25f : -25f)), m_ripDuration));
                sequence.Join(DOTween.To(() => alpha, value => { alpha = value; spear.SetFloat(EffectQuad.AlphaId, value); },
                    0f, m_ripDuration * 1.4f).SetEase(Ease.InQuad));
                sequence.OnKill(() => m_pool.Release(spear));
            }

            DOVirtual.DelayedCall(lastStab, () => ScreenShake.Play(0.03f, m_holdBeforeRip)).SetLink(gameObject, LinkBehaviour.KillOnDisable);
            DOVirtual.DelayedCall(ripAt, () => PlayRend(target, cell)).SetLink(gameObject, LinkBehaviour.KillOnDisable);
        }

        private void PlayRend(Vector3 target, float cell)
        {
            EffectQuad burst = m_pool.Get();
            burst.Transform.position = target;
            burst.Transform.localScale = Vector3.zero;
            burst.SetColor(SpearFx.Crimson);
            burst.SetFloat(EffectQuad.ShapeId, 2f);
            burst.SetFloat(EffectQuad.AlphaId, 0.9f);

            float alpha = 0.9f;
            Sequence sequence = DOTween.Sequence().SetLink(burst.GameObject, LinkBehaviour.KillOnDisable);
            sequence.Append(burst.Transform.DOScale(new Vector3(cell * 1.6f, cell * 1.6f, 1f), 0.12f).SetEase(Ease.OutQuad));
            sequence.Append(DOTween.To(() => alpha, value => { alpha = value; burst.SetFloat(EffectQuad.AlphaId, value); },
                0f, 0.25f).SetEase(Ease.InQuad));
            sequence.OnKill(() => m_pool.Release(burst));

            for (int i = 0; i < 6; i++)
                SpearFx.PlaySpark(m_pool, target + Quaternion.Euler(0f, 0f, i * 60f) * Vector3.right * (cell * 0.35f),
                    cell * 0.35f, SpearFx.Crimson, i * 60f);
            ScreenShake.Play(0.08f, 0.15f);
        }
    }
}
