namespace SquareTD
{
    // 대지 디버프 — 진흙(스킬 1) · 석화(스킬 2) (20번 4-11장)
    //   턴마다 (0.001 × Power × AbilityValue × 스택)% 확률로 속박 (다음 행동 0 / 0, 강한 CC) — 확률은 저항력만큼 줄어듦
    //   대가: 진흙은 방어력 +20%, 석화는 +10% (적에게 주는 이득이라 저항과 무관)
    //   석화의 고유 효과: 최대 행동(행동 주기) +2. 정수형이라 저항할 수 없음
    public class EarthDebuffRule : DebuffRule
    {
        public override StatusEffect CreateEffect(Zone zone, EnemyController enemy)
        {
            bool isSkill2 = zone.Stage == ZoneStage.Skill2;
            float bindChance = Resist(enemy, SkillFormula.ZoneValue(zone));
            return new EarthDebuffEffect(zone, bindChance, isSkill2 ? 0.1f : 0.2f, isSkill2 ? 2 : 0);
        }
    }

    // 진흙·석화가 적에게 걸려 있는 동안
    public class EarthDebuffEffect : EmblemDebuffEffect
    {
        private readonly float m_bindChance;

        public EarthDebuffEffect(Zone zone, float bindChance, float defenseUp, int actionUp)
            : base(TowerEmblem.Earth, zone.Stage, zone.Owner.Stats.Duration, bindChance,
                   $"턴마다 {bindChance * 100f:0.##}%로 속박, 방어력 +{defenseUp * 100f:0}%" + (actionUp > 0 ? $", 행동 주기 +{actionUp}" : ""),
                   Modifiers(defenseUp, actionUp))
        {
            m_bindChance = bindChance;
        }

        private static StatModifier[] Modifiers(float defenseUp, int actionUp)
        {
            string source = KeyOf(TowerEmblem.Earth);
            StatModifier defense = StatModifier.Percent(source, StatType.Defense, defenseUp);
            if (actionUp <= 0) return new[] { defense };
            return new[] { defense, StatModifier.Flat(source, StatType.MaxAction, actionUp) };
        }

        // ⑤ 턴마다 한 번 굴림. 속박은 강한 CC라 붙일 때 지금 저항력%로 한 번 더 막을 수 있음 (Status.Add)
        public override void OnTurn(EnemyController enemy)
        {
            // [D] 확률이 맞으면 속박 — IceDebuffEffect.OnTurn과 같은 모양 (확률은 m_bindChance, 거는 것은 new ImmobilizeEffect("속박"))
        }
    }
}
