using System;
using System.Collections.Generic;
using Unity.Hierarchy;
using Unity.VisualScripting;
using UnityEngine;

public class SC_PhysicObject : MonoBehaviour
{
    [Header("Stats")]
    public float speed = 10f;
    public float mass = 1f;
    [HideInInspector] public float start_mass;
    public float acceleration = 1f;
    public float rotation_speed = 1f;
    public float terrain_rotate_speed = 10f;
    public float gravity_multipler = 1f;
    [Range(0, 1)]
    public float bounciness = 0.85f;
    [Range(0, 1)]
    public float side_grip_straight = 0.8f;
    [Range(0, 1)]
    public float side_grip_turn = 1;
    public Vector3 velocity;
    public Vector3 max_velocity;
    public Vector3 rotation;
    public Vector3 max_rotation;

    private Vector3 _pos_prev, _pos_current;
    private Quaternion _rot_prev, _rot_current;

    public Vector3 Current_Position => _pos_current;
    public Vector3 Forward => _rot_current * Vector3.forward;
    public Vector3 Right => _rot_current * Vector3.right;
    public Vector3 Up => _rot_current * Vector3.up;

    public float current_velocity = 0;

    [Header("Collisions")]
    public LayerMask layer_cars;

    [Header("Ground")]
    public LayerMask layer_ground;
    [Range(0, 1)]
    public float ground_friction = 0.9f;
    public float ground_detection_offset = 1;
    public bool onGround = false;
    private float m_ground_timer = 0f;
    public const float MAX_WALKABLE_ANGLE = 75;

    [Header("Wheels")]
    [SerializeField] private List<Transform> m_wheels;
    [SerializeField] private Transform m_car;
    [SerializeField] private float m_wheel_offset = 0;
    private float wheel_car_height_difference = 0;
    private float[] m_wheels_rotation;

    [SerializeField] private Vector3 model_forward_axis = Vector3.down;
    [SerializeField] private Vector3 model_up_axis = Vector3.forward;

    private Quaternion m_axis_correction;


    private static readonly Vector3[] positions = { Vector3.right + Vector3.forward, Vector3.left + Vector3.forward, Vector3.right + Vector3.back, Vector3.left + Vector3.back };


    protected Vector3 m_start_position;
    private Quaternion m_start_rotation;

    public Vector3 aux_offset;

    protected virtual void Start()
    {
        // Store the current position and rotation
        _pos_current = transform.position;
        _rot_current = transform.rotation;

        start_mass = mass;

        m_start_position = transform.position;
        m_start_rotation = transform.rotation;

        // Wheels
        m_wheels_rotation = new float[m_wheels.Count];
        wheel_car_height_difference = m_car.position.y - m_wheels[0].position.y;

        foreach (Transform t in m_wheels)
        {
            CreateParent(t);
        }

        CreateParent(m_car);

        m_axis_correction = Quaternion.Inverse(Quaternion.LookRotation(model_forward_axis, model_up_axis));
    }

    private void CreateParent(Transform t)
    {
        // One-time setup per wheel, e.g. in Start()
        GameObject pivotGO = new GameObject(t.name + "_Pivot");
        pivotGO.transform.SetParent(transform, false);
        pivotGO.transform.position = t.position;

        Vector3 s = transform.lossyScale;
        pivotGO.transform.localScale = new Vector3(1f / s.x, 1f / s.y, 1f / s.z); // cancels the parent's scale

        t.SetParent(pivotGO.transform, true);
    }

    protected virtual void Update()
    {
        if (!onGround)
        {
            m_ground_timer += Time.deltaTime;
        }
        else
        {
            m_ground_timer = 0;
        }

        // Interpolate the current position and rotation
        float alpha = (Time.time - Time.fixedTime) / Time.fixedDeltaTime;
        alpha = Mathf.Clamp01(alpha);
        transform.position = Vector3.Lerp(_pos_prev, _pos_current, alpha);
        transform.rotation = Quaternion.Slerp(_rot_prev, _rot_current, alpha);
    }
    protected virtual void FixedUpdate()
    {
        // Store the current position and rotation
        _pos_prev = _pos_current;
        _rot_prev = _rot_current;

        if (_pos_current.y < -80)
        {
            _pos_current = m_start_position;
            _rot_current = m_start_rotation;
            velocity = Vector3.zero;
            rotation = Vector3.zero;

            transform.position = m_start_position;

            RestartEvent();
        }

        bool start_on_ground = onGround;

        HandleWallCollision(ref velocity);
        HandleCarCollision(ref velocity);
        RaycastHit hit = new RaycastHit();

        int any_grounded = 0;
        float highest_position = m_wheels[0].position.y;
        for (int i = 0; i < positions.Length; i++)
        {
            hit = HandleGroundDetection(positions[i]);

            HandlePenetration(hit, positions.Length);
            HandleGroundRotation(hit);

            HandleWheelPosition(m_wheels[i], hit);
            HandleWheelRotation(m_wheels[i], i);

            if (m_wheels[i].position.y > highest_position)
                highest_position = m_wheels[i].position.y;

            if (onGround) any_grounded++;
        }

        HandleCarRotation();

        if (any_grounded > 0) onGround = true;

        // Check if it has to bounce
        if (start_on_ground != onGround && onGround && velocity.y < -0.25f && m_ground_timer > 0.5f)
        {
            Bounce(ref velocity);
        }

        HandleGravity(ref velocity);


        float turn_amount = Mathf.Abs(rotation.y) / max_rotation.y;
        ApplyDrift(ref velocity, turn_amount);

        HandleGroundFriction(ref velocity, false);
        HandleGroundFriction(ref rotation, true);

        ClampVelocity(ref velocity, max_velocity);
        ClampVelocity(ref rotation, max_rotation);

        // Assign the current position and rotation
        _pos_current += velocity;
        _rot_current *= Quaternion.Euler(0, rotation.y, 0);

        current_velocity = velocity.magnitude * 3.6f; // conversion to kmh
    }

