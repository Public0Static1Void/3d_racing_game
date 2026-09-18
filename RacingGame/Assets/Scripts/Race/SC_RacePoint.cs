using UnityEngine;

public class SC_RacePoint : MonoBehaviour
{
    [HideInInspector] public int index = -1;

    [Header("Racing Line")]
    public float lateralOffset = 10;

    [Header("Speed")]
    public float targetSpeed = 100f;

    [Header("Corner")]
    public bool corner = false;

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, transform.position + transform.right * lateralOffset);
    }
}
