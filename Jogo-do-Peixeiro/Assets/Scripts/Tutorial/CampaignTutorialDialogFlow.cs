using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Playables;

public class CampaignTutorialDialogFlow : MonoBehaviour
{
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
    [SerializeField, Min(0f)] private float completionWatchdogDelay = 0.25f;

    private Coroutine activeRoutine;
    private Action activeCompletion;
    private float activeFlowStartedAt;
    private bool isActiveFlow;
    private bool hasPlayedMoneyLenderIntro;
    private bool hasPlayedBoatBeforeIntroEdgeDialog;
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
            _fadeBeforeDialogFallback: fadeBeforeOpeningDialogFallback);
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
            _fadeBeforeDialogFallback: false);
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
        bool _fadeBeforeDialogFallback)
    {
        if (isActiveFlow)
            return true;

        BeginActiveFlow(_onFinished);

        if (_tryPlayControllerCutscene != null && _tryPlayControllerCutscene.Invoke(CompleteActiveFlow))
            return true;

        if (TryPlayTutorialTimeline(_fallbackDirector, _shouldPlayFallback, CompleteActiveFlow))
            return true;

        if (TryPlayRoteiroDialogFallback(_selectFallbackDialogs, CompleteActiveFlow, _fadeBeforeDialogFallback))
            return true;

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
        bool _fadeBeforeDialogFallback)
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
            activeRoutine = StartCoroutine(PlayRoteiroDialogFallbackWithFadeRoutine(dialogs, _onFinished));
            return true;
        }

        return PlayRoteiroDialogSequence(dialogs, _onFinished);
    }

    private IEnumerator PlayRoteiroDialogFallbackWithFadeRoutine(DialogSequenceAsset[] _dialogs, Action _onFinished)
    {
        GameManager.GameState? previousState = LockGameplayForDialogFade();
        SceneTransitionFadeController.SetBlackImmediate();
        SnapGameplayCameraToPlayer();
        yield return null;
        SnapGameplayCameraToPlayer();

        yield return SceneTransitionFadeController.FadeInAndWait(
            openingDialogFallbackFadeInDuration,
            openingDialogFallbackFadeInDelay);

        activeRoutine = null;
        RestoreGameplayAfterDialogFade(previousState);

        if (!PlayRoteiroDialogSequence(_dialogs, _onFinished))
            _onFinished?.Invoke();
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

    private void BeginActiveFlow(Action _onFinished)
    {
        ResolveReferences();
        activeCompletion = _onFinished;
        activeFlowStartedAt = Time.unscaledTime;
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
        activeCompletion = null;
        isActiveFlow = false;
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
}
