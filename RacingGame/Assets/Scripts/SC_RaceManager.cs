using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

public class SC_RaceManager : MonoBehaviour
{
    private List<SC_RaceProgress> m_cars;
    private List<SC_RaceProgress> m_ranked = new List<SC_RaceProgress>();

    public List<GameObject> traffic_lights;
    private Material m_traffic_lights_material;

    public SC_CameraMovement m_mainCamera;

    public AudioClip clip_pip;
    public AudioClip clip_buzz;

    private void Start()
    {
        SC_RaceProgress[] progress = FindObjectsByType<SC_RaceProgress>(FindObjectsSortMode.None);
        m_cars = new List<SC_RaceProgress>(progress);

        SC_PhysicObject[] cars = new SC_PhysicObject[progress.Length];
        for (int i = 0; i < m_cars.Count; i++)
        {
            cars[i] = m_cars[i].GetComponent<SC_PhysicObject>();
            cars[i].can_drive = false;
        }

        m_traffic_lights_material = traffic_lights[0].GetComponent<MeshRenderer>().sharedMaterial;

        StartCoroutine(StartRaceCountdown(cars));
    }

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
            var sorted_group = group.OrderBy(r => r.DistanceToCurrentCheckpoint());
            foreach (var car in sorted_group)
            {
                car.UpdateUIPosition(position, total_cars);
                position++;
            }
        }
    }

    private IEnumerator StartRaceCountdown(SC_PhysicObject[] cars)
    {
        m_mainCamera.SetLookpoint(traffic_lights[0].transform);
        m_mainCamera.SetDistance(0.1f);

        yield return new WaitForSeconds(Random.Range(1f, 2f));

        float seconds = Random.Range(2.75f, 3.75f);

        // Activate the lights and changes their color
        m_traffic_lights_material.SetColor("_EmissionColor", Color.red * 10);
        for (int i = 0; i < traffic_lights.Count; i++)
        {
            SC_SoundManager.instance.InstantiateSound(clip_buzz, traffic_lights[i].transform.position, volume: 2);
            traffic_lights[i].SetActive(true);
            yield return new WaitForSeconds(seconds / 3);
        }

        SC_SoundManager.instance.InstantiateSound(clip_buzz, traffic_lights[2].transform.position, volume: 2, pitch: 0.5f);

        m_traffic_lights_material.SetColor("_EmissionColor", Color.green * 10);

        // Restores the camera lookpoint and enables the cars movement
        m_mainCamera.SetLookpoint(null);
        m_mainCamera.SetDistance(-1);

        foreach (var car in cars)
        {
            car.can_drive = true;
        }

        yield return new WaitForSeconds(10);
        foreach (var ob in traffic_lights)
        {
            ob.SetActive(false);
        }
    }
}