using UnityEngine;

[System.Serializable]
public class SC_NPC_DrivingProfile
{
    [Header("Skill")]
    [Range(0f, 1f)]
    public float drivingSkill = 0.8f;

    [Range(0f, 1f)]
    public float brakingSkill = 0.8f;

    [Range(0f, 1f)]
    public float corneringSkill = 0.8f;

    [Header("Personality")]
    [Range(0f, 1f)]
    public float aggression = 0.5f;

    [Range(0f, 1f)]
    public float riskTolerance = 0.5f;

    [Range(0f, 1f)]
    public float defensive = 0.5f;

    [Range(0f, 1f)]
    public float opportunistic = 0.5f;

    [Header("Consistency")]
    [Range(0f, 1f)]
    public float consistency = 0.8f;

    [Header("Reaction")]
    public float reactionTime = 0.15f;
}
