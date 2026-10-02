using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

/// <summary>
/// 플레이 중 마지막으로 클릭한 대상(UI / 타워 / 타일)을 보여주는 디버그 창입니다.
/// Tools > Click Debug 로 엽니다.
/// </summary>
public class ClickDebugWindow : EditorWindow
{
    private readonly List<GameObject> m_uiHits = new List<GameObject>();
    private readonly List<string> m_boardLines = new List<string>();

    private bool m_hasClick;
    private bool m_wasPressed;
    private bool m_logToConsole;
    private Vector2 m_screenPosition;
    private Vector2 m_scroll;

    [MenuItem("Tools/Click Debug")]
    private static void Open()
    {
        GetWindow<ClickDebugWindow>("Click Debug");
    }

    private void OnEnable()
    {
        InputSystem.onAfterUpdate += PollClick;
    }

    private void OnDisable()
    {
        InputSystem.onAfterUpdate -= PollClick;
    }

    private void PollClick()
    {
        // Input System은 에디터용과 게임용 입력 상태를 따로 들고 있습니다.
        // 게임 화면 클릭은 게임용 업데이트에서만 보이므로 에디터 업데이트는 건너뜁니다.
        if (InputState.currentUpdateType == InputUpdateType.Editor) return;

        if (!Application.isPlaying || Mouse.current == null)
        {
            m_wasPressed = false;
            return;
        }

        bool isPressed = Mouse.current.leftButton.isPressed;
        bool pressedNow = isPressed && !m_wasPressed;
        m_wasPressed = isPressed;
        if (!pressedNow) return;

        Camera camera = Camera.main != null ? Camera.main : FindFirstObjectByType<Camera>();
        if (camera == null) return;

        RecordClick(camera, Mouse.current.position.ReadValue());
        Repaint();
    }

    private void RecordClick(Camera camera, Vector2 screenPosition)
    {
        m_hasClick = true;
        m_screenPosition = screenPosition;
        m_uiHits.Clear();
        m_boardLines.Clear();

        CollectUIHits(screenPosition);
        CollectBoardInfo(camera, screenPosition);

        if (m_logToConsole)
        {
            string target = m_uiHits.Count > 0 ? $"UI: {GetPath(m_uiHits[0])}" : string.Join(" / ", m_boardLines);
            Debug.Log($"[ClickDebug] {target}");
        }
    }

    private void CollectUIHits(Vector2 screenPosition)
    {
        if (EventSystem.current == null) return;

        List<RaycastResult> results = new List<RaycastResult>();
        PointerEventData pointerData = new PointerEventData(EventSystem.current) { position = screenPosition };
        EventSystem.current.RaycastAll(pointerData, results);

        foreach (RaycastResult result in results)
        {
            if (result.gameObject != null) m_uiHits.Add(result.gameObject);
        }
    }

    private void CollectBoardInfo(Camera camera, Vector2 screenPosition)
    {
        // BoardInputController와 같은 방식으로 z=0 평면 위의 월드 좌표를 구합니다.
        Ray ray = camera.ScreenPointToRay(screenPosition);
        Plane gamePlane = new Plane(Vector3.forward, Vector3.zero);
        if (!gamePlane.Raycast(ray, out float distance)) return;

        Vector3 worldPosition = ray.GetPoint(distance);
        m_boardLines.Add($"월드 좌표: ({worldPosition.x:F2}, {worldPosition.y:F2})");

        TileManager tileManager = TileManager.Instance;
        if (tileManager == null) return;

        if (tileManager.TryGetTowerSpawnIndexAtWorldPosition(worldPosition, out int towerIndex))
        {
            tileManager.TryGetTowerSpawnTileCell(towerIndex, out Vector3Int towerCell);
            m_boardLines.Add($"타워 스폰 타일: index {towerIndex}, cell {towerCell}");
            m_boardLines.Add(tileManager.TryGetTowerAt(towerIndex, out TowerController tower)
                ? $"타워: {tower.name}"
                : "타워: 없음");
            return;
        }

        if (tileManager.TryGetPathCellAtWorldPosition(worldPosition, out Vector3Int pathCell))
        {
            m_boardLines.Add($"패스 타일: cell {pathCell}");
            m_boardLines.Add($"적: {tileManager.GetEnemyOccupantsAtPathCell(pathCell).Count}마리");
            return;
        }

        m_boardLines.Add("타일 없음");
    }

    private void OnGUI()
    {
        m_logToConsole = EditorGUILayout.ToggleLeft("콘솔에도 출력", m_logToConsole);

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("플레이 모드에서 게임 화면을 클릭하면 표시됩니다.", MessageType.Info);
            return;
        }

        if (!m_hasClick)
        {
            EditorGUILayout.HelpBox("아직 클릭한 대상이 없습니다.", MessageType.Info);
            return;
        }

        m_scroll = EditorGUILayout.BeginScrollView(m_scroll);

        EditorGUILayout.LabelField("화면 좌표", $"({m_screenPosition.x:F0}, {m_screenPosition.y:F0})");

        // UI가 하나라도 맞으면 BoardInputController는 보드 입력을 처리하지 않습니다.
        bool blockedByUI = m_uiHits.Count > 0;
        EditorGUILayout.LabelField("입력 대상", blockedByUI ? "UI (보드 입력 차단됨)" : "보드");

        EditorGUILayout.Space();
        EditorGUILayout.LabelField($"UI ({m_uiHits.Count})", EditorStyles.boldLabel);
        if (m_uiHits.Count == 0)
        {
            EditorGUILayout.LabelField("없음");
        }
        for (int i = 0; i < m_uiHits.Count; i++)
        {
            GameObject hit = m_uiHits[i];
            if (hit == null) continue;

            // 맨 위(0번)가 실제로 클릭을 받는 오브젝트입니다. 누르면 하이어라키에서 찾아줍니다.
            if (GUILayout.Button($"{i}. {GetPath(hit)}", EditorStyles.linkLabel))
            {
                EditorGUIUtility.PingObject(hit);
                Selection.activeGameObject = hit;
            }
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("보드", EditorStyles.boldLabel);
        foreach (string line in m_boardLines)
        {
            EditorGUILayout.LabelField(line);
        }

        EditorGUILayout.EndScrollView();
    }

    private static string GetPath(GameObject target)
    {
        string path = target.name;
        Transform parent = target.transform.parent;

        // 전체 경로는 너무 길어 부모 두 단계까지만 표시합니다.
        for (int depth = 0; depth < 2 && parent != null; depth++)
        {
            path = $"{parent.name}/{path}";
            parent = parent.parent;
        }

        return path;
    }
}
