using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Playables;

public class CampaignTutorialDialogFlow : MonoBehaviour
{
    private const float GroundSnapRayStartHeight = 2f;
    private const float GroundSnapRayDistance = 8f;
    private const float GroundSnapExtraSkin = 0.02f;

    private enum TutorialDialogPosePreview
    {
        OpeningDialog,
        OpeningAfterDialog,
        MoneyLenderDialog,
        MoneyLenderAfterDialog
    }

    private enum TutorialDialogPoseContext
    {
        None,
        Opening,
        MoneyLender
    }

    [Serializable]
    private class DialogPoseSettings
    {
        [Header("Player")]
        public Transform dialogPoint = null;
        public Transform afterDialogPoint = null;

        [Header("Camera During Dialog")]
        public bool snapCameraForDialog = true;
        public bool useSpecificDialogCameraAngle = false;
        public Vector2 dialogCameraYawPitch = new Vector2(0f, 18f);
        public float dialogCameraBehindPlayerYawOffset = 0f;
        [Min(0f)] public float dialogCameraDistance = 0f;

        [Header("Camera After Dialog")]
        public bool snapCameraAfterDialog = true;
        public bool useSpecificAfterDialogCameraAngle = false;
        public Vector2 afterDialogCameraYawPitch = new Vector2(0f, 18f);
        public float afterDialogCameraBehindPlayerYawOffset = 0f;
        [Min(0f)] public float afterDialogCameraDistance = 0f;
    }

    [Header("Cutscene Timeline")]
    [SerializeField] private CampaignCutsceneController cutsceneController;
    [SerializeField] private bool playOpeningTimelineBeforeIntroSlides = true;
    [SerializeField] private PlayableDirector openingCutsceneDirector;
    [SerializeField] private bool playMoneyLenderTimelineBeforeDockOwner = true;
    [SerializeField] private PlayableDirector moneyLenderIntroCutsceneDirector;
    [SerializeField] private bool playRoteiroDialogsWhenTimelineMissing = true;
    [SerializeField] private RoteiroDialogLibrary roteiroDialogLibrary;

    [Header("Dialog Fallback Fade")]
    [SerializeField] private bool fadeBeforeOpeningDialogFallback = true;
    [SerializeField, Min(0f)] private float openingDialogFallbackFadeInDuration = 3f;
    [SerializeField, Min(0f)] private float openingDialogFallbackFadeInDelay = 1.5f;
    [SerializeField] private bool fadeAfterOpeningDialogFallback = true;
    [SerializeField] private bool fadeBeforeMoneyLenderDialogFallback = true;
    [SerializeField, Min(0f)] private float moneyLenderDialogFallbackFadeInDuration = 1.5f;
    [SerializeField, Min(0f)] private float moneyLenderDialogFallbackFadeInDelay = 0.1f;
    [SerializeField] private bool fadeAfterMoneyLenderDialogFallback = true;
    [SerializeField, Min(0f)] private float dialogFallbackFadeOutDuration = 0.75f;
    [SerializeField, Min(0f)] private float dialogFallbackFadeHoldDuration = 0.1f;
    [SerializeField, Min(0f)] private float dialogFallbackFadeBackInDuration = 0.75f;
    [SerializeField, Range(0, 10)] private int dialogFallbackPoseSettleFrames = 2;
    [SerializeField, Min(0f)] private float completionWatchdogDelay = 0.25f;

    [Header("Opening Dialog Pose")]
    [SerializeField] private DialogPoseSettings openingDialogPose = new DialogPoseSettings();
    [SerializeField] private bool rotatePlayerAfterOpeningDialog = true;
    [SerializeField] private float openingAfterDialogYawOffset = 0f;

    [Header("Money Lender Dialog Pose")]
    [SerializeField] private DialogPoseSettings moneyLenderDialogPose = new DialogPoseSettings();

    [Header("Play Mode Tuning")]
    [SerializeField] private bool previewDialogPoseInUpdate;
    [SerializeField] private TutorialDialogPosePreview previewDialogPose = TutorialDialogPosePreview.OpeningDialog;

    private Coroutine activeRoutine;
    private Action activeCompletion;
    private float activeFlowStartedAt;
    private bool isActiveFlow;
    private bool hasPlayedMoneyLenderIntro;
    private bool hasPlayedBoatBeforeIntroEdgeDialog;
    private TutorialDialogPoseContext activeDialogPoseContext = TutorialDialogPoseContext.None;
    private bool hasPlayerAfkIdleLock;
    private bool hasInteractionLock;
    private DialogSequencePlayer dialogPlayer;
    private TextCanvaManager textCanvaManager;

