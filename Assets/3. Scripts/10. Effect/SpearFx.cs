using DG.Tweening;
using UnityEngine;

namespace SquareTD
{
    // 창 연출이 같이 쓰는 조각
    public static class SpearFx
    {
        public static readonly Color Steel = new Color(0.85f, 0.9f, 1f);
        public static readonly Color Crimson = new Color(0.95f, 0.25f, 0.22f);
        public const float TipOffset = 0.49f;

        public const float SpearShape = 9f;
        public const float BannerShape = 10f;
        public static readonly Color Banner = new Color(0.85f, 0.15f, 0.15f);

        public static EffectQuad MakeSpear(EffectQuadPool pool, Vector3 position, float angle, Color color, float shape = SpearShape)
        {
            EffectQuad spear = pool.Get();
            spear.Transform.position = position;
            spear.Transform.rotation = Quaternion.Euler(0f, 0f, angle);
            spear.Transform.localScale = Vector3.zero;
            spear.SetColor(color);
            spear.SetFloat(EffectQuad.ShapeId, shape);
            spear.SetFloat(EffectQuad.AlphaId, 1f);
            return spear;
        }

        public static Vector3 Scale(float length) => new Vector3(length, length * 0.4f, 1f);

        public static Vector3 BannerScale(float length, float angle)
        {
            float flip = Mathf.Cos(angle * Mathf.Deg2Rad) < -0.01f ? -1f : 1f;
            return new Vector3(length, length * 0.6f * flip, 1f);
        }

        public static Vector3 CenterForTip(Vector3 point, Vector3 forward, float length, float depth)
            => point - forward * (length * (TipOffset - depth));

        public static void PlaySpark(EffectQuadPool pool, Vector3 position, float size, Color color, float angle, float delay = 0f)
        {
            EffectQuad spark = pool.Get();
            spark.Transform.position = position;
            spark.Transform.rotation = Quaternion.Euler(0f, 0f, angle);
            spark.Transform.localScale = Vector3.zero;
            spark.SetColor(color);
            spark.SetFloat(EffectQuad.ShapeId, 1f);
            spark.SetFloat(EffectQuad.AlphaId, 1f);

            float alpha = 1f;
            Sequence sequence = DOTween.Sequence().SetLink(spark.GameObject, LinkBehaviour.KillOnDisable);
            sequence.AppendInterval(delay);
            sequence.Append(spark.Transform.DOScale(new Vector3(size * 1.4f, size * 0.7f, 1f), 0.05f).SetEase(Ease.OutQuad));
            sequence.Append(DOTween.To(() => alpha, value => { alpha = value; spark.SetFloat(EffectQuad.AlphaId, value); },
                0f, 0.16f).SetEase(Ease.InQuad));
            sequence.OnKill(() => pool.Release(spark));
        }
    }
}
