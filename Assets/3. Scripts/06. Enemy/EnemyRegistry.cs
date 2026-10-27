using System.Collections.Generic;
using UnityEngine;

namespace SquareTD
{
    public class EnemyRegistry
    {
        private readonly List<EnemyController> m_alive = new List<EnemyController>();
        public IReadOnlyList<EnemyController> Alive => m_alive;
        public int Count => m_alive.Count;
        public void Register(EnemyController enemy)
        {
            if (m_alive.Contains(enemy))
            {
                Debug.LogWarning($"[EnemyRegistry] 이미 등록된 적: {enemy.name}");
                return;
            }
            m_alive.Add(enemy);
        }
        public void Unregister(EnemyController enemy)
        {
            if (!m_alive.Remove(enemy))
            {
                Debug.LogWarning($"[EnemyRegistry] 등록되지 않은 적: {enemy.name}");
            }
        }
    }
}