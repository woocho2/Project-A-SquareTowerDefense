using UnityEngine;
using UnityEngine.InputSystem; // 💡 [추가] 새로운 인풋 시스템 네임스페이스

public class CameraController : MonoBehaviour
{
    [Header("이동 제한 (맵 크기)")]
    public float panLimitX = 20f;
    public float panLimitY = 15f;

    [Header("조작감 설정")]
    [Tooltip("관성(미끄러짐) 마찰력. 숫자가 높을수록 손을 뗐을 때 카메라가 빨리 멈춥니다.")]
    public float inertiaDamping = 10f;

    private Camera m_cam;
    private Vector3 m_dragOrigin;
    private Vector3 m_velocity;
    private bool m_isDragging;

    void Start()
    {
        m_cam = Camera.main;
    }

    void LateUpdate()
    {
        // 💡 [추가] 입력 장치(마우스, 터치 등)가 연결되어 있지 않다면 실행하지 않음
        if (Pointer.current == null) return;

        // 1. 화면 터치 (또는 마우스 좌클릭) 시작
        if (Pointer.current.press.wasPressedThisFrame)
        {
            m_dragOrigin = m_cam.ScreenToWorldPoint(Pointer.current.position.ReadValue());
            m_isDragging = true;
            m_velocity = Vector3.zero;
        }

        // 2. 화면을 누른 채로 밀어내기 (스와이프)
        if (Pointer.current.press.isPressed && m_isDragging)
        {
            Vector3 currentPos = m_cam.ScreenToWorldPoint(Pointer.current.position.ReadValue());

            Vector3 difference = m_dragOrigin - currentPos;
            m_cam.transform.position += difference;

            m_velocity = difference;
        }
        // 3. 화면 터치 종료
        else if (Pointer.current.press.wasReleasedThisFrame)
        {
            m_isDragging = false;
        }

        // 4. 터치 종료 후 관성에 의해 스르륵 미끄러지는 연출
        if (!m_isDragging && m_velocity.magnitude > 0.0001f)
        {
            m_cam.transform.position += m_velocity;
            m_velocity = Vector3.Lerp(m_velocity, Vector3.zero, inertiaDamping * Time.deltaTime);
        }

        // 5. 카메라가 맵 밖으로 벗어나지 않도록 강제 고정
        Vector3 pos = m_cam.transform.position;
        pos.x = Mathf.Clamp(pos.x, -panLimitX, panLimitX);
        pos.y = Mathf.Clamp(pos.y, -panLimitY, panLimitY);
        m_cam.transform.position = pos;
    }
}