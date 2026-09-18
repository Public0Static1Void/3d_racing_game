using UnityEngine;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Linq;

public class SC_RaceTrack : MonoBehaviour
{
    public static SC_RaceTrack instance {  get; private set; }

    [SerializeField] private List<SC_RacePoint> points = new List<SC_RacePoint>();

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        for (int i = 0; i < points.Count; i++)
        {
            points[i].index = i;
        }
    }

    public int PointCount => points.Count;

    public int GetNextIndex(int index) => (index + 1) % PointCount;

    public Vector3 GetPoint(int index) => points[index % PointCount].transform.position;

    public SC_RacePoint GetRacePoint(int index) => points[index % PointCount];

    public Vector3 GetForward(int index)
    {
        int next = GetNextIndex(index);
        return (GetPoint(next) - GetPoint(index)).normalized;
    }

    public float GetPointDistance(int index)
    {
        int next = GetNextIndex(index);
        return Vector3.Distance(GetPoint(index), GetPoint(next));
    }

    public int GetClosestPoint(Vector3 position)
    {
        int closest = 0;
        float closest_distance = float.MaxValue;

        for (int i = 0; i < PointCount; i++)
        {
            float distance = Vector3.SqrMagnitude(position - GetPoint(i));
            if (distance < closest_distance)
            {
                closest_distance = distance;
                closest = i;
            }
        }

        return closest;
    }

    public Vector3 GetSegmentCurve(Vector3 p0, Vector3 p0_forward, Vector3 p1, float t)
    {
        if (t == 0) return p0;

        float dist = (p0 - p1).magnitude;
        Vector3 control = p0 + p0_forward * dist * 0.5f;

        float u = 1 - t;
        return u * u * p0 + 2 * u * t * control + t * t * p1;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;

        Vector3[] ar_points = new Vector3[points.Count];
        for (int i = 0; i < points.Count; i++)
        {
            ar_points[i] = points[i].transform.position + points[i].transform.right * points[i].lateralOffset;
        }

        Gizmos.DrawLineStrip(ar_points, true);
    }
}