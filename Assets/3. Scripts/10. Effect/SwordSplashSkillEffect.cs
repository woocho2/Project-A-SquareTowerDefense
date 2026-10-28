using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace SquareTD
{
    // 소드 마스터(광역 검) 스킬 2 보이드 슬래시 연출 — 카툰 컷인 (2026-10-28 사용자가 정한 흐름)
    //   ① 화면을 찍어 둠 (컷인이 찍히지 않게 맨 먼저)
    //   ② 컷인: 비스듬한 띠 위로 스킬을 쓴 타워의 일러스트가 미끄러져 들어옴
    //   ③ 일러스트가 빠지는 순간, 화면을 8시 → 2시 방향으로 한 줄 벰 (빛 없이 까만 자국)
    //   ④ 벤 자리를 따라 화면이 두 조각으로 갈라짐 (틈은 까맣게) → 빛이 틈을 따라 8시 → 2시로 달림
    //      → 벤 자리에서 빛이 넓게 퍼져 나와 화면이 하얘짐 → 원래 화면
    //   ⑤ 돌아오는 순간 맵의 적마다 큰 베기와 같은 각도의 하얀 일자 참격이 번쩍. 벤 방향(8시 → 2시)을 따라 차례로
    //   (같은 날 칼집이 닫히는 장면과 조각이 작아지는 장면은 사용자 평으로 뺌)
    //   타워·규칙은 모름. 카메라·시작 위치·맞을 위치들·일러스트만 받아서 그림. 덮개는 판이 아니라서 EffectActivity.HoldFor로 시간을 걸어 둠
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

        [Header("컷인")]
        [SerializeField] private float m_cutInDuration = 0.18f;
        [SerializeField] private float m_cutInHold = 0.5f;
        [SerializeField] private float m_cutOutDuration = 0.12f;
        [Header("베기 · 갈라짐 · 빛")]
        [Tooltip("베는 선의 각도. 30 = 8시 → 2시")]
        [SerializeField] private float m_slashAngle = 30f;
        [SerializeField] private float m_slashDuration = 0.12f;
        [Tooltip("두 조각이 벌어지는 거리. 화면 높이에 대한 비율")]
        [SerializeField] private float m_splitRatio = 0.06f;
        [SerializeField] private float m_splitDuration = 0.14f;
        [SerializeField] private float m_splitHold = 0.12f;         // 갈라진 채로(틈은 까맣게) 머무는 시간
        [SerializeField] private float m_runDuration = 0.35f;       // 빛이 벤 자국을 따라 8시 → 2시로 달리는 시간
        [SerializeField] private float m_beamDuration = 0.3f;       // 벤 자리에서 빛이 퍼지는 시간
        [SerializeField] private float m_whiteIn = 0.1f;
        [SerializeField] private float m_whiteHold = 0.15f;
        [SerializeField] private float m_whiteOut = 0.35f;
        [Header("적이 맞는 참격")]
        [SerializeField] private float m_hitLength = 1.8f;          // 칸에 대한 비율
        [SerializeField] private float m_hitSpread = 0.3f;          // 8시 쪽 적부터 2시 쪽 적까지 차례로 터지는 데 걸리는 시간
        [Tooltip("갈라질 때 화면이 위아래로 뒤집혀 보이면 켬 (그래픽 API마다 찍힌 화면의 위아래가 다를 수 있음)")]
        [SerializeField] private bool m_invertCaptureFlip;

        private readonly System.Random m_random = new System.Random();     // 게임 쪽 Random을 건드리지 않게
        private EffectQuadPool m_cutPool;
        private EffectQuadPool m_flarePool;
        private EffectQuadPool m_ringPool;
        private EffectParticles m_sparks;
        private Material m_splitMaterial;
        private RenderTexture m_frame;
        private Sequence m_sequence;
        private Coroutine m_running;

        // 덮개 (화면 위 UI)
        private Canvas m_overlay;
        private RawImage m_split;
        private RectTransform m_band;
        private Image m_illustration;
        private Image m_white;

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
            ReleaseFrame();
        }

        // camera: 화면 크기를 잴 카메라 / origin: 타워 위치 / hitPoints: 맞을 적들의 위치 / cellWorldSize: 칸 한 변의 월드 길이
        // illustration: 컷인에 띄울 일러스트 (스킬을 쓴 타워). 없으면 띠만
        public void Play(Camera camera, Vector3 origin, IReadOnlyList<Vector3> hitPoints, float cellWorldSize, Sprite illustration)
        {
            if (camera == null || m_overlay == null) return;
            StopAll();
            EffectActivity.HoldFor(TotalDuration + m_hitSpread + 0.5f);
            var hits = hitPoints != null ? new List<Vector3>(hitPoints) : new List<Vector3>();
            m_running = StartCoroutine(CaptureThenPlay(illustration, hits, Mathf.Max(0.5f, cellWorldSize)));
        }

        private float TotalDuration => m_cutInDuration + m_cutInHold + m_slashDuration + 0.04f + m_splitDuration + m_splitHold
                                       + m_runDuration + m_beamDuration + m_whiteIn + m_whiteHold + m_whiteOut;

        // 프레임이 다 그려진 뒤 화면을 찍고(컷인이 나오기 전), 그다음 연출 전체를 한 시퀀스로
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

            // ② 컷인: 띠가 펼쳐지고 일러스트가 왼쪽에서 들어옴
            s.Insert(0f, m_band.DOScaleY(1f, m_cutInDuration).SetEase(Ease.OutBack));
            if (m_illustration.enabled)
            {
                RectTransform art = m_illustration.rectTransform;
                s.Insert(0f, AnchorX(art, -width * 0.12f, m_cutInDuration).SetEase(Ease.OutCubic));
                s.Insert(m_cutInDuration, AnchorX(art, -width * 0.06f, m_cutInHold).SetEase(Ease.Linear));          // 머무는 동안 살짝 흐름
            }

            // ③ 일러스트가 빠지는 순간 한 줄 베기 (그 순간부터 찍어 둔 화면이 덮개로)
            float slashAt = m_cutInDuration + m_cutInHold;
            if (m_illustration.enabled)
                s.Insert(slashAt, AnchorX(m_illustration.rectTransform, width * 0.8f, m_cutOutDuration).SetEase(Ease.InCubic));
            s.Insert(slashAt, m_band.DOScaleY(0f, m_cutOutDuration).SetEase(Ease.InBack));
            s.InsertCallback(slashAt, () =>
            {
                m_split.enabled = true;
                ScreenShake.Play(0.03f, 0.08f);
            });
            s.Insert(slashAt, FloatTween(0f, 1f, m_slashDuration, value => m_splitMaterial.SetFloat(s_drawId, value)).SetEase(Ease.OutCubic));

            // ④ 두 조각으로 갈라짐 → 벤 자리에서 빛이 퍼져 나옴 → 하얘짐
            float splitAt = slashAt + m_slashDuration + 0.04f;
            s.Insert(splitAt, FloatTween(0f, height * m_splitRatio, m_splitDuration, value => m_splitMaterial.SetFloat(s_shiftId, value)).SetEase(Ease.OutBack));
            s.InsertCallback(splitAt, () => ScreenShake.Play(0.05f, 0.12f));
            float runAt = splitAt + m_splitDuration + m_splitHold;                    // 까만 틈을 따라 빛이 8시 → 2시로 달림
            s.Insert(runAt, FloatTween(0f, 1f, m_runDuration, value => m_splitMaterial.SetFloat(s_runId, value)).SetEase(Ease.InOutSine));
            float beamAt = runAt + m_runDuration;
            s.Insert(beamAt, FloatTween(0f, 1f, m_beamDuration, value => m_splitMaterial.SetFloat(s_beamId, value)).SetEase(Ease.InQuad));
            s.InsertCallback(beamAt, () => ScreenShake.Play(0.04f, m_beamDuration));
            float whiteAt = beamAt + m_beamDuration;
            s.Insert(whiteAt, Fade(m_white, 1f, m_whiteIn).SetEase(Ease.InQuad));
            s.InsertCallback(whiteAt + m_whiteIn, () =>
            {
                m_split.enabled = false;                    // 하얀 동안 뒤의 덮개를 치움 → 걷히면 진짜 화면
                m_band.gameObject.SetActive(false);
                m_illustration.enabled = false;
            });

            // ⑤ 원래 화면으로 돌아오며 적마다 일자 참격
            float fadeAt = whiteAt + m_whiteIn + m_whiteHold;
            s.Insert(fadeAt, Fade(m_white, 0f, m_whiteOut).SetEase(Ease.OutQuad));
            s.InsertCallback(fadeAt + m_whiteOut * 0.25f, () => PlayHits(hits, cell));
            s.OnComplete(HideOverlay);
            s.OnKill(HideOverlay);
        }

        // 적마다 큰 베기와 같은 각도의 하얀 일자 참격 + 번쩍 + 퍼지는 고리 + 날 방향 불티
        //   벤 방향(8시 → 2시)으로 앞에 있는 적일수록 늦게 → 섬광이 판을 가로지르는 느낌
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

        // 값 하나를 from → to로 (시작 전에는 건드리지 않음 — 처음 값은 ResetOverlay가 맞춰 둠)
        private static Tween FloatTween(float from, float to, float duration, TweenCallback<float> setter)
        {
            float value = from;
            return DOTween.To(() => value, v => { value = v; setter(v); }, to, duration);
        }

        // DOTween UI 모듈을 켜지 않아서 (DOAnchorPos·DOFade 없음) 같은 일을 DOTween.To로
        private static Tween AnchorX(RectTransform rect, float to, float duration) =>
            DOTween.To(() => rect.anchoredPosition.x, x => rect.anchoredPosition = new Vector2(x, rect.anchoredPosition.y), to, duration);

        private static Tween Fade(Graphic graphic, float to, float duration) =>
            DOTween.To(() => graphic.color.a, a => { Color c = graphic.color; c.a = a; graphic.color = c; }, to, duration);

        // 크기는 화면 픽셀 기준 (덮개 캔버스에 배율 조정이 없음)
        private void ResetOverlay(Sprite illustration, float width, float height)
        {
            m_splitMaterial.SetFloat(s_drawId, 0f);
            m_splitMaterial.SetFloat(s_shiftId, 0f);
            m_splitMaterial.SetFloat(s_scaleId, 1f);
            m_splitMaterial.SetFloat(s_glowId, 0f);
            m_splitMaterial.SetFloat(s_runId, 0f);
            m_splitMaterial.SetFloat(s_beamId, 0f);
            m_splitMaterial.SetFloat(s_flashId, 0f);
            m_split.enabled = false;

            m_band.gameObject.SetActive(true);
            m_band.sizeDelta = new Vector2(width * 1.6f, height * 0.42f);
            m_band.localScale = new Vector3(1f, 0f, 1f);

            m_illustration.sprite = illustration;
            m_illustration.enabled = illustration != null;
            m_illustration.rectTransform.sizeDelta = new Vector2(height * 0.62f, height * 0.62f);
            m_illustration.rectTransform.anchoredPosition = new Vector2(-width * 0.7f, 0f);

            m_white.color = new Color(1f, 1f, 1f, 0f);
        }

        // 덮개 캔버스: 아래부터 찍은 화면 → 띠 → 일러스트 → 하얀 막
        private void CreateOverlay()
        {
            Shader shader = Resources.Load<Shader>("SkySplit");
            if (shader == null || !shader.isSupported)
            {
                Debug.LogWarning("[SwordSplashSkillEffect] SkySplit 셰이더를 쓸 수 없음 (연출 없이 진행)", this);
                return;
            }
            m_splitMaterial = new Material(shader) { name = "Sky Split (Runtime)" };

            var canvasObject = new GameObject("Sky Split Overlay", typeof(RectTransform), typeof(Canvas));
            canvasObject.transform.SetParent(transform, false);
            m_overlay = canvasObject.GetComponent<Canvas>();
            m_overlay.renderMode = RenderMode.ScreenSpaceOverlay;
            m_overlay.sortingOrder = short.MaxValue - 2;                       // UI까지 덮음 (임팩트 프레임은 그 위)
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

            m_white = MakeImage(root, "White", new Color(1f, 1f, 1f, 0f));
            Stretch(m_white.rectTransform);
        }

        private static Image MakeImage(RectTransform parent, string name, Color color)
        {
            var image = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
            image.rectTransform.SetParent(parent, false);
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        // 띠의 위·아래 금빛 테두리
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
