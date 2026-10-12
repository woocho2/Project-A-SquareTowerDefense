#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;

/// <summary>Inspector에서 직접 바꾼 Image 색만 저장하며 자동 테마 변경은 제외합니다.</summary>
[InitializeOnLoad]
public static class UIThemeManualColorTracker
{
    static UIThemeManualColorTracker() => Undo.postprocessModifications += Track;

    private static UndoPropertyModification[] Track(UndoPropertyModification[] changes)
    {
        foreach (UndoPropertyModification change in changes)
        {
            if (!(change.currentValue.target is Image image) ||
                !change.currentValue.propertyPath.StartsWith("m_Color")) continue;
            UIStageThemeController theme = image.GetComponentInParent<UIStageThemeController>(true);
            if (theme == null || !theme.ManagesImage(image) || theme.IsAutomaticColor(image)) continue;
            Undo.RecordObject(theme, "수동 UI 색상 저장");
            theme.RememberImageColor(image);
            EditorUtility.SetDirty(theme);
            PrefabUtility.RecordPrefabInstancePropertyModifications(theme);
            if (theme.gameObject.scene.IsValid() && theme.gameObject.scene.isLoaded)
                EditorSceneManager.MarkSceneDirty(theme.gameObject.scene);
        }
        return changes;
    }
}
#endif
