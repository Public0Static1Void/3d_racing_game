using UnityEngine;
using UnityEngine.InputSystem;

public class SC_Car : MonoBehaviour
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
    public float side_grip = 0.8f;
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

    private Vector2 m_input;

    private void Start()
    {
        // Store the current position and rotation
        _pos_current = transform.position;
        _rot_current = transform.rotation;
    }

    private void Update()
    {
        float alpha = (Time.time - Time.fixedTime) / Time.fixedDeltaTime;
        alpha = Mathf.Clamp01(alpha);
        transform.position = Vector3.Lerp(_pos_prev, _pos_current, alpha);
        transform.rotation = Quaternion.Slerp(_rot_prev, _rot_current, alpha);
    }
    private void FixedUpdate()
    {
        // Store the current position and rotation
        _pos_prev = _pos_current;
        _rot_prev = _rot_current;

        if (_pos_current.z > 160)
            _pos_current = new Vector3(_pos_current.x, _pos_current.y, 0);
        if (_pos_current.z < 0)                        
            _pos_current = new Vector3(_pos_current.x, _pos_current.y, 0);
        if (_pos_current.x < -5)
            _pos_current = new Vector3(5, _pos_current.y, _pos_current.z);
        if (_pos_current.x > 5)
            _pos_current = new Vector3(-5, _pos_current.y, _pos_current.z);

        RaycastHit hit = HandleGroundDetection();
        HandlePenetration(hit, ref velocity);

        HandleInputSpeed(ref velocity, ref rotation);
        

        ApplyDrift(ref velocity);

        HandleGravity(ref velocity);
        HandleGroundFriction(ref velocity, false);
        HandleGroundFriction(ref rotation, true);

        // Assign the current position and rotation
        _pos_current += velocity;

        float turnAmount = m_input.x * rotation_speed * Time.fixedDeltaTime;
        _rot_current *= Quaternion.Euler(rotation);

        ClampVelocity(ref velocity, max_velocity);
        ClampVelocity(ref rotation, max_rotation);
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
        float penetration = hit.distance - transform.localScale.y;
        if (penetration < 0)
        {
            _pos_current -= Vector3.up * penetration;
        }
    }
    private void HandleGravity(ref Vector3 vel)
    {
        if (onGround)
        {
            if (vel.y < -0.25f)
                vel.y *= -bounciness;
            else
                vel.y = 0;
            return;
        }

        vel += Physics.gravity * gravity_multipler * Time.fixedDeltaTime;
    }
    private void HandleGroundFriction(ref Vector3 vel, bool rotating)
    {
        if (!onGround)
        {
            if (rotating)
                if (m_input.x != 0) return;
            else
                if (m_input.y != 0) return;
        }

        vel *= ground_friction;
    }
    private void HandleInputSpeed(ref Vector3 vel, ref Vector3 rot)
    {
        if (onGround)
        {
            vel += transform.forward * m_input.y * acceleration * Time.fixedDeltaTime;
        }
        /// A dot tells you how much a vector is pointing in the direction of another
        float speedForward = Vector3.Dot(vel, transform.forward);
        rot.y += m_input.x * rotation_speed * Mathf.Abs(speedForward) * Time.fixedDeltaTime;
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
    private void ApplyDrift(ref Vector3 vel)
    {
        if (!onGround) return;

        Vector3 forward = transform.forward * Vector3.Dot(vel, transform.forward);
        Vector3 side = transform.right * Vector3.Dot(vel, transform.right);

        vel.x = forward.x + side.y * side_grip;
        vel.z = forward.z + side.z * side_grip;
    }


    /// <summary>
    /// Gets the actual movement input
    /// </summary>
    public void Move(InputAction.CallbackContext con)
    {
        m_input = con.ReadValue<Vector2>();
    }


    private void OnDrawGizmos()
    {
        Gizmos.color = Color.gray;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.down * (transform.localScale.y * ground_detection_offset));
    }
}