using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Playables;

public class CampaignCutsceneController : MonoBehaviour
{
    public static CampaignCutsceneController Instance { get; private set; }

    [Header("Runtime")]
    [SerializeField] private bool requireCampaignTutorialQuest = true;
    [SerializeField] private bool playOpeningOnSceneStartWhenTutorialIsDisabled = true;
    [SerializeField] private bool handleMoneyLenderIntroWhenTutorialIsDisabled = true;
    [SerializeField] private bool playMoneyLenderIntroOnlyOnce = true;

    [Header("Timelines")]
    [SerializeField] private PlayableDirector openingCutsceneDirector;
    [SerializeField] private PlayableDirector moneyLenderIntroCutsceneDirector;
    [SerializeField] private PlayableDirector finalCutsceneDirector;

    [Header("Dialog Fallback")]
    [SerializeField] private bool playRoteiroDialogsWhenTimelineMissing = true;
    [SerializeField] private RoteiroDialogLibrary dialogLibrary;
    [SerializeField] private bool fadeBeforeOpeningDialogFallback = true;
    [SerializeField] private bool fadeBeforeFinalDialogFallback = true;
    [SerializeField, Min(0f)] private float dialogFallbackFadeInDuration = 3f;
    [SerializeField, Min(0f)] private float dialogFallbackFadeInDelay = 1.5f;

    [Header("Camera Cleanup")]
    [SerializeField] private bool restorePlayerCameraAfterCutscene = true;
    [SerializeField] private bool manageCutsceneVirtualCameras = true;
    [SerializeField] private string cutsceneVirtualCameraNamePrefix = "VCam_";
    [SerializeField] private CinemachineCamera[] cutsceneVirtualCameras;

    private Coroutine activeTimelineRoutine;
    private bool hasPlayedOpeningCutscene;
    private bool hasPlayedMoneyLenderIntroCutscene;
    private CinemachineCamera[] cachedCutsceneVirtualCameras;

