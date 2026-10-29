using System;
using System.Collections.Generic;

namespace SquareTD
{
    // 상태 효과의 베이스와 훅
    public abstract class StatusEffect
    {
        public abstract string Key { get; }
        public virtual bool IsPermanent => false;
        public virtual bool IsStrongCC => false;
        public virtual float Strength => 0f;

        public int RemainingActions { get; private set; }
        public bool IsExpired => !IsPermanent && RemainingActions <= 0;

        protected StatusEffect(int durationActions)
        {
            RemainingActions = durationActions;
        }

        public void CountDown()
        {
            if (!IsPermanent) RemainingActions--;
        }

        public virtual IReadOnlyList<StatModifier> GetModifiers() => Array.Empty<StatModifier>();
        public virtual void OnAdded(EnemyController enemy) { }
        public virtual void OnRemoved(EnemyController enemy) { }
        public virtual void OnEnemyTurnStart(EnemyController enemy) { }
        public virtual MoveRange OnDecideMove(EnemyController enemy, MoveRange range) => range;
        public virtual void OnMoved(EnemyController enemy, int steps) { }
        public virtual void OnActed(EnemyController enemy) { }
        public virtual void OnTurn(EnemyController enemy) { }
        public virtual void OnBeforeHit(EnemyController enemy, ref DamageInfo info) { }
        public virtual float ModifyIncomingDamage(EnemyController enemy, DamageInfo info, float damage) => damage;
        public virtual void OnDamageTaken(EnemyController enemy, DamageInfo info, float damage) { }
        public virtual float GetCritRateBonus(TowerController attacker) => 0f;
        public virtual float GetStunChanceBonus(DamageInfo info) => 0f;
        public virtual void OnExpire(EnemyController enemy) { }
        public virtual void OnDeath(EnemyController enemy, KillInfo kill) { }
        public virtual float GetGoldBonusRate(KillInfo kill) => 0f;
        public virtual int RollGemBonus(KillInfo kill) => 0;

        public abstract string Describe();
    }
}
