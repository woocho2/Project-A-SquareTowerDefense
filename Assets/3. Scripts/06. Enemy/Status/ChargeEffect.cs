namespace SquareTD
{
    // 전하 — 적이 쌓는 전기 스택
    public class ChargeEffect : StatusEffect
    {
        public const string ChargeKey = "전하";

        public float Charge { get; private set; }

        private ChargeEffect(float charge) : base(1)
        {
            Charge = charge;
        }

        public override string Key => ChargeKey;
        public override bool IsPermanent => true;
        public override float Strength => Charge;

        public static void Add(EnemyController enemy, float amount)
        {
            if (enemy == null || !enemy.IsAlive || amount <= 0f) return;

            if (enemy.Status.Find(ChargeKey) is ChargeEffect existing) existing.Charge += amount;
            else enemy.Status.Add(new ChargeEffect(amount));
        }

        public static float Get(ITarget target)
        {
            EnemyController body = target?.Body;
            return body != null && body.Status != null && body.Status.Find(ChargeKey) is ChargeEffect charge ? charge.Charge : 0f;
        }

        public static void Clear(ITarget target)
        {
            if (target?.Body != null && target.Body.Status.Find(ChargeKey) is ChargeEffect charge) charge.Charge = 0f;
        }

        public override string Describe() => $"전하 {(int)Charge}";
    }
}
