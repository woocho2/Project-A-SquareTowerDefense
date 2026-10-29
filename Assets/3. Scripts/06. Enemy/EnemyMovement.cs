using System.Collections;
using DG.Tweening;
using UnityEngine;

namespace SquareTD
{
    // 적의 이동 (행동 카운트, 주사위, 칸 이동)
    public class EnemyMovement : MonoBehaviour
    {
        [SerializeField] private float m_stepDuration = 0.7f;
        [SerializeField] private float m_landDuration = 0.1f;
        [SerializeField] private EnemyCubeMeshVisual m_cube;
        public int PathIndex { get; private set; }
        public bool IsMoving { get; private set; }
        public bool ActedThisTurn { get; private set; }
        public int ActionStartIndex { get; private set; } = -1;
        public int ActionCounter => m_actionCounter;
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
            ActedThisTurn = false;
            ActionStartIndex = -1;
            PathIndex = startIndex;
            m_actionCounter = m_controller.Stats.GetMoveRange().Action;
            m_cube.RandomizeForSpawn();
            ApplyFace();
        }

        public IEnumerator TakeTurn()
        {
            EnemyStats stats = m_controller.Stats;
            ActedThisTurn = false;
            m_actionCounter -= 1 + Mathf.RoundToInt(stats.Get(StatType.ActionGainPerTurn));
            if (m_actionCounter > 0) yield break;

            ActedThisTurn = true;
            ActionStartIndex = PathIndex;
            MoveRange range = DecideMove(stats.GetMoveRange());
            m_actionCounter = range.Action;

            int steps = RollDice(range);
            yield return MoveSteps(steps);
            ApplyFace();
            OnMoved(steps);

            m_controller.Status.OnActed();
            m_controller.Resistance.OnActed();
        }

        private MoveRange DecideMove(MoveRange range)
        {
            BoardManager board = BoardManager.Instance;
            if (board != null && board.Grounds != null) range = board.Grounds.DecideMove(m_controller, range);
            return m_controller.Status.DecideMove(range);
        }

        private void OnMoved(int steps) => m_controller.Status.OnMoved(steps);

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

        public void AddActionProgress(int amount) => m_actionCounter -= amount;

        public void TeleportTo(int pathIndex)
        {
            BoardManager board = BoardManager.Instance;
            int fromIndex = PathIndex;
            PathIndex = board.Grid.WrapPathIndex(pathIndex);
            transform.position = board.GetWorldPosition(board.Grid.GetPathCell(PathIndex));
            board.Occupancy.Move(m_controller, fromIndex, PathIndex);
        }

        public void SetSortingOrder(int order) => m_cube.SetSortingOrder(order);
        public void SetAppearance(EnemyCubeAppearance appearance) => m_cube.SetAppearance(appearance);
    }
}
