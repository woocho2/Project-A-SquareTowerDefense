using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace SquareTD
{
    // 개발자 도구와 독립적인 리롤·경험치·판매·턴 종료·스킵 단축키
    [DisallowMultipleComponent]
    public sealed class GameplayShortcuts : MonoBehaviour
    {
        public static Func<bool> InputCaptured { get; set; }
        private void Update()
        {
            Keyboard keys = Keyboard.current;
            if (keys == null) return;
            if (keys.dKey.wasPressedThisFrame) TryHandle(Key.D);
            else if (keys.fKey.wasPressedThisFrame) TryHandle(Key.F);
            else if (keys.eKey.wasPressedThisFrame) TryHandle(Key.E);
            else if (keys.spaceKey.wasPressedThisFrame) TryHandle(Key.Space);
            else if (keys.rKey.wasPressedThisFrame) TryHandle(Key.R);
        }
        public bool TryHandle(Key key)
        {
            if (!CanReceiveInput()) return false;
            TurnManager turns = TurnManager.Instance;
            if (key == Key.R) return turns.SkipToNextPlayerTurn();
            if (!turns.Current.CanPlayerAct) return false;
            if (key == Key.Space) { turns.OnClickEndTurn(); return !turns.Current.CanPlayerAct; }
            TowerManager towers = TowerManager.Instance;
            if (key == Key.D) return towers.TryRerollShop();
            if (key == Key.F) return towers.TryBuyXp();
            if (key != Key.E) return false;
            TowerController selected = UIManager.Instance != null ? UIManager.Instance.SelectedTower : null;
            if (selected == null || !UIManager.Instance.IsTowerSelected(selected)) { GameNotice.Warn("판매할 타워를 먼저 선택해 주세요."); return false; }
            if (!towers.TrySell(selected)) return false;
            BoardInputController input = GetComponent<BoardInputController>(); if (input != null) input.ChangeMode(new IdleMode(input));
            return true;
        }
        private bool CanReceiveInput()
        {
            if (SimulationRunner.IsOn || SimulationRunner.SuppressCosmetics || GameSession.Instance == null || GameSession.Instance.IsOver || TurnManager.Instance == null || TowerManager.Instance == null) return false;
            if (Time.timeScale <= 0 || (UIManager.Instance != null && UIManager.Instance.IsPaused) || InputCaptured?.Invoke() == true) return false;
            BoardInputController input = GetComponent<BoardInputController>(); if (input != null && input.Current is DragTowerMode) return false;
            GameObject focus = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            if (focus == null) return true;
            TMP_InputField tmp = focus.GetComponentInParent<TMP_InputField>(); InputField legacy = focus.GetComponentInParent<InputField>();
            return !(tmp != null && tmp.isFocused) && !(legacy != null && legacy.isFocused);
        }
    }
}
