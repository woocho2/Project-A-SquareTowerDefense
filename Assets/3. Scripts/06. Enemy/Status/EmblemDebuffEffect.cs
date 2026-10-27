using System.Collections.Generic;

namespace SquareTD
{
    // 디버프존이 거는 문양 디버프 하나 (23번 #142의 틀). 내용(어떤 스탯을 얼마나)은 규칙이 정해 넘김 — 수업 26은 시험 규칙, 수업 27부터 DebuffRule
    //   같은 문양의 디버프는 겹치지 않음 → Key가 문양마다 하나 (20번 4-1장 [디버프의 규칙] 1)
    //   스킬 1: 그 적이 Duration번 행동하는 동안 / 스킬 2: 영구 (정화만 지움)
    public class EmblemDebuffEffect : StatusEffect
    {
        public static string KeyOf(TowerEmblem emblem) => $"{EmblemNames.GetEmblemName(emblem)} 디버프";

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
