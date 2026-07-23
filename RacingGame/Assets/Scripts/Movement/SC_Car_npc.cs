using UnityEngine;
using UnityEngine.AI;

public class SC_Car_npc : SC_PhysicObject
{
    private NavMeshAgent m_agent;

    [Header("AI")]
    public Transform target;

    private float m_timer_reaction = 0;
    public float reaction_delay = 0.15f;
    private float m_cached_turn = 0;

    public float noise_scale = 1f;
    public float m_noise_seed = 0;

    public Transform next_point;
    protected override void Start()
    {
        base.Start();
        m_agent = GetComponent<NavMeshAgent>();

        m_agent.updatePosition = false;
        m_agent.updateRotation = false;

        m_noise_seed = Random.Range(0, 10);

        SetDestination(SC_CheckpointManager.instance.GetNextCheckpointPosition(0));
    }

    protected override void FixedUpdate()
    {
        SyncAgentPosition();
        HandleNavigation();
        base.FixedUpdate();
    }

    private void SyncAgentPosition()
    {
        m_agent.nextPosition = transform.position;
    }
    private void HandleNavigation()
    {
        if (m_agent.pathPending) return; // Exits if there itsn't a path

        Vector3 desiredDir = GetLookAheadDir();
        desiredDir.y = 0;
        Debug.Log(desiredDir);
        if (desiredDir.sqrMagnitude < 0.001f)
        {
            rotation.y = 0; // no path direction, stop turning
            return;
        }

        desiredDir.Normalize();

        float angle = Vector3.SignedAngle(transform.forward, desiredDir, Vector3.up);
        float target_turn = Mathf.Clamp(angle / 45f, -1f, 1f);

        m_timer_reaction += Time.fixedDeltaTime;
        if (m_timer_reaction >= reaction_delay)
        {
            m_cached_turn = target_turn;
            m_timer_reaction = 0;
        }

        float current_normalized = rotation.y / max_rotation.y;
        float turn = Mathf.Lerp(current_normalized, m_cached_turn, Time.fixedDeltaTime * rotation_speed);
        float noise = (Mathf.PerlinNoise(Time.time * 0.5f, m_noise_seed) - 0.5f) * noise_scale;
        rotation.y = (turn + noise) * max_rotation.y;


        float turn_severity = Mathf.Abs(angle) / 90f; // 0 straight >1 heavy turn
        float speed_factor = Mathf.Clamp(1 - turn_severity, 0.75f, 1);
        if (m_agent.remainingDistance > m_agent.stoppingDistance && onGround)
        {
            velocity += transform.forward * acceleration * Time.fixedDeltaTime * speed_factor;
        }
    }

    private Vector3 GetLookAheadDir()
    {
        next_point.position = m_agent.destination;
        if (m_agent.path.corners.Length < 2)
        {
            return m_agent.desiredVelocity;
        }

        Vector3 lookTarget = m_agent.path.corners[m_agent.path.corners.Length - 1];
        for (int i = 1; i <  m_agent.path.corners.Length; i++)
        {
            if (Vector3.Distance(transform.position, m_agent.path.corners[i]) > 2)
            {
                lookTarget = m_agent.path.corners[i];
                break;
            }
        }
        return (lookTarget - transform.position).normalized;
    }

    public void SetDestination(Vector3 destination)
    {
        m_agent.SetDestination(destination);
    }
}