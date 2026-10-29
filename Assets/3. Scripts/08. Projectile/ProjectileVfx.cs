using UnityEngine;

namespace SquareTD
{
    // 탄에 붙는 파티클
    public class ProjectileVfx : MonoBehaviour
    {
        [SerializeField] private TrailRenderer m_trail;
        [SerializeField] private ParticleSystem m_particles;

        public void SetColor(Color color)
        {
            m_trail.colorGradient = MakeTrailGradient(color);
            ParticleSystem.ColorOverLifetimeModule colorOverLifetime = m_particles.colorOverLifetime;
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(MakeParticleGradient(color));
        }

        public void Play()
        {
            m_trail.Clear();
            m_trail.emitting = true;
            m_particles.Clear();
            m_particles.Play();
        }

        public void Stop()
        {
            m_trail.emitting = false;
            m_trail.Clear();
            m_particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        private static Gradient MakeTrailGradient(Color mid)
        {
            Color head = Color.Lerp(mid, Color.white, 0.55f);
            Color tail = mid * 0.65f;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(head, 0f), new GradientColorKey(mid, 0.35f), new GradientColorKey(tail, 0.8f) },
                new[] { new GradientAlphaKey(0.95f, 0f), new GradientAlphaKey(0.85f, 0.25f), new GradientAlphaKey(0.4f, 0.65f), new GradientAlphaKey(0f, 1f) });
            return gradient;
        }

        private static Gradient MakeParticleGradient(Color color)
        {
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(color, 0.4f) },
                new[] { new GradientAlphaKey(0.9f, 0f), new GradientAlphaKey(0.6f, 0.5f), new GradientAlphaKey(0f, 1f) });
            return gradient;
        }
    }
}
