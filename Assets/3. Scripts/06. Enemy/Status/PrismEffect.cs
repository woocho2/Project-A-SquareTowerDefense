namespace SquareTD
{
    // 프리즘 (21번 #140, 수업 39 빛 광역) — 줄어드는 남은 횟수. 그 적이 한 번 행동할 때마다 1씩 줄고 0이면 사라짐
    //   사라지기 전에 아무 타워의 "기본 공격"을 맞으면 터짐: 최대 체력의 (5 × 남은 스택)% (저항력만큼 줄어듦, 방어 무시). 터지면 사라짐
    //   지속 피해·다른 스킬로는 터지지 않음. 프리즘을 건 스킬 자체로도 (그 다음 공격부터)
    //   다시 걸면 스택을 더함 (2가 남은 적에게 4를 걸면 6)
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
        public override bool IsPermanent => true;           // 끝은 스택이 정함 (아래 OnActed)
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
                Source = DamageSource.Skill,             // 터진 피해로 다시 터지지 않게 기본 공격이 아닌 것으로
                Emblem = TowerEmblem.Light,
                BaseDamage = burst,
                ForceFixed = true,
            });
        }

        public override string Describe() => $"프리즘 {Stacks}스택: 기본 공격을 맞으면 최대 체력 {RatePerStack * Stacks * 100f:0}%";
    }
}
