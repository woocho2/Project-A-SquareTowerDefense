namespace SquareTD
{
    // 프리즘 — 남은 횟수만큼 빛 효과
    public class PrismEffect : StatusEffect
    {
        public const string PrismKey = "프리즘";
        public const float RatePerStack = 0.05f;

        public int Stacks { get; private set; }

        private PrismEffect(int stacks) : base(1)
        {
            Stacks = stacks;
        }

        public override string Key => PrismKey;
        public override bool IsPermanent => true;
        public override float Strength => Stacks;

        public static void Apply(EnemyController enemy, int stacks)
        {
            if (enemy == null || !enemy.IsAlive || stacks <= 0) return;

            if (enemy.Status.Find(PrismKey) is PrismEffect existing) existing.Stacks += stacks;
            else enemy.Status.Add(new PrismEffect(stacks));
        }

        public static int GetStacks(EnemyController enemy) =>
            enemy != null && enemy.Status.Find(PrismKey) is PrismEffect prism ? prism.Stacks : 0;

        public override void OnActed(EnemyController enemy)
        {
            Stacks--;
            if (Stacks <= 0) enemy.Status.Remove(PrismKey);
        }

        public override void OnDamageTaken(EnemyController enemy, DamageInfo info, float damage)
        {
            if (info.Source != DamageSource.Basic || Stacks <= 0 || !enemy.IsAlive) return;

            float burst = enemy.Resistance.ResistWeak(enemy.MaxHp * RatePerStack * Stacks);
            Stacks = 0;
            enemy.Status.Remove(PrismKey);
            enemy.TakeDamage(new DamageInfo
            {
                Attacker = info.Attacker,
                Source = DamageSource.Skill,
                Emblem = TowerEmblem.Light,
                BaseDamage = burst,
                ForceFixed = true,
            });
        }

        public override string Describe() => $"프리즘 {Stacks}스택: 기본 공격을 맞으면 최대 체력 {RatePerStack * Stacks * 100f:0}%";
    }
}
