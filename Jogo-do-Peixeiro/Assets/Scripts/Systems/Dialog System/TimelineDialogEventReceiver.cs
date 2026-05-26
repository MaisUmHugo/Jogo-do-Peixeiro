using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;

[Serializable]
public class TimelineDialogCue
{
    public string id;
    public DialogSequenceAsset dialog;
    public DialogCameraFocusTarget cameraFocusTarget;
}

[Serializable]
public class TimelineTutorialPanelCue
{
    public string id;
    public TutorialPanelSequence panelSequence;
}

public class TimelineDialogEventReceiver : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayableDirector director;
    [SerializeField] private DialogSequencePlayer dialogPlayer;

    [Header("Dialog Cues")]
    [SerializeField] private TimelineDialogCue[] dialogCues;

    [Header("Tutorial Panels")]
    [SerializeField] private TimelineTutorialPanelCue[] tutorialPanelCues;

    [Header("Gameplay Lock")]
    [SerializeField] private bool restorePreviousStateOnUnlock = true;
    [SerializeField] private GameManager.GameState fallbackStateAfterUnlock = GameManager.GameState.OnFoot;

    [Header("Fade Defaults")]
    [SerializeField, Min(0f)] private float defaultFadeDuration = 1f;

    [Header("Scene Flow")]
    [SerializeField] private string mainMenuSceneName = "Main Menu";

    private GameManager.GameState previousState;
    private bool hasStoredState;

    private void Awake()
    {
        ResolveReferences();
    }

    public void PlayDialogCue(int _index)
    {
        PlayDialogCueInternal(_index, false);
    }

    public void PlayDialogCueAndPauseTimeline(int _index)
    {
        PlayDialogCueInternal(_index, true);
    }

    public void PlayDialogCueById(string _id)
    {
        PlayDialogCueInternal(FindDialogCueIndex(_id), false);
    }

    public void PlayDialogCueByIdAndPauseTimeline(string _id)
    {
        PlayDialogCueInternal(FindDialogCueIndex(_id), true);
    }

    public void ShowTutorialPanelCue(int _index)
    {
        ShowTutorialPanelCueInternal(_index, false);
    }

    public void ShowTutorialPanelCueAndPauseTimeline(int _index)
    {
        ShowTutorialPanelCueInternal(_index, true);
    }

    public void ShowTutorialPanelCueById(string _id)
    {
        ShowTutorialPanelCueInternal(FindTutorialPanelCueIndex(_id), false);
    }

    public void ShowTutorialPanelCueByIdAndPauseTimeline(string _id)
    {
        ShowTutorialPanelCueInternal(FindTutorialPanelCueIndex(_id), true);
    }

    public void FocusCamera(DialogCameraFocusTarget _focusTarget)
    {
        TextCanvaManager.RequestCameraFocus(_focusTarget);
    }

    public void ClearCameraFocus()
    {
        TextCanvaManager.ClearCameraFocus();
    }

    public void StartUnlockedEndlessMode()
    {
        if (CampaignProgressSystem.Instance != null)
            CampaignProgressSystem.Instance.StartUnlockedEndlessMode();
    }

    public void SaveGame()
    {
        GameSaveManager.GetOrCreate().SaveGame();
    }

    public void QueueEndlessUnlockedNotice()
    {
        MainMenuManager.QueueEndlessUnlockedNotice();
    }

    public void LoadMainMenu()
    {
        Time.timeScale = 1f;
        SceneTransitionFadeController.RequestFadeInOnNextScene(defaultFadeDuration, 0f);
        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void FadeIn(float _duration)
    {
        SceneTransitionFadeController.FadeIn(_duration);
    }

    public void FadeInDefault()
    {
        FadeIn(defaultFadeDuration);
    }

    public void FadeOut(float _duration)
    {
        StartCoroutine(FadeOutRoutine(_duration));
    }

    public void FadeOutDefault()
    {
        FadeOut(defaultFadeDuration);
    }

    public void SetFadeBlackImmediate()
    {
        SceneTransitionFadeController.SetBlackImmediate();
    }

    public void SetBlackImmediateAndFadeInDefault()
    {
        SetFadeBlackImmediate();
        FadeInDefault();
    }

    public void LockGameplay()
    {
        if (GameManager.instance == null)
            return;

        previousState = GameManager.instance.currentState;
        hasStoredState = true;
        GameManager.instance.SetState(GameManager.GameState.InUI);
    }

    public void BeginCutsceneCameraMode()
    {
        PlayerCamera playerCamera = PlayerCamera.Instance;

        if (playerCamera == null)
            playerCamera = FindFirstObjectByType<PlayerCamera>(FindObjectsInactive.Include);

        if (playerCamera != null)
            playerCamera.BeginCutsceneCameraMode();
    }

    public void EndCutsceneCameraMode()
    {
        PlayerCamera playerCamera = PlayerCamera.Instance;

        if (playerCamera == null)
            playerCamera = FindFirstObjectByType<PlayerCamera>(FindObjectsInactive.Include);

        if (playerCamera != null)
            playerCamera.EndCutsceneCameraMode();
    }

    public void UnlockGameplay()
    {
        if (GameManager.instance == null)
            return;

        GameManager.GameState targetState = restorePreviousStateOnUnlock && hasStoredState
            ? previousState
            : fallbackStateAfterUnlock;

        GameManager.instance.SetState(targetState);
        hasStoredState = false;
    }

    public void ResumeTimeline()
    {
        if (director != null)
            director.Resume();
    }

    public void PauseTimeline()
    {
        if (director != null)
            director.Pause();
    }

    private void PlayDialogCueInternal(int _index, bool _pauseTimeline)
    {
        ResolveReferences();

        TimelineDialogCue cue = GetDialogCue(_index);
        if (cue == null || dialogPlayer == null)
        {
            if (_pauseTimeline)
                ResumeTimeline();

            return;
        }

        if (_pauseTimeline)
            PauseTimeline();

        dialogPlayer.Play(cue.dialog, cue.cameraFocusTarget, () =>
        {
            if (_pauseTimeline)
                ResumeTimeline();
        });
    }

    private void ShowTutorialPanelCueInternal(int _index, bool _pauseTimeline)
    {
        TimelineTutorialPanelCue cue = GetTutorialPanelCue(_index);
        if (cue == null || cue.panelSequence == null)
        {
            if (_pauseTimeline)
                ResumeTimeline();

            return;
        }

        if (_pauseTimeline)
            PauseTimeline();

        cue.panelSequence.Show(() =>
        {
            if (_pauseTimeline)
                ResumeTimeline();
        });
    }

    private TimelineDialogCue GetDialogCue(int _index)
    {
        if (dialogCues == null || _index < 0 || _index >= dialogCues.Length)
            return null;

        return dialogCues[_index];
    }

    private TimelineTutorialPanelCue GetTutorialPanelCue(int _index)
    {
        if (tutorialPanelCues == null || _index < 0 || _index >= tutorialPanelCues.Length)
            return null;

        return tutorialPanelCues[_index];
    }

    private int FindDialogCueIndex(string _id)
    {
        if (dialogCues == null || string.IsNullOrWhiteSpace(_id))
            return -1;

        for (int i = 0; i < dialogCues.Length; i++)
        {
            if (dialogCues[i] != null && dialogCues[i].id == _id)
                return i;
        }

        return -1;
    }

    private int FindTutorialPanelCueIndex(string _id)
    {
        if (tutorialPanelCues == null || string.IsNullOrWhiteSpace(_id))
            return -1;

        for (int i = 0; i < tutorialPanelCues.Length; i++)
        {
            if (tutorialPanelCues[i] != null && tutorialPanelCues[i].id == _id)
                return i;
        }

        return -1;
    }

    private IEnumerator FadeOutRoutine(float _duration)
    {
        yield return SceneTransitionFadeController.FadeOut(_duration);
    }

    private void ResolveReferences()
    {
        if (director == null)
            director = GetComponent<PlayableDirector>();

        if (dialogPlayer == null)
            dialogPlayer = FindFirstObjectByType<DialogSequencePlayer>(FindObjectsInactive.Include);
    }
}
