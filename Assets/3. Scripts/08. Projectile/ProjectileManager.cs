using System;
using System.Collections.Generic;
using UnityEngine;

namespace SquareTD
{
    // 기본 공격 탄의 발사 (코드로 그리는 탄 연출 표)
    public class ProjectileManager : Singleton<ProjectileManager>, IProjectileLauncher
    {
        private Dictionary<int, IShotEffect> m_shotEffects;

        public int FlyingCount { get; private set; }

        public static int GetLookId(int towerId) => towerId % 1000;

        protected override void Awake()
        {
            base.Awake();
            if (Instance != this) return;

            m_shotEffects = ShotEffectTable.Create(transform);
        }

        public void Fire(int lookId, Vector3 from, Vector3 to, float speed, float hold, float effectScale, Action onArrived)
        {
            if (m_shotEffects == null || !m_shotEffects.TryGetValue(lookId, out IShotEffect shot))
            {
                Debug.LogWarning($"[ProjectileManager] 탄 연출 {lookId}가 없어 탄 없이 바로 맞힘");
                onArrived?.Invoke();
                return;
            }

            FlyingCount++;
            shot.FireShot(from, to, speed, hold, effectScale * EffectSpace.CellWorldSize(), () =>
            {
                FlyingCount--;
                onArrived?.Invoke();
            });
        }
    }
}
