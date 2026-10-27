using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SquareTD
{
    // "아직 연출이 돌고 있나?" 를 한곳에서 봄 (2026-10-27 사용자: 연출이 끝나야 다음 타워·다음 턴)
    //   켜진 연출 판(EffectQuadPool), 살아 있는 파티클(EffectParticles), 판 없이 도는 연출이 걸어 둔 시간(HoldFor)
    //   연출 쪽이 알아서 등록하므로 새 연출도 EffectQuadPool·EffectParticles만 쓰면 따로 할 일 없음
    //   규칙 쪽은 이것을 모름: TurnManager가 WaitUntilIdle을 TowerTurnRunner에 넘겨 줌
    public static class EffectActivity
    {
        private const float IdleGrace = 0.1f;       // 연출 사이의 아주 짧은 틈(DelayedCall 뒤에 판을 꺼냄)에 넘어가지 않게
        private const float MaxWait = 6f;           // 판을 안 돌려놓는 실수가 있어도 게임이 멈추지 않게

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

        // 판 없이 도는 연출(화면 갈라짐 덮개 등)은 끝날 때까지의 시간을 직접 걸어 둠. 게임 시간(배속)을 따름
        public static void HoldFor(float seconds) => s_holdUntil = Mathf.Max(s_holdUntil, Time.time + seconds);

        public static bool IsBusy
        {
            get
            {
                if (Time.time < s_holdUntil) return true;

                s_gone.Clear();
                foreach (GameObject quad in s_quads)
                {
                    if (quad == null || !quad.activeInHierarchy) s_gone.Add(quad);     // 씬이 바뀌어 사라진 판은 빼 둠
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

        // 연출이 IdleGrace 동안 이어서 조용해질 때까지 기다림
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
