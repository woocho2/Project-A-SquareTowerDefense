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
    }
}
