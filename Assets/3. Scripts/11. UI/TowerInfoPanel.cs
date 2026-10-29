using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SquareTD
{
    // 타워 정보 카드
    public class TowerInfoPanel : MonoBehaviour
    {
        [Header("수집형 정보 카드")]
        [SerializeField] private TowerInfoCardView cardView;
        [Header("타워 정보")]
        [SerializeField] TextMeshProUGUI txt_level;
        [SerializeField] TextMeshProUGUI txt_name;
        [SerializeField] TextMeshProUGUI txt_tier;
        [SerializeField] TextMeshProUGUI txt_color;
        [SerializeField] TextMeshProUGUI txt_emblem;
        [SerializeField] Image img_tier;
        [SerializeField] Image img_color;
        [SerializeField] Image img_emblem;

        [Header("타겟 우선순위")]
        [SerializeField] TextMeshProUGUI txt_targetPriority;
        [SerializeField] Button btn_previousTarget;
        [SerializeField] Button btn_nextTarget;

        [Header("스탯")]
        [SerializeField] TextMeshProUGUI txt_power;
        [SerializeField] TextMeshProUGUI txt_range;
        [SerializeField] TextMeshProUGUI txt_action;
        [SerializeField] TextMeshProUGUI txt_attackCount;
        [SerializeField] TextMeshProUGUI txt_criticalRate;
        [SerializeField] TextMeshProUGUI txt_criticalDamage;
        [SerializeField] TextMeshProUGUI txt_abilityValue;
        [SerializeField] TextMeshProUGUI txt_duration;
        [SerializeField] TextMeshProUGUI txt_mastery;
        [SerializeField] TextMeshProUGUI txt_stack;

        [Header("설명")]
        [SerializeField] TextMeshProUGUI txt_default;
        [SerializeField] TextMeshProUGUI txt_skill;

        [Header("합성 및 판매")]
        [SerializeField] Button btn_combineColor;
        [SerializeField] Button btn_combineEmblem;
        [SerializeField] Button btn_combineExact;
        [SerializeField] Button btn_sellTower;
        [SerializeField] TextMeshProUGUI txt_sellTowerValue;

        [Header("스택 소모와 공격할 칸")]
        [SerializeField] Button btn_consumeStack;
        [SerializeField] Button btn_pickAttackCell;

        [Header("카드 앞·뒤 (2026-10-28) — 앞: 스탯 칸·스킬 칸의 수치 / 뒤: 기본 공격·스킬 설명")]
        [SerializeField] GameObject m_front;
        [SerializeField] GameObject m_back;
        [Tooltip("누르면 카드가 뒤집히는 자리 (앞·뒤의 스탯 칸·스킬 칸 바탕)")]
        [SerializeField] Graphic[] m_flipAreas = Array.Empty<Graphic>();
        [Tooltip("스킬 칸 제목 (앞·뒤): \"보이드 슬래시 · 액티브\"")]
        [SerializeField] TextMeshProUGUI[] txt_skillTitles = Array.Empty<TextMeshProUGUI>();
        [Tooltip("타워 그림 (타워 아틀라스의 일러스트)")]
        [SerializeField] Image img_portrait;

        private const string UnusedStatText = "-";

        private static readonly TargetPriority[] s_targetPriorityOrder =
        {
            TargetPriority.Closest,
            TargetPriority.Farthest,
            TargetPriority.MostHP,
            TargetPriority.LeastHP,
            TargetPriority.MostCrowded,
            TargetPriority.Boss
        };

        private TowerController m_selectedTower;
        private bool m_subscribed;

        private void OnEnable()
        {
            BindCardView();
            if (cardView != null) cardView.ResetToFront();
            ShowFront();
            Subscribe();

            if (UIManager.Instance != null) m_selectedTower = UIManager.Instance.SelectedTower;
            WatchStack(m_selectedTower);
            UpdateTowerInfo();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void Start()
        {
            BindCardView();

            if (btn_previousTarget != null) btn_previousTarget.onClick.AddListener(HandlePreviousTargetClick);
            if (btn_nextTarget != null) btn_nextTarget.onClick.AddListener(HandleNextTargetClick);
            foreach (Button merge in new[] { btn_combineColor, btn_combineEmblem, btn_combineExact })
                if (merge != null) merge.gameObject.SetActive(false);
            if (btn_sellTower != null) btn_sellTower.onClick.AddListener(HandleSellTowerClick);
            if (btn_consumeStack != null) btn_consumeStack.onClick.AddListener(HandleConsumeStackClick);
            if (btn_pickAttackCell != null) btn_pickAttackCell.onClick.AddListener(HandlePickAttackCellClick);
            foreach (Graphic area in m_flipAreas) MakeFlipButton(area);

            Subscribe();
        }

        private void MakeFlipButton(Graphic area)
        {
            if (area == null) return;
            area.raycastTarget = true;
            Button button = area.GetComponent<Button>();
            if (button == null) button = area.gameObject.AddComponent<Button>();
            button.targetGraphic = area;
            button.onClick.AddListener(FlipCard);
        }

        private void FlipCard()
        {
            if (m_front == null || m_back == null) return;
            bool toBack = m_front.activeSelf;
            CardFlip.Flip(transform, () =>
            {
                m_front.SetActive(!toBack);
                m_back.SetActive(toBack);
            });
        }

        private void ShowFront()
        {
            DG.Tweening.DOTween.Kill(transform, true);
            transform.localRotation = Quaternion.identity;
            if (m_front != null) m_front.SetActive(true);
            if (m_back != null) m_back.SetActive(false);
        }

        private void OnDestroy()
        {
            Unsubscribe();

            if (btn_previousTarget != null) btn_previousTarget.onClick.RemoveListener(HandlePreviousTargetClick);
            if (btn_nextTarget != null) btn_nextTarget.onClick.RemoveListener(HandleNextTargetClick);
            if (btn_sellTower != null) btn_sellTower.onClick.RemoveListener(HandleSellTowerClick);
            if (btn_consumeStack != null) btn_consumeStack.onClick.RemoveListener(HandleConsumeStackClick);
            if (btn_pickAttackCell != null) btn_pickAttackCell.onClick.RemoveListener(HandlePickAttackCellClick);
        }

        private void Subscribe()
        {
            if (m_subscribed || UIManager.Instance == null || TurnManager.Instance == null || TowerManager.Instance == null) return;

            UIManager.Instance.SelectedTowerChanged += HandleTowerSelected;
            TurnManager.Instance.StateChanged += HandleStateChanged;
            TurnManager.Instance.EnemyTurnEnded += HandleEnemyTurnEnded;
            TowerManager.Instance.TowerAdded += HandleBoardChanged;
            TowerManager.Instance.TowerRemoved += HandleBoardChanged;
            TowerManager.Instance.ColorLevels.LevelChanged += HandleColorLevelChanged;
            m_subscribed = true;
        }

        private void Unsubscribe()
        {
            WatchStack(null);
            if (!m_subscribed) return;

            if (UIManager.Instance != null) UIManager.Instance.SelectedTowerChanged -= HandleTowerSelected;
            if (TurnManager.Instance != null)
            {
                TurnManager.Instance.StateChanged -= HandleStateChanged;
                TurnManager.Instance.EnemyTurnEnded -= HandleEnemyTurnEnded;
            }
            if (TowerManager.Instance != null)
            {
                TowerManager.Instance.TowerAdded -= HandleBoardChanged;
                TowerManager.Instance.TowerRemoved -= HandleBoardChanged;
                TowerManager.Instance.ColorLevels.LevelChanged -= HandleColorLevelChanged;
            }
            m_subscribed = false;
        }

        private void HandleBoardChanged(TowerController tower) => UpdateTowerInfo();

        private void HandleColorLevelChanged(TowerColor color, int level) => UpdateTowerInfo();

        private void HandleTowerSelected(TowerController tower)
        {
            if (m_selectedTower != tower && cardView != null) cardView.ResetToFront();
            if (m_selectedTower != tower) ShowFront();
            m_selectedTower = tower;
            WatchStack(tower);
            UpdateTowerInfo();
        }

        private void HandleStateChanged(ITurnState state) => UpdateTowerInfo();

        private void HandleEnemyTurnEnded(int turn) => UpdateTowerInfo();

        private bool CanPlayerAct => TurnManager.Instance != null && TurnManager.Instance.Current.CanPlayerAct;

        private void UpdateTowerInfo()
        {
            if (m_selectedTower == null) return;

            TowerDefinition definition = m_selectedTower.Definition;
            TowerFinalStats stats = m_selectedTower.Stats;
            int towerId = definition.Id;

            int colorLevel = stats.ColorLevel;
            if (txt_level != null) txt_level.text = cardView != null ? $"Lv.{colorLevel}" : $"{colorLevel}";
            if (txt_name != null) txt_name.text = EmblemNames.GetConceptName(towerId);

            UpdateIdentityText(definition);
            UpdateIdentityImage(definition);
            UpdateTargetPriorityText();
            UpdateStatText(definition, stats);
            UpdateDescriptionText(definition, stats);
            UpdateButtons();
        }

        private void UpdateIdentityText(TowerDefinition definition)
        {
            int tier = definition.Tier;
            bool isSynergy = TowerId.IsSynergy(definition.Id);

            if (txt_tier != null) txt_tier.text = isSynergy ? "시너지" : GetTierName(tier);
            if (txt_color != null) txt_color.text = isSynergy ? "시너지" : EmblemNames.GetColorName(definition.Color);
            if (txt_emblem != null) txt_emblem.text = isSynergy ? "-" : EmblemNames.GetEmblemName(definition.Emblem);
            if (txt_sellTowerValue != null) txt_sellTowerValue.text = isSynergy || tier < 1 ? "-" : $"{GetSellGem(tier)}";
        }

        private static string GetTierName(int tier) => tier >= 1 && tier <= 5 ? EmblemNames.GetTierName(tier) : "-";

        private static int GetSellGem(int tier) => Mathf.RoundToInt(Mathf.Pow(3f, tier - 1));

        private void UpdateIdentityImage(TowerDefinition definition)
        {
            TowerVisual towerVisual = m_selectedTower.GetComponentInChildren<TowerVisual>(true);
            TowerIcons icons = TowerIcons.Instance;
            bool synergy = TowerId.IsSynergy(definition.Id);
            Sprite emblemSprite = icons != null && !synergy ? icons.GetEmblem(definition.Emblem) : null;
            Sprite tierSprite = synergy ? (towerVisual != null ? towerVisual.DecorationSprite : null)
                                        : icons != null ? icons.GetTier(definition.Tier) : null;
            Color colorTint = icons != null ? icons.GetColor(definition.Color) : Color.white;

            if (cardView != null)
            {
                int tier = definition.Tier;
                int attackIndex = synergy ? -1 : (int)definition.Color - 1;
                cardView.SetArtwork(
                    towerVisual != null ? towerVisual.IllustrationSprite : null,
                    emblemSprite,
                    tierSprite,
                    towerVisual != null ? towerVisual.TierColor : Color.white,
                    (int)definition.Emblem, tier, attackIndex,
                    "스킬공격");
                return;
            }

            if (img_portrait != null)
            {
                img_portrait.sprite = towerVisual != null ? towerVisual.IllustrationSprite : null;
                img_portrait.preserveAspect = true;
                img_portrait.enabled = img_portrait.sprite != null;
            }
            UpdateImage(img_tier, tierSprite);
            if (img_tier != null && towerVisual != null) img_tier.color = towerVisual.TierColor;
            UpdateImage(img_color, towerVisual != null ? towerVisual.ColorSprite : null);
            UpdateImage(img_emblem, emblemSprite);
            if (img_color != null) img_color.color = colorTint;
            if (img_emblem != null) img_emblem.color = colorTint;
        }

        private static void UpdateImage(Image image, Sprite sprite)
        {
            if (image == null) return;
            image.sprite = sprite;
            image.color = Color.white;
            image.enabled = sprite != null;
        }

        private void UpdateTargetPriorityText()
        {
            bool usesPriority = m_selectedTower.IsAttackTower;
            bool canChange = usesPriority && CanPlayerAct;

            if (btn_previousTarget != null) btn_previousTarget.interactable = canChange;
            if (btn_nextTarget != null) btn_nextTarget.interactable = canChange;
            if (txt_targetPriority != null)
            {
                int attackCell = m_selectedTower.State.AttackCellIndex;
                string priority = GetTargetPriorityName(m_selectedTower.State.TargetPriority);
                txt_targetPriority.text = !usesPriority ? UnusedStatText
                    : attackCell >= 0 ? $"{priority} · {attackCell}번 칸 먼저" : priority;
            }
        }

        private void UpdateStatText(TowerDefinition definition, TowerFinalStats stats)
        {
            bool isSupport = !m_selectedTower.IsAttackTower;

            SetText(txt_power, $"{stats.Power:0.##}{GetBonusText(stats, StatType.Power, 1f, "")}");
            SetText(txt_range, isSupport ? UnusedStatText : $"{stats.Range}칸");
            SetText(txt_action, $"{m_selectedTower.State.RemainingAction} / {stats.Action}");
            SetText(txt_attackCount, isSupport ? $"{stats.AttackCount}스택" : $"{stats.AttackCount}회");
            SetText(txt_criticalRate, isSupport ? UnusedStatText : $"{stats.CriticalRate * 100f:0.##}%{GetBonusText(stats, StatType.CritRate, 100f, "%")}");
            SetText(txt_criticalDamage, isSupport ? UnusedStatText : $"{CriticalRoll.Multiplier(stats.CriticalDamage) * 100f:0.##}%");
            SetText(txt_abilityValue, cardView != null ? $"x{stats.AbilityValue:0.##}" : $"×{stats.AbilityValue:0.##}");
            SetText(txt_duration, GetDurationText(definition.Color, stats, m_selectedTower.State));
            SetText(txt_mastery, GetMasteryText(definition.Color, stats));
            SetText(txt_stack, GetStackText(isSupport, stats));
        }

        private static string GetBonusText(TowerFinalStats stats, StatType stat, float scale, string unit)
        {
            float bonus = (stats.Get(stat) - stats.GetBase(stat)) * scale;
            return Mathf.Abs(bonus) < 0.005f ? "" : $" ({bonus:+0.##;-0.##}{unit})";
        }

        private static string GetReceivedBuffText(TowerFinalStats stats)
        {
            var bySource = new Dictionary<string, List<string>>();
            var order = new List<string>();
            foreach (StatSource source in stats.Sources)
            {
                if (!bySource.TryGetValue(source.Source, out List<string> parts))
                {
                    bySource[source.Source] = parts = new List<string>();
                    order.Add(source.Source);
                }
                parts.Add(FormatSource(source));
            }

            var lines = new List<string>();
            foreach (string name in order) lines.Add($"{name}: {string.Join(", ", bySource[name])}");
            return string.Join("\n", lines);
        }

        private static string FormatSource(StatSource source) => source.Stat switch
        {
            StatType.Power => $"공격력 {source.Amount:+0.##;-0.##}",
            StatType.CritRate => $"치명 확률 {source.Amount * 100f:+0.##;-0.##}%",
            StatType.CritDamage => $"치명 피해 {source.Amount * 100f:+0.##;-0.##}%",
            StatType.FinalDamage => $"최종 피해 {source.Amount * 100f:+0.##;-0.##}%",
            StatType.AttackCount => $"공격 횟수 {source.Amount:+0;-0}",
            StatType.Mastery => $"Mastery {source.Amount:+0;-0}",
            StatType.ActionGainPerTurn => $"행동 카운트 {source.Amount:+0;-0}",
            _ => $"{source.Stat} {source.Amount:+0.##;-0.##}",
        };

        private static void SetText(TextMeshProUGUI text, string value)
        {
            if (text != null) text.text = value;
        }

        private static string GetDurationText(TowerColor color, TowerFinalStats stats, TowerState state) => color switch
        {
            TowerColor.Red or TowerColor.Blue => $"{state.SkillHitCount} / {stats.Duration}회",
            TowerColor.Purple => $"{stats.Duration}회 행동",
            _ => UnusedStatText
        };

        private static string GetMasteryText(TowerColor color, TowerFinalStats stats)
        {
            switch (color)
            {
                case TowerColor.Red:
                    int side = Mathf.Max(1, stats.Mastery) * 2 - 1;
                    return $"{side}x{side}";
                case TowerColor.Blue:
                    return $"{TargetAction.GetShotsPerAttack(stats.Mastery)}발";
                case TowerColor.Yellow:
                case TowerColor.Purple:
                    return $"존 {stats.Mastery}개";
                default:
                    return UnusedStatText;
            }
        }

        private string GetStackText(bool isSupport, TowerFinalStats stats)
        {
            if (isSupport) return $"{stats.Stack}";
            TowerStack stack = m_selectedTower.State.Stack;
            return $"{stack.Current:0.#} / {stack.Max}";
        }

        private void UpdateDescriptionText(TowerDefinition definition, TowerFinalStats stats)
        {
            switch (definition.Color)
            {
                case TowerColor.Red:
                    int side = Mathf.Max(1, stats.Mastery) * 2 - 1;
                    SetText(txt_default, $"사거리 {stats.Range}칸 안의 적을 노려\n{side}x{side} 범위에 {stats.Power:0.##} 피해 × {stats.AttackCount}발");
                    break;
                case TowerColor.Blue:
                    int shots = stats.AttackCount * TargetAction.GetShotsPerAttack(stats.Mastery);
                    SetText(txt_default, $"사거리 {stats.Range}칸 안의 적 하나에게\n{stats.Power:0.##} 피해 × {shots}발");
                    break;
                case TowerColor.Yellow:
                    SetText(txt_default, $"존 {stats.Mastery}개에 스택 {stats.AttackCount}씩\n존 위의 타워에게 버프");
                    break;
                case TowerColor.Purple:
                    SetText(txt_default, $"존 {stats.Mastery}개에 스택 {stats.AttackCount}씩\n존을 밟은 적에게 디버프");
                    break;
                default:
                    SetText(txt_default, "시너지 타워 (효과 미정)");
                    break;
            }

            string buffs = m_selectedTower.IsAttackTower ? GetReceivedBuffText(stats) : "";
            if (buffs.Length > 0 && txt_default != null) txt_default.text += $"\n\n받는 버프\n{buffs}";

            SetText(txt_skill, SkillDescriptions.GetDescription(definition, stats));
            foreach (TextMeshProUGUI title in txt_skillTitles) SetText(title, SkillDescriptions.GetTitle(definition));
        }

        private void UpdateButtons()
        {
            if (btn_sellTower != null) btn_sellTower.interactable = CanPlayerAct && !TowerId.IsSynergy(m_selectedTower.Definition.Id)
                                                                 && !TowerManager.Instance.IsMergingTower(m_selectedTower);
            if (btn_consumeStack != null) btn_consumeStack.interactable = CanRequestConsume();
            if (btn_pickAttackCell != null) btn_pickAttackCell.interactable = CanPlayerAct && m_selectedTower.IsAttackTower;
        }

        private void HandlePreviousTargetClick() => ChangeTargetPriority(-1);

        private void HandleNextTargetClick() => ChangeTargetPriority(1);

        private void ChangeTargetPriority(int direction)
        {
            if (m_selectedTower == null || !m_selectedTower.IsAttackTower || !CanPlayerAct) return;

            int currentIndex = Mathf.Max(0, Array.IndexOf(s_targetPriorityOrder, m_selectedTower.State.TargetPriority));
            int nextIndex = (currentIndex + direction + s_targetPriorityOrder.Length) % s_targetPriorityOrder.Length;

            m_selectedTower.State.TargetPriority = s_targetPriorityOrder[nextIndex];
            UpdateTargetPriorityText();
        }

        private void HandleSellTowerClick()
        {
            if (m_selectedTower == null) return;
            if (TowerManager.Instance.TrySell(m_selectedTower)) CloseAfterTowerRemoved();
        }

        private bool CanRequestConsume()
        {
            if (m_selectedTower == null || !m_selectedTower.IsAttackTower) return false;
            if (TurnManager.Instance == null || !TurnManager.Instance.Current.CanConsumeStack) return false;

            TowerStack stack = m_selectedTower.State.Stack;
            return stack.IsFull && !stack.ManualRequested && TowerManager.Instance.StackModes.IsManual(m_selectedTower.Definition);
        }

        private void HandleConsumeStackClick()
        {
            if (!CanRequestConsume()) return;
            m_selectedTower.State.Stack.RequestManualConsume();
            UpdateTowerInfo();
        }

        private TowerStack m_watchedStack;

        private void WatchStack(TowerController tower)
        {
            if (m_watchedStack != null) m_watchedStack.Full -= UpdateTowerInfo;
            m_watchedStack = tower != null ? tower.State.Stack : null;
            if (m_watchedStack != null) m_watchedStack.Full += UpdateTowerInfo;
        }

        private void HandlePickAttackCellClick()
        {
            if (m_selectedTower == null || !m_selectedTower.IsAttackTower || !CanPlayerAct) return;

            BoardInputController input = FindFirstObjectByType<BoardInputController>();
            if (input != null) input.ChangeMode(new PickAttackCellMode(input, m_selectedTower));
        }

        private void CloseAfterTowerRemoved()
        {
            m_selectedTower = null;
            if (UIManager.Instance != null) UIManager.Instance.CloseTowerInfoPanel();
        }

        private static string GetTargetPriorityName(TargetPriority priority) => priority switch
        {
            TargetPriority.Closest => "가까운 적",
            TargetPriority.Farthest => "먼 적",
            TargetPriority.MostHP => "체력 많은 적",
            TargetPriority.LeastHP => "체력 적은 적",
            TargetPriority.MostCrowded => "적이 가장 많은 곳",
            TargetPriority.Boss => "보스",
            _ => "가까운 적"
        };

        private void BindCardView()
        {
            if (cardView == null) cardView = GetComponentInChildren<TowerInfoCardView>(true);
            if (cardView == null) return;
            txt_level = cardView.Level; txt_name = cardView.TowerName; txt_tier = cardView.TierName;
            txt_color = cardView.AttackName; txt_emblem = cardView.EmblemName;
            txt_power = cardView.Power; txt_range = cardView.Range; txt_action = cardView.Action;
            txt_attackCount = cardView.AttackCount; txt_criticalRate = cardView.CriticalRate; txt_criticalDamage = cardView.CriticalDamage;
            txt_abilityValue = cardView.AbilityValue; txt_duration = cardView.Duration; txt_mastery = cardView.Mastery;
            txt_default = cardView.BasicDescription; txt_skill = cardView.SkillDescription;
            txt_targetPriority = cardView.TargetPriority; btn_previousTarget = cardView.PreviousTarget; btn_nextTarget = cardView.NextTarget;
            btn_combineColor = cardView.CombineColor; btn_combineEmblem = cardView.CombineEmblem; btn_combineExact = cardView.CombineExact;
            btn_sellTower = cardView.Sell; txt_sellTowerValue = cardView.SellValue;
        }
    }
}
