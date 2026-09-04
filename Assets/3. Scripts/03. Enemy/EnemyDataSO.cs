using UnityEngine;

// 열거형(enum) 정의를 함께 배치하여 어디서든 EnemyType을 참조할 수 있도록 구성
public enum EnemyType
{
    Normal,
    Speed,
    Depend,
    MiddleBoss,
    Boss
}

[CreateAssetMenu(fileName = "NewEnemyData", menuName = "Enemy/Enemy Data")]
public class EnemyData : ScriptableObject
{
    public string EnemyID;
    public EnemyType enemyType; // 에셋 자체에서도 타입을 지정할 수 있도록 필드 추가
    public float MaxHP;
    public float Defend;
    public int Speed;
    public int Action;
    public float Resistance;
}
