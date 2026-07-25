using UnityEngine;

public class EffectController : MonoBehaviour
{
    private EffectPool2D m_effectPool;

    public void Awake()
    {
        m_effectPool = GetComponent<EffectPool2D>();
    }

    public void DestroyEffect()
    {
        if (m_effectPool != null)
        {
            m_effectPool.ManualReturnToPool();
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
