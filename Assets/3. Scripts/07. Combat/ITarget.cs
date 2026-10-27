using UnityEngine;

namespace SquareTD
{
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
        EnemyStatusEffects Status { get; }      // 걸린 효과 목록 (수업 25). 가짜 대상·더미는 null일 수 있음
    }
}
