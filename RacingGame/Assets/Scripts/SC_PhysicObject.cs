using System;
using UnityEngine;
using UnityEngine.Assertions.Must;

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

    [Header("Ground")]
    public LayerMask layer_ground;
    [Range(0, 1)]
    public float ground_friction = 0.9f;
    public float ground_detection_offset = 1;
    public bool onGround = false;
    private float m_ground_timer = 0f;
    public const float MAX_WALKABLE_ANGLE = 75;

    private static readonly Vector3[] positions = { Vector3.right + Vector3.forward, Vector3.left + Vector3.forward, Vector3.right + Vector3.back, Vector3.left + Vector3.back };


    private Vector3 m_start_position;

    protected virtual void Start()
    {
        // Store the current position and rotation
        _pos_current = transform.position;
        _rot_current = transform.rotation;

        start_mass = mass;

        m_start_position = transform.position;
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
            velocity = Vector3.zero;
            rotation = Vector3.zero;
        }

        bool start_on_ground = onGround;

        HandleWallCollision(ref velocity);
        RaycastHit hit = new RaycastHit();
        for (int i = 0; i < positions.Length; i++)
        {
            hit = HandleGroundDetection(positions[i]);

            HandlePenetration(hit, positions.Length);
            HandleGroundRotation(hit);
        }

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
            if (hit.collider.isTrigger) return;

            float slopeAngle = Vector3.Angle(hit.normal, Vector3.up);
            if (slopeAngle <= MAX_WALKABLE_ANGLE) return;

            // Reflect the ORIGINAL incoming velocity — this is the real bounce impulse
            Vector3 reflected = Vector3.Reflect(vel, hit.normal) * bounciness;

            // Clamp how far we're allowed to move THIS frame to the wall surface
            float allowedDist = Mathf.Max(hit.distance - 0.05f, 0f);
            float scale = allowedDist / dist;
            vel = vel * scale + reflected * Time.fixedDeltaTime;
            // small immediate correction toward the surface, but velocity carries the real bounce forward

            vel = reflected; // persistent velocity for next frame uses the real bounce, not the shrunk one
        }
    }

    private RaycastHit GetPositionHit(Vector3 offset)
    {
        Vector3 pos = _pos_current + _rot_current * offset;
        Physics.Raycast(pos, -transform.up, out RaycastHit hit, transform.localScale.y * ground_detection_offset, layer_ground);
        return hit;
    }
    private void HandleGroundRotation(RaycastHit hit)
    {
        if (!onGround) return;
        Quaternion rot = Quaternion.FromToRotation(transform.up, hit.normal) * _rot_current;

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

        Vector3 forward = Vector3.Project(vel, transform.forward);
        Vector3 side = Vector3.Project(vel, transform.right);

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

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.gray;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.down * (transform.localScale.y * ground_detection_offset));
    }
}
