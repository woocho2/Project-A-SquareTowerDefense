using System.Collections.Generic;

namespace SquareTD
{
    // 상태 효과를 설명 문구로 바꿈
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
