using UnityEngine;
using UnityEngine.SceneManagement;

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
        UpdateCursor();
    }

    private void UpdateCursor()
    {
        switch (currentState)
        {
            case GameState.OnFoot:
            case GameState.OnBoat:
            case GameState.Fishing:

                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                break;

            case GameState.InUI:
            case GameState.Paused:

                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                break;
        }
    }

    public bool IsGameplayBlocked()
    {
        return currentState == GameState.InUI || currentState == GameState.Paused;
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "Main Menu")
        {
            SetState(GameState.InUI); // libera o cursor no menu
        }
        else
        {
            SetState(GameState.OnFoot); // gameplay normal
        }
    }
}