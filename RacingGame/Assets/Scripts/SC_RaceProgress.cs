using UnityEngine;

public class SC_RaceProgress : MonoBehaviour
{
    public int current_checkpoint = 0;
    public int lap_count = 0;

    private int m_totalCheckpoints = 0;
    private SC_CheckpointManager m_checkpointManager;

    private SC_CarUI m_carUI;

    private void Start()
    {
        m_checkpointManager = SC_CheckpointManager.instance;
        m_totalCheckpoints = m_checkpointManager.checkpoints.Length;

        m_carUI = GetComponent<SC_CarUI>();
    }
    public void UpdateCurrentCheckpoint(int new_checkpoint)
    {
        if (current_checkpoint == new_checkpoint) return;

        if (new_checkpoint == m_checkpointManager.checkpoints.Length - 1 && current_checkpoint > 0)
        {
            lap_count++;
            current_checkpoint = 0;
            return;
        }

        current_checkpoint = new_checkpoint;
    }
    public float GetSegmentT()
    {
        int prev_index = (current_checkpoint - 1 + m_totalCheckpoints) % m_totalCheckpoints;
        Transform prev_checkpoint = m_checkpointManager.checkpoints[prev_index].transform;
        float segment_length = m_checkpointManager.GetSegmenthLength(prev_index);
        float dist_from_prev = Vector3.Distance(transform.position, prev_checkpoint.position);
        return segment_length > 0.001f ? Mathf.Clamp01(dist_from_prev / segment_length) : 0;
    }
    public float GetRaceProgress()
    {
        int prev_index = (current_checkpoint - 1 + m_checkpointManager.checkpoints.Length) % m_checkpointManager.checkpoints.Length;

        Transform prev_checkpoint = m_checkpointManager.checkpoints[prev_index].transform;
        Transform next_checkpoint = m_checkpointManager.checkpoints[current_checkpoint].transform;

        float segment_length = m_checkpointManager.GetSegmenthLength(prev_index);
        float dist_from_prev = Vector3.Distance(transform.position, prev_checkpoint.position);

        float segment_t = segment_length > 0.001f
            ? Mathf.Clamp01(dist_from_prev / segment_length)
            : 0;

        return lap_count * m_totalCheckpoints + current_checkpoint + segment_t;
    }

    public float DistanceToCurrentCheckpoint()
    {
        if (current_checkpoint > m_checkpointManager.checkpoints.Length) current_checkpoint = m_checkpointManager.checkpoints.Length - 1;
        Transform next = m_checkpointManager.checkpoints[current_checkpoint].transform;
        return Vector3.Distance(transform.position, next.position);
    }

    public void UpdateUIPosition(int position, int total_positions)
    {
        if (m_carUI == null) return;

        m_carUI.UpdatePlace(position, total_positions);
    }
}