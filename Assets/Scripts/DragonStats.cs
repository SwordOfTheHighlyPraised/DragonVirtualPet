using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine.SceneManagement;

public enum DragonStage
{
    Baby,
    Teen,
    Adult,
    Dead
}

public class DragonStats : MonoBehaviour
{
    [Header("Basic Stats")]
    public int Age = 0;
    public int Weight = 5;
    public int Happiness = 4;
    public int Hunger = 4;
    public int Energy = 10;

    [Header("Poop System")]
    public SpriteRenderer poopSprite1;
    public SpriteRenderer poopSprite2;
    public Sprite poopSpriteNormal;
    public Sprite poopSpriteChanged;
    private int poopCount = 0;
    private float poopTimer = 0f;
    private const float PoopInterval = 20f; // 30 minutes

    [Header("Death System")]
    public Sprite gravestoneSprite;
    public bool isDead = false;
    public AudioClip deathSound;

    [Header("Warning System")]
    public GameObject warningUIElement;
    public AudioClip warningSound;
    private float warningTimer = 0f;
    private const float WarningInterval = 900f; // 15 minutes
    private bool immediateAlertTriggered = false;

    [Header("Evolution Base Weights")]
    public int baseWeightBaby = 5;  // The weight the baby starts with
    public int baseWeightTeen = 10; // The base weight upon first evolving to teen
    public int baseWeightAdult = 20; // The base weight upon first evolving to adult

    private bool hasHatched = false;
    private bool isSleeping = false;
    public bool IsSleeping => isSleeping;

    private DateTime wakeUpTime;
    private float timeSinceLastUpdate = 0f;
    private const float statReductionInterval = 60f;

    private Animator animator;
    private Vector3 defaultSleepPosition = Vector3.zero;

    // Idle animation switching
    private float idleSwitchInterval = 0.5f;
    private float nextIdleSwitchTime = 0f;
    private int idleFrameCount = 2;

    [Header("Animation Sprites")]
    public SpriteRenderer dragonSpriteRenderer; // The SpriteRenderer used to show the dragon

    // Baby stage
    public Sprite[] babyEatFrames;
    public Sprite[] babyHappyFrames;
    public Sprite[] babyRefuseFrames;
    public Sprite[] babySleepFrames;

    // Teen stage
    public Sprite[] teenEatFrames;
    public Sprite[] teenHappyFrames;
    public Sprite[] teenRefuseFrames;
    public Sprite[] teenSleepFrames;

    // Adult stage
    public Sprite[] adultEatFrames;
    public Sprite[] adultHappyFrames;
    public Sprite[] adultRefuseFrames;
    public Sprite[] adultSleepFrames;

    // Food Sprites (optional)
    public SpriteRenderer foodSpriteRenderer; // A SpriteRenderer for the food item
    public Sprite[] fishEatSprites;
    public Sprite[] cakeEatSprites;

    public BabyController babyController;
    public AudioSource audioSource;
    public AudioClip happySound;
    public AudioClip refuseSound;

    [Header("Evolution and Death")]
    public DragonStage currentStage = DragonStage.Baby;

    // track how long we've been in the current stage (seconds)
    public float stageTimer = 0f;

    // track how long we've had hunger=0 and happiness=0 (if teen or adult)
    public float neglectTimer = 0f;
    private const float NEGLECT_DEATH_TIME = 10800f; // 3 hours

    [Header("Evolution Screen")]
    public SpriteRenderer evolveScreenSpriteRenderer; // A separate sprite for fade in/out
    public float fadeInTime = 1f;
    public float fadeOutTime = 1f;
    public Color evolveScreenColor = Color.white; // The tint color for the fade sprite
    public AudioClip evolveSound;
    private bool isEvolving = false;
    public UIController uiController;

