using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(EnemyHealthController))]
[RequireComponent(typeof(EnemyMovementController))]
public class EnemyDebuffController : MonoBehaviour
{
    private sealed class DebuffState
    {
        public int Tier;
        public int Stack;
        public int TurnCount;
        public int ExpireTurns = -1;
        public bool Triggered;
        public Vector3 ZonePosition;
        public float AbilityValue;
        public float PendingDamage;
    }

    private readonly Dictionary<DebuffTarget, DebuffState> m_states = new Dictionary<DebuffTarget, DebuffState>();
    private EnemyHealthController m_health;
    private EnemyMovementController m_movement;

    private void Awake()
    {
        m_health = GetComponent<EnemyHealthController>();
        m_movement = GetComponent<EnemyMovementController>();
    }

    public void ApplyZoneStack(DebuffTarget target, int tier, float abilityValue, Vector3 zonePosition)
    {
        if (target == DebuffTarget.None || m_health.CurrentHP <= 0f) return;
        if (!m_states.TryGetValue(target, out DebuffState state))
        {
            state = new DebuffState();
            m_states.Add(target, state);
        }

        state.Tier = Mathf.Max(state.Tier, Mathf.Clamp(tier, 1, 5));
        state.AbilityValue = Mathf.Max(state.AbilityValue, abilityValue);
        state.ZonePosition = zonePosition;
        if (target == DebuffTarget.Spear) state.ExpireTurns = -1;
        switch (target)
        {
            case DebuffTarget.Fire:
                // 불 장판의 첫 피해는 스택이 적용된 현재 행동 턴에 즉시 보여줍니다.
                m_health.ApplyDamage(m_health.MaxHP * TierValue(state.Tier, .5f, 1.5f, 4.5f, 13.5f, 40.5f) / 100f);
                break;
            case DebuffTarget.Sword:
                state.Stack = Mathf.Min(10, state.Stack + 1);
                state.Triggered = state.Stack == 10;
                break;
            case DebuffTarget.Axe:
                state.Stack = Mathf.Min(5, state.Stack + 1);
                break;
            case DebuffTarget.Ice:
            case DebuffTarget.Electricity:
                if (!state.Triggered)
                {
                    state.Stack = Mathf.Min(5, state.Stack + 1);
                    state.Triggered = state.Stack == 5;
                }
                break;
            case DebuffTarget.Wind:
                if (!state.Triggered)
                {
                    state.Stack = Mathf.Min(10, state.Stack + 1);
                    state.Triggered = state.Stack == 10;
                    if (state.Triggered) m_movement.ReverseNextDiceMove();
                }
                break;
            case DebuffTarget.Shield:
                m_movement.ForceNoMoveForTurns(2);
                break;
        }
        RefreshPersistentStats();
    }

    public void RegisterCriticalHit()
    {
        if (!m_states.TryGetValue(DebuffTarget.Spear, out DebuffState state)) return;

        state.Stack++;
        state.PendingDamage += state.AbilityValue * (TierValue(state.Tier, 5f, 10f, 20f, 50f, 100f) / 100f);
        if (m_health.CurrentHP <= state.PendingDamage)
        {
            m_health.ExecuteInstantKill();
        }
    }

    // 창 장판에서 벗어나면 누적 피해를 2 적 턴 뒤에 적용합니다.
    public void EndZoneDebuff(DebuffTarget target)
    {
        if (target == DebuffTarget.Spear && m_states.TryGetValue(target, out DebuffState state))
        {
            state.ExpireTurns = 2;
        }
    }

    public float GetFixedDamageConversionRatio()
    {
        return m_states.TryGetValue(DebuffTarget.Bow, out DebuffState state)
            ? TierValue(state.Tier, .1f, .2f, .4f, 1f, 2f) / 100f : 0f;
    }

