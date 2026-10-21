using System.Collections;
using UnityEngine;

namespace SquareTD
{
    public class EnemyMovement : MonoBehaviour
    {
        [SerializeField] private float m_stepDuration = 0.7f;
        [SerializeField] private EnemyCubeMeshVisual m_cube;
        public int PathIndex { get; private set; }
        private int m_actionCounter;
        private EnemyController m_controller;
        private EnemyCubeOrientation m_orientation;

        private void Awake()
        {
            m_controller = GetComponent<EnemyController>();
        }

        public void Init(int startIndex)
        {
            PathIndex = startIndex;
            m_actionCounter = m_controller.Definition.Action;
            m_orientation = m_cube.RandomizeForSpawn();
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
            yield return StandUpright();
        }

        private IEnumerator MoveOneTile(Vector3 target)
        {
            Vector3 start = transform.position;
            EnemyCubeRollDirection direction = GetRollDirection(target - start);
            EnemyCubeOrientation before = m_orientation;
            float elapsed = 0f;

            while (elapsed < m_stepDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / m_stepDuration);

                m_cube.AnimateTileRoll(before, direction, t, out float travel, out m_orientation);
                transform.position = Vector3.Lerp(start, target, travel);
                yield return null;
            }

            m_cube.AnimateTileRoll(before, direction, 1f, out _, out m_orientation);
            transform.position = target;
            m_cube.ShowOrientation(m_orientation);
        }

        private EnemyCubeRollDirection GetRollDirection(Vector3 move)
        {
            if (Mathf.Abs(move.x) >= Mathf.Abs(move.y))
                return move.x >= 0f ? EnemyCubeRollDirection.Right : EnemyCubeRollDirection.Left;
            return move.y >= 0f ? EnemyCubeRollDirection.Up : EnemyCubeRollDirection.Down;
        }

        private int RollDice()
        {
            EnemyDefinition definition = m_controller.Definition;
            return Random.Range(definition.SpeedMin, definition.SpeedMax + 1);
        }

        public void SetSortingOrder(int order) => m_cube.SetSortingOrder(order);

        private IEnumerator StandUpright()
        {
            while (m_orientation.CounterClockwiseQuarterTurns != 0)
            {
                EnemyCubeOrientation before = m_orientation;
                int direction = before.CounterClockwiseQuarterTurns == 3 ? 1 : -1;

                float elapsed = 0f;
                while (elapsed < m_cube.UprightHopDuration)
                {
                    elapsed += Time.deltaTime;
                    m_cube.AnimateUprightHop(before, direction, elapsed / m_cube.UprightHopDuration);
                    yield return null;
                }

                m_orientation = EnemyCubeOrientation.FromFace(before.VisibleFace,
                                                              before.CounterClockwiseQuarterTurns + direction);
                m_cube.ShowOrientation(m_orientation);
            }
        }

    }
}
