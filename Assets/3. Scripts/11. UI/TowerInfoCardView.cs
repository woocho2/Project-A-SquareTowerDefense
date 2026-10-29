using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SquareTD
{
    // 수집형 타워 카드 화면 (아직 프리팹에 없음)
    public sealed class TowerInfoCardView : MonoBehaviour
    {
        [Header("카드 면")]
        [SerializeField] private RectTransform flipRoot;
        [SerializeField] private GameObject front;
        [SerializeField] private GameObject back;
        [SerializeField] private GameObject basicPage;
        [SerializeField] private GameObject skillPage;
        [SerializeField] private Button basicButton;
        [SerializeField] private Button skillButton;
        [SerializeField] private Button returnButton;
        [SerializeField, Min(0.05f)] private float flipDuration = 0.3f;

        [Header("타워 정보")]
        [SerializeField] private TextMeshProUGUI level;
        [SerializeField] private TextMeshProUGUI towerName;
        [SerializeField] private TextMeshProUGUI tierName;
        [SerializeField] private TextMeshProUGUI attackName;
        [SerializeField] private TextMeshProUGUI emblemName;
        [SerializeField] private TextMeshProUGUI detailTowerName;
        [SerializeField] private TextMeshProUGUI skillTitle;
        [SerializeField] private TextMeshProUGUI skillSubtitle;
        [SerializeField] private Image portrait;
        [SerializeField] private Image tierIcon;
        [SerializeField] private TowerTierBadgeGraphic fallbackTierBadge;
        [SerializeField] private Image attackIcon;
        [SerializeField] private Image emblemIcon;
        [SerializeField] private Image[] backgroundMarks;
        [SerializeField] private Sprite swordIcon;
        [SerializeField] private Sprite splashIcon;
        [SerializeField] private Sprite bronzeIcon;
        [SerializeField] private Sprite swordBackground;
        [SerializeField] private Sprite[] attackIcons;

        [Header("스탯과 설명")]
        [SerializeField] private TextMeshProUGUI power;
        [SerializeField] private TextMeshProUGUI range;
        [SerializeField] private TextMeshProUGUI action;
        [SerializeField] private TextMeshProUGUI attackCount;
        [SerializeField] private TextMeshProUGUI criticalRate;
        [SerializeField] private TextMeshProUGUI criticalDamage;
        [SerializeField] private TextMeshProUGUI abilityValue;
        [SerializeField] private TextMeshProUGUI duration;
        [SerializeField] private TextMeshProUGUI mastery;
        [SerializeField] private TextMeshProUGUI basicDescription;
        [SerializeField] private TextMeshProUGUI skillDescription;

        [Header("기존 타워 조작")]
        [SerializeField] private TextMeshProUGUI targetPriority;
        [SerializeField] private Button previousTarget;
        [SerializeField] private Button nextTarget;
        [SerializeField] private Button combineColor;
        [SerializeField] private Button combineEmblem;
        [SerializeField] private Button combineExact;
        [SerializeField] private Button sell;
        [SerializeField] private TextMeshProUGUI sellValue;

        public TextMeshProUGUI Level => level;
        public TextMeshProUGUI TowerName => towerName;
        public TextMeshProUGUI TierName => tierName;
        public TextMeshProUGUI AttackName => attackName;
        public TextMeshProUGUI EmblemName => emblemName;
        public TextMeshProUGUI Power => power;
        public TextMeshProUGUI Range => range;
        public TextMeshProUGUI Action => action;
        public TextMeshProUGUI AttackCount => attackCount;
        public TextMeshProUGUI CriticalRate => criticalRate;
        public TextMeshProUGUI CriticalDamage => criticalDamage;
        public TextMeshProUGUI AbilityValue => abilityValue;
        public TextMeshProUGUI Duration => duration;
        public TextMeshProUGUI Mastery => mastery;
        public TextMeshProUGUI BasicDescription => basicDescription;
        public TextMeshProUGUI SkillDescription => skillDescription;
        public TextMeshProUGUI TargetPriority => targetPriority;
        public Button PreviousTarget => previousTarget;
        public Button NextTarget => nextTarget;
        public Button CombineColor => combineColor;
        public Button CombineEmblem => combineEmblem;
        public Button CombineExact => combineExact;
        public Button Sell => sell;
        public TextMeshProUGUI SellValue => sellValue;
        public bool IsDetailVisible => back != null && back.activeSelf;
        public bool IsFlipping => m_flip != null && m_flip.IsActive();

        private bool m_buttonsBound;
        private Sequence m_flip;
        private bool m_showingSkill;

        private void Awake() => BindButtons();
        private void OnDisable() => ResetToFront();

        private void BindButtons()
        {
            if (m_buttonsBound) return;
            if (basicButton != null) basicButton.onClick.AddListener(ShowBasic);
            if (skillButton != null) skillButton.onClick.AddListener(ShowSkill);
            if (returnButton != null) returnButton.onClick.AddListener(ShowFront);
            m_buttonsBound = true;
        }

        private void OnDestroy()
        {
            m_flip?.Kill();
            if (!m_buttonsBound) return;
            if (basicButton != null) basicButton.onClick.RemoveListener(ShowBasic);
            if (skillButton != null) skillButton.onClick.RemoveListener(ShowSkill);
            if (returnButton != null) returnButton.onClick.RemoveListener(ShowFront);
        }

        public void ShowBasic() => Flip(true, false);
        public void ShowSkill() => Flip(true, true);
        public void ShowFront() => Flip(false, false);

        public void ResetToFront()
        {
            m_flip?.Kill();
            m_flip = null;
            if (flipRoot != null) flipRoot.localRotation = Quaternion.identity;
            SetFace(false, false);
        }

        private void Flip(bool detail, bool skill)
        {
            BindButtons();
            if (IsFlipping) return;
            if (IsDetailVisible == detail && (!detail || m_showingSkill == skill)) return;
            if (!Application.isPlaying || !isActiveAndEnabled || flipRoot == null)
            {
                SetFace(detail, skill);
                return;
            }

            float half = Mathf.Max(0.025f, flipDuration * 0.5f);
            m_flip = DOTween.Sequence()
                .Append(flipRoot.DOLocalRotate(new Vector3(0f, 90f, 0f), half).SetEase(Ease.InOutSine))
                .AppendCallback(() =>
                {
                    SetFace(detail, skill);
                    flipRoot.localRotation = Quaternion.Euler(0f, -90f, 0f);
                })
                .Append(flipRoot.DOLocalRotate(Vector3.zero, half).SetEase(Ease.InOutSine))
                .SetUpdate(true)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable);
        }

        private void SetFace(bool detail, bool skill)
        {
            m_showingSkill = skill;
            if (front != null) front.SetActive(!detail);
            if (back != null) back.SetActive(detail);
            if (basicPage != null) basicPage.SetActive(!skill);
            if (skillPage != null) skillPage.SetActive(skill);
        }

        public void SetArtwork(Sprite character, Sprite emblem, Sprite tier, Color tierColor,
            int visualEmblem, int tierIndex, int attackIndex, string skillName)
        {
            SetImage(portrait, character, Color.white);
            bool useSword = visualEmblem == 1;
            SetImage(emblemIcon, useSword && swordIcon != null ? swordIcon : emblem,
                useSword ? Color.white : new Color(0.65f, 0.42f, 0.22f));
            bool useFallbackTier = tierIndex == 6 || (tierIndex != 1 && tier == null) || (tierIndex == 1 && bronzeIcon == null && tier == null);
            SetImage(tierIcon, useFallbackTier ? null : tierIndex == 1 && bronzeIcon != null ? bronzeIcon : tier,
                tierIndex == 1 ? Color.white : tierColor);
            if (fallbackTierBadge != null)
            {
                fallbackTierBadge.enabled = useFallbackTier;
                fallbackTierBadge.color = tierIndex switch
                {
                    2 => new Color(.77f, .85f, .92f), 3 => new Color(1f, .78f, .27f),
                    4 => new Color(.56f, .91f, .80f), 5 => new Color(.32f, .80f, 1f),
                    6 => new Color(.74f, .54f, .92f), _ => new Color(.78f, .46f, .24f)
                };
            }
            Sprite attack = attackIndex == 0 ? splashIcon :
                attackIcons != null && attackIndex >= 0 && attackIndex < attackIcons.Length ? attackIcons[attackIndex] : null;
            SetImage(attackIcon, attack, Color.white);
            Sprite watermark = useSword ? swordBackground : emblem;
            if (backgroundMarks != null)
            {
                for (int i = 0; i < backgroundMarks.Length; i++)
                    SetImage(backgroundMarks[i], watermark, new Color(0.62f, 0.45f, 0.25f, i == 0 ? 0.16f : 0.12f));
            }
            if (detailTowerName != null) detailTowerName.text = towerName != null ? towerName.text : "";
            if (skillTitle != null) skillTitle.text = skillName;
            if (skillSubtitle != null) skillSubtitle.text = skillName == "스킬공격" ? "상세 보기" : skillName;
        }

        private static void SetImage(Image image, Sprite sprite, Color color)
        {
            if (image == null) return;
            image.sprite = sprite;
            image.color = color;
            image.enabled = sprite != null;
        }
    }
}
