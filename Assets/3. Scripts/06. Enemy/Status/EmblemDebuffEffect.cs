using System.Collections.Generic;

namespace SquareTD
{
    // 디버프 타워가 거는 문양 디버프의 베이스. 스킬 1은 존을 벗어난 뒤부터 유지 시간을 셈. 같은 문양이면 스킬 2만
    public class EmblemDebuffEffect : StatusEffect
    {
        public static string KeyOf(TowerEmblem emblem) => $"{EmblemNames.GetEmblemName(emblem)} 디버프";

        public static EmblemDebuffEffect On(ITarget target, TowerEmblem emblem)
        {
            EnemyStatusEffects status = target?.Body != null ? target.Body.Status : target?.Status;
            return status?.Find(KeyOf(emblem)) as EmblemDebuffEffect;
        }

        public static bool Has(ITarget target, TowerEmblem emblem) => On(target, emblem) != null;

        public static bool HasSkill2(ITarget target, TowerEmblem emblem)
        {
            EmblemDebuffEffect effect = On(target, emblem);
            return effect != null && effect.IsSkill2;
        }

        private readonly string m_description;
        private readonly StatModifier[] m_modifiers;

        public SupportCast Cast { get; }
        public TowerEmblem Emblem => Cast.Emblem;
        public bool IsSkill2 => Cast.IsSkill2;
        public int ZoneCell { get; set; } = -1;

        public EmblemDebuffEffect(SupportCast cast, string description, params StatModifier[] modifiers)
            : base(cast.Duration)
        {
            Cast = cast;
            m_description = description;
            m_modifiers = modifiers;
        }

        public override string Key => KeyOf(Emblem);
        public override int Rank => IsSkill2 ? 1 : 0;
        public override float Strength => Cast.Amount;
        public override IReadOnlyList<StatModifier> GetModifiers() => m_modifiers;

        protected override bool ShouldCountDown(EnemyController enemy) => ZoneCell < 0 || enemy.PathIndex != ZoneCell;

        public override string Describe() => $"{Key} ({Cast.SkillName}): {m_description}, {RemainingActions}회 행동 남음";
    }
}
