using UnityEngine;

[CreateAssetMenu(menuName = "Cards/Card")]
public class SC_CardData : ScriptableObject
{
    public string card_name = "None";
    public Sprite card_sprite;

    public CardEffect card_effect;
}

public abstract class CardEffect : ScriptableObject
{
    public abstract void Activate(SC_Car player, float multiplier);
}