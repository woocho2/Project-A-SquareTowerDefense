using UnityEngine;

namespace SquareTD
{
    public static class ProjectileHitResolver
    {
        public static bool LogHits = false;     // 임시 체력 로그 "[Hit] …" 켜기·끄기 (2026-10-27 사용자 요청으로 끔, true면 다시 나옴)

        public static void HitSingle(ITarget target, DamageInfo info)
        {
            if (!target.IsAlive) return;
            target.TakeDamage(info);
            if (LogHits) Debug.Log($"[Hit] {info.Attacker.name} → {target.PathIndex}번 칸{(info.IsCritical ? " 치명" : "")} 체력 {target.CurrentHp:0.0} / {target.MaxHp:0.0}");
        }
    }
}
