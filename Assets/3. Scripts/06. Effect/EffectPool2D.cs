using UnityEngine;
using System.Collections;
using UnityEngine.Pool;

public class EffectPool2D : MonoBehaviour
{
    [SerializeField] ParticleSystem m_particleSystem;

    private Coroutine m_running;

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
    }

    private void OnEnable()
    {
        if (m_particleSystem != null)
        {
            m_particleSystem.Clear();
            m_particleSystem.Play();

            if (m_running != null)
            {
                StopCoroutine(m_running);
            }

            m_running = StartCoroutine(AutoOff());
        }
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
        else
        {
            yield return null;
        }

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

    public void ManualReturnToPool()
    {
        if (m_pool != null)
        {
            m_pool.Release(gameObject);
        }
        else
        {
            if (m_destroyOnFinish) Destroy(gameObject);
            gameObject.SetActive(false);
        }
    }
}
