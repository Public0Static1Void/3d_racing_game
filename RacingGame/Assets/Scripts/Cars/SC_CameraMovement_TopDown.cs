using UnityEngine;

public class SC_CameraMovement_TopDown : MonoBehaviour
{
    [Header("References")]
    public Transform target;
    private SC_PhysicObject m_target_physics;

    [Header("Stats")]
    public float height = 5;
    public float max_height = 10;
    public float look_ahead = 1;
    public float max_look_ahead = 5;

    public float follow_speed = 10;
    void Start()
    {
        if (transform.parent != null) transform.SetParent(null, true);

        m_target_physics = target.GetComponent<SC_PhysicObject>();
    }

    void Update()
    {
        float speed_t = Mathf.Clamp01(m_target_physics.current_velocity / (m_target_physics.speed * 1.5f));
        // Look
        Vector3 look_position = target.position + target.forward * Mathf.Lerp(look_ahead, max_look_ahead, speed_t);
        transform.LookAt(look_position);

        // Movement


        Vector3 follow_position = target.position + target.up * Mathf.SmoothStep(height, max_height, speed_t);
        transform.position = Vector3.Lerp(transform.position, follow_position, Time.deltaTime * follow_speed);
    }
}