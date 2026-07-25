using UnityEngine;

public class BossController : EnemyController
{

    protected override void Awake()
    {
        base.Awake();

        m_isBoss = true;
    }
}
