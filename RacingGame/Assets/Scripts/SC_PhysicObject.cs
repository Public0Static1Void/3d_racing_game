using UnityEngine;

public class SC_PhysicObject : MonoBehaviour
{
    [Header("Stats")]
    public float speed = 10f;
    public float mass = 1f;
    public float acceleration = 1f;
    public float rotation_speed = 1f;
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
    

    protected virtual void Start()
    {
        // Store the current position and rotation
        _pos_current = transform.position;
        _rot_current = transform.rotation;
    }

    protected virtual void Update()
    {
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

        if (_pos_current.z > 160)
            _pos_current = new Vector3(_pos_current.x, _pos_current.y, 0);
        if (_pos_current.z < 0)
            _pos_current = new Vector3(_pos_current.x, _pos_current.y, 0);
        if (_pos_current.x < -40)
            _pos_current = new Vector3(5, _pos_current.y, _pos_current.z);
        if (_pos_current.x > 40)
            _pos_current = new Vector3(-5, _pos_current.y, _pos_current.z);

        bool start_on_ground = onGround;
        RaycastHit hit = HandleGroundDetection();

        // Check if it has to bounce
        if (start_on_ground != onGround && onGround && velocity.y < -0.25f)
        {
            Bounce(ref velocity);
        }
        

        HandleGravity(ref velocity);
        HandlePenetration(hit, ref velocity);
        

        float turn_amount = Mathf.Abs(rotation.y) / max_rotation.y;
        ApplyDrift(ref velocity, turn_amount);

        
        HandleGroundFriction(ref velocity, false);
        HandleGroundFriction(ref rotation, true);

        ClampVelocity(ref velocity, max_velocity);
        ClampVelocity(ref rotation, max_rotation);

        // Assign the current position and rotation
        _pos_current += velocity;

        _rot_current *= Quaternion.Euler(rotation);
    }

    private void Bounce(ref Vector3 vel)
    {
        vel.y *= -bounciness;
    }
    private RaycastHit HandleGroundDetection()
    {
        onGround = Physics.Raycast(_pos_current, Vector3.down, out RaycastHit hit, transform.localScale.y * ground_detection_offset, layer_ground);
        Color draw_col = onGround ? Color.green : Color.red;
        Debug.DrawLine(_pos_current, _pos_current + Vector3.down * (transform.localScale.y * ground_detection_offset), draw_col);

        return hit;
    }
    private void HandlePenetration(RaycastHit hit, ref Vector3 vel)
    {
        if (hit.collider == null) return;
        float penetration = transform.localScale.y - hit.distance;
        if (penetration > 0)
        {
            _pos_current += Vector3.up * penetration;
        }
    }
    private void HandleGravity(ref Vector3 vel)
    {
        if (onGround) return;

        vel += Physics.gravity * gravity_multipler * Time.fixedDeltaTime;
    }
    protected virtual void HandleGroundFriction(ref Vector3 vel, bool rotating)
    {
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
