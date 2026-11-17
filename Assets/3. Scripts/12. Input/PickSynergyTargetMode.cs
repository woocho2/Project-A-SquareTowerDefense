using System;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace SquareTD
{
    // 로키는 길 두 칸, 뇨르드는 길 한 칸을 고른 뒤 기존 시너지 스킬로 넘김.
    public sealed class PickSynergyTargetMode : IInputMode
    {
        private readonly BoardInputController m_input;
        private readonly SynergyBoard m_board;
        private readonly SynergyBoard.Entry m_entry;
        private readonly Action m_changed;
        private SpriteRenderer m_marker;
        private bool m_finished;

        public int FirstPathIndex { get; private set; } = -1;
        public bool IsActive => !m_finished && m_input != null && m_input.Current == this;

        public PickSynergyTargetMode(BoardInputController input, SynergyBoard board, SynergyBoard.Entry entry, Action changed = null)
        {
            m_input = input;
            m_board = board;
            m_entry = entry;
            m_changed = changed;
        }

        public void Enter()
        {
            m_finished = false;
            Debug.Log(m_entry.Skill.Id == SynergyEffects.LokiId ? "[Synergy] 로키: 서로 바꿀 길 두 칸을 고르세요. 버튼을 다시 누르면 취소합니다." :
                "[Synergy] 뇨르드: 멈춤 칸으로 만들 길을 고르세요. 버튼을 다시 누르면 취소합니다.");
            m_changed?.Invoke();
        }

        public void Exit()
        {
            m_finished = true;
            if (m_marker != null)
            {
                if (Application.isPlaying) UnityEngine.Object.Destroy(m_marker.gameObject);
                else UnityEngine.Object.DestroyImmediate(m_marker.gameObject);
            }
            m_changed?.Invoke();
        }

        public void OnPointerDown(BoardPoint point)
        {
            if (!IsActive) return;
            ITurnState turn = TurnManager.Instance != null ? TurnManager.Instance.Current : null;
            if (!SynergySkillButton.CanActivate(m_entry.Skill, turn, Time.timeScale <= 0f) || !point.IsPath)
            {
                OnCancel();
                return;
            }
            if (m_entry.Skill.Id == SynergyEffects.LokiId && FirstPathIndex < 0)
            {
                FirstPathIndex = point.PathIndex;
                ShowFirstCell();
                m_changed?.Invoke();
                return;
            }
            if (m_entry.Skill.Id == SynergyEffects.LokiId && FirstPathIndex == point.PathIndex) return;
            bool used = m_entry.Skill.Id == SynergyEffects.LokiId
                ? m_board.TryUse(m_entry, FirstPathIndex, point.PathIndex)
                : m_board.TryUse(m_entry, point.PathIndex);
            if (used) m_input.ChangeMode(new IdleMode(m_input));
            m_changed?.Invoke();
        }

        public void OnPointerDrag(Vector3 world) { }
        public void OnPointerUp(BoardPoint point) { }
        public void OnCancel() { if (IsActive) m_input.ChangeMode(new IdleMode(m_input)); }

        private void ShowFirstCell()
        {
            BoardManager board = BoardManager.Instance;
            if (board == null || board.PathTilemap == null) return;
            Tilemap map = board.PathTilemap;
            Vector3Int cell = board.Grid.GetPathCell(FirstPathIndex);
            Sprite sprite = map.GetSprite(cell);
            if (sprite == null) return;
            m_marker = new GameObject("Synergy target A").AddComponent<SpriteRenderer>();
            m_marker.sprite = sprite;
            m_marker.transform.position = map.GetCellCenterWorld(cell);
            m_marker.transform.localScale = new Vector3(1.1f / sprite.bounds.size.x, 1.1f / sprite.bounds.size.y, 1);
            m_marker.color = new Color(1f, 0.83f, 0.25f, 0.4f);
            var renderer = map.GetComponent<TilemapRenderer>();
            if (renderer != null)
            {
                m_marker.sortingLayerID = renderer.sortingLayerID;
                m_marker.sortingOrder = renderer.sortingOrder + 50;
            }
        }
    }
}
