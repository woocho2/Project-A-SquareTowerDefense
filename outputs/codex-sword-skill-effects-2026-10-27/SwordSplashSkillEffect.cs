using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace SquareTD
{
    // 무작위 절단선을 그은 뒤, 나뉜 화면 조각을 살짝 어긋나게 이동시킨다.
    [DisallowMultipleComponent]
    public sealed class SwordSplashSkillEffect : MonoBehaviour
    {
        private const int LineCount = 4; // Keep in sync with CUT_COUNT in Resources/AreaSlash.shader.
        private static readonly int s_elapsedId = Shader.PropertyToID("_Elapsed");
        private static readonly int s_cutsId = Shader.PropertyToID("_Cuts");
        private static readonly int s_viewportSizeId = Shader.PropertyToID("_ViewportSize");
        private static readonly int s_lineIntervalId = Shader.PropertyToID("_LineInterval");
        private static readonly int s_drawDurationId = Shader.PropertyToID("_DrawDuration");
        private static readonly int s_cutProgressId = Shader.PropertyToID("_CutProgress");
        private static readonly int s_cutEnergyId = Shader.PropertyToID("_CutEnergy");
        private static readonly int s_movementId = Shader.PropertyToID("_Movement");
        private static readonly int s_lineOpacityId = Shader.PropertyToID("_LineOpacity");
        private static readonly int s_flashId = Shader.PropertyToID("_Flash");
        private static readonly int s_coverOpacityId = Shader.PropertyToID("_CoverOpacity");
        private static readonly int s_coverSpreadId = Shader.PropertyToID("_CoverSpread");
        private static readonly int s_cutViewportId = Shader.PropertyToID("_CutViewport");
        private static readonly int s_coverOnlyId = Shader.PropertyToID("_CoverOnly");
        private static readonly int s_chargeId = Shader.PropertyToID("_Charge");
        private static readonly int s_impactId = Shader.PropertyToID("_Impact");
        private static readonly int s_screenOffsetId = Shader.PropertyToID("_ScreenOffsetPixels");
        private static readonly int s_vfxStrengthId = Shader.PropertyToID("_VfxStrength");
        private static readonly int s_sceneReadyId = Shader.PropertyToID("_SceneReady");
        private static readonly int s_motionTimesId = Shader.PropertyToID("_MotionTimes");
        private static readonly int s_movePixelsId = Shader.PropertyToID("_MovePixels");

        // 연출용 난수는 게임 로직의 UnityEngine.Random 상태에 영향을 주지 않는다.
        private readonly System.Random m_random = new System.Random();
        private readonly Vector4[] m_cuts = new Vector4[LineCount];
        private readonly float[] m_cutProgress = new float[LineCount];
        private readonly float[] m_cutEnergy = new float[LineCount];
        private Sequence m_timeline;
        private Material m_material;
        private Material m_coverMaterial;
        private Camera m_sourceCamera;
        private Camera m_outputCamera;
        private RenderTexture m_frame;
        private RenderTexture m_originalTarget;
        private Canvas m_canvas;
        private Canvas m_coverCanvas;
        private RawImage m_image;
        private Vector2 m_viewportSize;
        private Rect m_viewport;
        private int m_screenWidth;
        private int m_screenHeight;
        private float m_elapsed;
        private float m_duration;
        private float m_motionStart;
        private float m_movement;
        private float m_lineOpacity;
        private float m_flash;
        private float m_coverOpacity;
        private float m_coverSpread;
        private float m_charge;
        private float m_impact;
        private Vector2 m_screenOffset;
        private bool m_playing;
        private bool m_capturing;

        private void Awake()
        {
            Shader shader = Resources.Load<Shader>("AreaSlash");
            if (shader == null || !shader.isSupported)
            {
                Debug.LogWarning("[SwordSplashSkillEffect] AreaSlash 셰이더를 사용할 수 없습니다.", this);
                enabled = false;
                return;
            }

            m_material = new Material(shader) { name = "Screen Cut Lines (Runtime)" };
            var canvasObject = new GameObject("Screen Cut Lines Canvas", typeof(RectTransform), typeof(Canvas));
            canvasObject.transform.SetParent(transform, false);
            m_canvas = canvasObject.GetComponent<Canvas>();
            m_canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            m_canvas.sortingOrder = short.MinValue;
            m_canvas.enabled = false;

            var imageObject = new GameObject("Cut Lines", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
            imageObject.transform.SetParent(canvasObject.transform, false);
            m_image = imageObject.GetComponent<RawImage>();
            m_image.material = m_material;
            m_image.texture = Texture2D.whiteTexture;
            m_image.raycastTarget = false;
            m_image.maskable = false;

            // Keep the finishing light above the HUD as well as the sliced scene.
            m_coverMaterial = new Material(shader) { name = "Screen Cut Light Cover (Runtime)" };
            m_coverMaterial.SetFloat(s_coverOnlyId, 1f);
            var coverObject = new GameObject("Screen Cut Light Cover", typeof(RectTransform), typeof(Canvas));
            coverObject.transform.SetParent(transform, false);
            m_coverCanvas = coverObject.GetComponent<Canvas>();
            m_coverCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            m_coverCanvas.sortingOrder = short.MaxValue;
            m_coverCanvas.enabled = false;
            var coverImageObject = new GameObject("Full Screen Light", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
            coverImageObject.transform.SetParent(coverObject.transform, false);
            RawImage coverImage = coverImageObject.GetComponent<RawImage>();
            coverImage.material = m_coverMaterial;
            coverImage.texture = Texture2D.whiteTexture;
            coverImage.raycastTarget = false;
            coverImage.maskable = false;
            coverImage.rectTransform.anchorMin = Vector2.zero;
            coverImage.rectTransform.anchorMax = Vector2.one;
            coverImage.rectTransform.offsetMin = Vector2.zero;
            coverImage.rectTransform.offsetMax = Vector2.zero;

            var output = new GameObject("Screen Cut Output Camera", typeof(Camera));
            output.transform.SetParent(transform, false);
            m_outputCamera = output.GetComponent<Camera>();
            m_outputCamera.enabled = false;
            m_outputCamera.cullingMask = 0;
            m_outputCamera.clearFlags = CameraClearFlags.SolidColor;
            m_outputCamera.backgroundColor = Color.black;
            m_outputCamera.allowHDR = false;
            m_outputCamera.allowMSAA = false;
        }

        public void Play(Camera source, float lineInterval = 0.2f, float drawDuration = 0.1f, float spawnDiameter = 500f,
            float moveDelay = 0.5f, float moveDuration = 0.9f, float moveX = 18f, float moveY = 12f,
            float vfxStrength = 1f)
        {
            if (m_material == null || source == null || !source.isActiveAndEnabled) return;

            Stop();
            m_sourceCamera = source;
            UpdateViewport();
            GenerateCuts(Mathf.Max(0f, spawnDiameter));
            lineInterval = Mathf.Max(0f, lineInterval);
            drawDuration = Mathf.Max(0.01f, drawDuration);
            moveDuration = Mathf.Max(0.1f, moveDuration);
            moveDelay = Mathf.Max(0f, moveDelay);
            vfxStrength = Mathf.Clamp(vfxStrength, 0f, 2f);
            float lastCutEnd = (LineCount - 1) * lineInterval + drawDuration;
            m_motionStart = lastCutEnd + moveDelay;
            m_duration = m_motionStart + moveDuration;
            m_elapsed = 0f;
            m_movement = m_flash = m_charge = m_impact = m_coverOpacity = m_coverSpread = 0f;
            m_lineOpacity = 1f;
            m_screenOffset = Vector2.zero;
            System.Array.Clear(m_cutProgress, 0, LineCount);
            System.Array.Clear(m_cutEnergy, 0, LineCount);
            m_material.SetFloat(s_lineIntervalId, lineInterval);
            m_material.SetFloat(s_drawDurationId, drawDuration);
            m_material.SetVector(s_motionTimesId, new Vector4(m_motionStart, moveDuration, 0f, 0f));
            m_material.SetVector(s_movePixelsId, new Vector4(Mathf.Abs(moveX), Mathf.Abs(moveY), 0f, 0f));
            m_material.SetFloat(s_vfxStrengthId, vfxStrength);
            m_coverMaterial.SetFloat(s_vfxStrengthId, vfxStrength);
            ApplyFrame();
            m_canvas.enabled = true;
            m_coverCanvas.enabled = true;
            m_playing = true;
            BuildTimeline(lineInterval, drawDuration, lastCutEnd, moveDelay, moveDuration, vfxStrength);
        }

        private void BuildTimeline(float interval, float drawDuration, float lastCutEnd,
            float delay, float moveDuration, float strength)
        {
            Sequence sequence = DOTween.Sequence();
            sequence.Pause();
            sequence.SetAutoKill(true).SetUpdate(true).SetTarget(this).SetLink(gameObject, LinkBehaviour.KillOnDisable);
            sequence.OnUpdate(ApplyFrame).OnKill(OnTimelineEnded);
            m_timeline = sequence;

            sequence.Insert(0f, DOTween.To(() => m_elapsed, value => m_elapsed = value,
                m_duration, m_duration).SetEase(Ease.Linear));
            for (int cut = 0; cut < LineCount; cut++)
            {
                int index = cut;
                float start = cut * interval;
                sequence.Insert(start, DOTween.To(() => m_cutProgress[index], value => m_cutProgress[index] = value,
                    1f, drawDuration).SetEase(Ease.OutExpo));
                float energyDuration = Mathf.Min(drawDuration + 0.24f, m_duration - start);
                sequence.Insert(start, DOTween.To(() => 1f, value => m_cutEnergy[index] = value,
                    0f, energyDuration).SetEase(Ease.OutCubic));
            }
            if (delay > 0f)
                sequence.Insert(lastCutEnd, DOTween.To(() => 0f, value => m_charge = value,
                    1f, delay).SetEase(Ease.InCubic));

            sequence.InsertCallback(m_motionStart, BeginMotion);
            sequence.Insert(m_motionStart, DOTween.To(() => 0f, value => m_movement = value,
                1f, moveDuration * 0.18f).SetEase(Ease.OutBack, 1.1f));
            sequence.Insert(m_motionStart, DOTween.To(() => 1f, value => m_charge = value,
                0f, Mathf.Min(0.12f, moveDuration * 0.2f)).SetEase(Ease.OutQuad));
            sequence.Insert(m_motionStart, DOTween.To(() => 1f, value => m_impact = value,
                0f, moveDuration * 0.48f).SetEase(Ease.OutCubic));

            float flashPeak = Mathf.Min(0.75f, 0.45f * strength);
            float flashAttack = Mathf.Min(0.025f, moveDuration * 0.04f);
            sequence.Insert(m_motionStart, DOTween.To(() => 0f, value => m_flash = value,
                flashPeak, flashAttack).SetEase(Ease.OutQuad));
            sequence.Insert(m_motionStart + flashAttack, DOTween.To(() => flashPeak, value => m_flash = value,
                0f, Mathf.Min(0.16f, moveDuration * 0.18f)).SetEase(Ease.OutCubic));
            // Split first, cover completely, restore while hidden, then reveal.
            sequence.InsertCallback(m_motionStart + moveDuration * 0.28f, () => m_coverOpacity = 1f);
            sequence.Insert(m_motionStart + moveDuration * 0.28f,
                DOTween.To(() => 0f, value => m_coverSpread = value, 1f, moveDuration * 0.22f).SetEase(Ease.InOutSine));
            sequence.Insert(m_motionStart + moveDuration * 0.34f,
                DOTween.To(() => 1f, value => m_lineOpacity = value, 0f, moveDuration * 0.16f).SetEase(Ease.InQuad));
            sequence.InsertCallback(m_motionStart + moveDuration * 0.56f, RestoreBehindLight);
            sequence.Insert(m_motionStart + moveDuration * 0.70f,
                DOTween.To(() => 1f, value => m_coverOpacity = value, 0f, moveDuration * 0.30f).SetEase(Ease.InOutSine));
            AddImpactShake(sequence, moveDuration, strength);
            sequence.Play();
        }

        private void RestoreBehindLight()
        {
            m_coverOpacity = m_coverSpread = 1f;
            m_movement = m_impact = m_charge = m_flash = m_lineOpacity = 0f;
            m_screenOffset = Vector2.zero;
        }

        private void AddImpactShake(Sequence sequence, float moveDuration, float strength)
        {
            // Shake only the sampled image, and use the effect's own RNG rather than gameplay Random.
            const int steps = 8;
            float stepDuration = Mathf.Min(0.24f, moveDuration * 0.4f) / steps;
            Vector2 previous = Vector2.zero;
            for (int i = 0; i < steps; i++)
            {
                Vector2 from = previous;
                float amplitude = (1f - (i + 1f) / steps) * strength;
                Vector2 target = i == steps - 1 ? Vector2.zero : new Vector2(
                    ((float)m_random.NextDouble() * 2f - 1f) * 7f,
                    ((float)m_random.NextDouble() * 2f - 1f) * 4.5f) * amplitude;
                sequence.Insert(m_motionStart + i * stepDuration,
                    DOTween.To(() => from, value => m_screenOffset = value, target, stepDuration).SetEase(Ease.OutSine));
                previous = target;
            }
        }

        private void ApplyFrame()
        {
            if (m_material == null) return;
            m_material.SetFloat(s_elapsedId, m_elapsed);
            m_material.SetFloatArray(s_cutProgressId, m_cutProgress);
            m_material.SetFloatArray(s_cutEnergyId, m_cutEnergy);
            m_material.SetFloat(s_movementId, m_movement);
            m_material.SetFloat(s_lineOpacityId, m_lineOpacity);
            m_material.SetFloat(s_flashId, m_flash);
            m_material.SetFloat(s_chargeId, m_charge);
            m_material.SetFloat(s_impactId, m_impact);
            m_material.SetVector(s_screenOffsetId, new Vector4(m_screenOffset.x, m_screenOffset.y, 0f, 0f));
            m_coverMaterial.SetFloat(s_elapsedId, m_elapsed);
            m_coverMaterial.SetFloat(s_coverOpacityId, m_coverOpacity);
            m_coverMaterial.SetFloat(s_coverSpreadId, m_coverSpread);
        }

        private void GenerateCuts(float diameter)
        {
            // 500 x 500 범위는 지름 500픽셀의 원이다. 작은 Game 뷰에서는 화면 안으로 제한한다.
            float radius = Mathf.Min(diameter * 0.5f, Mathf.Min(m_viewportSize.x, m_viewportSize.y) * 0.5f);
            for (int i = 0; i < LineCount; i++)
            {
                float polarAngle = (float)m_random.NextDouble() * Mathf.PI * 2f;
                float distance = Mathf.Sqrt((float)m_random.NextDouble()) * radius;
                Vector2 center = new Vector2(Mathf.Cos(polarAngle), Mathf.Sin(polarAngle)) * distance;
                float angle = Mathf.Lerp(15f, 75f, (float)m_random.NextDouble());
                if (i < LineCount / 2) angle = -angle;
                float radians = angle * Mathf.Deg2Rad;
                m_cuts[i] = new Vector4(center.x, center.y, Mathf.Cos(radians), Mathf.Sin(radians));
            }

            // 양수/음수 각도를 절반씩 만든 뒤, 실제로 그리는 순서를 섞는다.
            for (int i = LineCount - 1; i > 0; i--)
            {
                int other = m_random.Next(i + 1);
                Vector4 cut = m_cuts[i];
                m_cuts[i] = m_cuts[other];
                m_cuts[other] = cut;
            }
            m_material.SetVectorArray(s_cutsId, m_cuts);
            m_coverMaterial.SetVectorArray(s_cutsId, m_cuts);
        }

        private void UpdateViewport()
        {
            if (m_capturing) m_sourceCamera.targetTexture = m_originalTarget;
            m_viewport = m_sourceCamera.rect;
            m_viewportSize = new Vector2(Mathf.Max(1, m_sourceCamera.pixelWidth), Mathf.Max(1, m_sourceCamera.pixelHeight));
            m_canvas.targetDisplay = m_sourceCamera.targetDisplay;
            m_coverCanvas.targetDisplay = m_sourceCamera.targetDisplay;
            m_coverMaterial.SetVector(s_viewportSizeId, new Vector4(Screen.width, Screen.height, 0f, 0f));
            // Map the full-screen light back into the camera's cut coordinates, including inset viewports.
            m_coverMaterial.SetVector(s_cutViewportId, new Vector4(m_viewport.center.x, m_viewport.center.y,
                m_viewportSize.x / Mathf.Max(1f, Screen.width * m_viewport.width),
                m_viewportSize.y / Mathf.Max(1f, Screen.height * m_viewport.height)));
            RectTransform rect = m_image.rectTransform;
            rect.anchorMin = m_viewport.min;
            rect.anchorMax = m_viewport.max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            m_material.SetVector(s_viewportSizeId, new Vector4(m_viewportSize.x, m_viewportSize.y, 0f, 0f));
            m_screenWidth = Screen.width;
            m_screenHeight = Screen.height;
            if (m_capturing)
            {
                EnsureFrame();
                m_sourceCamera.targetTexture = m_frame;
                ConfigureOutputCamera();
            }
        }

        private void EnsureFrame()
        {
            int width = (int)m_viewportSize.x;
            int height = (int)m_viewportSize.y;
            if (m_frame == null || m_frame.width != width || m_frame.height != height)
            {
                ReleaseFrame();
                m_frame = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32)
                {
                    name = "Screen Cut Live Scene",
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp,
                };
            }
            if (!m_frame.IsCreated()) m_frame.Create();
            m_image.texture = m_frame;
        }

        private void ConfigureOutputCamera()
        {
            m_outputCamera.depth = m_sourceCamera.depth + 1f;
            m_outputCamera.targetDisplay = m_sourceCamera.targetDisplay;
            m_outputCamera.rect = m_viewport;
        }

        private void BeginMotion()
        {
            if (!m_playing || m_sourceCamera == null || !m_sourceCamera.isActiveAndEnabled)
            {
                Stop();
                return;
            }
            // 화면 이동 구간에만 카메라 출력을 텍스처로 받는다. 장면은 계속 실시간으로 움직인다.
            m_originalTarget = m_sourceCamera.targetTexture;
            EnsureFrame();
            m_sourceCamera.targetTexture = m_frame;
            ConfigureOutputCamera();
            m_outputCamera.enabled = true;
            m_material.SetFloat(s_sceneReadyId, 1f);
            m_capturing = true;
        }

        private void Update()
        {
            if (!m_playing) return;
            if (m_sourceCamera == null || !m_sourceCamera.isActiveAndEnabled)
            {
                Stop();
                return;
            }
            if (m_screenWidth != Screen.width || m_screenHeight != Screen.height || m_sourceCamera.rect != m_viewport)
                UpdateViewport();
        }

        public void Stop()
        {
            if (m_timeline != null && m_timeline.IsActive())
            {
                Sequence sequence = m_timeline;
                m_timeline = null;
                sequence.Kill(false);
            }
            else OnTimelineEnded();
        }

        private void OnTimelineEnded()
        {
            m_timeline = null;
            if (m_capturing && m_sourceCamera != null && m_sourceCamera.targetTexture == m_frame)
                m_sourceCamera.targetTexture = m_originalTarget;
            m_originalTarget = null;
            m_capturing = false;
            m_playing = false;
            m_sourceCamera = null;
            if (m_canvas != null) m_canvas.enabled = false;
            if (m_coverCanvas != null) m_coverCanvas.enabled = false;
            if (m_outputCamera != null) m_outputCamera.enabled = false;
            if (m_image != null) m_image.texture = Texture2D.whiteTexture;
            if (m_material != null) m_material.SetFloat(s_sceneReadyId, 0f);
        }

        private void ReleaseFrame()
        {
            if (m_frame == null) return;
            m_frame.Release();
            Destroy(m_frame);
            m_frame = null;
        }

        private void OnDisable() => Stop();

        private void OnDestroy()
        {
            Stop();
            ReleaseFrame();
            if (m_material != null) Destroy(m_material);
            if (m_coverMaterial != null) Destroy(m_coverMaterial);
        }
    }
}
