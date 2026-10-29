using UnityEngine;

namespace SquareTD
{
    // 적 한 종류의 기본 데이터 (체력, 방어, 이동력 등)
    [CreateAssetMenu(fileName = "NewEnemy", menuName = "SquareTD/Enemy Definition")]
    public class EnemyDefinition : ScriptableObject
    {
        [field: SerializeField] public string DisplayName { get; private set; }
        [field: SerializeField] public EnemyCubeAppearance Appearance { get; private set; }
        [field: SerializeField] public EnemyKind Kind { get; private set; }
        [field: SerializeField] public float MaxHP { get; private set; }
        [field: SerializeField] public float Defense { get; private set; }
        [field: SerializeField] public int Action { get; private set; }
        [field: SerializeField] public int SpeedMin { get; private set; }
        [field: SerializeField] public int SpeedMax { get; private set; }
        [field: SerializeField] public float BaseResistance { get; private set; }
    }
}