    private void Bounce(ref Vector3 vel)
    {
        vel.y *= -bounciness;
    }
    private RaycastHit HandleGroundDetection(Vector3 offset)
    {
        Vector3 center = _pos_current + _rot_current * offset;
        onGround = Physics.Raycast(center, Vector3.down, out RaycastHit hit, transform.localScale.y * ground_detection_offset, layer_ground);
        Color draw_col = onGround ? Color.green : Color.red;
        Debug.DrawLine(center, center + Vector3.down * (transform.localScale.y * ground_detection_offset), draw_col);

        return hit;
    }

    private void HandleWallCollision(ref Vector3 vel)
    {
        float dist = vel.magnitude;
        if (dist < 0.0001f) return; // only skip for genuinely zero movement

        Vector3 dir = vel.normalized;
        Vector3 dir_horizontal = new Vector3(dir.x, 0, dir.z);
        if (dir_horizontal.sqrMagnitude < 0.0001f) return;
        dir_horizontal.Normalize();

        Vector3 half_extents = transform.localScale * 0.5f;

        if (Physics.BoxCast(_pos_current, half_extents, dir_horizontal, out RaycastHit hit, _rot_current, dist, layer_ground))
        {
            // Don't process if the collider is trigger
            if (hit.collider.isTrigger)
            {
                return;
            }

            float slopeAngle = Vector3.Angle(hit.normal, Vector3.up);
            if (slopeAngle <= MAX_WALKABLE_ANGLE) return;

            // Reflect the ORIGINAL incoming velocity — this is the real bounce impulse
            Vector3 reflected = Vector3.Reflect(vel, hit.normal) * bounciness;

            // Clamp how far we're allowed to move THIS frame to the wall surface
            float allowedDist = Mathf.Max(hit.distance - 0.05f, 0f);
            float scale = allowedDist / dist;
            vel = vel * scale + reflected * Time.fixedDeltaTime;
        }
    }

    private void HandleCarCollision(ref Vector3 vel)
    {
        Vector3 center = _pos_current + Vector3.up * (transform.localScale.y * 0.5f);
        Vector3 half_extents = transform.localScale * 0.5f;

        float dist = velocity.magnitude;

        Collider[] hits = Physics.OverlapBox(center, half_extents, _rot_current, layer_cars, QueryTriggerInteraction.Collide);

        foreach (Collider hit in hits)
        {
            if (hit.gameObject == gameObject) continue; // Skip self
            if (!hit.CompareTag("PhysicObject")) continue; // Skip no physic objects

            Vector3 dir_collision = (Current_Position - hit.transform.position);
            dir_collision.y = 0;

            if (dir_collision.magnitude < 0.001f)
                dir_collision = Right; // Fallback for if the cars are overlapping perfectly

            float allowed_dist = Mathf.Max(dir_collision.magnitude - 0.05f, 0f); // How much distance is allowed the car to intersect this frame
            float scale = allowed_dist / dist;

            vel = dir_collision * scale * Time.fixedDeltaTime / mass;
        }
    }

    private RaycastHit GetPositionHit(Vector3 offset)
    {
        Vector3 pos = _pos_current + _rot_current * offset;
        Physics.Raycast(pos, -Up, out RaycastHit hit, transform.localScale.y * ground_detection_offset, layer_ground);
        return hit;
    }
    private void HandleGroundRotation(RaycastHit hit)
    {
        if (!onGround) return;
        Quaternion rot = Quaternion.FromToRotation(Up, hit.normal) * _rot_current;

        _rot_current = Quaternion.Slerp(_rot_current, rot, Time.fixedDeltaTime * terrain_rotate_speed);
    }
    private void HandlePenetration(RaycastHit hit, int ray_num)
    {
        if (hit.collider == null) return;
        float penetration = ground_detection_offset * transform.localScale.y - hit.distance;
        if (penetration > 0)
        {
            _pos_current += Vector3.up * penetration / ray_num;
        }
    }