    public bool HasPlayedMoneyLenderIntro => hasPlayedMoneyLenderIntro;
    public bool HasPlayedBoatBeforeIntroEdgeDialog => hasPlayedBoatBeforeIntroEdgeDialog;
    public bool IsPlaying => isActiveFlow;

    private void Awake()
    {
        ResolveReferences();
    }

    private void Update()
    {
        RunCompletionWatchdog();
        UpdateDialogPosePlayModePreview();
    }

    private void OnDisable()
    {
        if (activeRoutine != null)
        {
            StopCoroutine(activeRoutine);
            activeRoutine = null;
        }

        ClearActiveFlow();
    }

    public void Configure(
        CampaignCutsceneController _cutsceneController,
        bool _playOpeningTimelineBeforeIntroSlides,
        PlayableDirector _openingCutsceneDirector,
        bool _playMoneyLenderTimelineBeforeDockOwner,
        PlayableDirector _moneyLenderIntroCutsceneDirector,
        bool _playRoteiroDialogsWhenTimelineMissing,
        RoteiroDialogLibrary _roteiroDialogLibrary,
        bool _fadeBeforeOpeningDialogFallback,
        float _openingDialogFallbackFadeInDuration,
        float _openingDialogFallbackFadeInDelay)
    {
        if (_cutsceneController != null)
            cutsceneController = _cutsceneController;

        playOpeningTimelineBeforeIntroSlides = _playOpeningTimelineBeforeIntroSlides;

        if (_openingCutsceneDirector != null)
            openingCutsceneDirector = _openingCutsceneDirector;

        playMoneyLenderTimelineBeforeDockOwner = _playMoneyLenderTimelineBeforeDockOwner;

        if (_moneyLenderIntroCutsceneDirector != null)
            moneyLenderIntroCutsceneDirector = _moneyLenderIntroCutsceneDirector;

        playRoteiroDialogsWhenTimelineMissing = _playRoteiroDialogsWhenTimelineMissing;

        if (_roteiroDialogLibrary != null)
            roteiroDialogLibrary = _roteiroDialogLibrary;

        fadeBeforeOpeningDialogFallback = _fadeBeforeOpeningDialogFallback;
        openingDialogFallbackFadeInDuration = Mathf.Max(0f, _openingDialogFallbackFadeInDuration);
        openingDialogFallbackFadeInDelay = Mathf.Max(0f, _openingDialogFallbackFadeInDelay);

        ResolveReferences();
    }

    public bool PlayOpeningIntro(Action _onFinished)
    {
        return PlayCutsceneControllerOrFallback(
            _tryPlayControllerCutscene: playOpeningTimelineBeforeIntroSlides ? TryPlayOpeningControllerCutscene : null,
            _fallbackDirector: openingCutsceneDirector,
            _selectFallbackDialogs: _library => new[] { _library.IntroMarinaLoja },
            _shouldPlayFallback: playOpeningTimelineBeforeIntroSlides,
            _onFinished: _onFinished,
            _fadeBeforeDialogFallback: fadeBeforeOpeningDialogFallback,
            _fadeInDuration: openingDialogFallbackFadeInDuration,
            _fadeInDelay: openingDialogFallbackFadeInDelay,
            _fadeAfterDialogFallback: fadeAfterOpeningDialogFallback,
            _poseSettings: openingDialogPose,
            _poseContext: TutorialDialogPoseContext.Opening);
    }

    public bool PlayMoneyLenderIntro(Action _onFinished)
    {
        if (hasPlayedMoneyLenderIntro)
        {
            _onFinished?.Invoke();
            return true;
        }

        return PlayCutsceneControllerOrFallback(
            _tryPlayControllerCutscene: playMoneyLenderTimelineBeforeDockOwner ? TryPlayMoneyLenderControllerCutscene : null,
            _fallbackDirector: moneyLenderIntroCutsceneDirector,
            _selectFallbackDialogs: _library => new[] { _library.IntroCobradorCabana },
            _shouldPlayFallback: playMoneyLenderTimelineBeforeDockOwner,
            _onFinished: () =>
            {
                hasPlayedMoneyLenderIntro = true;
                _onFinished?.Invoke();
            },
            _fadeBeforeDialogFallback: fadeBeforeMoneyLenderDialogFallback,
            _fadeInDuration: moneyLenderDialogFallbackFadeInDuration,
            _fadeInDelay: moneyLenderDialogFallbackFadeInDelay,
            _fadeAfterDialogFallback: fadeAfterMoneyLenderDialogFallback,
            _poseSettings: moneyLenderDialogPose,
            _poseContext: TutorialDialogPoseContext.MoneyLender);
    }

