using System.Collections;
using UnityEngine;

public class SC_CameraMovement : MonoBehaviour
{
    public Transform target;
    private Transform m_startTarget;
    private SC_PhysicObject m_target_physics;

    [Header("Orbit")]
    public float distance = 6f;
    public float height = 3f;
    public float height_min = 1f;
    private float m_current_height = 1f;
    [Tooltip("Lower = camera follows car's turn quicker, higher = more lag/swing")]
    public float rotation_smooth_time = 0.25f;

    [Header("Look")]
    public float look_height_offset = 1f;

    private float m_current_yaw;
    private float m_yaw_velocity;

    [Header("Speed effects")]
    public float z_speed = 1;
    public float z_limit = 4;
    private float m_start_distance, m_stored_distance;
    private float m_start_fov = 0;
    public float max_fov = 90;
    public float fov_change_speed = 10;

    Camera m_camera, m_overlay_camera;

    [Header("Noise")]
    public float noise_level = 1;
    [Tooltip("How faster the car needs to go to let the noise start")]
    public float noise_threshold = 0.1f;
    public float noise_frequency = 1;
    public float noise_position_amount = 0.25f;
    public float noise_rotation_amount = 2; // degrees
    private float m_current_noise;
    private Vector3 m_noise_seed;
    private float m_impact_strength = 0;
    public float impact_decay = 3;

    private Vector3 m_start_pos_local;

    private Transform m_look_point;
    void Start()
    {
        m_camera = GetComponent<Camera>();
        m_overlay_camera = transform.GetChild(0).GetComponent<Camera>();

        m_target_physics = target.GetComponent<SC_PhysicObject>();

        m_current_yaw = target.eulerAngles.y;

        m_start_distance = distance;
        m_stored_distance = distance;
        m_start_fov = m_camera.fieldOfView;

        m_current_height = look_height_offset;

        m_startTarget = target;

        // Noise
        m_start_pos_local = transform.localPosition;
        m_noise_seed = new Vector3(Random.value, Random.value, Random.value) * 100;
        m_target_physics.collision_event += OnCollision;
    }

    void LateUpdate()
    {
        float speed_t = Mathf.Clamp01(m_target_physics.current_velocity / (m_target_physics.speed * 1.5f));

        // Noise -----------------------------------------
        m_impact_strength = Mathf.MoveTowards(m_impact_strength, 0, Time.deltaTime * impact_decay);

        float time = Time.time * noise_frequency;
        float noise_x = Mathf.PerlinNoise(m_noise_seed.x, time) * 2 - 1;
        float noise_y = Mathf.PerlinNoise(m_noise_seed.y, time) * 2 - 1;
        float noise_z = Mathf.PerlinNoise(m_noise_seed.z, time) * 2 - 1;

        float frame_strength = speed_t > noise_threshold ? speed_t : 0;
        float total_strength = frame_strength + m_impact_strength + noise_level;

        Vector3 pos_offset = new Vector3(noise_x, noise_y, noise_z) * total_strength * noise_position_amount;

        float target_yaw = target.eulerAngles.y;
        m_current_yaw = Mathf.SmoothDampAngle(m_current_yaw, target_yaw, ref m_yaw_velocity, rotation_smooth_time);

        Quaternion orbit_rot = Quaternion.Euler(0, m_current_yaw, 0);
        m_current_height = Mathf.Lerp(height, height_min, speed_t);

        Vector3 world_noise = transform.up * pos_offset.y + transform.right * pos_offset.x + transform.forward * pos_offset.z;
        Vector3 desired_pos = target.position - (orbit_rot * Vector3.forward * distance) + Vector3.up * m_current_height + world_noise; // <-- noise now actually applied

        float new_distance;
        if (speed_t < 0.5f)
            new_distance = Mathf.Lerp(distance, m_target_physics.current_velocity + m_start_distance, Time.deltaTime * speed_t);
        else
            new_distance = Mathf.SmoothStep(distance, m_target_physics.current_velocity + m_start_distance, Time.deltaTime * speed_t);

        distance = Mathf.Clamp(new_distance, m_start_distance, m_start_distance * z_limit);

        transform.position = desired_pos;

        Vector3 look_point = target.position + Vector3.up * look_height_offset;
        if (m_look_point != null) look_point = m_look_point.position;

        Vector3 rot_noise = new Vector3(noise_y, noise_x, noise_z) * total_strength * noise_rotation_amount; // separate tunable amount, see below
        Quaternion look_rotation = Quaternion.LookRotation((look_point - transform.position).normalized) * Quaternion.Euler(rot_noise);
        transform.rotation = Quaternion.Lerp(transform.rotation, look_rotation, Time.deltaTime * 1);

        // FOV -------------------------------------------
        m_camera.fieldOfView = Mathf.Lerp(m_start_fov, max_fov, speed_t);
        m_overlay_camera.fieldOfView = m_camera.fieldOfView;
    }

    public void SetLookpoint(Transform new_point)
    {
        m_look_point = new_point;
    }
    public void SetTarget(Transform new_target)
    {
        if (new_target == null) new_target = m_startTarget;
        target = new_target;
    }
    public void SetDistance(float new_distance)
    {
        if (new_distance < 0) new_distance = m_stored_distance;
        StartCoroutine(LerpDistance(new_distance));
    }
    private IEnumerator LerpDistance(float new_distance)
    {
        float duration = 2.5f;
        float timer = 0;
        float start = m_start_distance;
        while (timer < duration)
        {
            float t = timer / duration;
            t = Mathf.SmoothStep(0, 1, t);
            m_start_distance = Mathf.Lerp(start, new_distance, t);

            timer += Time.deltaTime;
            yield return null;
        }

        m_start_distance = new_distance;
    }

    private void OnCollision()
    {
        m_impact_strength = Mathf.Clamp(m_target_physics.current_velocity / m_target_physics.speed, 0.3f, 2);
    }
}