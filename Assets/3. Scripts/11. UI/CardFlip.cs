using System;
using DG.Tweening;
using UnityEngine;

namespace SquareTD
{
    // 정보 카드 뒤집기 연출 (UI 공용, 2026-10-28) — 세로축으로 돌려 옆모습(90°)이 됐을 때 면을 바꿈
    //   Flip: 한 번 뒤집기 (앞면 ↔ 뒷면). 0° → 90° → (면 바꿈) → −90° → 0°
    //   Spin: 빠르게 여러 번 돌다가 마지막에 다른 카드로 (타워·적 카드 ↔ 타일 카드)
    //   늘 −90° ~ 90° 사이만 보이게 돌림 → UI가 거울처럼 뒤집혀 보이는 일이 없음
    //   UI라서 배속과 상관없이 (SetUpdate(true)). 같은 카드를 다시 뒤집으면 하던 것을 끝까지 돌려놓고 새로
    public static class CardFlip
    {
        public const float FlipDuration = 0.28f;
        public const float SpinDuration = 0.5f;
        public const int SpinTurns = 4;

        // card를 한 번 뒤집음. 옆모습일 때 swap (앞면을 끄고 뒷면을 켜는 일)
        public static Sequence Flip(Transform card, Action swap, float duration = FlipDuration) =>
            Spin(card, card, swap, 1, duration);

        // from 카드가 turns번 돌고, 마지막 반 바퀴 직전에 swap → to 카드가 그 자리에서 멈춤 (from == to면 같은 카드의 면 바꾸기)
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
                sequence.Append(Turn(from, -90f, 90f, 180f * perDegree, Ease.Linear));      // 옆모습 → 앞모습 → 옆모습 (같은 면이 휙 지나감)
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

        // 세로축 각도를 start → end로 (DORotate는 −90 → 90처럼 반 바퀴를 어느 쪽으로 돌지 정하지 못해 각도 값을 직접 돌림)
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
