using System.Collections;
using UnityEngine;

[CreateAssetMenu(menuName = "Cards/Effects/FeatherBody")]
public class SC_FeatherBody : CardEffect
{
    public float feather_mass = 1;
    public override void Activate(SC_Car player, float multiplier)
    {
        player.StopCoroutine("ChangeCarMass");
        player.StartCoroutine(ChangeCarMass(player, multiplier));
    }

    private IEnumerator ChangeCarMass(SC_Car car, float multiplier)
    {
        float start_mass = car.start_mass;
        car.mass = feather_mass / multiplier;
        yield return new WaitForSeconds(3);
        car.mass = start_mass;
    }
}