namespace SquareTD
{
    // 디버프 규칙의 베이스 (문양마다 하나). 스킬 1은 존을 밟은 적에게, 스킬 2는 적 전체에게
    public abstract class DebuffRule
    {
        public virtual StatusEffect CreateEffect(SupportCast cast, EnemyController enemy) => null;

        public virtual void OnApply(SupportCast cast, EnemyController enemy) { }

        public virtual void OnSkill2(SupportCast cast) { }

        protected static float Resist(EnemyController enemy, float value) => enemy.Resistance.ResistWeak(value);
    }
}
