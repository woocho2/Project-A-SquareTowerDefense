using System;
using System.Collections.Generic;
using UnityEngine;

namespace SquareTD
{
    public class PathOccupancy
    {
        private readonly Dictionary<int, List<ITarget>> m_cells = new Dictionary<int, List<ITarget>>();
        private static readonly List<ITarget> Empty = new List<ITarget>();

        public event Action<int> CellChanged;

        public void Add(ITarget target, int index)
        {
            GetOrCreateList(index).Add(target);
            CellChanged?.Invoke(index);
        }

        public void Remove(ITarget target, int index)
        {
            if (m_cells.TryGetValue(index, out List<ITarget> list) && list.Remove(target))
            {
                CellChanged?.Invoke(index);
                return;
            }
            Debug.LogWarning($"[PathOccupancy]  {index}번 칸에 없는 적: {target.Transform.name}");
        }

        public IReadOnlyList<ITarget> GetAt(int index)
        {
            if (m_cells.TryGetValue(index, out List<ITarget> list)) return list;
            return Empty;
        }

        public void Move(ITarget target, int from, int to)
        {
            if (from == to) return;

            if (!(m_cells.TryGetValue(from, out List<ITarget> fromlist) && fromlist.Remove(target)))
            {
                Debug.LogWarning($"[PathOccupancy] {from}번 칸에 없는 적: {target.Transform.name}");
                return;
            }

            List<ITarget> toList = GetOrCreateList(to);
            toList.Add(target);

            CellChanged?.Invoke(from);
            CellChanged?.Invoke(to);
        }

        private List<ITarget> GetOrCreateList(int index)
        {
            if (!m_cells.TryGetValue(index, out List<ITarget> list))
            {
                list = new List<ITarget>();
                m_cells[index] = list;
            }
            return list;
        }
    }
}