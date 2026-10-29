using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace SquareTD
{
    // 소드 마스터(광역 검) 스킬 2 보이드 슬래시 연출
    [DisallowMultipleComponent]
    public sealed class SwordSplashSkillEffect : MonoBehaviour
    {
        private static readonly Color s_gold = new Color(1f, 0.82f, 0.4f);
        private static readonly Color s_light = new Color(1f, 0.96f, 0.85f);
        private static readonly Color s_band = new Color(0.07f, 0.09f, 0.24f, 0.94f);
        private static readonly int s_screenSizeId = Shader.PropertyToID("_ScreenSize");
        private static readonly int s_cutDirId = Shader.PropertyToID("_CutDir");
        private static readonly int s_drawId = Shader.PropertyToID("_Draw");
        private static readonly int s_shiftId = Shader.PropertyToID("_Shift");
        private static readonly int s_scaleId = Shader.PropertyToID("_Scale");
        private static readonly int s_glowId = Shader.PropertyToID("_Glow");
        private static readonly int s_runId = Shader.PropertyToID("_Run");
        private static readonly int s_beamId = Shader.PropertyToID("_Beam");
        private static readonly int s_flashId = Shader.PropertyToID("_Flash");
        private static readonly int s_voidOnlyId = Shader.PropertyToID("_VoidOnly");
        private static readonly int s_seepId = Shader.PropertyToID("_Seep");

        [Header("컷인")]
        [SerializeField] private float m_cutInDuration = 0.18f;
        [SerializeField] private float m_cutInHold = 0.5f;
        [SerializeField] private float m_cutOutDuration = 0.12f;
        [Header("베기 · 갈라짐 · 공허")]
        [Tooltip("베는 선의 각도. 30 = 8시 → 2시")]
        [SerializeField] private float m_slashAngle = 30f;
        [SerializeField] private float m_slashDuration = 0.12f;
        [Tooltip("두 조각이 벌어지는 거리. 화면 높이에 대한 비율")]
        [SerializeField] private float m_splitRatio = 0.06f;
        [SerializeField] private float m_splitDuration = 0.14f;
        [SerializeField] private float m_splitHold = 0.12f;
        [Tooltip("좁은 틈에서 우주가 새어 나와 화면 위로 번지는 시간")]
        [FormerlySerializedAs("m_runDuration")]
        [SerializeField] private float m_voidSpreadDuration = 0.65f;
        [Tooltip("화면을 덮은 우주를 유지하는 시간")]
        [FormerlySerializedAs("m_whiteHold")]
        [SerializeField] private float m_voidHold = 0.4f;
        [Tooltip("공허가 걷히고 실제 게임 화면으로 돌아오는 시간")]
        [FormerlySerializedAs("m_whiteOut")]
        [SerializeField] private float m_returnDuration = 0.65f;
        [Header("적이 맞는 참격")]
        [SerializeField] private float m_hitLength = 1.8f;
        [SerializeField] private float m_hitSpread = 0.3f;
        [Tooltip("갈라질 때 화면이 위아래로 뒤집혀 보이면 켬 (그래픽 API마다 찍힌 화면의 위아래가 다를 수 있음)")]
        [SerializeField] private bool m_invertCaptureFlip;

        private readonly System.Random m_random = new System.Random();
        private EffectQuadPool m_cutPool;
        private EffectQuadPool m_flarePool;
        private EffectQuadPool m_ringPool;
        private EffectParticles m_sparks;
        private Material m_splitMaterial;
        private Material m_voidMaterial;
        private RenderTexture m_frame;
        private Sequence m_sequence;
        private Coroutine m_running;

        private Canvas m_overlay;
        private RawImage m_split;
        private RectTransform m_band;
        private Image m_illustration;
        private Image m_void;

        private void Awake()
        {
            m_cutPool = new EffectQuadPool(transform, "EffectSprite", "Sky Split Cut", 31, EffectTextures.SlashLine, true);
            m_flarePool = new EffectQuadPool(transform, "EffectSprite", "Sky Split Flare", 32, EffectTextures.Flare, true);
            m_ringPool = new EffectQuadPool(transform, "EffectSprite", "Sky Split Ring", 30, EffectTextures.Ring, true);
            m_sparks = new EffectParticles(transform, "Sky Split Sparks", EffectTextures.Spark, true, 3f, 33, true, 1.2f, false);
            CreateOverlay();
        }

        private void OnDisable() => StopAll();

        private void OnDestroy()
        {
            StopAll();
            m_cutPool?.Dispose(); m_flarePool?.Dispose(); m_ringPool?.Dispose();
            if (m_splitMaterial != null) Destroy(m_splitMaterial);
            if (m_voidMaterial != null) Destroy(m_voidMaterial);
            ReleaseFrame();
        }

        public void Play(Camera camera, Vector3 origin, IReadOnlyList<Vector3> hitPoints, float cellWorldSize, Sprite illustration)
        {
            if (camera == null || m_overlay == null) return;
            StopAll();
            EffectActivity.HoldFor(TotalDuration + m_hitSpread + 0.5f);
            var hits = hitPoints != null ? new List<Vector3>(hitPoints) : new List<Vector3>();
            m_running = StartCoroutine(CaptureThenPlay(illustration, hits, Mathf.Max(0.5f, cellWorldSize)));
        }

        private float TotalDuration => m_cutInDuration + m_cutInHold + m_slashDuration + 0.04f + m_splitDuration + m_splitHold
                                       + m_voidSpreadDuration + m_voidHold + m_returnDuration;

        private IEnumerator CaptureThenPlay(Sprite illustration, List<Vector3> hits, float cell)
        {
            yield return new WaitForEndOfFrame();
            m_running = null;

            int width = Screen.width, height = Screen.height;
            EnsureFrame(width, height);
            ScreenCapture.CaptureScreenshotIntoRenderTexture(m_frame);

            bool flip = SystemInfo.graphicsUVStartsAtTop != m_invertCaptureFlip;
            float radians = m_slashAngle * Mathf.Deg2Rad;
            m_split.texture = m_frame;
            m_split.uvRect = flip ? new Rect(0f, 1f, 1f, -1f) : new Rect(0f, 0f, 1f, 1f);
            m_splitMaterial.SetVector(s_screenSizeId, new Vector4(width, height, 0f, 0f));
            m_splitMaterial.SetVector(s_cutDirId, new Vector4(Mathf.Cos(radians), flip ? -Mathf.Sin(radians) : Mathf.Sin(radians), 0f, 0f));

            BuildSequence(illustration, hits, cell, width, height);
        }

        private void BuildSequence(Sprite illustration, List<Vector3> hits, float cell, float width, float height)
        {
            ResetOverlay(illustration, width, height);
            m_overlay.enabled = true;

            Sequence s = DOTween.Sequence().SetLink(gameObject, LinkBehaviour.KillOnDisable);
            m_sequence = s;

            bool cutIn = illustration != null;
            m_band.gameObject.SetActive(cutIn);
            if (cutIn)
            {
                s.Insert(0f, m_band.DOScaleY(1f, m_cutInDuration).SetEase(Ease.OutBack));
                RectTransform art = m_illustration.rectTransform;
                s.Insert(0f, AnchorX(art, -width * 0.12f, m_cutInDuration).SetEase(Ease.OutCubic));
                s.Insert(m_cutInDuration, AnchorX(art, -width * 0.06f, m_cutInHold).SetEase(Ease.Linear));
            }

            float slashAt = cutIn ? m_cutInDuration + m_cutInHold : 0f;
            if (cutIn)
            {
                s.Insert(slashAt, AnchorX(m_illustration.rectTransform, width * 0.8f, m_cutOutDuration).SetEase(Ease.InCubic));
                s.Insert(slashAt, m_band.DOScaleY(0f, m_cutOutDuration).SetEase(Ease.InBack));
            }
            s.InsertCallback(slashAt, () =>
            {
                m_split.enabled = true;
                ScreenShake.Play(0.03f, 0.08f);
            });
            s.Insert(slashAt, FloatTween(0f, 1f, m_slashDuration, value => m_splitMaterial.SetFloat(s_drawId, value)).SetEase(Ease.OutCubic));

            float splitAt = slashAt + m_slashDuration + 0.04f;
            s.Insert(splitAt, FloatTween(0f, height * m_splitRatio, m_splitDuration, value => m_splitMaterial.SetFloat(s_shiftId, value)).SetEase(Ease.OutBack));
            s.InsertCallback(splitAt, () => ScreenShake.Play(0.05f, 0.12f));
            float voidAt = splitAt + m_splitDuration + m_splitHold;
            // Keep the captured halves at the original narrow split. Space spills over them.
            s.Insert(voidAt, FloatTween(0f, 1f, m_voidSpreadDuration,
                value => m_splitMaterial.SetFloat(s_seepId, value)).SetEase(Ease.InOutSine));
            float coveredAt = voidAt + m_voidSpreadDuration;
            s.InsertCallback(coveredAt, () =>
            {
                m_void.color = Color.white;
                m_split.enabled = false;
                m_band.gameObject.SetActive(false);
                m_illustration.enabled = false;
            });

            float returnAt = coveredAt + m_voidHold;
            s.Insert(returnAt, Fade(m_void, 0f, m_returnDuration).SetEase(Ease.Linear));
            s.InsertCallback(returnAt + m_returnDuration * 0.25f, () => PlayHits(hits, cell));
            s.OnComplete(HideOverlay);
            s.OnKill(HideOverlay);
        }

        private void PlayHits(List<Vector3> hits, float cell)
        {
            if (hits.Count == 0) return;
            float radians = m_slashAngle * Mathf.Deg2Rad;
            Vector3 dir = new Vector3(Mathf.Cos(radians), Mathf.Sin(radians), 0f);
            float min = float.MaxValue, max = float.MinValue;
            foreach (Vector3 point in hits)
            {
                float along = Vector3.Dot(point, dir);
                min = Mathf.Min(min, along);
                max = Mathf.Max(max, along);
            }

            foreach (Vector3 point in hits)
            {
                float t = max > min ? (Vector3.Dot(point, dir) - min) / (max - min) : 0f;
                float delay = t * m_hitSpread + Range(0f, 0.03f);
                float angle = m_slashAngle + Range(-8f, 8f);
                TexturedFx.Swipe(m_cutPool, point, angle, new Vector3(cell * m_hitLength, cell * 0.16f, 1f), s_light, 3.5f,
                    delay, 0.05f, 0.05f, 0.14f);
                TexturedFx.Pop(m_flarePool, point, angle, cell * 0.2f, cell * 0.8f, Color.white, 3f, delay + 0.03f, 0.05f, 0.14f);
                TexturedFx.Pop(m_ringPool, point, 0f, cell * 0.2f, cell * 1.3f, s_gold, 1.6f, delay + 0.03f, 0.22f, 0.2f, Ease.OutCubic);
                Vector3 at = point;
                DOVirtual.DelayedCall(delay + 0.04f, () =>
                        m_sparks.Burst(at, 6, s_light, cell * 2.5f, cell * 6f, cell * 0.07f, cell * 0.13f, 0.18f, 0.3f, new Vector2(dir.x, dir.y), 40f))
                    .SetLink(gameObject, LinkBehaviour.KillOnDisable);
            }
        }

        private static Tween FloatTween(float from, float to, float duration, TweenCallback<float> setter)
        {
            float value = from;
            return DOTween.To(() => value, v => { value = v; setter(v); }, to, duration);
        }

        private static Tween AnchorX(RectTransform rect, float to, float duration) =>
            DOTween.To(() => rect.anchoredPosition.x, x => rect.anchoredPosition = new Vector2(x, rect.anchoredPosition.y), to, duration);

        private static Tween Fade(Graphic graphic, float to, float duration) =>
            DOTween.To(() => graphic.color.a, a => { Color c = graphic.color; c.a = a; graphic.color = c; }, to, duration);

        private void ResetOverlay(Sprite illustration, float width, float height)
        {
            m_splitMaterial.SetFloat(s_drawId, 0f);
            m_splitMaterial.SetFloat(s_shiftId, 0f);
            m_splitMaterial.SetFloat(s_scaleId, 1f);
            m_splitMaterial.SetFloat(s_glowId, 0f);
            m_splitMaterial.SetFloat(s_runId, 0f);
            m_splitMaterial.SetFloat(s_beamId, 0f);
            m_splitMaterial.SetFloat(s_flashId, 0f);
            m_splitMaterial.SetFloat(s_seepId, 0f);
            m_split.enabled = false;

            m_band.gameObject.SetActive(true);
            m_band.sizeDelta = new Vector2(width * 1.6f, height * 0.42f);
            m_band.localScale = new Vector3(1f, 0f, 1f);

            m_illustration.sprite = illustration;
            m_illustration.enabled = illustration != null;
            m_illustration.rectTransform.sizeDelta = new Vector2(height * 0.62f, height * 0.62f);
            m_illustration.rectTransform.anchoredPosition = new Vector2(-width * 0.7f, 0f);

            m_void.color = new Color(1f, 1f, 1f, 0f);
        }

        private void CreateOverlay()
        {
            Shader shader = Resources.Load<Shader>("SkySplit");
            if (shader == null || !shader.isSupported)
            {
                Debug.LogWarning("[SwordSplashSkillEffect] SkySplit 셰이더를 쓸 수 없음 (연출 없이 진행)", this);
                return;
            }
            m_splitMaterial = new Material(shader) { name = "Sky Split (Runtime)" };
            m_voidMaterial = new Material(shader) { name = "Void Starfield (Runtime)" };
            m_voidMaterial.SetFloat(s_voidOnlyId, 1f);

            var canvasObject = new GameObject("Sky Split Overlay", typeof(RectTransform), typeof(Canvas));
            canvasObject.transform.SetParent(transform, false);
            m_overlay = canvasObject.GetComponent<Canvas>();
            m_overlay.renderMode = RenderMode.ScreenSpaceOverlay;
            m_overlay.sortingOrder = short.MaxValue - 2;
            m_overlay.enabled = false;
            RectTransform root = (RectTransform)canvasObject.transform;

            m_split = new GameObject("Captured Screen", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage)).GetComponent<RawImage>();
            m_split.rectTransform.SetParent(root, false);
            m_split.material = m_splitMaterial;
            m_split.raycastTarget = false;
            m_split.maskable = false;
            Stretch(m_split.rectTransform);

            m_band = MakeImage(root, "Cut-in Band", s_band).rectTransform;
            m_band.localRotation = Quaternion.Euler(0f, 0f, -8f);
            MakeEdge(m_band, "Band Edge Top", 1f);
            MakeEdge(m_band, "Band Edge Bottom", 0f);

            m_illustration = MakeImage(root, "Cut-in Illustration", Color.white);
            m_illustration.preserveAspect = true;

            m_void = MakeImage(root, "Starfield Void", new Color(1f, 1f, 1f, 0f));
            m_void.material = m_voidMaterial;
            Stretch(m_void.rectTransform);
        }

        private static Image MakeImage(RectTransform parent, string name, Color color)
        {
            var image = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
            image.rectTransform.SetParent(parent, false);
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static void MakeEdge(RectTransform band, string name, float anchorY)
        {
            RectTransform edge = MakeImage(band, name, s_gold).rectTransform;
            edge.anchorMin = new Vector2(0f, anchorY);
            edge.anchorMax = new Vector2(1f, anchorY);
            edge.sizeDelta = new Vector2(0f, 6f);
        }

        private static void Stretch(RectTransform rect)
        {
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
            if (m_split != null && m_split.texture == m_frame) m_split.texture = null;
            m_frame.Release();
            Destroy(m_frame);
            m_frame = null;
        }

        private void StopAll()
        {
            if (m_running != null)
            {
                StopCoroutine(m_running);
                m_running = null;
            }
            if (m_sequence != null && m_sequence.IsActive()) m_sequence.Kill();
            m_sequence = null;
            HideOverlay();
        }

        private void HideOverlay()
        {
            if (m_overlay != null) m_overlay.enabled = false;
        }

        private float Range(float min, float max) => Mathf.Lerp(min, max, (float)m_random.NextDouble());
    }
}
