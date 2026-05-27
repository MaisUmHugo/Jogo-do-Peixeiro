using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CampaignOutcomeController : MonoBehaviour
{
    #region Fields

    [Header("References")]
    [SerializeField] private CampaignProgressSystem campaignProgress;
    [SerializeField] private GameOutcomePanelUI outcomePanel;
    [SerializeField] private DebtSystem debtSystem;
    [SerializeField] private CampaignCutsceneController cutsceneController;

    [Header("Quest Failure")]
    [SerializeField] private bool showQuestFailurePanel = true;
    [SerializeField] private bool pauseOnQuestFailure = true;
    [SerializeField] private string questFailureTitle = "Quest falhou";
    [SerializeField, TextArea] private string questFailureMessage = "O prazo acabou antes de pagar a meta. Tente novamente ou volte ao menu principal.";

    [Header("Campaign Completion")]
    [SerializeField] private bool showCampaignCompletionPanel = true;
    [SerializeField] private bool playFinalCutsceneOnCampaignCompletion = true;
    [SerializeField] private bool playFinalDialogFallbackWhenCutsceneMissing = true;
    [SerializeField] private RoteiroDialogLibrary roteiroDialogLibrary;
    [SerializeField] private bool loadMainMenuAfterFinalDialogFallback = true;
    [SerializeField] private string mainMenuSceneName = "Main Menu";
    [SerializeField, Min(0f)] private float fallbackMainMenuFadeDuration = 1f;
    [SerializeField] private bool fadeBeforeFinalDialogFallback = true;
    [SerializeField, Min(0f)] private float finalDialogFallbackFadeInDuration = 3f;
    [SerializeField, Min(0f)] private float finalDialogFallbackFadeInDelay = 1.5f;
    [SerializeField] private bool showFinalFadeScreenWhenUsingDialogFallback = true;
    [SerializeField] private string fallbackFinalText = "FIM?";
    [SerializeField] private string fallbackEndlessUnlockedText = "Modo sem fim foi liberado!";
    [SerializeField, Min(0f)] private float fallbackFinalFadeInDuration = 1.5f;
    [SerializeField, Min(0f)] private float fallbackFinalHoldDuration = 1.2f;
    [SerializeField, Min(0f)] private float fallbackEndlessNoticeHoldDuration = 1.6f;
    [SerializeField] private bool pauseOnCampaignCompletion = true;
    [SerializeField] private bool saveGameOnCampaignCompletion = true;
    [SerializeField] private string campaignCompletionTitle = "Dívida quitada";
    [SerializeField, TextArea] private string campaignCompletionMessage = "O cobrador pega o dinheiro, sorri e revela as outras dívidas acumuladas. Dívida atual: R$ {0}. O modo sem fim foi liberado no menu principal.";

    private bool isSubscribed;
    private bool hasShownQuestFailure;
    private bool hasShownCampaignCompletion;
    private bool hasStartedCampaignCompletionCutscene;
    private bool isResolvingCampaignCompletion;
    private Coroutine pendingQuestFailureRoutine;
    private Coroutine pendingCampaignCompletionRoutine;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnEnable()
    {
        ResolveReferences();
        Subscribe();
    }

    private void Start()
    {
        ResolveReferences();
        TryShowCurrentOutcome(false);
    }

    private void OnDisable()
    {
        if (pendingQuestFailureRoutine != null)
        {
            StopCoroutine(pendingQuestFailureRoutine);
            pendingQuestFailureRoutine = null;
        }

        if (pendingCampaignCompletionRoutine != null)
        {
            StopCoroutine(pendingCampaignCompletionRoutine);
            pendingCampaignCompletionRoutine = null;
        }

        Unsubscribe();
    }

    #endregion

    #region Event Handling

    private void HandleQuestAdvanced()
    {
        hasShownQuestFailure = false;
    }

    private void HandleProgressChanged()
    {
        if (isResolvingCampaignCompletion)
            return;

        TryShowCurrentOutcome(false);
    }

    private void HandleQuestDeadlineExpired()
    {
        TryShowQuestFailure();
    }

    private void TryShowQuestFailure()
    {
        if (!showQuestFailurePanel || hasShownQuestFailure || campaignProgress == null)
            return;

        if (campaignProgress.IsCampaignCompleted)
            return;

        if (IsQuestFailureHandledByTutorialGuidance())
            return;

        if (ForcedSleepController.IsAnySleepTransitionRunning())
        {
            if (pendingQuestFailureRoutine == null)
                pendingQuestFailureRoutine = StartCoroutine(ShowQuestFailureAfterSleep());

            return;
        }

        hasShownQuestFailure = true;

        if (outcomePanel == null)
        {
            Debug.LogWarning("[CampaignOutcomeController] OutcomePanel não configurado.", this);
            return;
        }

        outcomePanel.ShowFailure(questFailureTitle, questFailureMessage, pauseOnQuestFailure);
    }

    private IEnumerator ShowQuestFailureAfterSleep()
    {
        while (ForcedSleepController.IsAnySleepTransitionRunning())
            yield return null;

        pendingQuestFailureRoutine = null;
        TryShowQuestFailure();
    }

    private void HandleCampaignCompleted()
    {
        if (pendingCampaignCompletionRoutine != null)
            StopCoroutine(pendingCampaignCompletionRoutine);

        isResolvingCampaignCompletion = true;
        pendingCampaignCompletionRoutine = StartCoroutine(ResolveCampaignCompletionRoutine(saveGameOnCampaignCompletion));
    }

    public bool DebugPlayCampaignCompletionFlow(bool _saveGame = false)
    {
        if (!isActiveAndEnabled)
            return false;

        ResolveReferences();

        if (pendingCampaignCompletionRoutine != null)
        {
            StopCoroutine(pendingCampaignCompletionRoutine);
            pendingCampaignCompletionRoutine = null;
        }

        if (campaignProgress == null)
            campaignProgress = CampaignProgressSystem.GetOrCreate();

        if (debtSystem == null)
            debtSystem = DebtSystem.GetOrCreate();

        if (campaignProgress != null && debtSystem != null && campaignProgress.CampaignCompletionDebtAmount > 0)
            debtSystem.SetDebt(campaignProgress.CampaignCompletionDebtAmount);

        hasShownCampaignCompletion = false;
        hasStartedCampaignCompletionCutscene = false;
        isResolvingCampaignCompletion = true;
        pendingCampaignCompletionRoutine = StartCoroutine(ResolveCampaignCompletionRoutine(_saveGame));
        return true;
    }

    private IEnumerator ResolveCampaignCompletionRoutine(bool _saveGame)
    {
        yield return null;

        pendingCampaignCompletionRoutine = null;

        if (TryPlayCampaignCompletionCutscene(_saveGame))
        {
            isResolvingCampaignCompletion = false;
            yield break;
        }

        if (TryPlayCampaignCompletionDialogFallback(_saveGame))
        {
            isResolvingCampaignCompletion = false;
            yield break;
        }

        isResolvingCampaignCompletion = false;
        TryShowCampaignCompletion(_saveGame);
    }

    private bool TryPlayCampaignCompletionCutscene(bool _saveGame)
    {
        if (!playFinalCutsceneOnCampaignCompletion || hasStartedCampaignCompletionCutscene)
            return false;

        ResolveReferences();

        if (cutsceneController == null)
            return false;

        Action onFinished = cutsceneController.HasFinalCutsceneTimeline
            ? null
            : () => CompleteCampaignCompletionDialogFallback(_saveGame);

        if (!cutsceneController.TryPlayFinalCutscene(onFinished))
            return false;

        hasStartedCampaignCompletionCutscene = true;
        hasShownCampaignCompletion = true;
        return true;
    }

    private bool TryPlayCampaignCompletionDialogFallback(bool _saveGame)
    {
        if (!playFinalDialogFallbackWhenCutsceneMissing || hasStartedCampaignCompletionCutscene)
            return false;

        ResolveReferences();

        if (roteiroDialogLibrary == null)
            return false;

        DialogSequenceAsset[] dialogs = new[]
        {
            roteiroDialogLibrary.FimCampanhaLoja,
            roteiroDialogLibrary.FimCampanhaAirFishers
        };

        if (!HasAnyDialogFallback(dialogs))
            return false;

        Action onFinished = () => CompleteCampaignCompletionDialogFallback(_saveGame);
        bool started;

        if (fadeBeforeFinalDialogFallback && isActiveAndEnabled)
        {
            pendingCampaignCompletionRoutine = StartCoroutine(PlayCampaignCompletionDialogFallbackWithFadeRoutine(dialogs, onFinished));
            started = true;
        }
        else
        {
            started = RoteiroDialogPlayback.TryPlaySequence(dialogs, onFinished);
        }

        if (!started)
            return false;

        hasStartedCampaignCompletionCutscene = true;
        hasShownCampaignCompletion = true;
        return true;
    }

    private IEnumerator PlayCampaignCompletionDialogFallbackWithFadeRoutine(DialogSequenceAsset[] _dialogs, Action _onFinished)
    {
        SceneTransitionFadeController.SetBlackImmediate();
        yield return SceneTransitionFadeController.FadeInAndWait(
            finalDialogFallbackFadeInDuration,
            finalDialogFallbackFadeInDelay);

        pendingCampaignCompletionRoutine = null;

        if (!RoteiroDialogPlayback.TryPlaySequence(_dialogs, _onFinished))
            _onFinished?.Invoke();
    }

    private static bool HasAnyDialogFallback(DialogSequenceAsset[] _dialogs)
    {
        if (_dialogs == null || _dialogs.Length == 0)
            return false;

        for (int i = 0; i < _dialogs.Length; i++)
        {
            if (_dialogs[i] != null && _dialogs[i].HasLines)
                return true;
        }

        return false;
    }

    private void CompleteCampaignCompletionDialogFallback(bool _saveGame)
    {
        if (showFinalFadeScreenWhenUsingDialogFallback && isActiveAndEnabled)
        {
            pendingCampaignCompletionRoutine = StartCoroutine(CompleteCampaignCompletionDialogFallbackRoutine(_saveGame));
            return;
        }

        FinishCampaignCompletionDialogFallback(_saveGame);
    }

    private IEnumerator CompleteCampaignCompletionDialogFallbackRoutine(bool _saveGame)
    {
        GameObject overlay = CreateFallbackEndingOverlay(out CanvasGroup canvasGroup, out TMP_Text messageText);

        if (overlay == null || canvasGroup == null || messageText == null)
        {
            pendingCampaignCompletionRoutine = null;
            FinishCampaignCompletionDialogFallback(_saveGame);
            yield break;
        }

        messageText.text = fallbackFinalText;
        yield return FadeCanvasGroup(canvasGroup, 0f, 1f, fallbackFinalFadeInDuration);

        if (fallbackFinalHoldDuration > 0f)
            yield return new WaitForSecondsRealtime(fallbackFinalHoldDuration);

        if (!string.IsNullOrWhiteSpace(fallbackEndlessUnlockedText))
        {
            messageText.text = fallbackEndlessUnlockedText;

            if (fallbackEndlessNoticeHoldDuration > 0f)
                yield return new WaitForSecondsRealtime(fallbackEndlessNoticeHoldDuration);
        }

        Destroy(overlay);
        pendingCampaignCompletionRoutine = null;
        FinishCampaignCompletionDialogFallback(_saveGame);
    }

    private void FinishCampaignCompletionDialogFallback(bool _saveGame)
    {
        if (campaignProgress == null)
            campaignProgress = CampaignProgressSystem.GetOrCreate();

        if (campaignProgress != null)
            campaignProgress.StartUnlockedEndlessMode();

        if (_saveGame)
            GameSaveManager.GetOrCreate().SaveGame();

        MainMenuManager.QueueEndlessUnlockedNotice();

        if (!loadMainMenuAfterFinalDialogFallback || string.IsNullOrWhiteSpace(mainMenuSceneName))
            return;

        Time.timeScale = 1f;
        SceneTransitionFadeController.RequestFadeInOnNextScene(fallbackMainMenuFadeDuration, 0f);
        SceneManager.LoadScene(mainMenuSceneName);
    }

    private GameObject CreateFallbackEndingOverlay(out CanvasGroup _canvasGroup, out TMP_Text _messageText)
    {
        GameObject root = new GameObject("CampaignFallbackEndingOverlay", typeof(Canvas), typeof(CanvasScaler), typeof(CanvasGroup), typeof(GraphicRaycaster));
        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = short.MaxValue;

        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        _canvasGroup = root.GetComponent<CanvasGroup>();
        _canvasGroup.alpha = 0f;
        _canvasGroup.blocksRaycasts = true;
        _canvasGroup.interactable = false;

        GameObject backgroundObject = new GameObject("Background", typeof(RectTransform), typeof(Image));
        RectTransform backgroundTransform = backgroundObject.GetComponent<RectTransform>();
        backgroundTransform.SetParent(root.transform, false);
        backgroundTransform.anchorMin = Vector2.zero;
        backgroundTransform.anchorMax = Vector2.one;
        backgroundTransform.offsetMin = Vector2.zero;
        backgroundTransform.offsetMax = Vector2.zero;

        Image background = backgroundObject.GetComponent<Image>();
        background.color = Color.black;
        background.raycastTarget = false;

        GameObject textObject = new GameObject("Message", typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform textTransform = textObject.GetComponent<RectTransform>();
        textTransform.SetParent(root.transform, false);
        textTransform.anchorMin = new Vector2(0.5f, 0.5f);
        textTransform.anchorMax = new Vector2(0.5f, 0.5f);
        textTransform.pivot = new Vector2(0.5f, 0.5f);
        textTransform.anchoredPosition = Vector2.zero;
        textTransform.sizeDelta = new Vector2(1200f, 240f);

        _messageText = textObject.GetComponent<TMP_Text>();
        _messageText.alignment = TextAlignmentOptions.Center;
        _messageText.color = Color.white;
        _messageText.fontSize = 72f;
        _messageText.textWrappingMode = TextWrappingModes.Normal;

        return root;
    }

    private static IEnumerator FadeCanvasGroup(CanvasGroup _canvasGroup, float _from, float _to, float _duration)
    {
        if (_canvasGroup == null)
            yield break;

        if (_duration <= 0f)
        {
            _canvasGroup.alpha = _to;
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < _duration)
        {
            elapsed += Time.unscaledDeltaTime;
            _canvasGroup.alpha = Mathf.Lerp(_from, _to, Mathf.Clamp01(elapsed / _duration));
            yield return null;
        }

        _canvasGroup.alpha = _to;
    }

    private void TryShowCampaignCompletion(bool _saveGame)
    {
        if (!showCampaignCompletionPanel || hasShownCampaignCompletion)
            return;

        hasShownCampaignCompletion = true;

        if (_saveGame)
            GameSaveManager.GetOrCreate().SaveGame();

        if (outcomePanel == null)
        {
            Debug.LogWarning("[CampaignOutcomeController] OutcomePanel não configurado.", this);
            return;
        }

        string message = campaignCompletionMessage.Replace("{0}", GetCurrentDebtValue().ToString());
        outcomePanel.ShowCompletion(campaignCompletionTitle, message, pauseOnCampaignCompletion);
    }

    #endregion

    #region Reference And Subscription Helpers

    private void ResolveReferences()
    {
        if (campaignProgress == null)
            campaignProgress = CampaignProgressSystem.GetOrCreate();

        if (outcomePanel == null)
            outcomePanel = FindFirstObjectByType<GameOutcomePanelUI>(FindObjectsInactive.Include);

        if (debtSystem == null)
            debtSystem = DebtSystem.GetOrCreate();

        if (cutsceneController == null)
            cutsceneController = FindFirstObjectByType<CampaignCutsceneController>(FindObjectsInactive.Include);

        if (roteiroDialogLibrary == null)
            roteiroDialogLibrary = RoteiroDialogPlayback.LoadLibrary();
    }

    private void Subscribe()
    {
        if (isSubscribed || campaignProgress == null)
            return;

        campaignProgress.OnProgressChanged += HandleProgressChanged;
        campaignProgress.OnQuestAdvanced += HandleQuestAdvanced;
        campaignProgress.OnQuestDeadlineExpired += HandleQuestDeadlineExpired;
        campaignProgress.OnCampaignCompleted += HandleCampaignCompleted;
        isSubscribed = true;
    }

    private void Unsubscribe()
    {
        if (!isSubscribed || campaignProgress == null)
            return;

        campaignProgress.OnProgressChanged -= HandleProgressChanged;
        campaignProgress.OnQuestAdvanced -= HandleQuestAdvanced;
        campaignProgress.OnQuestDeadlineExpired -= HandleQuestDeadlineExpired;
        campaignProgress.OnCampaignCompleted -= HandleCampaignCompleted;
        isSubscribed = false;
    }

    private void TryShowCurrentOutcome(bool _saveCompletion)
    {
        if (campaignProgress == null)
            return;

        if (campaignProgress.GameMode != GameProgressMode.Campaign &&
            campaignProgress.GameMode != GameProgressMode.Endless)
            return;

        if (campaignProgress.IsCampaignCompleted)
        {
            TryShowCampaignCompletion(_saveCompletion);
            return;
        }

        if (campaignProgress.HasFailedCurrentQuest)
            TryShowQuestFailure();
    }

    private bool IsQuestFailureHandledByTutorialGuidance()
    {
        if (campaignProgress == null || !campaignProgress.IsCurrentQuestTutorial)
            return false;

        CampaignQuestGuidanceController guidanceController = CampaignQuestGuidanceController.instance;

        return guidanceController != null &&
               guidanceController.isActiveAndEnabled &&
               (guidanceController.IsTutorialRunning || guidanceController.IsTutorialFailed);
    }

    private int GetCurrentDebtValue()
    {
        if (debtSystem == null)
            debtSystem = DebtSystem.GetOrCreate();

        return debtSystem != null ? debtSystem.CurrentDebt : 0;
    }

    #endregion
}
