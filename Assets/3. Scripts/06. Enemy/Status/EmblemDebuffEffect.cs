using System.Collections.Generic;

namespace SquareTD
{
    // 디버프 타워가 거는 문양 디버프의 베이스. 스킬 1은 존을 벗어난 뒤부터 유지 시간을 셈. 같은 문양이면 스킬 2만
    public class EmblemDebuffEffect : StatusEffect
    {
        private static readonly Dictionary<TowerEmblem, string> s_keys = new Dictionary<TowerEmblem, string>();

        public static string KeyOf(TowerEmblem emblem)
        {
            if (!s_keys.TryGetValue(emblem, out string key)) s_keys[emblem] = key = $"{EmblemNames.GetEmblemName(emblem)} 디버프";
            return key;
        }

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

        public static bool IsHard(StatModifier modifier)
        {
            if (modifier.Stat == StatType.MaxAction) return modifier.Value > 0f;
            bool movement = modifier.Stat == StatType.SpeedMin || modifier.Stat == StatType.SpeedMax || modifier.Stat == StatType.ActionGainPerTurn;
            return movement && modifier.Value < 0f;
        }

        private readonly string m_description;
        private readonly StatModifier[] m_modifiers;
        private readonly StatModifier[] m_softModifiers;

        public SupportCast Cast { get; }
        public TowerEmblem Emblem => Cast.Emblem;
        public bool IsSkill2 => Cast.IsSkill2;
        public int ZoneCell { get; set; } = -1;
        public bool HardResisted { get; private set; }
        public bool HardApplied => HasHardPart && !HardResisted;
        protected virtual bool HasHardPart => m_softModifiers.Length < m_modifiers.Length;

        public EmblemDebuffEffect(SupportCast cast, string description, params StatModifier[] modifiers)
            : base(cast.Duration)
        {
            Cast = cast;
            m_description = description;
            m_modifiers = modifiers;
            m_softModifiers = System.Array.FindAll(modifiers, modifier => !IsHard(modifier));
        }

        public override string Key => KeyOf(Emblem);
        public override int Rank => IsSkill2 ? 1 : 0;
        public override float Strength => Cast.Amount;
        public override IReadOnlyList<StatModifier> GetModifiers() => HardResisted ? m_softModifiers : m_modifiers;

        public override void OnResist(EnemyResistance resistance)
        {
            if (HasHardPart) HardResisted = !resistance.TryStrongCC();
        }

        protected override bool ShouldCountDown(EnemyController enemy) => ZoneCell < 0 || enemy.PathIndex != ZoneCell;

        public override string Describe() => $"{Key} ({Cast.SkillName}): {m_description}{(HardResisted ? " (이동력·행동력 효과는 저항함)" : "")}, {RemainingActions}회 행동 남음";
    }
}
