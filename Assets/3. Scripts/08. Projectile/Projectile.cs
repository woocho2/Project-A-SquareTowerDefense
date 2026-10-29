using DG.Tweening;
using UnityEngine;

namespace SquareTD
{
    // 날아가는 탄 하나
    public class Projectile : MonoBehaviour
    {
        private static readonly int IsFlying = Animator.StringToHash("IsFlying");

        [SerializeField] private SpriteRenderer m_sprite;
        [SerializeField] private Animator m_animator;
        [SerializeField] private ProjectileVfx m_vfx;

        public void ApplyLook(ProjectileCatalog.Entry look)
        {
            transform.rotation = Quaternion.identity;
            m_sprite.sprite = look.Sprite;
            m_animator.runtimeAnimatorController = look.Animation;
            m_vfx.SetColor(look.EffectColor);
            m_vfx.Stop();
        }

        public Tween FlyTo(Vector3 destination, float speed, float hold)
        {
            Vector3 direction = destination - transform.position;
            float duration = direction.magnitude / Mathf.Max(0.01f, speed);

            Sequence sequence = DOTween.Sequence();
            sequence.AppendInterval(hold);
            sequence.AppendCallback(() => Launch(direction));
            sequence.Append(transform.DOMove(destination, duration).SetEase(Ease.Linear));
            sequence.SetLink(gameObject, LinkBehaviour.KillOnDisable);
            return sequence;
        }

        private void Launch(Vector3 direction)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
            m_animator.SetBool(IsFlying, true);
            m_vfx.Play();
        }
    }
}
