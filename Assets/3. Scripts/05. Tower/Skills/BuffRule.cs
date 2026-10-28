using System.Collections.Generic;

namespace SquareTD
{
    // 버프 규칙의 베이스 (21번 #56, 수업 27) — 문양마다 하나 (SwordBuffRule …), SkillRegistry가 문양으로 찾아 줌
    //   규칙은 상태를 갖지 않음: 스택·단계는 Zone이 들고 있고, 규칙은 "이 존이면 이만큼"만 계산 → 버프 타워 여럿이 한 규칙을 같이 씀
    //   훅은 그 훅을 처음 쓰는 문양의 수업에서 더함 (21번 4-4)
    public abstract class BuffRule
    {
        // 존 위에 선 공격 타워(receiver)에게 줄 수정자들. source는 출처 이름 ("검 버프존") — 뗄 때 이 이름으로 찾음
        public abstract List<StatModifier> GetModifiers(Zone zone, TowerController receiver, string source);

        // 스택을 존이 아니라 버프 타워 자신이 쌓나 (방패, 수업 31). true면 BuffZoneAction이 타워에 쌓고 존들은 그 값을 따라감
        public virtual bool UsesOwnerStack => false;

        // 적 수 한도 보정 (도끼 −, 수업 31 방패 +). 버프 타워가 판 위에 있는 동안. baseLimit = 사이클의 기본 한도
        //   같은 문양 버프 타워가 여럿이면 GameSession이 그중 값이 가장 큰 하나만 씀 (도끼: 가장 높은 티어 = 가장 덜 깎음)
        public virtual int GetLimitModifier(TowerController owner, int baseLimit) => 0;

        // 시너지: 무기 문양은 같은 속성끼리(베기·찌르기·타격), 원소 문양은 같은 문양끼리 (20번 4-2장)
        protected static bool IsSynergy(Zone zone, TowerController receiver)
        {
            AttackAttribute attribute = zone.Owner.Definition.Attribute;
            if (attribute != AttackAttribute.None) return receiver.Definition.Attribute == attribute;
            return receiver.Definition.Emblem == zone.Emblem;
        }
    }
}
