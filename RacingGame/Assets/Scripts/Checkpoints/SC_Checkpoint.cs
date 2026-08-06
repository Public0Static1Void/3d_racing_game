using UnityEngine;
using System.Collections.Generic;

public class SC_Checkpoint : MonoBehaviour
{
    private SC_CheckpointManager sc_CheckpointManager;

    private SC_Car_npc[] npcs;
    public bool opened = false;
    public float dist = 11;

    public int check_point_num = -1;

    private float m_timer = 0;
    private void Start()
    {
        sc_CheckpointManager = SC_CheckpointManager.instance;

        npcs = FindObjectsByType<SC_Car_npc>(FindObjectsSortMode.None);

        if (check_point_num < 0)
            Debug.LogWarning("The checkpoint number must be assigned");
    }
    private void Update()
    {
        if (opened)
        {
            m_timer += Time.deltaTime;
            if (m_timer > 0.1f)
            {
                opened = false;
                m_timer = 0;
            }
            return;
        }

        foreach (SC_Car_npc npc in npcs)
        {
            dist = Vector3.Distance(transform.position, npc.transform.position);
            if (dist < transform.localScale.x)
            {
                npc.SetDestination(sc_CheckpointManager.GetNextCheckpointPosition(check_point_num + 1));
                opened = true;
            }
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.gray;
        Gizmos.DrawWireSphere(transform.position, transform.localScale.x);
    }
}