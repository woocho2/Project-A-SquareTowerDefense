using System;
using DG.Tweening;
using UnityEngine;

namespace SquareTD
{
    // 정보 카드 뒤집기 연출
    public static class CardFlip
    {
        public const float FlipDuration = 0.28f;
        public const float SpinDuration = 0.5f;
        public const int SpinTurns = 4;

        public static Sequence Flip(Transform card, Action swap, float duration = FlipDuration) =>
            Spin(card, card, swap, 1, duration);

        public static Sequence Spin(Transform from, Transform to, Action swap, int turns = SpinTurns, float duration = SpinDuration)
        {
            DOTween.Kill(from, true);
            if (to != from) DOTween.Kill(to, true);
            turns = Mathf.Max(1, turns);
            float perDegree = duration / (turns * 180f);

            Sequence sequence = DOTween.Sequence().SetId(from).SetUpdate(true);
            sequence.Append(Turn(from, 0f, 90f, 90f * perDegree, turns == 1 ? Ease.InQuad : Ease.InSine));
            for (int i = 1; i < turns; i++)
            {
                sequence.Append(Turn(from, -90f, 90f, 180f * perDegree, Ease.Linear));
            }
            sequence.AppendCallback(() =>
            {
                from.localRotation = Quaternion.identity;
                swap?.Invoke();
                to.localRotation = Quaternion.Euler(0f, -90f, 0f);
            });
            sequence.Append(Turn(to, -90f, 0f, 90f * perDegree, turns == 1 ? Ease.OutQuad : Ease.OutBack));
            sequence.OnKill(() =>
            {
                if (from != null) from.localRotation = Quaternion.identity;
                if (to != null) to.localRotation = Quaternion.identity;
            });
            return sequence;
        }

        private static Tween Turn(Transform card, float start, float end, float duration, Ease ease)
        {
            float angle = start;
            return DOTween.To(() => angle, value =>
            {
                angle = value;
                if (card != null) card.localRotation = Quaternion.Euler(0f, value, 0f);
            }, end, duration).SetEase(ease).OnStart(() => angle = start);
        }
    }
}
