using DG.Tweening;
using UnityEngine;

namespace SquareTD
{
    // 그림 연출 판을 움직이는 공용 동작
    public static class TexturedFx
    {
        public static Sequence Swipe(EffectQuadPool pool, Vector3 position, float angle, Vector3 scale, Color color, float intensity,
            float delay, float draw, float hold, float tail)
        {
            EffectQuad quad = pool.Get();
            quad.Transform.position = position;
            quad.Transform.rotation = Quaternion.Euler(0f, 0f, angle);
            quad.Transform.localScale = scale;
            quad.SetColor(color);
            quad.SetFloat(EffectQuad.IntensityId, intensity);
            quad.SetFloat(EffectQuad.HeadFollowId, 1f);
            quad.SetFloat(EffectQuad.ProgressId, 0f);
            quad.SetFloat(EffectQuad.TailId, 0f);

            float progress = 0f, erased = 0f;
            Sequence sequence = DOTween.Sequence().SetLink(quad.GameObject, LinkBehaviour.KillOnDisable);
            sequence.AppendInterval(delay);
            sequence.Append(DOTween.To(() => progress, v => { progress = v; quad.SetFloat(EffectQuad.ProgressId, v); }, 1f, draw)
                .SetEase(Ease.OutExpo));
            sequence.AppendInterval(hold);
            sequence.Append(DOTween.To(() => erased, v => { erased = v; quad.SetFloat(EffectQuad.TailId, v); }, 1f, tail)
                .SetEase(Ease.InQuad));
            sequence.OnKill(() => pool.Release(quad));
            return sequence;
        }

        public static Sequence Pop(EffectQuadPool pool, Vector3 position, float angle, float from, float to, Color color, float intensity,
            float delay, float grow, float fade, Ease growEase = Ease.OutQuad)
        {
            EffectQuad quad = pool.Get();
            quad.Transform.position = position;
            quad.Transform.rotation = Quaternion.Euler(0f, 0f, angle);
            quad.Transform.localScale = Vector3.zero;
            quad.SetColor(color);
            quad.SetFloat(EffectQuad.IntensityId, intensity);

            float alpha = 1f;
            Sequence sequence = DOTween.Sequence().SetLink(quad.GameObject, LinkBehaviour.KillOnDisable);
            sequence.AppendInterval(delay);
            sequence.AppendCallback(() => quad.Transform.localScale = new Vector3(from, from, 1f));
            sequence.Append(quad.Transform.DOScale(new Vector3(to, to, 1f), grow).SetEase(growEase));
            sequence.Insert(delay + Mathf.Max(0f, grow - fade * 0.5f),
                DOTween.To(() => alpha, v => { alpha = v; quad.SetFloat(EffectQuad.AlphaId, v); }, 0f, fade).SetEase(Ease.InQuad));
            sequence.OnKill(() => pool.Release(quad));
            return sequence;
        }

        public static Tween DissolveOut(EffectQuad quad, float duration)
        {
            float amount = 0f;
            return DOTween.To(() => amount, v => { amount = v; quad.SetFloat(EffectQuad.DissolveId, v); }, 1f, duration)
                .SetEase(Ease.InQuad).SetLink(quad.GameObject, LinkBehaviour.KillOnDisable);
        }
    }
}
