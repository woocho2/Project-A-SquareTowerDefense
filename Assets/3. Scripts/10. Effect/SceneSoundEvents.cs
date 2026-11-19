using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace SquareTD
{
    // 웨이브·생성·합성·레벨업·시너지·게임 종료를 사운드 매니저에 연결
    public sealed class SceneSoundEvents : MonoBehaviour
    {
        private WaveManager m_wave;
        private TowerManager m_towers;
        private TurnManager m_turn;
        private int m_mergeFrame = -1;
        private void Start()
        {
            m_wave = WaveManager.Instance; m_towers = TowerManager.Instance; m_turn = TurnManager.Instance;
            if (m_wave != null) { m_wave.WaveChanged += Wave; if (m_wave.CurrentWave > 0) Wave(m_wave.CurrentWave); }
            if (m_towers != null)
            {
                m_towers.TowerAdded += Built; m_towers.TowerReplaced += Merged;
                m_towers.ColorLevels.LevelChanged += Upgraded;
                if (m_towers.Synergy != null) m_towers.Synergy.Used += Synergy;
            }
            if (m_turn != null) m_turn.StateChanged += State;
            Scan();
        }
        private void Update() { if (Pointer.current != null && Pointer.current.press.wasPressedThisFrame) Scan(); }
        private void Scan()
        {
            foreach (Button button in Object.FindObjectsByType<Button>()) if (button.GetComponent<AudioButtonFeedback>() == null) button.gameObject.AddComponent<AudioButtonFeedback>();
            foreach (Toggle toggle in Object.FindObjectsByType<Toggle>()) if (toggle.GetComponent<AudioButtonFeedback>() == null) toggle.gameObject.AddComponent<AudioButtonFeedback>();
        }
        private void Wave(int wave) { if (SoundManager.Instance == null) return; SoundManager.Instance.PlayMusic(WaveTable.HasFinalBoss(wave) ? 2 : 1); SoundManager.Instance.Cue(SoundCue.Wave); }
        private void Built(TowerController tower) { if (m_mergeFrame != Time.frameCount && SoundManager.Instance != null) SoundManager.Instance.Cue(SoundCue.Build); }
        private void Merged(TowerController oldTower, TowerController newTower) { m_mergeFrame = Time.frameCount; if (SoundManager.Instance != null) SoundManager.Instance.Cue(SoundCue.Merge); }
        private void Upgraded(int level) { if (SoundManager.Instance != null) SoundManager.Instance.Cue(SoundCue.Upgrade); }
        private void Synergy(SynergyBoard.Entry entry) { if (SoundManager.Instance != null) SoundManager.Instance.Synergy(entry.Recipe.Id); }
        private void State(ITurnState state)
        {
            if (SoundManager.Instance == null) return;
            if (state is GameClearState || state is GameOverState) { SoundManager.Instance.StopMusic(); SoundManager.Instance.Cue(state is GameClearState ? SoundCue.Victory : SoundCue.Defeat); }
        }
        private void OnDestroy()
        {
            if (m_wave != null) m_wave.WaveChanged -= Wave;
            if (m_towers != null)
            {
                m_towers.TowerAdded -= Built; m_towers.TowerReplaced -= Merged; m_towers.ColorLevels.LevelChanged -= Upgraded;
                if (m_towers.Synergy != null) m_towers.Synergy.Used -= Synergy;
            }
            if (m_turn != null) m_turn.StateChanged -= State;
        }
    }
}