    public bool PlayBoatBeforeIntroEdgeDialog(Action _onFinished)
    {
        hasPlayedBoatBeforeIntroEdgeDialog = true;
        return PlayRoteiroDialogAsset(GetRoteiroDialogLibrary()?.EdgeBarcoAntesIntro, _onFinished);
    }

    private bool TryPlayOpeningControllerCutscene(Action _onFinished)
    {
        if (cutsceneController == null || !HasPlayable(cutsceneController.OpeningCutsceneDirector))
            return false;

        return cutsceneController.TryPlayOpeningCutscene(_onFinished);
    }

    private bool TryPlayMoneyLenderControllerCutscene(Action _onFinished)
    {
        if (cutsceneController == null || !HasPlayable(cutsceneController.MoneyLenderIntroCutsceneDirector))
            return false;

        return cutsceneController.TryPlayMoneyLenderIntroCutscene(_onFinished);
    }

    private bool PlayCutsceneControllerOrFallback(
        Func<Action, bool> _tryPlayControllerCutscene,
        PlayableDirector _fallbackDirector,
        Func<RoteiroDialogLibrary, DialogSequenceAsset[]> _selectFallbackDialogs,
        bool _shouldPlayFallback,
        Action _onFinished,
        bool _fadeBeforeDialogFallback,
        float _fadeInDuration,
        float _fadeInDelay,
        bool _fadeAfterDialogFallback,
        DialogPoseSettings _poseSettings,
        TutorialDialogPoseContext _poseContext)
    {
        if (isActiveFlow)
            return true;

        BeginActiveFlow(_onFinished, _poseContext);

        if (_tryPlayControllerCutscene != null && _tryPlayControllerCutscene.Invoke(CompleteActiveFlow))
            return true;

        if (TryPlayTutorialTimeline(_fallbackDirector, _shouldPlayFallback, CompleteActiveFlow))
            return true;

        if (TryPlayRoteiroDialogFallback(
                _selectFallbackDialogs,
                CompleteActiveFlow,
                _fadeBeforeDialogFallback,
                _fadeInDuration,
                _fadeInDelay,
                _fadeAfterDialogFallback,
                _poseSettings))
        {
            return true;
        }

        ClearActiveFlow();
        return false;
    }

    private bool TryPlayTutorialTimeline(PlayableDirector _director, bool _shouldPlay, Action _onFinished)
    {
        if (!_shouldPlay || !HasPlayable(_director))
            return false;

        if (activeRoutine != null)
            StopCoroutine(activeRoutine);

        activeRoutine = StartCoroutine(PlayTutorialTimelineRoutine(_director, _onFinished));
        return true;
    }

    private IEnumerator PlayTutorialTimelineRoutine(PlayableDirector _director, Action _onFinished)
    {
        bool finished = false;

        void HandleStopped(PlayableDirector _stoppedDirector)
        {
            if (_stoppedDirector == _director)
                finished = true;
        }

        _director.stopped += HandleStopped;
        _director.extrapolationMode = DirectorWrapMode.None;
        _director.time = 0;
        _director.Evaluate();
        _director.Play();

        while (!finished)
            yield return null;

        _director.stopped -= HandleStopped;
        activeRoutine = null;
        _onFinished?.Invoke();
    }

