using UnityEngine;

public class DialogInteractable : MonoBehaviour, IInteractable
{
    [Header("Dialog")]
    [SerializeField] private DialogSequenceAsset dialog;
    [SerializeField] private DialogSequenceAsset[] randomDialogPool;
    [SerializeField] private DialogSequencePlayer dialogPlayer;
    [SerializeField] private DialogCameraFocusTarget cameraFocusTarget;

    [Header("Interaction")]
    [SerializeField] private int interactionPriority = 0;
    [SerializeField] private bool playOnce;
    [SerializeField] private bool disableAfterPlayed;

    private bool hasPlayed;

    private void Awake()
    {
        ResolveReferences();
    }

    public void Interact()
    {
        if (!CanInteract())
            return;

        ResolveReferences();

        if (dialogPlayer == null)
            return;

        hasPlayed = true;
        dialogPlayer.Play(GetDialogToPlay(), cameraFocusTarget);

        if (disableAfterPlayed)
            enabled = false;
    }

    public int GetInteractionPriority()
    {
        return interactionPriority;
    }

    public bool CanInteract()
    {
        if (!enabled || !HasAnyDialog())
            return false;

        if (playOnce && hasPlayed)
            return false;

        return dialogPlayer == null || !dialogPlayer.IsPlaying;
    }

    private DialogSequenceAsset GetDialogToPlay()
    {
        if (randomDialogPool != null && randomDialogPool.Length > 0)
        {
            DialogSequenceAsset[] availableDialogs = System.Array.FindAll(
                randomDialogPool,
                candidate => candidate != null && candidate.HasLines
            );

            if (availableDialogs.Length > 0)
                return availableDialogs[Random.Range(0, availableDialogs.Length)];
        }

        return dialog;
    }

    private bool HasAnyDialog()
    {
        if (dialog != null && dialog.HasLines)
            return true;

        if (randomDialogPool == null)
            return false;

        for (int i = 0; i < randomDialogPool.Length; i++)
        {
            if (randomDialogPool[i] != null && randomDialogPool[i].HasLines)
                return true;
        }

        return false;
    }

    private void ResolveReferences()
    {
        if (dialogPlayer == null)
            dialogPlayer = FindFirstObjectByType<DialogSequencePlayer>(FindObjectsInactive.Include);

        if (cameraFocusTarget == null)
            cameraFocusTarget = GetComponentInChildren<DialogCameraFocusTarget>();
    }
}
