using System.Collections.Generic;
using UnityEngine;

public class PersonaManager : MonoBehaviour
{
    [Header("Available Persona Types")]
    public List<PassengerPersona> personaPool;

    [Header("Active Shift Tracker")]
    public PassengerPersona lastBoardedPersona;

    public PassengerPersona GetRandomPersona()
    {
        if (personaPool == null || personaPool.Count == 0)
        {
            Debug.LogWarning("Persona pool is empty!");
            return null;
        }
        int index = Random.Range(0, personaPool.Count);
        lastBoardedPersona = personaPool[index];
        return lastBoardedPersona;
    }

    public float CalculatePassengerFare(PassengerPersona persona, float baseFare, float routeTimeRemaining)
    {
        if (persona == null) return baseFare;

        float finalFare = baseFare * persona.maxFareMultiplierTolerance;
        
        if (routeTimeRemaining > 100f)
        {
            finalFare *= persona.tipMultiplier;
        }

        return finalFare;
    }
}