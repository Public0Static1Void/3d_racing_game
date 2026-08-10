using UnityEngine;

public class SC_CameraMovement : MonoBehaviour
{
    public Transform target;
    private SC_PhysicObject m_target_physics;

    [Header("Orbit")]
    public float distance = 6f;
    public float height = 3f;
    [Tooltip("Lower = camera follows car's turn quicker, higher = more lag/swing")]
    public float rotation_smooth_time = 0.25f;

    [Header("Look")]
    public float look_height_offset = 1f;

    private float m_current_yaw;
    private float m_yaw_velocity;

    [Header("Speed effects")]
    public float z_speed = 1;
    public float z_limit = 4;
    private float m_start_distance;
    private float m_start_fov = 0;
    public float max_fov = 90;

    Camera m_camera;
    void Start()
    {
        m_camera = GetComponent<Camera>();
        m_target_physics = target.GetComponent<SC_PhysicObject>();

        m_current_yaw = target.eulerAngles.y;

        m_start_distance = distance;
        m_start_fov = m_camera.fieldOfView;
    }

    void LateUpdate()
    {
        float target_yaw = target.eulerAngles.y;

        // Smoothly swing the camera's orbit angle toward the car's current yaw
        m_current_yaw = Mathf.SmoothDampAngle(m_current_yaw, target_yaw, ref m_yaw_velocity, rotation_smooth_time);

        Quaternion orbit_rot = Quaternion.Euler(0, m_current_yaw, 0);
        Vector3 desired_pos = target.position - (orbit_rot * Vector3.forward * distance) + Vector3.up * height;

        // Get the object velocity
        float new_distance = 0;
        float speed_t = m_target_physics.current_velocity / (m_target_physics.speed * 3.6f);
        if (speed_t < 0.5f)
        {
            new_distance = Mathf.Lerp(distance, m_target_physics.current_velocity + m_start_distance, Time.deltaTime * speed_t);
        }
        else
        {
            new_distance = Mathf.SmoothStep(distance, m_target_physics.current_velocity + m_start_distance, Time.deltaTime * speed_t);
        }
        distance = new_distance;
        distance = Mathf.Clamp(distance, m_start_distance, m_start_distance * z_limit);

        transform.position = desired_pos;

        Vector3 look_point = target.position + Vector3.up * look_height_offset;
        transform.LookAt(look_point);

        
        m_camera.fieldOfView = Mathf.Lerp(m_start_fov, max_fov, speed_t);
    }
}