using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EggController : MonoBehaviour
{
    public GameObject startScreen; // The start screen UI
    public GameObject eggImage;    // The egg Image object
    public GameObject babyObject;  // The baby GameObject
    public Animator eggAnimator;   // The egg's Animator
    [SerializeField] private float hatchTimer = 60f; // 1 minute
    [HideInInspector] public bool gameStarted = false; // Public so UIController can check state

    [Header("Audio")]
    public AudioSource audioSource;  // Assign in Inspector (Add an AudioSource component to this GameObject)
    public AudioClip hatchSound;     // Assign the hatching sound clip in Inspector

    void Start()
    {
        // Ensure initial state: start screen is shown, egg is hidden, baby is hidden
        startScreen.SetActive(true);
        eggImage.SetActive(false);
        babyObject.SetActive(false);
    }

    void Update()
    {
        if (gameStarted)
        {
            hatchTimer -= Time.deltaTime;

            if (hatchTimer <= 0)
            {
                // Trigger the hatching animation
                eggAnimator.SetTrigger("Hatch");

                // Play the hatching sound
                if (audioSource != null && hatchSound != null)
                {
                    audioSource.PlayOneShot(hatchSound);
                }

                gameStarted = false; // Stop the timer
                Invoke(nameof(TransitionToBaby), 5f); // Wait 5 seconds after hatch animation
            }
        }
    }

    public void StartGame()
    {
        // Hide the start screen and show the egg
        startScreen.SetActive(false);
        eggImage.SetActive(true);

        // Start the game (start the hatch timer)
        gameStarted = true;
        Debug.Log("Game Started! Egg is visible, timer running.");
    }

    private void TransitionToBaby()
    {
        // Hide the egg and show the baby
        eggImage.SetActive(false);
        babyObject.SetActive(true);
        Debug.Log("Egg hatched, baby is now visible!");
    }
}
