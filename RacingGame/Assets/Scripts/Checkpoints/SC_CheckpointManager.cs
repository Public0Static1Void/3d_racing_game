using UnityEngine;
using System.Collections.Generic;

public class SC_CheckpointManager : MonoBehaviour
{
    public static SC_CheckpointManager instance {  get; private set; }

    public Transform checkpoints_parent;
    [HideInInspector] public SC_Checkpoint[] checkpoints;

    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(this.gameObject);

        checkpoints = checkpoints_parent.GetComponentsInChildren<SC_Checkpoint>();

        for (int i = 0; i < checkpoints.Length; i++)
        {
            checkpoints[i].check_point_num = i;
        }

        if (checkpoints.Length <= 1)
        {
            Debug.LogWarning("No checkpoints or only one stored");
        }
    }

    public Vector3 GetNextCheckpointPosition(int index)
    {
        return checkpoints[index % checkpoints.Length].transform.position + Vector3.right * Random.Range(-5f, 5f);
    }
    public (Vector3, int) GetNearestCheckpoint(Vector3 position)
    {
        float dist = 10000;
        int index = 0;
        for (int i = 1; i < checkpoints.Length; i++)
        {
            float new_dist = Vector3.Distance(position, checkpoints[i].transform.position);
            if (new_dist < dist && dist > 10)
            {
                dist = new_dist;
                index = i;
            }
        }

        if (index >= checkpoints.Length || index < 0) return (position, -1);
        return (checkpoints[index].transform.position, index);
    }

    /// <summary>
    /// Returns the length between two segments
    /// </summary>
    public float GetSegmenthLength(int index)
    {
        Transform a = checkpoints[index].transform;
        Transform b = checkpoints[(index+ 1) % checkpoints.Length].transform;
        return Vector3.Distance(a.position, b.position);
    }
}