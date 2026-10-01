using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>보드 클릭, 타워 이동, 선택된 디버프 장판 이동을 한 곳에서 처리합니다.</summary>
public class BoardInputController : MonoBehaviour
{
    [SerializeField] private Camera m_camera;
    [SerializeField, Min(0f)] private float m_dragThreshold = 0.5f;

    public static event Action<TowerController> OnTowerClickedAction;
    public static event Action<Vector3> OnBoardWorldClickedAction;

    private static BoardInputController s_instance;

    private TowerController m_selectedTower;
    private TowerController m_pressedTower;
    private DebuffZone m_pressedZone;
    private int m_startTowerIndex;
    private Vector3 m_startObjectPosition;
    private Vector3 m_startPointerPosition;
    private Vector3 m_pointerOffset;
    private bool m_isTracking;
    private bool m_isDragging;
    private bool m_canDrag;
    private bool m_isTouch;

    private void Awake()
    {
        s_instance = this;
        if (m_camera == null) m_camera = Camera.main;
    }

    private void OnDisable() => CancelInteraction();

    private void OnDestroy()
    {
        if (s_instance == this) s_instance = null;
    }

    private void Update()
    {
        if (m_camera == null || TileManager.Instance == null) return;

        if (!m_isTracking && TryGetPointerPress(out Vector2 pressedScreen, out bool isTouch))
        {
            BeginPress(pressedScreen, isTouch);
        }

        if (!m_isTracking) return;
        if (m_pressedTower == null && m_pressedZone == null)
        {
            CancelInteraction();
            return;
        }

        if (!TryGetPointerPosition(m_isTouch, out Vector2 pointerScreen) ||
            !TryGetWorldPosition(pointerScreen, out Vector3 pointerWorld))
        {
            CancelInteraction();
            return;
        }

        if (IsPointerHeld(m_isTouch) && m_canDrag)
        {
            if (!m_isDragging && Vector2.Distance(m_startPointerPosition, pointerWorld) > m_dragThreshold)
            {
                m_isDragging = true;
            }

            if (m_isDragging && m_pressedTower != null)
            {
                m_pressedTower.transform.position = pointerWorld + m_pointerOffset;
            }
            else if (m_isDragging && m_pressedZone != null)
            {
                m_pressedZone.TryPreviewAtWorldPosition(pointerWorld);
            }
        }

        if (WasPointerReleased(m_isTouch)) EndPress();
    }

    private void BeginPress(Vector2 screenPosition, bool isTouch)
    {
        List<RaycastResult> uiHits = GetUIHits(screenPosition);
        if (uiHits.Count > 0)
        {
            bool onTowerInfo = false;
            for (int i = 0; i < uiHits.Count; i++)
            {
                if (UIManager.Instance == null || !UIManager.Instance.IsTowerInfoPanelElement(uiHits[i].gameObject)) continue;
                onTowerInfo = true;
                break;
            }

            if (!onTowerInfo) ClearSelection();
            return;
        }

        if (!TryGetWorldPosition(screenPosition, out Vector3 worldPosition)) return;

        // 겹친 장판도 선택된 타워의 장판 한 개만 입력을 받는다.
        DebuffZone selectedZone = m_selectedTower != null ? m_selectedTower.GetDebuffZone() : null;
        if (selectedZone != null && selectedZone.isActiveAndEnabled &&
            selectedZone.IsAtWorldPosition(worldPosition))
        {
            m_pressedZone = selectedZone;
            BeginTracking(selectedZone.transform.position, worldPosition, isTouch);
            return;
        }

        TileManager tileManager = TileManager.Instance;
        if (tileManager.TryGetTowerSpawnIndexAtWorldPosition(worldPosition, out int towerIndex) &&
            tileManager.TryGetTowerAt(towerIndex, out TowerController tower))
        {
            SelectTower(tower);
            m_pressedTower = tower;
            m_startTowerIndex = towerIndex;
            BeginTracking(tower.transform.position, worldPosition, isTouch);
            return;
        }

        ClearSelection();
        OnBoardWorldClickedAction?.Invoke(worldPosition);
    }

    private void BeginTracking(Vector3 objectPosition, Vector3 pointerPosition, bool isTouch)
    {
        m_startObjectPosition = objectPosition;
        m_startPointerPosition = pointerPosition;
        m_pointerOffset = objectPosition - pointerPosition;
        m_isTracking = true;
        m_isDragging = false;
        m_canDrag = GameManager.Instance != null && GameManager.Instance.CanPerformPlayerAction;
        m_isTouch = isTouch;
    }

