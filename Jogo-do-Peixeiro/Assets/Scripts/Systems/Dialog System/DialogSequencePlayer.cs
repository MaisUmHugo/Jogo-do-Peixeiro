using System;
using UnityEngine;

public class DialogSequencePlayer : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TextCanvaManager textCanvaManager;

    [Header("State")]
    [SerializeField] private bool lockGameplayDuringDialog = true;
    [SerializeField] private bool restorePreviousState = true;
    [SerializeField] private GameManager.GameState fallbackStateAfterDialog = GameManager.GameState.OnFoot;

    private GameManager.GameState previousState;
    private bool hasStoredState;

    public bool IsPlaying => textCanvaManager != null && textCanvaManager.IsDialogActive;

    public static DialogSequencePlayer GetOrCreate()
    {
        DialogSequencePlayer player = FindFirstObjectByType<DialogSequencePlayer>(FindObjectsInactive.Include);

        if (player != null)
            return player;

        TextCanvaManager textManager = FindFirstObjectByType<TextCanvaManager>(FindObjectsInactive.Include);

        if (textManager == null)
            return null;

        player = textManager.GetComponent<DialogSequencePlayer>();

        if (player == null)
            player = textManager.gameObject.AddComponent<DialogSequencePlayer>();

        return player;
    }

    private void Awake()
    {
        ResolveReferences();
    }

    public void Play(DialogSequenceAsset _dialog)
    {
        Play(_dialog, null, null);
    }

    public void Play(DialogSequenceAsset _dialog, DialogCameraFocusTarget _focusTarget)
    {
        Play(_dialog, _focusTarget, null);
    }

    public void Play(DialogSequenceAsset _dialog, DialogCameraFocusTarget _focusTarget, Action _onFinished)
    {
        ResolveReferences();

        if (_dialog == null || !_dialog.HasLines || textCanvaManager == null)
        {
            _onFinished?.Invoke();
            return;
        }

        StoreAndLockState();
        textCanvaManager.InitializeDialog(_dialog.ToDialogSequenceData(), () =>
        {
            RestoreState();
            _onFinished?.Invoke();
        }, _focusTarget);
    }

    private void ResolveReferences()
    {
        if (textCanvaManager == null)
            textCanvaManager = FindFirstObjectByType<TextCanvaManager>(FindObjectsInactive.Include);
    }

    private void StoreAndLockState()
    {
        if (!lockGameplayDuringDialog || GameManager.instance == null)
            return;

        previousState = GameManager.instance.currentState;
        hasStoredState = true;
        GameManager.instance.SetState(GameManager.GameState.InUI);
    }

    private void RestoreState()
    {
        if (!lockGameplayDuringDialog || GameManager.instance == null)
            return;

        GameManager.GameState targetState = restorePreviousState && hasStoredState
            ? previousState
            : fallbackStateAfterDialog;

        GameManager.instance.SetState(targetState);
        hasStoredState = false;
    }
}
