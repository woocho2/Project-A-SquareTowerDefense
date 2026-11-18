using System;
using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SquareTD
{
    // 무거운 로딩 씬을 읽기 전에 즉시 그리는 가벼운 전환 화면
    public sealed class SceneTransitionOverlay : MonoBehaviour
    {
        private Tween m_motion;
        public int FirstRenderedFrame { get; private set; } = -1;
        public int LoadingStartedFrame { get; private set; } = -1;
        public static int LastOverlayFrame { get; private set; } = -1;
        public static int LastLoadFrame { get; private set; } = -1;

        public static void Begin(string loadingScene)
        {
            LastOverlayFrame = Time.frameCount;
            LastLoadFrame = -1;
            var root = new GameObject("SceneTransitionOverlay", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(SceneTransitionOverlay));
            Canvas canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = short.MaxValue;
            CanvasScaler scaler = root.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = 1;
            var shade = new GameObject("Background", typeof(RectTransform), typeof(Image)); shade.transform.SetParent(root.transform, false);
            RectTransform rect = (RectTransform)shade.transform; rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
            shade.GetComponent<Image>().color = new Color32(17, 23, 43, 255);
            var label = new GameObject("Loading", typeof(RectTransform), typeof(TextMeshProUGUI)); label.transform.SetParent(root.transform, false);
            rect = (RectTransform)label.transform; rect.anchorMin = rect.anchorMax = new Vector2(.5f, 0); rect.pivot = new Vector2(0, 0);
            rect.anchoredPosition = new Vector2(-700, 145); rect.sizeDelta = new Vector2(1400, 70);
            TextMeshProUGUI text = label.GetComponent<TextMeshProUGUI>(); text.text = "Loading..."; text.fontSize = 30; text.color = Color.white; text.raycastTarget = false;
            var bar = new GameObject("ProgressTrack", typeof(RectTransform), typeof(Image)); bar.transform.SetParent(root.transform, false);
            rect = (RectTransform)bar.transform; rect.anchorMin = rect.anchorMax = new Vector2(.5f, 0); rect.anchoredPosition = new Vector2(0, 122); rect.sizeDelta = new Vector2(1400, 12);
            bar.GetComponent<Image>().color = new Color32(72, 85, 108, 255);
            SceneTransitionOverlay transition = root.GetComponent<SceneTransitionOverlay>();
            transition.m_motion = text.DOFade(.55f, .55f).SetLoops(-1, LoopType.Yoyo).SetUpdate(true);
            transition.StartCoroutine(transition.Load(loadingScene));
        }

        private IEnumerator Load(string sceneName)
        {
            yield return null;
            FirstRenderedFrame = Time.frameCount;
            yield return null;
            LoadingStartedFrame = Time.frameCount;
            LastLoadFrame = LoadingStartedFrame;
            AsyncOperation operation;
            try { operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single); }
            catch (Exception exception) { SceneFlow.FinishLoading(); Debug.LogException(exception); Destroy(gameObject); yield break; }
            if (operation == null) { SceneFlow.FinishLoading(); Destroy(gameObject); yield break; }
            yield return operation;
        }

        private void OnDestroy() => m_motion?.Kill();
    }
}
