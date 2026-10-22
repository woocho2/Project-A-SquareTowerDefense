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

        private void Awake()
        {
            m_controller = GetComponent<EnemyController>();
        }

        public void Init(int startIndex)
        {
            transform.localScale = Vector3.one;
            IsMoving = false;
            PathIndex = startIndex;
            m_actionCounter = m_controller.Definition.Action;
            m_cube.RandomizeForSpawn();
        }

        public IEnumerator TakeTurn()
        {
            m_actionCounter--;
            if (m_actionCounter > 0) yield break;

            m_actionCounter = m_controller.Definition.Action;
            yield return MoveSteps(RollDice());
        }

        private IEnumerator MoveSteps(int steps)
        {
            IsMoving = true;
            BoardManager board = BoardManager.Instance;
            int fromIndex = PathIndex;

            for (int i = 0; i < steps; i++)
            {
                int nextIndex = board.Grid.WrapPathIndex(PathIndex + 1);
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

        private int RollDice()
        {
            EnemyDefinition definition = m_controller.Definition;
            return Random.Range(definition.SpeedMin, definition.SpeedMax + 1);
        }

        public void SetSortingOrder(int order) => m_cube.SetSortingOrder(order);
    }
}
