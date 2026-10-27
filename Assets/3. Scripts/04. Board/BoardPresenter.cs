using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace SquareTD
{
    public class BoardPresenter : MonoBehaviour
    {
        [SerializeField] TextMeshPro m_labelPrefab;
        [SerializeField] Vector3 m_labelOffset;
        private readonly Dictionary<int, TextMeshPro> m_labels = new Dictionary<int, TextMeshPro>();

        private void Start()
        {
            BoardManager.Instance.Occupancy.CellChanged += OnCellChanged;
        }

        private void OnDestroy()
        {
            if (BoardManager.Instance != null)
            {
                BoardManager.Instance.Occupancy.CellChanged -= OnCellChanged;
            }
        }

        private void OnCellChanged(int index)
        {
            IReadOnlyList<ITarget> targets = BoardManager.Instance.Occupancy.GetAt(index);
            for (int i = 0; i < targets.Count; i++)
            {
                targets[i].SetSortingOrder((i + 1) * 2);
            }

            int count = targets.Count;
            TextMeshPro label = GetOrCreateLabel(index);

            if (count == 0)
            {
                label.gameObject.SetActive(false);
                return;
            }

            label.text = $"x{count}";
            label.gameObject.SetActive(true);
        }

        private TextMeshPro GetOrCreateLabel(int index)
        {
            if (!m_labels.TryGetValue(index, out TextMeshPro label))
            {
                label = Instantiate(m_labelPrefab, transform);

                BoardManager board = BoardManager.Instance;
                Vector3 cellCenter = board.GetWorldPosition(board.Grid.GetPathCell(index));
                label.transform.position = cellCenter + m_labelOffset;

                m_labels[index] = label;
            }

            return label;
        }
    }
}