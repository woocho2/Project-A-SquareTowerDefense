using System.Collections.Generic;

namespace SquareTD
{
    // 빛 버프 — 헤일로 · 어센션 (추가 피해, 확률로 초월 카운트 +받는 타워의 Tier, 어센션 카운트 +1,
    //                            초월 웨이브에 어센션을 쓰면 다음 웨이브도 초월, 빛 타워가 공격하면 이 버프 타워의 스킬 카운트 +1)
    public class LightBuffRule : BuffRule
    {
        public const float AscensionCount = 1f;
        public const float LightSkillCount = 1f;

        public override List<StatModifier> GetModifiers(SupportCast cast, TowerController receiver, string source)
        {
            return new List<StatModifier> { StatModifier.Flat(source, StatType.BonusDamage, cast.Rate) };
        }

        public override void OnGiveSkill1(SupportCast cast, TowerController receiver) => RollCount(cast, receiver);

        public override void OnSkill2(SupportCast cast)
        {
            EmblemSharedState shared = EmblemSharedState.Current;
            if (BoardManager.Instance != null)
            {
                TowerSlots slots = BoardManager.Instance.Slots;
                for (int i = 0; i < slots.Count; i++)
                {
                    TowerController tower = slots.Get(i);
                    if (tower != null && tower.IsAttackTower) RollCount(cast, tower);
                }
            }
            shared.AddTranscend(AscensionCount);
            shared.ExtendTranscend();
        }

        public override void OnReceiverActed(SupportCast cast, TowerController receiver)
        {
            if (!IsSynergy(cast, receiver) || cast.Owner == null || cast.Owner.State == null) return;
            cast.Owner.State.SkillCount += LightSkillCount;
        }

        private static void RollCount(SupportCast cast, TowerController receiver)
        {
            if (Chance.Roll(cast.Rate)) EmblemSharedState.Current.AddTranscend(receiver.Definition.Tier);
        }
    }
}
