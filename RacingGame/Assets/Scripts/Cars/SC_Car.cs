using UnityEngine;
using UnityEngine.InputSystem;

public class SC_Car : SC_PhysicObject
{
    private Vector2 m_input;

    private int last_direction = 0;

    protected override void FixedUpdate()
    {
        base.FixedUpdate();
        HandleInputSpeed(ref velocity, ref rotation);
    }

    protected override void Update()
    {
        base.Update();

        if (m_input == Vector2.zero && (m_current_acceleration > 0.01f || m_current_acceleration < -0.01f))
            m_current_acceleration = Mathf.MoveTowards(m_current_acceleration, 0, acceleration * Time.deltaTime * 4);
    }

    #region PhysicFunctions
    /// <summary>
    /// Gets the actual movement input
    /// </summary>
    private void HandleInputSpeed(ref Vector3 vel, ref Vector3 rot)
    {
        if (!can_drive)
        {
            m_current_acceleration = 0;
            return;
        }

        if (onGround)
        {
            if (m_input.y > last_direction && last_direction != 0 && m_current_acceleration > 0)
            {
                m_current_acceleration -= acceleration * Time.deltaTime;
            }
            else
            {
                m_current_acceleration += acceleration * Time.deltaTime;
            }
                
            vel += Forward * m_current_acceleration * m_input.y * Time.fixedDeltaTime;

            if (m_current_acceleration > speed) m_current_acceleration = speed;

            last_direction = (int)m_input.y;
        }
        /// A dot tells you how much a vector is pointing in the direction of another
        float speedForward = Vector3.Dot(vel, Forward);
        rot.y += m_input.x * rotation_speed * Mathf.Abs(speedForward * 2) * Time.fixedDeltaTime;
    }

    protected override void HandleGroundFriction(ref Vector3 vel, bool rotating)
    {
        if (!onGround)
        {
            if (rotating)
                if (m_input.x != 0) return;
            else
                if (m_input.y != 0) return;
        }
        base.HandleGroundFriction(ref vel, rotating);
    }

    public void Boost(float amount)
    {
        AddForce(Forward * amount);
    }
    #endregion

    #region Input
    // Input
    public void Move(InputAction.CallbackContext con)
    {
        m_input = con.ReadValue<Vector2>();
    }
    #endregion
}