    private void EndPress()
    {
        TowerController tower = m_pressedTower;
        DebuffZone zone = m_pressedZone;
        bool wasDragging = m_isDragging;
        bool canDrag = m_canDrag;

        m_pressedTower = null;
        m_pressedZone = null;
        m_isTracking = false;
        m_isDragging = false;
        m_canDrag = false;

        if (!wasDragging)
        {
            if (tower != null) OnTowerClickedAction?.Invoke(tower);
            return;
        }

        if (!canDrag)
        {
            RestoreDraggedObject(tower, zone);
            return;
        }

        if (zone != null)
        {
            zone.CommitPreviewPosition();
            return;
        }

        if (tower == null || TowerManager.Instance == null || TileManager.Instance == null)
        {
            RestoreDraggedObject(tower, null);
            return;
        }

        int endIndex = TileManager.Instance.TryGetTowerSpawnIndexAtWorldPosition(tower.transform.position, out int foundIndex)
            ? foundIndex : -1;
        TowerManager.Instance.MoveTowerOnGrid(m_startTowerIndex, endIndex);

        // 이동이 거부되어 점유가 원래 칸에 있으면 화면 위치도 되돌린다.
        if (TileManager.Instance.TryGetTowerAt(m_startTowerIndex, out TowerController stillAtStart) && stillAtStart == tower)
        {
            tower.transform.position = m_startObjectPosition;
        }
    }

    private void RestoreDraggedObject(TowerController tower, DebuffZone zone)
    {
        if (tower != null) tower.transform.position = m_startObjectPosition;
        if (zone != null) zone.CancelPreview();
    }

    private void SelectTower(TowerController tower)
    {
        if (m_selectedTower == tower) return;
        ClearSelection();
        m_selectedTower = tower;
        m_selectedTower.SetSelected(true);
    }

    private void ClearSelection()
    {
        if (m_selectedTower != null) m_selectedTower.SetSelected(false);
        m_selectedTower = null;
    }

    private void CancelInteraction()
    {
        if (m_isDragging) RestoreDraggedObject(m_pressedTower, m_pressedZone);
        m_pressedTower = null;
        m_pressedZone = null;
        m_isTracking = false;
        m_isDragging = false;
        m_canDrag = false;
        ClearSelection();
    }

    public static void CancelAllInteractions()
    {
        if (s_instance != null) s_instance.CancelInteraction();
    }

    public static void SelectReplacementTower(TowerController tower)
    {
        if (s_instance != null && tower != null) s_instance.SelectTower(tower);
    }

    private bool TryGetWorldPosition(Vector2 screenPosition, out Vector3 worldPosition)
    {
        Ray ray = m_camera.ScreenPointToRay(screenPosition);
        Plane gamePlane = new Plane(Vector3.forward, Vector3.zero);
        if (gamePlane.Raycast(ray, out float distance))
        {
            worldPosition = ray.GetPoint(distance);
            return true;
        }

        worldPosition = default;
        return false;
    }

    private static List<RaycastResult> GetUIHits(Vector2 screenPosition)
    {
        List<RaycastResult> results = new List<RaycastResult>();
        if (EventSystem.current == null) return results;
        PointerEventData pointerData = new PointerEventData(EventSystem.current) { position = screenPosition };
        EventSystem.current.RaycastAll(pointerData, results);
        return results;
    }

    private static bool TryGetPointerPress(out Vector2 screenPosition, out bool isTouch)
    {
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            screenPosition = Mouse.current.position.ReadValue();
            isTouch = false;
            return true;
        }

        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        {
            screenPosition = Touchscreen.current.primaryTouch.position.ReadValue();
            isTouch = true;
            return true;
        }

        screenPosition = default;
        isTouch = false;
        return false;
    }

    private static bool TryGetPointerPosition(bool isTouch, out Vector2 position)
    {
        if (isTouch && Touchscreen.current != null)
        {
            position = Touchscreen.current.primaryTouch.position.ReadValue();
            return true;
        }
        if (!isTouch && Mouse.current != null)
        {
            position = Mouse.current.position.ReadValue();
            return true;
        }

        position = default;
        return false;
    }

    private static bool IsPointerHeld(bool isTouch) => isTouch
        ? Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed
        : Mouse.current != null && Mouse.current.leftButton.isPressed;

    private static bool WasPointerReleased(bool isTouch) => isTouch
        ? Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasReleasedThisFrame
        : Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame;
}
