using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace SquareTD
{
    public class BoardInputController : MonoBehaviour
    {
        [SerializeField] private Camera m_camera;

        public IInputMode Current { get; private set; }

        public event System.Action<BoardPoint> Clicked;

        private bool m_pressing;

        private void Start()
        {
            if (m_camera == null) m_camera = Camera.main;
            ChangeMode(new IdleMode(this));
            TurnManager.Instance.StateChanged += HandleStateChanged;
        }

        private void OnDestroy()
        {
            if (TurnManager.Instance != null) TurnManager.Instance.StateChanged -= HandleStateChanged;
        }

        public void ChangeMode(IInputMode next)
        {
            Current?.Exit();
            Current = next;
            Current.Enter();
        }

        public void NotifyClicked(BoardPoint point) => Clicked?.Invoke(point);

        private void Update()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null) return;

            Vector3 world = ScreenToWorld(mouse.position.ReadValue());

            if (mouse.leftButton.wasPressedThisFrame)
            {
                if (IsPointerOverUI()) return;
                m_pressing = true;
                Current.OnPointerDown(MakePoint(world));
            }
            else if (m_pressing && mouse.leftButton.wasReleasedThisFrame)
            {
                m_pressing = false;
                Current.OnPointerUp(MakePoint(world));
            }
            else if (m_pressing && mouse.leftButton.isPressed)
            {
                Current.OnPointerDrag(world);
            }
        }

        private Vector3 ScreenToWorld(Vector2 screen)
        {
            Vector3 world = m_camera.ScreenToWorldPoint(screen);
            world.z = 0f;
            return world;
        }

        private static BoardPoint MakePoint(Vector3 world)
        {
            BoardManager.Instance.TryGetPoint(world, out BoardPoint point);
            return point;
        }

        private static bool IsPointerOverUI()
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        }

        private void HandleStateChanged(ITurnState state)
        {
            if (!state.CanPlayerAct) Current.OnCancel();
        }
    }
}
