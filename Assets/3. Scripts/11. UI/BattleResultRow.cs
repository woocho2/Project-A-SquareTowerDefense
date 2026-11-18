using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SquareTD
{
    // 결과 목록 한 줄의 얼굴·이름·티어·누적 피해 표시
    public sealed class BattleResultRow : MonoBehaviour
    {
        [SerializeField] private Image m_portrait, m_damageFill;
        [SerializeField] private TowerTierBadgeGraphic m_badge;
        [SerializeField] private TextMeshProUGUI m_rank, m_name, m_detail, m_tier, m_damage, m_share;
        public BattleResults.Row Data { get; private set; }
        public void Bind(BattleResults.Row row, int rank, double maximum, double total)
        {
            Data = row; m_rank.text = rank.ToString("00"); m_name.text = row.Name;
            m_detail.text = row.Role + "  /  " + row.Location;
            m_tier.text = TowerId.IsSynergy(row.Id) ? "시너지" : EmblemNames.GetTierName(row.Tier);
            m_badge.color = row.TierColor; m_portrait.sprite = row.Portrait; m_portrait.enabled = row.Portrait != null;
            m_damage.text = BattleResultPanel.FormatDamage(row.Damage);
            m_share.text = (total > 0 ? row.Damage / total : 0).ToString("P1");
            m_damageFill.fillAmount = maximum > 0 ? (float)(row.Damage / maximum) : 0;
        }
    }
}
