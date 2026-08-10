using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

public class SC_RaceManager : MonoBehaviour
{
    private List<SC_RaceProgress> m_cars;
    private List<SC_RaceProgress> m_ranked = new List<SC_RaceProgress>();

    private void Start()
    {
        SC_RaceProgress[] progress = FindObjectsByType<SC_RaceProgress>(FindObjectsSortMode.None);
        m_cars = new List<SC_RaceProgress>(progress);
    }

    // Update is called once per frame
    private void Update()
    {
        m_ranked.Clear();
        m_ranked.AddRange(m_cars);

        // Store the cars by their checkpoints
        int totalCheckpoints = SC_CheckpointManager.instance.checkpoints.Length;

        var groups = m_ranked
            .GroupBy(r => r.lap_count * totalCheckpoints + r.current_checkpoint) // Sums the current checkpoint to the total checkpoints * lap (lap 1 checkpoint 0 > lap 0 checkpoint 0)
            .OrderByDescending(g => g.Key);

        int total_cars = m_ranked.Count;
        int position = 1;
        foreach (var group in groups)
        {
            var sorted_group = group.OrderBy(r => r.DistanceToNextCheckpoint());
            foreach (var car in sorted_group)
            {
                car.UpdateUIPosition(position, total_cars);
                position++;
            }
        }
    }
}