    public void AdvanceDebuffTurn()
    {
        if (m_health.CurrentHP <= 0f) return;
        m_movement.AdvanceDebuffTurn();
        foreach (KeyValuePair<DebuffTarget, DebuffState> pair in m_states)
        {
            DebuffState state = pair.Value;
            state.TurnCount++;
            if (pair.Key == DebuffTarget.Spear && state.ExpireTurns < 0 &&
                m_movement.CurrentTileIndex != m_movement.GetPathIndexClosestTo(state.ZonePosition))
            {
                state.ExpireTurns = 2;
            }
            if (pair.Key == DebuffTarget.Spear && state.ExpireTurns > 0)
            {
                state.ExpireTurns--;
                if (state.ExpireTurns == 0 && state.PendingDamage > 0f)
                {
                    m_health.ApplyDamage(state.PendingDamage);
                    state.PendingDamage = 0f;
                    state.Stack = 0;
                }
            }
            switch (pair.Key)
            {
                case DebuffTarget.Fire:
                    m_health.ApplyDamage(m_health.MaxHP * TierValue(state.Tier, .5f, 1.5f, 4.5f, 13.5f, 40.5f) / 100f);
                    break;
                case DebuffTarget.Earth:
                    if (state.TurnCount % 5 == 0)
                    {
                        m_movement.ApplyTemporaryActionPenalty(3, 1);
                        m_health.ApplyDamage(m_health.MaxHP * TierValue(state.Tier, .5f, 1f, 2f, 4f, 8f) / 100f);
                    }
                    break;
                case DebuffTarget.Light:
                    if (m_health.CurrentHP / m_health.MaxHP <= TierValue(state.Tier, 1f, 2f, 5f, 10f, 20f) / 100f) m_health.ExecuteInstantKill();
                    break;
                case DebuffTarget.Darkness:
                    ApplyDarknessMovementModifier(state);
                    break;
            }
            if (m_health.CurrentHP <= 0f) break;
        }
        RefreshPersistentStats();
    }

    private void ApplyDarknessMovementModifier(DebuffState state)
    {
        int zoneIndex = m_movement.GetPathIndexClosestTo(state.ZonePosition);
        int indexDifference = m_movement.CurrentTileIndex - zoneIndex;
        int affectedRange = state.Tier switch
        {
            1 => 2,
            2 => 3,
            3 => 5,
            4 => 7,
            _ => 10
        };

        // 장판과 같은 칸은 영향이 없으며, 범위 밖 적도 이동력 변화가 없습니다.
        if (indexDifference == 0 || Mathf.Abs(indexDifference) > affectedRange) return;

        if (indexDifference < 0)
        {
            // 장판보다 낮은 인덱스(뒤쪽) 적은 이동력 +1
            m_movement.ApplyTemporaryDiceMaxModifier(1, 1);
            return;
        }

        // 장판보다 높은 인덱스(앞쪽) 적은 티어에 따라 이동력이 감소합니다.
        int movementReduction = state.Tier switch
        {
            4 => 2,
            5 => 3,
            _ => 1
        };
        m_movement.ApplyTemporaryDiceMaxModifier(-movementReduction, 1);
    }

    private void RefreshPersistentStats()
    {
        float curse = 0f;
        if (m_states.TryGetValue(DebuffTarget.Sword, out DebuffState sword)) curse = TierValue(sword.Tier, 1f, 2f, 3f, 4f, 5f) * sword.Stack;
        m_health.SetMaxHealthReductionPercent(curse);

        float vulnerability = 1f;
        if (m_states.TryGetValue(DebuffTarget.Axe, out DebuffState axe)) vulnerability += TierValue(axe.Tier, .5f, 1f, 2f, 4f, 8f) * axe.Stack / 100f;
        if (m_states.TryGetValue(DebuffTarget.Ice, out DebuffState ice) && ice.Triggered) vulnerability *= 1.1f;
        m_health.SetVulnerability(vulnerability);

        float defenseMultiplier = 1f;
        if (m_states.TryGetValue(DebuffTarget.Hammer, out DebuffState hammer)) defenseMultiplier -= TierValue(hammer.Tier, 5f, 10f, 15f, 20f, 25f) / 100f;
        m_health.SetDefendMultiplier(defenseMultiplier);

        if (m_states.TryGetValue(DebuffTarget.Ice, out ice) && ice.Triggered) m_movement.SetPermanentActionPenalty(2);
        if (m_states.TryGetValue(DebuffTarget.Electricity, out DebuffState electricity) && electricity.Triggered) m_movement.SetPermanentDiceMaxModifier(-2);
    }

    private static float TierValue(int tier, float one, float two, float three, float four, float five)
    {
        switch (Mathf.Clamp(tier, 1, 5))
        {
            case 1: return one;
            case 2: return two;
            case 3: return three;
            case 4: return four;
            default: return five;
        }
    }

    public void AddDebuff(DebuffBase _) { }

    public void ClearAllDebuffs()
    {
        StopAllCoroutines();
        m_states.Clear();
        if (m_movement != null)
        {
            m_movement.SetSpeedMultiplier(1f);
            m_movement.SetPermanentActionPenalty(0);
            m_movement.SetPermanentDiceMaxModifier(0);
        }
        if (m_health != null)
        {
            m_health.SetMaxHealthReductionPercent(0f);
            m_health.SetVulnerability(1f);
            m_health.SetDefendMultiplier(1f);
        }
    }
}
