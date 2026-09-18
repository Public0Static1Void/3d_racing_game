using System;
using UnityEngine;

public class SC_Car_npc_predictive : SC_PhysicObject
{
    public enum CarAction { DRIVING, OVERTAKING, DEFENDING }

    public SC_RacePoint next_point = null;
    private SC_RaceTrack m_track;

    [Header("AI")]
    public CarAction current_action = CarAction.DRIVING;
    public SC_NPC_DrivingProfile m_profile;

    public const int MAX_NEAR_CARS = 4;
    public SC_PhysicObject[] near_cars = new SC_PhysicObject[MAX_NEAR_CARS];

    public float pressure = 0;

    private float current_random_lateral_offset = 0;
    public float random_lateral_range = 1;

    public float current_speed = 0;
    public float current_rotation = 0;
    public float steering_smooth = 3;
    private float m_steering = 0;

    public float look_ahead = 0.5f;
    public float max_distance_look = 10;

    Vector3 future_point;
    private Vector3 m_decided_point;

    private float timer = 0;

    protected override void Start()
    {
        base.Start();

        m_track = SC_RaceTrack.instance;
    }

    protected override void Update()
    {
        if (!can_drive) return;
        base.Update();
    }

    protected override void FixedUpdate()
    {
        if (!can_drive) return;

        if (next_point != null)
        {
            HandleDriving(HandleAIDecisions());
        }
        base.FixedUpdate();
    }

    private Vector3 HandleAIDecisions()
    {
        if (near_cars[0] == null) return m_decided_point;

        // Only make calculations for every reaction time
        if (timer < m_profile.reactionTime)
        {
            timer += Time.fixedDeltaTime;
            return m_decided_point;
        }
        timer = 0;

        m_decided_point = Vector3.zero;

        // Evaluate the near cars and their actions
            /// First order the cars by their proximity
        Array.Sort(near_cars, (a, b) =>
        {
            if (a == null && b == null) return 0;
            if (a == null) return 1;  // nulls sort to the end
            if (b == null) return -1;
            if (a == this) return 1;

            float distA = (a.Current_Position - Current_Position).sqrMagnitude;
            float distB = (b.Current_Position - Current_Position).sqrMagnitude;
            return distA.CompareTo(distB);
        });

        SC_PhysicObject first = near_cars[0];
        // The other car is going faster
        if (first.current_velocity > current_velocity)
        {
            float dot = Vector3.Dot(Forward, first.Forward);
            // The cars are going in the same direction
            if (dot > 0.75f)
            {
                Vector3 other_local = transform.InverseTransformPoint(first.Current_Position);
                // The other car is behind
                if (other_local.z < 0)
                {
                    Vector3 diff = (Current_Position + Forward * first.current_velocity) - first.Current_Position;
                    Vector3 other_future_position = first.Current_Position + first.Forward * (first.current_velocity * diff.magnitude);

                    if (GetActionProbability() > 0.5f)
                    {
                        current_action = CarAction.OVERTAKING;
                        m_decided_point = other_future_position;
                    }
                }
            }
        }

        return m_decided_point;
    }

    private float GetActionProbability()
    {
        // Gets the average between the two
        float prob = (pressure + m_profile.aggression) / 2;
        Debug.Log($"Got prob: {prob}");
        return Mathf.Clamp01(prob);
    }

    private void HandleDriving(Vector3 override_position, float acceleration_multiplier = 0)
    {
        // Acceleration ---------------------------------
        float target_speed = next_point.targetSpeed > speed ? speed : next_point.targetSpeed;
        float accel_t = 1 - current_velocity / target_speed;
        if (accel_t < 1)
        {
            velocity += Forward * m_current_acceleration * Time.fixedDeltaTime;
            m_current_acceleration = Mathf.MoveTowards(m_current_acceleration, accel_t * speed, Time.fixedDeltaTime * acceleration);
        }
            

        // Rotation -------------------------------------
            /// Get the future point
        if (override_position != Vector3.zero)
        {
            future_point = override_position; /// Overrides the target point and makes the car steer to it
        }
        else if (!next_point.corner)
        {
            SC_RacePoint future_point_data = m_track.GetRacePoint(m_track.GetNextIndex(next_point.index));
            Vector3 p0 = next_point.transform.position
                + next_point.transform.right
                * (next_point.lateralOffset + current_random_lateral_offset);
            Vector3 p1 =
                future_point_data.transform.position +
                future_point_data.transform.right
                * (future_point_data.lateralOffset + current_random_lateral_offset)
                + Forward * current_velocity;
            future_point = m_track.GetSegmentCurve(p0, -next_point.transform.forward, p1, (p0 - p1).magnitude > max_distance_look ? 0 : look_ahead);
        }
        else
        {
            future_point = next_point.transform.position + next_point.transform.right * next_point.lateralOffset;
        }

        Vector3 diff = (future_point - Current_Position);
        if (diff.sqrMagnitude > 0.01f)
        {
            Vector3 dir = diff.normalized;

            float angle_diff = Vector3.SignedAngle(Forward, dir, Vector3.up);
            float frame_steering = Mathf.Clamp(angle_diff / 45, -1, 1);

            m_steering = Mathf.MoveTowards(m_steering, frame_steering, steering_smooth * Time.fixedDeltaTime);

            Vector3 rot = Vector3.up * m_steering * rotation_speed;
            float speedForward = Vector3.Dot(velocity, Forward);
            rotation += rot * Mathf.Abs(speedForward * 2) * Time.fixedDeltaTime;
        }
    }

    public void SetNextPoint(SC_RacePoint next)
    {
        next_point = next;
        current_random_lateral_offset = UnityEngine.Random.Range(-random_lateral_range, random_lateral_range);
    }
    

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(future_point, 0.5f);
    }
}