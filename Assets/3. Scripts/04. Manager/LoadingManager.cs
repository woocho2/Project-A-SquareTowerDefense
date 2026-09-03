using System.Collections;
using TMPro;
using UnityEngine;                                // Unity 기본 기능 (MonoBehaviour, Time, Mathf 등)
using UnityEngine.SceneManagement;                // 씬 로드(SceneManager, AsyncOperation)를 쓰기 위해 필요
using UnityEngine.UI;                             // uGUI(Slider, Imange)를 쓰기 위해 필요

/// <summary>
/// 이 스크립트는 "로딩 화면"을 담당합니다.
/// - 프로그레스 바(진행률) 표시
/// - 퍼센트 텍스트 표시
/// - 스피너(빙글빙글 도는 아이콘) 회전
/// - 검은 화면 페이드 인/아웃 (CanvasGroup.alpha)
/// 
/// 또한 "테스트 모드"가 있어서:
/// - 실제 씬 로드 없이도 로딩 화면을 일정 시간 동안 강제로 보여줄 수 있습니다.
/// - UI가 잘 보이는지, 애니메이션이 자연스러운지 확인할 때 유용합니다.
/// 
/// 동작 흐름(아주 크게):
/// 1) Start 코루틴에서 페이드 인(검은 화면 -> 투명)
/// 2) 테스트 모드면: 지정 시간 동안 0%% -> 100% 연출 후 페이드 아웃
/// 3) 테스트 모드가 아니면 : 실제 비동기 씬 로드 진행률을 UI에 표시
/// </summary>

public class LoadingManager : MonoBehaviour
{
    // ------------------------------------------------------------
    // [1] 인스펙터에서 연결할 UI 참조들
    // ------------------------------------------------------------
    // Slider: 진행률 막대(0~1 값을 넣으면 바가 차오름)
    [SerializeField] Slider m_progressBar;

    // TMP_Text : "23%" 같은 텍스트 표시용 (TextMeshPro)
    [SerializeField] TMP_Text m_percentText;

    // CanvasGroup : 로딩 UI 전체를 한꺼번에 페이드(투명도) 처리할 때 사용
    // alpha : 0(투명) ~ 1(불투명)
    // blocksRaycasts: UI 클릭 같은 입력을 막을지 결정
    [SerializeField] CanvasGroup m_fadeGroup;
    [SerializeField] Image m_image;
    [SerializeField] Image m_LoadingImage1;
    [SerializeField] Image m_LoadingImage2;

    // ------------------------------------------------------------
    // [3] 내부에서 쓰는 표시용 진행값
    // ------------------------------------------------------------
    // 실제 로딩 progress는 뚝뚝 끊겨 보일 수 있어요
    // 그래서 "표시용 값"을 따로 두고 부드럽게 변화시키면 보기 좋아집니다.
    float m_displayed;

    ////////////////////////////////////////////////////////////////////////////////
    // Test Mode (테스트용 로딩 화면 강제 표시)
    ////////////////////////////////////////////////////////////////////////////////

    [Header("Test Mode")]
    // true면 실제 씬 로드 없이, 지정 시간만큼 로딩 연출만 합니다.
    public bool m_testMode = true;

    // 테스트용으로 로딩 화면을 표시할 총 시간(초)
    [Tooltip("테스트용으로 로딩 화면을 표시할 시간(초)")]
    public float m_testDuration = 3f;

    // 테스트가 끝난 뒤 NextScene이 있으면 실제로 씬 이동까지 자동으로 할지
    [Tooltip("테스트 종료 후 SceneLoader.NextScene이 설정되어 있으면 실제 씬 로드를 수행합니다.")]
    public bool m_autoLoadNextScene = false;

    /// <summary>
    /// Awake
    /// - 씬이 시작될 때 가장 먼저 호출되는 초기화 함수입니다.
    /// - 인스펙터에 UI 참조를 안 넣었을 때 (실수 방지)
    /// 자식 오브젝트들에서 자동으로 찾아서 할당합니다.
    /// </summary>

    private void Awake()
    {
        // m_progressBar가 비어 있으면(= null 이면)
        // 자식에서 Slider를 찾아서 넣어봅니다.
        if (m_progressBar == null)
            m_progressBar = GetComponentInChildren<Slider>();

        // 퍼센트 텍스트도 비어 있으면 자식에서 TMP_Text를 찾아봅니다.
        if (m_percentText == null)
            m_percentText = GetComponentInChildren<TMP_Text>();

        // 페이드용 CanvasGroup도 비어 있으면 자식에서 찾아봅니다.
        if (m_fadeGroup == null)
            m_fadeGroup = GetComponentInChildren<CanvasGroup>();
    }

    /// <summary>
    /// IEnumerator Start()
    /// - Start를 코루틴으로 만들면 "yield return"으로 흐름을 나눌 수 있습니다.
    /// - 여기서 로딩 연출 전체를 관리합니다.
    /// 
    /// 중요한 포인트 : 
    /// - Time.unscaledDeltaTime을 사용합니다.
    ///  => 게임 타임스케일(Time.timeScale)이 0(일시정지)이어도 로딩 연출이 정상 동작합니다.
    ///  </summary>

