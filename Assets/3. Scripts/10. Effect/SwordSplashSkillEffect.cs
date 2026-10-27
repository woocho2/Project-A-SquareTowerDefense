using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace SquareTD
{
    // 검성(광역 검) 스킬 2 천지일섬 연출 — 그림 + 셰이더 + 파티클 버전
    //   ① 기 모으기: 금빛 알갱이가 타워로 빨려 들고, 빛 덩어리와 별빛이 커짐
    //   ② 네 번 베기: 화면 끝에서 끝까지 금빛 줄기(FX_SlashLine) 네 줄이 엇갈려 그어짐
    //   ③ 갈라짐: 화면을 찍어 네 줄을 따라 조각내 벌림(SkySplit 셰이더) + 임팩트 프레임 + 잠깐 슬로우 + 확대 + 흔들림
    //   ④ 복구: 조각이 붙고 덮개가 걷히는 순간, 맵의 적마다 반달 X자(FX_SlashTrail) + 불티
    //   타워·규칙은 모름. 카메라·시작 위치·맞을 위치들만 받아서 그림
    [DisallowMultipleComponent]
    public sealed class SwordSplashSkillEffect : MonoBehaviour
    {
        private const int CutCount = 4;     // SkySplit.shader의 CUT_COUNT와 같게
        private static readonly Color s_gold = new Color(1f, 0.78f, 0.38f);
        private static readonly Color s_light = new Color(1f, 0.95f, 0.8f);
        private static readonly int s_screenSizeId = Shader.PropertyToID("_ScreenSize");
        private static readonly int s_cutsId = Shader.PropertyToID("_Cuts");
        private static readonly int s_shiftId = Shader.PropertyToID("_Shift");
        private static readonly int s_glowId = Shader.PropertyToID("_Glow");
        private static readonly int s_flashId = Shader.PropertyToID("_Flash");

        [Header("기 모으기 · 베기")]
        [SerializeField] private float m_chargeDuration = 0.35f;
        [SerializeField] private float m_cutInterval = 0.11f;
        [SerializeField] private float m_cutDrawDuration = 0.08f;
        [SerializeField] private float m_holdBeforeSplit = 0.2f;
        [SerializeField] private float m_glow = 3f;                // 1보다 크면 블룸으로 번짐
        [Header("갈라짐 · 복구")]
        [Tooltip("조각이 벌어지는 거리. 화면 짧은 변에 대한 비율")]
        [SerializeField] private float m_splitRatio = 0.035f;
        [SerializeField] private float m_openDuration = 0.12f;
        [SerializeField] private float m_holdOpen = 0.2f;
        [SerializeField] private float m_closeDuration = 0.25f;
        [SerializeField] private float m_shakePixels = 14f;
        [Tooltip("갈라질 때 화면이 위아래로 뒤집혀 보이면 켬 (그래픽 API마다 찍힌 화면의 위아래가 다를 수 있음)")]
        [SerializeField] private bool m_invertCaptureFlip;

        private readonly System.Random m_random = new System.Random();     // 게임 쪽 Random을 건드리지 않게
        private readonly Vector3[] m_cutPoints = new Vector3[CutCount];
        private readonly float[] m_cutAngles = new float[CutCount];
        private readonly Vector4[] m_cutsInTexture = new Vector4[CutCount];
        private EffectQuadPool m_glowPool;
        private EffectQuadPool m_flarePool;
        private EffectQuadPool m_linePool;
        private EffectQuadPool m_arcPool;
        private EffectParticles m_sparks;
        private EffectParticles m_motes;
        private Material m_splitMaterial;
        private Canvas m_overlay;
        private RawImage m_image;
        private RenderTexture m_frame;
        private Sequence m_split;
        private Coroutine m_capture;

        private void Awake()
        {
            m_glowPool = new EffectQuadPool(transform, "EffectSprite", "Sky Split Glow", 30, EffectTextures.Glow, true);
            m_flarePool = new EffectQuadPool(transform, "EffectSprite", "Sky Split Flare", 32, EffectTextures.Flare, true);
            m_linePool = new EffectQuadPool(transform, "EffectSprite", "Sky Split Cut", 31, EffectTextures.SlashLine, true);
            m_arcPool = new EffectQuadPool(transform, "EffectSprite", "Sky Split Hit", 31, EffectTextures.SlashTrail, true, ArcMesh.Get(110f, 0.35f));
            m_sparks = new EffectParticles(transform, "Sky Split Sparks", EffectTextures.Spark, true, 3f, 33, true, 1.2f, false);
            m_motes = new EffectParticles(transform, "Sky Split Motes", EffectTextures.Glow, true, 2.5f, 33, false, 0f, false);
            CreateOverlay();
        }

        private void OnDisable() => StopSplit();

        private void OnDestroy()
        {
            StopSplit();
            m_glowPool?.Dispose(); m_flarePool?.Dispose(); m_linePool?.Dispose(); m_arcPool?.Dispose();
            if (m_splitMaterial != null) Destroy(m_splitMaterial);
            ReleaseFrame();
        }

        // camera: 화면 크기를 잴 카메라 / origin: 타워 위치 / hitPoints: 맞을 적들의 위치 / cellWorldSize: 칸 한 변의 월드 길이
        public void Play(Camera camera, Vector3 origin, IReadOnlyList<Vector3> hitPoints, float cellWorldSize)
        {
            if (camera == null || !m_glowPool.IsReady || !m_linePool.IsReady) return;
            StopSplit();

            float cell = Mathf.Max(0.5f, cellWorldSize);
            float height = camera.orthographic ? camera.orthographicSize * 2f : 10f;
            float width = height * camera.aspect;
            Vector3 center = new Vector3(camera.transform.position.x, camera.transform.position.y, origin.z);
            float diagonal = Mathf.Sqrt(width * width + height * height);
            float cutThickness = Mathf.Min(width, height) * 0.05f;

            PlayCharge(origin, cell);

            float lastCutEnd = m_chargeDuration + (CutCount - 1) * m_cutInterval + m_cutDrawDuration;
            float splitAt = lastCutEnd + m_holdBeforeSplit;
            EffectActivity.HoldFor(splitAt + m_openDuration + m_holdOpen + m_closeDuration + 0.1f);   // 갈라짐 덮개는 판이 아니라서 시간을 걸어 둠
            for (int i = 0; i < CutCount; i++)
            {
                m_cutPoints[i] = center + new Vector3(Range(-0.22f, 0.22f) * width, Range(-0.22f, 0.22f) * height, 0f);
                m_cutAngles[i] = Range(20f, 70f) * (i % 2 == 0 ? 1f : -1f);           // 오른쪽 위 ↔ 오른쪽 아래를 번갈아
                float startAt = m_chargeDuration + i * m_cutInterval;
                // 찍힐 때까지 남았다가(그대로 찍혀 함께 갈라짐) 덮개가 씌워진 뒤 사라짐
                TexturedFx.Swipe(m_linePool, m_cutPoints[i], m_cutAngles[i], new Vector3(diagonal * 1.15f, cutThickness, 1f), s_gold, m_glow,
                    startAt, m_cutDrawDuration, splitAt - startAt - m_cutDrawDuration + 0.1f, 0.01f);
                DOVirtual.DelayedCall(startAt + m_cutDrawDuration * 0.5f, () => ScreenShake.Play(0.02f, 0.06f))
                    .SetLink(gameObject, LinkBehaviour.KillOnDisable);
            }

            var hits = hitPoints != null ? new List<Vector3>(hitPoints) : new List<Vector3>();
            DOVirtual.DelayedCall(splitAt, () => m_capture = StartCoroutine(CaptureAndSplit(camera, center, width, height, hits, cell)))
                .SetLink(gameObject, LinkBehaviour.KillOnDisable);
        }

        // 금빛 알갱이가 타워로 빨려 들며 빛 덩어리와 별빛이 커졌다가 첫 베기와 함께 터짐
        private void PlayCharge(Vector3 origin, float cell)
        {
            m_motes.Gather(origin, 26, s_gold, cell * 2.2f, cell * 0.12f, m_chargeDuration);
            TexturedFx.Pop(m_glowPool, origin, 0f, 0f, cell * 2.6f, s_gold, 1.8f, 0f, m_chargeDuration, 0.15f, Ease.InQuad);
            TexturedFx.Pop(m_flarePool, origin, 0f, 0f, cell * 2f, s_light, m_glow, 0f, m_chargeDuration, 0.15f, Ease.OutBack);
        }

        // 프레임이 다 그려진 뒤 화면을 찍고, 덮개를 씌워 조각을 벌렸다가 붙임
        private IEnumerator CaptureAndSplit(Camera camera, Vector3 center, float width, float height, List<Vector3> hits, float cell)
        {
            yield return new WaitForEndOfFrame();
            m_capture = null;
            if (m_splitMaterial == null || camera == null) yield break;

            int screenWidth = Screen.width;
            int screenHeight = Screen.height;
            EnsureFrame(screenWidth, screenHeight);
            ScreenCapture.CaptureScreenshotIntoRenderTexture(m_frame);

            bool flip = SystemInfo.graphicsUVStartsAtTop != m_invertCaptureFlip;
            m_image.uvRect = flip ? new Rect(0f, 1f, 1f, -1f) : new Rect(0f, 0f, 1f, 1f);
            for (int i = 0; i < CutCount; i++)
            {
                Vector3 screen = camera.WorldToScreenPoint(m_cutPoints[i]);
                float radians = m_cutAngles[i] * Mathf.Deg2Rad;
                Vector2 direction = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
                // 셰이더는 "텍스처 픽셀" 좌표로 계산. 찍힌 화면이 뒤집혀 있으면 y를 뒤집어 넘김
                m_cutsInTexture[i] = flip
                    ? new Vector4(screen.x, screenHeight - screen.y, direction.x, -direction.y)
                    : new Vector4(screen.x, screen.y, direction.x, direction.y);
            }
            m_splitMaterial.SetVector(s_screenSizeId, new Vector4(screenWidth, screenHeight, 0f, 0f));
            m_splitMaterial.SetVectorArray(s_cutsId, m_cutsInTexture);
            m_splitMaterial.SetFloat(s_shiftId, 0f);
            m_splitMaterial.SetFloat(s_glowId, 0f);
            m_splitMaterial.SetFloat(s_flashId, 0f);
            m_image.texture = m_frame;
            m_image.rectTransform.anchoredPosition = Vector2.zero;
            m_overlay.enabled = true;

            // 갈라지는 순간의 무게: 임팩트 프레임 + 잠깐 슬로우 + 확대
            ImpactFx.Frame(new Color(1f, 0.97f, 0.88f, 0.85f), 0.05f);
            ImpactFx.Slow(0.25f, 0.12f);
            ImpactFx.Punch(0.03f, 0.35f);

            float shift = Mathf.Min(screenWidth, screenHeight) * m_splitRatio;
            float current = 0f, glow = 0f, flash = 0f;
            Sequence sequence = DOTween.Sequence().SetLink(gameObject, LinkBehaviour.KillOnDisable);
            m_split = sequence;
            sequence.Append(DOTween.To(() => current, v => { current = v; m_splitMaterial.SetFloat(s_shiftId, v); },
                shift, m_openDuration).SetEase(Ease.OutBack));
            sequence.Join(DOTween.To(() => glow, v => { glow = v; m_splitMaterial.SetFloat(s_glowId, v); }, 1f, m_openDuration));
            sequence.Join(DOTween.To(() => flash, v => { flash = v; m_splitMaterial.SetFloat(s_flashId, v); }, 0.55f, 0.05f));
            sequence.Insert(0.05f, DOTween.To(() => flash, v => { flash = v; m_splitMaterial.SetFloat(s_flashId, v); }, 0f, 0.3f)
                .SetEase(Ease.InQuad));
            RectTransform shaken = m_image.rectTransform;
            sequence.Insert(0f, DOTween.Shake(() => (Vector3)shaken.anchoredPosition, value => shaken.anchoredPosition = value,
                0.35f, new Vector3(m_shakePixels, m_shakePixels * 0.7f, 0f), 25, 90f, true));
            sequence.AppendInterval(m_holdOpen);
            sequence.Append(DOTween.To(() => current, v => { current = v; m_splitMaterial.SetFloat(s_shiftId, v); },
                0f, m_closeDuration).SetEase(Ease.InCubic));
            sequence.Join(DOTween.To(() => glow, v => { glow = v; m_splitMaterial.SetFloat(s_glowId, v); }, 0f, m_closeDuration));
            sequence.OnComplete(() =>
            {
                HideOverlay();
                TexturedFx.Pop(m_glowPool, center, 0f, width, width * 1.4f, s_light, 0.6f, 0f, 0.05f, 0.35f);
                PlayHits(hits, cell);
            });
            sequence.OnKill(HideOverlay);
        }

        // 덮개가 걷히는 순간 맵의 적마다 반달 X자 + 불티
        private void PlayHits(List<Vector3> hits, float cell)
        {
            float radius = cell * 0.55f;
            foreach (Vector3 point in hits)
            {
                float delay = Range(0f, 0.06f);
                float first = 45f + Range(-15f, 15f);
                PlayArc(point, first, radius, false, delay);
                PlayArc(point, first + 90f, radius, true, delay + 0.04f);
                Vector3 at = point;
                DOVirtual.DelayedCall(delay + 0.06f, () => m_sparks.Burst(at, 8, s_gold, cell * 2f, cell * 6f, cell * 0.08f, cell * 0.16f, 0.2f, 0.4f))
                    .SetLink(gameObject, LinkBehaviour.KillOnDisable);
            }
            if (hits.Count > 0) ScreenShake.Play(0.05f, 0.12f);
        }

        private void PlayArc(Vector3 center, float angle, float radius, bool mirror, float delay)
        {
            float radians = angle * Mathf.Deg2Rad;
            Vector3 pivot = center - new Vector3(Mathf.Cos(radians), Mathf.Sin(radians), 0f) * radius;
            TexturedFx.Swipe(m_arcPool, pivot, angle, new Vector3(radius, mirror ? -radius : radius, 1f), s_gold, m_glow,
                delay, 0.07f, 0.04f, 0.16f);
        }

        private void CreateOverlay()
        {
            Shader shader = Resources.Load<Shader>("SkySplit");
            if (shader == null || !shader.isSupported)
            {
                Debug.LogWarning("[SwordSplashSkillEffect] SkySplit 셰이더를 쓸 수 없음 (화면 갈라짐 없이 진행)", this);
                return;
            }
            m_splitMaterial = new Material(shader) { name = "Sky Split (Runtime)" };

            var canvasObject = new GameObject("Sky Split Overlay", typeof(RectTransform), typeof(Canvas));
            canvasObject.transform.SetParent(transform, false);
            m_overlay = canvasObject.GetComponent<Canvas>();
            m_overlay.renderMode = RenderMode.ScreenSpaceOverlay;
            m_overlay.sortingOrder = short.MaxValue - 2;                       // UI까지 덮음 (임팩트 프레임은 그 위)
            m_overlay.enabled = false;

            var imageObject = new GameObject("Captured Screen", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
            imageObject.transform.SetParent(canvasObject.transform, false);
            m_image = imageObject.GetComponent<RawImage>();
            m_image.material = m_splitMaterial;
            m_image.raycastTarget = false;
            m_image.maskable = false;
            RectTransform rect = m_image.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private void EnsureFrame(int width, int height)
        {
            if (m_frame != null && m_frame.width == width && m_frame.height == height) return;
            ReleaseFrame();
            m_frame = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32) { name = "Sky Split Capture" };
            m_frame.Create();
        }

        private void ReleaseFrame()
        {
            if (m_frame == null) return;
            if (m_image != null && m_image.texture == m_frame) m_image.texture = null;
            m_frame.Release();
            Destroy(m_frame);
            m_frame = null;
        }

        private void StopSplit()
        {
            if (m_capture != null)
            {
                StopCoroutine(m_capture);
                m_capture = null;
            }
            if (m_split != null && m_split.IsActive()) m_split.Kill();
            m_split = null;
            HideOverlay();
        }

        private void HideOverlay()
        {
            if (m_overlay != null) m_overlay.enabled = false;
            if (m_image != null) m_image.rectTransform.anchoredPosition = Vector2.zero;
        }

        private float Range(float min, float max) => Mathf.Lerp(min, max, (float)m_random.NextDouble());
    }
}
