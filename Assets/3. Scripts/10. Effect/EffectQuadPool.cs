using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace SquareTD
{
    // 셰이더로 그리는 연출 판과 그 풀
    public sealed class EffectQuad
    {
        public static readonly int ColorId = Shader.PropertyToID("_Color");
        public static readonly int AlphaId = Shader.PropertyToID("_Alpha");
        public static readonly int OpacityId = Shader.PropertyToID("_Opacity");
        public static readonly int ShapeId = Shader.PropertyToID("_Shape");
        public static readonly int ProgressId = Shader.PropertyToID("_Progress");
        public static readonly int FadeId = Shader.PropertyToID("_Fade");
        public static readonly int CoreId = Shader.PropertyToID("_Core");
        public static readonly int WhitenId = Shader.PropertyToID("_Whiten");
        public static readonly int ClipBelowId = Shader.PropertyToID("_ClipBelow");
        public static readonly int Color2Id = Shader.PropertyToID("_Color2");
        public static readonly int PhaseId = Shader.PropertyToID("_Phase");
        public static readonly int IntensityId = Shader.PropertyToID("_Intensity");
        public static readonly int TailId = Shader.PropertyToID("_Tail");
        public static readonly int DissolveId = Shader.PropertyToID("_Dissolve");
        public static readonly int HeadFollowId = Shader.PropertyToID("_HeadFollow");

        private static Mesh s_mesh;

        public readonly GameObject GameObject;
        public readonly Transform Transform;
        private readonly MeshRenderer m_renderer;
        private readonly MaterialPropertyBlock m_block = new MaterialPropertyBlock();
        private readonly int m_baseSortingOrder;

        public EffectQuad(Transform parent, Material material, string name, int sortingOrder, Mesh mesh = null)
        {
            GameObject = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            Transform = GameObject.transform;
            Transform.SetParent(parent, false);
            GameObject.GetComponent<MeshFilter>().sharedMesh = mesh != null ? mesh : GetMesh();
            m_renderer = GameObject.GetComponent<MeshRenderer>();
            m_renderer.sharedMaterial = material;
            m_renderer.sortingLayerName = "Projectile";
            m_renderer.sortingOrder = sortingOrder;
            m_baseSortingOrder = sortingOrder;
            m_renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            m_renderer.receiveShadows = false;
            GameObject.SetActive(false);
        }

        public void ResetValues()
        {
            m_block.Clear();
            m_renderer.SetPropertyBlock(m_block);
            m_renderer.sortingOrder = m_baseSortingOrder;
        }

        public void SetSortingOffset(int offset) => m_renderer.sortingOrder = m_baseSortingOrder + offset;

        public void SetSorting(int layerId, int order)
        {
            m_renderer.sortingLayerID = layerId;
            m_renderer.sortingOrder = order;
        }

        public void SetFloat(int id, float value)
        {
            m_block.SetFloat(id, value);
            m_renderer.SetPropertyBlock(m_block);
        }

        public void SetColor(Color color)
        {
            m_block.SetColor(ColorId, color);
            m_renderer.SetPropertyBlock(m_block);
        }

        public void SetColorById(int id, Color color)
        {
            m_block.SetColor(id, color);
            m_renderer.SetPropertyBlock(m_block);
        }

        private static Mesh GetMesh()
        {
            if (s_mesh != null) return s_mesh;
            s_mesh = new Mesh { name = "Effect Quad" };
            s_mesh.SetVertices(new List<Vector3>
            {
                new Vector3(-0.5f, -0.5f, 0f), new Vector3(0.5f, -0.5f, 0f),
                new Vector3(0.5f, 0.5f, 0f), new Vector3(-0.5f, 0.5f, 0f),
            });
            s_mesh.SetUVs(0, new List<Vector2> { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f) });
            s_mesh.SetColors(new List<Color> { Color.white, Color.white, Color.white, Color.white });
            s_mesh.SetTriangles(new[] { 0, 2, 1, 0, 3, 2 }, 0);
            return s_mesh;
        }
    }

    public sealed class EffectQuadPool
    {
        private readonly Stack<EffectQuad> m_free = new Stack<EffectQuad>();
        private readonly Transform m_parent;
        private readonly string m_name;
        private readonly int m_sortingOrder;
        private readonly Mesh m_mesh;
        private Material m_material;

        public bool IsReady => m_material != null;

        public EffectQuadPool(Transform parent, string shaderName, string name, int sortingOrder,
            string texture = null, bool additive = false, Mesh mesh = null, bool proceduralTextures = false)
        {
            m_parent = parent;
            m_name = name;
            m_sortingOrder = sortingOrder;
            m_mesh = mesh;

            Shader shader = Resources.Load<Shader>(shaderName);
            if (shader == null || !shader.isSupported)
            {
                Debug.LogWarning($"[EffectQuadPool] {shaderName} 셰이더를 쓸 수 없음");
                return;
            }
            m_material = new Material(shader) { name = $"{name} (Runtime)" };
            if (texture != null)
                m_material.SetTexture("_MainTex", proceduralTextures ? EffectTextures.GetProcedural(texture) : EffectTextures.Get(texture));
            if (m_material.HasProperty("_NoiseTex"))
                m_material.SetTexture("_NoiseTex", proceduralTextures ? EffectTextures.GetProcedural(EffectTextures.Noise) : EffectTextures.Get(EffectTextures.Noise));
            if (m_material.HasProperty("_DstBlend")) m_material.SetFloat("_DstBlend", additive ? 1f : 10f);
        }

        public EffectQuad Get()
        {
            EffectQuad quad = m_free.Count > 0 ? m_free.Pop() : new EffectQuad(m_parent, m_material, m_name, m_sortingOrder, m_mesh);
            quad.ResetValues();
            quad.Transform.localScale = Vector3.one;
            quad.Transform.rotation = Quaternion.identity;
            quad.GameObject.SetActive(true);
            EffectActivity.QuadOn(quad.GameObject);
            return quad;
        }

        public void Release(EffectQuad quad)
        {
            if (quad.GameObject == null || !quad.GameObject.activeSelf) return;
            EffectActivity.QuadOff(quad.GameObject);
            quad.GameObject.SetActive(false);
            m_free.Push(quad);
        }

        public void Dispose()
        {
            if (m_material != null) Object.Destroy(m_material);
            m_material = null;
        }
    }

    public static class SlashStroke
    {
        public static Sequence Play(EffectQuadPool pool, Vector3 center, float angle, float length, float thickness,
            float core, Color color, float delay, float drawDuration, float holdDuration, float fadeDuration, float whiten = 1f)
        {
            EffectQuad quad = pool.Get();
            quad.Transform.position = center;
            quad.Transform.rotation = Quaternion.Euler(0f, 0f, angle);
            quad.Transform.localScale = new Vector3(length, thickness, 1f);
            quad.SetColor(color);
            quad.SetFloat(EffectQuad.CoreId, core);
            quad.SetFloat(EffectQuad.WhitenId, whiten);
            quad.SetFloat(EffectQuad.ProgressId, 0f);
            quad.SetFloat(EffectQuad.FadeId, 1f);

            float progress = 0f;
            float fade = 1f;
            Sequence sequence = DOTween.Sequence().SetLink(quad.GameObject, LinkBehaviour.KillOnDisable);
            sequence.AppendInterval(delay);
            sequence.Append(DOTween.To(() => progress, value => { progress = value; quad.SetFloat(EffectQuad.ProgressId, value); },
                1f, drawDuration).SetEase(Ease.OutExpo));
            sequence.AppendInterval(holdDuration);
            sequence.Append(DOTween.To(() => fade, value => { fade = value; quad.SetFloat(EffectQuad.FadeId, value); },
                0f, fadeDuration).SetEase(Ease.InQuad));
            sequence.OnComplete(() => pool.Release(quad));
            return sequence;
        }
    }
}
