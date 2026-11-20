using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SquareTD.EditorTools
{
    [InitializeOnLoad]
    public static class ControlsEffectsSetup
    {
        public const string Folder = "ArtPreviews/ControlsEffects_20261119";
        private static double s_next;
        static ControlsEffectsSetup() { EditorApplication.update += Poll; }
        private static void Poll()
        {
            if (EditorApplication.timeSinceStartup < s_next || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            s_next = EditorApplication.timeSinceStartup + .5;
            string request = Folder + "/request.txt"; if (!File.Exists(request)) return;
            string command = File.ReadAllText(request).Trim(); File.Delete(request);
            try
            {
                if (command == "state") File.WriteAllText(Folder + "/editor-state.txt", $"Playing={EditorApplication.isPlayingOrWillChangePlaymode}, Step={SessionState.GetInt("SquareTD.ControlsEffectsVerification", 0)}, CompilationFailed={typeof(EditorUtility).GetProperty("scriptCompilationFailed", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic)?.GetValue(null)}\n" + string.Join("\n", Enumerable.Range(0, SceneManager.sceneCount).Select(i => SceneManager.GetSceneAt(i).path + " dirty=" + SceneManager.GetSceneAt(i).isDirty)));
                else if (command == "apply") Apply();
                else if (command == "verify") ControlsEffectsVerification.Run();
            }
            catch (Exception exception) { File.WriteAllText(Folder + "/setup-error.txt", exception.ToString()); }
        }
        [MenuItem("Tools/SquareTD/플레이 단축키·전장 효과 UI 적용")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("플레이를 종료해 주세요.");
            GameObject root = PrefabUtility.LoadPrefabContents(GameHudSetup.Prefab);
            try { Style(root); PrefabUtility.SaveAsPrefabAsset(root, GameHudSetup.Prefab); }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            AssetDatabase.SaveAssets(); File.WriteAllText(Folder + "/apply-result.txt", "Success");
        }
        public static void Style(GameObject root)
        {
            GameInfoPanel info = root.GetComponentInChildren<GameInfoPanel>(true);
            if (info.GetComponent<GlobalEffectsPanel>() == null) info.gameObject.AddComponent<GlobalEffectsPanel>();
            PlayerTurnPanel actions = root.GetComponentInChildren<PlayerTurnPanel>(true);
            var so = new SerializedObject(actions);
            StyleReroll((Button)so.FindProperty("btn_createTower").objectReferenceValue);
            Hint((Button)so.FindProperty("btn_buyXp").objectReferenceValue, "[F]  경험치 투자");
            so = new SerializedObject(info);
            TextMeshProUGUI skip = (TextMeshProUGUI)so.FindProperty("txt_skip").objectReferenceValue;
            if (skip != null) { skip.text = "스킵\n<size=11>R</size>"; skip.fontSize = 18; skip.lineSpacing = 0; }
            TowerInfoCardView card = root.GetComponentInChildren<TowerInfoCardView>(true);
            TowerInfoPanel towerPanel = root.GetComponentInChildren<TowerInfoPanel>(true);
            Button sell = card != null ? card.Sell : towerPanel != null ? (Button)new SerializedObject(towerPanel).FindProperty("btn_sellTower").objectReferenceValue : null;
            if (sell != null)
            foreach (TextMeshProUGUI text in sell.GetComponentsInChildren<TextMeshProUGUI>(true))
                if (text.text.StartsWith("판매")) { text.text = "판매  [E]"; text.enableAutoSizing = true; text.fontSizeMin = 11; text.fontSizeMax = 15; }
        }
        public static void StyleReroll(Button button)
        {
            Transform root = button.transform;
            root.name = "ShopReroll";
            TextMeshProUGUI label = root.Find("Label").GetComponent<TextMeshProUGUI>();
            label.text = "리롤";
            root.Find("Cost").GetComponent<TextMeshProUGUI>().text = BalanceTables.ShopRerollCost.ToString("N0");
            Hint(button, $"[D]  새 카드 {BalanceTables.ShopSize}장");

            Transform oldIcon = root.Find("TowerIcon");
            if (oldIcon != null) oldIcon.gameObject.SetActive(false);
            Transform existing = root.Find("RerollIcon");
            if (existing != null && existing.GetComponent<TextMeshProUGUI>() == null) return;
            if (existing != null) UnityEngine.Object.DestroyImmediate(existing.gameObject);
            var icon = new GameObject("RerollIcon", typeof(RectTransform));
            icon.layer = button.gameObject.layer;
            icon.transform.SetParent(root, false);
            var rect = (RectTransform)icon.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.up;
            rect.anchoredPosition = new Vector2(15, -12);
            rect.sizeDelta = new Vector2(39, 43);
            const int segments = 15;
            const float radius = 12f;
            Vector2 Point(float angle) => new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            for (int i = 0; i < segments; i++)
            {
                float from = Mathf.Lerp(310, 35, (float)i / segments) * Mathf.Deg2Rad;
                float to = Mathf.Lerp(310, 35, (float)(i + 1) / segments) * Mathf.Deg2Rad;
                IconStroke(rect, "Arc", Point(from), Point(to));
            }
            Vector2 tip = Point(35 * Mathf.Deg2Rad);
            Vector2 radial = tip.normalized;
            Vector2 backwards = new Vector2(-radial.y, radial.x) * 8;
            IconStroke(rect, "Arrowhead", tip, tip + backwards + radial * 5);
            IconStroke(rect, "Arrowhead", tip, tip + backwards - radial * 5);
        }

        private static void IconStroke(RectTransform parent, string name, Vector2 from, Vector2 to)
        {
            var segment = new GameObject(name, typeof(RectTransform), typeof(Image));
            segment.layer = parent.gameObject.layer;
            segment.transform.SetParent(parent, false);
            var rect = (RectTransform)segment.transform;
            rect.anchoredPosition = (from + to) * .5f;
            Vector2 direction = to - from;
            rect.sizeDelta = new Vector2(direction.magnitude + .85f, 2.75f);
            rect.localEulerAngles = new Vector3(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
            Image image = segment.GetComponent<Image>();
            image.color = new Color32(226, 194, 120, 255);
            image.raycastTarget = false;
        }
        private static void Hint(Button button, string message)
        {
            Transform hint = button != null ? button.transform.Find("Hint") : null;
            if (hint == null) throw new InvalidOperationException("행동 버튼의 Hint가 없습니다.");
            TextMeshProUGUI text = hint.GetComponent<TextMeshProUGUI>(); text.text = message; text.color = new Color32(226, 194, 120, 255); text.fontSize = 14;
        }
    }
}
