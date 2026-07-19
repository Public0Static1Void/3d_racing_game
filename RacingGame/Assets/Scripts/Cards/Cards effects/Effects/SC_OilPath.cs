using System.Collections;
using UnityEngine;

[CreateAssetMenu(menuName = "Cards/Effects/Oil path")]
public class SC_OilPath : CardEffect
{
    public override void Activate(SC_Car player, float multiplier)
    {
        player.StartCoroutine(OilPathRoutine(player));
    }
    private IEnumerator OilPathRoutine(SC_Car player)
    {
        player.ground_friction = 1;
        player.side_grip_straight = 1;
        player.side_grip_turn = 1;
        yield return new WaitForSeconds(5);
        player.ground_friction = 0.99f;
        player.side_grip_straight = 0.3f;
        player.side_grip_turn = 0.8f;
    }
}