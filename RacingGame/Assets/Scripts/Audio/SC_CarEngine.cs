using UnityEngine;

[RequireComponent(typeof(SC_PhysicObject))]
public class SC_CarEngine : MonoBehaviour
{
    private SC_PhysicObject m_car_physics;

    [Header("Audio")]
    private AudioSource m_as_low_rpm;
    private AudioSource m_as_high_rpm;

    public Vector2 rpm_range;
    public Vector2 max_pitch;

    public float rpm_smooth_speed = 4;

    public float m_current_rpm = 1, m_target_rpm = 1;

    void Start()
    {
        AudioSource[] audioSources = GetComponents<AudioSource>();
        m_as_low_rpm = audioSources[0];
        m_as_high_rpm = audioSources[1];

        m_car_physics = GetComponent<SC_PhysicObject>();
    }

    private void FixedUpdate()
    {
        UpdateEngineRPM(m_car_physics.current_velocity / (m_car_physics.max_velocity.z * 3.6f));
        UpdateEngineAudio();
    }

    private void UpdateEngineRPM(float throttle_input) // throttle_input: -1 to 1, or 0-1 for accel amount
    {
        float speed_ratio = Mathf.Clamp01(m_car_physics.current_velocity / (m_car_physics.max_velocity.z)); // normalized 0-1 based on top speed

        // RPM rises with both throttle intent and current speed, blended
        float throttle_factor = Mathf.Abs(throttle_input);
        m_target_rpm = Mathf.Lerp(rpm_range.x, rpm_range.y, Mathf.Max(speed_ratio, throttle_factor * 0.6f));

        // Smooth so it doesn't jump instantly — engines have inertia
        m_current_rpm = Mathf.Lerp(m_current_rpm, m_target_rpm, rpm_smooth_speed * Time.fixedDeltaTime);
    }
    private void UpdateEngineAudio()
    {
        float t_rpm = Mathf.InverseLerp(rpm_range.x, rpm_range.y, m_current_rpm);

        // Fade between the two audiosources
        m_as_low_rpm.volume = Mathf.Lerp(1, 0, Mathf.Clamp01(t_rpm * 1.4f));
        m_as_high_rpm.volume = Mathf.Lerp(0, 1, Mathf.Clamp01(t_rpm * 1.4f));

        // Small pitch change between the two
        float new_pitch = Mathf.Lerp(max_pitch.x, max_pitch.y, t_rpm);
        m_as_low_rpm.pitch = new_pitch;
        m_as_high_rpm.pitch = new_pitch;
    }
}