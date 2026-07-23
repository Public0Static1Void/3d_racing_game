using System.Collections;
using UnityEngine;

[CreateAssetMenu(menuName = "Cards/Effects/Ramp")]
public class SC_RampEffect : CardEffect
{
    public GameObject ob_ramp;
    public float offset = 10;
    public override void Activate(SC_Car player, float multiplier)
    {
        GameObject ob = Instantiate(ob_ramp, player.transform.position + player.transform.forward * offset, player.transform.rotation * Quaternion.Euler(70, 0, 0));
        ob.transform.localScale *= multiplier;
    }
}