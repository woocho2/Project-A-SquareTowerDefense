using System.Collections.Generic;

namespace SquareTD
{
    // 전기 버프 — 충전(스킬 1) → 과충전(스킬 2) → 방전 → 충전 … (20번 4-14장)
    //   충전·과충전: 치명 확률과 치명 피해가 각각 +(0.001 × Power × AbilityValue × 스택)%
    //   과충전의 고유 효과: 과충전 존에 선 타워의 기본 공격이 맞을 때마다 그 적에게 전하 (광역 2, 단일 5)
    //   과충전은 (컬러 강화 레벨 × 5)턴 동안. 끝나면 방전: 아무 효과 없이 5턴 (티어·레벨과 무관). 끝나면 0스택의 충전
    public class ElectricBuffRule : BuffRule
    {
        public const int OverchargeTurnsPerLevel = 5;
        public const int DischargeTurns = 5;
        public const int SplashCharge = 2;
        public const int TargetCharge = 5;

        public override List<StatModifier> GetModifiers(Zone zone, TowerController receiver, string source)
        {
            var modifiers = new List<StatModifier>();
            if (zone.Stage == ZoneStage.Discharge) return modifiers;

            float value = SkillFormula.ZoneValue(zone);
            modifiers.Add(StatModifier.Flat(source, StatType.CritRate, value));
            // [D] 치명 피해도 같은 값 — 바로 위 CritRate 줄에서 스탯 이름만 CritDamage로
            return modifiers;
        }

        public override void OnReceiverHit(Zone zone, TowerController receiver, ITarget target)
        {
            if (zone.Stage != ZoneStage.Skill2) return;
            ChargeEffect.Add(target?.Body, receiver.IsSplash ? SplashCharge : TargetCharge);
        }

        // 과충전: 켜진 다음 턴부터 (레벨 × 5)턴 → 방전 5턴 → 0스택의 충전
        public override bool OnZoneTurn(Zone zone)
        {
            if (zone.Stage == ZoneStage.Skill2)
            {
                if (zone.StageTurnsLeft == 0)
                {
                    zone.StartStageTimer(zone.Owner.Stats.ColorLevel * OverchargeTurnsPerLevel);
                    return false;
                }
                if (!zone.TickStageTimer()) return false;
                zone.StartDischarge(DischargeTurns);
                return true;
            }

            if (zone.Stage == ZoneStage.Discharge)
            {
                if (!zone.TickStageTimer()) return false;
                zone.ResetToSkill1();
                return true;
            }
            return false;
        }
    }
}