    public Coroutine eatRoutine;
    public bool isEatCancelled = false;
    private bool isAnimationPlaying = false; // Tracks if any animation is currently playing
    public bool IsAnimationPlaying => isAnimationPlaying; // Expose it for other scripts
    private bool statsApplied = false;

    void Start()
    {
        animator = GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogError("No Animator component found! Please attach an Animator.");
        }

        nextIdleSwitchTime = Time.time + idleSwitchInterval;

        // Initialize poop and warning systems
        if (poopSprite1) poopSprite1.enabled = false;
        if (poopSprite2) poopSprite2.enabled = false;
    }

    void Update()
    {
        if (isDead) return;

        DateTime now = DateTime.Now;
        Hatch();

        if (currentStage == DragonStage.Dead)
        {
            // Once dead, do nothing else
            return;
        }

        if (hasHatched && !isSleeping)
        {
            // Stat reduction over time
            timeSinceLastUpdate += Time.deltaTime;
            if (timeSinceLastUpdate >= statReductionInterval)
            {
                ReduceStats();
                timeSinceLastUpdate -= statReductionInterval;
            }

            poopTimer += Time.deltaTime;
            if (poopTimer >= PoopInterval && !isAnimationPlaying)
            {
                poopTimer -= PoopInterval;
                AddPoop();
            }

            if ((Hunger == 0 || Happiness == 0) && !immediateAlertTriggered)
            {
                StartCoroutine(TriggerImmediateAlert());
                immediateAlertTriggered = true; // Prevent multiple immediate alerts
            }

            // Reset immediate alert flag when conditions improve
            if (Hunger > 0 && Happiness > 0)
            {
                immediateAlertTriggered = false;
            }

            // Idle frame switching if needed
            if (Time.time >= nextIdleSwitchTime)
            {
                SwitchIdleFrame();
                nextIdleSwitchTime = Time.time + idleSwitchInterval;
            }


            // 3) Stage Timer - track how long in this stage
            stageTimer += Time.deltaTime;
            CheckEvolution(); // see if we should evolve based on stageTimer

            // 4) Neglect Timer - only for Teen or Adult
            if ((currentStage == DragonStage.Teen || currentStage == DragonStage.Adult))
            {
                if (Hunger == 0 || Happiness == 0)
                {
                    neglectTimer += Time.deltaTime;
                    if (neglectTimer >= NEGLECT_DEATH_TIME)
                    {
                        // Dragon dies
                        Die();
                    }
                }
                else
                {
                    // reset if hunger>0 or happiness>0
                    neglectTimer = 0f;
                }
            }

        }

        // Force sleep at 9pm
        if (now.Hour == 21 && now.Minute == 0 && now.Second == 0)
        {
            StartSleep();
        }

        // Wake up when the set wakeUpTime passes
        if (isSleeping && DateTime.Now >= wakeUpTime)
        {
            WakeUp();
        }

        if (currentStage == DragonStage.Dead) return;
    }

    public void Hatch()
    {
        hasHatched = true;
    }

    private void CheckEvolution()
    {
        // If stage is Baby and we have been in Baby for 1 hour => evolve to Teen
        if (currentStage == DragonStage.Baby && stageTimer >= 600f && !isAnimationPlaying)
        {
            EvolveToTeen();
        }
        else if (currentStage == DragonStage.Baby && stageTimer >= 600f && isAnimationPlaying)
        {
            Debug.Log("Delaying evolution to Teen until animation finishes.");
            StartCoroutine(QueueEvolution(DragonStage.Teen));
        }

        // If stage is Teen and we have been in Teen for 3 hours => evolve to Adult
        if (currentStage == DragonStage.Teen && stageTimer >= 3600f && !isAnimationPlaying)
        {
            EvolveToAdult();
        }
        else if (currentStage == DragonStage.Teen && stageTimer >= 3600f && isAnimationPlaying)
        {
            Debug.Log("Delaying evolution to Adult until animation finishes.");
            StartCoroutine(QueueEvolution(DragonStage.Adult));
        }
    }

    private IEnumerator QueueEvolution(DragonStage targetStage)
    {
        while (isAnimationPlaying)
        {
            yield return null; // Wait for the current animation to finish
        }

        if (targetStage == DragonStage.Teen)
        {
            EvolveToTeen();
        }
        else if (targetStage == DragonStage.Adult)
        {
            EvolveToAdult();
        }
    }

    private void EvolveToTeen()
    {
        if (isEvolving) return;
        isEvolving = true;
        int overage = Weight - baseWeightBaby;
        Weight = baseWeightTeen + overage;
        Debug.Log("Evolving from Baby to Teen.");
        StartCoroutine(PlayEvolutionScreenAnimation(DragonStage.Teen));
    }

    private void EvolveToAdult()
    {
        if (isEvolving) return;
        isEvolving = true;
        int overage = Weight - baseWeightTeen;
        Weight = baseWeightAdult + overage;
        Debug.Log("Evolving from Teen to Adult.");
        StartCoroutine(PlayEvolutionScreenAnimation(DragonStage.Adult));
    }

    public void Die()
    {
        isDead = true;

        Debug.Log("Dragon has died from neglect...");
        currentStage = DragonStage.Dead;
        
        transform.position = new Vector3(0f, transform.position.y, transform.position.z);
        
        // Play the death sound
        if (audioSource != null && deathSound != null)
        {
            audioSource.PlayOneShot(deathSound);
        }

        // Possibly remove the sprite, or set a 'dead' sprite
        if (dragonSpriteRenderer)
        {
            dragonSpriteRenderer.sprite = gravestoneSprite;
        }
        // disable movement, animations, UI
        if (animator) animator.enabled = false;
        if (babyController) babyController.enabled = false;
    }

    void OnEnable()
    {
        if (isSleeping)
        {
            // Ensure position is reset to x = 0 when the object is re-enabled
            transform.position = new Vector3(0f, transform.position.y, transform.position.z);

            // Start the sleep animation to ensure it resumes correctly
            StartCoroutine(PlaySleepAnimation());
        }
    }

    void OnDisable()
    {
        if (isSleeping)
        {
            // Stop the sleep animation to avoid continuing it while inactive
            StopCoroutine(PlaySleepAnimation());
        }
    }

    public void StartNap()
    {
        if (currentStage == DragonStage.Dead)
        {
            Debug.Log("Cannot nap. Dragon is dead.");
            return;
        }

        isSleeping = true;
        DateTime now = DateTime.Now;

        if (now.Hour >= 7 && now.Hour < 21) // Nap allowed between 7am and 9pm
        {
            Debug.Log("Nap conditions met. Taking a nap...");
            isSleeping = true;
            wakeUpTime = now.AddHours(3); // Naps last 3 hours

            if (wakeUpTime.Hour >= 21)
            {
                Debug.Log("Nap overlaps with 9pm. Transitioning to sleep...");
                StartSleep();
            }
            else
            {
                transform.position = new Vector3(0f, transform.position.y, transform.position.z);
                StartCoroutine(PlaySleepAnimation());
            }
        }
        else
        {
            Debug.Log("Naps are not allowed at this time. Forcing sleep...");
            StartSleep();
        }

    }

    public void StartSleep()
    {
        DateTime now = DateTime.Now;
        Debug.Log("Dragon is going to sleep...");
        isSleeping = true;
        GoToSleep(defaultSleepPosition);

        if (now.Hour >= 21)
        {
            wakeUpTime = now.Date.AddDays(1).AddHours(7);
        }
        else if (now.Hour < 7)
        {
            wakeUpTime = now.Date.AddHours(7);
        }

        // Start the sleep animation
        StartCoroutine(PlaySleepAnimation());

    }

    private void GoToSleep(Vector3 sleepPosition)
    {
        Debug.Log($"Moving dragon to sleep position: {sleepPosition}");
        transform.position = sleepPosition;

        if (animator != null)
        {
            Debug.Log("Setting Animator parameter IsSleeping = true.");
            animator.SetBool("IsSleeping", true);
        }
    }

    public void WakeUp()
    {
        if (!isSleeping)
        {
            Debug.Log("Dragon is already awake.");
            return;
        }

        isSleeping = false;
        Debug.Log("Dragon has woken up!");

        Energy = 10; // Restore full energy

        // Age only after sleep (not nap)
        if (wakeUpTime.Hour == 7)
        {
            Age++;
            Debug.Log($"Dragon has aged! New age: {Age}");
        }

        // Stop the sleep animation
        StopCoroutine(PlaySleepAnimation());

        // Reset to an idle frame
        string stage = GetStage();
        Sprite[] idleFrames = GetIdleFrames(stage);
        if (dragonSpriteRenderer != null && idleFrames != null && idleFrames.Length > 0)
        {
            dragonSpriteRenderer.sprite = idleFrames[0];
        }

    }

    private void ReduceStats()
    {
        Happiness = Mathf.Max(Happiness - 1, 0);
        Hunger = Mathf.Max(Hunger - 1, 0);
        Debug.Log($"Stats reduced: Happiness={Happiness}, Hunger={Hunger}");
    }

    public void Feed()
    {
        if (Hunger < 8)
        {
            Hunger = Mathf.Min(Hunger + 2, 8);
            Weight++; // Gain weight
        }
        else
        {
            Debug.Log("Dragon is full!");
        }
    }

    public void Cake()
    {
        Weight++; // Gain weight
        if (Happiness < 8)
        {
            Happiness = Mathf.Min(Happiness + 2, 8);
        }
    }

    public void StartFeedingAnimation(int feedIndex)
    {

        // If the stage is Dead, no feeding
        if (currentStage == DragonStage.Dead)
        {
            Debug.Log("Dragon is dead, cannot feed.");
            return;
        }

        isEatCancelled = false;

        // If Hunger is max (8), do a refusal
        if (feedIndex == 0 && Hunger >= 8)
        {
            Debug.Log("Hunger is already 8; refusing to eat.");
            // Start the short refusal animation instead
            if (babyController) babyController.enabled = false;
            if (animator) animator.enabled = false;
            StartCoroutine(PlayRefuseAnimation());
            return;
        }

        // feedIndex = 0 => Fish, 1 => Cake
        // Called by UI code when user selects feed choice
        if (babyController) babyController.enabled = false;
        babyController.isFeedingAnimation = true;
        eatRoutine = StartCoroutine(PlayFeedingAnimation(feedIndex));
        StartCoroutine(PlayFeedingAnimation(feedIndex));
        UpdatePoopVisibility(); // Hide poop

    }

    public void ForceEndEating()
    {
        isEatCancelled = true;

        if (eatRoutine != null)
        {
            StopCoroutine(eatRoutine);
            eatRoutine = null;
        }
        // Cleanup: remove the food sprite, revert to idle, etc.
        if (foodSpriteRenderer != null)
            foodSpriteRenderer.sprite = null;



        // Re-enable idle logic
        if (babyController) babyController.enabled = true;
        babyController.isFeedingAnimation = false;

        if (uiController != null)
            uiController.isEatingLock = false;

        UpdatePoopVisibility(); // Show poop after ending feeding
    }

    private IEnumerator PlayFeedingAnimation(int feedIndex)
    {
        isAnimationPlaying = true; // Animation starts
        statsApplied = false; // Reset the flag at the start of the animation

        if (babyController) babyController.enabled = false;
        if (animator) animator.enabled = false;

        // 1) Move dragon to X=12 (keeping same Y, Z)
        Vector3 oldPos = transform.position;
        transform.position = new Vector3(12f, oldPos.y, oldPos.z);

        // 3) Wait 1 second
        yield return new WaitForSeconds(0f);

        // Determine correct stage
        string stage = GetStage();
        Sprite[] eatFrames = GetEatFrames(stage);   // e.g., babyEatFrames
        Sprite[] happyFrames = GetHappyFrames(stage); // e.g., babyHappyFrames

        // Decide if we’re feeding fish (feedIndex == 0) or cake (feedIndex == 1)
        bool isFish = (feedIndex == 0);
        bool isCake = (feedIndex == 1);


        float eatDuration = 8f;
        float eatElapsed = 0f;
        int eatFrameIndex = 0;
        // Time each frame stays visible; adjust as desired
        float eatFrameTime = 1f;

        Vector3 foodOldPos = Vector3.zero;

        Sprite[] currentFoodSprites = null;

        if (!statsApplied)
        {

            if (isFish && fishEatSprites != null && fishEatSprites.Length > 0)
            {
                currentFoodSprites = fishEatSprites;
                // Also apply the stat changes in DragonStats (e.g., Feed())
                Feed();
                Debug.Log("Feeding fish (applying stats).");
            }
            else if (isCake && cakeEatSprites != null && cakeEatSprites.Length > 0)
            {
                currentFoodSprites = cakeEatSprites;
                Cake();
                Debug.Log("Feeding cake (applying stats).");
            }
            statsApplied = true;
        }

        // If fish or cake was chosen, show or apply stats now
        if (foodSpriteRenderer != null)
        {
            foodOldPos = foodSpriteRenderer.transform.position;

            // Place the food sprite at X = -27, Y = -12, same Z
            Vector3 oldFoodPos = foodSpriteRenderer.transform.position;
            foodSpriteRenderer.transform.position = new Vector3(
                -27f,
                -12f,
                oldFoodPos.z
            );
        }

        Debug.Log("Starting looped eat animation at X=12.");

        while (eatElapsed < eatDuration)
        {
            if (isEatCancelled)
            {
                Debug.Log("Eat animation cancelled mid-loop");
                // remove food sprite
                if (foodSpriteRenderer != null)
                    foodSpriteRenderer.sprite = null;
                yield break;
            }

            if (dragonSpriteRenderer != null && eatFrames != null && eatFrames.Length > 0)
            {
                // Show current frame
                dragonSpriteRenderer.sprite = eatFrames[eatFrameIndex];

                // Advance frame index
                eatFrameIndex = (eatFrameIndex + 1) % eatFrames.Length;
            }
            else
            {
                // If no frames or references, break out
                Debug.LogWarning("Eat frames not assigned properly.");
                break;
            }

            if (foodSpriteRenderer != null && currentFoodSprites != null && currentFoodSprites.Length > 0)
            {
                // Each sprite is shown for eatDuration / currentFoodSprites.Length seconds
                // e.g. 8 / 4 = 2s per sprite
                float segmentTime = eatDuration / currentFoodSprites.Length;
                // Convert the elapsed time to an index 0..(length-1)
                int spriteIndex = (int)(eatElapsed / segmentTime);
                spriteIndex = Mathf.Clamp(spriteIndex, 0, currentFoodSprites.Length - 1);

                foodSpriteRenderer.sprite = currentFoodSprites[spriteIndex];
            }

            yield return new WaitForSeconds(eatFrameTime);
            eatElapsed += eatFrameTime;
        }
        // 5) Remove the food sprite after finishing
        if (foodSpriteRenderer != null)
        {
        foodSpriteRenderer.sprite = null;
        }

        eatRoutine = null;

        // 6) Move the dragon to X=0 for the happy animation
        transform.position = new Vector3(0f, oldPos.y, oldPos.z);

        // 6) Loop the happy animation for e.g. 3 seconds
        float happyDuration = 3f;
        float happyElapsed = 0f;
        int happyFrameIndex = 0;
        // Time per happy frame
        float happyFrameTime = 0.5f;

        if (audioSource != null && happySound != null)
        {
            // PlayOneShot() will play the sound once.
            // If you want to loop, you can set audioSource.loop = true beforehand
            // and stop it after the while loop ends.
            audioSource.PlayOneShot(happySound);
        }

        Debug.Log("Now looping happy animation at X=0.");

        while (happyElapsed < happyDuration)
        {
            if (dragonSpriteRenderer != null && happyFrames != null && happyFrames.Length > 0)
            {
                dragonSpriteRenderer.sprite = happyFrames[happyFrameIndex];
                happyFrameIndex = (happyFrameIndex + 1) % happyFrames.Length;
            }
            else
            {
                Debug.LogWarning("Happy frames not assigned properly.");
                break;
            }

            yield return new WaitForSeconds(happyFrameTime);
            happyElapsed += happyFrameTime;
        }

        // 7) Restore original position if desired (or leave at X=0)
        transform.position = oldPos;
        if (uiController != null) uiController.isEatingLock = false;
        Debug.Log("Done feeding animation.");

        // 8) Re-enable babyController so idle logic can resume
        if (babyController) babyController.enabled = true;
        babyController.isFeedingAnimation = false;

        isAnimationPlaying = false; // Animation ends

        UpdatePoopVisibility(); // Show poop after ending feeding

    }

    private Sprite[] GetIdleFrames(string stage)
    {
        switch (stage)
        {
            case "Baby": return babyController.babyIdleSprites;
            case "Teen": return babyController.teenIdleSprites;
            case "Adult": return babyController.adultIdleSprites;
            default: return babyController.babyIdleSprites; // Fallback to baby frames
        }
    }

    private Sprite[] GetEatFrames(string stage)
    {
        if (stage == "Baby") return babyEatFrames;
        if (stage == "Teen") return teenEatFrames;
        if (stage == "Adult") return adultEatFrames;
        return babyEatFrames;
    }

    private Sprite[] GetHappyFrames(string stage)
    {
        if (stage == "Baby") return babyHappyFrames;
        if (stage == "Teen") return teenHappyFrames;
        if (stage == "Adult") return adultHappyFrames;
        return babyHappyFrames;
    }

    private Sprite[] GetRefuseFrames(DragonStage stage)
    {
        switch (stage)
        {
            case DragonStage.Baby: return babyRefuseFrames;
            case DragonStage.Teen: return teenRefuseFrames;
            case DragonStage.Adult: return adultRefuseFrames;
            default: return babyRefuseFrames; // fallback
        }
    }
    private Sprite[] GetSleepFrames(string stage)
    {
        switch (stage)
        {
            case "Baby": return babySleepFrames;
            case "Teen": return teenSleepFrames;
            case "Adult": return adultSleepFrames;
            default: return babySleepFrames; // Fallback to baby frames
        }
    }

    private IEnumerator PlayRefuseAnimation()
    {
        isAnimationPlaying = true; // Animation ends

        // Move dragon to X=0 (assuming oldPos is the current position)
        Vector3 oldPos = transform.position;
        transform.position = new Vector3(0f, oldPos.y, oldPos.z);

        Debug.Log("Playing refusal animation at X=0.");

        // Decide which frames to use based on stage
        string stage = GetStage();
        Sprite[] refuseFrames = GetRefuseFrames(currentStage);
        if (refuseFrames == null || refuseFrames.Length == 0)
        {
            Debug.LogWarning("No refuse frames assigned, just waiting 3s...");
            yield return new WaitForSeconds(3f);
        }
        else
        {
            // 3-second refusal
            float refuseDuration = 3f;
            float elapsed = 0f;
            int frameIndex = 0;


            if (audioSource != null && refuseSound != null)
            {
                // PlayOneShot() will play the sound once.
                // If you want to loop, you can set audioSource.loop = true beforehand
                // and stop it after the while loop ends.
                audioSource.PlayOneShot(refuseSound);
            }

            // If you want e.g. 2 frames total, each shown for 1 second, repeated, 
            // that’s about a 1-second cycle repeated thrice. Adjust as needed.
            while (elapsed < refuseDuration)
            {
                dragonSpriteRenderer.sprite = refuseFrames[frameIndex];
                frameIndex = (frameIndex + 1) % refuseFrames.Length;

                // Wait 1 second (or 0.5f, or whatever time you want per frame)
                float frameTime = 1f;
                yield return new WaitForSeconds(frameTime);
                elapsed += frameTime;
            }

        }

        // Return to old position if desired
        transform.position = oldPos;

        // Re-enable idle logic
        if (babyController) babyController.enabled = true;
        babyController.isFeedingAnimation = false;
        isAnimationPlaying = false; // Animation ends
        Debug.Log("Refusal animation done, returning to normal.");
    }

    private IEnumerator PlaySleepAnimation()
    {
        isAnimationPlaying = true; // Lock other animations
        string stage = GetStage();
        Sprite[] sleepFrames = GetSleepFrames(stage);

        if (sleepFrames == null || sleepFrames.Length == 0)
        {
            Debug.LogWarning("No sleep frames assigned.");
            yield break;
        }

        float frameTime = 1f; // Adjust frame duration as needed
        int frameIndex = 0;

        while (isSleeping)
        {
            if (dragonSpriteRenderer != null)
            {
                dragonSpriteRenderer.sprite = sleepFrames[frameIndex];
                frameIndex = (frameIndex + 1) % sleepFrames.Length;
            }
            else
            {
                Debug.LogError("DragonSpriteRenderer is not assigned!");
            }

            yield return new WaitForSeconds(frameTime);
        }

        isAnimationPlaying = false; // Unlock animations
    }

    private IEnumerator PlayEvolutionScreenAnimation(DragonStage newStage)
    {
        UpdatePoopVisibility();

        if (uiController != null) uiController.isEvolutionLock = true;

        if (dragonSpriteRenderer != null)
            dragonSpriteRenderer.enabled = false;

        if (evolveScreenSpriteRenderer != null)
        {
            // Start fully transparent
            Color c = evolveScreenColor;
            c.a = 0f;
            evolveScreenSpriteRenderer.color = c;
            evolveScreenSpriteRenderer.gameObject.SetActive(true);
        }

        float elapsed = 0f;
        while (elapsed < fadeInTime)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeInTime);

            if (evolveScreenSpriteRenderer != null)
            {
                Color col = evolveScreenColor;
                col.a = t; // alpha goes from 0 to 1
                evolveScreenSpriteRenderer.color = col;
            }

            yield return null;
        }

        yield return new WaitForSeconds(0.5f);
        
        if (audioSource != null && evolveSound != null)
        {
            audioSource.PlayOneShot(evolveSound);
            Debug.Log("Playing evolve sound.");
        }

        elapsed = 0f;
        while (elapsed < fadeOutTime)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeOutTime);

            if (evolveScreenSpriteRenderer != null)
            {
                Color col = evolveScreenColor;
                // alpha goes from 1 down to 0
                col.a = 1f - t;
                evolveScreenSpriteRenderer.color = col;
            }
            yield return null;
        }

        if (evolveScreenSpriteRenderer != null)
        {
            evolveScreenSpriteRenderer.gameObject.SetActive(false);
        }

        SetStage(newStage); // We'll define a helper or call your EvolveToTeen/EvolveToAdult logic here

        // 4) Show the newly evolved dragon
        if (dragonSpriteRenderer != null)
        {
            dragonSpriteRenderer.enabled = true;
        }

        isEvolving = false;
        Debug.Log($"Evolution to {newStage} complete!");
        UpdatePoopVisibility();
        if (uiController != null) uiController.isEvolutionLock = false;
    }
    private void SetStage(DragonStage newStage)
    {
        currentStage = newStage;
        stageTimer = 0f;
        Debug.Log($"Stage is now {currentStage}");
    }
    public void Play()
    {
        if (Happiness < 8 && Energy > 2)
        {
            Happiness = Mathf.Min(Happiness + 2, 8);
            Energy = Mathf.Max(Energy - 2, 0);
        }
    }

    private void AddPoop()
    {
        poopCount++;
        switch (poopCount)
        {
            case 1:
                if (poopSprite1)
                {
                    poopSprite1.sprite = poopSpriteNormal;
                    poopSprite1.transform.position = new Vector3(-36f, -10f, 0f);
                    poopSprite1.enabled = true;
                }
                break;
            case 2:
                if (poopSprite2)
                {
                    poopSprite2.sprite = poopSpriteNormal;
                    poopSprite2.transform.position = new Vector3(-36f, 12f, 0f);
                    poopSprite2.enabled = true;
                }
                break;
            case 3:
                if (poopSprite1) poopSprite1.sprite = poopSpriteChanged;
                break;
            case 4:
                if (poopSprite2) poopSprite2.sprite = poopSpriteChanged;
                break;
            default:
                Debug.Log("Maximum poops reached, no further poops added.");
                break;
        }
    }

    public void FlushPoop()
    {
        poopCount = 0;
        poopTimer = 0f;

        if (poopSprite1) poopSprite1.enabled = false;
        if (poopSprite2) poopSprite2.enabled = false;

        Debug.Log("Poop flushed!");
    }

    public void UpdatePoopVisibility()
    {
        bool shouldShowPoop = !isAnimationPlaying && !uiController.inFeedMode && !uiController.inInfoMode && !uiController.isEatingLock && !uiController.isEvolutionLock;

        if (poopSprite1) poopSprite1.enabled = shouldShowPoop && (poopCount >= 1);
        if (poopSprite2) poopSprite2.enabled = shouldShowPoop && (poopCount >= 2);
    }


    public string GetStage()
    {
        switch (currentStage)
        {
            case DragonStage.Baby: return "Baby";
            case DragonStage.Teen: return "Teen";
            case DragonStage.Adult: return "Adult";
            case DragonStage.Dead: return "Adult"; // or "Dead" if you want special logic
        }
        return "Baby";
    }

    private void SwitchIdleFrame()
    {
        if (animator != null && !isSleeping)
        {
            int randomFrame = UnityEngine.Random.Range(0, idleFrameCount);
            animator.SetInteger("IdleFrame", randomFrame);
        }
    }

    public void ResetGame()
    {
        Debug.Log("Reloading the scene to reset the game.");

        // Reload the currently active scene
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private IEnumerator TriggerImmediateAlert()
    {
        Debug.Log("Immediate alert triggered! Hunger or Happiness has reached 0.");

        if (warningUIElement == null) yield break;

        // Access the SpriteRenderer or Image component
        var warningRenderer = warningUIElement.GetComponent<SpriteRenderer>();
        if (warningRenderer == null)
        {
            Debug.LogError("WarningUIElement does not have a SpriteRenderer component!");
            yield break;
        }

        // Flash the UI element 3 times immediately, and play the warning sound simultaneously
        for (int i = 0; i < 3; i++)
        {
            // Change color to fully visible
            Color c = warningRenderer.color;
            c.a = 1f; // Fully visible
            warningRenderer.color = c;

            // Play warning sound
            if (audioSource != null && warningSound != null)
            {
                audioSource.PlayOneShot(warningSound);
            }

            yield return new WaitForSeconds(0.5f); // Stay bright for 0.5 seconds

            // Change color back to dim
            c.a = 110f / 255f; // Dim alpha
            warningRenderer.color = c;

            yield return new WaitForSeconds(0.5f); // Stay dim for 0.5 seconds
        }

        // Reset alpha to dim
        Color resetColor = warningRenderer.color;
        resetColor.a = 110f / 255f;
        warningRenderer.color = resetColor;
    }
}
