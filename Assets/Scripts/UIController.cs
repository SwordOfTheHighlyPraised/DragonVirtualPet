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

    private int currentItemIndex = -1; // -1 means no item selected
    private bool inInfoMode = false;
    private int infoIndex = 0; // 0=Age/Weight, 1=Hunger, 2=Happiness, 3=Energy

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
    }

    // Called by the A UI Button’s OnClick
    public void OnAButtonClicked()
    {
        // If in info mode, A also cycles info screens (menu cycle action)
        PlayMenuCycleSound();

        if (inInfoMode)
        {
            CycleInfoScreens();
        }
        else
        {
            CycleMenuItems();
        }
    }

    // Called by the B UI Button’s OnClick
    public void OnBButtonClicked()
    {
        // Pressing B is confirm action (start game or confirm selection)
        PlayConfirmSound();

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
            // Game started and dragon awake:
            if (inInfoMode)
            {
                // If we're in info mode, B cycles info screens
                CycleInfoScreens();
            }
            else
            {
                // Not in info mode: confirm selection of highlighted item
                ConfirmSelection();
            }
        }
    }

    // Called by the C UI Button’s OnClick
    public void OnCButtonClicked()
    {
        // Pressing C can just use the original button sound
        PlayButtonSound();

        if (inInfoMode)
        {
            // In info mode, C exits info mode and returns to baby view
            ExitInfoMode();
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
    }

    private void ExitInfoMode()
    {
        inInfoMode = false;
        // Show baby dragon again
        if (babyObject != null) babyObject.SetActive(true);
        // Hide info panel
        if (infoPanel != null) infoPanel.SetActive(false);
        Debug.Log("Exited info mode.");
    }

    private void CycleInfoScreens()
    {
        infoIndex = (infoIndex + 1) % 4; // 4 screens: 0=Age/Weight,1=Hunger,2=Happiness,3=Energy
        ShowInfoScreen();
    }

    private void ShowInfoScreen()
    {
        if (infoText == null) return;

        switch (infoIndex)
        {
            case 0:
                infoText.text = "Age: " + dragonStats.Age + "\nWeight: " + dragonStats.Weight;
                Debug.Log("Displaying Age/Weight screen.");
                break;
            case 1:
                infoText.text = "Hunger";
                Debug.Log("Displaying Hunger screen.");
                break;
            case 2:
                infoText.text = "Happiness";
                Debug.Log("Displaying Happiness screen.");
                break;
            case 3:
                infoText.text = "Energy";
                Debug.Log("Displaying Energy screen.");
                break;
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
