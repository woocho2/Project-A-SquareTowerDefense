using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Pool;

namespace SquareTD
{
    public class EnemyPool : MonoBehaviour
    {
        [SerializeField] private EnemyController m_prefab;     // 찍어 낼 적 프리팹
        [SerializeField] private Transform m_container;         // 만든 적을 넣어 둘 부모 (하이어라키 정리용)
        [SerializeField] private int m_defaultCapacity = 40;    // 처음에 잡아 둘 크기
        [SerializeField] private int m_maxSize = 200;           // 돌려받아 보관할 최대 수

        private ObjectPool<EnemyController> m_pool;

        private void Awake()
        {
            m_pool = new ObjectPool<EnemyController>(
                CreateEnemy,        // 만들기: 꺼낼 게 없을 때 새로 하나
                null,               // 꺼낼 때 할 일 없음 (켜는 건 spawner가)
                OnReleaseEnemy,     // 돌려놓을 때
                OnDestroyEnemy,     // 버릴 때 (maxSize를 넘어 보관 못 할 때)
                true,               // 같은 적을 두 번 돌려놓으면 에러로 알려 줌
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

        // ── 수업 5 확인용 임시 코드. 수업 7에서 지울 것 ──
        private readonly List<EnemyController> m_testTaken = new List<EnemyController>();

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (keyboard.kKey.wasPressedThisFrame)            // K: 40마리 꺼내기
            {
                for (int i = 0; i < 40; i++) m_testTaken.Add(Get());
                Debug.Log($"[EnemyPool] 40마리 꺼냄 / 풀이 만든 수 {m_pool.CountAll}");
            }

            if (keyboard.lKey.wasPressedThisFrame)            // L: 전부 돌려놓기
            {
                foreach (EnemyController enemy in m_testTaken) Release(enemy);
                m_testTaken.Clear();
                Debug.Log($"[EnemyPool] 전부 돌려놓음 / 보관 중 {m_pool.CountInactive}");
            }
        }
    }
}
