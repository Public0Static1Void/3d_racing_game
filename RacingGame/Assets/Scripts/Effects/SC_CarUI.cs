using TMPro;
using UnityEngine;

[RequireComponent(typeof(SC_PhysicObject))]
public class SC_CarUI : MonoBehaviour
{
    private SC_PhysicObject m_car_physics;
    public TMP_Text txt_speed;
    public TMP_Text txt_place;
    void Start()
    {
        m_car_physics = GetComponent<SC_PhysicObject>();
    }

    void LateUpdate()
    {
        txt_speed.text = $"{m_car_physics.current_velocity * 40:F0} KM/H";
    }

    public void UpdatePlace(int new_place, int total_places)
    {
        txt_place.text = $"{new_place} / {total_places}";
    }
}