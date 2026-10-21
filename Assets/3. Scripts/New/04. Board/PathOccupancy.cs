using System;
using System.Collections.Generic;
using UnityEngine;

namespace SquareTD
{
    public class PathOccupancy
    {
        private readonly Dictionary<int, List<EnemyController>> m_cells = new Dictionary<int, List<EnemyController>>();
        private static readonly List<EnemyController> Empty = new List<EnemyController>();

        public event Action<int> CellChanged;

        public void Add(EnemyController enemy, int index)
        {
            GetOrCreateList(index).Add(enemy);
            CellChanged?.Invoke(index);
        }

        public IReadOnlyList<EnemyController> GetAt(int index)
        {
            if (m_cells.TryGetValue(index, out List<EnemyController> list)) return list;
            return Empty;
        }

        public void Remove(EnemyController enemy, int index)
        {
            if (m_cells.TryGetValue(index, out List<EnemyController> list) && list.Remove(enemy))
            {
                CellChanged?.Invoke(index);
                return;
            }
            Debug.LogWarning($"[PathOccupancy]  {index}번 칸에 없는 적: {enemy.name}");
        }

        public void Move(EnemyController enemy, int from, int to)
        {
            if (from == to) return;

            if (!(m_cells.TryGetValue(from, out List<EnemyController> fromlist) && fromlist.Remove(enemy)))
            {
                Debug.LogWarning($"[PathOccupancy] {from}번 칸에 없는 적: {enemy.name}");
                return;
            }

            List<EnemyController> toList = GetOrCreateList(to);
            toList.Add(enemy);            

            CellChanged?.Invoke(from);
            CellChanged?.Invoke(to);
        }

        private List<EnemyController> GetOrCreateList(int index)
        {
            if (!m_cells.TryGetValue(index, out List<EnemyController> list))
            {
                list = new List<EnemyController>();
                m_cells[index] = list;
            }
            return list;
        }
    }
}