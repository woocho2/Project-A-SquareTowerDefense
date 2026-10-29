using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SquareTD
{
    // 존 정보 패널
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
        }

        private void OnDisable()
        {
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
