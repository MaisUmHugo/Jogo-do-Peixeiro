using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    public enum GameState
    {
        OnFoot,
        OnBoat,
        Fishing,
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

    private void Start()
    {
        //currentState = GameState.OnFoot;
    }

    public void SetState(GameState _newState)
    {
        currentState = _newState;
    }
}