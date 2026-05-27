using System.Collections;
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

    [Header("Arrival")]
    [SerializeField] private string targetArrivalPointId;

    [Header("Fade Transition")]
    [SerializeField] private bool useFadeTransition = true;
    [SerializeField, Min(0f)] private float fadeOutDuration = 0.45f;
    [SerializeField, Min(0f)] private float nextSceneFadeInDuration = 0.55f;
    [SerializeField, Min(0f)] private float nextSceneFadeInDelay = 0.15f;

    [Header("Upgrade Gate")]
    [SerializeField] private bool requireFireproofBoatUpgrade;
    [SerializeField] private DockUpgradeSystem dockUpgradeSystem;
    [SerializeField] private string missingFireproofBoatUpgradeWarning = "Você precisa do upgrade Barco à prova de fogo para acessar a lava.";
    [SerializeField] private bool showHudWarningWhenMissingUpgrade = true;

    [Header("Interaction")]
    [SerializeField] private PlayerStateRequirement playerStateRequirement = PlayerStateRequirement.OnBoat;
    [HideInInspector, SerializeField] private bool requirePlayerOnFoot;
    [SerializeField] private int interactionPriority = 10;

    private bool isLoading;

    public void Interact()
    {
        if (!CanInteract())
            return;

        if (!HasRequiredUpgrade())
        {
            ShowMissingUpgradeWarning();
            return;
        }

        if (requireSceneInBuildSettings && !Application.CanStreamedLevelBeLoaded(targetSceneName))
        {
            ShowSceneUnavailableWarning();
            return;
        }

        isLoading = true;
        Time.timeScale = 1f;

        if (useFadeTransition)
        {
            StartCoroutine(LoadSceneWithFadeRoutine());
            return;
        }

        LoadTargetScene(false);
    }

    private IEnumerator LoadSceneWithFadeRoutine()
    {
        yield return SceneTransitionFadeController.FadeOut(fadeOutDuration);
        LoadTargetScene(true);
    }

    private void LoadTargetScene(bool _requestFadeInOnNextScene)
    {
        if (saveBeforeTransition)
            GameSaveManager.SaveCurrentGameAndRequestLoadOnNextScene();

        if (_requestFadeInOnNextScene)
            SceneTransitionFadeController.RequestFadeInOnNextScene(nextSceneFadeInDuration, nextSceneFadeInDelay);

        SceneTransitionRequest.RequestArrival(targetArrivalPointId);
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

    private bool HasRequiredUpgrade()
    {
        if (!requireFireproofBoatUpgrade)
            return true;

        if (dockUpgradeSystem == null)
            dockUpgradeSystem = FindFirstObjectByType<DockUpgradeSystem>(FindObjectsInactive.Include);

        return dockUpgradeSystem != null && dockUpgradeSystem.HasFireproofBoatUpgrade;
    }

    private void ShowMissingUpgradeWarning()
    {
        string warning = string.IsNullOrWhiteSpace(missingFireproofBoatUpgradeWarning)
            ? "Você precisa do upgrade Barco à prova de fogo."
            : missingFireproofBoatUpgradeWarning;

        if (showHudWarningWhenMissingUpgrade && HUDWarningUI.Instance != null)
            HUDWarningUI.Instance.ShowWarning(warning);

        Debug.LogWarning("[SceneTransitionInteractable] Transição bloqueada: upgrade Barco à prova de fogo não comprado.");
    }
}
