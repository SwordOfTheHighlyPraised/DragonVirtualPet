using UnityEngine;

public enum GameState
{
    StartScreen,
    Gameplay
}

public class GameManager : MonoBehaviour
{
    public GameState currentState = GameState.StartScreen;

    public void EnterGameplay()
    {
        currentState = GameState.Gameplay;
        Debug.Log("Entered Gameplay State.");
    }

    public void EnterStartScreen()
    {
        currentState = GameState.StartScreen;
        Debug.Log("Entered Start Screen State.");
    }
}
