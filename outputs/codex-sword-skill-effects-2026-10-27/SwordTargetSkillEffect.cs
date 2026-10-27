using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace SquareTD
{
    // A single batched mesh keeps the 24 blades and drifting petals independent of gameplay objects.
    [DisallowMultipleComponent]
    public sealed class SwordTargetSkillEffect : MonoBehaviour
    {
        public const int SlashCount = 24;
        private const int BladeSegments = 24;
        private static readonly int s_elapsedId = Shader.PropertyToID("_Elapsed");
        private static readonly int s_opacityId = Shader.PropertyToID("_Opacity");
        private readonly System.Random m_random = new System.Random();
        private readonly Vector2[] m_slashDirections = new Vector2[SlashCount];
        private readonly float[] m_slashHalfLengths = new float[SlashCount];
        private readonly float[] m_slashBends = new float[SlashCount];
        private readonly List<Vector3> m_vertices = new List<Vector3>();
        private readonly List<Color> m_colors = new List<Color>();
        private readonly List<Vector2> m_uv = new List<Vector2>();
        private readonly List<Vector4> m_timing = new List<Vector4>();
        private readonly List<Vector4> m_motion = new List<Vector4>();
        private readonly List<Vector4> m_kind = new List<Vector4>();
        private readonly List<int> m_triangles = new List<int>();
        private Canvas m_canvas;
        private CanvasRenderer m_renderer;
        private Mesh m_mesh;
        private Material m_material;
        private Sequence m_timeline;
        private Camera m_camera;
        private Vector2 m_screenSize;
        private float m_elapsed;
        private float m_opacity;

        private void Awake()
        {
            Shader shader = Resources.Load<Shader>("PlumBlossomSword");
            if (shader == null || !shader.isSupported)
            {
                Debug.LogWarning("[SwordTargetSkillEffect] 매화검법 셰이더를 사용할 수 없습니다.", this);
                enabled = false;
                return;
            }
            m_material = new Material(shader) { name = "Plum Blossom Sword (Runtime)" };
            m_mesh = new Mesh { name = "24 Blades and Blossom Petals" };
            var canvasObject = new GameObject("Plum Blossom Sword Canvas", typeof(RectTransform), typeof(Canvas));
            canvasObject.transform.SetParent(transform, false);
            m_canvas = canvasObject.GetComponent<Canvas>();
            m_canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            m_canvas.sortingOrder = 100;
            m_canvas.additionalShaderChannels = AdditionalCanvasShaderChannels.TexCoord1
                | AdditionalCanvasShaderChannels.TexCoord2 | AdditionalCanvasShaderChannels.TexCoord3;
            m_canvas.enabled = false;
            var meshObject = new GameObject("Blades and Petals", typeof(RectTransform), typeof(CanvasRenderer));
            meshObject.transform.SetParent(canvasObject.transform, false);
            RectTransform rect = meshObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            m_renderer = meshObject.GetComponent<CanvasRenderer>();
            m_renderer.materialCount = 1;
            m_renderer.SetMaterial(m_material, 0);
            m_renderer.SetTexture(Texture2D.whiteTexture);
        }

        public void PlayAtCell(Camera source, BoardManager board, Vector3Int cell,
            float interval = 0.075f, float drawDuration = 0.045f, int petalCount = 160, float petalDuration = 2f,
            float bladeFadeDuration = 0.65f)
        {
            if (m_material == null || source == null || !source.isActiveAndEnabled || board == null || board.Grid == null)
                return;
            if (!board.Grid.TryGetPathIndex(cell, out _) && !board.Grid.TryGetSpawnIndex(cell, out _)) return;
            Vector3 worldCenter = board.GetWorldPosition(cell);
            Vector3 screenCenter = source.WorldToScreenPoint(worldCenter);
            if (screenCenter.z <= 0f) return;
            Vector2 tileSize = new Vector2(
                Vector2.Distance(screenCenter, source.WorldToScreenPoint(board.GetWorldPosition(cell + Vector3Int.right))),
                Vector2.Distance(screenCenter, source.WorldToScreenPoint(board.GetWorldPosition(cell + Vector3Int.up))));
            if (Mathf.Min(tileSize.x, tileSize.y) < 1f) return;

            Stop();
            m_camera = source;
            m_screenSize = new Vector2(Screen.width, Screen.height);
            m_canvas.targetDisplay = source.targetDisplay;
            Vector2 center = (Vector2)screenCenter - m_screenSize * 0.5f;
            interval = Mathf.Max(0.025f, interval);
            drawDuration = Mathf.Max(0.01f, drawDuration);
            petalCount = Mathf.Clamp(petalCount, 24, 320);
            petalDuration = Mathf.Max(0.4f, petalDuration);
            bladeFadeDuration = Mathf.Max(0.1f, bladeFadeDuration);
            float duration = BuildMesh(center, tileSize, interval, drawDuration, petalCount, petalDuration, bladeFadeDuration);
            m_elapsed = 0f;
            m_opacity = 1f;
            ApplyFrame();
            m_canvas.enabled = true;
            Sequence sequence = DOTween.Sequence();
            sequence.Pause();
            sequence.SetAutoKill(true).SetUpdate(true).SetTarget(this).SetLink(gameObject, LinkBehaviour.KillOnDisable);
            sequence.OnUpdate(ApplyFrame).OnKill(OnTimelineEnded);
            m_timeline = sequence;
            sequence.Insert(0f, DOTween.To(() => m_elapsed, value => m_elapsed = value,
                duration, duration).SetEase(Ease.Linear));
            float fadeDuration = Mathf.Min(0.45f, duration * 0.2f);
            sequence.Insert(duration - fadeDuration, DOTween.To(() => 1f, value => m_opacity = value,
                0f, fadeDuration).SetEase(Ease.InQuad));
            sequence.Play();
        }

        private float BuildMesh(Vector2 center, Vector2 tileSize, float interval,
            float drawDuration, int petalCount, float petalDuration, float bladeFadeDuration)
        {
            m_vertices.Clear(); m_colors.Clear(); m_uv.Clear(); m_timing.Clear();
            m_motion.Clear(); m_kind.Clear(); m_triangles.Clear();
            float tileDiameter = Mathf.Min(tileSize.x, tileSize.y);
            float halfLength = tileDiameter * 0.46f;
            float halfWidth = Mathf.Clamp(tileDiameter * 0.18f, 2f, 18f);
            const float windup = 0.16f;
            float lastSlash = windup + (SlashCount - 1) * interval;
            // Let the last blade and its echo finish fading before the global fade starts.
            float duration = lastSlash + drawDuration * 1.5f + bladeFadeDuration * 1.5f + 0.016f + 0.45f;
            for (int i = 0; i < SlashCount; i++)
            {
                bool finisher = i == SlashCount - 1;
                float angle = Range(0f, Mathf.PI * 2f);
                Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                Vector2 normal = new Vector2(-direction.y, direction.x);
                m_slashDirections[i] = direction;
                m_slashHalfLengths[i] = halfLength * (finisher ? 1f : Range(0.82f, 1f));
                m_slashBends[i] = m_slashHalfLengths[i] * Range(0.35f, 0.65f) * (m_random.Next(2) == 0 ? -1f : 1f);
                float sweepDuration = drawDuration * (finisher ? 1.5f : 1f);
                Vector4 timing = new Vector4(windup + i * interval,
                    sweepDuration, sweepDuration + bladeFadeDuration * (finisher ? 1.5f : 1f), i);
                float width = halfWidth * (finisher ? 1.5f : 1f);
                AddBlade(center, direction, m_slashHalfLengths[i], width, m_slashBends[i], timing, false);
                Vector4 echoTiming = timing;
                echoTiming.x += 0.016f;
                AddBlade(center + normal * width * 0.25f, direction, m_slashHalfLengths[i], width * 0.7f,
                    m_slashBends[i], echoTiming, true);
                AddImpact(center, tileDiameter * (finisher ? 0.32f : 0.17f),
                    new Vector4(timing.x + sweepDuration * (1f - Mathf.Pow(0.5f, 1f / 3f)), 0.07f, 0f, angle));
            }
            for (int i = 0; i < petalCount; i++)
            {
                bool fromBlade = i >= petalCount / 2;
                int slash = i % SlashCount;
                Vector2 direction = m_slashDirections[slash];
                Vector2 normal = new Vector2(-direction.y, direction.x);
                float pathPosition = Range(-1f, 1f);
                float sweepDuration = drawDuration * (slash == SlashCount - 1 ? 1.5f : 1f);
                float pathProgress = (pathPosition + 1f) * 0.5f;
                // Invert the blade's ease-out curve so petals appear after its head reaches this point.
                float emissionTime = 1f - Mathf.Pow(1f - pathProgress, 1f / 3f);
                float birth = fromBlade ? windup + slash * interval + emissionTime * sweepDuration : Range(0f, lastSlash);
                float life = petalDuration * Range(0.7f, 1f);
                Vector2 position = fromBlade ? BladePoint(center, direction, m_slashHalfLengths[slash], m_slashBends[slash], pathPosition)
                    : new Vector2(Range(-0.65f, 0.5f) * m_screenSize.x, Range(-0.45f, 0.65f) * m_screenSize.y);
                Vector4 timing = new Vector4(birth, life, Range(5f, 13f), Range(0f, 100f));
                Vector2 velocity = fromBlade ? direction * Range(65f, 145f) + normal * Range(-65f, 65f)
                    : new Vector2(Range(0.09f, 0.22f) * m_screenSize.x, Range(-90f, -30f));
                Vector4 motion = new Vector4(velocity.x, velocity.y, Range(-Mathf.PI, Mathf.PI), Range(-4f, 4f));
                AddPetal(position, timing, motion, fromBlade);
                duration = Mathf.Max(duration, birth + life);
            }
            m_mesh.Clear();
            m_mesh.SetVertices(m_vertices);
            m_mesh.SetColors(m_colors);
            m_mesh.SetUVs(0, m_uv);
            m_mesh.SetUVs(1, m_timing);
            m_mesh.SetUVs(2, m_motion);
            m_mesh.SetUVs(3, m_kind);
            m_mesh.SetTriangles(m_triangles, 0);
            // Petals move in the vertex shader, beyond their initial mesh bounds.
            m_mesh.bounds = new Bounds(Vector3.zero, new Vector3(m_screenSize.x * 4f, m_screenSize.y * 4f, 10f));
            m_renderer.SetMesh(m_mesh);
            return duration;
        }

        private static Vector2 BladePoint(Vector2 center, Vector2 direction, float halfLength, float bend, float t)
            => center + direction * (halfLength * t) + new Vector2(-direction.y, direction.x) * (bend * t * t);

        private void AddBlade(Vector2 center, Vector2 direction, float halfLength, float halfWidth, float bend,
            Vector4 timing, bool echo)
        {
            int first = m_vertices.Count;
            Color tint = echo ? new Color(1f, 0.18f, 0.52f, 0.30f) : new Color(1f, 0.38f, 0.68f, 1f);
            Vector2 normal = new Vector2(-direction.y, direction.x);
            for (int segment = 0; segment <= BladeSegments; segment++)
            {
                float u = segment / (float)BladeSegments;
                float t = u * 2f - 1f;
                Vector2 point = BladePoint(center, direction, halfLength, bend, t);
                Vector2 tangent = direction * halfLength + normal * (2f * bend * t);
                Vector2 perpendicular = new Vector2(-tangent.y, tangent.x).normalized;
                float width = halfWidth * (0.45f + Mathf.Sin(u * Mathf.PI) * 0.55f);
                for (int side = -1; side <= 1; side += 2)
                    AddVertex(point + perpendicular * (width * side), new Vector2(u, side), tint,
                        timing, Vector4.zero, new Vector4(0f, echo ? 1f : 0f, 0f, 0f));
                if (segment == 0) continue;
                int previous = first + (segment - 1) * 2;
                m_triangles.Add(previous); m_triangles.Add(previous + 2); m_triangles.Add(previous + 1);
                m_triangles.Add(previous + 1); m_triangles.Add(previous + 2); m_triangles.Add(previous + 3);
            }
        }

        private void AddImpact(Vector2 center, float size, Vector4 timing)
        {
            int first = m_vertices.Count;
            for (int corner = 0; corner < 4; corner++)
            {
                Vector2 uv = Corner(corner);
                AddVertex(center + uv * size, uv, Color.white, timing, Vector4.zero, new Vector4(2f, 0f, 0f, 0f));
            }
            AddTriangles(first);
        }

        private void AddPetal(Vector2 center, Vector4 timing, Vector4 motion, bool fromBlade)
        {
            int first = m_vertices.Count;
            Color tint = Color.Lerp(new Color(1f, 0.48f, 0.70f), new Color(1f, 0.91f, 0.96f), Range(0f, 1f));
            for (int corner = 0; corner < 4; corner++)
                AddVertex(center, Corner(corner), tint, timing, motion, new Vector4(1f, fromBlade ? 1f : 0f, 0f, 0f));
            AddTriangles(first);
        }

        private static Vector2 Corner(int index) => new Vector2(index == 1 || index == 2 ? 1f : -1f, index >= 2 ? 1f : -1f);

        private void AddVertex(Vector2 position, Vector2 uv, Color color, Vector4 timing, Vector4 motion, Vector4 kind)
        {
            m_vertices.Add(position); m_uv.Add(uv); m_colors.Add(color);
            m_timing.Add(timing); m_motion.Add(motion); m_kind.Add(kind);
        }

        private void AddTriangles(int first)
        {
            m_triangles.Add(first); m_triangles.Add(first + 1); m_triangles.Add(first + 2);
            m_triangles.Add(first + 2); m_triangles.Add(first + 3); m_triangles.Add(first);
        }

        private float Range(float min, float max) => Mathf.Lerp(min, max, (float)m_random.NextDouble());

        private void ApplyFrame()
        {
            if (m_material == null) return;
            m_material.SetFloat(s_elapsedId, m_elapsed);
            m_material.SetFloat(s_opacityId, m_opacity);
        }

        private void Update()
        {
            if (m_timeline == null) return;
            if (m_camera == null || !m_camera.isActiveAndEnabled || Screen.width != m_screenSize.x || Screen.height != m_screenSize.y)
                Stop();
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
            m_camera = null;
            if (m_canvas != null) m_canvas.enabled = false;
        }

        private void OnDisable() => Stop();

        private void OnDestroy()
        {
            Stop();
            if (m_mesh != null) Destroy(m_mesh);
            if (m_material != null) Destroy(m_material);
        }
    }
}
