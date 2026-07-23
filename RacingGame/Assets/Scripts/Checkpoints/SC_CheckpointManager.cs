using UnityEngine;
using System.Collections.Generic;

public class SC_CheckpointManager : MonoBehaviour
{
    public static SC_CheckpointManager instance {  get; private set; }

    [SerializeField] private List<SC_Checkpoint> checkpoints;

    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(this.gameObject);

        if (checkpoints.Count <= 1)
        {
            Debug.LogWarning("No checkpoints or only one stored");
        }
    }

    public Vector3 GetNextCheckpointPosition(int index)
    {
        return checkpoints[index % checkpoints.Count].transform.position;
    }
    public (Vector3, int) GetNearestCheckpoint(Vector3 position)
    {
        float dist = 10000;
        int index = 0;
        for (int i = 1; i < checkpoints.Count; i++)
        {
            float new_dist = Vector3.Distance(position, checkpoints[i].transform.position);
            if (new_dist < dist && dist > 10)
            {
                dist = new_dist;
                index = i;
            }
        }

        if (index >= checkpoints.Count || index < 0) return (position, -1);
        return (checkpoints[index].transform.position, index);
    }
}