using UnityEditor;
using UnityEngine;

namespace SquareTD.EditorTools
{
    // 옛 버전으로 저장된 플러그인 .meta(DOTween)를 지금 Unity 버전으로 다시 저장
    public static class MetaUpgradeTool
    {
        private static readonly string[] s_paths =
        {
            "Assets/Plugins/Demigiant/DOTween/DOTween.dll",
            "Assets/Plugins/Demigiant/DOTween/Editor/DOTweenEditor.dll",
        };

        [MenuItem("Tools/SquareTD/DOTween .meta 다시 저장")]
        private static void Upgrade()
        {
            AssetDatabase.ForceReserializeAssets(s_paths, ForceReserializeAssetsOptions.ReserializeMetadata);
            AssetDatabase.Refresh();
            Debug.Log("[MetaUpgradeTool] DOTween .meta 2개를 다시 저장함");
        }
    }
}
