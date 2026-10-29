using UnityEngine;

namespace SquareTD
{
    // 탄이 닿았을 때 피해를 넣음
    public static class ProjectileHitResolver
    {
        public static bool LogHits = false;

        public static void HitSingle(ITarget target, DamageInfo info)
        {
            if (!target.IsAlive) return;
            target.TakeDamage(info);
            if (LogHits) Debug.Log($"[Hit] {info.Attacker.name} → {target.PathIndex}번 칸{(info.IsCritical ? " 치명" : "")} 체력 {target.CurrentHp:0.0} / {target.MaxHp:0.0}");
        }
    }
}
