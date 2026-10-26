using UnityEngine;
using UnityEngine.UI;

namespace SquareTD
{
    // 카메라의 실시간 출력에 일곱 선을 긋고, 나뉜 조각을 좌우로 어긋나게 표시한다.
    [DisallowMultipleComponent]
    public sealed class AreaSlashEffect : MonoBehaviour
    {
        private const int LineCount = 7;
        private const float LineDrawDuration = 0.18f;
        private const float SplitDelay = 1f;
        private static readonly int s_elapsedId = Shader.PropertyToID("_Elapsed");
        private static readonly int s_timingsId = Shader.PropertyToID("_Timings");
        private static readonly int s_slideDistanceId = Shader.PropertyToID("_SlideDistance");
        private static readonly int s_directionId = Shader.PropertyToID("_CutDirection");
        private static readonly int s_drawStartsId = Shader.PropertyToID("_DrawStarts");

        // 연출 순서를 섞을 때 게임 로직의 UnityEngine.Random 상태를 바꾸지 않는다.
        private readonly System.Random m_lineRandom = new System.Random();
        private readonly int[] m_lineOrder = new int[LineCount];
        private readonly float[] m_drawStarts = new float[LineCount];

        private Material m_material;
        private RenderTexture m_frame;
        private RenderTexture m_originalTarget;
        private Camera m_sourceCamera;
        private Camera m_outputCamera;
        private Canvas m_canvas;
        private RawImage m_image;
        private float m_elapsed;
        private float m_duration;
        private int m_screenWidth;
        private int m_screenHeight;
        private bool m_playing;

        private void Awake()
        {
            Shader shader = Resources.Load<Shader>("AreaSlash");
            if (shader == null || !shader.isSupported)
            {
                Debug.LogWarning("[AreaSlashEffect] AreaSlash 셰이더를 사용할 수 없습니다.", this);
                enabled = false;
                return;
            }

            m_material = new Material(shader) { name = "Screen Slash (Runtime)" };

            // 원래 카메라는 텍스처로 렌더링한다. 빈 출력 카메라는 화면 배경만 정리한다.
            var output = new GameObject("Screen Slash Output Camera", typeof(Camera));
            output.transform.SetParent(transform, false);
            m_outputCamera = output.GetComponent<Camera>();
            m_outputCamera.enabled = false;
            m_outputCamera.cullingMask = 0;
            m_outputCamera.clearFlags = CameraClearFlags.SolidColor;
            m_outputCamera.backgroundColor = Color.black;
            m_outputCamera.allowHDR = false;
            m_outputCamera.allowMSAA = false;

            var canvasObject = new GameObject("Screen Slash Canvas", typeof(RectTransform), typeof(Canvas));
            canvasObject.transform.SetParent(transform, false);
            m_canvas = canvasObject.GetComponent<Canvas>();
            m_canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            m_canvas.sortingOrder = short.MinValue;
            m_canvas.enabled = false;

            var imageObject = new GameObject("Split Scene", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
            imageObject.transform.SetParent(canvasObject.transform, false);
            m_image = imageObject.GetComponent<RawImage>();
            m_image.material = m_material;
            m_image.raycastTarget = false;
            m_image.maskable = false;
        }

        public void Play(Camera source, float slideDistance = 0.025f, float duration = 1.35f, float angle = -20f)
        {
            if (m_material == null || source == null || !source.isActiveAndEnabled) return;

            if (!m_playing || m_sourceCamera != source)
            {
                Stop();
                m_sourceCamera = source;
                m_originalTarget = source.targetTexture;
                RedirectCamera();
            }

            m_elapsed = 0f;
            float splitDuration = Mathf.Max(0.1f, duration);
            m_duration = LineDrawDuration * LineCount + SplitDelay + splitDuration;
            float radians = angle * Mathf.Deg2Rad;
            m_material.SetVector(s_directionId, new Vector4(Mathf.Cos(radians), Mathf.Sin(radians), 0f, 0f));
            m_material.SetFloat(s_slideDistanceId, Mathf.Clamp(slideDistance, 0f, 0.1f));
            m_material.SetVector(s_timingsId, new Vector4(LineDrawDuration, SplitDelay, splitDuration, 0f));
            ShuffleLineOrder();
            m_material.SetFloat(s_elapsedId, 0f);

            m_outputCamera.depth = source.depth + 1f;
            m_outputCamera.targetDisplay = source.targetDisplay;
            m_outputCamera.rect = source.rect;
            m_canvas.targetDisplay = source.targetDisplay;
            RectTransform rect = m_image.rectTransform;
            rect.anchorMin = source.rect.min;
            rect.anchorMax = source.rect.max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            m_outputCamera.enabled = true;
            m_canvas.enabled = true;
            m_playing = true;
        }

        private void ShuffleLineOrder()
        {
            for (int i = 0; i < LineCount; i++) m_lineOrder[i] = i;
            for (int i = LineCount - 1; i > 0; i--)
            {
                int other = m_lineRandom.Next(i + 1);
                int line = m_lineOrder[i];
                m_lineOrder[i] = m_lineOrder[other];
                m_lineOrder[other] = line;
            }

            for (int rank = 0; rank < LineCount; rank++)
                m_drawStarts[m_lineOrder[rank]] = rank * LineDrawDuration;

            m_material.SetFloatArray(s_drawStartsId, m_drawStarts);
        }

        private void RedirectCamera()
        {
            // 해상도를 바꾼 경우에도 원래 화면 크기로 텍스처를 다시 만든다.
            m_sourceCamera.targetTexture = m_originalTarget;
            int width = Mathf.Max(1, m_sourceCamera.pixelWidth);
            int height = Mathf.Max(1, m_sourceCamera.pixelHeight);
            if (m_frame == null || m_frame.width != width || m_frame.height != height)
            {
                ReleaseFrame();
                m_frame = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32)
                {
                    name = "Screen Slash Live Scene",
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp,
                };
            }

            if (!m_frame.IsCreated()) m_frame.Create();
            m_image.texture = m_frame;
            m_sourceCamera.targetTexture = m_frame;
            m_screenWidth = Screen.width;
            m_screenHeight = Screen.height;
        }

        private void Update()
        {
            if (!m_playing) return;
            if (m_sourceCamera == null || !m_sourceCamera.isActiveAndEnabled)
            {
                Stop();
                return;
            }

            if (m_screenWidth != Screen.width || m_screenHeight != Screen.height || !m_frame.IsCreated())
                RedirectCamera();

            m_elapsed += Time.unscaledDeltaTime;
            m_material.SetFloat(s_elapsedId, m_elapsed);
            if (m_elapsed >= m_duration) Stop();
        }

        public void Stop()
        {
            if (m_sourceCamera != null && m_sourceCamera.targetTexture == m_frame)
                m_sourceCamera.targetTexture = m_originalTarget;

            m_sourceCamera = null;
            m_originalTarget = null;
            m_playing = false;
            if (m_canvas != null) m_canvas.enabled = false;
            if (m_outputCamera != null) m_outputCamera.enabled = false;
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
        }
    }
}
