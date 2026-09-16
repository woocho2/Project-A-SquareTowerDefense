using UnityEngine;
using System.Collections;
using UnityEngine.Pool;

public class EffectPool2D : MonoBehaviour
{
    [SerializeField] ParticleSystem m_particleSystem;
    [SerializeField] private Animator m_animator;
    [Tooltip("파티클과 애니메이터가 모두 없을 때 이펙트를 자동 회수하는 시간입니다.")]
    [SerializeField, Min(0.1f)] private float m_fallbackLifetime = 2f;

    private Coroutine m_running;
    private bool m_isReturning;

    private bool m_destroyOnFinish = true;

    private IObjectPool<GameObject> m_pool;

    public void SetPool(IObjectPool<GameObject> pool) => m_pool = pool;
    private void Awake()
    {
        if (m_particleSystem == null)
        {
            if (TryGetComponent<ParticleSystem>(out ParticleSystem ps))
            {
                m_particleSystem = ps;
            }
        }

        if (m_animator == null)
        {
            TryGetComponent(out m_animator);
        }
    }

    private void OnEnable()
    {
        m_isReturning = false;

        if (m_particleSystem != null)
        {
            m_particleSystem.Clear();
            m_particleSystem.Play();
        }

        if (m_animator != null)
        {
            // 풀에서 재사용될 때 항상 애니메이션 첫 프레임부터 시작합니다.
            m_animator.Rebind();
            m_animator.Update(0f);
        }

        if (m_running != null)
        {
            StopCoroutine(m_running);
        }

        // 애니메이션 이벤트가 누락되어도 반드시 풀로 돌아가도록 모든 이펙트에 안전 회수를 적용합니다.
        m_running = StartCoroutine(AutoOff());
    }

    private void OnDisable()
    {
        if (m_running != null)
        {
            StopCoroutine(m_running);
            m_running = null;
        }
    }

    public void SetDestoryOnFinish(bool destroyOnFinish)
    {
        m_destroyOnFinish = destroyOnFinish;
    }

    IEnumerator AutoOff()
    {
        if (m_particleSystem != null)
        {
            yield return new WaitUntil(() => m_particleSystem.IsAlive(true) == false);
        }
        else if (m_animator != null)
        {
            // Animator가 현재 클립 정보를 갱신할 한 프레임을 기다립니다.
            yield return null;

            float animationLifetime = m_fallbackLifetime;
            AnimatorClipInfo[] clips = m_animator.GetCurrentAnimatorClipInfo(0);
            if (clips.Length > 0 && clips[0].clip != null)
            {
                float animatorSpeed = Mathf.Max(0.01f, Mathf.Abs(m_animator.speed));
                animationLifetime = clips[0].clip.length / animatorSpeed + 0.1f;
            }

            yield return new WaitForSeconds(animationLifetime);
        }
        else
        {
            yield return new WaitForSeconds(m_fallbackLifetime);
        }

        ReturnEffect();
    }

    public void ManualReturnToPool()
    {
        ReturnEffect();
    }

    private void ReturnEffect()
    {
        // 애니메이션 이벤트와 안전 코루틴이 같은 프레임에 끝나도 중복 Release하지 않습니다.
        if (m_isReturning) return;
        m_isReturning = true;

        if (m_pool != null)
        {
            m_pool.Release(gameObject);
        }
        else if (m_destroyOnFinish)
        {
            Destroy(gameObject);
        }
        else
        {
            gameObject.SetActive(false);
        }

        m_running = null;
    }
}
