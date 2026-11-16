using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SquareTD.EditorTools
{
    // 몬스터 공통 프레임·상태 배지·최종 보스 기믹 배지를 연결하는 에디터 도구
    public static class EnemyCubeVisualSetup
    {
        private const string Common = "Assets/4. Asset/0. Common/";
        private const string Enemy = "Assets/4. Asset/4. Enemy/";
        private const string PrefabPath = "Assets/2. Prefab/2. Enemy.prefab";
        private static readonly string[] Names = { "Heal", "Defense", "Action", "Mobility", "Locked", "Hourglass" };

        [MenuItem("Tools/SquareTD/Apply Enemy Cube Visuals")]
        public static void Apply()
        {
            AssetDatabase.ImportAsset(Enemy + "Enemy_State_Badges_Atlas.png", ImportAssetOptions.ForceSynchronousImport);
            string iconPath = Enemy + "Enemy_Boss_Gimmick_Badge.png";
            AssetDatabase.ImportAsset(iconPath, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(iconPath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 256;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = true;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 256;
            importer.SaveAndReimport();

            Texture2D thin = AssetDatabase.LoadAssetAtPath<Texture2D>(Common + "Board_Decoration_Thin.png");
            var materials = new Material[Names.Length];
            for (int i = 0; i < Names.Length; i++)
            {
                materials[i] = AssetDatabase.LoadAssetAtPath<Material>(Common + "Enemy_State_" + Names[i] + ".mat");
                if (materials[i] == null || thin == null) throw new InvalidOperationException("Enemy frame material is missing");
                materials[i].SetTexture("_DecorationTex", thin);
                EditorUtility.SetDirty(materials[i]);
            }
            Color gray = new Color(0.7830189f, 0.7830189f, 0.7830189f, 1f);
            materials[4].SetColor("_DecorationColor", new Color(0.06f, 0.06f, 0.06f, 1f));
            materials[5].SetColor("_DecorationColor", gray);

            string bossPath = Common + "Enemy_State_BossGimmick.mat";
            Material boss = AssetDatabase.LoadAssetAtPath<Material>(bossPath);
            if (boss == null)
            {
                boss = new Material(materials[4]);
                AssetDatabase.CreateAsset(boss, bossPath);
            }
            else boss.CopyPropertiesFromMaterial(materials[4]);
            boss.name = "Enemy_State_BossGimmick";
            boss.SetTexture("_BadgeTex", AssetDatabase.LoadAssetAtPath<Texture2D>(iconPath));
            boss.SetTextureScale("_BadgeTex", Vector2.one);
            boss.SetTextureOffset("_BadgeTex", Vector2.zero);
            EditorUtility.SetDirty(boss);

            GameObject prefab = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                var cube = prefab.GetComponentInChildren<EnemyCubeMeshVisual>(true);
                var serialized = new SerializedObject(cube);
                serialized.FindProperty("m_bossMechanicMaterial").objectReferenceValue = boss;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(prefab, PrefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(prefab); }
            AssetDatabase.SaveAssets();
            ValidateSwap(materials, boss);
            RenderPreview(materials, boss);
            Debug.Log("[EnemyCubeVisualSetup] Applied shared thin frame, black damage-up, gray damage-down and final-boss-only crown badge.");
        }

        private static void ValidateSwap(Material[] materials, Material boss)
        {
            var go = new GameObject("EnemyBadgeValidation");
            try
            {
                go.AddComponent<MeshFilter>().sharedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(Enemy + "Enemy_StateCube.asset");
                MeshRenderer renderer = go.AddComponent<MeshRenderer>();
                renderer.sharedMaterials = (Material[])materials.Clone();
                var cube = go.AddComponent<EnemyCubeMeshVisual>();
                var serialized = new SerializedObject(cube);
                SerializedProperty pool = serialized.FindProperty("m_faceAssetPool");
                pool.arraySize = 6;
                for (int i = 0; i < 6; i++) pool.GetArrayElementAtIndex(i).objectReferenceValue = materials[i];
                serialized.FindProperty("m_bossMechanicMaterial").objectReferenceValue = boss;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
                typeof(EnemyCubeMeshVisual).GetMethod("Awake", flags).Invoke(cube, null);
                var indices = (int[])typeof(EnemyCubeMeshVisual).GetField("m_faceAssetIndices", flags).GetValue(cube);
                for (int i = 0; i < 6; i++) indices[i] = (int)EnemyCubeFaceAsset.Locked;
                cube.SetFinalBoss(true);
                for (int i = 0; i < 6; i++)
                {
                    if (renderer.sharedMaterials[i] != boss || cube.GetFaceAsset((EnemyCubeFace)i) != EnemyCubeFaceAsset.Locked)
                        throw new InvalidOperationException("Final-boss badge changed the logical face or did not swap material");
                }
                cube.SetFinalBoss(false);
                for (int i = 0; i < 6; i++)
                    if (renderer.sharedMaterials[i] != materials[4])
                        throw new InvalidOperationException("Pooled normal/middle enemy retained boss badge");
                File.WriteAllText(Path.GetFullPath("ArtPreviews/EnemyCubeIcons_20261116/material-swap-validation.json"),
                    "{\"finalBossBadge\":true,\"logicalFacePreserved\":true,\"normalPoolReuse\":true,\"facesChecked\":6,\"playModeVerified\":false}");
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }

        private static void RenderPreview(Material[] materials, Material boss)
        {
            Scene scene = EditorSceneManager.NewPreviewScene();
            RenderTexture target = null;
            Texture2D pixels = null;
            Camera camera = null;
            RenderTexture previous = RenderTexture.active;
            try
            {
                var cameraObject = new GameObject("EnemyPreviewCamera");
                SceneManager.MoveGameObjectToScene(cameraObject, scene);
                camera = cameraObject.AddComponent<Camera>();
                camera.scene = scene;
                camera.transform.position = new Vector3(0, 0, -10);
                camera.orthographic = true;
                camera.orthographicSize = 2;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(30 / 255f, 32 / 255f, 45 / 255f);
                target = new RenderTexture(1400, 400, 24, RenderTextureFormat.ARGB32);
                camera.targetTexture = target;
                var appearance = AssetDatabase.LoadAssetAtPath<EnemyCubeAppearance>(Enemy + "Enemy_Data/Valkyrie_Appearance.asset");
                Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(Enemy + "Enemy_StateCube.asset");
                string[] labels = { "Heal", "Defense", "Action", "Move", "Taken +", "Taken -", "Boss" };
                Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                for (int i = 0; i < 7; i++)
                {
                    float x = (i - 3) * 1.5f;
                    var cubeObject = new GameObject(labels[i]);
                    SceneManager.MoveGameObjectToScene(cubeObject, scene);
                    cubeObject.transform.position = new Vector3(x, 0.15f, 0);
                    cubeObject.AddComponent<MeshFilter>().sharedMesh = mesh;
                    MeshRenderer renderer = cubeObject.AddComponent<MeshRenderer>();
                    Material material = i < 6 ? materials[i] : boss;
                    renderer.sharedMaterials = new[] { material, material, material, material, material, material };
                    if (appearance != null)
                    {
                        Rect uv = appearance.CharacterUvRect;
                        var block = new MaterialPropertyBlock();
                        block.SetTexture("_MainTex", appearance.CharacterTexture);
                        block.SetVector("_MainTex_ST", new Vector4(uv.width, uv.height, uv.x, uv.y));
                        renderer.SetPropertyBlock(block);
                    }
                    var labelObject = new GameObject("Label");
                    SceneManager.MoveGameObjectToScene(labelObject, scene);
                    labelObject.transform.position = new Vector3(x, -0.65f, -0.6f);
                    var label = labelObject.AddComponent<TextMesh>();
                    label.text = labels[i];
                    label.font = font;
                    label.fontSize = 48;
                    label.characterSize = 0.07f;
                    label.anchor = TextAnchor.MiddleCenter;
                    label.color = Color.white;
                    label.GetComponent<MeshRenderer>().sharedMaterial = font.material;
                }
                camera.Render();
                RenderTexture.active = target;
                pixels = new Texture2D(target.width, target.height, TextureFormat.RGBA32, false);
                pixels.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
                pixels.Apply();
                string path = Path.GetFullPath("ArtPreviews/EnemyCubeIcons_20261116/unity-material-preview.png");
                File.WriteAllBytes(path, pixels.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = previous;
                if (camera != null) camera.targetTexture = null;
                if (pixels != null) UnityEngine.Object.DestroyImmediate(pixels);
                if (target != null) UnityEngine.Object.DestroyImmediate(target);
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }
    }
}
