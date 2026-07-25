using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    // m_gameSpeed는 게임의 전체적인 속도를 조절하는 변수입니다. 이 변수의 값을 변경하면 게임의 시간 흐름이 빨라지거나 느려질 수 있습니다. 예를 들어, m_gameSpeed가 2.0f로 설정되면 게임이 2배 빠르게 진행되고, 0.5f로 설정되면 게임이 절반 속도로 진행됩니다.
    [SerializeField] float m_gameSpeed = 1.0f;

    // m_totalLife는 플레이어가 게임에서 가지고 있는 총 생명 수를 나타내는 변수입니다. 이 값이 0이 되면 게임 오버 상태가 됩니다. 게임의 난이도나 레벨에 따라 이 값을 조절할 수 있습니다.
    [SerializeField] int m_totalLife;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            // 이미 다른 인스턴스가 존재하는 경우, 현재 게임 오브젝트를 파괴하여 싱글톤 패턴을 유지합니다.
            Destroy(gameObject);
            return;
        }

        // 현재 인스턴스를 싱글톤 인스턴스로 설정합니다.
        Instance = this;

        // 씬이 변경되어도 이 게임 오브젝트가 파괴되지 않도록 설정합니다.
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
    }

    void Update()
    {
    }
        
    public void OnClickGameSpeed(float gamespeed)
    {
        // m_gameSpeed의 현재 값에 따라 다음 게임 속도를 결정하는 switch 표현식입니다. 현재 게임 속도가 1.0f인 경우 다음 게임 속도는 1.5f가 되고, 1.5f인 경우 다음 게임 속도는 2.0f가 됩니다. 2.0f인 경우 다음 게임 속도는 3.0f가 되고, 3.0f인 경우 다음 게임 속도는 다시 1.0f로 돌아갑니다. 이렇게 하면 게임 속도를 순환적으로 변경할 수 있습니다.
        float nextSpeed = m_gameSpeed switch
        {
            1.0f => 1.5f,
            1.5f => 2.0f,
            2.0f => 3.0f,
            3.0f => 1.0f,

            _ => 1.0f
        };
        // 다음 게임 속도를 설정하는 SetGameSpeed 메서드를 호출하여 게임 속도를 변경합니다. 이렇게 하면 게임의 시간 흐름이 다음 게임 속도로 조절됩니다.
        SetGameSpeed(nextSpeed);
    }

    public void SetGameSpeed(float speed)
    {
        // m_gameSpeed 변수에 전달된 speed 값을 할당하여 게임 속도를 업데이트합니다. 이렇게 하면 게임 속도가 변경되며, 이후에 Time.timeScale을 설정할 때 이 값이 사용됩니다.
        m_gameSpeed = speed;

        // Time.timeScale을 m_gameSpeed로 설정하여 게임의 시간 흐름을 조절합니다. 이렇게 하면 게임이 설정된 속도로 진행되도록 합니다. 예를 들어, m_gameSpeed가 2.0f로 설정되면 게임이 2배 빠르게 진행되고, 0.5f로 설정되면 게임이 절반 속도로 진행됩니다.
        Time.timeScale = m_gameSpeed;

        // Time.fixedDeltaTime를 0.02f에 Time.timeScale을 곱한 값으로 설정하여 물리 업데이트의 간격을 조절합니다. 이렇게 하면 게임 속도가 변경될 때 물리 업데이트도 함께 조절되어 게임이 원활하게 진행되도록 합니다. 예를 들어, 게임 속도가 빨라지면 물리 업데이트 간격이 짧아지고, 게임 속도가 느려지면 물리 업데이트 간격이 길어집니다.
        Time.fixedDeltaTime = 0.02f * Time.timeScale;
    }

    public void OnStartGame()
    {
        // 게임이 시작될 때 디버그 로그를 출력하여 게임이 시작되었음을 알립니다.
        Debug.Log("Game Started!");

        // 게임이 시작될 때 Time.timeScale을 1.0f로 설정하여 게임의 시간 흐름을 정상적으로 시작합니다. 이렇게 하면 게임 내의 모든 시간 기반 이벤트가 정상적으로 작동하게 됩니다.
        Time.timeScale = 1.0f;
    }

    public void OnPauseGame()
    {
        // 게임을 일시정지할 때 Time.timeScale을 0.0f로 설정하여 게임의 시간 흐름을 멈춥니다. 이렇게 하면 게임 내의 모든 시간 기반 이벤트가 일시정지 상태가 됩니다. 예를 들어, 적의 이동, 타워의 공격, 애니메이션 등이 모두 멈추게 됩니다.
        Time.timeScale = 0.0f;
    }

    public void OnResumeGame()
    {
        // 게임을 다시 시작할 때 Time.timeScale을 GetGameSpeed() 메서드를 호출하여 현재 설정된 게임 속도로 설정합니다. 이렇게 하면 게임이 일시정지 상태에서 다시 시작될 때, 이전에 설정된 게임 속도로 진행되도록 합니다.
        Time.timeScale = GetGameSpeed();
    }

    // OnQuitGame 메서드는 게임을 종료하는 메서드입니다. 이 메서드를 호출하면 게임이 종료됩니다. Unity 에디터에서는 EditorApplication.isPlaying을 false로 설정하여 게임을 종료하고, 빌드된 애플리케이션에서는 Application.Quit()을 호출하여 게임을 종료합니다.
    public void OnQuitGame()
    {
#if UNITY_EDITOR
        // Unity 에디터에서 게임을 종료하는 메서드입니다.
        UnityEditor.EditorApplication.isPlaying = false;
#else
        // 애플리케이션을 종료하는 메서드입니다. 이 메서드를 호출하면 게임이 종료됩니다.
        Application.Quit();
#endif
    }

    // GetGameSpeed 메서드는 현재 설정된 게임 속도를 반환하는 메서드입니다. 이 메서드를 호출하면 m_gameSpeed 변수에 저장된 현재 게임 속도 값을 얻을 수 있습니다. 예를 들어, 게임 속도가 1.5f로 설정되어 있다면 이 메서드는 1.5f를 반환합니다.
    public float GetGameSpeed()
    {
        return m_gameSpeed;
    }


    // OnGameOver 메서드는 게임 오버 상태가 되었을 때 호출되는 메서드입니다. 이 메서드를 호출하면 게임이 일시정지되고, UIManager의 ShowGameOver() 메서드를 호출하여 게임 오버 화면을 표시합니다. 이렇게 하면 플레이어에게 게임이 종료되었음을 알리고, 다시 시작하거나 종료할 수 있는 옵션을 제공할 수 있습니다.
    public void OnGameOver()
    {
        OnPauseGame();

        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowGameOver();
        }
    }

    // OnGameClear 메서드는 게임 클리어 상태가 되었을 때 호출되는 메서드입니다. 이 메서드를 호출하면 게임이 일시정지되고, UIManager의 ShowGameClear() 메서드를 호출하여 게임 클리어 화면을 표시합니다. 이렇게 하면 플레이어에게 게임이 성공적으로 완료되었음을 알리고, 다음 단계로 진행하거나 다시 시작할 수 있는 옵션을 제공할 수 있습니다.
    void OnGameClear()
    {
        OnPauseGame();
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowGameClear();
        }
    }
}
