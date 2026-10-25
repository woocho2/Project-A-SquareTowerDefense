using System.Collections;
using DG.Tweening;
using UnityEngine;

namespace SquareTD
{
    public class EnemyMovement : MonoBehaviour
    {
        [SerializeField] private float m_stepDuration = 0.7f;
        [SerializeField] private float m_landDuration = 0.1f;
        [SerializeField] private EnemyCubeMeshVisual m_cube;
        public int PathIndex { get; private set; }
        public bool IsMoving { get; private set; }
        private int m_actionCounter;
        private EnemyController m_controller;
        private void ApplyFace() => EnemyCubeFaces.Apply(m_controller.Stats, m_cube.GetFaceAsset(m_cube.VisibleFace));

        private void Awake()
        {
            m_controller = GetComponent<EnemyController>();
        }

        public void Init(int startIndex)
        {
            transform.localScale = Vector3.one;
            IsMoving = false;
            PathIndex = startIndex;            
            m_actionCounter = m_controller.Stats.GetMoveRange().Action;
            m_cube.RandomizeForSpawn();
            ApplyFace();
        }

        public IEnumerator TakeTurn()
        {
            EnemyStats stats = m_controller.Stats;            
            m_actionCounter -= 1 + Mathf.RoundToInt(stats.Get(StatType.ActionGainPerTurn));
            if (m_actionCounter > 0) yield break;

            MoveRange range = DecideMove(stats.GetMoveRange());
            m_actionCounter = range.Action;

            int steps = RollDice(range);
            yield return MoveSteps(steps);
            ApplyFace();
            OnMoved(steps);
        }

        // 수업 25: 상태 효과의 "다음 이동을 정할 때" 훅 (행동 불가 0 / 0, 역풍 최소 = −최대, 급류 뒤로 1칸). 지금은 받은 그대로
        private MoveRange DecideMove(MoveRange range) => range;

        // 수업 25: 상태 효과의 "이동한 뒤" 훅 (난기류 칸 수 × 피해, 프리즘 감소). 지금은 빈 자리
        private void OnMoved(int steps) { }

        private IEnumerator MoveSteps(int steps)
        {
            IsMoving = true;
            BoardManager board = BoardManager.Instance;
            int fromIndex = PathIndex;            
            int direction = steps >= 0 ? 1 : -1;            
            int count = Mathf.Abs(steps);

            for (int i = 0; i < count; i++)
            {
                int nextIndex = board.Grid.WrapPathIndex(PathIndex + direction);
                Vector3 target = board.GetWorldPosition(board.Grid.GetPathCell(nextIndex));
                yield return MoveOneTile(target);
                PathIndex = nextIndex;
            }

            board.Occupancy.Move(m_controller, fromIndex, PathIndex);

            Tween upright = m_cube.StandUpright();
            if (upright != null)
            {
                yield return upright.WaitForCompletion();
                m_cube.SnapRotation();
            }
            IsMoving = false;
        }

        private IEnumerator MoveOneTile(Vector3 target)
        {
            Vector3 direction = GetRollDirection(target - transform.position);

            Sequence roll = DOTween.Sequence();
            roll.Append(transform.DOMove(target, m_stepDuration).SetEase(Ease.InOutSine));
            roll.Join(m_cube.Roll(direction, m_stepDuration));
            roll.Append(transform.DOPunchScale(new Vector3(0.1f, -0.1f, 0f), m_landDuration, 1, 0f));
            roll.SetLink(gameObject, LinkBehaviour.KillOnDisable);

            yield return roll.WaitForCompletion();
            m_cube.SnapRotation();
        }

        private Vector3 GetRollDirection(Vector3 move)
        {
            if (Mathf.Abs(move.x) >= Mathf.Abs(move.y))
                return move.x >= 0f ? Vector3.right : Vector3.left;
            return move.y >= 0f ? Vector3.up : Vector3.down;
        }

        private int RollDice(MoveRange range)
        {            
            return Random.Range(range.Min, range.Max + 1);
        }

        public void SetSortingOrder(int order) => m_cube.SetSortingOrder(order);
    }
}
