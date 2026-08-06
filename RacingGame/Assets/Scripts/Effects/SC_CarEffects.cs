using UnityEngine;

[RequireComponent(typeof(SC_PhysicObject))]
public class SC_CarEffects : MonoBehaviour
{
    private SC_PhysicObject m_physics;

    public AudioSource as_DriftSource;
    private float m_drift_volume = 0;

    public ParticleSystem ps_DriftParticles;
    public ParticleSystem ps_DriftParticles2;
    private ParticleSystem.EmissionModule emission;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        m_physics = GetComponent<SC_PhysicObject>();

        emission = ps_DriftParticles.emission;
    }

    // Update is called once per frame
    void Update()
    {
        if (!m_physics.drifting)
        {
            if (m_drift_volume > 0)
            {
                m_drift_volume -= Time.deltaTime * 10;
                as_DriftSource.volume = m_drift_volume;
            }
            else
            {
                m_drift_volume = 0;
                if (as_DriftSource.isPlaying)
                    as_DriftSource.Pause();
            }
            if (ps_DriftParticles.isPlaying) ps_DriftParticles.Pause();
            if (ps_DriftParticles2.isPlaying) ps_DriftParticles2.Pause();

            return;
        }

        if (!ps_DriftParticles.isPlaying) ps_DriftParticles.Play();
        if (!ps_DriftParticles2.isPlaying) ps_DriftParticles2.Play();

        if (!as_DriftSource.isPlaying) as_DriftSource.UnPause();

        if (m_drift_volume < 1)
        {
            m_drift_volume += Time.deltaTime * 10;
            as_DriftSource.volume = m_drift_volume;
        }
    }
}