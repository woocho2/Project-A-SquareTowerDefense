using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Pool;

public class DamageText : MonoBehaviour
{
    [SerializeField] private TextMeshPro m_textMesh;

    [Header("Floating Settings")]
    [SerializeField] private float m_moveSpeed = 1.5f;
    [SerializeField] private float m_fadeTime = 1.0f;

    private IObjectPool<DamageText> m_managedPool;

    public void SetPool(IObjectPool<DamageText> pool)
    {
        m_managedPool = pool;
    }

    public void Setup(float damageAmount, bool isCritical)
    {
        if (m_textMesh != null)
        {
            m_textMesh.text = damageAmount.ToString("F2");

            if (isCritical)
            {
                Color critColor = Color.red;
                critColor.a = 1f;
                m_textMesh.color = critColor;
            }
            else
            {
                Color normalColor = Color.black;
                normalColor.a = 1f;
                m_textMesh.color = normalColor;
            }
            StartCoroutine(FloatAndFadeRoutine());
        }
    }

    private IEnumerator FloatAndFadeRoutine()
    {
        Color originalColor = m_textMesh.color;
        float timer = 0f;

        while (timer < m_fadeTime)
        {
            transform.Translate(Vector3.up * m_moveSpeed * Time.deltaTime);

            float alpha = Mathf.Lerp(1f, 0f, timer / m_fadeTime);
            m_textMesh.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);

            timer += Time.deltaTime;
            yield return null;
        }

        if (m_managedPool != null)
        {
            m_managedPool.Release(this);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}