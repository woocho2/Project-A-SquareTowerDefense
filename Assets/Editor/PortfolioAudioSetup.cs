using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SquareTD.EditorTools
{
    [InitializeOnLoad]
    public static class PortfolioAudioSetup
    {
        public const string Output = "ArtPreviews/Audio_20261118";
        public const string AudioFolder = "Assets/4. Asset/6. Audio";
        public const string CatalogPath = "Assets/Resources/Audio/SoundCatalog.asset";
        private static double s_next;
        static PortfolioAudioSetup() { EditorApplication.update += Poll; }
        private static void Poll()
        {
            if (EditorApplication.timeSinceStartup < s_next || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
            s_next = EditorApplication.timeSinceStartup + .5;
            string request = Output + "/request.txt"; if (!File.Exists(request)) return;
            string command = File.ReadAllText(request).Trim(); File.Delete(request);
            try
            {
                if (command == "apply") { Apply(); File.WriteAllText(Output + "/apply-result.txt", "Success"); }
                else if (command == "verify") PortfolioAudioVerification.Run();
                else if (command == "defaults") { PortfolioAudioWindow.ResetVolumes(); File.WriteAllText(Output + "/volume-settings.txt", "Master=0.65, Music=0.24, Effects=0.45, UI=0.40"); }
            }
            catch (Exception exception) { File.WriteAllText(Output + "/setup-error.txt", exception.ToString()); Debug.LogException(exception); }
        }
        [MenuItem("Tools/SquareTD/사운드 파일 연결·검증")]
        public static void Apply()
        {
            var rows = new List<string>();
            string[] paths = Directory.GetFiles(AudioFolder, "*.wav", SearchOption.AllDirectories).Select(path => path.Replace('\\', '/')).ToArray();
            if (paths.Length != 113) throw new InvalidOperationException("오디오 파일은 113개여야 합니다: " + paths.Length);
            long bytes = 0;
            foreach (string path in paths)
            {
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                var importer = (AudioImporter)AssetImporter.GetAtPath(path);
                bool music = path.Contains("/BGM/");
                AudioImporterSampleSettings settings = importer.defaultSampleSettings;
                settings.loadType = music ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad;
                settings.compressionFormat = AudioCompressionFormat.Vorbis; settings.quality = .75f; settings.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
                settings.preloadAudioData = true;
                importer.defaultSampleSettings = settings; importer.forceToMono = !music; importer.loadInBackground = music; importer.SaveAndReimport();
                using (var reader = new BinaryReader(File.OpenRead(path)))
                {
                    reader.BaseStream.Position = 22; int channels = reader.ReadInt16(); int rate = reader.ReadInt32(); reader.BaseStream.Position = 40; int size = reader.ReadInt32();
                    double peak = 0, square = 0; int samples = size / 2; short first = 0, last = 0;
                    for (int i = 0; i < samples; i++) { short sample = reader.ReadInt16(); if (i == 0) first = sample; last = sample; double value = sample / 32768.0; peak = Math.Max(peak, Math.Abs(value)); square += value * value; }
                    double rms = Math.Sqrt(square / samples);
                    if (peak >= .99 || rms < .002 || channels != (music ? 2 : 1) || rate != 44100) throw new InvalidOperationException("오디오 파형 검사 실패: " + path);
                    if (music && Math.Abs(first - last) / 32768.0 > .01) throw new InvalidOperationException("루프 경계 검사 실패: " + path);
                    rows.Add($"{path} | {samples / (double)(channels * rate):0.00}s | {channels}ch | peak {peak:0.000} | RMS {rms:0.000}");
                }
                bytes += new FileInfo(path).Length;
            }
            if (!AssetDatabase.IsValidFolder("Assets/Resources/Audio")) AssetDatabase.CreateFolder("Assets/Resources", "Audio");
            SoundCatalog catalog = AssetDatabase.LoadAssetAtPath<SoundCatalog>(CatalogPath);
            if (catalog == null) { catalog = ScriptableObject.CreateInstance<SoundCatalog>(); AssetDatabase.CreateAsset(catalog, CatalogPath); }
            catalog.Music = new[] { Clip("BGM/BGM_Yggdrasil.wav"), Clip("BGM/BGM_Battle.wav"), Clip("BGM/BGM_Boss.wav") };
            catalog.Ui = Enum.GetNames(typeof(SoundCue)).Select(name => Clip("UI/" + name + ".wav")).ToArray();
            catalog.Attacks = new AudioClip[14]; catalog.Hits = new AudioClip[14]; catalog.Skills = new AudioClip[56]; catalog.Synergies = new AudioClip[14];
            for (int e = 1; e <= 14; e++)
            {
                string emblem = ((TowerEmblem)e).ToString();
                catalog.Attacks[e - 1] = Clip($"Attack/Attack_{e:00}_{emblem}.wav"); catalog.Hits[e - 1] = Clip($"Hit/Hit_{e:00}_{emblem}.wav");
                for (int c = 1; c <= 4; c++) catalog.Skills[(e - 1) * 4 + c - 1] = Clip($"Skill/Skill_{e:00}_{emblem}_{(TowerColor)c}.wav");
            }
            string[] gods = { "Odin", "Thor", "Loki", "Hel", "Surtr", "Frey", "Baldr", "Skadi", "Freyja", "Ymir", "Ullr", "Brunhild", "BrokEitri", "Njord" };
            for (int i = 0; i < 14; i++) catalog.Synergies[i] = Clip($"Synergy/Synergy_{i + 1:00}_{gods[i]}.wav");
            EditorUtility.SetDirty(catalog); AssetDatabase.SaveAssets();
            File.WriteAllLines(Output + "/audio-analysis.txt", rows);
            File.WriteAllText(Output + "/asset-summary.txt", $"파일: {paths.Length}\nWAV 합계: {bytes} bytes ({bytes / 1048576.0:0.00} MiB)\n44100Hz / 16bit / BGM stereo / SFX mono\nBGM Streaming + Vorbis, SFX DecompressOnLoad + Vorbis\n클리핑·무음·BGM 루프 경계 검사 통과\n사람이 직접 청음한 결과와 구분함\n");
        }
        private static AudioClip Clip(string relative)
        {
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(AudioFolder + "/" + relative);
            if (clip == null) throw new InvalidOperationException("오디오 연결 누락: " + relative); return clip;
        }
    }

    public sealed class PortfolioAudioWindow : EditorWindow
    {
        private int m_group;
        private Vector2 m_scroll;
        [MenuItem("Tools/SquareTD/사운드 설정·미리듣기")]
        public static void Open() => GetWindow<PortfolioAudioWindow>("사운드 미리듣기");
        public static void ResetVolumes()
        {
            if (SoundManager.Instance != null) SoundManager.Instance.SetVolumes(.65f, .24f, .45f, .4f);
            else
            {
                PlayerPrefs.SetFloat("SquareTD.Audio.Master", .65f); PlayerPrefs.SetFloat("SquareTD.Audio.Music", .24f);
                PlayerPrefs.SetFloat("SquareTD.Audio.Effects", .45f); PlayerPrefs.SetFloat("SquareTD.Audio.UI", .4f);
            }
            PlayerPrefs.Save();
        }
        private void OnGUI()
        {
            SoundCatalog catalog = AssetDatabase.LoadAssetAtPath<SoundCatalog>(PortfolioAudioSetup.CatalogPath);
            if (catalog == null) { EditorGUILayout.HelpBox("사운드 카탈로그가 없습니다.", MessageType.Info); return; }
            EditorGUILayout.LabelField("게임 볼륨", EditorStyles.boldLabel); EditorGUI.BeginChangeCheck();
            float master = EditorGUILayout.Slider("전체", PlayerPrefs.GetFloat("SquareTD.Audio.Master", .65f), 0, 1);
            float music = EditorGUILayout.Slider("배경음악", PlayerPrefs.GetFloat("SquareTD.Audio.Music", .24f), 0, 1);
            float effects = EditorGUILayout.Slider("전투 효과음", PlayerPrefs.GetFloat("SquareTD.Audio.Effects", .45f), 0, 1);
            float ui = EditorGUILayout.Slider("UI", PlayerPrefs.GetFloat("SquareTD.Audio.UI", .4f), 0, 1);
            if (EditorGUI.EndChangeCheck())
            {
                if (SoundManager.Instance != null) SoundManager.Instance.SetVolumes(master, music, effects, ui);
                else { PlayerPrefs.SetFloat("SquareTD.Audio.Master", master); PlayerPrefs.SetFloat("SquareTD.Audio.Music", music); PlayerPrefs.SetFloat("SquareTD.Audio.Effects", effects); PlayerPrefs.SetFloat("SquareTD.Audio.UI", ui); }
                PlayerPrefs.Save();
            }
            EditorGUILayout.HelpBox("볼륨은 게임에 저장됩니다. 편집 모드의 개별 미리듣기는 Unity 오디오 미리보기 볼륨을 사용합니다.", MessageType.Info);
            if (GUILayout.Button("게임 볼륨을 기본값으로")) ResetVolumes();
            m_group = EditorGUILayout.Popup("분류", m_group, new[] { "배경음악", "UI·이벤트", "기본 공격", "명중", "타워 스킬", "시너지" });
            AudioClip[] clips = new[] { catalog.Music, catalog.Ui, catalog.Attacks, catalog.Hits, catalog.Skills, catalog.Synergies }[m_group];
            if (GUILayout.Button("미리듣기 중지")) { Preview(null); if (SoundManager.Instance != null) SoundManager.Instance.StopEffects(); }
            m_scroll = EditorGUILayout.BeginScrollView(m_scroll);
            for (int i = 0; i < clips.Length; i++)
            {
                EditorGUILayout.BeginHorizontal();
                string label = clips[i] != null ? clips[i].name : "누락";
                if (m_group == 4) label = EmblemNames.GetConceptName((TowerEmblem)(i / 4 + 1), (TowerColor)(i % 4 + 1)) + " / " + SkillDescriptions.GetName(new TowerDefinition(TowerId.Make(1, (TowerColor)(i % 4 + 1), (TowerEmblem)(i / 4 + 1)), 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, TargetPriority.Closest));
                EditorGUILayout.LabelField(label);
                if (GUILayout.Button("듣기", GUILayout.Width(50))) { if (Application.isPlaying && SoundManager.Instance != null) SoundManager.Instance.Audition(clips[i]); else Preview(clips[i]); }
                if (GUILayout.Button("선택", GUILayout.Width(50))) Selection.activeObject = clips[i];
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();
        }
        private static void Preview(AudioClip clip)
        {
            Type utility = typeof(AudioImporter).Assembly.GetType("UnityEditor.AudioUtil");
            utility?.GetMethod("StopAllPreviewClips", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)?.Invoke(null, null);
            if (clip != null) utility?.GetMethod("PlayPreviewClip", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static, null, new[] { typeof(AudioClip), typeof(int), typeof(bool) }, null)?.Invoke(null, new object[] { clip, 0, false });
        }
    }
}
