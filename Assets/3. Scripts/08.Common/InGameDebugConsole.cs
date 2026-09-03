using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class InGameDebugConsole : MonoBehaviour
{
    [Header("UI 연결")]
    [SerializeField, Tooltip("로그를 출력할 TextMeshPro 컴포넌트")]
    private TextMeshProUGUI m_debugText;

    [Header("설정")]
    [SerializeField, Tooltip("화면에 표시할 최대 로그 줄 수")]
    private int m_maxLines = 5;

    [SerializeField] Color m_basecolor;
    [SerializeField] Color m_warningcolor;
    [SerializeField] Color m_errorcolor;

    private Queue<string> m_logQueue = new Queue<string>();

    private void OnEnable()
    {
        Application.logMessageReceived += HandleLog;
    }

    private void OnDisable()
    {
        Application.logMessageReceived -= HandleLog;
    }


    private void HandleLog(string logString, string stackTrace, LogType type)
    {
        Color colorTag = m_basecolor;
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
        {
            colorTag = m_errorcolor;
        }
        else if (type == LogType.Warning)
        {
            colorTag = m_warningcolor;
        }

        string hexColor = ColorUtility.ToHtmlStringRGB(colorTag);

        string formattedLog = $"<color=#{hexColor}>[{type}] {logString}</color>";

        m_logQueue.Enqueue(formattedLog);

        if (m_logQueue.Count > m_maxLines)
        {
            m_logQueue.Dequeue();
        }

        if (m_debugText != null)
        {
            m_debugText.text = string.Join("\n", m_logQueue);
        }
    }

    public void TestLog()
    {
        Debug.Log("일반 시스템 메시지입니다.");
        Debug.LogWarning("주의가 필요한 경고 메시지입니다.");
        Debug.LogError("치명적인 에러가 발생했습니다!");
    }
}