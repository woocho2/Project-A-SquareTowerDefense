using UnityEngine;

namespace SquareTD
{
    // 씬에 하나만 있는 매니저의 공용 베이스 (Instance)
    public abstract class Singleton<T> : MonoBehaviour
    where T : MonoBehaviour
    {
        public static T Instance { get; private set; }

        protected virtual void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this as T;
        }

        protected virtual void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}
