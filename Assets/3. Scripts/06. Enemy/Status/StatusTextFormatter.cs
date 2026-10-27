using System.Collections.Generic;

namespace SquareTD
{
    // 효과 → 설명 문구 (23번 #166). Click Debug 창과 적 정보 패널이 같이 씀
    public static class StatusTextFormatter
    {
        public static string Describe(StatusEffect effect)
        {
            string tags = effect.IsPermanent ? " [영구]" : "";
            if (effect.IsStrongCC) tags += " [강한 CC]";
            return effect.Describe() + tags;
        }

        public static List<string> DescribeAll(EnemyStatusEffects status)
        {
            var lines = new List<string>();
            if (status == null) return lines;
            foreach (StatusEffect effect in status.Effects) lines.Add(Describe(effect));
            return lines;
        }
    }
}
