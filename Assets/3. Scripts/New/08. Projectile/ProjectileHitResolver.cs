using UnityEngine;

namespace SquareTD
{
    public static class ProjectileHitResolver
    {
        public static void HitSingle(ITarget target, DamageInfo info)
        {
            if (!target.IsAlive) return;
            target.TakeDamage(info);
            Debug.Log($"[Hit] {info.Attacker.name} → {target.PathIndex}번 칸{(info.IsCritical ? " 치명" : "")} 체력 {target.CurrentHp:0.0} / {target.MaxHp:0.0}");
        }
    }
}
