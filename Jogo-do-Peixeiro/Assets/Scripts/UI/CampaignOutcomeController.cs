using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class CampaignOutcomeController : MonoBehaviour
{
    #region Fields

    private const float GroundSnapRayStartHeight = 2f;
    private const float GroundSnapRayDistance = 8f;
    private const float GroundSnapExtraSkin = 0.02f;

    private enum FinalDialogFallbackPreviewPose
    {
        CameraOnly,
        StoreDialog,
        MorningDialog,
        EndlessStart
    }

    private enum FinalDialogFallbackPreviewPhase
    {
        None,
        StoreDialog,
        MorningDialog,
        EndlessStart
    }

    [Serializable]
    private class FinalDialogCameraPoseSettings
    {
        public bool useDefaultFinalDialogCamera = false;
        public bool snapCameraForDialog = true;
        public bool useSpecificDialogCameraAngle = false;
        public Vector2 dialogCameraYawPitch = new Vector2(0f, 18f);
        public float dialogCameraBehindPlayerYawOffset = 0f;
        [Min(0f)] public float dialogCameraDistance = 0f;
    }

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
    [SerializeField] private bool continueEndlessInCurrentSceneAfterFinalDialogFallback = true;
    [SerializeField] private bool deleteCampaignSaveAfterEndlessTransition = true;
    [SerializeField] private bool loadMainMenuAfterFinalDialogFallback = true;
    [SerializeField] private string mainMenuSceneName = "Main Menu";
    [SerializeField, Min(0f)] private float fallbackMainMenuFadeDuration = 1f;
    [SerializeField] private bool fadeBeforeFinalDialogFallback = true;
    [SerializeField, Min(0f)] private float finalDialogFallbackFadeInDuration = 3f;
    [SerializeField, Min(0f)] private float finalDialogFallbackFadeInDelay = 1.5f;
    [SerializeField, Min(0f)] private float finalDialogFallbackRestFadeOutDuration = 1.5f;
    [SerializeField, Min(0f)] private float finalDialogFallbackRestHoldDuration = 0.5f;
    [SerializeField, Range(0, 5)] private int finalDialogFallbackPoseSettleFrames = 1;
    [SerializeField, Range(0f, 24f)] private float finalDialogFallbackMorningHour = 6f;
    [SerializeField] private bool movePlayerToMoneyLenderForFinalDialogFallback = true;
    [SerializeField] private Transform finalDialogFallbackStorePoint;
    [SerializeField] private Transform finalDialogFallbackMorningPoint;
    [SerializeField] private bool movePlayerToEndlessStartPointAfterFinalDialog = true;
    [SerializeField, FormerlySerializedAs("finalDialogFallbackMoneyLenderPoint")] private Transform finalDialogFallbackEndlessStartPoint;
    [SerializeField] private bool moveMoneyLenderToFinalDialogActorPoint;
    [SerializeField] private Transform finalDialogFallbackMoneyLenderActorPoint;
    [SerializeField, Min(0f)] private float finalDialogFallbackMoneyLenderDistance = 2.25f;

    [Header("Final Dialog Camera")]
    [SerializeField] private bool snapCameraForFinalDialogFallback = true;
    [SerializeField] private bool useSpecificFinalDialogFallbackCameraAngle;
    [SerializeField] private Vector2 finalDialogFallbackCameraYawPitch = new Vector2(0f, 18f);
    [SerializeField] private float finalDialogFallbackCameraBehindPlayerYawOffset;
    [SerializeField, Min(0f)] private float finalDialogFallbackCameraDistance;
    [SerializeField] private FinalDialogCameraPoseSettings finalDialogFallbackStoreCameraPose = new FinalDialogCameraPoseSettings();
    [SerializeField] private FinalDialogCameraPoseSettings finalDialogFallbackMorningCameraPose = new FinalDialogCameraPoseSettings();
    [SerializeField] private FinalDialogCameraPoseSettings finalDialogFallbackEndlessStartCameraPose = new FinalDialogCameraPoseSettings();

    [Header("Final Dialog Play Mode Tuning")]
    [SerializeField] private bool previewFinalDialogFallbackInUpdate;
    [SerializeField] private FinalDialogFallbackPreviewPose previewFinalDialogFallbackPose = FinalDialogFallbackPreviewPose.MorningDialog;

    [SerializeField] private bool showFinalFadeScreenWhenUsingDialogFallback = true;
    [SerializeField] private string fallbackFinalText = "FIM?";
    [SerializeField] private string fallbackEndlessUnlockedText = "Modo sem fim foi liberado!";
    [SerializeField] private TMP_FontAsset fallbackEndingFont;
    [SerializeField, Min(0f)] private float fallbackFinalFadeInDuration = 1.5f;
    [SerializeField, Min(0f)] private float fallbackFinalHoldDuration = 1.2f;
    [SerializeField, Min(0f)] private float fallbackEndlessNoticeHoldDuration = 1.6f;
    [SerializeField, Min(0f)] private float fallbackFinalFadeOutDuration = 1.5f;
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
    private int campaignCompletionModalToken = UIModalManager.InvalidToken;
    private bool hasCampaignCompletionPreviousState;
    private GameManager.GameState campaignCompletionPreviousState;
    private FinalDialogFallbackPreviewPhase activeFinalDialogFallbackPreviewPhase = FinalDialogFallbackPreviewPhase.None;
    private bool hasPlayerAfkIdleLock;
    private bool hasInteractionLock;

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

    private void Update()
    {
        UpdateActiveFinalDialogFallbackCameraPose();
        UpdateFinalDialogFallbackPlayModePreview();
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

        EndCampaignCompletionPresentation();
        ClearFinalDialogFallbackPlayModePreview();
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

        CampaignQuestGuidanceController.instance?.HideTutorialGuidanceForCampaignFinal();
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
        CampaignQuestGuidanceController.instance?.HideTutorialGuidanceForCampaignFinal();
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
        EndCampaignCompletionPresentation();
        TryShowCampaignCompletion(_saveGame);
    }

    private bool TryPlayCampaignCompletionCutscene(bool _saveGame)
    {
        if (!playFinalCutsceneOnCampaignCompletion || hasStartedCampaignCompletionCutscene)
            return false;

        ResolveReferences();

        if (cutsceneController == null)
            return false;

        if (!cutsceneController.HasFinalCutsceneTimeline)
            return false;

        Action onFinished = () => FinishCampaignCompletionDialogFallback(_saveGame);

        if (!cutsceneController.TryPlayFinalCutscene(onFinished))
            return false;

        BeginCampaignCompletionPresentation(false);
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

        DialogSequenceAsset storeDialog = roteiroDialogLibrary.FimCampanhaLoja;
        DialogSequenceAsset airFishersDialog = roteiroDialogLibrary.FimCampanhaAirFishers;
        DialogSequenceAsset[] dialogs = new[] { storeDialog, airFishersDialog };

        if (!HasAnyDialogFallback(dialogs))
            return false;

        Action onFinished = () => CompleteCampaignCompletionDialogFallback(_saveGame);
        bool started;

        if (fadeBeforeFinalDialogFallback && isActiveAndEnabled)
        {
            pendingCampaignCompletionRoutine = StartCoroutine(PlayCampaignCompletionDialogFallbackWithFadeRoutine(storeDialog, airFishersDialog, onFinished));
            started = true;
        }
        else
        {
            started = TryPlayCampaignCompletionDialogFallbackSequence(storeDialog, airFishersDialog, onFinished);
        }

        if (!started)
            return false;

        BeginCampaignCompletionPresentation(false);
        hasStartedCampaignCompletionCutscene = true;
        hasShownCampaignCompletion = true;
        return true;
    }

    private IEnumerator PlayCampaignCompletionDialogFallbackWithFadeRoutine(
        DialogSequenceAsset _storeDialog,
        DialogSequenceAsset _airFishersDialog,
        Action _onFinished)
    {
        SceneTransitionFadeController.SetBlackImmediate();
        yield return PrepareFinalFallbackStoreDialogPoseRoutine();

        yield return SceneTransitionFadeController.FadeInAndWait(
            finalDialogFallbackFadeInDuration,
            finalDialogFallbackFadeInDelay);

        pendingCampaignCompletionRoutine = null;

        if (!TryPlayCampaignCompletionDialogFallbackSequence(_storeDialog, _airFishersDialog, _onFinished))
            _onFinished?.Invoke();
    }

    private bool TryPlayCampaignCompletionDialogFallbackSequence(
        DialogSequenceAsset _storeDialog,
        DialogSequenceAsset _airFishersDialog,
        Action _onFinished)
    {
        DialogSequencePlayer player = DialogSequencePlayer.GetOrCreate();

        if (player == null)
            return false;

        if (_storeDialog != null && _storeDialog.HasLines)
        {
            SetFinalDialogFallbackPreviewPhase(FinalDialogFallbackPreviewPhase.StoreDialog);
            PrepareFinalFallbackStoreDialogPose();
            player.Play(_storeDialog, null, () =>
            {
                StopFinalDialogFallbackPlayModePreview(FinalDialogFallbackPreviewPhase.StoreDialog);

                if (_airFishersDialog != null && _airFishersDialog.HasLines && isActiveAndEnabled)
                {
                    pendingCampaignCompletionRoutine = StartCoroutine(
                        PlayFinalRestTransitionThenDialogRoutine(player, _airFishersDialog, _onFinished));
                    return;
                }

                PlayDialogOrFinish(player, _airFishersDialog, _onFinished);
            });

            return true;
        }

        SetFinalDialogFallbackPreviewPhase(FinalDialogFallbackPreviewPhase.MorningDialog);
        PrepareFinalFallbackMorningWithoutDeadlinePenalty();
        PlayDialogOrFinish(player, _airFishersDialog, _onFinished);
        return _airFishersDialog != null && _airFishersDialog.HasLines;
    }

    private IEnumerator PlayFinalRestTransitionThenDialogRoutine(
        DialogSequencePlayer _player,
        DialogSequenceAsset _dialog,
        Action _onFinished)
    {
        yield return SceneTransitionFadeController.FadeOut(finalDialogFallbackRestFadeOutDuration);

        if (finalDialogFallbackRestHoldDuration > 0f)
            yield return new WaitForSecondsRealtime(finalDialogFallbackRestHoldDuration);

        yield return PrepareFinalFallbackMorningWithoutDeadlinePenaltyRoutine();
        SetFinalDialogFallbackPreviewPhase(FinalDialogFallbackPreviewPhase.MorningDialog);

        yield return SceneTransitionFadeController.FadeInAndWait(finalDialogFallbackFadeInDuration, 0f);
        pendingCampaignCompletionRoutine = null;
        PlayDialogOrFinish(_player, _dialog, _onFinished);
    }

    private void PrepareFinalFallbackMorningWithoutDeadlinePenalty()
    {
        ApplyFinalFallbackMorningWithoutDeadlinePenalty();
        MoveMoneyLenderToFinalFallbackPoint();
        MovePlayerToFinalFallbackMorningPoint();
        SnapCameraForFinalFallbackDialog(finalDialogFallbackMorningCameraPose);
    }

    private void PrepareFinalFallbackStoreDialogPose()
    {
        MovePlayerToFinalFallbackStorePoint();
        SnapCameraForFinalFallbackDialog(finalDialogFallbackStoreCameraPose);
    }

    private IEnumerator PrepareFinalFallbackStoreDialogPoseRoutine()
    {
        PrepareFinalFallbackStoreDialogPose();
        yield return WaitForFinalFallbackPoseToSettle();
        PrepareFinalFallbackStoreDialogPose();
        yield return WaitForFinalFallbackPoseToSettle();
    }

    private IEnumerator PrepareFinalFallbackMorningWithoutDeadlinePenaltyRoutine()
    {
        PrepareFinalFallbackMorningWithoutDeadlinePenalty();
        yield return WaitForFinalFallbackPoseToSettle();
        PrepareFinalFallbackMorningWithoutDeadlinePenalty();
        yield return WaitForFinalFallbackPoseToSettle();
    }

    private IEnumerator WaitForFinalFallbackPoseToSettle()
    {
        Physics.SyncTransforms();
        InputHandler.instance?.ResetGameplayInput();

        int frameCount = Mathf.Max(0, finalDialogFallbackPoseSettleFrames);

        for (int i = 0; i < frameCount; i++)
            yield return null;

        Physics.SyncTransforms();
    }

    private void UpdateFinalDialogFallbackPlayModePreview()
    {
        if (!previewFinalDialogFallbackInUpdate || !Application.isPlaying || !Application.isEditor)
            return;

        if (!PreviewPoseTargetsFinalPhase(previewFinalDialogFallbackPose, activeFinalDialogFallbackPreviewPhase))
            return;

        switch (previewFinalDialogFallbackPose)
        {
            case FinalDialogFallbackPreviewPose.StoreDialog:
                PrepareFinalFallbackStoreDialogPose();
                break;

            case FinalDialogFallbackPreviewPose.MorningDialog:
                PrepareFinalFallbackMorningWithoutDeadlinePenalty();
                break;

            case FinalDialogFallbackPreviewPose.EndlessStart:
                MovePlayerToFinalFallbackEndlessStartPoint();
                SnapCameraForFinalFallbackDialog(finalDialogFallbackEndlessStartCameraPose);
                break;

            default:
                SnapCameraForFinalFallbackDialog(GetCameraPoseSettingsForPhase(activeFinalDialogFallbackPreviewPhase));
                break;
        }
    }

    private void UpdateActiveFinalDialogFallbackCameraPose()
    {
        if (!Application.isPlaying)
            return;

        if (activeFinalDialogFallbackPreviewPhase == FinalDialogFallbackPreviewPhase.None)
            return;

        if (activeFinalDialogFallbackPreviewPhase == FinalDialogFallbackPreviewPhase.EndlessStart)
            return;

        SnapCameraForFinalFallbackDialog(GetCameraPoseSettingsForPhase(activeFinalDialogFallbackPreviewPhase));
    }

    private static bool PreviewPoseTargetsFinalPhase(FinalDialogFallbackPreviewPose _pose, FinalDialogFallbackPreviewPhase _phase)
    {
        if (_phase == FinalDialogFallbackPreviewPhase.None)
            return false;

        switch (_pose)
        {
            case FinalDialogFallbackPreviewPose.CameraOnly:
                return true;

            case FinalDialogFallbackPreviewPose.StoreDialog:
                return _phase == FinalDialogFallbackPreviewPhase.StoreDialog;

            case FinalDialogFallbackPreviewPose.MorningDialog:
                return _phase == FinalDialogFallbackPreviewPhase.MorningDialog;

            case FinalDialogFallbackPreviewPose.EndlessStart:
                return _phase == FinalDialogFallbackPreviewPhase.EndlessStart;

            default:
                return false;
        }
    }

    private void SetFinalDialogFallbackPreviewPhase(FinalDialogFallbackPreviewPhase _phase)
    {
        activeFinalDialogFallbackPreviewPhase = _phase;
    }

    private void StopFinalDialogFallbackPlayModePreview(FinalDialogFallbackPreviewPhase _phase)
    {
        if (previewFinalDialogFallbackInUpdate && Application.isPlaying && Application.isEditor &&
            PreviewPoseTargetsFinalPhase(previewFinalDialogFallbackPose, _phase))
        {
            previewFinalDialogFallbackInUpdate = false;
        }

        if (activeFinalDialogFallbackPreviewPhase == _phase)
            activeFinalDialogFallbackPreviewPhase = FinalDialogFallbackPreviewPhase.None;
    }

    private void ClearFinalDialogFallbackPlayModePreview()
    {
        activeFinalDialogFallbackPreviewPhase = FinalDialogFallbackPreviewPhase.None;

        if (previewFinalDialogFallbackInUpdate && Application.isPlaying && Application.isEditor)
            previewFinalDialogFallbackInUpdate = false;
    }

    private void ApplyFinalFallbackMorningWithoutDeadlinePenalty()
    {
        DayCycle dayCycle = FindFirstObjectByType<DayCycle>(FindObjectsInactive.Include);

        if (dayCycle == null)
            return;

        int nextCurrentDay = Mathf.Max(1, dayCycle.CurrentDay + 1);
        int nextElapsedDay = Mathf.Max(1, dayCycle.ElapsedDays + 1);
        float normalizedMorning = Mathf.Repeat(finalDialogFallbackMorningHour, 24f) / 24f;
        dayCycle.SetCycleState(nextCurrentDay, nextElapsedDay, normalizedMorning);
    }

    private void MovePlayerToFinalFallbackMorningPoint()
    {
        if (!movePlayerToMoneyLenderForFinalDialogFallback)
            return;

        Transform playerTransform = ResolvePlayerTransform();

        if (playerTransform == null)
            return;

        if (!TryResolveFinalFallbackMorningPose(playerTransform.rotation, out Vector3 position, out Quaternion rotation))
            return;

        PlaceTransform(playerTransform, position, rotation, true);

        if (GameManager.instance != null)
            GameManager.instance.SetState(GameManager.GameState.OnFoot);
    }

    private void MovePlayerToFinalFallbackStorePoint()
    {
        if (finalDialogFallbackStorePoint == null)
            return;

        Transform playerTransform = ResolvePlayerTransform();

        if (playerTransform == null)
            return;

        PlaceTransform(
            playerTransform,
            finalDialogFallbackStorePoint.position,
            finalDialogFallbackStorePoint.rotation,
            true);

        if (GameManager.instance != null)
            GameManager.instance.SetState(GameManager.GameState.OnFoot);
    }

    private void MoveMoneyLenderToFinalFallbackPoint()
    {
        if (!moveMoneyLenderToFinalDialogActorPoint || finalDialogFallbackMoneyLenderActorPoint == null)
            return;

        Transform moneyLenderTransform = ResolveMoneyLenderTransform();

        if (moneyLenderTransform == null)
            return;

        PlaceTransform(
            moneyLenderTransform,
            finalDialogFallbackMoneyLenderActorPoint.position,
            finalDialogFallbackMoneyLenderActorPoint.rotation,
            true);
    }

    private void MovePlayerToFinalFallbackEndlessStartPoint()
    {
        if (!movePlayerToEndlessStartPointAfterFinalDialog || finalDialogFallbackEndlessStartPoint == null)
            return;

        Transform playerTransform = ResolvePlayerTransform();

        if (playerTransform == null)
            return;

        PlaceTransform(
            playerTransform,
            finalDialogFallbackEndlessStartPoint.position,
            finalDialogFallbackEndlessStartPoint.rotation,
            true);

        if (GameManager.instance != null)
            GameManager.instance.SetState(GameManager.GameState.OnFoot);

        SnapGameplayCameraForFinalFallbackDialog(finalDialogFallbackEndlessStartCameraPose);
    }

    private bool TryResolveFinalFallbackMorningPose(
        Quaternion _fallbackRotation,
        out Vector3 _position,
        out Quaternion _rotation)
    {
        _position = default;
        _rotation = _fallbackRotation;

        if (finalDialogFallbackMorningPoint != null)
        {
            _position = finalDialogFallbackMorningPoint.position;
            _rotation = finalDialogFallbackMorningPoint.rotation;
            return true;
        }

        MoneyLenderController moneyLenderController = FindFirstObjectByType<MoneyLenderController>(FindObjectsInactive.Include);

        if (moneyLenderController != null)
            return TryResolveMoneyLenderPose(moneyLenderController.transform, moneyLenderController, _fallbackRotation, out _position, out _rotation);

        MoneyLender moneyLender = FindFirstObjectByType<MoneyLender>(FindObjectsInactive.Include);

        if (moneyLender == null)
            return false;

        _position = GetPositionInFrontOf(moneyLender.transform);
        _rotation = GetFacingRotation(_fallbackRotation, _position, moneyLender.transform);
        return true;
    }

    private bool TryResolveMoneyLenderPose(
        Transform _moneyLenderTransform,
        MoneyLenderController _moneyLenderController,
        Quaternion _fallbackRotation,
        out Vector3 _position,
        out Quaternion _rotation)
    {
        _position = default;
        _rotation = _fallbackRotation;

        if (_moneyLenderTransform == null)
            return false;

        InteractablePromptPoint promptPoint = _moneyLenderController.GetComponent<InteractablePromptPoint>();

        if (promptPoint == null)
            promptPoint = _moneyLenderController.GetComponentInChildren<InteractablePromptPoint>(true);

        if (promptPoint != null)
        {
            foreach (Transform point in promptPoint.GetPromptPoints())
            {
                if (point == null)
                    continue;

                _position = point.position;
                _rotation = GetFacingRotation(point.rotation, _position, _moneyLenderTransform);
                return true;
            }
        }

        _position = GetPositionInFrontOf(_moneyLenderTransform);
        _rotation = GetFacingRotation(_fallbackRotation, _position, _moneyLenderTransform);
        return true;
    }

    private Vector3 GetPositionInFrontOf(Transform _target)
    {
        if (_target == null)
            return Vector3.zero;

        Vector3 forward = _target.forward;

        if (forward.sqrMagnitude <= 0.0001f)
            forward = Vector3.forward;

        return _target.position + forward.normalized * finalDialogFallbackMoneyLenderDistance;
    }

    private static Quaternion GetFacingRotation(Quaternion _fallbackRotation, Vector3 _position, Transform _lookTarget)
    {
        if (_lookTarget == null)
            return _fallbackRotation;

        Vector3 direction = _lookTarget.position - _position;
        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.0001f)
            return _fallbackRotation;

        return Quaternion.LookRotation(direction.normalized, Vector3.up);
    }

    private static Transform ResolvePlayerTransform()
    {
        PlayerController playerController = FindFirstObjectByType<PlayerController>(FindObjectsInactive.Include);

        if (playerController != null)
            return playerController.transform;

        PlayerMove playerMove = FindFirstObjectByType<PlayerMove>(FindObjectsInactive.Include);

        if (playerMove != null)
            return playerMove.transform;

        CharacterController characterController = FindFirstObjectByType<CharacterController>(FindObjectsInactive.Include);
        return characterController != null ? characterController.transform : null;
    }

    private static Transform ResolveMoneyLenderTransform()
    {
        MoneyLenderController moneyLenderController = FindFirstObjectByType<MoneyLenderController>(FindObjectsInactive.Include);

        if (moneyLenderController != null)
            return moneyLenderController.transform;

        MoneyLender moneyLender = FindFirstObjectByType<MoneyLender>(FindObjectsInactive.Include);
        return moneyLender != null ? moneyLender.transform : null;
    }

    private static void PlaceTransform(Transform _transform, Vector3 _position, Quaternion _rotation, bool _handleCharacterController)
    {
        if (_transform == null)
            return;

        CharacterController characterController = _handleCharacterController
            ? _transform.GetComponent<CharacterController>()
            : null;
        bool restoreCharacterController = characterController != null && characterController.enabled;

        if (restoreCharacterController)
            characterController.enabled = false;

        Vector3 resolvedPosition = ResolveStableGroundedPosition(_position, characterController);
        _transform.SetPositionAndRotation(resolvedPosition, _rotation);

        if (restoreCharacterController)
        {
            characterController.enabled = true;
            characterController.Move(Vector3.down * 0.03f);
        }

        Physics.SyncTransforms();
        _transform.GetComponent<PlayerMove>()?.ResetMovementState();
        InputHandler.instance?.ResetGameplayInput();
    }

    private static Vector3 ResolveStableGroundedPosition(Vector3 _position, CharacterController _characterController)
    {
        Vector3 origin = _position + Vector3.up * GroundSnapRayStartHeight;

        if (!Physics.Raycast(
                origin,
                Vector3.down,
                out RaycastHit hit,
                GroundSnapRayStartHeight + GroundSnapRayDistance,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore))
        {
            return _position;
        }

        float yOffset = 0f;

        if (_characterController != null)
            yOffset = (_characterController.height * 0.5f) - _characterController.center.y + _characterController.skinWidth + GroundSnapExtraSkin;

        return new Vector3(_position.x, hit.point.y + yOffset, _position.z);
    }

    private FinalDialogCameraPoseSettings GetCameraPoseSettingsForPhase(FinalDialogFallbackPreviewPhase _phase)
    {
        switch (_phase)
        {
            case FinalDialogFallbackPreviewPhase.StoreDialog:
                return finalDialogFallbackStoreCameraPose;

            case FinalDialogFallbackPreviewPhase.MorningDialog:
                return finalDialogFallbackMorningCameraPose;

            case FinalDialogFallbackPreviewPhase.EndlessStart:
                return finalDialogFallbackEndlessStartCameraPose;

            default:
                return null;
        }
    }

    private void SnapCameraForFinalFallbackDialog(FinalDialogCameraPoseSettings _poseSettings)
    {
        if (!TryResolveFinalCameraPose(_poseSettings, out float targetYaw, out float targetPitch, out float cameraDistance))
            return;

        if (PlayerCamera.Instance.TryShowFinalDialogCameraPose(targetYaw, targetPitch, cameraDistance))
            return;

        PlayerCamera.Instance.SnapToGameplayTarget(targetYaw, targetPitch, cameraDistance);
    }

    private void SnapGameplayCameraForFinalFallbackDialog(FinalDialogCameraPoseSettings _poseSettings)
    {
        if (!TryResolveFinalCameraPose(_poseSettings, out float targetYaw, out float targetPitch, out float cameraDistance))
            return;

        PlayerCamera.Instance.EndFinalDialogCameraPose(false);
        PlayerCamera.Instance.SnapToGameplayTarget(targetYaw, targetPitch, cameraDistance);
    }

    private bool TryResolveFinalCameraPose(
        FinalDialogCameraPoseSettings _poseSettings,
        out float _targetYaw,
        out float _targetPitch,
        out float _cameraDistance)
    {
        _targetYaw = 0f;
        _targetPitch = 0f;
        _cameraDistance = 0f;

        if (!snapCameraForFinalDialogFallback || PlayerCamera.Instance == null)
            return false;

        bool useDefaultCameraPose = _poseSettings == null || _poseSettings.useDefaultFinalDialogCamera;
        bool shouldSnap = useDefaultCameraPose || _poseSettings.snapCameraForDialog;

        if (!shouldSnap)
            return false;

        Transform playerTransform = ResolvePlayerTransform();
        bool useSpecificAngle = useDefaultCameraPose
            ? useSpecificFinalDialogFallbackCameraAngle
            : _poseSettings.useSpecificDialogCameraAngle;
        Vector2 yawPitch = useDefaultCameraPose
            ? finalDialogFallbackCameraYawPitch
            : _poseSettings.dialogCameraYawPitch;
        float yawOffset = useDefaultCameraPose
            ? finalDialogFallbackCameraBehindPlayerYawOffset
            : _poseSettings.dialogCameraBehindPlayerYawOffset;
        _cameraDistance = useDefaultCameraPose
            ? finalDialogFallbackCameraDistance
            : _poseSettings.dialogCameraDistance;
        _targetYaw = yawPitch.x;
        _targetPitch = yawPitch.y;

        if (!useSpecificAngle && playerTransform != null)
            _targetYaw = playerTransform.eulerAngles.y + yawOffset;

        return true;
    }

    private static void SnapGameplayCameraToPlayer()
    {
        if (PlayerCamera.Instance != null)
        {
            PlayerCamera.Instance.EndScriptedDialogCameraPose(false);
            PlayerCamera.Instance.EndFinalDialogCameraPose(false);
            PlayerCamera.Instance.SnapToGameplayTarget();
        }
    }

    private static void EndFinalDialogCameraPose(bool _snapGameplayCamera)
    {
        if (PlayerCamera.Instance != null)
            PlayerCamera.Instance.EndFinalDialogCameraPose(_snapGameplayCamera);
    }

    private static void PlayDialogOrFinish(DialogSequencePlayer _player, DialogSequenceAsset _dialog, Action _onFinished)
    {
        if (_player != null && _dialog != null && _dialog.HasLines)
        {
            _player.Play(_dialog, null, _onFinished);
            return;
        }

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
        StopFinalDialogFallbackPlayModePreview(FinalDialogFallbackPreviewPhase.MorningDialog);
        EndFinalDialogCameraPose(false);

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

        SetFinalDialogFallbackPreviewPhase(FinalDialogFallbackPreviewPhase.EndlessStart);
        MovePlayerToFinalFallbackEndlessStartPoint();
        yield return WaitForFinalFallbackPoseToSettle();
        MovePlayerToFinalFallbackEndlessStartPoint();
        yield return WaitForFinalFallbackPoseToSettle();

        if (continueEndlessInCurrentSceneAfterFinalDialogFallback && fallbackFinalFadeOutDuration > 0f)
            yield return FadeCanvasGroup(canvasGroup, 1f, 0f, fallbackFinalFadeOutDuration);

        StopFinalDialogFallbackPlayModePreview(FinalDialogFallbackPreviewPhase.EndlessStart);
        Destroy(overlay);
        pendingCampaignCompletionRoutine = null;
        FinishCampaignCompletionDialogFallback(_saveGame);
    }

    private void FinishCampaignCompletionDialogFallback(bool _saveGame)
    {
        MovePlayerToFinalFallbackEndlessStartPoint();
        ClearFinalDialogFallbackPlayModePreview();
        EndFinalDialogCameraPose(true);
        EndCampaignCompletionPresentation();

        if (campaignProgress == null)
            campaignProgress = CampaignProgressSystem.GetOrCreate();

        if (!_saveGame)
            return;

        if (campaignProgress != null)
            campaignProgress.StartUnlockedEndlessMode();

        GameSaveManager saveManager = GameSaveManager.GetOrCreate();

        if (_saveGame)
            saveManager.SaveGame();

        if (deleteCampaignSaveAfterEndlessTransition)
            saveManager.DeleteSave(GameProgressMode.Campaign, false);

        if (continueEndlessInCurrentSceneAfterFinalDialogFallback)
        {
            if (!string.IsNullOrWhiteSpace(fallbackEndlessUnlockedText))
                HUDWarningUI.Instance?.ShowWarning(fallbackEndlessUnlockedText);

            return;
        }

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
        if (fallbackEndingFont != null)
            _messageText.font = fallbackEndingFont;

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

    private void BeginCampaignCompletionPresentation(bool _pauseTime)
    {
        CampaignQuestGuidanceController.instance?.HideTutorialGuidanceForCampaignFinal();
        PushPlayerAfkIdleLock();
        PushInteractionLock();

        if (campaignCompletionModalToken == UIModalManager.InvalidToken)
        {
            UIModalRequest request = UIModalRequest.Create(
                this,
                _pauseTime,
                true,
                true,
                true);

            campaignCompletionModalToken = UIModalManager.PushModal(request);
        }

        if (GameManager.instance == null || hasCampaignCompletionPreviousState)
            return;

        campaignCompletionPreviousState = GameManager.instance.currentState;
        hasCampaignCompletionPreviousState = true;
        GameManager.instance.SetState(GameManager.GameState.InUI);
        InputHandler.instance?.ResetGameplayInput();
    }

    private void EndCampaignCompletionPresentation()
    {
        if (campaignCompletionModalToken != UIModalManager.InvalidToken)
            UIModalManager.PopModal(ref campaignCompletionModalToken);

        if (GameManager.instance != null && hasCampaignCompletionPreviousState)
            GameManager.instance.SetState(campaignCompletionPreviousState);

        hasCampaignCompletionPreviousState = false;
        campaignCompletionPreviousState = default;
        PopPlayerAfkIdleLock();
        PopInteractionLock();
    }

    private void PushPlayerAfkIdleLock()
    {
        if (hasPlayerAfkIdleLock)
            return;

        PlayerAnimationController.PushAfkIdleLock();
        hasPlayerAfkIdleLock = true;
    }

    private void PopPlayerAfkIdleLock()
    {
        if (!hasPlayerAfkIdleLock)
            return;

        PlayerAnimationController.PopAfkIdleLock();
        hasPlayerAfkIdleLock = false;
    }

    private void PushInteractionLock()
    {
        if (hasInteractionLock)
            return;

        PlayerInteract.PushInteractionLock();
        hasInteractionLock = true;
    }

    private void PopInteractionLock()
    {
        if (!hasInteractionLock)
            return;

        PlayerInteract.PopInteractionLock();
        hasInteractionLock = false;
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
