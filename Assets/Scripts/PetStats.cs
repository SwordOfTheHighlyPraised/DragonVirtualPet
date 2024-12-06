using UnityEngine;

[CreateAssetMenu(fileName = "NewPetStats", menuName = "VirtualPet/PetStats", order = 1)]
public class PetStatsSO : ScriptableObject
{
    [Header("Pet Stats")]
    public int Age = 0;
    public int Weight = 5;
    public int Hunger = 10;
    public int Happiness = 10;
    public int Energy = 10;

    public void Feed()
    {
        Hunger = Mathf.Min(Hunger + 2, 10);
        Weight++;
    }

    public void Clean()
    {
        // Logic to clean and prevent sickness
    }

    public void Sleep()
    {
        Energy = 10;
    }
}
