using UnityEngine;
using UnityEngine.AI;

public class SC_Car_npc : SC_PhysicObject
{
    private NavMeshAgent m_agent;

    private float m_accelerating_speed = 0;

    [Header("AI")]
    public Transform target;

    private float m_timer_reaction = 0;
    public float reaction_delay = 0.15f;
    private float m_cached_turn = 0;

    public float noise_scale = 1f;
    public float m_noise_seed = 0;

    public float overturn = 1;

    public Transform next_point;
    protected override void Start()
    {
        base.Start();
        m_agent = GetComponent<NavMeshAgent>();

        m_agent.updatePosition = false;
        m_agent.updateRotation = false;

        m_noise_seed = Random.Range(0, 10);

        reaction_delay = Random.Range(reaction_delay - 0.015f, reaction_delay + 0.015f);
        if (reaction_delay < 0) reaction_delay = 0.1f;

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
        m_agent.nextPosition = Current_Position;
    }
    private void HandleNavigation()
    {
        if (m_agent.pathPending || !onGround) return; // Exits if there itsn't a path or the car isn't on the ground

        Vector3 desiredDir = GetLookAheadDir();
        desiredDir.y = 0;
        if (desiredDir.sqrMagnitude < 0.001f)
        {
            rotation.y = 0; // no path direction, stop turning
            return;
        }

        desiredDir.Normalize();

        float angle = Vector3.SignedAngle(Forward, desiredDir, Vector3.up);
        float turn_severity = Mathf.Clamp01(Mathf.Abs(angle) / 90f); // 0 straight >1 heavy turn
        float target_turn = Mathf.Abs(angle) > 1 ? Mathf.Clamp(angle / 45f, -1f, 1f) + (turn_severity * overturn) : 0;

        m_timer_reaction += Time.fixedDeltaTime;
        if (m_timer_reaction >= reaction_delay)
        {
            m_cached_turn = target_turn;
            m_timer_reaction = 0;
        }

        float current_normalized = rotation.y / max_rotation.y;
        float speed_turn_factor = Mathf.Abs(Vector3.Dot(velocity, Forward));
        float turn = Mathf.Lerp(current_normalized, m_cached_turn, Time.fixedDeltaTime * rotation_speed * speed_turn_factor);
        rotation.y = turn * max_rotation.y;

        float speed_factor = Mathf.Clamp(1 - turn_severity, 0.1f, 1);
        float move_towards_speed = 0;
        if (m_agent.remainingDistance > m_agent.stoppingDistance)
        {
            velocity += Forward * m_accelerating_speed * Time.fixedDeltaTime;

            move_towards_speed = speed * speed_factor;
        }

        m_accelerating_speed = Mathf.MoveTowards(m_accelerating_speed, move_towards_speed, acceleration * Time.fixedDeltaTime);
    }

    private Vector3 GetLookAheadPoint(float lookAheadDistance)
    {
        Vector3[] corners = m_agent.path.corners;

        float remaining = lookAheadDistance;
        Vector3 start = Current_Position;

        for (int i = 0; i < corners.Length - 1; i++)
        {
            Vector3 end = corners[i + 1];
            float segment_length = Vector3.Distance(start, end);

            if (segment_length >= remaining)
            {
                return Vector3.Lerp(start, end, segment_length > 0.001f ? remaining / segment_length : 0);
            }

            remaining -= segment_length;
            start = end;
        }

        return corners[corners.Length - 1];
    }

    private Vector3 GetLookAheadDir()
    {
        float look_ahead_distance = Mathf.Clamp(velocity.magnitude, 2, 5);

        next_point.position = m_agent.destination;
        if (m_agent.path.corners.Length < 2)
        {
            return m_agent.desiredVelocity;
        }

        Vector3 lookTarget = GetLookAheadPoint(look_ahead_distance);

        Vector3 path_dir = (lookTarget - Current_Position).normalized;
        Vector3 cross = Vector3.Cross(Vector3.up, path_dir);
        float wander = (Mathf.PerlinNoise(Time.time * 0.05f, m_noise_seed) - 0.5f) * noise_scale;
        path_dir += cross * wander;

        Vector3 normalized = path_dir.normalized;
        return normalized;
    }

    public void SetDestination(Vector3 destination)
    {
        m_agent.SetDestination(destination);
    }

    protected override void RestartEvent()
    {
        base.RestartEvent();

        if (NavMesh.SamplePosition(m_start_position, out NavMeshHit navHit, 2f, NavMesh.AllAreas))
            m_agent.Warp(navHit.position);
        else
            m_agent.Warp(m_start_position);

        m_accelerating_speed = 0;
        SetDestination(SC_CheckpointManager.instance.GetNextCheckpointPosition(0));
    }

    private void OnDrawGizmos()
    {
        if (m_agent != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(Current_Position, m_agent.destination);
        }
    }
}