    IEnumerator Start()
    {
        // ------------------------------------------------------------
        // (A) 시작하자마자 "페이드 인" (검은 화면 -> 투명)
        // ------------------------------------------------------------
        // alpha 1: 완전 검은 덮개(불투명)
        // alpha 0: 덮개가 사라짐(투명)
        // 0.25초 동안 천천히 1 -> 0 으로 이동
        yield return CoFade(1f, 0f, 1.5f);

        // ------------------------------------------------------------
        // [B] 테스트 모드라면 : 실제 씬 로딩 없이 로딩 연출만 진행
        // ------------------------------------------------------------
        if (m_testMode)
        {
            // 안전 장치:
            // duration이 -3 같은 값이면 이상하니까 0이상으로 보정합니다.
            float duration = Mathf.Max(0f, m_testDuration);

            // 표시 진행률을 0으로 초기화 (0%에서 시작)
            m_displayed = 0f;

            // t는 "지나간 시간(초)" 누적값
            float t = 0f;

            // duration 동안 반복하며 0% -> 100%로 올립니다.
            while (t < duration)
            {
                // 이번 프레임에서 지난 시간(실제 시간)을 누적
                // unscaledDeltaTime이라 timeScale에 영향 안 받음
                t += Time.unscaledDeltaTime;

                // 진행률 = (지나간 시간 / 총 시간)
                // duration이 0에 가까울 때 나누기 오류를 막기 위해 최소값을 둠
                m_displayed = Mathf.Clamp01(t / Mathf.Max(0.0001f, duration));

                // UI 적용 (바, 퍼센트 텍스트)
                UpdateUI(m_displayed);

                // 다음 프레임까지 대기 (코루틴에서 "한 프레임 쉬기")
                yield return null;
            }

            // while을 빠져나왔으면 duration이 끝난 것
            // 혹시 반올림/오차 때문에 0.999가 될 수 있으니 확실히 1로 맞춤
            m_displayed = 1f;

            // 100% UI 적용
            UpdateUI(m_displayed);

            // 100%가 잠깐 눈에 보이도록 0.2초 유지
            // (원치 않으면 0으로 줄여도 됩니다.)
            yield return new WaitForSecondsRealtime(0.2f);

            // ------------------------------------------------------------
            // 테스트 종료 : 페이드 아웃(투명 -> 검은 덮개)
            // ------------------------------------------------------------
            yield return CoFade(0f, 1f, 0.2f);

            // ------------------------------------------------------------
            // (선택) 테스트 끝나고 자동으로 실제 씬 로드할지
            // ------------------------------------------------------------
            // m_autoLoadNextScene가 true이고
            // SceneLoader.NextScene 문자열이 비어잇지 않으면
            // 실제 씬 전환 수행
            if (m_autoLoadNextScene && !string.IsNullOrWhiteSpace(SceneLoader.NextScene))
            {
                // 씬을 "즉시" 로드 (동기 로드)
                // 보통은 비동기 로드가 더 자연스럽지만
                // 여기서는 테스트 종료 후 바로 씬 이동만 하려는 용도
                SceneManager.LoadScene(SceneLoader.NextScene, LoadSceneMode.Single);
            }

            // 테스트 모드는 여기서 끝
            // 아래 "실제 비동기 로딩" 코드를 실행하지 않도록 종료합니다.
            yield break;
        }

        // ------------------------------------------------------------
        // (C) 테스트 모드가 아니면: 실제 비동기 씬 로드
        // ------------------------------------------------------------

        // 안전 검사:
        // 로드할 씬 이름(SceneLoader.NextScene)이 비어 있으면 로드할 수 없음
        if (string.IsNullOrWhiteSpace(SceneLoader.NextScene))
        {
            Debug.LogError("[LoadingScreenController] NextScene is empty or null. Aborting async load.");

            // 화면을 다시 검게 덮고 종료
            yield return CoFade(0f, 1f, 3f);
            yield break;
        }

        // AsyncOperation:
        // Unity에서 비동기 로딩을 시작하면 이를 통해 progress값을 받을 수 있습니다.
        AsyncOperation op = null;

        try
        {
            // 비동기 씬 로드 시작
            // 씬을 로드하면서도 현재 씬(로딩 화면)은 계속 표시할 수 있습니다.
            op = SceneManager.LoadSceneAsync(SceneLoader.NextScene);
        }
        catch (System.Exception ex)
        {
            // 씬 이름이 틀렸거나, 설정 문제 등으로 예외가 날 수 있습니다.
            Debug.LogError($"LoadingScreenController] LoadSceneAsync threw exception for '{SceneLoader.NextScene}' : {ex.Message}");
        }

        // op가 null 이면 비동기 로딩 시작에 실패한 것
        if (op == null)
        {
            Debug.LogError($"[LoadingScreenController] Failed to start async load for '{SceneLoader.NextScene}'.");

            // 화면을 다시 덮고 종료
            yield return CoFade(0f, 1f, 0.2f);
            yield break;
        }

        // ------------------------------------------------------------
        // Unity 비동기 로딩의 유명한 특징:
        // - op.porgress는 0.0 ~ 0.9 까지만 올라감
        // - 0.9에서 "씬 준비 완료" 상태가 되고
        // - allowSceneActivation = true가 되는 순간 1.0으로 가며 씬 전환
        // ------------------------------------------------------------

        // 씬 전환 타이밍을 우리가 제어하기 위해 false로 듭니다.
        // (즉, 로딩이 끝나도 " 바로 씬으로 넘어가지 않게" 막아두는 것)
        op.allowSceneActivation = false;

        // ------------------------------------------------------------
        // (C-1) op.progress(0..0.9)를 UI(0..1)로 매핑해서 표시
        // ------------------------------------------------------------

        while (op.progress < 0.9f)
        {
            // progress (0..0.9)를  0..1로 정규화
            // 예: progress 0.455면 => 0.45 / 0.9 = 0.5 (50%)
            float target = Mathf.Clamp01(op.progress / 0.9f);

            // UI 표시용 값(m_displayed)을 target까지 "천천히" 따라가게 함
            // MoveTowards는 "현재 -> 목표로 일정 속도로 이동"하는 함수
            m_displayed = Mathf.MoveTowards(
                m_displayed,                       // 현재 표시 값
                target,                            // 목표 값
                Time.unscaledDeltaTime * 1.5f     // 한 프레임에 움직일 최대량(속도)
                );

            // 다음 프레임
            yield return null;
        }

        // ------------------------------------------------------------
        // (C-2) op.progress는 0.9에서 멈추므로
        // 남은 10% (0.9 -> 1.0)를 "연출"로 채우기
        // ------------------------------------------------------------

        while (m_displayed < 1f)
        {
            // 표시값을 1까지 부드럽게 올림
            m_displayed = Mathf.MoveTowards(
                m_displayed,
                1f,
                Time.unscaledDeltaTime * 1.5f
            );

            UpdateUI(m_displayed);
            yield return null;
        }

        // ------------------------------------------------------------
        // (C-3) 100% 되었으면 화면을 검게 덮어서 전환 준비
        // ------------------------------------------------------------
        Destroy(m_image);

        if (m_LoadingImage1 != null)
        {
            yield return CoFade(0f, 1f, 1.5f);

        }
        if (m_LoadingImage2 != null)
        {
            yield return CoFade(0f, 0f, 1.5f);
        }

        // ------------------------------------------------------------
        // (C-4) 이제 씬 전환 허용!
        // allowSceneActivation을 true로 바꾸면
        // Unity가 실제로 씬을 전환합니다.
        // ------------------------------------------------------------

        op.allowSceneActivation = true;
    }

