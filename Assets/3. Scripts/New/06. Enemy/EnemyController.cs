using UnityEngine;

namespace SquareTD
{
    public class EnemyController : MonoBehaviour
    {
        public EnemyDefinition Definition { get; private set; }

        public float MaxHp { get; private set; }
        public float Defense { get; private set; }

        public void Setup(EnemyDefinition definition, float hpMultiplier, float defenseMultiplier)
        {
            Definition = definition;
            MaxHp = definition.MaxHP * hpMultiplier;
            Defense = definition.Defense * defenseMultiplier;
        }

        // ── 수업 4 확인용 임시 코드. 수업 7(EnemySpawner)이 생기면 지울 것 ──
        [SerializeField] private EnemyDefinition m_testDefinition;

        private void Start()
        {
            if (m_testDefinition == null) return;

            Setup(m_testDefinition, 1f, 1f);

            Vector3Int startCell = BoardManager.Instance.Grid.GetPathCell(0);               // 빈칸 3: 번호 → 좌표 함수 (수업 3에서 만든 것)
            transform.position = BoardManager.Instance.GetWorldPosition(startCell);

            Debug.Log($"[EnemyController] {Definition.DisplayName} 체력 {MaxHp}, 0번 칸 {startCell}");
        }
    }
}
