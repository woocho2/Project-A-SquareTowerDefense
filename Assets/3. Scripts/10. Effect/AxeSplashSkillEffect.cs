using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace SquareTD
{
    // 휠윈드: 원화 도끼 + 투영된 스윙 평면 + 속도에 반응하는 짧은 궤적.
    [DisallowMultipleComponent]
    public sealed class AxeSplashSkillEffect : MonoBehaviour
    {
        private const string AxeTexture = "FX_WhirlwindGreatAxe";
        private const float WeaponAspect = .5f;
        private static readonly Vector2 GripUV = new Vector2(.14f, .37f);
        private static readonly Vector2 CuttingTipUV = new Vector2(.985f, .57f);
        private static readonly Color Steel = new Color(.96f, .98f, 1f);
        private static readonly Color Edge = new Color(1f, .96f, .86f);
        private static readonly Color Blood = new Color(.80f, .055f, .075f);
        private static readonly Color Hot = new Color(1f, .53f, .33f);
        private static readonly int TrailSpanId = Shader.PropertyToID("_TrailSpan");
        private static readonly int WidthId = Shader.PropertyToID("_Width");
        private static readonly int FlattenId = Shader.PropertyToID("_PlaneFlatten");
        private static readonly int PivotYId = Shader.PropertyToID("_PivotY");
        private static readonly int HemisphereId = Shader.PropertyToID("_Hemisphere");
        private static readonly int GhostId = Shader.PropertyToID("_Ghost");
        private static readonly int FrameId = Shader.PropertyToID("_Frame");
        private static readonly int Color3Id = Shader.PropertyToID("_Color3");

        // 손그림 플립북 (Resources/Effects, 4x4 · 16프레임). 없으면 절차적 궤적·타격으로 대신함
        private const string SlashAtlas = "FX_WhirlwindSlash_Atlas";
        private const string HitAtlas = "FX_WhirlwindHit_Atlas";
        private const string BurstAtlas = "FX_WhirlwindBurst_Atlas";
        private const float FlipbookLast = 15f;
        private const float SlashSpinFrames = 11f;   // 0~11: 회전 중, 12~15: 흩어짐
        private const float SlashArcRadius = .8f;     // 궤적 중심선 반지름 / 칸 반폭
        private static readonly Color BurstDeep = new Color(.32f, .06f, .05f);
        private static readonly Color BurstMid = new Color(.86f, .36f, .22f);
        private static readonly AnimationCurve SwingCurve = new AnimationCurve(
            new Keyframe(0f, 0f, 0f, 0f),
            new Keyframe(.17f, .06f, .85f, .85f),
            new Keyframe(.60f, .80f, 1.65f, 1.65f),
            new Keyframe(.80f, .96f, .45f, .45f),
            new Keyframe(1f, 1f, 0f, 0f));

        [Header("대도끼")]
        [SerializeField, Min(.1f)] private float m_axeLength = 4f;
        [SerializeField, Min(.01f)] private float m_appear = .09f;
        [SerializeField, Range(0f, 60f)] private float m_backSwing = 40f;
        [SerializeField, Min(.01f)] private float m_windUp = .28f;
        [Header("한 바퀴 — 가속 / 관성 / 제동")]
        [SerializeField, Min(.05f)] private float m_spinDuration = .58f;
        [SerializeField] private float m_startAngle = 100f;
        [SerializeField, Range(.4f, 1f)] private float m_planeFlatten = .72f;
        [SerializeField, Range(20f, 160f)] private float m_trailDegrees = 95f;
        [SerializeField, Range(12f, 60f)] private float m_ghostAngleStep = 28f;
        [SerializeField, Min(.02f)] private float m_ghostLife = .085f;
        [Header("마무리")]
        [SerializeField, Min(0f)] private float m_endHold = .07f;
        [SerializeField, Min(.02f)] private float m_finish = .22f;
        [Header("손그림 플립북")]
        [SerializeField, Min(.05f)] private float m_slashDissipate = .2f;
        [SerializeField, Min(.05f)] private float m_hitDuration = .3f;
        [SerializeField, Min(.05f)] private float m_burstDuration = .45f;
        [SerializeField, Min(.1f)] private float m_hitSize = 1.6f;
        [SerializeField, Min(.1f)] private float m_burstReach = 2.1f;
        [SerializeField, Range(-90f, 90f)] private float m_slashAngleOffset = 0f; // 궤적이 날보다 앞서 보이면 +, 뒤처지면 −

        private EffectQuadPool m_axePool, m_ghostPool, m_trailPool, m_edgePool;
        private EffectQuadPool m_backTrailPool, m_backEdgePool, m_cutPool, m_flarePool, m_ringPool;
        private EffectQuadPool m_slashFrontFx, m_slashRearFx, m_hitFx, m_burstFx;
        private readonly System.Random m_random = new System.Random();
        private EffectParticles m_sparks;
        private readonly HashSet<Tween> m_tweens = new HashSet<Tween>();

        private sealed class Hit
        {
            public Vector3 Point;
            public float Direction, Sweep;
            public bool Critical, Played;
        }

        private void Awake()
        {
            m_axePool = new EffectQuadPool(transform, "WhirlwindWeapon", "Whirlwind Painted Great Axe",
                33, AxeTexture);
            m_ghostPool = new EffectQuadPool(transform, "WhirlwindWeapon", "Whirlwind Blade Afterimage",
                30, AxeTexture, true);
            m_trailPool = new EffectQuadPool(transform, "AxeWhirl", "Whirlwind Front Wake", 29,
                EffectTextures.SlashTrail, true);
            m_edgePool = new EffectQuadPool(transform, "AxeWhirl", "Whirlwind Front Edge", 31,
                EffectTextures.SlashTrail, true);
            m_backTrailPool = new EffectQuadPool(transform, "AxeWhirl", "Whirlwind Rear Wake", 48,
                EffectTextures.SlashTrail, true);
            m_backEdgePool = new EffectQuadPool(transform, "AxeWhirl", "Whirlwind Rear Edge", 49,
                EffectTextures.SlashTrail, true);
            m_cutPool = new EffectQuadPool(transform, "EffectSprite", "Whirlwind Contact Cut", 35,
                EffectTextures.SlashLine, true);
            m_flarePool = new EffectQuadPool(transform, "EffectSprite", "Whirlwind Contact Flash", 36,
                EffectTextures.Flare, true);
            m_ringPool = new EffectQuadPool(transform, "EffectSprite", "Whirlwind Pressure Ring", 28,
                EffectTextures.Ring, true);
            m_sparks = new EffectParticles(transform, "Whirlwind Sparks", EffectTextures.Spark,
                true, 1.6f, 36, true, .4f, false);

            if (EffectTextures.Has(SlashAtlas))
            {
                m_slashFrontFx = FlipbookPool("Whirlwind Painted Slash Front", 32, SlashAtlas);
                m_slashRearFx = FlipbookPool("Whirlwind Painted Slash Rear", 48, SlashAtlas);
            }
            if (EffectTextures.Has(HitAtlas)) m_hitFx = FlipbookPool("Whirlwind Painted Hit", 35, HitAtlas);
            if (EffectTextures.Has(BurstAtlas)) m_burstFx = FlipbookPool("Whirlwind Painted Burst", 47, BurstAtlas);
        }

        private EffectQuadPool FlipbookPool(string name, int order, string texture)
        {
            var pool = new EffectQuadPool(transform, "FlipbookFx", name, order, texture);
            return pool.IsReady ? pool : null;
        }

        private bool PaintedSlash => m_slashFrontFx != null && m_slashRearFx != null;

        private void Track(Tween tween, Action release)
        {
            m_tweens.Add(tween);
            tween.OnKill(() => { release?.Invoke(); m_tweens.Remove(tween); });
        }

        private void OnDisable()
        {
            foreach (Tween tween in new List<Tween>(m_tweens)) tween.Kill();
            m_tweens.Clear();
        }

        private void OnDestroy()
        {
            m_axePool?.Dispose(); m_ghostPool?.Dispose(); m_trailPool?.Dispose(); m_edgePool?.Dispose();
            m_backTrailPool?.Dispose(); m_backEdgePool?.Dispose();
            m_cutPool?.Dispose(); m_flarePool?.Dispose(); m_ringPool?.Dispose();
            m_slashFrontFx?.Dispose(); m_slashRearFx?.Dispose(); m_hitFx?.Dispose(); m_burstFx?.Dispose();
        }

        public void Play(Camera camera, Vector3 origin, IReadOnlyList<Vector3> hitPoints, int range, float cellWorldSize)
        {
            if (m_axePool == null || !m_axePool.IsReady || !m_trailPool.IsReady
                || !m_backTrailPool.IsReady || !m_edgePool.IsReady || !m_backEdgePool.IsReady) return;
            float cell = Mathf.Max(.01f, cellWorldSize);
            float length = cell * Mathf.Max(.1f, m_axeLength);
            float flatten = Mathf.Clamp(m_planeFlatten, .4f, 1f);
            float windUp = Mathf.Max(.01f, Mathf.Max(m_appear, m_windUp));
            float spinDuration = Mathf.Max(.05f, m_spinDuration);
            float spinEnd = windUp + spinDuration;
            float dissolveAt = spinEnd + Mathf.Max(0f, m_endHold);
            float finish = Mathf.Max(.02f, m_finish);
            bool painted = PaintedSlash;
            float slashDissipate = Mathf.Max(.05f, m_slashDissipate);
            float total = Mathf.Max(dissolveAt + finish, painted ? spinEnd + slashDissipate : 0f);
            float firstAngle = m_startAngle - m_backSwing;
            Vector2 tipOffset = new Vector2(CuttingTipUV.x - GripUV.x,
                (CuttingTipUV.y - GripUV.y) * WeaponAspect);
            float bladeOffset = Mathf.Atan2(tipOffset.y, tipOffset.x) * Mathf.Rad2Deg;
            float radius = tipOffset.magnitude * length;
            var hits = new List<Hit>();
            if (hitPoints != null)
            {
                foreach (Vector3 point in hitPoints)
                {
                    Vector3 delta = point - origin;
                    // 화면에 투영되기 전 각도로 접촉 시점을 계산한다.
                    float angle = Mathf.Atan2(delta.y / flatten, delta.x) * Mathf.Rad2Deg;
                    hits.Add(new Hit {
                        Point = point, Direction = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg,
                        Sweep = Mathf.Repeat(angle - firstAngle - bladeOffset, 360f) / 360f,
                        Critical = Mathf.Max(Mathf.Abs(delta.x), Mathf.Abs(delta.y)) <= (range + .5f) * cell
                    });
                }
            }

            EffectQuad axe = PrepareWeapon(m_axePool, origin, flatten, Steel, Edge, false);
            EffectQuad trail = null, edge = null, rear = null, rearEdge = null;
            EffectQuad slashFront = null, slashRear = null;
            EffectQuad[] wakes;
            if (painted)
            {
                float diameter = radius * 2f / SlashArcRadius;
                slashFront = PrepareFlipbook(m_slashFrontFx, origin, firstAngle + bladeOffset,
                    new Vector3(diameter, diameter, 1f), flatten, origin.y, -1f, Blood, Hot, Edge);
                slashRear = PrepareFlipbook(m_slashRearFx, origin, firstAngle + bladeOffset,
                    new Vector3(diameter, diameter, 1f), flatten, origin.y, 1f, Blood, Hot, Edge);
                slashRear.SetSorting(SortingLayer.NameToID("MapTile"), 48);
                wakes = new EffectQuad[0];
            }
            else
            {
                trail = PrepareTrail(m_trailPool, origin, firstAngle + bladeOffset, radius,
                    flatten, Blood, Hot, .068f, false);
                edge = PrepareTrail(m_edgePool, origin, firstAngle + bladeOffset, radius,
                    flatten, Hot, Edge, .012f, false);
                rear = PrepareTrail(m_backTrailPool, origin, firstAngle + bladeOffset, radius,
                    flatten, Blood, Hot, .068f, true);
                rearEdge = PrepareTrail(m_backEdgePool, origin, firstAngle + bladeOffset, radius,
                    flatten, Hot, Edge, .012f, true);
                wakes = new[] { trail, edge, rear, rearEdge };
            }
            Place(axe, origin, m_startAngle, length, 0f);
            float clock = 0f, previousTime = 0f, previousProgress = 0f;
            float ghostStep = Mathf.Clamp(m_ghostAngleStep, 12f, 60f) / 360f;
            float nextGhost = ghostStep;
            bool finished = false, contactPulse = false;
            EffectActivity.HoldFor(total + .15f);

            Tween motion = DOTween.To(() => clock, time => {
                clock = time;
                bool spinning = time >= windUp;
                float progress = SpinProgress((time - windUp) / spinDuration);
                float angle = spinning ? firstAngle + 360f * progress
                    : Mathf.Lerp(m_startAngle, firstAngle, Mathf.Pow(Mathf.Clamp01(time / windUp), 2.5f));
                float appear = Mathf.SmoothStep(0f, 1f, time / Mathf.Max(.01f, m_appear));
                float dissolve = Mathf.Clamp01((time - dissolveAt) / finish);
                float fade = 1f - Mathf.SmoothStep(0f, 1f, dissolve);
                float speed = Mathf.Max(0f, (progress - previousProgress) / Mathf.Max(.001f, time - previousTime));
                float wakeFade = spinning ? 1f - Mathf.Clamp01((time - spinEnd) / .10f) : 0f;
                float wakeStrength = Mathf.Clamp01(speed * .42f) * wakeFade;
                float span = Mathf.Clamp(speed * .085f, .025f, Mathf.Clamp(m_trailDegrees, 20f, 160f) / 360f);
                Place(axe, origin, angle, length * (.94f + .06f * appear), appear * fade);
                SetWeaponDepth(axe, angle + bladeOffset, 33);
                axe.SetFloat(EffectQuad.DissolveId, dissolve);
                axe.SetFloat(EffectQuad.PhaseId, spinning ? progress : -.2f + .3f * appear);
                axe.SetFloat(EffectQuad.WhitenId, .13f * Mathf.Clamp01(speed * .22f));
                for (int i = 0; i < wakes.Length; i++)
                {
                    EffectQuad wake = wakes[i];
                    wake.SetFloat(EffectQuad.ProgressId, progress);
                    wake.SetFloat(TrailSpanId, span);
                    wake.SetFloat(EffectQuad.AlphaId, wakeStrength * (i % 2 == 0 ? .44f : .64f));
                }
                if (painted)
                {
                    // 회전 중엔 날을 따라 돌며 0~11프레임, 멈춘 뒤 12~15프레임으로 흩어짐
                    float after = Mathf.Clamp01((time - spinEnd) / slashDissipate);
                    float frame = time <= spinEnd ? progress * SlashSpinFrames
                        : Mathf.Lerp(SlashSpinFrames, FlipbookLast, after);
                    float slashAlpha = spinning ? 1f - Mathf.SmoothStep(.8f, 1f, after) : 0f;
                    Quaternion blade = Quaternion.Euler(0f, 0f, angle + bladeOffset - m_slashAngleOffset);
                    slashFront.Transform.rotation = blade; slashRear.Transform.rotation = blade;
                    slashFront.SetFloat(FrameId, frame); slashRear.SetFloat(FrameId, frame);
                    slashFront.SetFloat(EffectQuad.AlphaId, slashAlpha); slashRear.SetFloat(EffectQuad.AlphaId, slashAlpha);
                }

                // 각도 간격의 날 잔상은 고속 구간에만 남겨 손잡이가 겹쳐 보이지 않게 한다.
                while (nextGhost <= progress)
                {
                    if (!painted && speed > .9f && time <= spinEnd)
                        Ghost(origin, firstAngle + 360f * nextGhost, length, flatten, bladeOffset);
                    nextGhost += ghostStep;
                }
                if (spinning)
                {
                    foreach (Hit hit in hits)
                    {
                        if (hit.Played || hit.Sweep > progress) continue;
                        hit.Played = true;
                        PlayHit(hit, cell);
                        if (!contactPulse && hit.Critical)
                        {
                            contactPulse = true;
                            ScreenShake.Play(.022f, .07f);
                        }
                    }
                }
                if (!finished && time >= spinEnd)
                {
                    finished = true;
                    Finish(origin, angle, length, flatten, cell);
                }
                previousTime = time;
                previousProgress = progress;
            }, total, total).SetEase(Ease.Linear).SetLink(gameObject, LinkBehaviour.KillOnDisable);
            Track(motion, () => {
                m_axePool.Release(axe);
                if (painted)
                {
                    m_slashFrontFx.Release(slashFront); m_slashRearFx.Release(slashRear);
                    return;
                }
                m_trailPool.Release(trail); m_edgePool.Release(edge);
                m_backTrailPool.Release(rear); m_backEdgePool.Release(rearEdge);
            });
        }

        private static float SpinProgress(float time) => Mathf.Clamp01(SwingCurve.Evaluate(Mathf.Clamp01(time)));

        private static EffectQuad PrepareWeapon(EffectQuadPool pool, Vector3 origin, float flatten,
            Color body, Color edge, bool ghost)
        {
            EffectQuad quad = pool.Get();
            quad.SetColor(body); quad.SetColorById(EffectQuad.Color2Id, edge);
            quad.SetFloat(EffectQuad.AlphaId, 0f);
            quad.SetFloat(FlattenId, flatten); quad.SetFloat(PivotYId, origin.y);
            quad.SetFloat(GhostId, ghost ? 1f : 0f);
            quad.SetFloat(EffectQuad.DissolveId, 0f);
            return quad;
        }

        private EffectQuad PrepareTrail(EffectQuadPool pool, Vector3 origin, float angle, float radius,
            float flatten, Color color, Color edge, float width, bool rear)
        {
            EffectQuad quad = pool.Get();
            quad.Transform.SetPositionAndRotation(origin, Quaternion.Euler(0f, 0f, angle));
            float diameter = radius * 2f / .83f;
            quad.Transform.localScale = new Vector3(diameter, diameter, 1f);
            quad.SetFloat(EffectQuad.ShapeId, 0f);
            quad.SetFloat(TrailSpanId, m_trailDegrees / 360f);
            quad.SetFloat(WidthId, width); quad.SetFloat(EffectQuad.AlphaId, 0f);
            quad.SetFloat(EffectQuad.ProgressId, 0f);
            quad.SetFloat(FlattenId, flatten); quad.SetFloat(PivotYId, origin.y);
            quad.SetFloat(HemisphereId, rear ? 1f : -1f);
            quad.SetColor(color); quad.SetColorById(EffectQuad.Color2Id, edge);
            if (rear) quad.SetSorting(SortingLayer.NameToID("MapTile"), width < .02f ? 49 : 48);
            return quad;
        }

        private static void Place(EffectQuad quad, Vector3 origin, float angle, float length, float alpha)
        {
            Quaternion rotation = Quaternion.Euler(0f, 0f, angle);
            Vector3 gripOffset = new Vector3((GripUV.x - .5f) * length,
                (GripUV.y - .5f) * length * WeaponAspect, 0f);
            quad.Transform.SetPositionAndRotation(origin - rotation * gripOffset, rotation);
            quad.Transform.localScale = new Vector3(length, length * WeaponAspect, 1f);
            quad.SetFloat(EffectQuad.AlphaId, alpha);
        }

        private static void SetWeaponDepth(EffectQuad quad, float bladeAngle, int frontOrder)
        {
            bool behind = Mathf.Sin(bladeAngle * Mathf.Deg2Rad) > 0f;
            quad.SetSorting(SortingLayer.NameToID(behind ? "MapTile" : "Projectile"), behind ? 50 : frontOrder);
        }

        private static Vector3 TipPosition(Vector3 origin, float angle, float length, float flatten)
        {
            Vector3 delta = Quaternion.Euler(0f, 0f, angle) * new Vector3(
                (CuttingTipUV.x - GripUV.x) * length,
                (CuttingTipUV.y - GripUV.y) * length * WeaponAspect, 0f);
            delta.y *= flatten;
            return origin + delta;
        }

        private void Ghost(Vector3 origin, float angle, float length, float flatten, float bladeOffset)
        {
            EffectQuad ghost = PrepareWeapon(m_ghostPool, origin, flatten, Blood, Hot, true);
            Place(ghost, origin, angle, length, .12f);
            SetWeaponDepth(ghost, angle + bladeOffset, 30);
            float life = 0f;
            Tween fade = DOTween.To(() => life, t => {
                life = t;
                ghost.SetFloat(EffectQuad.AlphaId, .12f * (1f - t) * (1f - t));
            }, 1f, Mathf.Max(.02f, m_ghostLife)).SetEase(Ease.Linear).SetLink(gameObject, LinkBehaviour.KillOnDisable);
            Track(fade, () => m_ghostPool.Release(ghost));
        }

        private void PlayHit(Hit hit, float cell)
        {
            if (m_hitFx != null)
            {
                float tilt = (float)(m_random.NextDouble() * 30.0 - 15.0);
                float size = cell * m_hitSize * (hit.Critical ? 1.3f : 1f);
                PlayFlipbook(m_hitFx, hit.Point, hit.Direction + 90f + tilt, new Vector3(size, size, 1f), 1f, hit.Point.y,
                    m_hitDuration, Blood, Hot, Edge, hit.Critical ? 1.35f : 1f);
            }
            else
            {
                Cut(hit.Point, hit.Direction + 90f, cell * (hit.Critical ? 1.6f : 1.05f),
                    cell * (hit.Critical ? .18f : .11f), hit.Critical ? Edge : Blood);
                if (hit.Critical) Accent(m_flarePool, hit.Point, cell * .66f, Edge, .16f, 1.4f);
            }
            float angle = (hit.Direction + 90f) * Mathf.Deg2Rad;
            m_sparks.Burst(hit.Point, hit.Critical ? 7 : 4, hit.Critical ? Hot : Blood,
                cell * 1.4f, cell * 3.6f, cell * .035f, cell * .065f, .12f, .23f,
                new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)), 45f);
        }

        private void Cut(Vector3 point, float angle, float length, float width, Color color)
        {
            EffectQuad quad = m_cutPool.Get();
            quad.Transform.SetPositionAndRotation(point, Quaternion.Euler(0f, 0f, angle));
            quad.Transform.localScale = new Vector3(length, width, 1f);
            quad.SetColor(color); quad.SetFloat(EffectQuad.IntensityId, 1.65f);
            quad.SetFloat(EffectQuad.HeadFollowId, 1f);
            quad.SetFloat(EffectQuad.ProgressId, 0f);
            float time = 0f;
            Tween swipe = DOTween.To(() => time, t => {
                time = t;
                quad.SetFloat(EffectQuad.ProgressId, Mathf.Clamp01(t / .22f));
                quad.SetFloat(EffectQuad.TailId, Mathf.Clamp01((t - .35f) / .65f));
                quad.SetFloat(EffectQuad.AlphaId, 1f - t * t);
            }, 1f, .24f).SetEase(Ease.Linear).SetLink(gameObject, LinkBehaviour.KillOnDisable);
            Track(swipe, () => m_cutPool.Release(quad));
        }

        private void Accent(EffectQuadPool pool, Vector3 point, float size, Color color, float duration, float intensity)
        {
            EffectQuad quad = pool.Get();
            quad.Transform.position = point; quad.Transform.localScale = Vector3.zero;
            quad.SetColor(color); quad.SetFloat(EffectQuad.IntensityId, intensity);
            if (pool == m_ringPool) quad.SetSorting(SortingLayer.NameToID("MapTile"), 47);
            float time = 0f;
            Tween pop = DOTween.To(() => time, t => {
                time = t;
                float scale = size * (.55f + .45f * Mathf.Sin(t * Mathf.PI * .5f));
                quad.Transform.localScale = new Vector3(scale, scale * (pool == m_ringPool ? m_planeFlatten : 1f), 1f);
                quad.SetFloat(EffectQuad.AlphaId, (1f - t) * (1f - t));
            }, 1f, duration).SetEase(Ease.Linear).SetLink(gameObject, LinkBehaviour.KillOnDisable);
            Track(pop, () => pool.Release(quad));
        }


        private void Finish(Vector3 origin, float angle, float length, float flatten, float cell)
        {
            Vector3 tip = TipPosition(origin, angle, length, flatten);
            Accent(m_flarePool, tip, cell * .56f, Edge, .11f, 1.35f);
            // 엔딩은 도끼 정지와 짧은 압력파만 남기고 판 전체의 빛 번짐을 피한다.
            if (m_burstFx != null)
            {
                float reach = length * m_burstReach;
                PlayFlipbook(m_burstFx, origin, 0f, new Vector3(reach, reach, 1f), flatten, origin.y,
                    m_burstDuration, BurstDeep, BurstMid, Edge, 1f);
            }
            else Accent(m_ringPool, origin, length * 1.68f, Blood, .16f, .38f);
            ScreenShake.Play(.035f, .10f);
        }

        private static EffectQuad PrepareFlipbook(EffectQuadPool pool, Vector3 position, float angle, Vector3 scale,
            float flatten, float pivotY, float hemisphere, Color deep, Color mid, Color hot, float intensity = 1f)
        {
            EffectQuad quad = pool.Get();
            quad.Transform.SetPositionAndRotation(position, Quaternion.Euler(0f, 0f, angle));
            quad.Transform.localScale = scale;
            quad.SetColor(deep); quad.SetColorById(EffectQuad.Color2Id, mid); quad.SetColorById(Color3Id, hot);
            quad.SetFloat(FlattenId, flatten); quad.SetFloat(PivotYId, pivotY);
            quad.SetFloat(HemisphereId, hemisphere);
            quad.SetFloat(EffectQuad.IntensityId, intensity);
            quad.SetFloat(FrameId, 0f);
            quad.SetFloat(EffectQuad.AlphaId, 0f);
            return quad;
        }

        // 16프레임을 한 번 재생하고 반환
        private void PlayFlipbook(EffectQuadPool pool, Vector3 position, float angle, Vector3 scale, float flatten, float pivotY,
            float duration, Color deep, Color mid, Color hot, float intensity)
        {
            EffectQuad quad = PrepareFlipbook(pool, position, angle, scale, flatten, pivotY, 0f, deep, mid, hot, intensity);
            quad.SetFloat(EffectQuad.AlphaId, 1f);
            float time = 0f;
            Tween play = DOTween.To(() => time, t => {
                time = t;
                quad.SetFloat(FrameId, t * FlipbookLast);
            }, 1f, Mathf.Max(.05f, duration)).SetEase(Ease.Linear).SetLink(gameObject, LinkBehaviour.KillOnDisable);
            Track(play, () => pool.Release(quad));
        }
    }
}
