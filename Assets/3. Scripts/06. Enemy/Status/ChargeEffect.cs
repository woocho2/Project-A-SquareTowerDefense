namespace SquareTD
{
    // 전하 (21번 #139, 수업 38) — 적이 가지는 스택. 상한 없음, 과부하로 터뜨리면 0. 소수 그대로 쌓고 화면에는 정수로
    //   쌓는 것: 라이트닝 볼텍스, 전기 광역의 기본 공격, 누전·감전, 과충전 존에 선 타워의 공격. 저항력과 무관
    //   효과는 하나만 두고 값을 더함 (새로 Add하지 않음 → 같은 키끼리 세기를 겨루지 않게)
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

        // 과부하가 터뜨림 → 0
        public static void Clear(ITarget target)
        {
            if (target?.Body != null && target.Body.Status.Find(ChargeKey) is ChargeEffect charge) charge.Charge = 0f;
        }

        public override string Describe() => $"전하 {(int)Charge}";
    }
}