    public bool IsPlaying { get; private set; }
    public PlayableDirector OpeningCutsceneDirector => openingCutsceneDirector;
    public PlayableDirector MoneyLenderIntroCutsceneDirector => moneyLenderIntroCutsceneDirector;
    public PlayableDirector FinalCutsceneDirector => finalCutsceneDirector;
    public bool HasFinalCutsceneTimeline => HasPlayable(finalCutsceneDirector);

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        SetCutsceneVirtualCamerasEnabled(false);
    }

    private void OnEnable()
    {
        TutorialEvents.MoneyLenderInteractionRequested += HandleMoneyLenderInteraction;
    }

    private void Start()
    {
        if (!playOpeningOnSceneStartWhenTutorialIsDisabled)
            return;

        if (IsTutorialControllerActive())
            return;

        TryPlayOpeningCutscene(null);
    }

    private void OnDisable()
    {
        TutorialEvents.MoneyLenderInteractionRequested -= HandleMoneyLenderInteraction;

        if (activeTimelineRoutine != null)
        {
            StopCoroutine(activeTimelineRoutine);
            activeTimelineRoutine = null;
        }

        IsPlaying = false;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public bool TryPlayOpeningCutscene(Action _onFinished)
    {
        if (hasPlayedOpeningCutscene)
            return false;

        return TryPlayCutsceneOrDialog(
            openingCutsceneDirector,
            _library => new[] { _library.IntroMarinaLoja },
            _onFinished,
            () => hasPlayedOpeningCutscene = true,
            CanPlayInCurrentTutorialContext,
            fadeBeforeOpeningDialogFallback);
    }

    public bool TryPlayMoneyLenderIntroCutscene(Action _onFinished)
    {
        if (playMoneyLenderIntroOnlyOnce && hasPlayedMoneyLenderIntroCutscene)
            return false;

        return TryPlayCutsceneOrDialog(
            moneyLenderIntroCutsceneDirector,
            _library => new[] { _library.IntroCobradorCabana },
            _onFinished,
            () => hasPlayedMoneyLenderIntroCutscene = true,
            CanPlayInCurrentTutorialContext,
            false);
    }

    public bool TryPlayFinalCutscene(Action _onFinished)
    {
        return TryPlayCutsceneOrDialog(
            finalCutsceneDirector,
            _library => new[] { _library.FimCampanhaLoja, _library.FimCampanhaAirFishers },
            _onFinished,
            null,
            CanPlayFinalCutsceneContext,
            fadeBeforeFinalDialogFallback);
    }

    public bool ForcePlayOpeningCutscene(Action _onFinished = null)
    {
        return TryPlayCutsceneOrDialog(
            openingCutsceneDirector,
            _library => new[] { _library.IntroMarinaLoja },
            _onFinished,
            () => hasPlayedOpeningCutscene = true,
            null,
            fadeBeforeOpeningDialogFallback);
    }

    public bool ForcePlayMoneyLenderIntroCutscene(Action _onFinished = null)
    {
        return TryPlayCutsceneOrDialog(
            moneyLenderIntroCutsceneDirector,
            _library => new[] { _library.IntroCobradorCabana },
            _onFinished,
            () => hasPlayedMoneyLenderIntroCutscene = true,
            null,
            false);
    }

    public bool ForcePlayFinalCutscene(Action _onFinished = null)
    {
        return TryPlayCutsceneOrDialog(
            finalCutsceneDirector,
            _library => new[] { _library.FimCampanhaLoja, _library.FimCampanhaAirFishers },
            _onFinished,
            null,
            null,
            fadeBeforeFinalDialogFallback);
    }

    public void PlayOpeningCutscene()
    {
        TryPlayOpeningCutscene(null);
    }

    public void PlayMoneyLenderIntroCutscene()
    {
        TryPlayMoneyLenderIntroCutscene(null);
    }

    public void PlayFinalCutscene()
    {
        TryPlayFinalCutscene(null);
    }

    private bool HandleMoneyLenderInteraction(MoneyLender _moneyLender, PaymentUI _paymentUI, MoneyLenderUI _moneyLenderUI)
    {
        if (!handleMoneyLenderIntroWhenTutorialIsDisabled || IsTutorialControllerActive())
            return false;

        return TryPlayMoneyLenderIntroCutscene(() => OpenMoneyLenderUi(_moneyLender, _paymentUI, _moneyLenderUI));
    }

    private bool TryPlayCutsceneOrDialog(
        PlayableDirector _director,
        Func<RoteiroDialogLibrary, DialogSequenceAsset[]> _selectFallbackDialogs,
        Action _onFinished,
        Action _onStarted,
        Func<bool> _canPlayInCurrentContext,
        bool _fadeBeforeDialogFallback = false)
    {
        if (!isActiveAndEnabled || IsPlaying)
            return false;

        if (_canPlayInCurrentContext != null && !_canPlayInCurrentContext.Invoke())
            return false;

        if (HasPlayable(_director))
        {
            _onStarted?.Invoke();
            activeTimelineRoutine = StartCoroutine(PlayCutsceneRoutine(_director, _onFinished));
            return true;
        }

        return TryPlayDialogFallback(_selectFallbackDialogs, _onFinished, _onStarted, _fadeBeforeDialogFallback);
    }

    private IEnumerator PlayCutsceneRoutine(PlayableDirector _director, Action _onFinished)
    {
        IsPlaying = true;
        bool finished = false;

        void HandleStopped(PlayableDirector _stoppedDirector)
        {
            if (_stoppedDirector == _director)
                finished = true;
        }

        _director.stopped += HandleStopped;
        _director.extrapolationMode = DirectorWrapMode.None;
        _director.time = 0;
        PrepareCameraForCutscene();
        _director.Evaluate();
        _director.Play();

        try
        {
            while (!finished)
                yield return null;
        }
        finally
        {
            _director.stopped -= HandleStopped;
            activeTimelineRoutine = null;
            IsPlaying = false;
            RestoreCameraAfterCutscene();
        }

        _onFinished?.Invoke();
    }

    private bool CanPlayInCurrentTutorialContext()
    {
        if (!requireCampaignTutorialQuest)
            return true;

        CampaignProgressSystem campaignProgress = CampaignProgressSystem.Instance;

        if (campaignProgress == null)
            campaignProgress = CampaignProgressSystem.GetOrCreate();

        return campaignProgress != null &&
               campaignProgress.IsCurrentQuestTutorial &&
               !campaignProgress.HasFailedCurrentQuest &&
               !campaignProgress.IsCampaignCompleted;
    }

    private bool CanPlayFinalCutsceneContext()
    {
        if (!requireCampaignTutorialQuest)
            return true;

        CampaignProgressSystem campaignProgress = CampaignProgressSystem.Instance;

        if (campaignProgress == null)
            campaignProgress = CampaignProgressSystem.GetOrCreate();

        return campaignProgress != null &&
               campaignProgress.GameMode == GameProgressMode.Campaign &&
               campaignProgress.IsCampaignCompleted &&
               !campaignProgress.HasFailedCurrentQuest;
    }

    private static bool HasPlayable(PlayableDirector _director)
    {
        return _director != null && _director.playableAsset != null;
    }

    private bool TryPlayDialogFallback(
        Func<RoteiroDialogLibrary, DialogSequenceAsset[]> _selectFallbackDialogs,
        Action _onFinished,
        Action _onStarted,
        bool _fadeBeforeDialogFallback)
    {
        if (!playRoteiroDialogsWhenTimelineMissing || _selectFallbackDialogs == null)
            return false;

        if (dialogLibrary == null)
            dialogLibrary = RoteiroDialogPlayback.LoadLibrary();

        if (dialogLibrary == null)
            return false;

        DialogSequenceAsset[] dialogs = _selectFallbackDialogs.Invoke(dialogLibrary);

        if (!HasDialogFallback(dialogs))
            return false;

        _onStarted?.Invoke();
        IsPlaying = true;

        if (_fadeBeforeDialogFallback && isActiveAndEnabled)
        {
            activeTimelineRoutine = StartCoroutine(PlayDialogFallbackWithFadeRoutine(dialogs, _onFinished));
            return true;
        }

        if (!RoteiroDialogPlayback.TryPlaySequence(dialogs, () =>
        {
            IsPlaying = false;
            _onFinished?.Invoke();
        }))
        {
            IsPlaying = false;
            return false;
        }

        return true;
    }

    private IEnumerator PlayDialogFallbackWithFadeRoutine(DialogSequenceAsset[] _dialogs, Action _onFinished)
    {
        SceneTransitionFadeController.SetBlackImmediate();
        yield return SceneTransitionFadeController.FadeInAndWait(
            dialogFallbackFadeInDuration,
            dialogFallbackFadeInDelay);

        activeTimelineRoutine = null;

        if (!RoteiroDialogPlayback.TryPlaySequence(_dialogs, () =>
        {
            IsPlaying = false;
            _onFinished?.Invoke();
        }))
        {
            IsPlaying = false;
            _onFinished?.Invoke();
        }
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

    private void PrepareCameraForCutscene()
    {
        SetCutsceneVirtualCamerasEnabled(true);

        if (!restorePlayerCameraAfterCutscene)
            return;

        PlayerCamera playerCamera = GetPlayerCamera();

        if (playerCamera != null)
            playerCamera.BeginCutsceneCameraMode();
    }

    private void RestoreCameraAfterCutscene()
    {
        SetCutsceneVirtualCamerasEnabled(false);

        if (!restorePlayerCameraAfterCutscene)
            return;

        PlayerCamera playerCamera = GetPlayerCamera();

        if (playerCamera != null)
            playerCamera.RestoreGameplayCameraAfterCutscene();
    }

    private void SetCutsceneVirtualCamerasEnabled(bool _enabled)
    {
        if (!manageCutsceneVirtualCameras)
            return;

        CinemachineCamera[] cameras = GetCutsceneVirtualCameras();

        if (cameras == null)
            return;

        for (int i = 0; i < cameras.Length; i++)
        {
            if (cameras[i] != null)
                cameras[i].enabled = _enabled;
        }
    }

    private CinemachineCamera[] GetCutsceneVirtualCameras()
    {
        if (cutsceneVirtualCameras != null && cutsceneVirtualCameras.Length > 0)
            return cutsceneVirtualCameras;

        if (cachedCutsceneVirtualCameras != null)
            return cachedCutsceneVirtualCameras;

        CinemachineCamera[] sceneCameras = FindObjectsByType<CinemachineCamera>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
        List<CinemachineCamera> matches = new List<CinemachineCamera>();

        for (int i = 0; i < sceneCameras.Length; i++)
        {
            CinemachineCamera sceneCamera = sceneCameras[i];

            if (sceneCamera == null || !IsCutsceneVirtualCamera(sceneCamera))
                continue;

            matches.Add(sceneCamera);
        }

        cachedCutsceneVirtualCameras = matches.ToArray();
        return cachedCutsceneVirtualCameras;
    }

    private bool IsCutsceneVirtualCamera(CinemachineCamera _camera)
    {
        if (_camera == null)
            return false;

        if (string.IsNullOrWhiteSpace(cutsceneVirtualCameraNamePrefix))
            return true;

        return _camera.name.StartsWith(cutsceneVirtualCameraNamePrefix, StringComparison.OrdinalIgnoreCase);
    }

    private static PlayerCamera GetPlayerCamera()
    {
        PlayerCamera playerCamera = PlayerCamera.Instance;

        if (playerCamera == null)
            playerCamera = FindFirstObjectByType<PlayerCamera>(FindObjectsInactive.Include);

        return playerCamera;
    }

    private static bool IsTutorialControllerActive()
    {
        CampaignQuestGuidanceController tutorial = CampaignQuestGuidanceController.instance;

        return tutorial != null &&
               tutorial.isActiveAndEnabled &&
               tutorial.IsTutorialEnabled;
    }

    private static void OpenMoneyLenderUi(MoneyLender _moneyLender, PaymentUI _paymentUI, MoneyLenderUI _moneyLenderUI)
    {
        if (_moneyLender == null)
            return;

        if (_paymentUI != null)
        {
            _paymentUI.Open(_moneyLender);
            return;
        }

        if (_moneyLenderUI != null)
            _moneyLenderUI.Open(_moneyLender);
    }
}
