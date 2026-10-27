using System;
using DG.Tweening;
using UnityEngine;

namespace SquareTD
{
    // 활 연출이 같이 쓰는 조각 (레인저의 화살비, 헌터의 화살)
    //   화살 판은 SwordQi 셰이더의 모양 8: 오른쪽(+X)이 촉. 판 가로 = 화살 길이
    public static class ArrowFx
    {
        public static readonly Color Wood = new Color(0.92f, 0.84f, 0.7f);
        private static readonly Color s_dust = new Color(0.78f, 0.68f, 0.56f);
        private const float TipOffset = 0.475f;     // 판 가운데에서 촉 끝까지 (판 가로에 대한 비율)

        // 하늘에서 비스듬히 떨어져 땅에 꽂히는 화살 하나: delay 뒤 나타나 fall초 동안 떨어짐 → 촉이 땅 아래로 들어가 지워짐 → stay초 뒤 흐려짐
        //   slant: 수직(아래)에서 기운 각도. onLand: 꽂히는 순간
        public static void PlayFallingArrow(EffectQuadPool pool, Vector3 land, float length, float delay, float slant,
            float fallHeight, float fall, float stay, Color color, bool dust, Action onLand = null)
        {
            float angle = -90f + slant;
            float radians = angle * Mathf.Deg2Rad;
            Vector3 direction = new Vector3(Mathf.Cos(radians), Mathf.Sin(radians), 0f);
            Vector3 end = land - direction * (length * (TipOffset - 0.14f));        // 촉이 땅선 아래로 조금 들어간 자리
            Vector3 start = end - direction * fallHeight;
            Vector3 scale = new Vector3(length, length * 0.5f, 1f);

            EffectQuad arrow = pool.Get();
            arrow.Transform.position = start;
            arrow.Transform.rotation = Quaternion.Euler(0f, 0f, angle);
            arrow.Transform.localScale = Vector3.zero;
            arrow.SetColor(color);
            arrow.SetFloat(EffectQuad.ShapeId, 8f);
            arrow.SetFloat(EffectQuad.AlphaId, 1f);

            float alpha = 1f;
            Sequence sequence = DOTween.Sequence().SetLink(arrow.GameObject, LinkBehaviour.KillOnDisable);
            sequence.AppendInterval(delay);
            sequence.AppendCallback(() => arrow.Transform.localScale = scale);
            sequence.Append(arrow.Transform.DOMove(end, fall).SetEase(Ease.InQuad));
            sequence.AppendCallback(() =>
            {
                arrow.SetFloat(EffectQuad.ClipBelowId, land.y);                  // 땅 아래는 안 보임
                if (dust) PlayDust(pool, land, length * 0.6f);
                onLand?.Invoke();
            });
            sequence.Append(arrow.Transform.DOPunchRotation(new Vector3(0f, 0f, 6f), 0.12f, 8, 0.5f));      // 꽂히며 파르르
            sequence.AppendInterval(stay);
            sequence.Append(DOTween.To(() => alpha, value => { alpha = value; arrow.SetFloat(EffectQuad.AlphaId, value); },
                0f, 0.2f).SetEase(Ease.InQuad));
            sequence.OnKill(() => pool.Release(arrow));
        }

        // 화살이 꽂힌 자리의 작은 흙먼지
        public static void PlayDust(EffectQuadPool pool, Vector3 position, float size)
        {
            EffectQuad dust = pool.Get();
            dust.Transform.position = position;
            dust.Transform.localScale = Vector3.zero;
            dust.SetColor(s_dust);
            dust.SetFloat(EffectQuad.ShapeId, 2f);
            dust.SetFloat(EffectQuad.AlphaId, 0.45f);
            dust.SetSortingOffset(-5);

            float alpha = 0.45f;
            Sequence sequence = DOTween.Sequence().SetLink(dust.GameObject, LinkBehaviour.KillOnDisable);
            sequence.Append(dust.Transform.DOScale(new Vector3(size, size * 0.6f, 1f), 0.28f).SetEase(Ease.OutCubic));
            sequence.Join(DOTween.To(() => alpha, value => { alpha = value; dust.SetFloat(EffectQuad.AlphaId, value); },
                0f, 0.28f).SetEase(Ease.InQuad));
            sequence.OnKill(() => pool.Release(dust));
        }

        // 작은 반짝 (화살이 맞은 자리)
        public static void PlaySpark(EffectQuadPool pool, Vector3 position, float size, Color color)
        {
            EffectQuad spark = pool.Get();
            spark.Transform.position = position;
            spark.Transform.rotation = Quaternion.Euler(0f, 0f, 45f);
            spark.Transform.localScale = Vector3.zero;
            spark.SetColor(color);
            spark.SetFloat(EffectQuad.ShapeId, 1f);
            spark.SetFloat(EffectQuad.AlphaId, 1f);

            float alpha = 1f;
            Sequence sequence = DOTween.Sequence().SetLink(spark.GameObject, LinkBehaviour.KillOnDisable);
            sequence.Append(spark.Transform.DOScale(new Vector3(size, size, 1f), 0.05f).SetEase(Ease.OutQuad));
            sequence.Append(DOTween.To(() => alpha, value => { alpha = value; spark.SetFloat(EffectQuad.AlphaId, value); },
                0f, 0.15f).SetEase(Ease.InQuad));
            sequence.OnKill(() => pool.Release(spark));
        }
    }
}
