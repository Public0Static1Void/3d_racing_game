using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(SC_PhysicObject))]
public class SC_CollisionEffects : MonoBehaviour
{
    [Header("Audio")]
    public List<AudioClip> clips_collision;

    public float collision_recover = 0.25f;
    private float m_timer = 0;
    private bool m_collided = false;

    [Header("Particles")]
    public ParticleSystem parts_sparks;

    private SC_PhysicObject m_car;

    private bool m_being_called = false;
    private float m_timer_call = 0;

    private Transform m_tr_mainCamera;

    void Start()
    {
        m_tr_mainCamera = Camera.main.transform;

        m_car = GetComponent<SC_PhysicObject>();

        m_car.collision_event += SpawnCollisionSound;
        m_car.regular_collision_event += RegularCollisionEffect;
    }

    private void Update()
    {
        if (m_collided)
        {
            m_timer += Time.deltaTime;
            if (m_timer > collision_recover)
            {
                m_timer = 0;
                m_collided = false;
            }
        }
        if (m_being_called)
        {
            m_timer_call += Time.deltaTime;
            if (m_timer_call > 0.25f)
            {
                m_being_called = false;
                CloseActiveEffects();
            }
        }
    }

    private void SpawnCollisionSound()
    {
        if (!m_collided)
        {
            AudioClip clip = clips_collision[Random.Range(0, clips_collision.Count - 1)];

            SC_SoundManager.instance.InstantiateSound(clip, m_car.Current_Position);

            m_collided = true;
        }
    }

    private void RegularCollisionEffect(Vector3 point, Vector3 direction)
    {
        float dot = Vector3.Dot(m_car.Forward, direction);
        if (dot > 0.05f)
        {
            if (!m_being_called)
                parts_sparks.Play();

            parts_sparks.transform.position = point;
            parts_sparks.transform.LookAt(m_tr_mainCamera.position + Vector3.up);

            m_being_called = true;
            m_timer_call = 0;
        }
    }

    private void CloseActiveEffects()
    {
        parts_sparks.Stop();
    }
}
