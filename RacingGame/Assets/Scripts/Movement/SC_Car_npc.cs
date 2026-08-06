using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Windows;

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

    [Header("Stuck Detection")]
    public float stuck_check_interval = 0.5f;   // how often we sample position
    public float stuck_move_threshold = 0.5f;   // min distance expected to move in that interval if not stuck
    public float stuck_time_threshold = 1.5f;   // how long displacement must stay below threshold before we call it "stuck"
    public float reverse_duration = 1f;
    public float reverse_speed = 6f;

    private enum DriveState { Driving, Reversing }
    private DriveState m_state = DriveState.Driving;

    private Vector3 m_stuck_check_pos;
    private float m_stuck_check_timer = 0f;
    private float m_low_progress_timer = 0f;
    private float m_reverse_timer = 0f;
    private float m_reverse_turn = 0f;

    [Header("Steering Lookahead")]
    public float steer_lookahead_min = 4f;
    public float steer_lookahead_max = 18f;
    public float steer_lookahead_speed_mult = 0.8f;
    public float corner_anticipation_distance = 12f; // how far past the immediate point to peek for early turn-in
    [Range(0f, 1f)]
    public float corner_anticipation_weight = 0.5f;  // 0 = ignore upcoming corner, 1 = fully steer toward it early

    [Header("Corner Anticipation (Speed)")]
    public float corner_lookahead_distance = 30f;   // base distance to scan for turns, auto-extended at high speed
    public float min_corner_speed = 3f;             // speed floor for very sharp corners
    public float corner_deceleration = 15f;         // assumed braking capacity, tune to your friction/mass

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

        m_stuck_check_pos = Current_Position;
    }

    protected override void FixedUpdate()
    {
        SyncAgentPosition();

        if (m_state == DriveState.Reversing)
            HandleReversing();
        else
            HandleNavigation();

        UpdateStuckDetection();

        base.FixedUpdate();
    }

    private void SyncAgentPosition()
    {
        m_agent.nextPosition = Current_Position;
    }

    private void UpdateStuckDetection()
    {
        // Only evaluate stuck-ness while actively trying to drive somewhere
        bool trying_to_move = m_state == DriveState.Driving
                               && !m_agent.pathPending
                               && onGround
                               && m_agent.remainingDistance > m_agent.stoppingDistance;

        m_stuck_check_timer += Time.fixedDeltaTime;
        if (m_stuck_check_timer < stuck_check_interval) return;

        float moved = Vector3.Distance(Current_Position, m_stuck_check_pos);
        m_stuck_check_pos = Current_Position;
        m_stuck_check_timer = 0f;

        if (!trying_to_move)
        {
            m_low_progress_timer = 0f;
            return;
        }

        if (moved < stuck_move_threshold)
        {
            m_low_progress_timer += stuck_check_interval;
            if (m_low_progress_timer >= stuck_time_threshold)
            {
                BeginReverse();
            }
        }
        else
        {
            m_low_progress_timer = 0f;
        }
    }

    private void BeginReverse()
    {
        m_state = DriveState.Reversing;
        m_reverse_timer = 0f;
        m_low_progress_timer = 0f;
        m_accelerating_speed = 0f;

        // Steer away from whatever turn we were attempting, to help un-wedge from a corner
        m_reverse_turn = Mathf.Sign(m_cached_turn);
    }

    private void HandleReversing()
    {
        m_reverse_timer += Time.fixedDeltaTime;

        velocity += -Forward * reverse_speed * Time.fixedDeltaTime;
        rotation.y = m_reverse_turn * max_rotation.y * 0.25f;

        if (m_reverse_timer >= reverse_duration)
        {
            m_state = DriveState.Driving;
            m_stuck_check_pos = Current_Position; // reset baseline so we don't instantly re-trigger
        }
    }

    private void HandleNavigation()
    {
        if (m_agent.pathPending || !onGround) return; // Exits if there itsn't a path or the car isn't on the ground

        Vector3 desiredDir = GetLookAheadDir();
        desiredDir.y = 0;
        if (desiredDir.sqrMagnitude < 0.001f)
        {
            desiredDir = (m_agent.destination - Current_Position).normalized;
        }

        desiredDir.Normalize();

        float angle = Vector3.SignedAngle(Forward, desiredDir, Vector3.up);
        float turn_severity = Mathf.Clamp01(Mathf.Abs(angle) / 60f); // 0 straight >1 heavy turn
        float target_turn = Mathf.Abs(angle) > 1 ? Mathf.Clamp(angle / 45f, -1f, 1f) * (turn_severity * overturn) : 0;

        m_timer_reaction += Time.fixedDeltaTime;
        if (m_timer_reaction >= reaction_delay)
        {
            m_cached_turn = target_turn;
            m_timer_reaction = 0;
        }

        float current_normalized = rotation.y / max_rotation.y;
        float speed_turn_factor = Mathf.Abs(Vector3.Dot(velocity, Forward));
        speed_turn_factor = 1;
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
        float look_ahead_distance = Mathf.Clamp(
            velocity.magnitude * steer_lookahead_speed_mult,
            steer_lookahead_min,
            steer_lookahead_max
        );

        if (m_agent.path.corners.Length < 3)
        {
            return m_agent.desiredVelocity;
        }

        Vector3 lookTarget = GetLookAheadPoint(look_ahead_distance);
        Vector3 path_dir = (lookTarget - Current_Position).normalized;

        // Peek further down the path to anticipate the NEXT bend and start turning in early
        Vector3 futureTarget = GetLookAheadPoint(look_ahead_distance + corner_anticipation_distance);
        Vector3 future_dir = (futureTarget - lookTarget).normalized;

        if (future_dir.sqrMagnitude > 0.001f)
        {
            path_dir = Vector3.Slerp(path_dir, future_dir, corner_anticipation_weight).normalized;
        }

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
        m_state = DriveState.Driving;
        m_low_progress_timer = 0f;
        m_stuck_check_pos = Current_Position;
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