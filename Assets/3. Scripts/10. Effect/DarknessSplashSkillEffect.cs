using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace SquareTD
{
    // 블랙홀(광역 어둠) 스킬 2 연출 — 어둠 구슬이 날아가 블랙홀이 열리고, 장판이 남은 동안 소용돌이치며, 턴마다 적을 빨아들이고, 끝나면 무너짐
    [DisallowMultipleComponent]
    public sealed class DarknessSplashSkillEffect : MonoBehaviour
    {
        private sealed class Hole
        {
            public Vector3 Center;
            public float Size;
            public float Area;
            public float Open;
            public float Boost;
            public float Flow;
            public float Phase;
            public float BusyUntil;
            public EffectQuad Shade;
            public EffectQuad Halo;
            public EffectQuad Core;
            public EffectQuad Rim;
            public readonly List<EffectQuad> Arcs = new List<EffectQuad>();
            public readonly List<float> Angles = new List<float>();
            public Tween Opening;
        }

        [SerializeField] private Color m_coreColor = new Color(0f, 0f, 0.01f, 1f);
        [SerializeField] private Color m_shadeColor = new Color(0.03f, 0f, 0.06f, 0.75f);
        [SerializeField] private Color m_glowColor = new Color(0.45f, 0.1f, 0.85f, 0.9f);
        [SerializeField] private Color m_edgeColor = new Color(0.85f, 0.15f, 0.7f, 1f);
        [SerializeField] private float m_coreScale = 1.2f;
        [SerializeField] private Color m_dustColor = new Color(0.08f, 0.01f, 0.14f, 0.85f);
        [SerializeField] private int m_arcCount = 6;
        [SerializeField] private float m_spinSpeed = 120f;
        [SerializeField] private float m_inflow = 0.35f;
        [SerializeField] private float m_boostSpin = 5f;
        [SerializeField] private float m_flyDuration = 0.35f;
        [SerializeField] private float m_openDuration = 0.45f;
        [SerializeField] private float m_pulseDuration = 0.5f;
        [SerializeField] private float m_collapseDuration = 0.45f;

        private readonly Dictionary<BlackHoleGround, Hole> m_holes = new Dictionary<BlackHoleGround, Hole>();
        private readonly List<Hole> m_closing = new List<Hole>();
        private readonly HashSet<GroundEffect> m_present = new HashSet<GroundEffect>();
        private readonly List<BlackHoleGround> m_gone = new List<BlackHoleGround>();
        private readonly System.Random m_random = new System.Random();
        private EffectQuadPool m_shadePool;
        private EffectQuadPool m_haloPool;
        private EffectQuadPool m_rimPool;
        private EffectQuadPool m_arcPool;
        private EffectQuadPool m_voidPool;
        private EffectQuadPool m_glowPool;
        private EffectQuadPool m_ringPool;
        private EffectQuadPool m_flarePool;
        private EffectParticles m_motes;
        private EffectParticles m_dust;
        private int m_groundLayer;
        private int m_groundOrder;

        private void Awake()
        {
            m_shadePool = new EffectQuadPool(transform, "EffectSprite", "Black Hole Shade", 0, EffectTextures.Glow);
            m_haloPool = new EffectQuadPool(transform, "EffectSprite", "Black Hole Halo", 0, EffectTextures.Glow, true);
            m_rimPool = new EffectQuadPool(transform, "EffectSprite", "Black Hole Rim", 0, EffectTextures.Ring, true);
            m_arcPool = new EffectQuadPool(transform, "EffectSprite", "Black Hole Swirl", 0, EffectTextures.SlashTrail, true, ArcMesh.Get(140f, 0.3f));
            m_voidPool = new EffectQuadPool(transform, "EffectSprite", "Black Hole Orb Core", 27, EffectTextures.Glow);
            m_glowPool = new EffectQuadPool(transform, "EffectSprite", "Black Hole Orb Glow", 26, EffectTextures.Glow, true);
            m_ringPool = new EffectQuadPool(transform, "EffectSprite", "Black Hole Ring", 27, EffectTextures.Ring, true);
            m_flarePool = new EffectQuadPool(transform, "EffectSprite", "Black Hole Flare", 28, EffectTextures.Flare, true);
            m_dust = new EffectParticles(transform, "Black Hole Dust", EffectTextures.Smoke, false, 1f, 25, false, 0f, true);
            m_motes = new EffectParticles(transform, "Black Hole Motes", EffectTextures.Spark, true, 2.2f, 26, true, 0f, false);

            ZoneGlowEffect zones = FindAnyObjectByType<ZoneGlowEffect>();
            Renderer board = zones != null ? zones.GetComponent<Renderer>() : null;
            m_groundLayer = board != null ? board.sortingLayerID : SortingLayer.NameToID("MapTile");
            m_groundOrder = (board != null ? board.sortingOrder : 0) + 4;
        }

        private void OnDestroy()
        {
            m_shadePool?.Dispose(); m_haloPool?.Dispose(); m_rimPool?.Dispose(); m_arcPool?.Dispose();
            m_voidPool?.Dispose(); m_glowPool?.Dispose(); m_ringPool?.Dispose(); m_flarePool?.Dispose();
        }

        private void Update()
        {
            Sync();
            float deltaTime = Time.deltaTime;
            foreach (Hole hole in m_holes.Values) Animate(hole, deltaTime);
            foreach (Hole hole in m_closing) Animate(hole, deltaTime);
        }

        public void Sync()
        {
            if (!m_shadePool.IsReady) return;

            GroundEffectRegistry grounds = BoardManager.Instance != null ? BoardManager.Instance.Grounds : null;
            m_present.Clear();
            if (grounds != null) foreach (GroundEffect effect in grounds.All) m_present.Add(effect);

            m_gone.Clear();
            foreach (BlackHoleGround ground in m_holes.Keys) if (!m_present.Contains(ground)) m_gone.Add(ground);
            foreach (BlackHoleGround ground in m_gone) Collapse(ground);

            foreach (GroundEffect effect in m_present)
            {
                if (effect is BlackHoleGround ground && !m_holes.ContainsKey(ground)) Open(ground);
            }
        }

        public void PlayTurn(BlackHoleGround ground, IReadOnlyList<Vector3> hitPositions)
        {
            Sync();
            if (ground == null || !m_holes.TryGetValue(ground, out Hole hole)) return;

            float cell = Mathf.Max(0.5f, EffectSpace.CellWorldSize());
            Vector3 center = hole.Center;
            hole.Boost = 1f;
            hole.BusyUntil = Time.time + m_pulseDuration;

            TexturedFx.Pop(m_ringPool, center, 0f, hole.Area * 1.3f, hole.Size * 0.4f, m_glowColor, 2f, 0f, m_pulseDuration, 0.2f, Ease.InQuad);
            m_motes.Gather(center, 24, m_edgeColor, hole.Area * 0.75f, cell * 0.1f, m_pulseDuration * 0.8f);
            m_dust.Gather(center, 8, m_dustColor, hole.Area * 0.6f, cell * 0.5f, m_pulseDuration);
            if (hitPositions != null)
            {
                foreach (Vector3 hit in hitPositions)
                {
                    TexturedFx.Pop(m_flarePool, hit, Range(0f, 90f), cell * 0.2f, cell * 0.9f, m_glowColor, 2.2f, 0f, 0.1f, 0.25f);
                    m_motes.Burst(hit, 4, m_edgeColor, cell * 2f, cell * 4f, cell * 0.06f, cell * 0.1f, 0.2f, 0.3f, center - hit, 30f);
                }
            }

            DOVirtual.DelayedCall(m_pulseDuration * 0.85f, () =>
            {
                TexturedFx.Pop(m_flarePool, center, 0f, cell * 0.2f, hole.Size * 1.6f, m_edgeColor, 2.5f, 0f, 0.1f, 0.25f);
                ScreenShake.Play(0.02f, 0.12f);
            }).SetLink(gameObject, LinkBehaviour.KillOnDisable);
        }

        private void Open(BlackHoleGround ground)
        {
            BoardManager board = BoardManager.Instance;
            if (board == null || board.Grid == null) return;

            float cell = Mathf.Max(0.5f, EffectSpace.CellWorldSize());
            Vector3 center = board.GetWorldPosition(board.Grid.GetPathCell(ground.PullCenter));
            var hole = new Hole
            {
                Center = center,
                Size = cell * (m_coreScale + 0.3f * ground.Radius),
                Area = cell * (ground.Radius * 2 + 1),
                Phase = Range(0f, 6f),
            };
            hole.Shade = Persistent(m_shadePool, center, m_shadeColor, 1f, 0, false);
            hole.Halo = Persistent(m_haloPool, center, m_glowColor, 1.6f, 1, false);
            hole.Core = Persistent(m_shadePool, center, m_coreColor, 1f, 2, false);
            for (int i = 0; i < m_arcCount; i++)
            {
                hole.Arcs.Add(Persistent(m_arcPool, center, i % 2 == 0 ? m_glowColor : m_edgeColor, 2.2f, 0, true));
                hole.Angles.Add(i * 137f);
            }
            hole.Rim = Persistent(m_rimPool, center, m_edgeColor, 2.6f, 1, true);
            m_holes[ground] = hole;

            float flight = ground.Owner != null ? m_flyDuration : 0f;
            if (flight > 0f) PlayOrb(ground.Owner.transform.position, center, cell, flight);
            EffectActivity.HoldFor(flight + m_openDuration);
            hole.BusyUntil = Time.time + flight + m_openDuration;

            hole.Opening = DOTween.To(() => hole.Open, value => hole.Open = value, 1f, m_openDuration)
                .SetDelay(flight).SetEase(Ease.OutBack).SetLink(gameObject, LinkBehaviour.KillOnDisable);
            DOVirtual.DelayedCall(flight, () =>
            {
                TexturedFx.Pop(m_ringPool, center, 0f, hole.Area * 1.4f, hole.Size * 0.3f, m_edgeColor, 2.2f, 0f, m_openDuration, 0.2f, Ease.InQuad);
                TexturedFx.Pop(m_flarePool, center, 0f, cell * 0.3f, hole.Size * 2.2f, m_edgeColor, 3f, 0f, 0.12f, 0.3f);
                m_motes.Gather(center, 30, m_edgeColor, hole.Area * 0.8f, cell * 0.12f, m_openDuration);
                ScreenShake.Play(0.03f, 0.25f);
                ImpactFx.Punch(0.02f, 0.3f);
            }).SetLink(gameObject, LinkBehaviour.KillOnDisable);
        }

        private void PlayOrb(Vector3 from, Vector3 to, float cell, float duration)
        {
            EffectQuad glow = m_glowPool.Get();
            glow.Transform.position = from;
            glow.Transform.localScale = new Vector3(cell * 0.9f, cell * 0.9f, 1f);
            glow.SetColor(m_glowColor);
            glow.SetFloat(EffectQuad.IntensityId, 2.5f);

            EffectQuad core = m_voidPool.Get();
            core.Transform.position = from;
            core.Transform.localScale = new Vector3(cell * 0.4f, cell * 0.4f, 1f);
            core.SetColor(m_coreColor);

            Sequence sequence = DOTween.Sequence().SetLink(glow.GameObject, LinkBehaviour.KillOnDisable);
            sequence.Append(glow.Transform.DOJump(to, cell * 1.5f, 1, duration).SetEase(Ease.InQuad));
            sequence.OnUpdate(() =>
            {
                core.Transform.position = glow.Transform.position;
                m_motes.Burst(glow.Transform.position, 1, m_glowColor, cell * 0.3f, cell * 0.8f, cell * 0.05f, cell * 0.09f, 0.15f, 0.25f);
            });
            sequence.OnKill(() =>
            {
                m_glowPool.Release(glow);
                m_voidPool.Release(core);
            });
        }

        private void Collapse(BlackHoleGround ground)
        {
            Hole hole = m_holes[ground];
            m_holes.Remove(ground);
            m_closing.Add(hole);
            hole.Opening?.Kill();

            float cell = Mathf.Max(0.5f, EffectSpace.CellWorldSize());
            float delay = Mathf.Max(0f, hole.BusyUntil - Time.time);
            EffectActivity.HoldFor(delay + m_collapseDuration + 0.2f);

            Sequence sequence = DOTween.Sequence().SetLink(gameObject, LinkBehaviour.KillOnDisable);
            sequence.AppendInterval(delay);
            sequence.Append(DOTween.To(() => hole.Open, value => hole.Open = value, 1.25f, m_collapseDuration * 0.35f).SetEase(Ease.OutQuad));
            sequence.Join(DOTween.To(() => hole.Boost, value => hole.Boost = value, 1f, m_collapseDuration * 0.35f));
            sequence.Append(DOTween.To(() => hole.Open, value => hole.Open = value, 0f, m_collapseDuration * 0.65f).SetEase(Ease.InBack));
            sequence.AppendCallback(() =>
            {
                TexturedFx.Pop(m_flarePool, hole.Center, 0f, cell * 0.2f, hole.Size * 2.5f, Color.white, 3f, 0f, 0.1f, 0.25f);
                TexturedFx.Pop(m_ringPool, hole.Center, 0f, hole.Size * 0.3f, hole.Area * 1.2f, m_edgeColor, 2f, 0f, 0.3f, 0.3f, Ease.OutCubic);
                m_motes.Burst(hole.Center, 20, m_edgeColor, cell * 3f, cell * 6f, cell * 0.06f, cell * 0.12f, 0.25f, 0.45f);
                ScreenShake.Play(0.03f, 0.15f);
            });
            sequence.OnKill(() =>
            {
                Release(hole);
                m_closing.Remove(hole);
            });
        }

        private void Animate(Hole hole, float deltaTime)
        {
            hole.Boost = Mathf.MoveTowards(hole.Boost, 0f, deltaTime / m_pulseDuration);
            float open = Mathf.Max(0f, hole.Open);
            float breath = 1f + 0.06f * Mathf.Sin(Time.time * 3f + hole.Phase);
            float speed = 1f + hole.Boost * m_boostSpin;

            SetSize(hole.Shade, Mathf.Max(hole.Area * 1.25f, hole.Size * 2f) * open);
            SetSize(hole.Halo, hole.Size * 2.8f * open * breath);
            SetSize(hole.Core, hole.Size * 0.9f * open * breath);
            SetSize(hole.Rim, hole.Size * open * breath);

            float outer = Mathf.Max(hole.Size * 1.1f, hole.Area * 0.55f);
            float inner = hole.Size * 0.35f;
            for (int i = 0; i < hole.Arcs.Count; i++)
            {
                float fall = Mathf.Repeat(hole.Flow + i / (float)hole.Arcs.Count, 1f);
                float radius = Mathf.Lerp(outer, inner, fall * fall);
                hole.Angles[i] += deltaTime * m_spinSpeed * speed * (outer / radius);

                EffectQuad arc = hole.Arcs[i];
                arc.Transform.rotation = Quaternion.Euler(0f, 0f, hole.Angles[i]);
                SetSize(arc, radius * open);
                arc.SetFloat(EffectQuad.AlphaId, Mathf.Sin(fall * Mathf.PI));
            }
            hole.Flow += deltaTime * m_inflow * speed;
        }

        private EffectQuad Persistent(EffectQuadPool pool, Vector3 center, Color color, float intensity, int orderOffset, bool overEnemies)
        {
            EffectQuad quad = pool.Get();
            EffectActivity.QuadOff(quad.GameObject);
            if (overEnemies) quad.SetSorting(SortingLayer.NameToID("Projectile"), 5 + orderOffset);
            else quad.SetSorting(m_groundLayer, m_groundOrder + orderOffset);
            quad.SetColor(color);
            quad.SetFloat(EffectQuad.IntensityId, intensity);
            quad.Transform.position = center;
            quad.Transform.localScale = Vector3.zero;
            return quad;
        }

        private void Release(Hole hole)
        {
            m_shadePool.Release(hole.Shade);
            m_haloPool.Release(hole.Halo);
            m_shadePool.Release(hole.Core);
            m_rimPool.Release(hole.Rim);
            foreach (EffectQuad arc in hole.Arcs) m_arcPool.Release(arc);
        }

        private static void SetSize(EffectQuad quad, float size) => quad.Transform.localScale = new Vector3(size, size, 1f);

        private float Range(float min, float max) => Mathf.Lerp(min, max, (float)m_random.NextDouble());
    }
}
