using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SquareTD
{
    // 연출이 아직 돌고 있는지 한곳에서 봄
    public static class EffectActivity
    {
        private const float IdleGrace = 0.1f;
        private const float MaxWait = 6f;

        private static readonly HashSet<GameObject> s_quads = new HashSet<GameObject>();
        private static readonly List<ParticleSystem> s_particles = new List<ParticleSystem>();
        private static readonly List<GameObject> s_gone = new List<GameObject>();
        private static float s_holdUntil;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay()
        {
            s_quads.Clear();
            s_particles.Clear();
            s_holdUntil = 0f;
        }

        public static void QuadOn(GameObject quad) => s_quads.Add(quad);
        public static void QuadOff(GameObject quad) => s_quads.Remove(quad);
        public static void Register(ParticleSystem system) => s_particles.Add(system);

        public static void HoldFor(float seconds) => s_holdUntil = Mathf.Max(s_holdUntil, Time.time + seconds);

        public static bool IsBusy
        {
            get
            {
                if (Time.time < s_holdUntil) return true;

                s_gone.Clear();
                foreach (GameObject quad in s_quads)
                {
                    if (quad == null || !quad.activeInHierarchy) s_gone.Add(quad);
                }
                foreach (GameObject quad in s_gone) s_quads.Remove(quad);
                if (s_quads.Count > 0) return true;

                s_particles.RemoveAll(system => system == null);
                foreach (ParticleSystem system in s_particles)
                {
                    if (system.particleCount > 0) return true;
                }
                return false;
            }
        }

        public static IEnumerator WaitUntilIdle()
        {
            float idle = 0f;
            float waited = 0f;
            while (idle < IdleGrace)
            {
                if (waited >= MaxWait)
                {
                    Debug.LogWarning($"[EffectActivity] 연출이 {MaxWait}초 넘게 끝나지 않아 기다리지 않고 넘어감 (돌려놓지 않은 판이 있는지 확인)");
                    yield break;
                }

                idle = IsBusy ? 0f : idle + Time.deltaTime;
                waited += Time.deltaTime;
                yield return null;
            }
        }
    }
}
