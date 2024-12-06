using UnityEngine;
using System;

public class DragonStats : MonoBehaviour
{
    [Header("Basic Stats")]
    public int Age = 0;
    public int Weight = 5;
    public int Happiness = 8;
    public int Hunger = 8;
    public int Energy = 10;

    [Header("Stage Minimum Weights")]
    public int BabyWeightMin = 5;
    public int TeenWeightMin = 10;
    public int AdultWeightMin = 20;

    private bool hasHatched = false;
    private bool isSleeping = false;
    public bool IsSleeping => isSleeping;

    private DateTime wakeUpTime;
    private float timeSinceLastUpdate = 0f;
    private const float statReductionInterval = 3600f;

    private Animator animator;
    private Vector3 defaultSleepPosition = Vector3.zero;

    // Idle animation switching
    private float idleSwitchInterval = 0.5f;
    private float nextIdleSwitchTime = 0f;
    private int idleFrameCount = 2;

    void Start()
    {
        animator = GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogError("No Animator component found! Please attach an Animator.");
        }

        nextIdleSwitchTime = Time.time + idleSwitchInterval;
    }

    void Update()
    {
        DateTime now = DateTime.Now;

        if (hasHatched && !isSleeping)
        {
            // Stat reduction over time
            timeSinceLastUpdate += Time.deltaTime;
            if (timeSinceLastUpdate >= statReductionInterval)
            {
                ReduceStats();
                timeSinceLastUpdate -= statReductionInterval;
            }

            // Force sleep at 9pm
            if (now.Hour == 21 && now.Minute == 0 && now.Second == 0)
            {
                StartSleep();
            }

            // Idle frame switching if needed
            if (Time.time >= nextIdleSwitchTime)
            {
                SwitchIdleFrame();
                nextIdleSwitchTime = Time.time + idleSwitchInterval;
            }
        }

        // Wake up when the set wakeUpTime passes
        if (isSleeping && DateTime.Now >= wakeUpTime)
        {
            WakeUp();
        }
    }

    public void Hatch()
    {
        hasHatched = true;
        Debug.Log("Dragon has hatched! Stats will now decrease over time.");
    }

    public void StartNap()
    {
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
                GoToSleep(defaultSleepPosition);
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
        isSleeping = false;
        Debug.Log("Dragon has woken up!");

        Energy = 10; // Restore full energy

        // Age only after sleep (not nap)
        if (wakeUpTime.Hour == 7)
        {
            Age++;
            Debug.Log($"Dragon has aged! New age: {Age}");
        }

        // Stop sleeping animation
        if (animator != null)
        {
            animator.SetBool("IsSleeping", false);
        }

        // Reset the idle switching timer so the dragon resumes switching frames
        nextIdleSwitchTime = Time.time + idleSwitchInterval;
    }

    private void ReduceStats()
    {
        Happiness = Mathf.Max(Happiness - 2, 0);
        Hunger = Mathf.Max(Hunger - 2, 0);
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

    public void Play()
    {
        if (Happiness < 8 && Energy > 2)
        {
            Happiness = Mathf.Min(Happiness + 3, 8);
            Energy = Mathf.Max(Energy - 2, 0);
        }
        else
        {
            Debug.Log("Dragon is too tired to play!");
        }
    }

    public string GetStage()
    {
        if (Weight >= AdultWeightMin) return "Adult";
        if (Weight >= TeenWeightMin) return "Teen";
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
}
