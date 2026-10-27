using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SquareTD
{
    public class ZoneInfoPanel : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI txt_title;
        [SerializeField] TextMeshProUGUI txt_owner;
        [SerializeField] TextMeshProUGUI txt_stack;
        [SerializeField] TextMeshProUGUI txt_stage;
        [SerializeField] TextMeshProUGUI txt_description;
        [SerializeField] Button btn_close;

        private int m_pathIndex = -1;
        private int m_spawnIndex = -1;

        private void Start()
        {
            if (btn_close != null) btn_close.onClick.AddListener(HandleCloseClick);
        }

        private void OnDestroy()
        {
            if (btn_close != null) btn_close.onClick.RemoveListener(HandleCloseClick);
        }

        private void OnEnable()
        {
            // 수업 26: ZoneRegistry.ZoneChanged += HandleZoneChanged
        }

        private void OnDisable()
        {
            // 수업 26: ZoneRegistry.ZoneChanged -= HandleZoneChanged
        }

        public void OpenForTile(int pathIndex, int spawnIndex)
        {
            m_pathIndex = pathIndex;
            m_spawnIndex = spawnIndex;
            gameObject.SetActive(true);
            Refresh();
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void Refresh()
        {
            bool isBuffZone = m_spawnIndex >= 0;
            if (txt_title != null) txt_title.text = isBuffZone ? $"버프존 (스폰 {m_spawnIndex}번 칸)" : $"디버프존 (길 {m_pathIndex}번 칸)";

            // 수업 26: 한 칸에 존이 여럿(문양이 다르면 3개까지)이면 줄마다 하나씩
            //   foreach (Zone zone in 이 칸의 존들)
            //     주인 타워 이름, 문양, 스택 "지금 / 문턱" (스킬 2면 "완성"), 단계 (스킬 1 / 스킬 2 / 불길 / 충전·과충전·방전 + 남은 턴)
            //     설명 = StatusTextFormatter.Describe(zone)
            if (txt_owner != null) txt_owner.text = "-";
            if (txt_stack != null) txt_stack.text = "-";
            if (txt_stage != null) txt_stage.text = "-";
            if (txt_description != null) txt_description.text = "존은 수업 26에서";
        }

        private void HandleCloseClick()
        {
            Hide();
        }
    }
}
