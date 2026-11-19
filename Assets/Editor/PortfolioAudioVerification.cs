using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SquareTD.EditorTools
{
    [InitializeOnLoad]
    public static class PortfolioAudioVerification
    {
        private const string Key = "SquareTD.AudioVerification", Folder = PortfolioAudioSetup.Output;
        private static readonly List<string> Checks = new List<string>(), Errors = new List<string>(), Existing = new List<string>();
        private static double s_next;
        private static int s_arrivals, s_managerId, s_before;
        private static float s_master, s_music, s_effects, s_ui;
        private static TowerController s_red, s_blue, s_yellow, s_purple;
        [Serializable] private class Scenes { public SceneSetup[] values; }
        [Serializable] private class Report { public bool verified; public string[] checks, errors, existingErrors; public bool listenedByHuman; }
        static PortfolioAudioVerification() { EditorApplication.update += Tick; Application.logMessageReceived += Log; }
        [MenuItem("Tools/SquareTD/사운드 재생 연결 검증")]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || Enumerable.Range(0, SceneManager.sceneCount).Any(i => SceneManager.GetSceneAt(i).isDirty)) throw new InvalidOperationException("플레이를 종료하고 열린 씬을 저장해 주세요.");
            SimulationSettings settings = SimulationSettings.Load(); if (settings != null && settings.Active && !settings.Done) throw new InvalidOperationException("밸런스 시뮬레이션이 진행 중입니다.");
            SessionState.SetString(Key + ".scenes", JsonUtility.ToJson(new Scenes { values = EditorSceneManager.GetSceneManagerSetup() }));
            SessionState.SetFloat(Key + ".deadline", (float)EditorApplication.timeSinceStartup + 180);
            SessionState.SetInt(Key, 1); Checks.Clear(); Errors.Clear(); Existing.Clear();
            SessionState.SetBool(Key + ".captured", false);
            SessionState.SetBool(Key + ".background", Application.runInBackground);
            EditorSceneManager.OpenScene("Assets/1. Scenes/2. Menu.unity", OpenSceneMode.Single); EditorApplication.isPaused = false; EditorApplication.isPlaying = true;
        }
        private static void Check(bool value, string label) { if (!value) throw new InvalidOperationException(label); Checks.Add(label); }
        private static void Next(int step, double delay = .5) { SessionState.SetInt(Key, step); s_next = EditorApplication.timeSinceStartup + delay; }
        private static void Log(string text, string stack, LogType type)
        {
            if (SessionState.GetInt(Key, 0) <= 0 || (type != LogType.Error && type != LogType.Exception && type != LogType.Assert)) return;
            if (text.StartsWith("Cannot instantiate objects with a parent which is persistent.") && stack.Contains("TowerFactory")) Existing.Add(text); else Errors.Add(text);
        }
        private static void Tick()
        {
            int step = SessionState.GetInt(Key, 0); if (step == 0 || EditorApplication.isCompiling) return;
            EditorApplication.QueuePlayerLoopUpdate();
            if (EditorApplication.timeSinceStartup < s_next) return;
            try
            {
                if (step == 100)
                {
                    if (EditorApplication.isPlayingOrWillChangePlaymode) return;
                    SessionState.SetInt(Key, 0); Scenes restore = JsonUtility.FromJson<Scenes>(SessionState.GetString(Key + ".scenes", ""));
                    if (restore?.values != null) EditorSceneManager.RestoreSceneManagerSetup(restore.values); return;
                }
                if (EditorApplication.timeSinceStartup > SessionState.GetFloat(Key + ".deadline", 0)) throw new TimeoutException("사운드 검증 시간 초과: " + step);
                if (!EditorApplication.isPlaying) return;
                Application.runInBackground = true;
                EditorApplication.isPaused = false;
                SoundManager sound = SoundManager.Instance;
                if (step == 1)
                {
                    File.WriteAllText(Folder + "/startup-state.txt", $"Frame={Time.frameCount}, sound={sound != null}, relay={Object.FindAnyObjectByType<SceneSoundEvents>() != null}, simulation={SimulationRunner.IsOn}, timeScale={Time.timeScale}, scene={SceneManager.GetActiveScene().name}");
                    if (sound == null || Object.FindAnyObjectByType<SceneSoundEvents>() == null) return;
                    SessionState.SetFloat(Key + ".listener", AudioListener.volume); AudioListener.volume = 0;
                    s_master = sound.MasterVolume; s_music = sound.MusicVolume; s_effects = sound.EffectsVolume; s_ui = sound.UiVolume;
                    SessionState.SetBool(Key + ".captured", true);
                    sound.SetVolumes(.65f, .24f, .45f, .4f); s_managerId = sound.GetEntityId().GetHashCode();
                    AudioClip[] clips = sound.Catalog.Music.Concat(sound.Catalog.Ui).Concat(sound.Catalog.Attacks).Concat(sound.Catalog.Hits).Concat(sound.Catalog.Skills).Concat(sound.Catalog.Synergies).ToArray();
                    Check(clips.Length == 113 && clips.All(clip => clip != null && clip.length > 0), "오디오 113개 카탈로그 연결");
                    Check(sound.Catalog.Music.All(clip => clip.loadType == AudioClipLoadType.Streaming), "BGM 스트리밍 설정");
                    Check(sound.Catalog.Skills.Length == 56 && sound.Catalog.Synergies.Length == 14, "일반 56종·시너지 14종 스킬 사운드");
                    Next(2, 1); return;
                }
                if (sound == null) throw new InvalidOperationException("사운드 매니저가 사라졌습니다.");
                switch (step)
                {
                    case 2:
                        File.WriteAllText(Folder + "/music-state.txt", "kind=" + sound.MusicKind + " dsp=" + AudioSettings.dspTime + " listenerPaused=" + AudioListener.pause + "\n" + string.Join("\n", sound.GetComponentsInChildren<AudioSource>().Where(source => source.loop).Select(source => source.name + " playing=" + source.isPlaying + " clip=" + source.clip?.name + " state=" + source.clip?.loadState + " samples=" + source.timeSamples + " volume=" + source.volume)));
                        if (sound.MusicKind == 0 && !sound.GetComponentsInChildren<AudioSource>().Any(source => source.loop && source.isPlaying)) { s_next = EditorApplication.timeSinceStartup + .25; return; }
                        Check(sound.MusicKind == 0 && sound.GetComponentsInChildren<AudioSource>().Any(source => source.loop && source.isPlaying), "메뉴 BGM 실제 재생");
                        StageSelectPanel stage = Object.FindAnyObjectByType<StageSelectPanel>();
                        Button button = new SerializedObject(stage).FindProperty("btn_stageSelect").objectReferenceValue as Button;
                        Check(button.GetComponent<AudioButtonFeedback>() != null, "실제 메뉴 버튼 자동 사운드 연결");
                        s_before = sound.PlayedCount; button.onClick.Invoke();
                        Check(sound.PlayedCount > s_before && sound.LastPlayed == "Confirm", "메뉴 선택 클릭음 재생");
                        SceneFlow.Load("3. Stage1"); Next(3, .1); break;
                    case 3:
                        if (SceneManager.GetActiveScene().name != "3. Stage1" || TowerManager.Instance?.Database == null || Object.FindAnyObjectByType<SceneSoundEvents>() == null) return;
                        Check(sound.GetEntityId().GetHashCode() == s_managerId && Object.FindObjectsByType<SoundManager>().Length == 1, "씬 이동 후 매니저 유지·중복 없음");
                        Next(4, 1); break;
                    case 4:
                        Check(sound.MusicKind == 1, "전투 BGM 전환");
                        s_red = TowerManager.Instance.SpawnForDebug(TowerManager.Instance.Database.Get(1101));
                        s_blue = TowerManager.Instance.SpawnForDebug(TowerManager.Instance.Database.Get(1202));
                        s_yellow = TowerManager.Instance.SpawnForDebug(TowerManager.Instance.Database.Get(1301));
                        s_purple = TowerManager.Instance.SpawnForDebug(TowerManager.Instance.Database.Get(1401));
                        s_arrivals = 0; s_before = sound.PlayedCount;
                        Vector3 from = s_red.transform.position, to = from + Vector3.right * EffectSpace.CellWorldSize();
                        for (int e = 1; e <= 14; e++) ProjectileManager.Instance.Fire(100 + e, from, to, 15, .05f, .5f, () => s_arrivals++);
                        Check(sound.PlayedCount >= s_before + 14, "14문양 실제 발사 경로에서 공격음 재생");
                        Check(sound.ActiveVoices <= 12, "동시 재생 풀 12개 제한"); Next(5, .1); break;
                    case 5:
                        if (s_arrivals < 14) return;
                        Check(s_arrivals == 14 && sound.PlayedCount >= s_before + 28, "14문양 도착 콜백·명중음 연결");
                        s_before = sound.PlayedCount;
                        UnityEngine.Random.State randomState = UnityEngine.Random.state;
                        for (int i = 0; i < 100; i++) sound.Attack(TowerEmblem.Sword);
                        Check(sound.PlayedCount == s_before + 1, "동일 공격음 연타 제한");
                        Check(UnityEngine.Random.state.Equals(randomState), "사운드 피치 변화가 게임 난수에 영향 없음"); Next(6); break;
                    case 6:
                        s_before = sound.PlayedCount;
                        SkillEffectTable.Play(s_red, s_red.transform.position, new[] { s_red.transform.position });
                        Check(sound.PlayedCount == s_before + 1 && sound.LastPlayed == "Skill_01_Sword_Red", "광역 스킬 연출에 맞는 사운드");
                        SkillEffectTable.PlayCast(s_blue);
                        Check(sound.LastPlayed == "Skill_02_Axe_Blue", "단일 시전형 스킬 사운드");
                        SkillEffectTable.PlaySupportSkill(s_yellow, new[] { s_red });
                        Check(sound.LastPlayed == "Skill_01_Sword_Yellow", "버프 스킬 사운드");
                        SkillEffectTable.PlayDebuffSkill(s_purple, new[] { s_red.transform.position });
                        Check(sound.LastPlayed == "Skill_01_Sword_Purple", "디버프 스킬 사운드");
                        sound.Synergy(6001); Check(sound.LastPlayed == "Synergy_01_Odin", "시너지별 사운드 선택"); Next(7, .5); break;
                    case 7:
                        while (WaveManager.Instance.CurrentWave < 10) WaveManager.Instance.NextWave();
                        Check(sound.MusicKind == 2, "보스 웨이브 이벤트의 BGM 전환");
                        WaveManager.Instance.NextWave(); Check(sound.MusicKind == 1, "다음 일반 웨이브 전투 BGM 복귀");
                        Time.timeScale = 0;
                        sound.Cue(SoundCue.Click); Check(sound.LastPlayed == "Click", "일시정지 중 UI 소리");
                        sound.SetVolumes(0, .24f, .45f, .4f); s_before = sound.PlayedCount; sound.Attack(TowerEmblem.Water);
                        Check(sound.PlayedCount == s_before && sound.GetComponentsInChildren<AudioSource>().All(source => source.volume == 0), "전체 볼륨 0 음소거");
                        sound.StopEffects(); Check(sound.ActiveVoices == 0, "미리듣기 효과음 중지");
                        sound.SetVolumes(.65f, .24f, .45f, .4f); Next(8); break;
                    case 8:
                        GameSession.Instance.GameClear();
                        Check(sound.MusicKind == -1 && sound.LastPlayed == "Victory", "클리어 이벤트 BGM 정지·승리음");
                        Type audio = typeof(AudioImporter).Assembly.GetType("UnityEditor.AudioUtil");
                        Check(audio?.GetMethod("PlayPreviewClip", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static, null, new[] { typeof(AudioClip), typeof(int), typeof(bool) }, null) != null, "에디터 미리듣기 API 확인");
                        Check(Errors.Count == 0, "새 사운드 오류 없음");
                        File.WriteAllText(Folder + "/playmode-result.json", JsonUtility.ToJson(new Report { verified = true, checks = Checks.ToArray(), errors = Errors.ToArray(), existingErrors = Existing.ToArray(), listenedByHuman = false }, true));
                        Finish(); break;
                }
            }
            catch (Exception exception) { File.WriteAllText(Folder + "/verification-error.txt", exception.ToString()); Finish(); }
        }
        private static void Finish()
        {
            if (SessionState.GetBool(Key + ".captured", false))
            {
                if (SoundManager.Instance != null) SoundManager.Instance.SetVolumes(s_master, s_music, s_effects, s_ui);
                AudioListener.volume = SessionState.GetFloat(Key + ".listener", 1);
            }
            Application.runInBackground = SessionState.GetBool(Key + ".background", false); Time.timeScale = 1;
            SessionState.SetInt(Key, 100); EditorApplication.isPlaying = false;
        }
    }
}
