using System.Collections.Generic;

namespace SquareTD
{
    // 디버프존이 거는 문양 디버프의 베이스
    public class EmblemDebuffEffect : StatusEffect
    {
        public static string KeyOf(TowerEmblem emblem) => $"{EmblemNames.GetEmblemName(emblem)} 디버프";

        public static ZoneStage StageOn(ITarget target, TowerEmblem emblem)
        {
            EnemyStatusEffects status = target?.Body != null ? target.Body.Status : target?.Status;
            if (status == null) return ZoneStage.None;
            return status.Find(KeyOf(emblem)) is EmblemDebuffEffect effect ? effect.Stage : ZoneStage.None;
        }

        private readonly string m_description;
        private readonly float m_strength;
        private readonly StatModifier[] m_modifiers;

        public TowerEmblem Emblem { get; }
        public ZoneStage Stage { get; }

        public EmblemDebuffEffect(TowerEmblem emblem, ZoneStage stage, int durationActions, float strength, string description, params StatModifier[] modifiers)
            : base(durationActions)
        {
            Emblem = emblem;
            Stage = stage;
            m_strength = strength;
            m_description = description;
            m_modifiers = modifiers;
        }

        public override string Key => KeyOf(Emblem);
        public override bool IsPermanent => Stage == ZoneStage.Skill2;
        public override float Strength => m_strength;
        public override IReadOnlyList<StatModifier> GetModifiers() => m_modifiers;

        public override string Describe() => IsPermanent
            ? $"{Key} (스킬 2): {m_description}"
            : $"{Key} (스킬 1): {m_description}, {RemainingActions}회 행동 남음";
    }
}
