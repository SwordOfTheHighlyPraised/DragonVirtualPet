using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PetBehavior : MonoBehaviour
{
    public PetStatsSO petStats; // Reference to the ScriptableObject

    void Start()
    {
        // Example usage
        Debug.Log($"Hunger: {petStats.Hunger}");
    }

    public void FeedPet()
    {
        petStats.Feed();
        Debug.Log($"Fed pet! Hunger is now {petStats.Hunger}");
    }

    public void PutPetToSleep()
    {
        petStats.Sleep();
        Debug.Log($"Pet is rested! Energy is now {petStats.Energy}");
    }
}