    private bool TryPlayRoteiroDialogFallback(
        Func<RoteiroDialogLibrary, DialogSequenceAsset[]> _selectFallbackDialogs,
        Action _onFinished,
        bool _fadeBeforeDialogFallback,
        float _fadeInDuration,
        float _fadeInDelay,
        bool _fadeAfterDialogFallback,
        DialogPoseSettings _poseSettings)
    {
        if (!playRoteiroDialogsWhenTimelineMissing || _selectFallbackDialogs == null)
            return false;

        RoteiroDialogLibrary library = GetRoteiroDialogLibrary();

        if (library == null)
            return false;

        DialogSequenceAsset[] dialogs = _selectFallbackDialogs.Invoke(library);

        if (!HasDialogFallback(dialogs))
            return false;

        if (_fadeBeforeDialogFallback && isActiveAndEnabled)
        {
            activeRoutine = StartCoroutine(PlayRoteiroDialogFallbackWithFadeRoutine(
                dialogs,
                _onFinished,
                _poseSettings,
                _fadeInDuration,
                _fadeInDelay,
                _fadeAfterDialogFallback));
            return true;
        }

        return PlayRoteiroDialogSequenceWithPose(dialogs, _onFinished, _poseSettings, true, _fadeAfterDialogFallback);
    }

    private IEnumerator PlayRoteiroDialogFallbackWithFadeRoutine(
        DialogSequenceAsset[] _dialogs,
        Action _onFinished,
        DialogPoseSettings _poseSettings,
        float _fadeInDuration,
        float _fadeInDelay,
        bool _fadeAfterDialogFallback)
    {
        GameManager.GameState? previousState = LockGameplayForDialogFade();
        SceneTransitionFadeController.SetBlackImmediate();
        PrepareDialogPose(_poseSettings);
        yield return WaitForDialogFallbackPoseToSettle();
        PrepareDialogPose(_poseSettings);
        yield return WaitForDialogFallbackPoseToSettle();

        yield return SceneTransitionFadeController.FadeInAndWait(
            _fadeInDuration,
            _fadeInDelay);

        activeRoutine = null;
        RestoreGameplayAfterDialogFade(previousState);

        if (!PlayRoteiroDialogSequenceWithPose(_dialogs, _onFinished, _poseSettings, false, _fadeAfterDialogFallback))
            CompleteDialogPose(_poseSettings, _onFinished, _fadeAfterDialogFallback);
    }

    private bool PlayRoteiroDialogAsset(DialogSequenceAsset _dialog, Action _onFinished)
    {
        if (_dialog == null || !_dialog.HasLines)
        {
            _onFinished?.Invoke();
            return true;
        }

        return PlayRoteiroDialogSequence(new[] { _dialog }, _onFinished);
    }

    private bool PlayRoteiroDialogSequence(DialogSequenceAsset[] _dialogs, Action _onFinished)
    {
        bool started = RoteiroDialogPlayback.TryPlaySequence(_dialogs, _onFinished);

        if (started)
            ResolveDialogPlayer();

        return started;
    }

    private bool PlayRoteiroDialogSequenceWithPose(
        DialogSequenceAsset[] _dialogs,
        Action _onFinished,
        DialogPoseSettings _poseSettings,
        bool _preparePose,
        bool _fadeAfterDialogFallback)
    {
        if (_preparePose)
            PrepareDialogPose(_poseSettings);

        bool started = PlayRoteiroDialogSequence(_dialogs, () => CompleteDialogPose(_poseSettings, _onFinished, _fadeAfterDialogFallback));

        if (!started && _preparePose)
            ApplyAfterDialogPose(_poseSettings, activeDialogPoseContext);

        return started;
    }

    private void PrepareDialogPose(DialogPoseSettings _poseSettings)
    {
        if (_poseSettings == null)
            return;

        MovePlayerToPoint(_poseSettings.dialogPoint);
        SnapCameraForDialogPose(_poseSettings, false);
    }

    private void CompleteDialogPose(DialogPoseSettings _poseSettings, Action _onFinished, bool _fadeAfterDialogFallback)
    {
        StopDialogPosePlayModePreview(activeDialogPoseContext);

        if (_fadeAfterDialogFallback && isActiveAndEnabled)
        {
            activeRoutine = StartCoroutine(CompleteDialogPoseWithFadeRoutine(
                _poseSettings,
                _onFinished,
                activeDialogPoseContext));
            return;
        }

        ApplyAfterDialogPose(
            _poseSettings,
            activeDialogPoseContext,
            ShouldRestoreDefaultGameplayCameraAfterDialog(activeDialogPoseContext));
        _onFinished?.Invoke();
    }

