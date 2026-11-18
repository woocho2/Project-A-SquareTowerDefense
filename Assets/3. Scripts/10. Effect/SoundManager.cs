using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SquareTD
{
    // 씬 간 유지되는 BGM 교차 페이드·효과음 풀·볼륨·동시 재생 제한
    public sealed class SoundManager : MonoBehaviour
    {
        public static SoundManager Instance { get; private set; }
        [SerializeField] private SoundCatalog m_catalog;
        [SerializeField, Range(0, 1)] private float m_master = .65f, m_music = .24f, m_effects = .45f, m_ui = .4f;
        private readonly Dictionary<int, float> m_lastPlay = new Dictionary<int, float>();
        private readonly System.Random m_pitchRandom = new System.Random();
        private readonly AudioSource[] m_musicSources = new AudioSource[2], m_voices = new AudioSource[12];
        private readonly float[] m_voiceGains = new float[12];
        private readonly double[] m_voiceTimes = new double[12];
        private AudioSource m_uiSource;
        private Tween m_crossfade;
        private int m_musicIndex, m_musicKind = -1;
        private int m_musicGeneration;
        private float m_musicBlend = 1;
        public SoundCatalog Catalog => m_catalog;
        public float MasterVolume => m_master;
        public float MusicVolume => m_music;
        public float EffectsVolume => m_effects;
        public float UiVolume => m_ui;
        public int PlayedCount { get; private set; }
        public string LastPlayed { get; private set; }
        public int MusicKind => m_musicKind;
        public int ActiveVoices { get { int count = 0; foreach (AudioSource source in m_voices) if (source != null && source.isPlaying) count++; return count; } }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() => Instance = null;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (Application.isBatchMode || Instance != null) return;
            new GameObject("SoundManager").AddComponent<SoundManager>();
        }
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this; DontDestroyOnLoad(gameObject);
            if (m_catalog == null) m_catalog = Resources.Load<SoundCatalog>("Audio/SoundCatalog");
            m_master = PlayerPrefs.GetFloat("SquareTD.Audio.Master", m_master); m_music = PlayerPrefs.GetFloat("SquareTD.Audio.Music", m_music);
            m_effects = PlayerPrefs.GetFloat("SquareTD.Audio.Effects", m_effects); m_ui = PlayerPrefs.GetFloat("SquareTD.Audio.UI", m_ui);
            for (int i = 0; i < m_musicSources.Length; i++) m_musicSources[i] = Source("Music " + i, true);
            for (int i = 0; i < m_voices.Length; i++) m_voices[i] = Source("Effect " + i, false);
            m_uiSource = Source("UI", false);
            SceneManager.sceneLoaded += SceneLoaded;
            SceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
        }
        private AudioSource Source(string name, bool music)
        {
            var root = new GameObject(name); root.transform.SetParent(transform, false);
            AudioSource source = root.AddComponent<AudioSource>(); source.playOnAwake = false; source.loop = music;
            source.spatialBlend = 0; source.dopplerLevel = 0; source.priority = music ? 160 : 64; return source;
        }
        private void SceneLoaded(Scene scene, LoadSceneMode mode)
        {
            foreach (AudioSource source in m_voices) source.Stop();
            if (!scene.name.StartsWith("LoadingScene")) PlayMusic(scene.name.Contains("Stage") ? 1 : 0);
            StartCoroutine(BindScene(scene));
        }
        private IEnumerator BindScene(Scene scene)
        {
            yield return null; yield return null;
            if (!scene.IsValid() || !scene.isLoaded || SimulationRunner.IsOn) yield break;
            var relay = new GameObject("Scene Sound Events"); SceneManager.MoveGameObjectToScene(relay, scene); relay.AddComponent<SceneSoundEvents>();
        }
        public void SetVolumes(float master, float music, float effects, float ui)
        {
            m_master = Mathf.Clamp01(master); m_music = Mathf.Clamp01(music); m_effects = Mathf.Clamp01(effects); m_ui = Mathf.Clamp01(ui);
            PlayerPrefs.SetFloat("SquareTD.Audio.Master", m_master); PlayerPrefs.SetFloat("SquareTD.Audio.Music", m_music);
            PlayerPrefs.SetFloat("SquareTD.Audio.Effects", m_effects); PlayerPrefs.SetFloat("SquareTD.Audio.UI", m_ui);
            ApplyVolumes();
        }
        public void PlayMusic(int kind)
        {
            if (SimulationRunner.IsOn || m_catalog == null || kind == m_musicKind) return;
            AudioClip clip = SoundCatalog.At(m_catalog.Music, kind); if (clip == null) return;
            m_crossfade?.Kill();
            int old = m_musicIndex; m_musicIndex = 1 - old; m_musicKind = kind; m_musicBlend = 0;
            AudioSource current = m_musicSources[m_musicIndex]; current.clip = clip; current.volume = 0;
            StartCoroutine(StartMusic(current, clip, old, ++m_musicGeneration));
        }
        private IEnumerator StartMusic(AudioSource current, AudioClip clip, int old, int generation)
        {
            if (clip.loadState == AudioDataLoadState.Unloaded) clip.LoadAudioData();
            while (clip.loadState == AudioDataLoadState.Loading) yield return null;
            if (generation != m_musicGeneration || clip.loadState != AudioDataLoadState.Loaded) yield break;
            current.Play();
            m_crossfade = DOVirtual.Float(0, 1, .8f, value =>
            {
                m_musicBlend = value; current.volume = m_master * m_music * value;
                m_musicSources[old].volume = m_master * m_music * (1 - value);
            }).SetUpdate(true).OnComplete(() => m_musicSources[old].Stop());
        }
        public void StopMusic()
        {
            m_musicGeneration++; m_crossfade?.Kill(); foreach (AudioSource source in m_musicSources) source.Stop(); m_musicKind = -1;
        }
        private bool Accept(int key, float interval)
        {
            if (!Application.isPlaying || SimulationRunner.IsOn || m_catalog == null || m_master <= 0) return false;
            float now = Time.unscaledTime;
            if (m_lastPlay.TryGetValue(key, out float previous) && now - previous < interval) return false;
            m_lastPlay[key] = now; return true;
        }
        private void Play(AudioClip clip, int key, float interval, float gain, bool ui = false)
        {
            if (clip == null || !Accept(key, interval)) return;
            if (ui) { m_uiSource.volume = m_master * m_ui; m_uiSource.PlayOneShot(clip, gain); }
            else
            {
                int chosen = 0; double oldest = double.MaxValue;
                for (int i = 0; i < m_voices.Length; i++)
                {
                    if (!m_voices[i].isPlaying) { chosen = i; break; }
                    if (m_voiceTimes[i] < oldest) { oldest = m_voiceTimes[i]; chosen = i; }
                }
                AudioSource source = m_voices[chosen]; source.Stop(); source.clip = clip; source.pitch = .96f + (float)m_pitchRandom.NextDouble() * .08f;
                m_voiceGains[chosen] = gain / Mathf.Sqrt(Mathf.Max(1, ActiveVoices)); source.volume = m_master * m_effects * m_voiceGains[chosen];
                m_voiceTimes[chosen] = AudioSettings.dspTime; source.Play();
            }
            PlayedCount++; LastPlayed = clip.name;
        }
        public void Cue(SoundCue cue) => Play(SoundCatalog.At(m_catalog != null ? m_catalog.Ui : null, (int)cue), 10 + (int)cue, .06f, .65f, true);
        public void Attack(TowerEmblem emblem) => Play(m_catalog != null ? m_catalog.Attack(emblem) : null, 100 + (int)emblem, .09f, .35f);
        public void Hit(TowerEmblem emblem) => Play(m_catalog != null ? m_catalog.Hit(emblem) : null, 200 + (int)emblem, .07f, .3f);
        public void Skill(TowerController tower)
        {
            if (tower == null || tower.Definition == null) return;
            Play(m_catalog != null ? m_catalog.Skill(tower.Definition.Emblem, tower.Definition.Color) : null, 300 + (int)tower.Definition.Emblem * 4 + (int)tower.Definition.Color, .2f, .6f);
        }
        public void Synergy(int id) => Play(SoundCatalog.At(m_catalog != null ? m_catalog.Synergies : null, id - 6001), 1000 + id, .3f, .65f);
        public void Contact(bool debuff) => Play(SoundCatalog.At(m_catalog != null ? m_catalog.Ui : null, (int)(debuff ? SoundCue.DebuffContact : SoundCue.BuffContact)), debuff ? 900 : 901, .2f, .15f);
        public void Audition(AudioClip clip) => Play(clip, -1, 0, .65f);
        public void StopEffects() { foreach (AudioSource source in m_voices) source.Stop(); m_uiSource.Stop(); }
        private void ApplyVolumes()
        {
            if (m_uiSource != null) m_uiSource.volume = m_master * m_ui;
            for (int i = 0; i < m_voices.Length; i++) if (m_voices[i] != null) m_voices[i].volume = m_master * m_effects * m_voiceGains[i];
            for (int i = 0; i < m_musicSources.Length; i++) if (m_musicSources[i] != null) m_musicSources[i].volume = m_master * m_music * (i == m_musicIndex ? m_musicBlend : 1 - m_musicBlend);
        }
        private void Update()
        {
            if (SimulationRunner.IsOn) { if (m_musicKind >= 0) StopMusic(); foreach (AudioSource voice in m_voices) if (voice.isPlaying) voice.Stop(); return; }
            ApplyVolumes();
        }
        private void OnApplicationQuit() => PlayerPrefs.Save();
        private void OnDestroy()
        {
            if (Instance != this) return;
            SceneManager.sceneLoaded -= SceneLoaded; m_crossfade?.Kill(); PlayerPrefs.Save(); Instance = null;
        }
    }
}
