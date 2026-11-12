using UnityEngine;

[CreateAssetMenu(fileName = "NewPassengerPersona", menuName = "KombiShift/Passenger Persona")]
public class PassengerPersona : ScriptableObject
{
    [Header("Persona Identity")]
    public string personaName = "Commuter";
    public GameObject visualPrefab;

    [Header("Economy & Patience Settings")]
    [Range(0.5f, 3.0f)] public float maxFareMultiplierTolerance = 1.5f;
    public float patienceSeconds = 45f;
    public float tipMultiplier = 1.2f;
}