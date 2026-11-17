using System;
using DG.Tweening;
using UnityEngine;

namespace SquareTD
{
    // 정보 패널과 내용 페이지의 페이드 전환
    public static class CardFlip
    {
        public const float FlipDuration = 0.18f;
        public const float SpinDuration = 0.22f;

        public static Sequence Flip(Transform card, Action swap, float duration = FlipDuration) =>
            Fade(Group(card), Group(card), card, swap, duration);

        public static Sequence ChangePage(Transform owner, GameObject from, GameObject to, Action afterSwap = null, float duration = FlipDuration)
        {
            return Fade(Group(from.transform), Group(to.transform), owner, () =>
            {
                from.SetActive(false);
                to.SetActive(true);
                afterSwap?.Invoke();
            }, duration);
        }

        public static Sequence Spin(Transform from, Transform to, Action swap, float duration = SpinDuration) =>
            Fade(Group(from), Group(to), Group(from), swap, duration);

        private static CanvasGroup Group(Transform target)
        {
            CanvasGroup group = target.GetComponent<CanvasGroup>();
            if (group == null) group = target.gameObject.AddComponent<CanvasGroup>();
            return group;
        }

        private static Sequence Fade(CanvasGroup from, CanvasGroup to, object id, Action swap, float duration)
        {
            DOTween.Kill(id, true);
            bool fromInteractable = from.interactable, toInteractable = to.interactable;
            from.alpha = 1f;
            from.interactable = to.interactable = false;
            duration = Mathf.Max(0.05f, duration);
            void Restore()
            {
                if (from != null) { from.alpha = 1f; from.interactable = fromInteractable; }
                if (to != null) { to.alpha = 1f; to.interactable = toInteractable; }
            }
            return DOTween.Sequence().SetId(id).SetUpdate(true)
                .Append(from.DOFade(0f, duration * 0.4f).SetEase(Ease.InQuad))
                .AppendCallback(() =>
                {
                    swap?.Invoke();
                    if (from != to) from.alpha = 1f;
                    to.alpha = 0f;
                })
                .Append(to.DOFade(1f, duration * 0.6f).SetEase(Ease.OutQuad))
                .OnComplete(Restore).OnKill(Restore);
        }
    }
}
