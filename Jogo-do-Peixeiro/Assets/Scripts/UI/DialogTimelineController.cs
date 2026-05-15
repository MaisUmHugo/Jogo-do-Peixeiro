using UnityEngine;
using UnityEngine.Playables;

public class DialogTimelineController : MonoBehaviour
{
    [Header("Timeline")]
    [SerializeField] private PlayableDirector director;
    [SerializeField] private bool playOnDialogStarted;
    [SerializeField] private bool stopOnDialogFinished = true;
    [SerializeField] private bool useUnscaledTime = true;

    [Header("Camera")]
    [SerializeField] private DialogCameraFocusTarget defaultFocusTarget;
    [SerializeField] private bool focusCameraOnTimelinePlay;
    [SerializeField] private bool clearCameraFocusOnStop = true;

    private void OnEnable()
    {
        TextCanvaManager.DialogStarted += HandleDialogStarted;
        TextCanvaManager.DialogFinished += HandleDialogFinished;
        ConfigureDirectorTimeMode();
    }

    private void OnDisable()
    {
        TextCanvaManager.DialogStarted -= HandleDialogStarted;
        TextCanvaManager.DialogFinished -= HandleDialogFinished;
    }

    public void PlayTimeline()
    {
        PlayTimeline(director);
    }

    public void PlayTimeline(PlayableDirector _director)
    {
        if (_director == null)
            return;

        director = _director;
        ConfigureDirectorTimeMode();

        if (focusCameraOnTimelinePlay)
            FocusCamera(defaultFocusTarget);

        director.Play();
    }

    public void StopTimeline()
    {
        if (director != null)
            director.Stop();

        if (clearCameraFocusOnStop)
            ClearCameraFocus();
    }

    public void PauseTimeline()
    {
        if (director != null)
            director.Pause();
    }

    public void ResumeTimeline()
    {
        if (director != null)
            director.Resume();
    }

    public void FocusCamera(DialogCameraFocusTarget _focusTarget)
    {
        TextCanvaManager.RequestCameraFocus(_focusTarget != null ? _focusTarget : defaultFocusTarget);
    }

    public void ClearCameraFocus()
    {
        TextCanvaManager.ClearCameraFocus();
    }

    private void HandleDialogStarted(DialogCameraFocusTarget _focusTarget)
    {
        if (!playOnDialogStarted)
            return;

        if (_focusTarget != null)
            defaultFocusTarget = _focusTarget;

        PlayTimeline();
    }

    private void HandleDialogFinished()
    {
        if (stopOnDialogFinished)
            StopTimeline();
    }

    private void ConfigureDirectorTimeMode()
    {
        if (director == null)
            return;

        director.timeUpdateMode = useUnscaledTime
            ? DirectorUpdateMode.UnscaledGameTime
            : DirectorUpdateMode.GameTime;
    }
}
