using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class SC_Checkpoint : MonoBehaviour
{
    private SC_CheckpointManager sc_CheckpointManager;

    public SC_Car_npc npc;
    public bool opened = false;
    public float dist = 11;

    public int check_point_num = -1;

    private float m_timer = 0;
    private void Start()
    {
        BoxCollider coll = GetComponent<BoxCollider>();
        coll.isTrigger = true;

        sc_CheckpointManager = SC_CheckpointManager.instance;

        if (check_point_num < 0)
            Debug.LogWarning("The checkpoint number must be assigned");
    }
    private void Update()
    {
        if (opened)
        {
            m_timer += Time.deltaTime;
            if (m_timer > 1)
            {
                opened = false;
                m_timer = 0;
            }
            return;
        }

        dist = Vector3.Distance(transform.position, npc.transform.position);
        if (dist < 10)
        {
            npc.SetDestination(sc_CheckpointManager.GetNextCheckpointPosition(check_point_num + 1));
            opened = true;
        }
    }
}