using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace SquareTD
{
    /// <summary>Named weapon basic attacks. Only the impact callback deals damage through the existing caller.</summary>
    [DisallowMultipleComponent]
    public sealed class WeaponAttackEffect : MonoBehaviour, IShotEffect
    {
        public enum Attack { BurstShot = 103, ShieldBash = 105, HeavySmash = 106, Javelin = 204, ShieldToss = 205, ThrowingHammer = 206 }
        [SerializeField] private Attack m_attack = Attack.ShieldBash;
        private EffectQuadPool m_solid, m_light, m_projectile, m_trail, m_glow, m_ring, m_flare, m_smoke, m_spark, m_burst;
        private readonly HashSet<Tween> m_tweens = new HashSet<Tween>();
        private readonly Dictionary<EffectQuad, EffectQuadPool> m_leases = new Dictionary<EffectQuad, EffectQuadPool>();
        private bool m_stopping;
        private int m_generation, m_serial;
        private static readonly Color Steel = new Color(.58f, .74f, .88f);
        private static readonly Color Silver = new Color(.92f, .97f, 1f);
        private static readonly Color Gold = new Color(1f, .81f, .39f);
        private static readonly Color Amber = new Color(1f, .36f, .065f);
        private static readonly Color Dust = new Color(.56f, .46f, .35f, .58f);

        public static WeaponAttackEffect Make(Transform parent, Attack attack)
        {
            var effect = EffectSpace.Make<WeaponAttackEffect>(parent, attack + " Basic Attack");
            effect.m_attack = attack;
            return effect;
        }

        private void EnsurePools()
        {
            if (m_solid != null) return;
            m_solid = new EffectQuadPool(transform, "WeaponAttack", "Weapon Metal", 28);
            m_light = new EffectQuadPool(transform, "WeaponAttack", "Weapon Shock", 30, additive: true);
            m_projectile = new EffectQuadPool(transform, "SwordQi", "Weapon Projectile", 28);
            m_trail = new EffectQuadPool(transform, "EffectSprite", "Weapon Motion", 27, EffectTextures.SlashLine, true);
            m_glow = new EffectQuadPool(transform, "EffectSprite", "Weapon Halo", 26, EffectTextures.Glow, true);
            m_ring = new EffectQuadPool(transform, "EffectSprite", "Weapon Impact Ring", 27, EffectTextures.Ring, true);
            m_flare = new EffectQuadPool(transform, "EffectSprite", "Weapon Contact", 32, EffectTextures.Flare, true);
            m_smoke = new EffectQuadPool(transform, "EffectSprite", "Weapon Dust", 25, EffectTextures.Smoke);
            m_spark = new EffectQuadPool(transform, "EffectSprite", "Weapon Sparks", 32, EffectTextures.Spark, true);
            m_burst = new EffectQuadPool(transform, "EffectSprite", "Arrow Explosive Burst", 31, EffectTextures.MeteorBurst, true);
        }

        public void FireShot(Vector3 from, Vector3 to, float speed, float hold, float sizeWorld, Action onArrived)
        {
            EnsurePools();
            if (m_stopping || !isActiveAndEnabled || !Ready)
            {
                onArrived?.Invoke();
                return;
            }
            float cell = Mathf.Max(.001f, EffectSpace.CellWorldSize());
            float area = Mathf.Max(cell, sizeWorld);
            int generation = m_generation, seed = ++m_serial;
            bool arrived = false;
            Action hit = () =>
            {
                if (arrived || generation != m_generation || m_stopping) return;
                arrived = true;
                Impact(to, area, cell, Angle(to - from), seed);
                onArrived?.Invoke();
            };
            if (m_attack == Attack.HeavySmash)
                Smash(to, cell, Mathf.Max(hold, .16f), generation, hit);
            else
                Throw(from, to, speed, hold, cell, generation, hit);
        }

        private bool Ready => m_solid.IsReady && m_light.IsReady && m_projectile.IsReady &&
            m_trail.IsReady && m_glow.IsReady && m_ring.IsReady && m_flare.IsReady &&
            m_smoke.IsReady && m_spark.IsReady && m_burst.IsReady;

        private void Throw(Vector3 from, Vector3 to, float speed, float hold, float cell, int generation, Action hit)
        {
            bool arrow = m_attack == Attack.BurstShot;
            bool spear = m_attack == Attack.Javelin;
            bool bash = m_attack == Attack.ShieldBash;
            bool toss = m_attack == Attack.ShieldToss;
            Vector3 delta = to - from;
            Vector3 forward = delta.sqrMagnitude > .00001f ? delta.normalized : Vector3.right;
            Vector3 perpendicular = new Vector3(-forward.y, forward.x, 0);
            float angle = Angle(forward);
            float side = delta.x >= 0 ? 1 : -1;
            float size = cell * (spear ? 1.20f : arrow ? .76f : bash ? 1.22f : toss ? .72f : .78f);
            int shape = arrow ? 8 : spear ? 9 : bash ? 0 : toss ? 1 : 2;
            var pool = arrow || spear ? m_projectile : m_solid;
            EffectQuad weapon = Acquire(pool);
            EffectQuad payload = arrow ? Acquire(m_solid) : null;
            if (payload != null) {
                payload.SetFloat(EffectQuad.ShapeId, 6);
                payload.SetFloat(EffectQuad.AlphaId, 0);
                payload.SetColor(new Color(.25f, .17f, .10f));
                payload.SetColorById(EffectQuad.Color2Id, Gold);
                payload.SetSortingOffset(1);
            }
            weapon.SetColor(arrow ? ArrowFx.Wood : spear ? SpearFx.Steel : Steel);
            weapon.SetColorById(EffectQuad.Color2Id, toss ? Silver : Gold);
            weapon.SetFloat(EffectQuad.ShapeId, shape);
            weapon.SetFloat(EffectQuad.AlphaId, 0);
            Vector3 scale = arrow ? new Vector3(size, size * .5f, 1) :
                spear ? SpearFx.Scale(size) : bash ? new Vector3(size * .8f, size, 1) : new Vector3(size, size, 1);
            float baseAngle = arrow || spear ? angle : bash ? -side * 9 : 0;
            Vector3 launch = from - forward * cell * .16f;
            // Arrow/spear impact is aligned to the visible tip, rather than the centre of the quad.
            Vector3 contact = spear ? SpearFx.CenterForTip(to, forward, size, .035f) :
                arrow ? to - forward * (size * .475f) : to;
            weapon.Transform.position = launch;
            weapon.Transform.rotation = Quaternion.Euler(0, 0, baseAngle);
            weapon.Transform.localScale = Vector3.zero;
            float charge = Mathf.Max(0, hold, bash ? .16f : .12f);
            float flight = Mathf.Clamp(Vector3.Distance(launch, contact) / Mathf.Max(.01f, speed), .12f, bash ? .28f : .55f);
            float progress = 0, trailTime = 0;
            Halo(from, cell * .42f, charge, arrow ? Amber : Silver, .45f);
            Sequence sequence = DOTween.Sequence().SetLink(gameObject, LinkBehaviour.KillOnDisable);
            sequence.Append(weapon.Transform.DOScale(scale, .10f).SetEase(Ease.OutBack));
            sequence.Join(DOTween.To(() => 0f, a => weapon.SetFloat(EffectQuad.AlphaId, a), 1, .08f));
            sequence.AppendInterval(Mathf.Max(0, charge - .10f));
            sequence.Append(DOTween.To(() => progress, t =>
            {
                progress = t;
                if (generation != m_generation || m_stopping) return;
                float motion = bash ? t * t * t : t;
                float wave = Mathf.Sin(motion * Mathf.PI);
                Vector3 arc = toss ? perpendicular * (cell * .32f * wave) :
                    Vector3.up * cell * (spear ? .22f : arrow ? .06f : .28f) * wave * wave;
                weapon.Transform.position = Vector3.Lerp(launch, contact, motion) + arc;
                float rotation = baseAngle;
                if (toss || m_attack == Attack.ThrowingHammer) rotation -= side * t * flight * (toss ? 1080 : 1440);
                if (spear || arrow)
                {
                    Vector3 tangent = contact - launch + Vector3.up * cell * (spear ? .22f : .06f) *
                        (2 * Mathf.PI * Mathf.Sin(motion * Mathf.PI) * Mathf.Cos(motion * Mathf.PI));
                    rotation = Angle(tangent);
                }
                weapon.Transform.rotation = Quaternion.Euler(0, 0, rotation);
                weapon.Transform.localScale = bash ? scale * (1 + t * .12f) :
                    toss ? new Vector3(size * (.72f + .28f * Mathf.Abs(Mathf.Cos(t * Mathf.PI * 4))), size, 1) : scale;
                if (payload != null) {
                    payload.Transform.SetPositionAndRotation(weapon.Transform.position + weapon.Transform.rotation * (Vector3.right * size * .17f), weapon.Transform.rotation);
                    payload.Transform.localScale = new Vector3(cell * .23f, cell * .19f, 1);
                    payload.SetFloat(EffectQuad.AlphaId, 1);
                }
                float elapsed = t * flight;
                if (elapsed - trailTime >= .045f)
                {
                    trailTime = elapsed;
                    if (toss || m_attack == Attack.ThrowingHammer || bash)
                        Ghost(pool, weapon, shape, bash ? .16f : .25f);
                    else
                    {
                        Streak(weapon.Transform.position - forward * cell * .20f, angle, cell * .4f, cell * .09f, .13f, spear ? Silver : Gold);
                        if (arrow) Spark(weapon.Transform.position + forward * size * .22f, -forward * cell * .16f,
                            cell * .055f, .15f, Amber, elapsed * 200);
                    }
                }
            }, 1, flight).SetEase(Ease.Linear));
            sequence.AppendCallback(() =>
            {
                weapon.Transform.position = contact;
                if (spear || arrow) weapon.Transform.rotation = Quaternion.Euler(0, 0, angle);
                if (payload != null) payload.SetFloat(EffectQuad.AlphaId, 0);
                hit();
            });
            if (toss)
            {
                float back = 0;
                sequence.AppendInterval(.045f);
                sequence.Append(DOTween.To(() => back, t =>
                {
                    back = t;
                    if (generation != m_generation || m_stopping) return;
                    weapon.Transform.position = Vector3.Lerp(contact, from, t) - perpendicular * cell * .38f * Mathf.Sin(t * Mathf.PI);
                    weapon.Transform.rotation = Quaternion.Euler(0, 0, -side * (flight * 1080 + t * 540));
                    weapon.Transform.localScale = scale * (1 - .25f * t);
                    weapon.SetFloat(EffectQuad.AlphaId, Mathf.Clamp01((1 - t) * 6));
                }, 1, Mathf.Max(.14f, flight * .85f)).SetEase(Ease.InOutQuad));
            }
            else if (arrow)
                sequence.AppendCallback(() => weapon.SetFloat(EffectQuad.AlphaId, 0));
            else
            {
                float fade = 0;
                sequence.AppendInterval(spear ? .12f : .035f);
                sequence.Append(DOTween.To(() => fade, t =>
                {
                    fade = t;
                    if (generation != m_generation || m_stopping) return;
                    weapon.SetFloat(EffectQuad.AlphaId, 1 - t);
                    weapon.Transform.position = spear ? contact :
                        contact - forward * cell * .20f * t + Vector3.up * cell * (.22f * Mathf.Sin(t * Mathf.PI) - .12f * t);
                    if (bash) weapon.Transform.localScale = scale * (1 - t * .15f);
                    if (m_attack == Attack.ThrowingHammer)
                        weapon.Transform.rotation = Quaternion.Euler(0, 0, -side * flight * 1440 + side * t * 170);
                }, 1, .22f).SetEase(Ease.Linear));
            }
            Track(sequence, () => { if (payload != null) Release(payload); Release(weapon); });
        }

        private void Smash(Vector3 target, float cell, float charge, int generation, Action hit)
        {
            float length = cell * 1.9f;
            EffectQuad hammer = Acquire(m_solid);
            hammer.SetColor(Steel);
            hammer.SetColorById(EffectQuad.Color2Id, Gold);
            hammer.SetFloat(EffectQuad.ShapeId, 2);
            hammer.SetFloat(EffectQuad.AlphaId, 0);
            Vector3 contact = target + Vector3.up * cell * .10f;
            Vector3 high = contact + new Vector3(-cell * .55f, cell * 1.55f, 0);
            float swing = 0;
            PlaceHammer(hammer, high, -65, length * .6f);
            Halo(target, cell * .7f, charge, Gold, .38f);
            Sequence sequence = DOTween.Sequence().SetLink(gameObject, LinkBehaviour.KillOnDisable);
            sequence.Append(DOTween.To(() => swing, t =>
            {
                swing = t;
                if (generation != m_generation || m_stopping) return;
                PlaceHammer(hammer, high + Vector3.up * cell * t * .15f, -65 - t * 12, length * (.6f + .4f * t));
                hammer.SetFloat(EffectQuad.AlphaId, Mathf.Clamp01(t * 3));
            }, 1, charge).SetEase(Ease.OutQuad));
            float fall = 0;
            sequence.AppendCallback(() => Arc(target + Vector3.up * cell * .45f, cell * 1.65f, 55, .19f, Gold));
            sequence.Append(DOTween.To(() => fall, t =>
            {
                fall = t;
                if (generation != m_generation || m_stopping) return;
                PlaceHammer(hammer, Vector3.Lerp(high + Vector3.up * cell * .15f, contact, t),
                    Mathf.Lerp(-77, 20, t), length);
            }, 1, .17f).SetEase(Ease.InCubic));
            sequence.AppendCallback(() => hit());
            sequence.AppendInterval(.05f);
            float recoil = 0;
            sequence.Append(DOTween.To(() => recoil, t =>
            {
                recoil = t;
                if (generation != m_generation || m_stopping) return;
                PlaceHammer(hammer, contact + Vector3.up * cell * .17f * Mathf.Sin(t * Mathf.PI),
                    20 - t * 12, length * (1 - t * .10f));
                hammer.SetFloat(EffectQuad.AlphaId, 1 - t * t);
            }, 1, .27f).SetEase(Ease.Linear));
            Track(sequence, () => Release(hammer));
        }

        private static void PlaceHammer(EffectQuad hammer, Vector3 head, float angle, float length)
        {
            Quaternion rotation = Quaternion.Euler(0, 0, angle);
            hammer.Transform.position = head - rotation * (Vector3.up * length * .26f);
            hammer.Transform.rotation = rotation;
            hammer.Transform.localScale = new Vector3(length * .82f, length, 1);
        }

        private void Impact(Vector3 point, float area, float cell, float angle, int seed)
        {
            bool burst = m_attack == Attack.BurstShot;
            bool heavy = m_attack == Attack.HeavySmash;
            bool bash = m_attack == Attack.ShieldBash;
            Color color = burst ? Amber : heavy ? Gold : Silver;
            float size = burst || heavy || bash ? area : cell * .65f;
            if (burst || heavy || bash) ScreenShake.Play(heavy ? .045f : .025f, .10f);
            Pop(m_flare, point, cell * .70f, .15f, color, 1.3f);
            Halo(point, size * .90f, .24f, color, .65f);
            if (burst)
            {
                Pop(m_burst, point, size * .95f, .26f, Amber, 1.5f);
                Ring(point, size * 1.3f, .32f, Gold);
                Ring(point, size * .85f, .20f, Silver);
                for (int i = 0; i < 7; i++)
                    Puff(point, Direction(i * 137.5f + seed) * size * .30f + Vector3.up * cell * .16f,
                        cell * .48f, .42f, Dust);
                Debris(point, size * .48f, cell, 7, seed);
                Sparks(point, size * .5f, cell, 12, seed, Gold);
            }
            else if (heavy)
            {
                Ring(point, size * 1.25f, .34f, Gold);
                Ring(point, size * .80f, .22f, Silver);
                Crack(point, size, .48f, seed);
                for (int i = 0; i < 8; i++)
                    Puff(point + Direction(i * 45) * cell * .15f, Direction(i * 45) * size * .35f,
                        cell * .58f, .46f, Dust);
                Debris(point, size * .45f, cell, 10, seed);
                Sparks(point, size * .4f, cell, 9, seed, Gold);
            }
            else if (bash)
            {
                Vector3 forward = Direction(angle);
                Arc(point, size, angle, .24f, Silver);
                Arc(point + forward * cell * .16f, size * .75f, angle, .29f, Gold);
                Ring(point, size, .28f, Steel);
                Streak(point + forward * size * .24f, angle, size * .85f, cell * .18f, .22f, Silver);
                Sparks(point, size * .40f, cell, 9, seed, Silver);
                for (int i = 0; i < 4; i++)
                    Puff(point, Direction(angle - 50 + i * 33) * size * .36f, cell * .40f, .34f, Dust);
            }
            else if (m_attack == Attack.Javelin)
            {
                Streak(point + Direction(angle) * cell * .25f, angle, cell, cell * .13f, .17f, Silver);
                Streak(point, angle + 90, cell * .38f, cell * .08f, .15f, Silver);
                Sparks(point, cell * .36f, cell, 6, seed, Silver);
            }
            else
            {
                Ring(point, cell * .78f, .23f, color);
                Shape(m_light, point, new Vector3(cell * .7f, cell * .7f, 1), angle, 4, .19f, color, Gold,
                    (q, t) => q.SetFloat(EffectQuad.AlphaId, 1 - t));
                Sparks(point, cell * .42f, cell, 8, seed, m_attack == Attack.ShieldToss ? Silver : Gold);
                if (m_attack == Attack.ThrowingHammer)
                {
                    Puff(point, Vector3.up * cell * .1f, cell * .46f, .30f, Dust);
                    Streak(point, angle + 40, cell * .5f, cell * .12f, .17f, Gold);
                }
            }
        }

        private void Crack(Vector3 point, float size, float duration, int seed)
        {
            Shape(m_solid, point, new Vector3(size * 1.1f, size * .70f, 1), 0, 3, duration,
                new Color(.12f, .09f, .065f), Gold, (q, t) =>
                {
                    q.SetFloat(EffectQuad.PhaseId, seed);
                    q.SetFloat(EffectQuad.ProgressId, Out(Mathf.Min(1, t * 3)));
                    q.SetFloat(EffectQuad.AlphaId, Mathf.Clamp01((1 - t) * 3));
                });
        }
        private void Arc(Vector3 point, float size, float angle, float duration, Color color)
        {
            Shape(m_light, point, new Vector3(size, size * .78f, 1), angle, 5, duration, color, Silver, (q, t) =>
            {
                q.Transform.localScale = new Vector3(size * (.65f + t * .55f), size * (.5f + t * .43f), 1);
                q.SetFloat(EffectQuad.AlphaId, 1 - t * t);
            });
        }
        private void Ghost(EffectQuadPool pool, EffectQuad source, int shape, float alpha)
        {
            EffectQuad q = Acquire(pool);
            q.Transform.SetPositionAndRotation(source.Transform.position, source.Transform.rotation);
            q.Transform.localScale = source.Transform.localScale;
            q.SetColor(new Color(Steel.r, Steel.g, Steel.b, alpha));
            q.SetColorById(EffectQuad.Color2Id, Silver);
            q.SetFloat(EffectQuad.ShapeId, shape);
            q.SetSortingOffset(-2);
            Animate(q, .12f, (quad, t) => quad.SetFloat(EffectQuad.AlphaId, 1 - t));
        }
        private void Debris(Vector3 point, float radius, float cell, int count, int seed)
        {
            for (int i = 0; i < count; i++)
            {
                Vector3 drift = Direction(i * 137.5f + seed) * radius * (.6f + Hash(i + seed) * .4f);
                float size = cell * (.10f + Hash(i) * .10f);
                Shape(m_projectile, point, Vector3.one * size, i * 47, 7, .38f + Hash(i) * .12f,
                    Dust, Silver, (q, t) =>
                    {
                        q.Transform.position = point + drift * Out(t) + Vector3.up * cell * (.24f * Mathf.Sin(t * Mathf.PI) - .25f * t * t);
                        q.SetFloat(EffectQuad.AlphaId, 1 - t * t);
                    });
            }
        }
        private void Sparks(Vector3 point, float radius, float cell, int count, int seed, Color color)
        {
            for (int i = 0; i < count; i++)
                Spark(point, Direction(i * 137.5f + seed * 17) * radius, cell * (.06f + Hash(seed + i) * .055f),
                    .18f + Hash(i) * .14f, color, i * 31);
        }
        private void Spark(Vector3 point, Vector3 drift, float size, float duration, Color color, float angle)
        {
            Sprite(m_spark, point, color, duration, (q, t) =>
            {
                q.Transform.position = point + drift * Out(t);
                q.Transform.rotation = Quaternion.Euler(0, 0, angle);
                q.Transform.localScale = new Vector3(size * (1 - t * .5f), size * 2.1f * (1 - t * .5f), 1);
                q.SetFloat(EffectQuad.AlphaId, 1 - t * t);
            });
        }
        private void Streak(Vector3 point, float angle, float length, float width, float duration, Color color)
        {
            Sprite(m_trail, point, color, duration, (q, t) =>
            {
                q.Transform.rotation = Quaternion.Euler(0, 0, angle);
                q.Transform.localScale = new Vector3(length * (.65f + .35f * Out(t)), width, 1);
                q.SetFloat(EffectQuad.AlphaId, 1 - t);
            });
        }
        private void Ring(Vector3 point, float size, float duration, Color color)
        {
            Sprite(m_ring, point, color, duration, (q, t) =>
            {
                float diameter = size * (.18f + Out(t));
                q.Transform.localScale = new Vector3(diameter, diameter * .64f, 1);
                q.SetFloat(EffectQuad.AlphaId, (1 - t) * .85f);
            });
        }
        private void Halo(Vector3 point, float size, float duration, Color color, float alpha)
        {
            Sprite(m_glow, point, color, duration, (q, t) =>
            {
                q.Transform.localScale = Vector3.one * size * (.45f + t * .55f);
                q.SetFloat(EffectQuad.AlphaId, alpha * (1 - t));
            });
        }
        private void Pop(EffectQuadPool pool, Vector3 point, float size, float duration, Color color, float intensity)
        {
            Sprite(pool, point, color, duration, (q, t) =>
            {
                q.Transform.localScale = Vector3.one * size * (.25f + .75f * Out(Mathf.Min(1, t * 4)));
                q.SetFloat(EffectQuad.AlphaId, 1 - t * t);
                q.SetFloat(EffectQuad.IntensityId, intensity);
            });
        }
        private void Puff(Vector3 point, Vector3 drift, float size, float duration, Color color)
        {
            Sprite(m_smoke, point, color, duration, (q, t) =>
            {
                q.Transform.position = point + drift * Out(t);
                q.Transform.localScale = Vector3.one * size * (.5f + t * .8f);
                q.SetFloat(EffectQuad.AlphaId, Mathf.Sin(t * Mathf.PI) * .7f);
            });
        }

        private EffectQuad Acquire(EffectQuadPool pool)
        {
            EffectQuad quad = pool.Get();
            m_leases.Add(quad, pool);
            return quad;
        }
        private void Release(EffectQuad quad)
        {
            if (!m_leases.TryGetValue(quad, out var pool)) return;
            m_leases.Remove(quad);
            pool.Release(quad);
        }
        private void Shape(EffectQuadPool pool, Vector3 point, Vector3 scale, float angle, int shape, float duration,
            Color body, Color edge, Action<EffectQuad, float> update)
        {
            EffectQuad q = Acquire(pool);
            q.Transform.position = point;
            q.Transform.localScale = scale;
            q.Transform.rotation = Quaternion.Euler(0, 0, angle);
            q.SetColor(body);
            q.SetColorById(EffectQuad.Color2Id, edge);
            q.SetFloat(EffectQuad.ShapeId, shape);
            Animate(q, duration, update);
        }
        private void Sprite(EffectQuadPool pool, Vector3 point, Color color, float duration, Action<EffectQuad, float> update)
        {
            EffectQuad q = Acquire(pool);
            q.Transform.position = point;
            q.SetColor(color);
            q.SetFloat(EffectQuad.IntensityId, 1.15f);
            Animate(q, duration, update);
        }
        private void Animate(EffectQuad quad, float duration, Action<EffectQuad, float> update)
        {
            float time = 0;
            int generation = m_generation;
            update(quad, 0);
            Track(DOTween.To(() => time, t =>
            {
                time = t;
                if (generation == m_generation && !m_stopping) update(quad, t);
            }, 1, duration).SetEase(Ease.Linear).SetLink(gameObject, LinkBehaviour.KillOnDisable), () => Release(quad));
        }
        private void Track(Tween tween, Action cleanup)
        {
            m_tweens.Add(tween);
            tween.OnKill(() => { cleanup(); m_tweens.Remove(tween); });
        }
        private static float Angle(Vector3 v) => Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg;
        private static Vector3 Direction(float angle) => new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad), 0);
        private static float Out(float t) => 1 - (1 - t) * (1 - t);
        private static float Hash(int n) => Mathf.Repeat(Mathf.Sin(n * 127.1f + 31.7f) * 43758.5453f, 1);
        private void OnEnable() => m_stopping = false;
        private void OnDisable()
        {
            m_stopping = true;
            m_generation++;
            foreach (Tween tween in new List<Tween>(m_tweens)) tween.Kill();
            m_tweens.Clear();
            foreach (EffectQuad quad in new List<EffectQuad>(m_leases.Keys)) Release(quad);
        }
        private void OnDestroy()
        {
            foreach (var pool in new[] { m_solid, m_light, m_projectile, m_trail, m_glow, m_ring, m_flare, m_smoke, m_spark, m_burst })
                pool?.Dispose();
        }
    }
}
