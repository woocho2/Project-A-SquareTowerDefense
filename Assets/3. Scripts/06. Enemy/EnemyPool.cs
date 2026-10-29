using UnityEngine;
using UnityEngine.Pool;

namespace SquareTD
{
    // 적 오브젝트 풀
    public class EnemyPool : MonoBehaviour
    {
        [SerializeField] private EnemyController m_prefab;
        [SerializeField] private Transform m_container;
        [SerializeField] private int m_defaultCapacity = 40;
        [SerializeField] private int m_maxSize = 200;

        private ObjectPool<EnemyController> m_pool;

        private void Awake()
        {
            m_pool = new ObjectPool<EnemyController>(
                CreateEnemy,
                null,
                OnReleaseEnemy,
                OnDestroyEnemy,
                true,
                m_defaultCapacity,
                m_maxSize);
        }

        public EnemyController Get() => m_pool.Get();
        public void Release(EnemyController enemy) => m_pool.Release(enemy);

        private EnemyController CreateEnemy()
        {
            EnemyController enemy = Instantiate(m_prefab, m_container);
            enemy.gameObject.SetActive(false);
            return enemy;
        }

        private void OnReleaseEnemy(EnemyController enemy)
        {
            enemy.gameObject.SetActive(false);
        }

        private void OnDestroyEnemy(EnemyController enemy)
        {
            Destroy(enemy.gameObject);
        }
    }
}
