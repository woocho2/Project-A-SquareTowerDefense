using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SquareTD
{
    // 실제 비동기 씬 로딩과 타워 소개·진행률·글자 연출
    public sealed class LoadingSceneController : MonoBehaviour
    {
        [SerializeField] private string m_fallbackScene = "2. Menu";
        [SerializeField] private TextAsset m_towerCsv;
        [SerializeField] private TowerCharacterCatalog m_catalog;
        [SerializeField] private CanvasGroup m_fadeGroup;
        [SerializeField] private RectTransform m_safeArea;
        [SerializeField] private Slider m_progress;
        [SerializeField] private TextMeshProUGUI m_percent, m_status, m_name, m_category, m_introduction;
        [SerializeField] private TextMeshProUGUI m_basicTitle, m_basic, m_skillTitle, m_skill;
        [SerializeField] private Image m_portrait;
        [SerializeField] private RectTransform[] m_letters = Array.Empty<RectTransform>();
        [SerializeField, Min(0f)] private float m_minimumDisplayTime = 4f;
        [SerializeField] private bool m_previewOnly;
        [SerializeField] private int m_previewTowerId = 1101;
        private readonly List<Tween> m_animations = new List<Tween>();
        private Tween m_progressTween;
        private AsyncOperation m_operation;
        private float m_target;
        public float DisplayProgress => m_progress != null ? m_progress.value : 0;
        public string TowerName => m_name != null ? m_name.text : "";
        public string Destination { get; private set; }

        private IEnumerator Start()
        {
            FitSafeArea();
            ShowTower(m_previewOnly ? m_previewTowerId : PickTower());
            AnimateLetters();
            m_fadeGroup.alpha = 0;
            m_animations.Add(m_fadeGroup.DOFade(1, .25f).SetUpdate(true));
            SetProgress(0, true);
            Destination = SceneFlow.PendingScene ?? m_fallbackScene;
            m_status.text = "다음 화면을 준비하고 있습니다";
            if (m_previewOnly) { SetProgress(.64f); yield break; }
            yield return null;
            yield return null;
            if (string.IsNullOrEmpty(Destination) || Destination == SceneManager.GetActiveScene().name || !Application.CanStreamedLevelBeLoaded(Destination))
            {
                m_status.text = "이동할 씬을 확인해 주세요";
                Debug.LogError($"[LoadingScene] '{Destination}' 씬을 불러올 수 없습니다.");
                SceneFlow.FinishLoading();
                yield break;
            }
            float started = Time.realtimeSinceStartup;
            try { m_operation = SceneManager.LoadSceneAsync(Destination, LoadSceneMode.Single); }
            catch (Exception exception)
            {
                m_status.text = "씬을 불러오지 못했습니다";
                SceneFlow.FinishLoading();
                Debug.LogException(exception);
                yield break;
            }
            if (m_operation == null)
            {
                m_status.text = "씬을 불러오지 못했습니다";
                SceneFlow.FinishLoading();
                yield break;
            }
            m_operation.allowSceneActivation = false;
            while (m_operation.progress < .9f || Time.realtimeSinceStartup - started < m_minimumDisplayTime)
            {
                SetProgress(Mathf.Clamp01(m_operation.progress / .9f) * .97f);
                yield return null;
            }
            m_status.text = "준비 완료";
            SetProgress(1);
            if (m_progressTween != null) yield return m_progressTween.WaitForCompletion();
            yield return new WaitForSecondsRealtime(.12f);
            Tween fade = m_fadeGroup.DOFade(0, .25f).SetUpdate(true);
            m_animations.Add(fade);
            yield return fade.WaitForCompletion();
            SceneFlow.FinishLoading();
            m_operation.allowSceneActivation = true;
        }

        private int PickTower()
        {
            var choices = new List<int>();
            for (int emblem = 1; emblem <= 14; emblem++)
                for (int color = 1; color <= 4; color++)
                    if (m_catalog != null && m_catalog.GetCharacter(emblem, color) != null)
                        choices.Add(TowerId.Make(1, (TowerColor)color, (TowerEmblem)emblem));
            return choices.Count > 0 ? choices[UnityEngine.Random.Range(0, choices.Count)] : 1101;
        }

        public void ShowTower(int id)
        {
            var database = new TowerDatabase();
            if (m_towerCsv == null) throw new InvalidOperationException("로딩 화면의 Tower CSV가 비어 있습니다.");
            database.Load(m_towerCsv.text);
            TowerDefinition definition = database.Get(id);
            LoadingTowerProfile profile = LoadingTowerProfile.Create(definition);
            m_name.text = profile.Name;
            m_category.text = profile.Category;
            m_introduction.text = profile.Introduction;
            m_basicTitle.text = profile.Description.BasicTitle;
            m_basic.text = TowerDescription.Plain(profile.Description.Basic);
            m_skillTitle.text = profile.Description.SkillTitle;
            m_skill.text = TowerDescription.Plain(profile.Description.Skill);
            m_portrait.sprite = m_catalog.GetCharacter((int)definition.Emblem, (int)definition.Color);
            m_portrait.preserveAspect = true;
            m_portrait.enabled = m_portrait.sprite != null;
        }

        private void SetProgress(float value, bool immediate = false)
        {
            value = Mathf.Clamp01(value);
            if (!immediate && value <= m_target) return;
            m_target = value;
            m_progressTween?.Kill();
            if (immediate) { ApplyProgress(value); return; }
            m_progressTween = DOVirtual.Float(DisplayProgress, value, .28f, ApplyProgress).SetEase(Ease.OutSine).SetUpdate(true);
        }

        private void ApplyProgress(float value)
        {
            m_progress.SetValueWithoutNotify(value);
            m_percent.text = Mathf.FloorToInt(value * 100f) + "%";
        }

        private void AnimateLetters()
        {
            for (int i = 0; i < m_letters.Length; i++)
            {
                RectTransform letter = m_letters[i];
                if (letter == null) continue;
                Sequence motion = DOTween.Sequence().SetUpdate(true);
                motion.AppendInterval(i * .055f);
                motion.Append(letter.DOAnchorPosY(letter.anchoredPosition.y + 8, .22f).SetEase(Ease.OutSine));
                motion.Append(letter.DOAnchorPosY(letter.anchoredPosition.y, .28f).SetEase(Ease.InSine));
                motion.AppendInterval(1.25f - i * .055f);
                motion.SetLoops(-1);
                m_animations.Add(motion);
            }
        }

        private void FitSafeArea()
        {
            if (m_safeArea == null || Screen.width <= 0 || Screen.height <= 0) return;
            Rect area = Screen.safeArea;
            m_safeArea.anchorMin = new Vector2(area.xMin / Screen.width, area.yMin / Screen.height);
            m_safeArea.anchorMax = new Vector2(area.xMax / Screen.width, area.yMax / Screen.height);
            m_safeArea.offsetMin = m_safeArea.offsetMax = Vector2.zero;
        }

        private void OnDestroy()
        {
            m_progressTween?.Kill();
            foreach (Tween tween in m_animations) tween?.Kill();
            if (m_operation != null && !m_operation.isDone) m_operation.allowSceneActivation = true;
        }
    }
}
