using System;
using System.Collections.Generic;

namespace SquareTD
{
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

        public virtual IReadOnlyList<StatModifier> GetModifiers() => Array.Empty<StatModifier>();     // 걸려 있는 동안의 스탯 수정자 (출처는 Key로)
        public virtual void OnAdded(EnemyController enemy) { }                                        // 목록에 들어간 순간 한 번 (귀기: 최대 체력 줄이기)
        public virtual void OnRemoved(EnemyController enemy) { }                                      // 목록에서 빠질 때 — 끝남·정화·더 센 것으로 바뀜 (귀기: 되돌리기)
        public virtual void OnEnemyTurnStart(EnemyController enemy) { }                               // ⓪ (빛: 도깨비불 비우기)
        public virtual MoveRange OnDecideMove(EnemyController enemy, MoveRange range) => range;      // 다음 이동을 정할 때 (행동 불가 0 / 0, 역풍)
        public virtual void OnMoved(EnemyController enemy, int steps) { }                             // 이동한 뒤 (난기류, 프리즘)
        public virtual void OnActed(EnemyController enemy) { }                                        // 행동할 때마다 (유지 감소는 CountDown이 따로)
        public virtual void OnTurn(EnemyController enemy) { }                                         // ⑤ 매 턴 (지속 피해, 빙결 확률)
        public virtual void OnBeforeHit(EnemyController enemy, ref DamageInfo info) { }               // 맞기 전 (락 온: 치명 확률)
        public virtual float ModifyIncomingDamage(EnemyController enemy, DamageInfo info, float damage) => damage;   // 피해 계산에 끼어듦
        public virtual void OnDamageTaken(EnemyController enemy, DamageInfo info, float damage) { }  // 피해가 들어간 뒤 (처형)
        public virtual void OnExpire(EnemyController enemy) { }                                       // 유지가 끝날 때 (해동)
        public virtual void OnDeath(EnemyController enemy, KillInfo kill) { }                         // 그 적이 죽을 때 (원석·금맥)

        public abstract string Describe();                  // 설명 한 줄 (Click Debug 창, 적 정보 패널)
    }
}
