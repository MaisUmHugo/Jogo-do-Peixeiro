using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    public enum GameState
    {
        OnFoot,
        OnBoat,
        Fishing,
        InUI,
        Paused
    }

    public GameState currentState;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void SetState(GameState _newState)
    {
        currentState = _newState;
    }

    public bool IsGameplayBlocked()
    {
        return currentState == GameState.InUI || currentState == GameState.Paused;
    }
}