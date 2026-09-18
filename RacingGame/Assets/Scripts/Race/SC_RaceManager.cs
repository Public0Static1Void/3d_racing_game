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

    public List<SC_PhysicObject> sorted_cars = new List<SC_PhysicObject>();

    public List<GameObject> traffic_lights;
    private Material m_traffic_lights_material;

    public SC_CameraMovement m_mainCamera;

    public AudioClip clip_pip;
    public AudioClip clip_buzz;

    private void Start()
    {
        Debug.Log("Execution");
        SC_RaceProgress[] progress = FindObjectsByType<SC_RaceProgress>(FindObjectsSortMode.None);
        if (progress.Length <= 0) return;

        m_cars = new List<SC_RaceProgress>(progress);

        SC_PhysicObject[] cars = new SC_PhysicObject[progress.Length];
        for (int i = 0; i < m_cars.Count; i++)
        {
            cars[i] = m_cars[i].GetComponent<SC_PhysicObject>();
            cars[i].can_drive = false;
        }

        m_traffic_lights_material = traffic_lights[0].GetComponent<MeshRenderer>().sharedMaterial;

        if (cars != null && cars.Length > 0)
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

        sorted_cars.Clear();

        int total_cars = m_ranked.Count;
        int position = 1;
        SC_PhysicObject[] near_cars_list = new SC_PhysicObject[SC_Car_npc_predictive.MAX_NEAR_CARS];

        foreach (var group in groups)
        {
            var sorted_group = group.OrderBy(r => r.DistanceToCurrentCheckpoint());

            foreach (var car in sorted_group)
            {
                car.UpdateUIPosition(position, total_cars);
                position++;
                SC_PhysicObject physic = car.GetPhysicObject();
                sorted_cars.Add(physic);

                // Calculate the pressure for this car
                int near_cars = 0;
                foreach (var other_car in sorted_group)
                {
                    if (car == other_car) continue;
                    // Stop making calculations if the limit is reached
                    if (near_cars >= near_cars_list.Length) break;

                    SC_PhysicObject other_car_physics = other_car.GetPhysicObject();

                    Vector3 diff = physic.Current_Position - other_car_physics.Current_Position;
                    float distance = diff.magnitude;
                    if (distance < physic.pressure_range)
                    {
                        near_cars_list[near_cars] = other_car_physics;
                        near_cars++;
                    }
                }
                car.UpdatePressure((float)near_cars / (float)total_cars, near_cars_list);
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
            if (car == null) continue;
            car.can_drive = true;
        }

        yield return new WaitForSeconds(10);
        foreach (var ob in traffic_lights)
        {
            ob.SetActive(false);
        }
    }
}