    /// <summary>
    /// UpdateUI
    /// - 진행률 값 (t: 0..1)을 UI에 반영합니다.
    /// - m_progressBar, m_percentText가 없을 수도 있으니 null 체크를 합니다.
    /// </summary>

    void UpdateUI(float t)
    {
        // Slider가 있으면 값 적용 (0~1)
        if (m_progressBar)
            m_progressBar.value = t;
    }

    /// <summary>
    /// CoFade
    /// - CanvasGroup의 alpha를 from -> to로 duration초 동안 변화시킵니다.
    /// - 동시에 blocksRaycasts를 조절해서 입력을 막거나 풀어줍니다.
    /// 
    /// blocksRaycasts 사용 이유:
    /// - 페이드 중/검은 덮개가 있을 때 뒤에 있는 UI가 눌리면 안 되니까!
    /// </summary>

    IEnumerator CoFade(float from, float to, float duration)
    {
        // CanvasGroup이 없으면 페이드가 불가능하므로 종료
        if (!m_fadeGroup)
            yield break;

        // t: 페이드가 진행된 시간(초)
        float t = 0f;

        // 페이드 중에는 입력을 막습니다.
        // (검은 덮개가 있다고 가정하고, 뒤 UI 클릭 방지)
        m_fadeGroup.blocksRaycasts = true;


        // duration이 0이면 "즉시" alpha를 바꾸고 끝냅니다.
        if (duration <= 0f)
        {
            // 목표 알파로 즉시 설정
            m_fadeGroup.alpha = to;

            // 완전 투명 (to가 거의 0)이면 입력을 풀어도 되고,
            // 불투명 (to가 0보다 크면) 입력을 막아야 함
            m_fadeGroup.blocksRaycasts = to > 0.00001f;
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
            m_fadeGroup.alpha = a;

            // 다음 프레임까지 대기
            yield return null;
        }

        // 끝났으면 정확히 to 값으로 고정 (오차 방지)
        m_fadeGroup.alpha = to;

        // 최종 상태에 따라 입력 차단 여부 결정
        m_fadeGroup.blocksRaycasts = to > 0.00001f;
    }
}
