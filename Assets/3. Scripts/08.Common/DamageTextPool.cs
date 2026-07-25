using UnityEngine;
using UnityEngine.Pool;
public class DamageTextPool : MonoBehaviour
{
    public static DamageTextPool Instance { get; private set; }

    [Header("Pool Settings")]
    [SerializeField] private DamageText m_damageTextPrefab;
    [SerializeField] private int initialSize = 30;
    [SerializeField] private int maxSize = 200;

    private ObjectPool<DamageText> m_pool;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        m_pool = new ObjectPool<DamageText>(
            createFunc: CreateText,
            actionOnGet: OnGetText,
            actionOnRelease: OnReleaseText,
            actionOnDestroy: OnDestroyText,
            collectionCheck: true,
            defaultCapacity: Mathf.Max(0, initialSize),
            maxSize: Mathf.Max(1, maxSize)
        );
    }

    private DamageText CreateText()
    {
        DamageText text = Instantiate(m_damageTextPrefab, transform);
        text.SetPool(m_pool);
        return text;
    }

    private void OnGetText(DamageText text)
    {
        text.gameObject.SetActive(true);
    }

    private void OnReleaseText(DamageText text)
    {
        text.gameObject.SetActive(false);
    }

    private void OnDestroyText(DamageText text)
    {
        Destroy(text.gameObject);
    }

    public DamageText Spawn(Vector3 position, float damage, bool isCritical = false)
    {
        DamageText text = m_pool.Get();
        text.transform.position = position;
        text.Setup(damage, isCritical);
        return text;
    }
}