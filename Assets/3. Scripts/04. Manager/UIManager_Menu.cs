using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class UIManager_Menu : MonoBehaviour
{

    [SerializeField] CanvasGroup Canvas_Menu;
    [SerializeField] Button Btn_Stage;
    [SerializeField] Button Btn_Option;
    [SerializeField] Button Btn_Quit;

    [SerializeField] string m_sceneName;


    IEnumerator Start()
    {
        if (Btn_Stage != null)
        {
            yield return CoFade(0, 1, 2f);

            Btn_Stage.onClick.RemoveAllListeners();
            Btn_Stage.onClick.AddListener(StageButtonClick);
        }
    }

    public void StageButtonClick()
    {
        SceneLoader.StartLoad(m_sceneName);
    }

    IEnumerator CoFade(float from, float to, float duration)
    {
        // CanvasGroup이 없으면 페이드가 불가능하므로 종료
        if (!Canvas_Menu)
            yield break;

        // t: 페이드가 진행된 시간(초)
        float t = 0f;

        // 페이드 중에는 입력을 막습니다.
        // (검은 덮개가 있다고 가정하고, 뒤 UI 클릭 방지)
        Canvas_Menu.blocksRaycasts = true;


        // duration이 0이면 "즉시" alpha를 바꾸고 끝냅니다.
        if (duration <= 0f)
        {
            // 목표 알파로 즉시 설정
            Canvas_Menu.alpha = to;

            // 완전 투명 (to가 거의 0)이면 입력을 풀어도 되고,
            // 불투명 (to가 0보다 크면) 입력을 막아야 함
            Canvas_Menu.blocksRaycasts = to > 0.00001f;
            yield break;
        }

        // duration 동안 매 프레임 alpha를 조금씩 바꿉니다.
        while (t < duration)
        {
            // 시간 누적(타임스케일 영향 X)
            t += Time.unscaledDeltaTime;

            // 0..1 비율로 진행률 계산
            float normalized = t / duration;

            // Lerp: from과 to사이를 normalized 비율로 보간
            float a = Mathf.Lerp(from, to, normalized);

            // CanvasGroup 알파 적용
            Canvas_Menu.alpha = a;

            // 다음 프레임까지 대기
            yield return null;
        }

        // 끝났으면 정확히 to 값으로 고정 (오차 방지)
        Canvas_Menu.alpha = to;

        // 최종 상태에 따라 입력 차단 여부 결정
        Canvas_Menu.blocksRaycasts = to > 0.00001f;
    }
}
