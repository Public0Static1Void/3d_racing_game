using System.Collections;
using UnityEngine;

[CreateAssetMenu(menuName = "Cards/Effects/Duplicate")]
public class SC_DuplicateEffect : CardEffect
{
    public override void Activate(SC_Car player, float multiplier)
    {
        SC_DeckManager.instance.SetMultiplier(SC_DeckManager.instance.m_current_multiplier * 2);
    }
}