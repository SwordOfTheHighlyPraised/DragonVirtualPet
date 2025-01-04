using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIController : MonoBehaviour
{
    public EggController eggController;
    public DragonStats dragonStats;

    [Header("Start Screen")]
    public Button startButton; // Assign your actual Start button here

    [Header("Gameplay Menu")]
    public SpriteRenderer[] menuItems;  // Using SpriteRenderer instead of Image

    [Header("Info Mode UI")]
    public GameObject babyObject;   // Assign the baby dragon GameObject here
    public GameObject infoPanel;    // Assign a panel to display info screens
    public TextMeshProUGUI infoText; // A UI Text component for displaying info text

    [Header("Audio")]
    public AudioSource audioSource;        // Assign an AudioSource component in the Inspector
    public AudioClip buttonSound;          // Original button sound (can use for C or fallback)
    public AudioClip menuCycleSound;       // Sound played when menu or info screens are cycled
    public AudioClip confirmSound;         // Sound played when B is pressed to confirm

    [Header("Hunger UI")]
    public Image[] hungerHearts = new Image[4];  // Assign 4 heart Image objects in Inspector
    [Header("Happiness UI")]
    public Image[] happinessHearts = new Image[4];  // Assign 4 heart Image objects in Inspector

    [Header("Hearts UI")]
    public Image emptyHeartsImage; // 4 empty hearts
    public Image fullHeartsImage;  // 4 full hearts, set to Filled in the Inspector
    public Image energyBarImage;

    [Header("Feed Mode UI")]
    public GameObject feedPanel;
    public TextMeshProUGUI feedText;

    [Header("Lights UI")]
    public GameObject lights;
    private bool isLightOn = true; // Initial state of the light

    [Header("Flush UI")]
    public GameObject flushButton; // Reference to the Flush button

    private int currentItemIndex = -1; // -1 means no item selected
    public bool inInfoMode = false;
    private int infoIndex = 0; // 0=Age/Weight, 1=Hunger, 2=Happiness, 3=Energy

    public bool inFeedMode = false;
    private int feedIndex = 0;  // 0 = fish, 1 = cake

    [HideInInspector] public bool isEvolutionLock = false;
    [HideInInspector] public bool isEatingLock = false;

    void Start()
    {
        // Ensure all menu items start unhighlighted
        foreach (var item in menuItems)
        {
            SetItemHighlight(item, false);
        }

        // Initially, info panel is hidden
        if (infoPanel != null)
            infoPanel.SetActive(false);

        if (feedPanel)
            feedPanel.SetActive(false);
    }

    // Called by the A UI Button’s OnClick
    public void OnAButtonClicked()
    {
        if (isEvolutionLock) return;

        // If in info mode, A also cycles info screens (menu cycle action)
        PlayMenuCycleSound();

        if (inInfoMode)
        {
            CycleInfoScreens();
        }
        else if (inFeedMode)
        {
            CycleFeedOptions();
        }
        else
        {
            CycleMenuItems();
        }
    }

    // Called by the B UI Button’s OnClick
    public void OnBButtonClicked()
    {
        if (dragonStats != null && dragonStats.isDead)
        {
            Debug.Log("B button pressed after dragon's death. Resetting the game.");
            dragonStats.ResetGame();
            return; // Exit to prevent further processing
        }


        if (isEvolutionLock) return;

        if (isEatingLock)
        {
            Debug.Log("B pressed to skip the eat animation.");

            // Force-end the eat animation
            if (dragonStats != null)
            {
                dragonStats.ForceEndEating();
            }

            // Or if your code uses "StopCoroutine", call that directly or do any cleanup.
            // Then unlock:
            isEatingLock = false;
            dragonStats.UpdatePoopVisibility();
            return;
        }


        // Pressing B is confirm action (start game or confirm selection)
        PlayConfirmSound();

        if (currentItemIndex != -1 && menuItems[currentItemIndex].gameObject.name == "Lights")
        {
            Debug.Log("B pressed while Lights is selected. Toggling lights.");
            OnLightsToggled();
            return; // Stop further processing
        }

        if (currentItemIndex != -1 && menuItems[currentItemIndex].gameObject.name == "Flush")
        {
            Debug.Log("B pressed while Lights is selected. Toggling lights.");
            OnFlushToggled();
            return; // Stop further processing
        }

        // -----------------------------------------
        // 1) If we are in feed mode, confirm feed
        // -----------------------------------------
        if (inFeedMode)
        {
            Debug.Log("B pressed while in Feed Mode, confirming feed choice.");
            ConfirmFeedChoice();
            return; // Stop further logic
        }

        if (inInfoMode)
        {
            // If we're in info mode, B cycles info screens
            CycleInfoScreens();
        }

        if (!eggController.gameStarted)
        {
            // If game not started, pressing B simulates the Start button click if start screen is visible
            if (eggController.startScreen != null && eggController.startScreen.activeSelf)
            {
                Debug.Log("B button pressed at Start Screen, simulating Start Button click");
                startButton.onClick.Invoke();
            }
            else
            {
                // Start screen not active, but still allow confirming the highlighted item
                Debug.Log("Start screen not active, attempting to confirm selection anyway.");
                ConfirmSelection();
            }
        }
        else
        {
            // Not in info mode: confirm selection of highlighted item
            ConfirmSelection();
        }
    }

    // Called by the C UI Button’s OnClick
    public void OnCButtonClicked()
    {
        if (isEvolutionLock) return;

        if (dragonStats != null && dragonStats.isEatCancelled == false)
        {
            Debug.Log("C pressed, forcibly ending eat animation via boolean.");
            dragonStats.ForceEndEating(); // sets isEatCancelled = true
            return;
        }

        if (dragonStats != null && dragonStats.eatRoutine != null)
        {
            Debug.Log("C pressed during eating, forcibly ending eat animation.");
            dragonStats.ForceEndEating();
            return;
        }

        // Pressing C can just use the original button sound
        PlayButtonSound();

        if (inInfoMode)
        {
            // In info mode, C exits info mode and returns to baby view
            ExitInfoMode();
        }
        else if (inFeedMode)
        {
            ExitFeedMode();
        }
        else
        {
            // Not in info mode, C cancels selection
            CancelSelection();
        }
    }

    private void CycleMenuItems()
    {
        if (currentItemIndex == -1)
        {
            currentItemIndex = 0;
            SetItemHighlight(menuItems[currentItemIndex], true);
        }
        else
        {
            SetItemHighlight(menuItems[currentItemIndex], false);
            currentItemIndex = (currentItemIndex + 1) % menuItems.Length;
            SetItemHighlight(menuItems[currentItemIndex], true);
        }

        Debug.Log("Cycled to menu item index: " + currentItemIndex);
    }

    private void ConfirmSelection()
    {
        // Check if the Egg is visible. If yes, do not allow confirmation.
        if (eggController != null && eggController.eggImage != null && eggController.eggImage.activeSelf)
        {
            Debug.Log("Cannot select menu items while the Egg is visible. You can only cycle items.");
            return;
        }

        if (currentItemIndex != -1)
        {
            Debug.Log("Selected item: " + currentItemIndex);
            // Check if the selected item is the Info item
            if (menuItems[currentItemIndex].gameObject.name == "Info")
            {
                EnterInfoMode();
            }
            else if (menuItems[currentItemIndex].gameObject.name == "Feed")
            {
                EnterFeedMode();
            }
            else
            {
                Debug.Log("No special action defined for this item.");
            }
        }
        else
        {
            Debug.Log("No item selected to confirm.");
        }
    }

    private void CancelSelection()
    {
        if (currentItemIndex != -1)
        {
            SetItemHighlight(menuItems[currentItemIndex], false);
            currentItemIndex = -1;
            Debug.Log("Selection cancelled, no item highlighted.");
        }
        else
        {
            Debug.Log("No selection to cancel.");
        }
    }

    private void SetItemHighlight(SpriteRenderer item, bool highlighted)
    {
        Color c = item.color;
        c.a = highlighted ? 1f : (110f / 255f);
        item.color = c;
    }

    // Info Mode Methods

    private void EnterInfoMode()
    {
        inInfoMode = true;
        infoIndex = 0;
        // Hide baby dragon
        if (babyObject != null) babyObject.SetActive(false);
        // Show info panel
        if (infoPanel != null) infoPanel.SetActive(true);

        ShowInfoScreen();
        CancelSelection();

        dragonStats.UpdatePoopVisibility(); // Hide poop
    }

    private void ExitInfoMode()
    {
        inInfoMode = false;
        // Show baby dragon again
        if (babyObject != null) babyObject.SetActive(true);
        // Hide info panel
        if (infoPanel != null) infoPanel.SetActive(false);

        emptyHeartsImage.gameObject.SetActive(false);
        fullHeartsImage.gameObject.SetActive(false);
        energyBarImage.gameObject.SetActive(false);
        Debug.Log("Exited info mode.");

        dragonStats.UpdatePoopVisibility(); // Hide poop
    }

    private void CycleInfoScreens()
    {
        infoIndex = (infoIndex + 1) % 4; // 4 screens: 0=Age/Weight,1=Hunger,2=Happiness,3=Energy
        ShowInfoScreen();
    }

    private void ShowInfoScreen()
    {
        if (infoText == null) return;

        emptyHeartsImage.gameObject.SetActive(false);
        fullHeartsImage.gameObject.SetActive(false);
        energyBarImage.gameObject.SetActive(false);

        infoText.rectTransform.anchoredPosition = new Vector2(0f, 0f);

        switch (infoIndex)
        {
            case 0:
                infoText.text = "Age: " + dragonStats.Age + "\nWeight: " + dragonStats.Weight;
                Debug.Log("Displaying Age/Weight screen.");
                emptyHeartsImage.gameObject.SetActive(false);
                fullHeartsImage.gameObject.SetActive(false);
                energyBarImage.gameObject.SetActive(false);
                break;
            case 1:
                infoText.gameObject.SetActive(true);
                infoText.rectTransform.anchoredPosition = new Vector2(0f, 6f);
                infoText.text = "Hunger: ";
                Debug.Log("Displaying Hunger screen.");
                emptyHeartsImage.gameObject.SetActive(true);
                fullHeartsImage.gameObject.SetActive(true);
                energyBarImage.gameObject.SetActive(false);
                float hungerFill = Mathf.Clamp01(dragonStats.Hunger / 8f);
                fullHeartsImage.fillAmount = hungerFill;
                SetHeartsActive(hungerHearts, true);
                break;
            case 2:
                infoText.gameObject.SetActive(true);
                infoText.rectTransform.anchoredPosition = new Vector2(0f, 6f);
                infoText.text = "Happiness: ";
                Debug.Log("Displaying Happiness screen.");
                emptyHeartsImage.gameObject.SetActive(true);
                fullHeartsImage.gameObject.SetActive(true);
                energyBarImage.gameObject.SetActive(false);
                float happinessFill = Mathf.Clamp01(dragonStats.Happiness / 8f);
                fullHeartsImage.fillAmount = happinessFill;
                SetHeartsActive(happinessHearts, true);
                break;
            case 3:
                infoText.text = "Energy: ";
                infoText.rectTransform.anchoredPosition = new Vector2(0f, 6f);
                Debug.Log("Displaying Energy screen.");
                emptyHeartsImage.gameObject.SetActive(false);
                fullHeartsImage.gameObject.SetActive(false);
                energyBarImage.gameObject.SetActive(true);
                float energyFill = Mathf.Clamp01(dragonStats.Energy / 10f);
                energyBarImage.fillAmount = energyFill;
                break;
        }
    }

    private void SetHeartsActive(Image[] hearts, bool active)
    {
        if (hearts == null) return;
        foreach (var heart in hearts)
        {
            if (heart != null) heart.gameObject.SetActive(active);
        }
    }

    void UpdateHearts(int value)
    {
        // value is between 0 and 8
        float fill = Mathf.Clamp01(value / 8f);
        fullHeartsImage.fillAmount = fill;
    }

    // --------------------
    // Feed Mode
    // --------------------
    private void EnterFeedMode()
    {
        inFeedMode = true;
        feedIndex = 0;   // Start on fish by default
        // Possibly hide baby or show a feed panel
        if (babyObject) babyObject.SetActive(false);
        if (feedPanel) feedPanel.SetActive(true);

        UpdateFeedModeUI();
        CancelSelection();

        dragonStats.UpdatePoopVisibility(); // Hide poop
    }

    // Confirm the feed choice
    private void ConfirmFeedChoice()
    {
        if (feedIndex == 0)
        {
            // Fish
            Debug.Log("Feeding fish. Calls dragonStats.Feed().");
        }
        else
        {
            // Cake
            Debug.Log("Feeding cake. Calls dragonStats.Cake().");

        }
        babyObject.SetActive(true);
        isEatingLock = true;
        dragonStats.StartFeedingAnimation(feedIndex);
        ExitFeedMode();

        dragonStats.UpdatePoopVisibility(); // Hide poop
    }

    private void ExitFeedMode()
    {
        inFeedMode = false;
        if (babyObject) babyObject.SetActive(true);
        if (feedPanel) feedPanel.SetActive(false);

        dragonStats.UpdatePoopVisibility(); // Hide poop
        Debug.Log("Exited feed mode.");
    }

    // Cycle feed options: 0 = Fish, 1 = Cake
    private void CycleFeedOptions()
    {
        feedIndex = (feedIndex + 1) % 2; // Only 2 items: fish or cake
        UpdateFeedModeUI();
    }

    // Update Feed Panel text or visuals
    private void UpdateFeedModeUI()
    {
        if (feedText == null) return;

        if (feedIndex == 0)
        {
            feedText.text = "FISH";
        }
        else
        {
            feedText.text = "CAKE";
        }
    }

    public void OnLightsToggled()
    {
        if (dragonStats.currentStage == DragonStage.Dead)
        {
            Debug.Log("Cannot toggle lights; the dragon is dead.");
            return;
        }

        // Toggle the light state
        isLightOn = !isLightOn;

        if (isLightOn)
        {
            dragonStats.WakeUp();
            Debug.Log("Lights are ON. Waking up the dragon.");
        }
        else
        {
            dragonStats.StartNap();
            Debug.Log("Lights are OFF. Putting the dragon to nap.");
        }
    }

    private void OnFlushToggled()
    {
        Debug.Log("Flush toggled. Triggering FlushPoop in DragonStats.");

        // Trigger the flush functionality
        if (dragonStats != null)
        {
            dragonStats.FlushPoop();
        }
    }

    // Audio Methods
    private void PlayMenuCycleSound()
    {
        if (audioSource != null && menuCycleSound != null)
        {
            audioSource.PlayOneShot(menuCycleSound);
        }
    }

    private void PlayConfirmSound()
    {
        if (audioSource != null && confirmSound != null)
        {
            audioSource.PlayOneShot(confirmSound);
        }
    }

    private void PlayButtonSound()
    {
        if (audioSource != null && buttonSound != null)
        {
            audioSource.PlayOneShot(buttonSound);
        }
    }
}
