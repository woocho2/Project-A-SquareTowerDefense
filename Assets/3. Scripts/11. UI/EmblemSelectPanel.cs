using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SquareTD
{
    // 게임 시작 전 문양 고르기 패널
    public class EmblemSelectPanel : MonoBehaviour
    {
        [Header("모드")]
        [SerializeField] Button btn_normal;
        [SerializeField] Button btn_hard;
        [SerializeField] Button btn_ragnarok;
        [SerializeField] TextMeshProUGUI txt_mode;

        [Header("문양 (검, 도끼, 활, 창, 방패, 해머, 불, 물, 대지, 바람, 얼음, 전기, 빛, 어둠 순서)")]
        [SerializeField] Toggle[] tgl_emblems = new Toggle[14];
        [SerializeField] TextMeshProUGUI txt_count;

        [Header("시작")]
        [SerializeField] Button btn_start;
        [SerializeField] string m_sceneName_Stage = "3. Stage1";

        private const int NormalCount = 6;
        private const int HardCount = 8;
        private const int RagnarokCount = 6;

        private int m_requiredCount = NormalCount;
        private string m_modeName = "노말";

        private void Start()
        {
            if (btn_normal != null) btn_normal.onClick.AddListener(() => SelectMode("노말", NormalCount));
            if (btn_hard != null) btn_hard.onClick.AddListener(() => SelectMode("하드", HardCount));
            if (btn_ragnarok != null) btn_ragnarok.onClick.AddListener(() => SelectMode("라그나로크", RagnarokCount));
            if (btn_start != null) btn_start.onClick.AddListener(HandleStartClick);

            foreach (Toggle toggle in tgl_emblems)
            {
                if (toggle != null) toggle.onValueChanged.AddListener(HandleEmblemToggled);
            }

            SelectMode(m_modeName, m_requiredCount);
        }

        private void OnDestroy()
        {
            if (btn_normal != null) btn_normal.onClick.RemoveAllListeners();
            if (btn_hard != null) btn_hard.onClick.RemoveAllListeners();
            if (btn_ragnarok != null) btn_ragnarok.onClick.RemoveAllListeners();
            if (btn_start != null) btn_start.onClick.RemoveListener(HandleStartClick);

            foreach (Toggle toggle in tgl_emblems)
            {
                if (toggle != null) toggle.onValueChanged.RemoveListener(HandleEmblemToggled);
            }
        }

        private void SelectMode(string modeName, int requiredCount)
        {
            m_modeName = modeName;
            m_requiredCount = requiredCount;
            if (txt_mode != null) txt_mode.text = modeName;
            Refresh();
        }

        private void HandleEmblemToggled(bool isOn) => Refresh();

        private List<TowerEmblem> GetSelectedEmblems()
        {
            var selected = new List<TowerEmblem>();
            for (int i = 0; i < tgl_emblems.Length; i++)
            {
                if (tgl_emblems[i] != null && tgl_emblems[i].isOn) selected.Add((TowerEmblem)(i + 1));
            }
            return selected;
        }

        private void Refresh()
        {
            int count = GetSelectedEmblems().Count;
            if (txt_count != null) txt_count.text = $"{count} / {m_requiredCount}";
            if (btn_start != null) btn_start.interactable = count == m_requiredCount;
        }

        private void HandleStartClick()
        {
            List<TowerEmblem> emblems = GetSelectedEmblems();
            if (emblems.Count != m_requiredCount) return;

            Debug.Log($"[EmblemSelectPanel] {m_modeName} · 문양 {string.Join(", ", emblems)} (설정 넘기기는 수업 42)");
            SceneFlow.Load(m_sceneName_Stage);
        }
    }
}
