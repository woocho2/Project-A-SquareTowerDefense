namespace SquareTD
{
    // 디버프 규칙의 베이스 (21번 #57, 수업 27) — 문양마다 하나 (SwordDebuffRule …), SkillRegistry가 문양으로 찾아 줌
    //   규칙은 상태를 갖지 않음: 적에게 걸 효과(StatusEffect)를 만들어 주기만 함. 효과가 매 턴 피해 같은 일을 직접 함
    public abstract class DebuffRule
    {
        // 디버프존을 밟은 적에게 걸 효과 하나
        public abstract StatusEffect CreateEffect(Zone zone, EnemyController enemy);

        // 존을 밟은 그 순간 한 번 (효과를 걸기 전). 효과가 이미 걸려 있어도 매번 (전기 누전: 밟을 때마다 전하, 수업 38)
        public virtual void OnStep(Zone zone, EnemyController enemy) { }

        // 모든 디버프 수치는 적의 기본 저항력만큼 줄어듦: 값 × (1 − 저항력%) (20번 4-1장 [디버프의 저항])
        protected static float Resist(EnemyController enemy, float value) => enemy.Resistance.ResistWeak(value);
    }
}
