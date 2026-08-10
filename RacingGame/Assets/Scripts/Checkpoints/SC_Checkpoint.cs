using UnityEngine;
using System.Collections.Generic;

public class SC_Checkpoint : MonoBehaviour
{
    private SC_CheckpointManager sc_CheckpointManager;

    private SC_Car_npc[] npcs;
    private SC_RaceProgress[] m_cars;
    public bool opened = false;
    public float dist = 11;

    private bool[] cars_opened;
    private float[] cars_dist;
    private float[] cars_timer;

    public int check_point_num = -1;

    private float m_timer = 0;
    private void Start()
    {
        sc_CheckpointManager = SC_CheckpointManager.instance;

        npcs = FindObjectsByType<SC_Car_npc>(FindObjectsSortMode.None);
        m_cars = FindObjectsByType<SC_RaceProgress>(FindObjectsSortMode.None);
        cars_dist = new float[m_cars.Length];
        cars_timer = new float[m_cars.Length];
        cars_opened = new bool[m_cars.Length];

        if (check_point_num < 0)
            Debug.LogWarning("The checkpoint number must be assigned");
    }
    private void Update()
    {
        for (int i = 0; i < m_cars.Length; i++)
        {
            if (!cars_opened[i])
            {
                cars_dist[i] = Vector3.Distance(transform.position, m_cars[i].transform.position);

                if (cars_dist[i] < transform.localScale.x)
                {
                    m_cars[i].UpdateCurrentCheckpoint(check_point_num + 1);

                    if (m_cars[i].name.Contains("NPC"))
                    {
                        npcs[i % npcs.Length].SetDestination(sc_CheckpointManager.GetNextCheckpointPosition(check_point_num + 1));
                    }

                    cars_opened[i] = true;
                }
            }
            else
            {
                cars_timer[i] += Time.deltaTime;
                if (cars_timer[i] > 10)
                {
                    cars_opened[i] = false;
                    cars_timer[i] = 0;
                }
            }
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.gray;
        Gizmos.DrawWireSphere(transform.position, transform.localScale.x);
    }
}