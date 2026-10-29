using UnityEngine;

namespace SquareTD
{
    // 타워가 공격할 수 있는 대상의 공통 모양
    public interface ITarget
    {
        Transform Transform { get; }
        int PathIndex { get; }
        bool IsAlive { get; }
        bool IsBoss { get; }
        bool IsDummy { get; }
        EnemyController Body { get; }
        float CurrentHp { get; }
        float MaxHp { get; }
        float PreviewDamage(DamageInfo info);
        void TakeDamage(DamageInfo info);
        void SetSortingOrder(int order);
        EnemyStats Stats { get; }
        EnemyStatusEffects Status { get; }
    }
}
