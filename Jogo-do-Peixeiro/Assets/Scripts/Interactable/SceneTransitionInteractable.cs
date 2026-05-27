using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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

    [Header("Missing Upgrade Confirmation")]
    [SerializeField] private bool confirmMissingFireproofBoatUpgradeEntry = true;
    [SerializeField, Min(1)] private int missingUpgradeConfirmationSteps = 5;
    [SerializeField, TextArea] private string missingUpgradeConfirmationMessage = "Se você tentar entrar aqui, seu barco vai queimar. Tem certeza que quer entrar?\nÉ necessário upgrade de barco à prova de fogo.";
    [SerializeField, TextArea] private string missingUpgradeRepeatConfirmationMessage = "Tem certeza mesmo? Seu barco vai queimar.";
    [SerializeField] private string missingUpgradeConfirmationYesText = "Sim";
    [SerializeField] private string missingUpgradeConfirmationNoText = "Não";
    [SerializeField] private string burnedBoatForcedSleepText = "Seu barco queimou";

    [Header("Interaction")]
    [SerializeField] private PlayerStateRequirement playerStateRequirement = PlayerStateRequirement.OnBoat;
    [HideInInspector, SerializeField] private bool requirePlayerOnFoot;
    [SerializeField] private int interactionPriority = 10;

    private bool isLoading;
    private bool isShowingMissingUpgradeConfirmation;
    private int missingUpgradeConfirmationCount;
    private GameObject missingUpgradeConfirmationRoot;
    private TMP_Text missingUpgradeConfirmationBodyText;
    private GameManager.GameState missingUpgradePreviousState;
    private bool hasMissingUpgradePreviousState;

    private void OnDisable()
    {
        CloseMissingUpgradeConfirmation(true);
    }

    public void Interact()
    {
        if (!CanInteract())
            return;

        if (!HasRequiredUpgrade())
        {
            HandleMissingUpgradeAttempt();
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

        if (isShowingMissingUpgradeConfirmation)
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

    private void HandleMissingUpgradeAttempt()
    {
        if (!confirmMissingFireproofBoatUpgradeEntry)
        {
            ShowMissingUpgradeWarning();
            return;
        }

        ShowMissingUpgradeConfirmation();
    }

    private void ShowMissingUpgradeConfirmation()
    {
        if (isShowingMissingUpgradeConfirmation)
            return;

        missingUpgradeConfirmationCount = 0;
        isShowingMissingUpgradeConfirmation = true;
        LockGameplayForMissingUpgradeConfirmation();

        missingUpgradeConfirmationRoot = CreateMissingUpgradeConfirmationPanel();
        UpdateMissingUpgradeConfirmationMessage();

        AudioManager.Instance?.RefreshUIButtonAudioFeedback();
    }

    private GameObject CreateMissingUpgradeConfirmationPanel()
    {
        GameObject root = new GameObject(
            "MissingFireproofUpgradeConfirmation",
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(CanvasGroup),
            typeof(GraphicRaycaster));

        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = short.MaxValue - 10;

        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        CanvasGroup canvasGroup = root.GetComponent<CanvasGroup>();
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;
        canvasGroup.interactable = true;

        GameObject backgroundObject = new GameObject("Background", typeof(RectTransform), typeof(Image));
        RectTransform backgroundTransform = backgroundObject.GetComponent<RectTransform>();
        backgroundTransform.SetParent(root.transform, false);
        backgroundTransform.anchorMin = Vector2.zero;
        backgroundTransform.anchorMax = Vector2.one;
        backgroundTransform.offsetMin = Vector2.zero;
        backgroundTransform.offsetMax = Vector2.zero;

        Image background = backgroundObject.GetComponent<Image>();
        background.color = new Color(0f, 0f, 0f, 0.68f);

        GameObject panelObject = new GameObject("Panel", typeof(RectTransform), typeof(Image));
        RectTransform panelTransform = panelObject.GetComponent<RectTransform>();
        panelTransform.SetParent(root.transform, false);
        panelTransform.anchorMin = new Vector2(0.5f, 0.5f);
        panelTransform.anchorMax = new Vector2(0.5f, 0.5f);
        panelTransform.pivot = new Vector2(0.5f, 0.5f);
        panelTransform.anchoredPosition = Vector2.zero;
        panelTransform.sizeDelta = new Vector2(860f, 420f);

        Image panelImage = panelObject.GetComponent<Image>();
        panelImage.color = new Color(0.06f, 0.07f, 0.08f, 0.97f);

        TMP_Text titleText = CreateText(panelTransform, "Title", "Barco em risco", 54f, FontStyles.Bold);
        RectTransform titleTransform = titleText.rectTransform;
        titleTransform.anchorMin = new Vector2(0.08f, 0.68f);
        titleTransform.anchorMax = new Vector2(0.92f, 0.9f);
        titleTransform.offsetMin = Vector2.zero;
        titleTransform.offsetMax = Vector2.zero;

        missingUpgradeConfirmationBodyText = CreateText(panelTransform, "Message", string.Empty, 34f, FontStyles.Normal);
        RectTransform bodyTransform = missingUpgradeConfirmationBodyText.rectTransform;
        bodyTransform.anchorMin = new Vector2(0.08f, 0.32f);
        bodyTransform.anchorMax = new Vector2(0.92f, 0.68f);
        bodyTransform.offsetMin = Vector2.zero;
        bodyTransform.offsetMax = Vector2.zero;

        Button noButton = CreateButton(panelTransform, "NoButton", GetNoButtonText(), OnMissingUpgradeConfirmNo);
        RectTransform noTransform = noButton.GetComponent<RectTransform>();
        noTransform.anchorMin = new Vector2(0.18f, 0.1f);
        noTransform.anchorMax = new Vector2(0.45f, 0.24f);
        noTransform.offsetMin = Vector2.zero;
        noTransform.offsetMax = Vector2.zero;

        Button yesButton = CreateButton(panelTransform, "YesButton", GetYesButtonText(), OnMissingUpgradeConfirmYes);
        RectTransform yesTransform = yesButton.GetComponent<RectTransform>();
        yesTransform.anchorMin = new Vector2(0.55f, 0.1f);
        yesTransform.anchorMax = new Vector2(0.82f, 0.24f);
        yesTransform.offsetMin = Vector2.zero;
        yesTransform.offsetMax = Vector2.zero;

        UISelectionHelper.Select(noButton, root);
        return root;
    }

    private TMP_Text CreateText(Transform _parent, string _name, string _text, float _fontSize, FontStyles _fontStyle)
    {
        GameObject textObject = new GameObject(_name, typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(_parent, false);

        TMP_Text text = textObject.GetComponent<TMP_Text>();
        text.text = _text;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.fontSize = _fontSize;
        text.fontStyle = _fontStyle;
        text.textWrappingMode = TextWrappingModes.Normal;

        return text;
    }

    private Button CreateButton(Transform _parent, string _name, string _label, UnityEngine.Events.UnityAction _onClick)
    {
        GameObject buttonObject = new GameObject(_name, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(_parent, false);

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.14f, 0.18f, 0.2f, 1f);

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(_onClick);

        TMP_Text labelText = CreateText(buttonObject.transform, "Label", _label, 32f, FontStyles.Bold);
        RectTransform labelTransform = labelText.rectTransform;
        labelTransform.anchorMin = Vector2.zero;
        labelTransform.anchorMax = Vector2.one;
        labelTransform.offsetMin = Vector2.zero;
        labelTransform.offsetMax = Vector2.zero;

        return button;
    }

    private void OnMissingUpgradeConfirmYes()
    {
        missingUpgradeConfirmationCount++;

        if (missingUpgradeConfirmationCount >= Mathf.Max(1, missingUpgradeConfirmationSteps))
        {
            bool hadStoredState = hasMissingUpgradePreviousState;
            GameManager.GameState storedState = missingUpgradePreviousState;
            CloseMissingUpgradeConfirmation(false);
            TriggerMissingUpgradeBurnReset(hadStoredState, storedState);
            return;
        }

        UpdateMissingUpgradeConfirmationMessage();
    }

    private void OnMissingUpgradeConfirmNo()
    {
        CloseMissingUpgradeConfirmation(true);
    }

    private void UpdateMissingUpgradeConfirmationMessage()
    {
        if (missingUpgradeConfirmationBodyText == null)
            return;

        string message = missingUpgradeConfirmationCount <= 0
            ? missingUpgradeConfirmationMessage
            : missingUpgradeRepeatConfirmationMessage;

        if (string.IsNullOrWhiteSpace(message))
            message = "Tem certeza mesmo? Seu barco vai queimar.";

        int maxSteps = Mathf.Max(1, missingUpgradeConfirmationSteps);
        int currentStep = Mathf.Clamp(missingUpgradeConfirmationCount + 1, 1, maxSteps);
        missingUpgradeConfirmationBodyText.text = $"{message}\n\nConfirmação {currentStep}/{maxSteps}";
    }

    private void CloseMissingUpgradeConfirmation(bool _restoreGameplayState)
    {
        missingUpgradeConfirmationCount = 0;
        isShowingMissingUpgradeConfirmation = false;
        missingUpgradeConfirmationBodyText = null;

        if (missingUpgradeConfirmationRoot != null)
        {
            Destroy(missingUpgradeConfirmationRoot);
            missingUpgradeConfirmationRoot = null;
        }

        if (_restoreGameplayState)
            RestoreGameplayAfterMissingUpgradeConfirmation();
        else
            ClearMissingUpgradeStoredState();
    }

    private void TriggerMissingUpgradeBurnReset(bool _hadStoredState, GameManager.GameState _storedState)
    {
        ForcedSleepController forcedSleepController = FindFirstObjectByType<ForcedSleepController>(FindObjectsInactive.Include);
        string message = string.IsNullOrWhiteSpace(burnedBoatForcedSleepText)
            ? "Seu barco queimou"
            : burnedBoatForcedSleepText;

        if (forcedSleepController != null && forcedSleepController.StartCustomForcedSleep(message))
            return;

        if (GameManager.instance != null && _hadStoredState)
            GameManager.instance.SetState(_storedState);

        ShowMissingUpgradeWarning();
        Debug.LogWarning("[SceneTransitionInteractable] Nao foi possivel iniciar o sono forcado do barco queimado.");
    }

    private void LockGameplayForMissingUpgradeConfirmation()
    {
        if (GameManager.instance == null || hasMissingUpgradePreviousState)
            return;

        missingUpgradePreviousState = GameManager.instance.currentState;
        hasMissingUpgradePreviousState = true;
        GameManager.instance.SetState(GameManager.GameState.InUI);
    }

    private void RestoreGameplayAfterMissingUpgradeConfirmation()
    {
        if (GameManager.instance != null && hasMissingUpgradePreviousState)
            GameManager.instance.SetState(missingUpgradePreviousState);

        ClearMissingUpgradeStoredState();
    }

    private void ClearMissingUpgradeStoredState()
    {
        hasMissingUpgradePreviousState = false;
        missingUpgradePreviousState = default;
    }

    private string GetYesButtonText()
    {
        return string.IsNullOrWhiteSpace(missingUpgradeConfirmationYesText)
            ? "Sim"
            : missingUpgradeConfirmationYesText;
    }

    private string GetNoButtonText()
    {
        return string.IsNullOrWhiteSpace(missingUpgradeConfirmationNoText)
            ? "Não"
            : missingUpgradeConfirmationNoText;
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