    // Wheels
    private void HandleWheelPosition(Transform wheel, RaycastHit hit)
    {
        if (hit.collider == null) return;

        Vector3 pos = wheel.position; // world space, current x/y/z
        pos.y = hit.point.y - wheel_car_height_difference + m_wheel_offset; // hit.point is already world space too
        wheel.position = Vector3.Lerp(wheel.position, pos, 100f * Time.fixedDeltaTime); // assign back in world space
    }
    private void HandleWheelRotation(Transform wheel, int index)
    {
        m_wheels_rotation[index] += velocity.magnitude * current_velocity * Time.fixedDeltaTime * 500; // vertical rotation

        float steer_y = index > 1 ? rotation.y * 22.5f : 0;

        m_wheels[index].localRotation = Quaternion.Euler(0, steer_y, 0) * Quaternion.Euler(m_wheels_rotation[index], 0, 0);
    }
    private void HandleCarRotation()
    {
        Vector3 frWheel = m_wheels[3].position;
        Vector3 flWheel = m_wheels[2].position;
        Vector3 rrWheel = m_wheels[1].position;
        Vector3 rlWheel = m_wheels[0].position;

        Vector3 frontMid = (frWheel + flWheel) * 0.5f;
        Vector3 rearMid = (rrWheel + rlWheel) * 0.5f;
        Vector3 rightMid = (frWheel + rrWheel) * 0.5f;
        Vector3 leftMid = (flWheel + rlWheel) * 0.5f;

        Vector3 forwardVec = frontMid - rearMid;
        Vector3 rightVec = rightMid - leftMid;

        Vector3 normal = Vector3.Cross(forwardVec, rightVec).normalized;
        if (normal.y < 0) normal = -normal;

        Vector3 projectedForward = Vector3.ProjectOnPlane(transform.forward, normal).normalized;
        if (projectedForward.sqrMagnitude < 0.0001f) projectedForward = m_car.transform.forward;

        // Standard frame that faces the desired direction...
        Quaternion desiredStandardFrame = Quaternion.LookRotation(projectedForward, normal);

        // ...then remapped so the MESH's actual forward/up axes land on that direction, not local Z/Y.
        Quaternion targetWorldRotation = desiredStandardFrame * m_axis_correction;

        Quaternion targetLocalRotation = Quaternion.Inverse(transform.rotation) * targetWorldRotation;

        m_car.localRotation = Quaternion.Slerp(m_car.localRotation, targetLocalRotation, 100f * Time.fixedDeltaTime);
    }

    private void HandleGravity(ref Vector3 vel)
    {
        if (onGround) return;

        float mass_multiplier = mass / start_mass;

        vel += Physics.gravity * gravity_multipler * Time.fixedDeltaTime * mass_multiplier;
    }
    protected virtual void HandleGroundFriction(ref Vector3 vel, bool rotating)
    {
        if (!onGround) return;
        vel *= ground_friction;
    }
    
    private void ClampVelocity(ref Vector3 vel, Vector3 clamp_vector)
    {
        vel.x = Mathf.Clamp(vel.x, -clamp_vector.x, clamp_vector.x);
        vel.y = Mathf.Clamp(vel.y, -clamp_vector.y, clamp_vector.y);
        vel.z = Mathf.Clamp(vel.z, -clamp_vector.z, clamp_vector.z);

        if (Mathf.Abs(vel.x) < 0.001f) vel.x = 0;
        if (Mathf.Abs(vel.y) < 0.001f) vel.y = 0;
        if (Mathf.Abs(vel.z) < 0.001f) vel.z = 0;

        if (vel.magnitude < 0.001f)
            vel = Vector3.zero;
    }
    private void ApplyDrift(ref Vector3 vel, float steering_amount)
    {
        if (!onGround) return;

        float vertical_vel = vel.y;

        Vector3 forward = Vector3.Project(vel, Forward);
        Vector3 side = Vector3.Project(vel, Right);

        steering_amount = Mathf.Clamp01(steering_amount);
        float grip = Mathf.Lerp(side_grip_straight, side_grip_turn, steering_amount);

        // Only drift at high speeds
        float speed_factor = Mathf.Clamp01(velocity.magnitude / max_velocity.z);
        grip = Mathf.Lerp(1, grip, speed_factor);

        vel = forward + side * grip;
        vel.y = vertical_vel;
    }

    public void AddForce(Vector3 force)
    {
        velocity += force / mass;
    }

    protected virtual void RestartEvent() { }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.gray;
        Gizmos.DrawLine(Current_Position, Current_Position + Vector3.down * (transform.localScale.y * ground_detection_offset));
    }
}
