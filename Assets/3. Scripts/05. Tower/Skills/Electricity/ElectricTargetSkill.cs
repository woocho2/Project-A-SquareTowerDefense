using System.Collections.Generic;

namespace SquareTD
{
    // 전기 단일 — 과부하 (20번 4-14장)
    //   발동시킨 적에서 시작해 1 + (컬러 강화 레벨 × Mastery)명에게 연쇄 (Mastery는 발 수가 아니라 단계). 가까운 적부터, 거리가 같으면 전하가 높은 적부터
    //   맞은 적마다 그 적 자신의 전하로: Power × AbilityValue × (1 + 전하 × 0.01) (스택을 쓰면 ×). 감전인 적은 전하를 2배로 셈
    //   터뜨린 뒤 그 적의 전하는 0. 연쇄된 적도 처음 맞은 적과 같은 식 (줄지 않음)
    //   전기 타워끼리: 충전 존에 서 있으면 과부하의 AbilityValue 1.5배, 과충전 존에서는 2배
    public class ElectricTargetSkill : AttackSkill
    {
        public ElectricTargetSkill(BoardGrid grid, PathOccupancy occupancy, StackModeSettings stackModes)
            : base(grid, occupancy, stackModes) { }

        public static int GetChainCount(TowerController owner) => 1 + owner.Stats.ColorLevel * owner.Stats.Mastery;

        protected override List<ITarget> GetArea(TowerController owner, ITarget mainTarget)
        {
            return SkillArea.Chain(m_grid, m_occupancy, mainTarget, GetChainCount(owner), ChargeEffect.Get);
        }

        protected override float ModifyOutgoingDamage(TowerController owner, ITarget target)
        {
            float charge = ChargeEffect.Get(target);
            if (EmblemDebuffEffect.StageOn(target, TowerEmblem.Electricity) == ZoneStage.Skill2) charge *= 2f;     // 감전

            float zone = 1f;
            ZoneStage stage = BuffRule.ReceivedStage(owner, TowerEmblem.Electricity);
            if (stage == ZoneStage.Skill2) zone = 2f;
            else if (stage == ZoneStage.Skill1) zone = 1.5f;

            return (1f + charge * 0.01f) * zone;
        }

        protected override void OnHitTarget(TowerController owner, ITarget target, DamageInfo hit)
        {
            ProjectileHitResolver.HitSingle(target, hit);
            // [C] 터뜨린 뒤 그 적의 전하는 0 — ChargeEffect에 만들어 둔 Clear(대상)
        }
    }
}
