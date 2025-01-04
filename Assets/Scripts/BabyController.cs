using UnityEngine;

public class BabyController : MonoBehaviour
{
    [Header("Idle Sprites - Baby")]
    public Sprite[] babyIdleSprites;

    [Header("Idle Sprites - Teen")]
    public Sprite[] teenIdleSprites;

    [Header("Idle Sprites - Adult")]
    public Sprite[] adultIdleSprites;

    public float[] spriteWeights;
    public float movementLimitLeft = -24f;
    public float movementLimitRight = 24f;
    public float moveInterval = 0.5f;
    public float moveAmount = 3f;

    private SpriteRenderer spriteRenderer;
    private float nextMoveTime;
    private bool canMoveLeft = true;
    private bool canMoveRight = true;

    public DragonStats dragonStats;
    public bool isFeedingAnimation = false; // defaults to false

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        nextMoveTime = Time.time + moveInterval;
    }

    void Update()
    {
        // If S is pressed
        if (Input.GetKeyDown(KeyCode.S))
        {
            if (dragonStats != null && dragonStats.IsSleeping)
            {
                // Dragon is sleeping, wake up
                dragonStats.WakeUp();
            }
            else
            {
                // Dragon is not sleeping, start a nap
                dragonStats.StartNap();
            }
        }

        // If the dragon is sleeping, do not move or switch sprites
        if (dragonStats != null && dragonStats.IsSleeping)
        {
            return;
        }

        // NEW: If feeding, skip idle/movement
        if (isFeedingAnimation) return;

        // Regular movement and sprite switching logic
        if (Time.time >= nextMoveTime)
        {
            MoveRandomly();
            SwitchIdleSprite();
            nextMoveTime = Time.time + moveInterval;
        }

        // Feeding with F key
        if (Input.GetKeyDown(KeyCode.F))
        {
            dragonStats.Feed();
        }

        // Playing with P key
        if (Input.GetKeyDown(KeyCode.P))
        {
            dragonStats.Play();
        }
    }

    private void MoveRandomly()
    {
        bool shouldMoveLeft = canMoveLeft && Random.Range(0, 2) == 0;
        bool shouldMoveRight = canMoveRight && !shouldMoveLeft;

        if (!shouldMoveLeft && !shouldMoveRight)
        {
            return;
        }

        float direction = shouldMoveLeft ? -moveAmount : moveAmount;
        float newPositionX = transform.position.x + direction;
        transform.position = new Vector3(newPositionX, transform.position.y, transform.position.z);
        UpdateMovementFlags(newPositionX);
    }

    private void UpdateMovementFlags(float currentPositionX)
    {
        canMoveLeft = currentPositionX > movementLimitLeft;
        canMoveRight = currentPositionX < movementLimitRight;
    }

    private void SwitchIdleSprite()
    {
        // Decide which idle sprite array to use
        Sprite[] currentIdleSet = babyIdleSprites; // default to baby
        if (dragonStats != null)
        {
            switch (dragonStats.currentStage)
            {
                case DragonStage.Teen:
                    currentIdleSet = teenIdleSprites;
                    break;
                case DragonStage.Adult:
                    currentIdleSet = adultIdleSprites;
                    break;
                case DragonStage.Baby:
                default:
                    currentIdleSet = babyIdleSprites;
                    break;
            }
        }

        if (currentIdleSet == null || currentIdleSet.Length == 0)
        {
            Debug.LogWarning("No idle sprites found for the current stage.");
            return;
        }

        if (spriteWeights != null && spriteWeights.Length == currentIdleSet.Length)
        {
            // Weighted selection
            int weightedIndex = GetWeightedRandomIndex(spriteWeights);
            spriteRenderer.sprite = currentIdleSet[weightedIndex];
        }
        else
        {
            // fallback: random pick
            int selectedIndex = Random.Range(0, currentIdleSet.Length);
            spriteRenderer.sprite = currentIdleSet[selectedIndex];
        }
    }

    private int GetWeightedRandomIndex(float[] weights)
    {
        float totalWeight = 0f;
        foreach (float weight in weights)
        {
            totalWeight += weight;
        }

        float randomValue = Random.Range(0, totalWeight);
        float cumulative = 0f;
        for (int i = 0; i < weights.Length; i++)
        {
            cumulative += weights[i];
            if (randomValue <= cumulative)
            {
                return i;
            }
        }
        return weights.Length - 1; // fallback
    }
}
