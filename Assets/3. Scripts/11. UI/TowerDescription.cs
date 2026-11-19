using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace SquareTD
{
    // 타워 효과의 짧은 설명과 클릭 가능한 수치의 계산 근거
    public sealed class TowerDescription
    {
        public sealed class Value
        {
            public float Amount { get; }
            public string Title { get; }
            public string Explanation { get; }
            public Value(float amount, string title, string explanation) { Amount = amount; Title = title; Explanation = explanation; }
        }

        public int TowerId { get; internal set; }
        public string BasicTitle { get; internal set; }
        public string SkillTitle { get; internal set; }
        public string Basic { get; internal set; }
        public string Skill { get; internal set; }
        public string Activation { get; internal set; }
        public Dictionary<string, Value> Values { get; } = new Dictionary<string, Value>();
        public static string Plain(string richText) => Regex.Replace(richText ?? "", "<[^>]*>", "");
    }

    public readonly struct TowerDescriptionContext
    {
        public readonly int Limit, Gold;
        public readonly float Darkness;
        public readonly bool Transcend;
        public TowerDescriptionContext(int limit, int gold, float darkness, bool transcend)
        { Limit = limit; Gold = gold; Darkness = darkness; Transcend = transcend; }
        public static TowerDescriptionContext Current => new TowerDescriptionContext(
            GameSession.Instance != null ? GameSession.Instance.CurrentLimit : 0,
            CurrencyManager.Instance != null ? CurrencyManager.Instance.Gold : 0,
            EmblemSharedState.Current.DarknessCount, EmblemSharedState.Current.IsTranscendWave);
    }

    public sealed class TowerDescriptionBuilder
    {
        private readonly TowerDefinition m_definition;
        private readonly TowerFinalStats m_stats;
        private readonly TowerState m_state;
        private readonly TowerDescriptionContext m_context;
        private readonly TowerDescription m_document;
        private readonly float m_ability;

        private TowerDescriptionBuilder(TowerDefinition definition, TowerFinalStats stats, TowerState state, TowerDescriptionContext context)
        {
            m_definition = definition; m_stats = stats; m_state = state; m_context = context;
            m_document = new TowerDescription { TowerId = definition.Id };
            bool support = definition.Color == TowerColor.Yellow || definition.Color == TowerColor.Purple;
            m_ability = support ? stats.AbilityValue : stats.SkillAbility;
        }

        public static TowerDescription Build(TowerDefinition definition, TowerFinalStats stats, TowerState state = null) =>
            Build(definition, stats, state, TowerDescriptionContext.Current);

        public static TowerDescription Build(TowerDefinition definition, TowerFinalStats stats, TowerState state, TowerDescriptionContext context)
        {
            var builder = new TowerDescriptionBuilder(definition, stats, state, context);
            builder.Write();
            return builder.m_document;
        }

        private void Write()
        {
            if (global::SquareTD.TowerId.IsSynergy(m_definition.Id))
            {
                m_document.BasicTitle = "스택 충전";
                m_document.SkillTitle = SkillDescriptions.GetName(m_definition);
                m_document.Basic = "매 적 턴, 재료 문양 타워의 티어 합만큼 스택을 얻습니다.";
                m_document.Skill = SkillDescriptions.GetSynergyDescription(m_definition.Id);
                m_document.Activation = "스택이 차면 타워 아래 스킬 버튼으로 사용합니다.";
                return;
            }
            bool support = m_definition.Color == TowerColor.Yellow || m_definition.Color == TowerColor.Purple;
            string[] names = SkillDescriptions.GetName(m_definition).Split('→');
            m_document.BasicTitle = support ? names[0].Trim() + (m_definition.Color == TowerColor.Yellow ? " / 기본 버프" : " / 기본 디버프") : "기본 공격";
            m_document.SkillTitle = names[names.Length - 1].Trim() + (support ? m_definition.Color == TowerColor.Yellow ? " / 스킬 버프" : " / 스킬 디버프" : " / 스킬");
            m_document.Activation = $"스킬 카운트 {F(m_state?.SkillCount ?? 0)} / {F(m_stats.SkillCount)}";
            if (support)
            {
                bool buff = m_definition.Color == TowerColor.Yellow;
                m_document.Basic = buff ? Buff(false) : Debuff(false);
                m_document.Skill = buff ? Buff(true) : Debuff(true);
            }
            else { m_document.Basic = BasicAttack(); m_document.Skill = AttackSkill(); }
        }

        private static string F(float value) => value.ToString("0.##", CultureInfo.InvariantCulture);
        private float Counter(string key) => m_state != null ? m_state.GetCounter(key) : 0f;
        private string Value(string key, string title, float number, string unit, string equation, string extra = "", bool stats = true)
        {
            string explanation = $"{title}: {F(number)}{unit}\n\n{equation}";
            if (extra.Length > 0) explanation += "\n\n" + extra;
            if (stats) explanation += PowerDetails();
            m_document.Values[key] = new TowerDescription.Value(number, title, explanation);
            return $"<link=\"{key}\"><color=#6651A1><u>{F(number)}{unit}</u></color></link>";
        }

        private string PowerDetails()
        {
            var text = new StringBuilder($"\n\n현재 적용 수치\n공격력 {F(m_stats.Power)}\n스킬 계수 {F(m_stats.AbilityValue)}\n티어 {m_definition.Tier} · 스킬 레벨 {m_stats.ColorLevel}");
            foreach (StatSource source in m_stats.Sources ?? new List<StatSource>())
            {
                if (source.Stat != StatType.Power && source.Stat != StatType.AbilityValue && source.Stat != StatType.FinalDamage && source.Stat != StatType.BonusDamage) continue;
                string name = source.Stat == StatType.Power ? "공격력" : source.Stat == StatType.AbilityValue ? "스킬 계수" : "최종 피해 배율";
                text.Append($"\n{source.Source}: {name} {source.Amount:+0.##;-0.##;0}");
            }
            return text.ToString();
        }

        private string SkillBonusDetails()
        {
            float bonus = m_stats.Get(StatType.SkillDamageBonus);
            if (bonus == 0f) return "";
            return $"스킬 피해 보정\n{F(m_stats.AbilityValue)} x (1 + {F(bonus)}) = {F(m_ability)}";
        }

        private float AttackAmount(float ratio = 1f, bool outgoing = true)
        {
            float amount = SkillFormula.AttackSkill(m_stats.Power, m_ability);
            return amount * ratio * (outgoing ? m_stats.FinalDamageMultiplier : 1f);
        }

        private string Damage(string key = "skill.damage", float ratio = 1f, string extra = "", float additional = 1f)
        {
            float amount = AttackAmount(ratio) * additional;
            string factors = "공격력 x 적용 계수", numbers = $"{F(m_stats.Power)} x {F(m_ability)}";
            if (ratio != 1) { factors += " x 스킬 배율"; numbers += " x " + F(ratio); }
            if (m_stats.FinalDamageMultiplier != 1) { factors += " x 최종 피해 배율"; numbers += " x " + F(m_stats.FinalDamageMultiplier); }
            if (additional != 1f) { factors += " x 추가 보정"; numbers += " x " + F(additional); }
            string equation = factors + "\n" + numbers + " = " + F(amount);
            return Value(key, "기준 피해량", amount, "", equation, SkillBonusDetails() + "\n\n" + extra + "\n실제 피해에는 적의 방어력, 거리 보정과 치명타 등 타격 시의 조건이 추가로 적용됩니다.");
        }

        private string SkillPercent(string key = "skill.percent", string extra = "", string unit = "%", float multiplier = 1) => Value(key, "스킬 적용량", AttackAmount(1, false) * multiplier, unit,
            $"공격력 x 적용 계수\n{F(m_stats.Power)} x {F(m_ability)}" + (multiplier != 1 ? $" x {F(multiplier)} (초월)" : "") + $" = {F(AttackAmount(1, false) * multiplier)}{unit}", SkillBonusDetails() + "\n" + extra);

        private string DamagePercent(string key, string extra) => Value(key, "추가 피해 비율", AttackAmount(), "%",
            $"공격력 x 적용 계수 x 최종 피해 배율\n{F(m_stats.Power)} x {F(m_ability)} x {F(m_stats.FinalDamageMultiplier)}" + $" = {F(AttackAmount())}%", SkillBonusDetails() + "\n" + extra);

        private string SupportValue(bool skill, string key, string label, string unit = "%", float scale = 1f, float offset = 0f, float cap = float.PositiveInfinity, string extra = "")
        {
            float amount = skill ? SkillFormula.SupportSkill2(m_stats.Power, m_ability) : SkillFormula.SupportSkill1(m_stats.Power);
            float result = Mathf.Min(amount * scale + offset, cap);
            string equation = skill ? $"공격력 x (1 + 적용 계수)\n{F(m_stats.Power)} x (1 + {F(m_ability)}) = {F(amount)}" : $"기본 적용량 = 현재 공격력 = {F(amount)}";
            if (scale != 1f || offset != 0f) equation += $"\n{F(amount)} x {F(scale)} + ({F(offset)}) = {F(amount * scale + offset)}{unit}";
            if (!float.IsPositiveInfinity(cap)) equation += $"\n적용 상한: {F(cap)}{unit}";
            return Value((skill ? "skill." : "basic.") + key, label, result, unit, equation, extra);
        }

        private string Fixed(string key, string label, float amount, string unit = "", string extra = "") =>
            Value(key, label, amount, unit, "이 효과에 정해진 고정 수치입니다.", extra, false);

        private string BasicAttack()
        {
            string damage = Value("basic.damage", "기본 공격 1회 피해", m_stats.Power * m_stats.FinalDamageMultiplier, "",
                $"공격력 x 최종 피해 배율\n{F(m_stats.Power)} x {F(m_stats.FinalDamageMultiplier)} = {F(m_stats.Power * m_stats.FinalDamageMultiplier)}",
                "치명타·거리·적 방어력 등의 타격 조건은 별도로 적용됩니다.");
            bool splash = m_definition.Color == TowerColor.Red;
            int count = splash ? m_stats.AttackCount : m_stats.AttackCount * TargetAction.GetShotsPerAttack(m_stats.Mastery);
            string hits = Value("basic.hits", "행동당 공격 횟수", count, "회", splash ? $"공격 횟수 = {m_stats.AttackCount}" : $"공격 횟수 x 숙련도별 투사체 수\n{m_stats.AttackCount} x ({m_stats.Mastery} x 2) = {count}", "적 상태에 따른 추가 공격은 포함하지 않습니다.");
            if (!splash) return $"사거리 안의 적에게 {hits} 공격합니다.\n한 발당 {damage}의 피해를 줍니다.";
            int side = Mathf.Max(1, m_stats.Mastery) * 2 - 1;
            string area = Value("basic.area", "광역 공격 한 변의 길이", side, "칸", $"숙련도 x 2 - 1\n{m_stats.Mastery} x 2 - 1 = {side}");
            return $"착탄 지점 중심으로 한 변이 {area}인 범위를 {hits} 공격합니다.\n적마다 한 발당 {damage}의 피해를 줍니다.";
        }

        private string AttackSkill()
        {
            bool red = m_definition.Color == TowerColor.Red;
            string duration = Fixed("skill.duration", "효과 지속", SkillFormula.EffectTurns, "턴");
            string actions = Fixed("skill.actions", "버프 지속", SkillFormula.EffectActions, "회");
            switch (m_definition.Emblem)
            {
                case TowerEmblem.Sword:
                    return red ? $"적 전체에게 {Damage()}의 피해를 줍니다." : $"적 하나를 {Fixed("skill.hits", "검기 횟수", SwordTargetSkill.StormHits, "회")} 베어 총 {Damage()}의 피해를 줍니다.\n마지막에 잃은 체력의 {DamagePercent("skill.burst", "검기를 모두 맞은 뒤의 잃은 체력에 이 비율을 곱합니다. 거리 보정 등 타격 조건은 별도로 적용됩니다.")}만큼 추가 피해를 줍니다.";
                case TowerEmblem.Axe:
                    return red ? $"적 전체에게 {Damage()}의 피해를 줍니다. 사거리 안의 적에게는 반드시 치명타가 발생합니다." : $"자신의 공격력이 {SkillPercent()} 증가하고 치명타 확률이 100%p 증가합니다.\n{actions} 행동 동안 매 턴 행동 카운트를 1 추가로 얻습니다.";
                case TowerEmblem.Bow:
                    return red ? $"{duration} 동안 매 턴 적 전체에게 {Damage(ratio: ArrowRainGround.TurnRatio)}의 피해를 줍니다. 중첩할 수 있습니다." : $"적 하나에게 최대 체력의 {DamagePercent("skill.maxhp", "최대 체력에 이 비율을 곱한 뒤 거리 및 방어력 보정을 받습니다. 적의 저항력에는 영향을 받지 않습니다.")}에 해당하는 피해를 줍니다.";
                case TowerEmblem.Spear:
                    if (red) return $"사거리 안 아군 공격 타워의 공격력을 {SkillPercent()}, 치명타 확률을 {SkillPercent("skill.crit", unit: "%p")} 높입니다. {actions} 행동 동안 유지됩니다.";
                    return $"사거리 안에서 체력이 가장 높은 적에게 창 {LevelCount("skill.hits")}개를 던집니다.\n창 하나당 {Damage(ratio: SpearTargetSkill.SpearRatio)}의 피해를 줍니다.";
                case TowerEmblem.Shield:
                    return red ? $"착탄 범위의 적에게 {Damage()}의 피해를 주고, 저항하지 못한 적을 1회 행동 동안 기절시킵니다." : $"가까운 적 사이로 방패가 {LevelCount("skill.bounces")}번 튕기며 한 번에 {Damage(ratio: ShieldTargetSkill.BounceRatio)}의 피해를 줍니다.\n튕길 대상이 없으면 남은 피해를 합쳐 줍니다.";
                case TowerEmblem.Hammer:
                    return red ? $"착탄 범위의 적에게 {Damage()}의 피해를 주고 맞은편 칸으로 날려 보냅니다. 밀어내기는 적의 저항력에 영향을 받습니다." : $"적 하나를 {Fixed("skill.hits", "타격 횟수", HammerTargetSkill.Strikes, "회")} 공격해 총 {Damage()}의 피해를 줍니다.\n마지막 타격 후 1회 행동 동안 방어력을 낮춥니다.";
                case TowerEmblem.Fire:
                    return red ? $"적 전체에게 {Damage(extra: "적마다 화상 스택에 따라 추가로 x (1 + 화상 x 0.01)이 적용됩니다.")}의 피해를 줍니다. 화상이 많을수록 강해집니다." : $"{duration} 동안 매 턴 체력이 가장 높은 적에게 {Damage(extra: "화상 스택마다 피해가 1%씩 증가합니다.")}의 피해와 화상 1을 줍니다.\n브레스 중에는 기본 공격을 하지 않습니다.";
                case TowerEmblem.Water:
                    return red ? $"적 전체에게 {Damage(extra: "적마다 젖음 스택에 따라 추가로 x (1 + 젖음 x 0.01)이 적용됩니다.")}의 피해를 주고 최대 3칸 밀어냅니다. 젖음이 많을수록 강해집니다." : $"적 하나를 {duration} 동안 가두고, 매 턴 {Damage(extra: "젖음 스택에 따라 피해가 증가합니다. 가두기에 저항하면 피해도 저항력만큼 줄어듭니다.")}의 피해를 줍니다.\n젖음이 많을수록 강해지며 가두기는 저항할 수 있습니다.";
                case TowerEmblem.Earth:
                    string growth = Value("skill.growth", "현재 성장 스택", Counter(Growth.Key), "", "이 타워에 누적된 성장 스택입니다.", "어스퀘이크: 스킬 사용 후 계수가 성장 스택%만큼 영구 증가합니다.\n소잉: 공격력·치명타 확률·치명타 피해·공격 횟수 중 하나가 성장합니다. 공격 횟수는 성장 x 0.01을 누적하고 정수만 적용합니다.");
                    return red ? $"적 전체에게 {Damage()}의 피해를 주고 다음 행동을 1회 막습니다.\n사용할 때마다 성장 {growth}에 따라 스킬 계수가 영구 증가합니다." : $"적 하나에게 {Damage()}의 피해를 줍니다.\n사용할 때마다 무작위 능력치 하나가 성장 {growth}에 따라 영구 증가합니다.";
                case TowerEmblem.Wind:
                    float acceleration = Counter(Acceleration.Key);
                    if (red)
                    {
                        float critical = CriticalRoll.Multiplier(m_stats.CriticalDamage) + acceleration * WindSplashSkill.AccelerationRate;
                        return $"착탄 범위의 적에게 총 {Damage(extra: "확정 치명타 배율과 현재 가속을 반영한 합계입니다. 실제 타격 시 범위 안의 살아 있는 적 수로 나눕니다.", additional: critical)}의 피해를 나눠 줍니다. 반드시 치명타가 발생합니다.";
                    }
                    float chance = AttackAmount(1, false) + acceleration;
                    string chanceText = Value("skill.chance", "발동 확률", Mathf.Clamp(chance, 0, 100), "%", $"공격력 x 적용 계수 + 가속\n{F(AttackAmount(1, false))} + {F(acceleration)} = {F(chance)}%\n확률 상한 100%", SkillBonusDetails());
                    return $"{chanceText} 확률로 자신의 공격 횟수를 {Fixed("skill.hits", "공격 횟수 증가", acceleration > 0 ? WindTargetSkill.AcceleratedBonus : WindTargetSkill.AttackCountBonus, "회")} 늘립니다. {actions} 행동 동안 유지됩니다.";
                case TowerEmblem.Ice:
                    return red ? $"적 전체에게 {Damage()}의 피해를 줍니다.\n빙결된 적에게는 피해가 150% 증가하고, 한기 상태인 적은 빙결됩니다." : $"적 하나에게 {Damage()}의 피해를 줍니다. 한기·빙결 상태에서는 반드시 치명타가 발생하며, 빙결 시 치명타 피해가 300%p 추가됩니다.";
                case TowerEmblem.Electricity:
                    if (red)
                    {
                        float multiplier = m_state != null && m_state.SupportBuffs.TryGetValue(TowerEmblem.Electricity, out SupportBuffEntry entry) && entry.Skill2Left > 0 ? ElectricBuffRule.ElectricChargeMultiplier : 1;
                        float charge = m_definition.Tier * m_stats.ColorLevel * ElectricSplashSkill.VortexChargePerTierLevel * multiplier;
                        string chargeText = Value("skill.charge", "전하 부여량", charge, "", $"티어 x 스킬 레벨 x {F(ElectricSplashSkill.VortexChargePerTierLevel)} x 하이퍼 펄스 배율\n{m_definition.Tier} x {m_stats.ColorLevel} x {F(ElectricSplashSkill.VortexChargePerTierLevel)} x {F(multiplier)} = {F(charge)}");
                        return $"적 전체에게 {Damage()}의 피해와 전하 {chargeText}을 줍니다.";
                    }
                    return $"적 전체에게 {Damage(extra: "적의 전하에 따라 x (1 + 전하 x 0.01)이 적용됩니다.")}의 피해를 줍니다.\n전하가 많을수록 강해지며, 타격 후 전하를 모두 없앱니다.";
                case TowerEmblem.Light:
                    if (red) return $"아군 공격 타워 전체의 공격력을 {SkillPercent(multiplier: m_context.Transcend ? 2 : 1)}, 치명타 확률·피해를 {SkillPercent("skill.crit", unit: "%p", multiplier: m_context.Transcend ? 2 : 1)} 높입니다. {actions} 행동 동안 유지됩니다." + (m_context.Transcend ? "\n공격 횟수와 숙련도도 1 증가합니다." : "\n초월 중에는 효과가 강화됩니다.");
                    return $"{duration} 동안 {(m_context.Transcend ? "적 전체" : "적 하나")}에게 첫 턴 {Damage(ratio: m_context.Transcend ? 1 : LightTargetSkill.FirstRatio)}의 피해를 줍니다.\n매 턴 피해가 {Fixed("skill.growth", "턴별 피해 배율", m_context.Transcend ? LightTargetSkill.TranscendGrowth : LightTargetSkill.DoubleGrowth, "배")}가 됩니다.";
                case TowerEmblem.Darkness:
                    return red ? $"착탄 지점에 {duration} 동안 블랙홀을 만듭니다.\n매 턴 주변 적을 끌어당기고 {Damage(ratio: DarknessSplashSkill.TurnRatio)}의 피해를 줍니다." : $"적 하나에게 {Damage(extra: $"침식 보정: 1 + 현재 침식 카운트 {F(m_context.Darkness)} x 0.01", additional: DarknessTargetSkill.GetAbyssMultiplier(m_context.Darkness))}의 피해를 줍니다. 침식 카운트가 높을수록 강해집니다.";
                default: return $"적에게 {Damage()}의 피해를 줍니다.";
            }
        }

        private string LevelCount(string key) => Value(key, "티어·레벨에 따른 횟수", SkillFormula.LevelTierCount(m_stats.ColorLevel, m_definition.Tier), "", $"티어 x 스킬 레벨 (최소 1)\n{m_definition.Tier} x {m_stats.ColorLevel} = {SkillFormula.LevelTierCount(m_stats.ColorLevel, m_definition.Tier)}");

        private static float U(float bronze) => bronze / SkillFormula.SupportBasePower;

        private string Buff(bool skill)
        {
            string who = skill ? "아군 공격 타워 전체의 " : "존 위 아군 공격 타워의 ";
            string effect;
            switch (m_definition.Emblem)
            {
                case TowerEmblem.Sword: effect = who + $"공격력이 {SupportValue(skill, "amount", "공격력 증가", scale: U(SwordBuffRule.BronzePower * 100f))} 증가합니다." + (skill ? "\n최종 피해도 10% 증가하며, 베기 타워는 치명타 확률이 10%p 더 증가합니다." : ""); break;
                case TowerEmblem.Axe: effect = who + $"치명타 피해가 {SupportValue(skill, "crit", "치명타 피해 증가", "%p", U(AxeBuffRule.BronzeCritDamage * 100f))} 증가합니다." + (skill ? "\n행동 간격이 1 줄고, 베기 타워는 치명타 피해가 20%p 더 증가합니다." : ""); break;
                case TowerEmblem.Bow: effect = who + $"피해가 거리 1칸마다 {SupportValue(skill, "amount", "거리 1칸당 피해 증가", scale: U(BowBuffRule.BronzeDistanceDamage * 100f))} 증가합니다." + (skill ? "\n사거리도 1칸 늘어나고, 찌르기 타워는 항상 최대 거리 보정을 받습니다." : ""); break;
                case TowerEmblem.Spear: effect = who + $"공격을 1회로 모으고 기존 공격 횟수만큼 공격력을 보정합니다. 공격력도 {SupportValue(skill, "amount", "공격력 증가", scale: U(SpearBuffRule.BronzePower * 100f))} 증가합니다." + (skill ? "\n치명타 확률과 방어 관통이 10%p 증가하며, 찌르기 타워는 적의 받는 치유를 40%p 줄입니다." : ""); break;
                case TowerEmblem.Shield:
                    effect = who + $"최종 피해가 {SupportValue(skill, "limit", "한도 비례 피해 증가", scale: U(ShieldBuffRule.BronzeFinalDamagePerLimit * 100f) * m_context.Limit, extra: $"현재 적 수 한도 {m_context.Limit}을 사용합니다. 이지스가 한도를 바꾸면 실제 적용량도 갱신됩니다.")} 증가합니다." + (skill ? "\n적 수 한도를 50% 높이고, 타격 타워는 한도에 비례해 공격력이 더 증가합니다." : ""); break;
                case TowerEmblem.Hammer: effect = who + $"피해가 {SupportValue(skill, "gold", "골드 비례 피해 증가", scale: U(HammerBuffRule.BronzeDamagePerGold * 100f) * m_context.Gold, extra: $"현재 골드 {m_context.Gold}를 사용하며 골드가 바뀌면 실제 효과도 바뀝니다.")} 증가합니다." + (skill ? "\n처치 골드가 50% 증가하고, 타격 타워는 50% 확률로 추가 골드를 얻습니다." : ""); break;
                case TowerEmblem.Fire:
                    float fireScale = U(FireBuffRule.BronzeTotalGain * 100f) / FireBuffRule.AttackCountMultiplier;
                    float fireOffset = (1f / FireBuffRule.AttackCountMultiplier - 1f) * 100f + (skill ? FireBuffRule.RisingPower * 100f : 0f);
                    float powerChange = (skill ? SkillFormula.SupportSkill2(m_stats.Power, m_ability) : m_stats.Power) * fireScale + fireOffset;
                    string powerText = SupportValue(skill, "power", "공격력 증감", scale: fireScale, offset: fireOffset, extra: "공격 횟수 2배와 합쳐 화력 합계가 브론즈 기준 +10%가 되도록 공격력을 깎습니다." + (skill ? " 라이징 피닉스의 추가 공격력 10%까지 합산한 값입니다." : "")).Replace("<u>-", "<u>");
                    effect = who + $"공격 횟수가 기본값만큼 늘고 공격력이 {powerText} {(powerChange < 0 ? "감소" : "증가")}합니다." + (skill ? "\n불 타워는 치명타 피해도 20%p 증가합니다." : ""); break;
                case TowerEmblem.Water: effect = who + $"스킬 카운트 획득량이 {SupportValue(skill, "skillcount", "스킬 카운트 획득량 증가", scale: U(WaterBuffRule.BronzeSkillCountGain * 100f))} 증가합니다." + (skill ? "\n스킬 2 피해가 50% 증가하고, 물 타워는 적의 잃은 체력에 비례해 최대 10% 추가 피해를 줍니다." : ""); break;
                case TowerEmblem.Earth: effect = who + $"스킬 계수가 {SupportValue(skill, "amount", "스킬 계수 증가", scale: U(EarthBuffRule.BronzeAbilityValue * 100f))} 증가합니다." + (skill ? $"\n스킬 발동에 필요한 카운트가 {Fixed("skill.countcut", "스킬 카운트 감소", m_definition.Tier)} 줄고, 대지 타워는 공격 행동마다 성장을 1 얻습니다." : ""); break;
                case TowerEmblem.Wind:
                    effect = who + $"행동 간격이 1 줄고 공격 횟수가 1 늘어납니다.\n치명타 확률은 {SupportValue(skill, "critRate", "치명타 확률 증감", "%p", -U(WindBuffRule.BronzeCritRate * 100f), 2f * WindBuffRule.BronzeCritRate * 100f, extra: "패널티는 티어가 오를수록 줄고 다이아에서는 이득이 됩니다.")}, 치명타 피해는 {SupportValue(skill, "critDamage", "치명타 피해 증감", "%p", -U(WindBuffRule.BronzeCritDamage * 100f), 2f * WindBuffRule.BronzeCritDamage * 100f)} 변합니다." + (skill ? "\n공격력이 10% 증가하며, 바람 타워는 행동마다 한 번 적을 1칸 밀고 다음 행동을 앞당깁니다." : ""); break;
                case TowerEmblem.Ice: effect = who + $"공격력이 {SupportValue(skill, "amount", "공격력 증가", scale: U(IceBuffRule.BronzePower * 100f))}, 치명타 확률이 {SupportValue(skill, "critRate", "치명타 확률 증가", "%p", U(IceBuffRule.BronzeCritRate * 100f))}, 치명타 피해가 {SupportValue(skill, "critDamage", "치명타 피해 증가", "%p", U(IceBuffRule.BronzeCritDamage * 100f))} 증가합니다. 행동 간격은 4 늘어납니다." + (skill ? "\n숙련도가 1 증가하며, 얼음 타워의 행동 간격 증가는 2로 줄어듭니다." : ""); break;
                case TowerEmblem.Electricity: effect = who + $"공격 횟수가 기본값의 2배만큼 늘어납니다.\n공격력은 {SupportValue(skill, "penalty", "공격력 증감", "%", U(ElectricBuffRule.BronzeValue), -ElectricBuffRule.PenaltyBase, extra: "값(브론즈 20)에서 100을 뺀 만큼 공격력이 변합니다. 기본 공격 횟수 2배를 더해 기본값의 3배가 됩니다.")}, 치명타 확률은 {SupportValue(skill, "critRate", "치명타 확률 증감", "%p", -U(ElectricBuffRule.BronzeCritRate * 100f), 2f * ElectricBuffRule.BronzeCritRate * 100f)} 변하고, 타격마다 전하 {SupportValue(skill, "charge", "타격당 전하", "", U(ElectricBuffRule.BronzeValue))}을 줍니다." + (skill ? "\n공격 행동마다 스킬 카운트를 1 더 얻으며, 전기 타워는 전하 부여량이 2배가 됩니다." : ""); break;
                case TowerEmblem.Light: effect = who + $"피해가 {SupportValue(skill, "amount", "피해 증가", scale: U(LightBuffRule.BronzeBonusDamage * 100f))} 증가합니다.\n{SupportValue(skill, "chance", "초월 카운트 확률", scale: U(LightBuffRule.BronzeTranscendChance * 100f))} 확률로 받는 타워의 티어만큼 초월 카운트를 얻습니다." + (skill ? "\n초월 카운트를 1 더 얻고, 초월 중 사용하면 다음 웨이브까지 연장합니다. 빛 타워의 공격 행동은 이 버프 타워의 스킬 카운트를 1 올립니다." : ""); break;
                case TowerEmblem.Darkness: effect = $"침식 카운트를 {SupportValue(skill, "darkness", "침식 카운트 증가", "", U(DarknessBuffRule.BronzeDarkness), extra: DarknessStages())} 올리고, 침식 단계에 따라 {(skill ? "아군 공격 타워 전체" : "존 위 아군 공격 타워")}를 강화합니다." + (skill ? "\n타격마다 0.5% 확률로 적을 처형하고 침식을 50 얻습니다. 어둠 타워의 확률은 1%입니다." : ""); break;
                default: effect = who + $"능력치를 {SupportValue(skill, "amount", "버프 적용량")} 높입니다."; break;
            }
            return effect + (skill ? $"\n{SkillFormula.EffectActions}회 행동 동안 유지됩니다." : "");
        }
        private string DarknessStages() => $"현재 침식 카운트: {F(m_context.Darkness)}\n누적 단계별 효과\n100: 공격력 +20%\n200: 치명타 확률 +10%p\n300: 치명타 피해 +35%p\n400: 공격 횟수 +1\n500: 행동 간격 -1\n600: 보상 배율 +1.5\n700: 스킬 계수 +10%\n800: 숙련도 +1\n900: 피해를 고정 피해로 변경\n1000: 최종 피해 증가\n공격력·치명타·스킬 계수는 브론즈 기준이며 티어가 오를수록 커집니다.";

        private string Debuff(bool skill)
        {
            string who = skill ? "적 전체에게 " : "존에 들어온 적에게 ";
            string resistance = "표시 수치는 저항 적용 전 기준입니다. 실제 효과는 적의 저항력에 따라 줄어듭니다.";
            string Pct(string key, string label, float bronze, string unit = "%", float cap = float.PositiveInfinity, string more = "") =>
                SupportValue(skill, key, label, unit, U(bronze), cap: cap, extra: resistance + more);
            string effect;
            switch (m_definition.Emblem)
            {
                case TowerEmblem.Sword: effect = who + $"최대 체력을 {Pct("hp", "최대 체력 감소율", SwordDebuffRule.BronzeMaxHpCut * 100f, cap: 95, more: "\n저항 적용 후 감소 상한 95%가 적용됩니다.")} 낮춥니다." + (skill ? "\n매 턴 최대 체력의 1% 피해를 주며, 베기 피해 일부가 고정 피해로 바뀝니다." : ""); break;
                case TowerEmblem.Axe:
                    string threshold = Pct("amount", "처형 기준", AxeDebuffRule.BronzeThreshold * 100f);
                    effect = skill ? $"적 전체에게 표식을 새깁니다. 피해를 받은 뒤 체력이 {threshold} 미만이면 처형합니다.\n받는 피해가 10% 증가하며, 베기 공격은 처형 기준이 10%p 높습니다." : $"존에 들어온 적의 체력이 최대 체력의 {threshold} 미만이면 즉시 처형합니다."; break;
                case TowerEmblem.Bow: effect = who + $"받는 피해를 {Pct("amount", "받는 피해 증가", BowDebuffRule.BronzeDamageTaken * 100f)} 높이는 표식을 새깁니다." + (skill ? "\n받는 치명타 확률이 10%p 높아지며, 찌르기 피해의 20%를 고정 추가타로 받습니다." : ""); break;
                case TowerEmblem.Spear: effect = who + $"받은 피해의 {Pct("amount", "출혈 비율", SpearDebuffRule.BronzeBleedRate * 100f)}만큼 매 턴 피해를 주는 출혈을 3턴 동안 쌓습니다." + (skill ? "\n최소·최대 이동력을 1 줄이고, 찌르기 공격은 출혈량이 2배가 됩니다." : ""); break;
                case TowerEmblem.Shield: effect = who + $"저항력을 {Pct("resistance", "저항력 감소", ShieldDebuffRule.BronzeResistanceCut, "%p")} 낮춥니다." + (skill ? "\n최대 이동력을 2 줄이고, 타격 공격으로 받는 피해를 5% 더 높입니다." : ""); break;
                case TowerEmblem.Hammer: effect = who + $"방어력을 {Pct("amount", "방어력 감소", HammerDebuffRule.BronzeDefenseCut * 100f)} 낮춥니다." + (skill ? $"\n처치 시 골드 {Fixed("skill.gold", "추가 처치 골드", HammerDebuffRule.JackpotGold)}를 얻고, 타격 타워가 처치하면 같은 양을 더 얻습니다." : ""); break;
                case TowerEmblem.Fire: effect = who + $"화상 {Pct("burn", "화상 부여량", FireDebuffRule.BronzeBurn, "")}을 쌓습니다. 화상은 그 적이 움직인 턴마다 최대 체력의 스택 × 0.1% 피해를 주고 절반으로 줄어듭니다." + (skill ? "\n화상 피해가 2배가 되고, 불 타워의 공격이 화상을 추가로 쌓습니다." : ""); break;
                case TowerEmblem.Water: effect = who + $"젖음 {Pct("wet", "젖음 부여량", WaterDebuffRule.BronzeWet, "")}을 쌓습니다. 젖음이 {WetEffect.TriggerAt:0}에 닿으면 다음 행동이 끝날 때까지 받는 피해의 {WetEffect.FixedRatio * 100f:0}%가 고정 피해가 되고, 그 뒤 젖음이 모두 사라집니다." + (skill ? "\n받는 피해를 10% 높이며 물 공격에는 10% 더 높입니다. 물 공격에 맞은 적은 다음 이동력이 2 늘어납니다." : ""); break;
                case TowerEmblem.Earth:
                    float growth = Counter(Growth.Key) + (skill ? 0 : 1);
                    effect = who + $"최대 체력과 방어력을 {SupportValue(skill, "growthcut", "성장에 따른 감소율", scale: U(EarthDebuffRule.BronzeCut * 100f) * growth, cap: 95, extra: resistance + $"\n적용 시 자신의 성장 {F(growth)}배를 곱합니다." + (skill ? "" : "\n기본 효과는 적용 직전에 자신의 성장을 1 올립니다."))} 낮추고 다른 모든 아군에게 성장 {SupportValue(skill, "nourish", "성장 부여량", "", U(EarthDebuffRule.BronzeCut * 100f) * EarthDebuffRule.NourishRate)}을 줍니다." + (skill ? "\n적의 적응형 능력치를 1 줄이고, 대지 타워에게 성장에 비례한 추가 피해를 받게 합니다." : ""); break;
                case TowerEmblem.Wind: effect = who + $"다음 이동력을 5 높이고, 이동 1칸마다 최대 체력의 {Pct("step", "이동 한 칸당 피해", WindDebuffRule.BronzeStepDamage * 100f)} 고정 피해를 줍니다.\n모든 아군에게 가속 {SupportValue(skill, "acceleration", "가속 부여량", "", U(WindDebuffRule.BronzeAcceleration), extra: "가속이 20이 차면 그 타워의 다음 행동이 1턴 빨라집니다.")}을 줍니다." + (skill ? "\n적의 행동 간격을 3 늘리고, 바람 타워가 기본 공격으로 맞히면 스킬 카운트를 2 올립니다." : ""); break;
                case TowerEmblem.Ice: effect = skill ? "적 전체의 한기를 빙결로 바꿉니다.\n한기·빙결 중 받는 피해가 50% 증가하고, 얼음 타워의 공격 횟수도 늘어납니다." : who + $"한기 {Pct("chill", "한기 부여량", IceDebuffRule.BronzeChill, "")}를 쌓습니다. 한기 25 이상이면 최소·최대 이동력이 1 줄고, 50에 닿으면 빙결되며 빙결이 풀릴 때 한기가 모두 사라집니다."; break;
                case TowerEmblem.Electricity: effect = who + $"전하 {SupportValue(skill, "charge", "전하 부여량", "", U(ElectricDebuffRule.BronzeCharge), extra: "전하는 이 부여 단계에서 저항력으로 감소하지 않습니다.")}을 쌓습니다." + (skill ? "\n전하가 즉시 2배가 되며, 전기 타워에게 맞으면 주변 적에게 피해가 연쇄됩니다." : ""); break;
                case TowerEmblem.Light: effect = who + $"실명 {Pct("blind", "실명 부여량", LightDebuffRule.BronzeBlind, "", more: "\n자신에게도 같은 양의 실명을 누적하고, 100을 넘길 때마다 자신의 티어만큼 초월 카운트를 얻습니다.")}을 쌓습니다. 실명이 10에 닿으면 다음 행동은 뒤로 걷고 실명이 모두 사라집니다." + (skill ? "\n최대 체력을 10% 낮추고, 실명 중 빛 공격의 피해를 20% 높입니다. 초월 웨이브에서는 적 전체를 처형합니다." : ""); break;
                case TowerEmblem.Darkness: effect = who + $"침식 {SupportValue(skill, "erosion", "침식 부여량", "", U(DarknessDebuffRule.BronzeErosion), extra: "적의 침식 스택과 공통 침식 카운트에 같은 양을 더합니다.")}을 쌓고, 같은 양의 침식 카운트를 얻습니다." + (skill ? "\n침식된 적의 받는 피해가 10% 증가하며, 어둠 타워의 공격이 같은 칸의 적에게 침식을 1씩 추가합니다." : ""); break;
                default: effect = who + $"{SupportValue(skill, "amount", "디버프 적용량", extra: resistance)}의 디버프를 부여합니다."; break;
            }
            bool timed = skill || m_definition.Emblem == TowerEmblem.Sword || m_definition.Emblem == TowerEmblem.Bow || m_definition.Emblem == TowerEmblem.Spear || m_definition.Emblem == TowerEmblem.Shield || m_definition.Emblem == TowerEmblem.Hammer || m_definition.Emblem == TowerEmblem.Earth || m_definition.Emblem == TowerEmblem.Wind;
            return effect + (timed ? $"\n상태 효과는 {SkillFormula.EffectActions}회 행동 동안 유지됩니다." : "");
        }
    }
}
