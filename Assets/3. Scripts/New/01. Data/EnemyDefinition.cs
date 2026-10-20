using UnityEngine;

namespace SquareTD
{
    [CreateAssetMenu(fileName = "NewEnemy", menuName = "SquareTD/Enemy Definition")]
    public class EnemyDefinition : ScriptableObject
    {
        [field: SerializeField] public string DisplayName { get; private set; }
        [field: SerializeField] public EnemyKind Kind { get; private set; }
        [field: SerializeField] public float MaxHP { get; private set; }
        [field: SerializeField] public float Defense { get; private set; }
        [field: SerializeField] public int Action { get; private set; }
        [field: SerializeField] public int SpeedMin { get; private set; }
        [field: SerializeField] public int SpeedMax { get; private set; }
        [field: SerializeField] public float BaseResistance { get; private set; }
    }
}