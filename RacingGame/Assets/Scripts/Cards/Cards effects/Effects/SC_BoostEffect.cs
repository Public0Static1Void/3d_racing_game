using System.Collections;
using UnityEngine;

[CreateAssetMenu(menuName = "Cards/Effects/Boost")]
public class SC_BoostEffect : CardEffect
{
    public float boost_force = 10;
    public override void Activate(SC_Car player, float multiplier)
    {
        player.Boost(boost_force * multiplier);
    }
}