#if UNITY_EDITOR
using UnityEditor;
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
        "헬\nHel"
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

                if (GUILayout.Button(s_realmLabels[index], buttonStyle))
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
        var (backgroundColor, frameColor, realmName) = controller.GetCurrentThemeColors();
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField($"{realmName} 테마 색상 미리보기", EditorStyles.boldLabel);
        DrawColorPreview("TowerInfoPanel 기준 배경", backgroundColor);
        DrawColorPreview("UI 프레임 (_frame)", frameColor);
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(6);
        GUI.backgroundColor = new Color(1f, 0.82f, 0.25f);
        if (GUILayout.Button("👑 아스가르드 전용 UI 세트 자동 연결 & 즉시 적용", GUILayout.Height(36)))
        {
            controller.LoadDefaultAsgardSprites();
        }

        GUI.backgroundColor = new Color(0.35f, 0.75f, 1f);
        if (GUILayout.Button("🎨 2048x2048 마스터 UI 아틀라스 생성 & 자동 슬라이싱", GUILayout.Height(30)))
        {
            AsgardAtlasGenerator.GenerateAtlas();
        }

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
        EditorGUILayout.LabelField("전용 스프라이트 (Theme Sprites)", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("useThemeSprites"), new GUIContent("전용 스프라이트 사용"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("realmSprites"), new GUIContent("스테이지별 스프라이트 목록"), true);

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

    private static void DrawColorPreview(string label, Color color)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel(label);
        Rect colorRect = GUILayoutUtility.GetRect(120, 20);
        EditorGUI.DrawRect(colorRect, color);
        EditorGUILayout.LabelField($"#{ColorUtility.ToHtmlStringRGB(color)}", GUILayout.Width(70));
        EditorGUILayout.EndHorizontal();
    }
}
#endif
