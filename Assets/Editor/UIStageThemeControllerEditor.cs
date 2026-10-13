#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[CustomEditor(typeof(UIStageThemeController))]
public class UIStageThemeControllerEditor : Editor
{
    private static readonly string[] s_realmLabels =
    {
        "아스가르드\nAsgard",
        "알프하임\nAlfheim",
        "바나하임\nVanaheim",
        "미드가르드\nMidgard",
        "요툰하임\nJotunheim",
        "니다벨리르\nNidavellir",
        "니플하임\nNiflheim",
        "무스펠하임\nMuspelheim",
        "헬\nHel",
        "라그나로크\nRagnarok"
    };

    public override void OnInspectorGUI()
    {
        UIStageThemeController controller = (UIStageThemeController)target;
        serializedObject.Update();

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("스테이지 UI 테마 설정", EditorStyles.boldLabel);

        Canvas canvas = controller.GetComponent<Canvas>();
        string helpMessage = canvas != null
            ? $"Canvas '{canvas.gameObject.name}'에 연결되어 있습니다. _bg와 _frame 이미지에 테마 색을 적용합니다."
            : "_bg는 테마 배경색, _frame은 테마 프레임색을 사용합니다. 대상 Canvas를 직접 지정하거나 같은 오브젝트에 Canvas를 붙여 주세요.";
        EditorGUILayout.HelpBox(helpMessage, MessageType.Info);

        SerializedProperty realmProperty = serializedObject.FindProperty("currentRealm");
        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(realmProperty, new GUIContent("현재 스테이지 테마"));
        if (EditorGUI.EndChangeCheck())
        {
            serializedObject.ApplyModifiedProperties();
            controller.ApplyTheme();
        }

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("빠른 테마 선택", EditorStyles.miniBoldLabel);

        int currentIndex = (int)controller.CurrentRealm;
        const int columnCount = 3;
        for (int rowStart = 0; rowStart < s_realmLabels.Length; rowStart += columnCount)
        {
            EditorGUILayout.BeginHorizontal();
            for (int column = 0; column < columnCount; column++)
            {
                int index = rowStart + column;
                if (index >= s_realmLabels.Length) break;

                bool isSelected = index == currentIndex;
                GUI.backgroundColor = isSelected ? new Color(0.4f, 0.8f, 1f) : Color.white;

                GUIStyle buttonStyle = new GUIStyle(GUI.skin.button)
                {
                    fontStyle = isSelected ? FontStyle.Bold : FontStyle.Normal,
                    fixedHeight = 36
                };

                // 색을 직접 지정한 스테이지는 이름 뒤에 표시합니다.
                string label = controller.HasCustomThemeColor((StageRealm)index)
                    ? s_realmLabels[index] + " *"
                    : s_realmLabels[index];
                if (GUILayout.Button(label, buttonStyle))
                {
                    Undo.RecordObject(controller, "스테이지 테마 변경");
                    controller.CurrentRealm = (StageRealm)index;
                    EditorUtility.SetDirty(controller);
                }
            }
            EditorGUILayout.EndHorizontal();
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.Space(8);
        DrawThemeColorEditor(controller);

        EditorGUILayout.Space(6);
        GUI.backgroundColor = new Color(0.2f, 0.75f, 0.35f);
        if (GUILayout.Button("Canvas UI 테마/색상 즉시 적용", GUILayout.Height(30)))
        {
            controller.ApplyTheme();
        }
        GUI.backgroundColor = Color.white;

        if (GUILayout.Button("_bg / _frame 대상 UI 목록을 Console에 출력"))
        {
            controller.LogMatchingElements();
        }

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("공통 아틀라스", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("useSharedAtlas"), new GUIContent("A/B/C/프레임 색상 적용"));
        EditorGUILayout.HelpBox("Image의 Color를 직접 바꾸면 해당 UI의 색상을 저장하고 테마 자동 적용에서도 유지합니다.", MessageType.Info);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("imageColorOverrides"), new GUIContent("직접 지정한 UI 색상"), true);
        if (controller.ManualColorCount > 0 && GUILayout.Button("직접 지정한 색상을 지우고 테마 기본색으로 복원"))
        {
            serializedObject.ApplyModifiedProperties();
            Undo.RecordObject(controller, "수동 UI 색상 복원");
            controller.ClearManualColors();
            EditorUtility.SetDirty(controller);
            PrefabUtility.RecordPrefabInstancePropertyModifications(controller);
            serializedObject.Update();
        }

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("고급 설정", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("targetCanvas"), new GUIContent("대상 Canvas"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("preserveOriginalAlpha"), new GUIContent("기존 알파값 유지"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("bgKeyword"), new GUIContent("배경 이름 키워드"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("frameKeyword"), new GUIContent("프레임 이름 키워드"));

        SerializedProperty useCustomColors = serializedObject.FindProperty("useCustomColors");
        EditorGUILayout.PropertyField(useCustomColors, new GUIContent("사용자 정의 색상 사용"));
        if (useCustomColors.boolValue)
        {
            EditorGUILayout.PropertyField(
                serializedObject.FindProperty("customThemes"),
                new GUIContent("사용자 정의 테마 목록"),
                true);
        }

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("TowerInfoPanel 강조", EditorStyles.miniBoldLabel);
        EditorGUILayout.PropertyField(
            serializedObject.FindProperty("towerInfoBackground"),
            new GUIContent("강조할 BackGround_BG 이미지"));
        EditorGUILayout.PropertyField(
            serializedObject.FindProperty("towerInfoBackgroundBrightness"),
            new GUIContent("패널 배경 명도 보정"));

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("텍스트 대비", EditorStyles.miniBoldLabel);
        SerializedProperty autoTextContrast = serializedObject.FindProperty("autoTextContrast");
        EditorGUILayout.PropertyField(autoTextContrast, new GUIContent("텍스트 색상 자동 대비"));
        if (autoTextContrast.boolValue)
        {
            EditorGUILayout.PropertyField(
                serializedObject.FindProperty("textContrastThreshold"),
                new GUIContent("밝은 배경 판정 기준"));
        }

        serializedObject.ApplyModifiedProperties();
    }

    /// <summary>현재 스테이지의 테마 색 네 가지를 직접 고르는 칸입니다. 바꾸면 그 스테이지의 사용자 정의 색으로 저장됩니다.</summary>
    private void DrawThemeColorEditor(UIStageThemeController controller)
    {
        StageRealm realm = controller.CurrentRealm;
        RealmThemeColor colors = controller.GetEffectiveThemeColor(realm);
        bool isCustom = controller.HasCustomThemeColor(realm);

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField(
            $"{colors.displayName} 테마 색상 ({(isCustom ? "직접 지정" : "기본")})",
            EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        colors.bgColor = DrawColorField("배경 (_bg, A 레이어)", colors.bgColor);
        colors.frameColor = DrawColorField("UI 프레임 (_frame)", colors.frameColor);
        colors.colorB = DrawColorField("B 강조색", colors.colorB);
        colors.colorC = DrawColorField("C 짙은 바탕", colors.colorC);
        if (EditorGUI.EndChangeCheck())
        {
            serializedObject.ApplyModifiedProperties();
            Undo.RecordObject(controller, "스테이지 테마 색상 변경");
            controller.SetCustomThemeColor(colors);
            MarkControllerDirty(controller);
            serializedObject.Update();
        }

        using (new EditorGUI.DisabledScope(!isCustom))
        {
            if (GUILayout.Button("이 스테이지를 기본색으로 되돌리기"))
            {
                serializedObject.ApplyModifiedProperties();
                Undo.RecordObject(controller, "스테이지 테마 색상 복원");
                controller.ClearCustomThemeColor(realm);
                MarkControllerDirty(controller);
                serializedObject.Update();
            }
        }
        EditorGUILayout.EndVertical();
    }

    private static Color DrawColorField(string label, Color color)
    {
        EditorGUILayout.BeginHorizontal();
        // 알파는 테마가 쓰지 않으므로 고르는 칸에서 뺍니다.
        Color picked = EditorGUILayout.ColorField(new GUIContent(label), color, true, false, false);
        EditorGUILayout.LabelField($"#{ColorUtility.ToHtmlStringRGB(picked)}", GUILayout.Width(70));
        EditorGUILayout.EndHorizontal();
        return picked;
    }

    private static void MarkControllerDirty(UIStageThemeController controller)
    {
        EditorUtility.SetDirty(controller);
        PrefabUtility.RecordPrefabInstancePropertyModifications(controller);
        if (controller.gameObject.scene.IsValid() && controller.gameObject.scene.isLoaded)
            EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);
    }
}
#endif
