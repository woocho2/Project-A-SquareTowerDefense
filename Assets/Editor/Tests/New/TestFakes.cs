using System;
using System.Collections.Generic;
using UnityEngine;

namespace SquareTD.Tests
{
    // 여러 테스트가 같이 쓰는 가짜들 (TargetActionTest, SplashActionTest)

    // 맞으면 맞은 DamageInfo를 모아 두는 가짜 적. 점유에 들어가고, 죽으면 진짜 적처럼 점유에서 빠짐
    internal class OccupyingFakeTarget : ITarget
    {
        private readonly PathOccupancy m_occupancy;
        public readonly List<DamageInfo> Hits = new List<DamageInfo>();

        public OccupyingFakeTarget(PathOccupancy occupancy, int pathIndex, float hp, Transform parent)
        {
            m_occupancy = occupancy;
            PathIndex = pathIndex;
            CurrentHp = hp;
            MaxHp = hp;

            var go = new GameObject($"FakeTarget {pathIndex}");
            go.transform.SetParent(parent);
            go.transform.position = new Vector3(pathIndex, 0f, 0f);
            Transform = go.transform;

            m_occupancy.Add(this, pathIndex);
        }

        public Transform Transform { get; }
        public int PathIndex { get; }
        public bool IsAlive => CurrentHp > 0f;
        public bool IsBoss => false;
        public bool IsDummy => false;
        public EnemyController Body => null;
        public float CurrentHp { get; private set; }
        public float MaxHp { get; }
        public EnemyStats Stats => null;                                // 가짜 대상은 스탯 없이 받은 피해 그대로
        public EnemyStatusEffects Status => null;                       // 상태 효과도 없음

        public float PreviewDamage(DamageInfo info) => info.BaseDamage;

        public void TakeDamage(DamageInfo info)
        {
            if (!IsAlive) return;
            Hits.Add(info);
            CurrentHp -= PreviewDamage(info);
            if (!IsAlive) m_occupancy.Remove(this, PathIndex);
        }

        public void SetSortingOrder(int order) { }
    }

    // 쏘자마자 닿는 가짜 발사기 (탄이 날아가는 시간이 없음)
    internal class InstantLauncher : IProjectileLauncher
    {
        public void Fire(int lookId, Vector3 from, Vector3 to, float speed, float hold, float effectScale, Action onArrived) => onArrived?.Invoke();
    }

    // 쏜 탄을 모아 두었다가 ArriveAll을 부르면 그제야 전부 닿는 가짜 발사기 (탄이 날아가는 중을 흉내 냄)
    internal class DelayedLauncher : IProjectileLauncher
    {
        public readonly List<Action> Pending = new List<Action>();
        public readonly List<Vector3> Froms = new List<Vector3>();
        public readonly List<Vector3> Tos = new List<Vector3>();
        public readonly List<float> Holds = new List<float>();
        public int LastLookId;
        public Vector3 LastTo;
        public float LastSpeed;
        public float LastEffectScale;

        public void Fire(int lookId, Vector3 from, Vector3 to, float speed, float hold, float effectScale, Action onArrived)
        {
            LastLookId = lookId;
            LastTo = to;
            LastSpeed = speed;
            LastEffectScale = effectScale;
            Froms.Add(from);
            Tos.Add(to);
            Holds.Add(hold);
            Pending.Add(onArrived);
        }

        public void ArriveAll()
        {
            foreach (Action arrive in Pending) arrive?.Invoke();
            Pending.Clear();
        }
    }

    internal static class TestBoards
    {
        // BoardGridTest와 같은 판: 길 0번 (0,0), 1번 (1,0), 2번 (2,1), 3번 (1,1), 4번 (2,-1) / 스폰 0번 (0,1), 1번 (3,0), 2번 (1,-1)
        public static BoardGrid MakeGrid()
        {
            var path = new List<Vector3Int>
            {
                new Vector3Int(0, 0, 0),
                new Vector3Int(1, 0, 0),
                new Vector3Int(2, 1, 0),
                new Vector3Int(1, 1, 0),
                new Vector3Int(2, -1, 0),
            };
            var spawn = new List<Vector3Int>
            {
                new Vector3Int(0, 1, 0),
                new Vector3Int(3, 0, 0),
                new Vector3Int(1, -1, 0)
            };
            return new BoardGrid(path, spawn);
        }
    }
}
