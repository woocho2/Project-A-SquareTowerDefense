using UnityEngine;

public class EnemyMovementController : MonoBehaviour
{
    [SerializeField] private LayerMask m_endLayer;

    private Transform m_target;
    private int m_wavePointIndex = 0;
    private float m_baseSpeed = 2f;
    private float m_speedMultiplier = 1f;
    private float m_synergySlow = 1f;

    private EnemyHealthController m_health;

    public float CurrentSpeed => m_baseSpeed * Mathf.Clamp(m_speedMultiplier * m_synergySlow, 0f, 10f);
    public int WavePointIndex => m_wavePointIndex;

    private void Awake()
    {
        m_health = GetComponent<EnemyHealthController>();
    }

    private void Start()
    {
        InitWaypoint();
    }

    private void Update()
    {
        if (m_target == null || CurrentSpeed <= 0f) return;

        Vector3 dir = m_target.position - transform.position;
        dir.z = 0f;
        float distanceToTarget = dir.magnitude;

        float moveDistance = CurrentSpeed * Time.deltaTime;

        if (moveDistance >= distanceToTarget)
        {
            transform.position = m_target.position;
            GetNextWaypoint();
        }
        else
        {
            transform.Translate(dir.normalized * moveDistance, Space.World);
        }
    }

    public void InitMovement(float baseSpeed)
    {
        m_baseSpeed = baseSpeed;
        m_speedMultiplier = 1f;
        m_synergySlow = 1f;
        m_wavePointIndex = 0;

        InitWaypoint();
    }

    private void InitWaypoint()
    {
        if (WayPointController.points != null && WayPointController.points.Length > 0)
        {
            m_target = WayPointController.points[0];
        }
    }

    private void GetNextWaypoint()
    {
        if (WayPointController.points == null) return;

        if (m_wavePointIndex >= WayPointController.points.Length - 1)
        {
            OnReachEnd();
            return;
        }

        m_wavePointIndex++;
        m_target = WayPointController.points[m_wavePointIndex];
    }

    public void OnReachEnd()
    {
        UIManager.Instance?.OnPlayerHit(m_health != null && m_health.IsBoss);
        m_health?.ReturnToPool();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (CommonUtil.ContainsLayer(m_endLayer, collision.gameObject.layer))
        {
            OnReachEnd();
        }
    }

    public void SetSpeedMultiplier(float multiplier) => m_speedMultiplier = multiplier;
    public void SetSynergySlow(float slow) => m_synergySlow = slow;

    public void MoveBackward(float step)
    {
        Vector3 backwardTarget = (m_wavePointIndex > 0)
            ? WayPointController.points[m_wavePointIndex - 1].position
            : WayPointController.points[0].position;

        float distance = Vector3.Distance(transform.position, backwardTarget);

        if (distance <= step)
        {
            transform.position = backwardTarget;
            if (m_wavePointIndex > 0)
            {
                m_wavePointIndex--;
                m_target = WayPointController.points[m_wavePointIndex];
            }
        }
        else
        {
            Vector3 pushDirection = (backwardTarget - transform.position).normalized;
            transform.position += pushDirection * step;
        }
    }
}