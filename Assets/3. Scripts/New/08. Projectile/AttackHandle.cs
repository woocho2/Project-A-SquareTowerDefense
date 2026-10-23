using UnityEngine;

namespace SquareTD
{
    public class AttackHandle
    {
        public int PendingShots { get; private set; }
        public bool IsDone => PendingShots == 0;

        public void AddShot()
        {
            PendingShots++;
        }

        public void CompleteShot()
        {
            if (PendingShots <= 0)
            {
                Debug.LogWarning("[AttackHandle] 쏜 탄보다 도착한 탄이 많음");
                return;
            }
            
            PendingShots--;
        }

        public void ForceComplete()
        {
            if (PendingShots > 0)
            {
                Debug.LogWarning($"[AttackHandle] 도착하지 못한 탄 {PendingShots}발을 끝난 것으로 침");
            }

            PendingShots = 0;
        }
    }
}