    private IEnumerator CompleteDialogPoseWithFadeRoutine(
        DialogPoseSettings _poseSettings,
        Action _onFinished,
        TutorialDialogPoseContext _poseContext)
    {
        GameManager.GameState? previousState = LockGameplayForDialogFade();
        yield return SceneTransitionFadeController.FadeOut(dialogFallbackFadeOutDuration);

        if (dialogFallbackFadeHoldDuration > 0f)
            yield return new WaitForSecondsRealtime(dialogFallbackFadeHoldDuration);

        bool restoreDefaultGameplayCamera = ShouldRestoreDefaultGameplayCameraAfterDialog(_poseContext);
        ApplyAfterDialogPose(_poseSettings, _poseContext, restoreDefaultGameplayCamera);
        yield return WaitForDialogFallbackPoseToSettle();
        ApplyAfterDialogPose(_poseSettings, _poseContext, restoreDefaultGameplayCamera);
        yield return WaitForDialogFallbackPoseToSettle();

        yield return SceneTransitionFadeController.FadeInAndWait(dialogFallbackFadeBackInDuration, 0f);

        activeRoutine = null;
        RestoreGameplayAfterDialogFade(previousState);
        _onFinished?.Invoke();
    }

    private static bool ShouldRestoreDefaultGameplayCameraAfterDialog(TutorialDialogPoseContext _poseContext)
    {
        return _poseContext == TutorialDialogPoseContext.Opening;
    }

    private void ApplyAfterDialogPose(
        DialogPoseSettings _poseSettings,
        TutorialDialogPoseContext _poseContext = TutorialDialogPoseContext.None,
        bool _restoreDefaultGameplayCamera = false)
    {
        if (_poseSettings == null)
            return;

        MovePlayerToPoint(_poseSettings.afterDialogPoint);
        ApplyAfterDialogPlayerRotation(_poseSettings, _poseContext);
        EndScriptedDialogCameraPose(false);

        if (_restoreDefaultGameplayCamera)
        {
            RestoreDefaultGameplayCameraForAfterDialogPose(_poseSettings);
            return;
        }

        SnapCameraForDialogPose(_poseSettings, true);
    }

    private void ApplyAfterDialogPlayerRotation(DialogPoseSettings _poseSettings, TutorialDialogPoseContext _poseContext)
    {
        if (_poseContext != TutorialDialogPoseContext.Opening ||
            !rotatePlayerAfterOpeningDialog ||
            Mathf.Approximately(openingAfterDialogYawOffset, 0f))
        {
            return;
        }

        Transform playerTransform = ResolvePlayerTransform();

        if (playerTransform == null)
            return;

        Vector3 baseEuler = _poseSettings.afterDialogPoint != null
            ? _poseSettings.afterDialogPoint.eulerAngles
            : playerTransform.eulerAngles;

        playerTransform.rotation = Quaternion.Euler(
            baseEuler.x,
            baseEuler.y + openingAfterDialogYawOffset,
            baseEuler.z);

        Physics.SyncTransforms();
        playerTransform.GetComponent<PlayerMove>()?.ResetMovementState();
        InputHandler.instance?.ResetGameplayInput();
    }

    private static void RestoreDefaultGameplayCameraForAfterDialogPose(DialogPoseSettings _poseSettings)
    {
        if (_poseSettings == null || PlayerCamera.Instance == null)
            return;

        Transform playerTransform = ResolvePlayerTransform();
        float targetYaw = _poseSettings.afterDialogCameraYawPitch.x;

        if (!_poseSettings.useSpecificAfterDialogCameraAngle && playerTransform != null)
            targetYaw = playerTransform.eulerAngles.y + _poseSettings.afterDialogCameraBehindPlayerYawOffset;

        PlayerCamera.Instance.RestoreDefaultGameplayCameraInstant(
            targetYaw,
            _poseSettings.afterDialogCameraYawPitch.y);
    }

    private void UpdateDialogPosePlayModePreview()
    {
        if (!previewDialogPoseInUpdate || !Application.isPlaying || !Application.isEditor)
            return;

        TutorialDialogPosePreview poseToApply = ResolvePreviewPoseForActiveContext(previewDialogPose, activeDialogPoseContext);

        if (!PreviewPoseTargetsContext(poseToApply, activeDialogPoseContext))
            return;

        switch (poseToApply)
        {
            case TutorialDialogPosePreview.OpeningAfterDialog:
                ApplyAfterDialogPose(openingDialogPose);
                break;

            case TutorialDialogPosePreview.MoneyLenderDialog:
                PrepareDialogPose(moneyLenderDialogPose);
                break;

            case TutorialDialogPosePreview.MoneyLenderAfterDialog:
                ApplyAfterDialogPose(moneyLenderDialogPose);
                break;

            default:
                PrepareDialogPose(openingDialogPose);
                break;
        }
    }

