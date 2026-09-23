using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(EnemyHealthController))]
[RequireComponent(typeof(EnemyMoveController))]
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
        public int StackThreshold = 1;
        public int RemainingDuration = -1;
        public bool RefreshedOnCurrentTile;
        public readonly Dictionary<int, int> AppliedVersionsBySource = new Dictionary<int, int>();
    }

    private readonly Dictionary<DebuffTarget, DebuffState> m_states = new Dictionary<DebuffTarget, DebuffState>();
    private EnemyHealthController m_health;
    private EnemyMoveController m_movement;

    private void Awake()
    {
        m_health = GetComponent<EnemyHealthController>();
        m_movement = GetComponent<EnemyMoveController>();
    }

    public void ApplyZoneStack(DebuffTarget target, int tier, float abilityValue, Vector3 zonePosition, int stackThreshold = 1)
    {
        if (target == DebuffTarget.None || m_health.CurrentHP <= 0f) return;
        if (!m_states.TryGetValue(target, out DebuffState state))
        {
            state = new DebuffState();
            m_states.Add(target, state);
        }

        state.Tier = Mathf.Max(state.Tier, Mathf.Clamp(tier, 1, 5));
        state.AbilityValue = Mathf.Max(state.AbilityValue, abilityValue);
        state.StackThreshold = Mathf.Max(state.StackThreshold, stackThreshold);
        state.ZonePosition = zonePosition;
        if (target == DebuffTarget.Spear) state.ExpireTurns = -1;
        switch (target)
        {
            case DebuffTarget.Fire:
                if (!state.Triggered)
                {
                    state.Stack++;
                    if (state.Stack >= state.StackThreshold)
                    {
                        state.Stack = 0;
                        state.Triggered = true;
                    }
                }
                // 불 장판의 첫 피해는 스택이 적용된 현재 행동 턴에 즉시 보여줍니다.
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

    /// <summary>
    /// 적이 현재 서 있는 패스 타일의 디버프 데이터를 읽습니다.
    /// 같은 장판의 같은 공격 버전은 한 번만 스택을 올리고, 그 뒤로는 타일 위에 있는 동안 지속시간만 새로 고칩니다.
    /// </summary>
    public void RefreshTileDebuffs(Vector3Int currentCell)
    {
        if (TileManager.Instance == null || m_health == null || m_health.CurrentHP <= 0f) return;

        IReadOnlyList<PathTileDebuffEffect> effects = TileManager.Instance.GetPathDebuffEffectsAt(currentCell);
        for (int i = 0; i < effects.Count; i++)
        {
            PathTileDebuffEffect effect = effects[i];
            if (effect.Target == DebuffTarget.None) continue;

            // 장판이 막 배치된 상태(ApplicationVersion 0)는 UI 표기용입니다.
            // 디버프 타워가 실제 행동한 뒤에만 적 스택과 duration 갱신을 시작합니다.
            if (effect.ApplicationVersion <= 0) continue;

            bool isNewApplication = !m_states.TryGetValue(effect.Target, out DebuffState state) ||
                !state.AppliedVersionsBySource.TryGetValue(effect.SourceID, out int appliedVersion) ||
                appliedVersion != effect.ApplicationVersion;

            if (isNewApplication)
            {
                ApplyZoneStack(
                    effect.Target,
                    effect.Tier,
                    effect.AbilityValue,
                    effect.ZoneWorldPosition,
                    effect.StackThreshold);
                state = m_states[effect.Target];
                state.AppliedVersionsBySource[effect.SourceID] = effect.ApplicationVersion;
            }
            else
            {
                // 장판이 계속 유지되는 동안 더 높은 티어/수치 장판으로 교체되었을 때도 즉시 최신값을 사용합니다.
                state.Tier = Mathf.Max(state.Tier, effect.Tier);
                state.AbilityValue = Mathf.Max(state.AbilityValue, effect.AbilityValue);
                state.StackThreshold = Mathf.Max(state.StackThreshold, effect.StackThreshold);
                state.ZonePosition = effect.ZoneWorldPosition;
            }

            if (effect.Target != DebuffTarget.Fire)
            {
                state.RemainingDuration = Mathf.Max(state.RemainingDuration, effect.Duration);
            }
            state.RefreshedOnCurrentTile = true;
        }
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
        List<DebuffTarget> expiredTargets = new List<DebuffTarget>();
        foreach (KeyValuePair<DebuffTarget, DebuffState> pair in m_states)
        {
            DebuffState state = pair.Value;
            state.TurnCount++;

            // 타일 위에 있으면 RefreshTileDebuffs가 매 적 턴 남은 시간을 다시 duration으로 맞춥니다.
            // 타일을 벗어난 뒤부터만 1턴씩 줄어듭니다.
            if (pair.Key != DebuffTarget.Fire && !state.RefreshedOnCurrentTile && state.RemainingDuration > 0)
            {
                state.RemainingDuration--;
                if (state.RemainingDuration <= 0)
                {
                    expiredTargets.Add(pair.Key);
                    continue;
                }
            }

            // 아래 창 전용 2턴 처리는 레거시 직접 적용 경로(RemainingDuration < 0)와의 호환을 위해서만 유지합니다.
            if (pair.Key == DebuffTarget.Spear && state.RemainingDuration < 0 && state.ExpireTurns < 0 &&
                m_movement.CurrentTileIndex != m_movement.GetPathIndexClosestTo(state.ZonePosition))
            {
                state.ExpireTurns = 2;
            }
            if (pair.Key == DebuffTarget.Spear && state.RemainingDuration < 0 && state.ExpireTurns > 0)
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
                    float fireDamagePercent = state.AbilityValue * (state.Triggered ? 2f : Mathf.Max(1, state.Stack));
                    m_health.ApplyDamage(m_health.MaxHP * fireDamagePercent / 100f);
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
            state.RefreshedOnCurrentTile = false;
        }

        foreach (DebuffTarget target in expiredTargets) m_states.Remove(target);
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
        // 만료된 스택이 남긴 영구 보정이 유지되지 않도록, 매번 현재 활성 상태만으로 다시 계산합니다.
        m_movement.SetPermanentActionPenalty(0);
        m_movement.SetPermanentDiceMaxModifier(0);

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

        bool isIgnited = m_states.TryGetValue(DebuffTarget.Fire, out DebuffState fire) && fire.Triggered;
        m_health.SetHealReceivedMultiplier(isIgnited ? .6f : 1f);

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

    /// <summary>Fire meteors deal extra damage to Burned and Ignited enemies.</summary>
    public float GetFireMeteorDamageMultiplier()
    {
        if (!m_states.TryGetValue(DebuffTarget.Fire, out DebuffState fire)) return 1f;
        if (fire.Triggered) return 4f;
        return fire.Stack > 0 ? 2f : 1f;
    }

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
            m_health.SetHealReceivedMultiplier(1f);
        }
    }

    /// <summary>현재 적에게 적용 중인 모든 디버프의 상세 설명 목록을 반환합니다. (UI 표시용)</summary>
    public List<string> GetActiveDebuffDescriptions()
    {
        List<string> result = new List<string>();
        foreach (KeyValuePair<DebuffTarget, DebuffState> pair in m_states)
        {
            DebuffTarget target = pair.Key;
            DebuffState state = pair.Value;
            string targetName = GetDebuffTargetName(target);
            string durationText = state.RemainingDuration > 0 ? $" · 지속 {state.RemainingDuration}턴" : "";

            string detail = target switch
            {
                DebuffTarget.Fire => state.Triggered
                    ? $"티어 {state.Tier} · [점화] 매 턴 폭발 피해, 받는 치유 40% 감소"
                    : $"티어 {state.Tier} · 화상 {state.Stack}/{state.StackThreshold} 스택{durationText}",
                DebuffTarget.Sword => $"티어 {state.Tier} · 저주 {state.Stack}스택 (최대 체력 감소){durationText}",
                DebuffTarget.Axe => $"티어 {state.Tier} · 취약 {state.Stack}스택 (받는 피해 증가){durationText}",
                DebuffTarget.Ice => state.Triggered
                    ? $"티어 {state.Tier} · [빙결] 행동주기 +2, 받는 피해 +10%"
                    : $"티어 {state.Tier} · 냉기 {state.Stack}/5 스택{durationText}",
                DebuffTarget.Electricity => state.Triggered
                    ? $"티어 {state.Tier} · [감전] 이동 주사위 -2"
                    : $"티어 {state.Tier} · 전류 {state.Stack}/5 스택{durationText}",
                DebuffTarget.Wind => state.Triggered
                    ? $"티어 {state.Tier} · [돌풍] 다음 이동 역주행"
                    : $"티어 {state.Tier} · 바람 {state.Stack}/10 스택{durationText}",
                DebuffTarget.Shield => $"티어 {state.Tier} · [속박] 이동 불가{durationText}",
                DebuffTarget.Spear => $"티어 {state.Tier} · 처형 표식 (누적 피해 {state.PendingDamage:0})",
                DebuffTarget.Bow => $"티어 {state.Tier} · 약점 노출 (방어 무시 고정 피해 전환){durationText}",
                DebuffTarget.Earth => $"티어 {state.Tier} · 지진 (5턴마다 행동 지연 및 피해){durationText}",
                DebuffTarget.Light => $"티어 {state.Tier} · 섬광 (일정 체력 이하 즉사){durationText}",
                DebuffTarget.Darkness => $"티어 {state.Tier} · 암흑 (이동력 변화){durationText}",
                _ => $"티어 {state.Tier} · {state.Stack}스택{durationText}"
            };

            result.Add($"{targetName} 디버프\n{detail}");
        }
        return result;
    }

    private static string GetDebuffTargetName(DebuffTarget target)
    {
        return target switch
        {
            DebuffTarget.Fire => "불 (화상)",
            DebuffTarget.Sword => "검 (저주)",
            DebuffTarget.Axe => "도끼 (취약)",
            DebuffTarget.Ice => "얼음 (빙결)",
            DebuffTarget.Electricity => "전기 (감전)",
            DebuffTarget.Wind => "바람 (돌풍)",
            DebuffTarget.Shield => "방패 (속박)",
            DebuffTarget.Spear => "창 (처형)",
            DebuffTarget.Bow => "활 (약점)",
            DebuffTarget.Earth => "대지 (지진)",
            DebuffTarget.Light => "빛 (섬광)",
            DebuffTarget.Darkness => "어둠 (암흑)",
            _ => "특수"
        };
    }
}
