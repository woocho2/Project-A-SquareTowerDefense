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

    public string DisplayName => enemyType switch
    {
        EnemyType.Normal => "일반 몬스터",
        EnemyType.Speed => "스피드 몬스터",
        EnemyType.Depend => "방어형 몬스터",
        EnemyType.MiddleBoss => "중간 보스",
        EnemyType.Boss => "보스",
        _ => "몬스터"
    };
}
