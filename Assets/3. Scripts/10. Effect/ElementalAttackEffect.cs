using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace SquareTD
{
    /// <summary>Elemental basic attacks only. Damage and additional attacks remain owned by the caller.</summary>
    [DisallowMultipleComponent]
    public sealed class ElementalAttackEffect : MonoBehaviour, IShotEffect
    {
        public enum Attack
        {
            Fireball = 207, AquaBlast = 108, AquaShot = 208,
            RockBlast = 109, MudShot = 209, Gust = 110, WindCutter = 210,
            Snowball = 211, Sparkling = 112, ShockBolt = 212,
            StarDust = 113, LaserBeam = 213, ShadowBullet = 214
        }

        [SerializeField] private Attack m_attack;
        private EffectQuadPool m_shape, m_light, m_glow, m_ring, m_flare, m_smoke, m_spark, m_ice;
        private readonly List<Tween> m_active = new List<Tween>();
        private readonly Dictionary<EffectQuad, EffectQuadPool> m_leased = new Dictionary<EffectQuad, EffectQuadPool>();
        private int m_serial;
        private bool m_stopping;
        private Color Body => Palette(m_attack, false);
        private Color Core => Palette(m_attack, true);
        private bool Splash => (int)m_attack < 200;

        public static ElementalAttackEffect Make(Transform parent, Attack attack)
        {
            var effect = EffectSpace.Make<ElementalAttackEffect>(parent, attack + " Basic Attack");
            effect.m_attack = attack;
            return effect;
        }

        private void EnsurePools()
        {
            if (m_shape != null) return;
            m_shape = new EffectQuadPool(transform, "ElementalAttack", "Element Body", 28);
            m_light = new EffectQuadPool(transform, "ElementalAttack", "Element Stroke", 31, additive: true);
            m_glow = new EffectQuadPool(transform, "EffectSprite", "Element Halo", 26, EffectTextures.Glow, true);
            m_ring = new EffectQuadPool(transform, "EffectSprite", "Element Ripple", 27, EffectTextures.Ring, true);
            m_flare = new EffectQuadPool(transform, "EffectSprite", "Element Flash", 32, EffectTextures.Flare, true);
            m_smoke = new EffectQuadPool(transform, "EffectSprite", "Element Dust", 25, EffectTextures.Smoke);
            m_spark = new EffectQuadPool(transform, "EffectSprite", "Element Spark", 32, EffectTextures.Spark, true);
            m_ice = new EffectQuadPool(transform, "EffectSprite", "Snow Crystal", 30, EffectTextures.IceShard, true);
        }

        public void FireShot(Vector3 from, Vector3 to, float speed, float hold, float sizeWorld, Action onArrived)
        {
            EnsurePools();
            if (m_stopping || !isActiveAndEnabled || !m_shape.IsReady || !m_light.IsReady ||
                !m_glow.IsReady || !m_ring.IsReady || !m_flare.IsReady ||
                !m_smoke.IsReady || !m_spark.IsReady || !m_ice.IsReady)
            {
                onArrived?.Invoke();
                return;
            }

            float cell = Mathf.Max(.001f, EffectSpace.CellWorldSize());
            float area = Mathf.Max(cell, sizeWorld);
            float angle = Angle(to - from);
            float charge = Mathf.Max(0f, hold, m_attack == Attack.LaserBeam ? .18f : .10f);
            float flight = Mathf.Max(.12f, Vector3.Distance(from, to) / Mathf.Max(.01f, speed));
            // A beam reaches its target at the end of its draw; ground eruption needs no travelling orb.
            if (m_attack == Attack.LaserBeam) flight = .07f;
            if (m_attack == Attack.RockBlast) flight = .12f;
            int seed = ++m_serial;
            var bodies = new List<EffectQuad>();
            float lastTrail = 0f;
            bool arrived = false;
            float progress = 0f;

            Halo(m_attack == Attack.RockBlast ? to : from, cell * .16f, cell * .65f,
                charge, Body, .55f);
            if (m_attack == Attack.RockBlast)
                Shape(m_shape, to, new Vector2(area, area * .62f), 0, 9, .0f, charge + flight, (q, t) =>
                {
                    q.SetFloat(EffectQuad.ProgressId, t);
                    q.SetFloat(EffectQuad.AlphaId, t * .65f);
                });

            Sequence sequence = DOTween.Sequence().SetLink(gameObject, LinkBehaviour.KillOnDisable);
            sequence.AppendInterval(charge);
            sequence.AppendCallback(() =>
            {
                if (m_attack == Attack.RockBlast || m_attack == Attack.LaserBeam) return;
                int count = m_attack == Attack.WindCutter ? 3 : m_attack == Attack.Gust ? 4 : 1;
                for (int i = 0; i < count; i++)
                {
                    EffectQuadPool pool = UsesLightBody ? m_light : m_shape;
                    EffectQuad q = Acquire(pool);
                    SetShape(q, ShapeIndex, Body, Core, seed + i);
                    bodies.Add(q);
                }
            });
            sequence.Append(DOTween.To(() => progress, t =>
            {
                float eased = m_attack == Attack.ShockBolt ? Mathf.Sqrt(t) : t;
                Vector3 position = Vector3.Lerp(from, to, eased);
                if (m_attack == Attack.MudShot || m_attack == Attack.Snowball)
                    position += Vector3.up * (Mathf.Sin(t * Mathf.PI) * cell * .45f);
                progress = t;
                for (int i = 0; i < bodies.Count; i++)
                {
                    EffectQuad q = bodies[i];
                    Vector3 sideways = Quaternion.Euler(0, 0, angle) * Vector3.up;
                    float offset = (i - (bodies.Count - 1) * .5f) * cell * .26f;
                    q.Transform.position = position + sideways * offset;
                    q.Transform.rotation = Quaternion.Euler(0, 0, angle + (ShapeIndex == 2 ? -90f + i * 12f : 0));
                    Vector2 size = ProjectileSize * cell;
                    q.Transform.localScale = new Vector3(size.x, size.y, 1);
                    q.SetFloat(EffectQuad.PhaseId, seed + t * flight);
                    q.SetFloat(EffectQuad.AlphaId, Mathf.Min(1, t * 16 + .2f));
                }
                if (m_attack == Attack.LaserBeam)
                    return; // a separate beam quad is animated below, with no projectile body
                if (m_attack == Attack.RockBlast) return;
                float time = t * flight;
                if (time - lastTrail >= .045f)
                {
                    lastTrail = time;
                    Trail(position, angle, cell, seed + Mathf.FloorToInt(time / .045f));
                }
            }, 1f, flight).SetEase(Ease.Linear));
            if (m_attack == Attack.LaserBeam)
                sequence.InsertCallback(charge, () => Beam(from, to, cell));
            sequence.AppendCallback(() =>
            {
                foreach (var q in bodies) q.SetFloat(EffectQuad.AlphaId, 0);
                Impact(to, area, cell, angle, seed);
                if (!arrived) { arrived = true; onArrived?.Invoke(); }
            });
            Track(sequence, () =>
            {
                foreach (var q in bodies) Release(q);
            });
        }

        private bool UsesLightBody => m_attack == Attack.Gust || m_attack == Attack.WindCutter ||
            m_attack == Attack.ShockBolt || m_attack == Attack.Sparkling || m_attack == Attack.StarDust;
        private int ShapeIndex => m_attack == Attack.Fireball ? 7 :
            m_attack == Attack.MudShot ? 1 :
            m_attack == Attack.Gust || m_attack == Attack.WindCutter ? 2 :
            m_attack == Attack.ShockBolt || m_attack == Attack.Sparkling ? 3 :
            m_attack == Attack.StarDust ? 4 : m_attack == Attack.ShadowBullet ? 6 :
            m_attack == Attack.Snowball ? 8 : 0;
        private Vector2 ProjectileSize => m_attack == Attack.Fireball ? new Vector2(.9f, .55f) :
            m_attack == Attack.AquaBlast ? new Vector2(.88f, .55f) :
            m_attack == Attack.AquaShot ? new Vector2(.58f, .25f) :
            m_attack == Attack.Gust ? new Vector2(.9f, .78f) :
            m_attack == Attack.WindCutter ? new Vector2(.68f, .45f) :
            m_attack == Attack.ShockBolt ? new Vector2(1.05f, .45f) :
            m_attack == Attack.Sparkling ? new Vector2(.7f, .65f) :
            m_attack == Attack.MudShot ? new Vector2(.52f, .48f) :
            m_attack == Attack.StarDust ? new Vector2(.62f, .62f) : new Vector2(.55f, .55f);

        private void Trail(Vector3 position, float angle, float cell, int seed)
        {
            if (m_attack == Attack.Gust || m_attack == Attack.WindCutter)
            {
                Shape(m_light, position, new Vector2(cell * .5f, cell * .38f), angle - 90, 2, seed, .22f,
                    (q, t) => q.SetFloat(EffectQuad.AlphaId, (1 - t) * .45f));
                return;
            }
            if (m_attack == Attack.Sparkling || m_attack == Attack.ShockBolt)
            {
                Shape(m_light, position, new Vector2(cell * .5f, cell * .35f), angle + 90, 3, seed, .12f,
                    (q, t) => q.SetFloat(EffectQuad.AlphaId, 1 - t));
                return;
            }
            if (m_attack == Attack.ShadowBullet)
            {
                Shape(m_shape, position, Vector2.one * cell * .44f, 0, 6, seed, .23f, (q, t) =>
                {
                    q.Transform.localScale = Vector3.one * cell * (.44f + t * .20f);
                    q.SetFloat(EffectQuad.AlphaId, (1 - t) * .6f);
                });
                return;
            }
            Vector3 drift = Quaternion.Euler(0, 0, angle) * Vector3.left * cell * .25f;
            if (m_attack == Attack.MudShot)
                Puff(position, drift + Vector3.down * cell * .25f, cell * .18f, .30f, Body);
            else if (m_attack == Attack.Snowball)
                Fleck(position, drift, cell * .12f, .24f, Core, m_ice, seed * 31);
            else if (m_attack == Attack.StarDust)
                Star(position, cell * .18f, .23f, seed);
            else if (m_attack == Attack.Fireball)
            {
                Puff(position, Vector3.up * cell * .23f, cell * .32f, .26f, new Color(.22f, .12f, .08f, .45f));
                Fleck(position, drift, cell * .09f, .22f, Core, m_spark, seed * 31);
            }
            else // water droplets peel off the water cannon / compact water shot
                Shape(m_shape, position, Vector2.one * cell * .16f, 0, 0, seed, .23f, (q, t) =>
                {
                    q.Transform.position = position + drift * t + Vector3.down * cell * t * t * .2f;
                    q.SetFloat(EffectQuad.AlphaId, (1 - t) * .75f);
                });
        }

        private void Impact(Vector3 center, float area, float cell, float angle, int seed)
        {
            float radius = Splash ? area : cell * .78f;
            if (m_attack != Attack.MudShot && m_attack != Attack.RockBlast)
                Sprite(m_flare, center, Core, .14f, (q, t) => {
                    q.Transform.localScale = Vector3.one * cell * (.28f + t * .35f);
                    q.SetFloat(EffectQuad.AlphaId, (1 - t) * .65f);
                });
            switch (m_attack)
            {
                case Attack.RockBlast:
                    RockBlast(center, area, cell, seed);
                    return;
                case Attack.AquaBlast:
                case Attack.AquaShot:
                    Ripple(center, radius, .35f);
                    Halo(center, cell * .15f, radius * .95f, .23f, Core, .65f);
                    for (int i = 0; i < (Splash ? 12 : 6); i++)
                    {
                        float a = i * 137.508f + seed * 17;
                        Vector3 direction = Direction(a);
                        Vector3 start = center + direction * cell * .12f;
                        float distance = radius * (.34f + .14f * Hash(i + seed));
                        Shape(m_shape, start, new Vector2(cell * .17f, cell * .28f), a - 90, 0, i, .36f, (q, t) =>
                        {
                            q.Transform.position = start + direction * distance * Out(t) + Vector3.down * cell * t * t * .2f;
                            q.SetFloat(EffectQuad.AlphaId, 1 - t * t);
                        });
                    }
                    if (Splash)
                        for (int i = 0; i < 3; i++) WindArc(center, radius * (.75f + i * .14f), i * 120 + seed, .26f, Body);
                    break;
                case Attack.Gust:
                case Attack.WindCutter:
                    for (int i = 0; i < (Splash ? 5 : 3); i++)
                        WindArc(center + Direction(angle + 90) * (i - 1) * cell * .1f,
                            radius * (.7f + i * .09f), angle - 60 + i * 48, .23f + i * .025f, Core);
                    Flecks(center, radius * .5f, cell, seed, Body, 8);
                    break;
                case Attack.Sparkling:
                case Attack.ShockBolt:
                    Halo(center, cell * .1f, radius, .18f, Body, .9f);
                    for (int i = 0; i < (Splash ? 8 : 4); i++)
                    {
                        float a = seed * 17 + i * (Splash ? 45 : 90);
                        Vector3 end = center + Direction(a) * radius * .46f;
                        Bolt(center, end, cell * (Splash ? .34f : .24f), seed + i, .20f + i % 2 * .04f);
                        Fleck(end, Direction(a) * cell * .22f, cell * .1f, .24f, Core, m_spark, a);
                    }
                    if (Splash) Ripple(center, radius, .25f);
                    break;
                case Attack.StarDust:
                    Halo(center, cell * .15f, radius * 1.15f, .32f, Core, .75f);
                    Ripple(center, radius, .38f);
                    for (int i = 0; i < 9; i++)
                    {
                        float a = i * 137.508f + seed;
                        Vector3 end = center + Direction(a) * radius * (.24f + Hash(i + seed) * .22f);
                        Star(end, cell * (.15f + Hash(i) * .16f), .28f + i * .018f, i);
                        if (i < 5) Streak(center, end, cell * .08f, .22f, Core);
                    }
                    break;
                case Attack.LaserBeam:
                    Halo(center, cell * .1f, cell * .7f, .22f, Core, 1f);
                    Star(center, cell * .6f, .24f, seed);
                    Flecks(center, cell * .5f, cell, seed, Body, 6);
                    break;
                case Attack.ShadowBullet:
                    Shape(m_shape, center, Vector2.one * cell * .7f, 0, 6, seed, .24f, (q, t) =>
                    {
                        q.Transform.localScale = new Vector3(cell * (.7f + t * .5f), cell * (.7f + t * .5f), 1);
                        q.SetFloat(EffectQuad.AlphaId, 1 - t);
                    });
                    for (int i = 0; i < 4; i++) WindArc(center, cell * (.6f + i * .08f), i * 90, .22f, Core);
                    Flecks(center, cell * .5f, cell, seed, Core, 7);
                    break;
                case Attack.MudShot:
                    Puff(center, Vector3.zero, cell * .65f, .36f, Body);
                    for (int i = 0; i < 7; i++)
                    {
                        Vector3 velocity = Direction(i * 137.508f) * cell * (.25f + Hash(seed + i) * .3f);
                        Shape(m_shape, center, Vector2.one * cell * .2f, i * 36, 1, i, .34f, (q, t) =>
                        {
                            q.Transform.position = center + velocity * Out(t) + Vector3.down * cell * t * t * .25f;
                            q.SetFloat(EffectQuad.AlphaId, 1 - t * t);
                        });
                    }
                    break;
                case Attack.Snowball:
                    Puff(center, Vector3.up * cell * .1f, cell * .65f, .3f, new Color(.85f, .94f, 1f, .8f));
                    for (int i = 0; i < 8; i++)
                        Fleck(center, Direction(i * 45 + seed) * cell * .5f, cell * .14f, .32f, Core, m_ice, i * 41);
                    Ripple(center, cell * .65f, .25f);
                    break;
                case Attack.Fireball:
                    Halo(center, cell * .12f, cell * .9f, .2f, Core, 1f);
                    Ripple(center, cell * .8f, .24f);
                    for (int i = 0; i < 5; i++)
                        Puff(center, Direction(i * 72) * cell * .25f + Vector3.up * cell * .2f,
                            cell * .4f, .36f, new Color(.3f, .12f, .05f, .5f));
                    Flecks(center, cell * .6f, cell, seed, Core, 9);
                    break;
            }
        }

        private void RockBlast(Vector3 center, float area, float cell, int seed)
        {
            var centers = new List<Vector3>();
            BoardManager board = BoardManager.Instance;
            if (board != null && board.Grid != null && board.TryGetPoint(center, out BoardPoint point) && point.IsPath)
            {
                int radius = Mathf.Max(0, Mathf.RoundToInt((area / cell - 1) * .5f));
                foreach (int index in board.Grid.GetPathIndicesInSquare(board.Grid.GetPathCell(point.PathIndex), radius))
                    centers.Add(board.GetWorldPosition(board.Grid.GetPathCell(index)));
            }
            if (centers.Count == 0) centers.Add(center);
            foreach (Vector3 tile in centers)
            {
                Shape(m_light, tile, new Vector2(cell * 1.1f, cell * .7f), 0, 9, seed, .42f, (q, t) =>
                {
                    q.SetFloat(EffectQuad.ProgressId, Out(t));
                    q.SetFloat(EffectQuad.AlphaId, 1 - t);
                });
                for (int i = 0; i < 3; i++)
                {
                    int index = i;
                    Vector3 origin = tile + new Vector3((i - 1) * cell * .24f, -cell * .1f, 0);
                    float height = cell * (.75f + Hash(seed + i) * .35f);
                    Shape(m_shape, origin, new Vector2(cell * .47f, height), (i - 1) * -14f, 1, i, .55f, (q, t) =>
                    {
                        float rise = Out(Mathf.Min(1, t / .19f));
                        float sink = Mathf.Max(0, (t - .52f) / .48f);
                        q.Transform.position = origin + Vector3.up * height * .28f * (rise - sink);
                        q.Transform.localScale = new Vector3(cell * .47f, height * Mathf.Max(.01f, rise - sink), 1);
                        q.SetFloat(EffectQuad.AlphaId, 1 - Mathf.Max(0, (t - .62f) / .38f));
                        q.SetSortingOffset(index);
                    });
                }
                Puff(tile, Vector3.left * cell * .3f, cell * .6f, .43f, new Color(.6f, .46f, .27f, .55f));
                Puff(tile, Vector3.right * cell * .3f, cell * .6f, .43f, new Color(.6f, .46f, .27f, .55f));
            }
            Flecks(center, area * .46f, cell, seed, Core, 10);
        }

        private void Beam(Vector3 from, Vector3 to, float cell)
        {
            float length = Vector3.Distance(from, to);
            float angle = Angle(to - from);
            Shape(m_light, (from + to) * .5f, new Vector2(Mathf.Max(cell * .1f, length), cell * .7f),
                angle, 5, 0, .23f, (q, t) =>
                {
                    float draw = Mathf.Clamp01(t / .30f);
                    q.Transform.position = Vector3.Lerp(from, to, draw * .5f);
                    q.Transform.localScale = new Vector3(Mathf.Max(.001f, length * draw), cell * (.7f - t * .25f), 1);
                    q.SetFloat(EffectQuad.AlphaId, 1 - Mathf.Max(0, (t - .36f) / .64f));
                });
            Halo(from, cell * .18f, cell * .55f, .18f, Core, .8f);
        }

        private void Bolt(Vector3 from, Vector3 to, float width, int seed, float life)
        {
            Shape(m_light, (from + to) * .5f, new Vector2(Vector3.Distance(from, to), width),
                Angle(to - from), 3, seed, life, (q, t) =>
                {
                    q.SetFloat(EffectQuad.PhaseId, seed + t * life);
                    q.SetFloat(EffectQuad.AlphaId, 1 - t * t);
                });
        }
        private void Streak(Vector3 from, Vector3 to, float width, float life, Color color)
        {
            Shape(m_light, (from + to) * .5f, new Vector2(Vector3.Distance(from, to), width * 3),
                Angle(to - from), 5, 0, life, (q, t) => q.SetFloat(EffectQuad.AlphaId, 1 - t), color);
        }
        private void WindArc(Vector3 position, float size, float angle, float life, Color color)
        {
            Shape(m_light, position, new Vector2(size, size * .78f), angle, 2, 0, life, (q, t) =>
            {
                q.Transform.rotation = Quaternion.Euler(0, 0, angle + t * 80);
                q.Transform.localScale = new Vector3(size * (.7f + t * .5f), size * (.55f + t * .4f), 1);
                q.SetFloat(EffectQuad.AlphaId, 1 - t * t);
            }, color);
        }
        private void Star(Vector3 center, float size, float life, int seed)
        {
            Shape(m_light, center, Vector2.one * size, seed * 23, 4, seed, life, (q, t) =>
            {
                q.Transform.localScale = Vector3.one * size * Mathf.Sin(t * Mathf.PI);
                q.SetFloat(EffectQuad.AlphaId, 1 - t * .65f);
            });
        }
        private void Ripple(Vector3 center, float diameter, float life)
        {
            Sprite(m_ring, center, Body, life, (q, t) =>
            {
                float size = diameter * (.22f + Out(t));
                q.Transform.localScale = new Vector3(size, size * .62f, 1);
                q.SetFloat(EffectQuad.AlphaId, (1 - t) * .8f);
            });
        }
        private void Halo(Vector3 center, float start, float end, float life, Color color, float alpha)
        {
            Sprite(m_glow, center, color, life, (q, t) =>
            {
                q.Transform.localScale = Vector3.one * Mathf.Lerp(start, end, Out(t));
                q.SetFloat(EffectQuad.AlphaId, alpha * (1 - t));
            });
        }
        private void Puff(Vector3 center, Vector3 drift, float size, float life, Color color)
        {
            Sprite(m_smoke, center, color, life, (q, t) =>
            {
                q.Transform.position = center + drift * Out(t);
                q.Transform.localScale = Vector3.one * size * (.5f + t * .8f);
                q.SetFloat(EffectQuad.AlphaId, Mathf.Sin(t * Mathf.PI) * .7f);
            });
        }
        private void Flecks(Vector3 center, float radius, float cell, int seed, Color color, int count)
        {
            for (int i = 0; i < count; i++)
                Fleck(center, Direction(i * 137.508f + seed * 13) * radius,
                    cell * (.07f + Hash(i + seed) * .06f), .23f + Hash(i) * .16f, color, m_spark, i * 23);
        }
        private void Fleck(Vector3 center, Vector3 drift, float size, float life, Color color, EffectQuadPool pool, float angle)
        {
            Sprite(pool, center, color, life, (q, t) =>
            {
                q.Transform.position = center + drift * Out(t);
                q.Transform.rotation = Quaternion.Euler(0, 0, angle + t * 70);
                q.Transform.localScale = new Vector3(size * (1 - t * .6f), size * 1.7f * (1 - t * .6f), 1);
                q.SetFloat(EffectQuad.AlphaId, 1 - t * t);
            });
        }
        private void Shape(EffectQuadPool pool, Vector3 center, Vector2 scale, float angle, int shape, float phase,
            float duration, Action<EffectQuad, float> update, Color? color = null)
        {
            EffectQuad q = Acquire(pool);
            q.Transform.position = center;
            q.Transform.localScale = new Vector3(scale.x, scale.y, 1);
            q.Transform.rotation = Quaternion.Euler(0, 0, angle);
            SetShape(q, shape, color ?? Body, Core, phase);
            Animate(pool, q, duration, update);
        }
        private void Sprite(EffectQuadPool pool, Vector3 center, Color color, float duration, Action<EffectQuad, float> update)
        {
            EffectQuad q = Acquire(pool);
            q.Transform.position = center;
            q.SetColor(color);
            q.SetFloat(EffectQuad.IntensityId, 1.2f);
            Animate(pool, q, duration, update);
        }
        private void Animate(EffectQuadPool pool, EffectQuad quad, float duration, Action<EffectQuad, float> update)
        {
            float time = 0;
            update(quad, 0);
            Track(DOTween.To(() => time, t => { time = t; update(quad, t); }, 1, duration)
                .SetEase(Ease.Linear).SetLink(gameObject, LinkBehaviour.KillOnDisable), () => Release(quad));
        }
        private EffectQuad Acquire(EffectQuadPool pool)
        {
            EffectQuad quad = pool.Get();
            m_leased.Add(quad, pool);
            return quad;
        }
        private void Release(EffectQuad quad)
        {
            if (!m_leased.TryGetValue(quad, out var pool)) return;
            m_leased.Remove(quad);
            pool.Release(quad);
        }
        private void Track(Tween tween, Action release)
        {
            m_active.Add(tween);
            tween.OnKill(() => { release(); m_active.Remove(tween); });
        }
        private static void SetShape(EffectQuad q, int shape, Color body, Color core, float phase)
        {
            q.SetColor(body);
            q.SetColorById(EffectQuad.Color2Id, core);
            q.SetFloat(EffectQuad.ShapeId, shape);
            q.SetFloat(EffectQuad.PhaseId, phase);
        }
        private static float Angle(Vector3 v) => Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg;
        private static Vector3 Direction(float degrees) => new Vector3(Mathf.Cos(degrees * Mathf.Deg2Rad), Mathf.Sin(degrees * Mathf.Deg2Rad), 0);
        private static float Out(float t) => 1 - (1 - t) * (1 - t);
        private static float Hash(int n) => Mathf.Repeat(Mathf.Sin(n * 127.1f + 31.7f) * 43758.5453f, 1);
        private static Color Palette(Attack attack, bool core)
        {
            switch (attack)
            {
                case Attack.Fireball: return core ? new Color(1, .86f, .32f) : new Color(1, .23f, .025f);
                case Attack.AquaBlast: case Attack.AquaShot:
                    return core ? new Color(.74f, .97f, 1) : new Color(.035f, .48f, .91f);
                case Attack.RockBlast: case Attack.MudShot:
                    return core ? new Color(.83f, .66f, .37f) : new Color(.29f, .17f, .085f);
                case Attack.Gust: case Attack.WindCutter:
                    return core ? new Color(.85f, 1, .92f) : new Color(.28f, .78f, .63f);
                case Attack.Snowball:
                    return core ? new Color(.95f, 1, 1) : new Color(.53f, .75f, .88f);
                case Attack.Sparkling: case Attack.ShockBolt:
                    return core ? new Color(1, .98f, .74f) : new Color(.53f, .37f, 1);
                case Attack.StarDust: case Attack.LaserBeam:
                    return core ? new Color(1, .98f, .83f) : new Color(1, .71f, .24f);
                default: return core ? new Color(.77f, .32f, 1) : new Color(.045f, .012f, .09f);
            }
        }
        private void OnEnable() => m_stopping = false;
        private void OnDisable()
        {
            m_stopping = true;
            // Snapshot: OnKill removes itself from the live list.
            foreach (Tween tween in m_active.ToArray()) tween.Kill();
            m_active.Clear();
            // Also release leases whose tween was killed before DOTween completed startup.
            foreach (var quad in new List<EffectQuad>(m_leased.Keys)) Release(quad);
        }
        private void OnDestroy()
        {
            foreach (var pool in new[] { m_shape, m_light, m_glow, m_ring, m_flare, m_smoke, m_spark, m_ice })
                pool?.Dispose();
        }
    }
}
