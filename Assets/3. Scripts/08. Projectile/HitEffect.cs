using System;
using DG.Tweening;
using UnityEngine;

namespace SquareTD
{
    // 탄이 맞은 자리의 이펙트
    public class HitEffect : MonoBehaviour
    {
        [SerializeField] private float m_safetyLifetime = 2f;

        private Action<HitEffect> m_onFinished;

        public void Play(Action<HitEffect> onFinished)
        {
            m_onFinished = onFinished;
            DOVirtual.DelayedCall(m_safetyLifetime, DestroyEffect)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable);
        }

        public void DestroyEffect()
        {
            if (m_onFinished == null) return;
            Action<HitEffect> finished = m_onFinished;
            m_onFinished = null;
            finished(this);
        }
    }
}
