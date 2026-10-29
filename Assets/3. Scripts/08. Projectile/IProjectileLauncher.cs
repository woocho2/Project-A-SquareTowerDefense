using System;
using UnityEngine;

namespace SquareTD
{
    // 탄을 쏘는 쪽의 공통 모양
    public interface IProjectileLauncher
    {
        void Fire(int lookId, Vector3 from, Vector3 to, float speed, float hold, float effectScale ,Action onArrived);
    }
}
