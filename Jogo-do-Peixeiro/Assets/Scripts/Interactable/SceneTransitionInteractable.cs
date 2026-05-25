using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransitionInteractable : MonoBehaviour, IInteractable
{
    private enum PlayerStateRequirement
    {
        AnyGameplay,
        OnFoot,
        OnBoat
    }

    [Header("Scene")]
    [SerializeField] private string targetSceneName = "Lava";
    [SerializeField] private bool requireSceneInBuildSettings = true;
    [SerializeField] private bool saveBeforeTransition = true;
    [SerializeField] private string sceneUnavailableWarning = "A cena da lava ainda não está pronta.";
    [SerializeField] private bool showHudWarningWhenSceneUnavailable = true;

    [Header("Interaction")]
    [SerializeField] private PlayerStateRequirement playerStateRequirement = PlayerStateRequirement.OnBoat;
    [HideInInspector, SerializeField] private bool requirePlayerOnFoot;
    [SerializeField] private int interactionPriority = 10;

    private bool isLoading;

    public void Interact()
    {
        if (!CanInteract())
            return;

        if (requireSceneInBuildSettings && !Application.CanStreamedLevelBeLoaded(targetSceneName))
        {
            ShowSceneUnavailableWarning();
            return;
        }

        isLoading = true;
        Time.timeScale = 1f;

        if (saveBeforeTransition)
            GameSaveManager.GetOrCreate()?.SaveGame();

        SceneManager.LoadScene(targetSceneName);
    }

    public int GetInteractionPriority()
    {
        return interactionPriority;
    }

    public bool CanInteract()
    {
        if (isLoading)
            return false;

        if (string.IsNullOrWhiteSpace(targetSceneName))
            return false;

        if (GameManager.instance == null)
            return playerStateRequirement == PlayerStateRequirement.AnyGameplay;

        return playerStateRequirement switch
        {
            PlayerStateRequirement.AnyGameplay => GameManager.instance.currentState != GameManager.GameState.Fishing &&
                                                  !GameManager.instance.IsGameplayBlocked(),
            PlayerStateRequirement.OnFoot => GameManager.instance.currentState == GameManager.GameState.OnFoot,
            PlayerStateRequirement.OnBoat => GameManager.instance.currentState == GameManager.GameState.OnBoat,
            _ => false
        };
    }

    private void ShowSceneUnavailableWarning()
    {
        string warning = string.IsNullOrWhiteSpace(sceneUnavailableWarning)
            ? $"Cena '{targetSceneName}' ainda não está pronta."
            : sceneUnavailableWarning;

        if (showHudWarningWhenSceneUnavailable && HUDWarningUI.Instance != null)
            HUDWarningUI.Instance.ShowWarning(warning);

        Debug.LogWarning($"[SceneTransitionInteractable] Cena '{targetSceneName}' não encontrada no Build Settings.");
    }
}
