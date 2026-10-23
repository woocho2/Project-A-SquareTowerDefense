using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SquareTD
{
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
            // 수업 42: 라그나로크의 문양 규칙(고정 문양 등)과 모드별 해금 상태를 20번 9장대로
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

            // 수업 42: StartSettings(씬을 건너는 static 설정)에 모드·스테이지·고른 문양을 적고 스테이지 씬으로
            //   StartSettings.Set(모드, 스테이지, emblems);
            //   GameSession과 TowerManager가 Awake에서 StartSettings를 읽음 (지금은 인스펙터의 m_mode·m_selectedEmblems)
            Debug.Log($"[EmblemSelectPanel] {m_modeName} · 문양 {string.Join(", ", emblems)} (설정 넘기기는 수업 42)");
            SceneFlow.Load(m_sceneName_Stage);
        }
    }
}
