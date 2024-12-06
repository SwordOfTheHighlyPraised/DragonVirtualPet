using UnityEngine;

public class BabyController : MonoBehaviour
{
    public Sprite[] idleSprites;
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

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        nextMoveTime = Time.time + moveInterval;

        if (idleSprites.Length != spriteWeights.Length)
        {
            Debug.LogError("Idle Sprites and Sprite Weights must have the same length!");
        }
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
        int selectedSpriteIndex = GetWeightedRandomIndex(spriteWeights);
        spriteRenderer.sprite = idleSprites[selectedSpriteIndex];
    }

    private int GetWeightedRandomIndex(float[] weights)
    {
        float totalWeight = 0;
        foreach (float weight in weights)
            totalWeight += weight;

        float randomValue = Random.Range(0, totalWeight);
        float cumulativeWeight = 0;
        for (int i = 0; i < weights.Length; i++)
        {
            cumulativeWeight += weights[i];
            if (randomValue <= cumulativeWeight)
            {
                return i;
            }
        }
        return weights.Length - 1;
    }
}
