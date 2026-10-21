using UnityEngine;

namespace SquareTD
{
    public class EnemyController : MonoBehaviour
    {
        public EnemyDefinition Definition { get; private set; }
        public EnemyMovement Movement { get; private set; }

        public float MaxHp { get; private set; }
        public float Defense { get; private set; }

        private void Awake()
        {
            Movement = GetComponent<EnemyMovement>();
        }

        public void Setup(EnemyDefinition definition, float hpMultiplier, float defenseMultiplier)
        {
            Definition = definition;
            MaxHp = definition.MaxHP * hpMultiplier;
            Defense = definition.Defense * defenseMultiplier;
        }        
    }
}
