using System.Collections.Generic;

namespace SquareTD
{
    // 상급 정령의 적 전체 최대 체력·방어력·저항력 강화
    public sealed class BossSpiritBlessing : StatusEffect
    {
        public const string Source = "상급 정령의 가호";
        private readonly int m_appliedTurn;
        private bool m_countingTurn;
        public override string Key => Source;
        public override bool IsDebuff => false;
        public override float Strength => 0.5f;

        public BossSpiritBlessing(int turns, int currentTurn) : base(turns) => m_appliedTurn = currentTurn;
        protected override bool ShouldCountDown(EnemyController enemy) => m_countingTurn;
        public override IReadOnlyList<StatModifier> GetModifiers() => new[] { StatModifier.Percent(Source, StatType.Defense, 0.5f) };

        public override void OnAdded(EnemyController enemy)
        {
            enemy.Health.ScaleMax(1.5f);
            enemy.Resistance.SetBossMultiplier(1.5f);
        }

        public override void OnRemoved(EnemyController enemy)
        {
            enemy.Health.ScaleMax(1f / 1.5f);
            enemy.Resistance.SetBossMultiplier(1f);
        }

        public void EndTurn(EnemyController enemy, int turn)
        {
            if (turn <= m_appliedTurn) return;
            m_countingTurn = true;
            CountDown(enemy);
            m_countingTurn = false;
            if (IsExpired) enemy.Status.Remove(Key);
        }

        public override string Describe() => $"최대 체력·방어력·저항력 +50%, {RemainingActions}턴";
    }
}
