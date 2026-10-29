using UnityEngine;

namespace SquareTD
{
    // 파티클 시스템을 코드로 만들어 그림을 입힘
    public sealed class EffectParticles
    {
        private readonly ParticleSystem m_system;
        private readonly System.Random m_random = new System.Random();

        public bool IsReady => m_system != null;

        public EffectParticles(Transform parent, string name, string texture, bool additive, float intensity,
            int sortingOrder, bool stretch, float gravity, bool spin)
        {
            Shader shader = Resources.Load<Shader>("EffectSprite");
            if (shader == null || !shader.isSupported) return;

            var gameObject = new GameObject(name);
            gameObject.transform.SetParent(parent, false);
            m_system = gameObject.AddComponent<ParticleSystem>();
            m_system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = m_system.main;
            main.playOnAwake = false;
            main.loop = false;
            main.maxParticles = 600;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = gravity;
            main.startRotation3D = false;

            ParticleSystem.EmissionModule emission = m_system.emission;
            emission.enabled = false;
            ParticleSystem.ShapeModule shape = m_system.shape;
            shape.enabled = false;

            ParticleSystem.ColorOverLifetimeModule fade = m_system.colorOverLifetime;
            fade.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.6f), new GradientAlphaKey(0f, 1f) });
            fade.color = gradient;

            ParticleSystem.SizeOverLifetimeModule size = m_system.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, stretch ? 0.3f : 0.8f));

            if (spin)
            {
                ParticleSystem.NoiseModule noise = m_system.noise;
                noise.enabled = true;
                noise.strength = 0.6f;
                noise.frequency = 0.8f;
            }

            var renderer = gameObject.GetComponent<ParticleSystemRenderer>();
            var material = new Material(shader) { name = $"{name} (Runtime)" };
            material.SetTexture("_MainTex", EffectTextures.Get(texture));
            material.SetTexture("_NoiseTex", EffectTextures.Get(EffectTextures.Noise));
            material.SetFloat("_DstBlend", additive ? 1f : 10f);
            material.SetFloat("_Intensity", intensity);
            renderer.sharedMaterial = material;
            renderer.renderMode = stretch ? ParticleSystemRenderMode.Stretch : ParticleSystemRenderMode.Billboard;
            renderer.velocityScale = stretch ? 0.06f : 0f;
            renderer.lengthScale = stretch ? 1.5f : 1f;
            renderer.sortingLayerName = "Projectile";
            renderer.sortingOrder = sortingOrder;
            renderer.alignment = ParticleSystemRenderSpace.View;
            m_system.Play();
            EffectActivity.Register(m_system);
        }

        public void Burst(Vector3 center, int count, Color color, float speedMin, float speedMax, float sizeMin, float sizeMax,
            float lifeMin, float lifeMax, Vector2 direction = default, float spreadDegrees = 360f, float spawnRadius = 0f)
        {
            if (m_system == null) return;
            float baseAngle = direction.sqrMagnitude > 0.0001f ? Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg : 0f;
            for (int i = 0; i < count; i++)
            {
                float angle = (baseAngle + Range(-spreadDegrees * 0.5f, spreadDegrees * 0.5f)) * Mathf.Deg2Rad;
                Vector3 velocity = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * Range(speedMin, speedMax);
                var emit = new ParticleSystem.EmitParams
                {
                    position = center + InCircle(spawnRadius),
                    velocity = velocity,
                    startSize = Range(sizeMin, sizeMax),
                    startLifetime = Range(lifeMin, lifeMax),
                    startColor = color,
                    rotation = Range(0f, 360f),
                    angularVelocity = Range(-240f, 240f),
                };
                m_system.Emit(emit, 1);
            }
        }

        private float Range(float min, float max) => Mathf.Lerp(min, max, (float)m_random.NextDouble());

        private Vector3 InCircle(float radius)
        {
            float angle = Range(0f, Mathf.PI * 2f);
            float distance = Mathf.Sqrt((float)m_random.NextDouble()) * radius;
            return new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * distance;
        }

        public void Gather(Vector3 center, int count, Color color, float radius, float size, float life)
        {
            if (m_system == null) return;
            for (int i = 0; i < count; i++)
            {
                float angle = Range(0f, Mathf.PI * 2f);
                Vector3 from = center + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * (radius * Range(0.7f, 1f));
                var emit = new ParticleSystem.EmitParams
                {
                    position = from,
                    velocity = (center - from) / life,
                    startSize = size * Range(0.7f, 1.2f),
                    startLifetime = life,
                    startColor = color,
                };
                m_system.Emit(emit, 1);
            }
        }
    }
}
