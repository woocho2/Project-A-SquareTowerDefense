using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 로딩 화면의 페이드, 진행률 표시와 비동기 씬 전환을 담당합니다.
/// 테스트 모드에서는 실제 씬 로드 없이 지정 시간 동안 동일한 연출을 재생할 수 있습니다.
/// </summary>
public class LoadingManager : MonoBehaviour
{
    #region Inspector

    [Header("Loading UI")]
    [SerializeField] private Slider m_progressBar;
    [SerializeField] private TMP_Text m_percentText;
    [SerializeField] private CanvasGroup m_fadeGroup;
    [SerializeField] private Image m_image;
    [SerializeField] private Image m_LoadingImage1;
    [SerializeField] private Image m_LoadingImage2;

    [Header("Test Mode")]
    [Tooltip("실제 씬을 로드하지 않고 로딩 연출만 재생합니다.")]
    [SerializeField] private bool m_testMode = true;
    [Tooltip("테스트용 로딩 화면을 표시할 시간(초)")]
    [SerializeField] private float m_testDuration = 3f;
    [Tooltip("테스트 종료 후 SceneLoader.NextScene으로 이동합니다.")]
    [SerializeField] private bool m_autoLoadNextScene = false;

    #endregion

    private float m_displayed;

    #region Unity Lifecycle

    private void Awake()
    {
        m_progressBar ??= GetComponentInChildren<Slider>();
        m_percentText ??= GetComponentInChildren<TMP_Text>();
        m_fadeGroup ??= GetComponentInChildren<CanvasGroup>();
    }

    private IEnumerator Start()
    {
        // 검은 덮개를 걷은 뒤 테스트 또는 실제 로딩을 시작합니다.
        yield return CoFade(1f, 0f, 1.5f);

        if (m_testMode)
        {
            yield return RunTestLoading();
            yield break;
        }

        yield return LoadNextSceneAsync();
    }

    #endregion

    #region Loading Flow

    private IEnumerator RunTestLoading()
    {
        float duration = Mathf.Max(0f, m_testDuration);
        float elapsed = 0f;
        m_displayed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            m_displayed = Mathf.Clamp01(elapsed / Mathf.Max(0.0001f, duration));
            UpdateUI(m_displayed);
            yield return null;
        }

        m_displayed = 1f;
        UpdateUI(m_displayed);
        yield return new WaitForSecondsRealtime(0.2f);
        yield return CoFade(0f, 1f, 0.2f);

        if (m_autoLoadNextScene && !string.IsNullOrWhiteSpace(SceneLoader.NextScene))
        {
            SceneManager.LoadScene(SceneLoader.NextScene, LoadSceneMode.Single);
        }
    }

    private IEnumerator LoadNextSceneAsync()
    {
        if (string.IsNullOrWhiteSpace(SceneLoader.NextScene))
        {
            Debug.LogError("[LoadingManager] SceneLoader.NextScene이 비어 있습니다.");
            yield return CoFade(0f, 1f, 3f);
            yield break;
        }

        AsyncOperation operation = null;
        try
        {
            operation = SceneManager.LoadSceneAsync(SceneLoader.NextScene);
        }
        catch (System.Exception exception)
        {
            Debug.LogError($"[LoadingManager] '{SceneLoader.NextScene}' 로드 시작 실패: {exception.Message}");
        }

        if (operation == null)
        {
            yield return CoFade(0f, 1f, 0.2f);
            yield break;
        }

        // Unity 비동기 로딩은 0.9에서 준비가 끝나며, 활성화를 허용하면 씬을 전환합니다.
        operation.allowSceneActivation = false;

        while (operation.progress < 0.9f)
        {
            float target = Mathf.Clamp01(operation.progress / 0.9f);
            m_displayed = Mathf.MoveTowards(m_displayed, target, Time.unscaledDeltaTime * 1.5f);
            yield return null;
        }

        while (m_displayed < 1f)
        {
            m_displayed = Mathf.MoveTowards(m_displayed, 1f, Time.unscaledDeltaTime * 1.5f);
            UpdateUI(m_displayed);
            yield return null;
        }

        if (m_image != null) Destroy(m_image);

        if (m_LoadingImage1 != null)
        {
            yield return CoFade(0f, 1f, 1.5f);
        }

        if (m_LoadingImage2 != null)
        {
            yield return CoFade(0f, 0f, 1.5f);
        }

        operation.allowSceneActivation = true;
    }

    #endregion

    #region UI Helpers

    private void UpdateUI(float progress)
    {
        if (m_progressBar != null)
        {
            m_progressBar.value = progress;
        }
    }

    private IEnumerator CoFade(float from, float to, float duration)
    {
        if (m_fadeGroup == null) yield break;

        m_fadeGroup.blocksRaycasts = true;

        if (duration <= 0f)
        {
            m_fadeGroup.alpha = to;
            m_fadeGroup.blocksRaycasts = to > 0.00001f;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            m_fadeGroup.alpha = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }

        m_fadeGroup.alpha = to;
        m_fadeGroup.blocksRaycasts = to > 0.00001f;
    }

    #endregion
}
