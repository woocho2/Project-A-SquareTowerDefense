using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SquareTD
{
    // 스킬 설명의 수치 링크를 클릭해 계산 근거를 여는 입력
    public sealed class SkillValueLink : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private TextMeshProUGUI m_text;
        [SerializeField] private SkillDescriptionView m_view;

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.dragging || m_text == null || m_view == null) return;
            int index = TMP_TextUtilities.FindIntersectingLink(m_text, eventData.position, eventData.pressEventCamera);
            if (index >= 0) m_view.ShowValue(m_text.textInfo.linkInfo[index].GetLinkID());
        }
    }
}
