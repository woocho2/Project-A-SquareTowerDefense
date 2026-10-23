using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SquareTD
{
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
            Subscribe();

            if (UIManager.Instance != null) m_selectedTower = UIManager.Instance.SelectedTower;
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
            if (btn_combineColor != null) btn_combineColor.onClick.AddListener(HandleCombineColorClick);
            if (btn_combineEmblem != null) btn_combineEmblem.onClick.AddListener(HandleCombineEmblemClick);
            if (btn_combineExact != null) btn_combineExact.onClick.AddListener(HandleCombineExactClick);
            if (btn_sellTower != null) btn_sellTower.onClick.AddListener(HandleSellTowerClick);
            if (btn_consumeStack != null) btn_consumeStack.onClick.AddListener(HandleConsumeStackClick);
            if (btn_pickAttackCell != null) btn_pickAttackCell.onClick.AddListener(HandlePickAttackCellClick);

            Subscribe();
        }

        private void OnDestroy()
        {
            Unsubscribe();

            if (btn_previousTarget != null) btn_previousTarget.onClick.RemoveListener(HandlePreviousTargetClick);
            if (btn_nextTarget != null) btn_nextTarget.onClick.RemoveListener(HandleNextTargetClick);
            if (btn_combineColor != null) btn_combineColor.onClick.RemoveListener(HandleCombineColorClick);
            if (btn_combineEmblem != null) btn_combineEmblem.onClick.RemoveListener(HandleCombineEmblemClick);
            if (btn_combineExact != null) btn_combineExact.onClick.RemoveListener(HandleCombineExactClick);
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
            // 수업 20: ColorUpgradeState.LevelChanged += (컬러, 레벨) => UpdateTowerInfo()
            // 수업 23: TowerStack.Full += 스택 소모 버튼 켜기
            m_subscribed = true;
        }

        private void Unsubscribe()
        {
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
            }
            m_subscribed = false;
        }

        private void HandleBoardChanged(TowerController tower) => UpdateTowerInfo();   // 판의 타워가 바뀌면 합성 버튼이 켜질지 다시 봄

        private void HandleTowerSelected(TowerController tower)
        {
            if (m_selectedTower != tower && cardView != null) cardView.ResetToFront();
            m_selectedTower = tower;
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

            // 수업 20: 컬러 강화 레벨 = ColorUpgradeState.GetLevel(definition.Color)
            int colorLevel = 1;
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

            if (cardView != null)
            {
                int tier = definition.Tier;
                int attackIndex = TowerId.IsSynergy(definition.Id) ? -1 : (int)definition.Color - 1;
                cardView.SetArtwork(
                    towerVisual != null ? towerVisual.CharacterSprite : null,
                    towerVisual != null ? towerVisual.ProjectileEmblemSprite : null,
                    towerVisual != null ? towerVisual.TierSprite : null,
                    towerVisual != null ? towerVisual.TierColor : Color.white,
                    (int)definition.Emblem, tier, attackIndex,
                    "스킬공격");   // 수업 27 ~ 40: 문양별 스킬 2의 이름
                return;
            }

            UpdateImage(img_tier, towerVisual != null ? towerVisual.TierSprite : null);
            if (img_tier != null && towerVisual != null) img_tier.color = towerVisual.TierColor;
            UpdateImage(img_color, towerVisual != null ? towerVisual.ColorSprite : null);
            UpdateImage(img_emblem, towerVisual != null ? towerVisual.EmblemSprite : null);
            if (towerVisual != null && img_color != null) img_color.color = towerVisual.EmblemColor;
            if (towerVisual != null && img_emblem != null) img_emblem.color = towerVisual.EmblemColor;
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

            SetText(txt_power, $"{stats.Power:0.##}");
            SetText(txt_range, isSupport ? UnusedStatText : $"{stats.Range}칸");
            SetText(txt_action, $"{m_selectedTower.State.RemainingAction} / {stats.Action}");
            SetText(txt_attackCount, isSupport ? $"{stats.AttackCount}스택" : $"{stats.AttackCount}회");
            SetText(txt_criticalRate, isSupport ? UnusedStatText : $"{stats.CriticalRate * 100f:0.##}%");
            SetText(txt_criticalDamage, isSupport ? UnusedStatText : $"{CriticalRoll.Multiplier(stats.CriticalDamage) * 100f:0.##}%");
            SetText(txt_abilityValue, cardView != null ? $"x{stats.AbilityValue:0.##}" : $"×{stats.AbilityValue:0.##}");
            SetText(txt_duration, GetDurationText(definition.Color, stats));
            SetText(txt_mastery, GetMasteryText(definition.Color, stats));
            SetText(txt_stack, GetStackText(isSupport, stats));
        }

        private static void SetText(TextMeshProUGUI text, string value)
        {
            if (text != null) text.text = value;
        }

        private static string GetDurationText(TowerColor color, TowerFinalStats stats) => color switch
        {
            TowerColor.Red => $"{stats.Duration}턴",
            TowerColor.Blue => $"같은 적 {stats.Duration}회",
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

        private static string GetStackText(bool isSupport, TowerFinalStats stats)
        {
            // 수업 23: 공격 타워는 TowerStack의 "지금 / 최대"
            return isSupport ? $"{stats.Stack}" : $"0 / {stats.Stack}";
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

            // 수업 27 ~ 40: 문양별 스킬 2의 이름과 설명 (SkillRegistry에서)
            SetText(txt_skill, "스킬 2는 문양 수업에서");
        }

        private void UpdateButtons()
        {
            if (btn_combineColor != null) btn_combineColor.interactable = CanMerge(MergeMode.Color);
            if (btn_combineEmblem != null) btn_combineEmblem.interactable = CanMerge(MergeMode.Emblem);
            if (btn_combineExact != null) btn_combineExact.interactable = CanMerge(MergeMode.Exact);
            if (btn_sellTower != null) btn_sellTower.interactable = CanPlayerAct && !TowerId.IsSynergy(m_selectedTower.Definition.Id);
            // 수업 23: 다 찼고 수동 소모일 때만. 적 턴에도 누를 수 있음 (TurnManager.Current.CanConsumeStack)
            if (btn_consumeStack != null) btn_consumeStack.interactable = false;
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

        private readonly List<TowerController> m_mergeMaterials = new List<TowerController>();

        private bool CanMerge(MergeMode mode)
        {
            return CanPlayerAct && m_selectedTower != null
                && TowerProgression.TryFindMaterials(m_selectedTower, mode, BoardManager.Instance.Slots, m_mergeMaterials);
        }

        private void HandleCombineColorClick() => Merge(MergeMode.Color);

        private void HandleCombineEmblemClick() => Merge(MergeMode.Emblem);

        private void HandleCombineExactClick() => Merge(MergeMode.Exact);

        private void Merge(MergeMode mode)
        {
            if (m_selectedTower == null) return;
            TowerManager.Instance.TryMerge(m_selectedTower, mode);     // 성공하면 결과 타워가 선택되어 패널이 그 타워로 바뀜
        }

        private void HandleSellTowerClick()
        {
            if (m_selectedTower == null) return;
            if (TowerManager.Instance.TrySell(m_selectedTower)) CloseAfterTowerRemoved();
        }

        private void HandleConsumeStackClick()
        {
            // 수업 23: m_selectedTower.Stack.RequestConsume();
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
