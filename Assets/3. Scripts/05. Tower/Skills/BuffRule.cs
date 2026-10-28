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

        // 버프를 막 준 뒤 (버프 타워가 행동할 때, GetModifiers 다음). 확률로 터지는 버프 (바람 순풍·질풍, 빛 광채·광휘, 수업 36·39)
        public virtual void OnGiveBuff(Zone zone, TowerController receiver) { }

        // 존을 벗어나 버프가 끝날 때. 확률로 터진 시간 버프도 같이 끝남 (바람·빛)
        public virtual void OnTaken(Zone zone, TowerController receiver) { }

        // 존 위 타워가 행동할 때마다 (해머 세공: 10% 젬, 수업 32)
        public virtual void OnReceiverActed(Zone zone, TowerController receiver) { }

        // 존 위 타워의 기본 공격이 적에게 맞을 때마다 (얼음 냉기·한기: 이동력 감소, 전기 과충전: 전하, 수업 37·38)
        public virtual void OnReceiverHit(Zone zone, TowerController receiver, ITarget target) { }

        // 적 턴마다 한 번 (⓪). 단계가 도는 문양 (불 불길 → 불씨, 전기 과충전 → 방전 → 충전, 수업 33·38). 단계가 바뀌었으면 true
        public virtual bool OnZoneTurn(Zone zone) => false;

        // 시너지: 무기 문양은 같은 속성끼리(베기·찌르기·타격), 원소 문양은 같은 문양끼리 (20번 4-2장)
        protected static bool IsSynergy(Zone zone, TowerController receiver)
        {
            AttackAttribute attribute = zone.Owner.Definition.Attribute;
            if (attribute != AttackAttribute.None) return receiver.Definition.Attribute == attribute;
            return receiver.Definition.Emblem == zone.Emblem;
        }

        // 그 타워가 지금 받고 있는 그 문양 버프존의 단계 (없으면 None). 원소 문양의 맞물림에서 씀 ("불씨 존에 서 있으면 …")
        public static ZoneStage ReceivedStage(TowerController tower, TowerEmblem emblem)
        {
            ZoneStage best = ZoneStage.None;
            if (tower == null || tower.State == null) return best;

            foreach (Zone zone in tower.State.ReceivedZoneBuffs.Keys)
            {
                if (zone.Emblem != emblem || zone.Stage == ZoneStage.Discharge) continue;
                if (zone.Stage > best) best = zone.Stage;
            }
            return best;
        }
    }
}