    private static TutorialDialogPosePreview ResolvePreviewPoseForActiveContext(
        TutorialDialogPosePreview _pose,
        TutorialDialogPoseContext _context)
    {
        bool wantsAfterDialog =
            _pose == TutorialDialogPosePreview.OpeningAfterDialog ||
            _pose == TutorialDialogPosePreview.MoneyLenderAfterDialog;

        switch (_context)
        {
            case TutorialDialogPoseContext.Opening:
                return wantsAfterDialog
                    ? TutorialDialogPosePreview.OpeningAfterDialog
                    : TutorialDialogPosePreview.OpeningDialog;

            case TutorialDialogPoseContext.MoneyLender:
                return wantsAfterDialog
                    ? TutorialDialogPosePreview.MoneyLenderAfterDialog
                    : TutorialDialogPosePreview.MoneyLenderDialog;

            default:
                return _pose;
        }
    }

    private static bool PreviewPoseTargetsContext(TutorialDialogPosePreview _pose, TutorialDialogPoseContext _context)
    {
        if (_context == TutorialDialogPoseContext.None)
            return false;

        switch (_pose)
        {
            case TutorialDialogPosePreview.OpeningDialog:
            case TutorialDialogPosePreview.OpeningAfterDialog:
                return _context == TutorialDialogPoseContext.Opening;

            case TutorialDialogPosePreview.MoneyLenderDialog:
            case TutorialDialogPosePreview.MoneyLenderAfterDialog:
                return _context == TutorialDialogPoseContext.MoneyLender;

            default:
                return false;
        }
    }

    private static void MovePlayerToPoint(Transform _point)
    {
        if (_point == null)
            return;

        Transform playerTransform = ResolvePlayerTransform();

        if (playerTransform == null)
            return;

        PlaceTransform(playerTransform, _point.position, _point.rotation, true);

        if (GameManager.instance != null)
            GameManager.instance.SetState(GameManager.GameState.OnFoot);
    }

    private static void SnapCameraForDialogPose(DialogPoseSettings _poseSettings, bool _afterDialog)
    {
        if (_poseSettings == null || PlayerCamera.Instance == null)
            return;

        bool shouldSnap = _afterDialog
            ? _poseSettings.snapCameraAfterDialog
            : _poseSettings.snapCameraForDialog;

        if (!shouldSnap)
            return;

        Transform playerTransform = ResolvePlayerTransform();
        bool useSpecificAngle = _afterDialog
            ? _poseSettings.useSpecificAfterDialogCameraAngle
            : _poseSettings.useSpecificDialogCameraAngle;
        Vector2 yawPitch = _afterDialog
            ? _poseSettings.afterDialogCameraYawPitch
            : _poseSettings.dialogCameraYawPitch;
        float yawOffset = _afterDialog
            ? _poseSettings.afterDialogCameraBehindPlayerYawOffset
            : _poseSettings.dialogCameraBehindPlayerYawOffset;
        float cameraDistance = _afterDialog
            ? _poseSettings.afterDialogCameraDistance
            : _poseSettings.dialogCameraDistance;

        float targetYaw = yawPitch.x;

        if (!useSpecificAngle && playerTransform != null)
            targetYaw = playerTransform.eulerAngles.y + yawOffset;

        if (!_afterDialog && PlayerCamera.Instance.TryShowScriptedDialogCameraPose(targetYaw, yawPitch.y, cameraDistance))
            return;

        PlayerCamera.Instance.SnapToGameplayTarget(targetYaw, yawPitch.y, cameraDistance);
    }

    private IEnumerator WaitForDialogFallbackPoseToSettle()
    {
        Physics.SyncTransforms();
        InputHandler.instance?.ResetGameplayInput();

        int frameCount = Mathf.Max(0, dialogFallbackPoseSettleFrames);

        for (int i = 0; i < frameCount; i++)
            yield return null;

        Physics.SyncTransforms();
    }

