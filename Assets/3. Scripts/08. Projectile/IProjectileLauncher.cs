using System;
using UnityEngine;

namespace SquareTD
{
    public interface IProjectileLauncher
    {
        void Fire(int lookId, Vector3 from, Vector3 to, float speed, float hold, float effectScale ,Action onArrived);
    }
}
