using UnityEngine;
using UnityEngine.InputSystem;

public class SC_Car : SC_PhysicObject
{
    private Vector2 m_input;

    [Header("Velocity")]
    public float current_velocity = 0;

    protected override void FixedUpdate()
    {
        base.FixedUpdate();
        HandleInputSpeed(ref velocity, ref rotation);

        current_velocity = velocity.magnitude;
    }

    #region PhysicFunctions
    /// <summary>
    /// Gets the actual movement input
    /// </summary>
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
        AddForce(transform.forward * amount);
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