    private static void EndScriptedDialogCameraPose(bool _snapGameplayCamera)
    {
        if (PlayerCamera.Instance != null)
            PlayerCamera.Instance.EndScriptedDialogCameraPose(_snapGameplayCamera);
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

        Vector3 resolvedPosition = ResolveStableGroundedPosition(_transform, _position, characterController);
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

    private static Vector3 ResolveStableGroundedPosition(
        Transform _transform,
        Vector3 _position,
        CharacterController _characterController)
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

    private void BeginActiveFlow(Action _onFinished, TutorialDialogPoseContext _poseContext)
    {
        ResolveReferences();
        PushPlayerAfkIdleLock();
        PushInteractionLock();
        activeCompletion = _onFinished;
        activeFlowStartedAt = Time.unscaledTime;
        activeDialogPoseContext = _poseContext;
        isActiveFlow = true;
    }

    private void CompleteActiveFlow()
    {
        Action completion = activeCompletion;
        ClearActiveFlow();
        completion?.Invoke();
    }

    private void ClearActiveFlow()
    {
        StopDialogPosePlayModePreview(activeDialogPoseContext);
        EndScriptedDialogCameraPose(false);
        PopPlayerAfkIdleLock();
        PopInteractionLock();
        activeCompletion = null;
        activeDialogPoseContext = TutorialDialogPoseContext.None;
        isActiveFlow = false;
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

    private void StopDialogPosePlayModePreview(TutorialDialogPoseContext _poseContext)
    {
        if (!previewDialogPoseInUpdate || !Application.isPlaying || !Application.isEditor)
            return;

        TutorialDialogPosePreview poseToApply = ResolvePreviewPoseForActiveContext(previewDialogPose, _poseContext);

        if (PreviewPoseTargetsContext(poseToApply, _poseContext))
            previewDialogPoseInUpdate = false;
    }

    private void RunCompletionWatchdog()
    {
        if (!isActiveFlow)
            return;

        if (Time.unscaledTime - activeFlowStartedAt < completionWatchdogDelay)
            return;

        if (activeRoutine != null)
            return;

        if (cutsceneController != null && cutsceneController.IsPlaying)
            return;

        ResolveDialogPlayer();

        if (dialogPlayer != null && dialogPlayer.IsPlaying)
            return;

        if (textCanvaManager != null && textCanvaManager.IsDialogActive)
            return;

        if (SceneTransitionFadeController.IsGameplayBlocking)
            return;

        CompleteActiveFlow();
    }

    private void ResolveReferences()
    {
        if (cutsceneController == null)
            cutsceneController = FindFirstObjectByType<CampaignCutsceneController>(FindObjectsInactive.Include);

        ResolveDialogPlayer();

        if (textCanvaManager == null)
            textCanvaManager = FindFirstObjectByType<TextCanvaManager>(FindObjectsInactive.Include);
    }

    private void ResolveDialogPlayer()
    {
        if (dialogPlayer == null)
            dialogPlayer = FindFirstObjectByType<DialogSequencePlayer>(FindObjectsInactive.Include);
    }

    private RoteiroDialogLibrary GetRoteiroDialogLibrary()
    {
        if (roteiroDialogLibrary == null)
            roteiroDialogLibrary = RoteiroDialogPlayback.LoadLibrary();

        return roteiroDialogLibrary;
    }

    private static bool HasPlayable(PlayableDirector _director)
    {
        return _director != null && _director.playableAsset != null;
    }

    private static bool HasDialogFallback(DialogSequenceAsset[] _dialogs)
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

    private static GameManager.GameState? LockGameplayForDialogFade()
    {
        if (GameManager.instance == null)
            return null;

        GameManager.GameState previousState = GameManager.instance.currentState;
        GameManager.instance.SetState(GameManager.GameState.InUI);
        return previousState;
    }

    private static void RestoreGameplayAfterDialogFade(GameManager.GameState? _previousState)
    {
        if (!_previousState.HasValue || GameManager.instance == null)
            return;

        if (GameManager.instance.currentState == GameManager.GameState.InUI)
            GameManager.instance.SetState(_previousState.Value);
    }

    private static void SnapGameplayCameraToPlayer()
    {
        if (PlayerCamera.Instance != null)
            PlayerCamera.Instance.SnapToGameplayTarget();
    }

    private static void RestoreDefaultGameplayCameraInstant()
    {
        if (PlayerCamera.Instance != null)
            PlayerCamera.Instance.RestoreDefaultGameplayCameraInstant();
    }
}
