using UnityEngine;

public class SC_CameraMovement : MonoBehaviour
{
    public Transform target;
    public float offset_z, offset_y, offset_look_z = 1;
    public float speed_follow = 10;
    public float speed_turn = 1;

    void Start()
    {
        
    }

    void Update()
    {
        Vector3 pos = target.transform.position + (target.transform.forward * offset_z) + Vector3.up * offset_y;
        
        transform.position = Vector3.Lerp(transform.position, pos, speed_follow * Time.deltaTime);

        Vector3 look_dir = ((target.transform.position + (target.transform.forward * offset_look_z)) - transform.position).normalized;
        transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(look_dir), speed_turn * Time.deltaTime);
    }


    private void OnDrawGizmos()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(target.transform.position + (target.transform.forward * offset_z) + Vector3.up * offset_y, 0.25f);

        Gizmos.color = Color.gray;
        Gizmos.DrawWireSphere(target.transform.position + (target.transform.forward * offset_look_z), 0.25f);
    }
}