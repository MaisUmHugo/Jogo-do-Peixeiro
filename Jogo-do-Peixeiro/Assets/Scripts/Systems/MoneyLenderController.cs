using UnityEngine;

public class MoneyLenderController : MonoBehaviour, IInteractable
{
    [Header("References")]
    [SerializeField] private MoneyLender moneyLender;
    [SerializeField] private PaymentUI paymentUI;
    [SerializeField] private MoneyLenderUI moneyLenderUI;

    [Header("Optional Dialog")]
    [SerializeField] private bool playDialogBeforeOpeningUi;
    [SerializeField] private DialogSequencePlayer dialogPlayer;
    [SerializeField] private DialogSequenceAsset[] preOpenDialogPool;
    [SerializeField] private DialogCameraFocusTarget dialogFocusTarget;

    private bool isWaitingDialogToOpenUi;

    public void Interact()
    {
        if (moneyLender == null)
            return;

        if (TutorialEvents.TryHandleMoneyLenderInteraction(moneyLender, paymentUI, moneyLenderUI))
            return;

        if (TryPlayPreOpenDialog())
            return;

        OpenUi();
    }

    public bool CanInteract()
    {
        return !isWaitingDialogToOpenUi;
    }

    public void ConfigureOptionalDialogs(
        DialogSequencePlayer _dialogPlayer,
        DialogSequenceAsset[] _preOpenDialogPool,
        DialogCameraFocusTarget _dialogFocusTarget = null,
        bool _playDialogBeforeOpeningUi = true)
    {
        dialogPlayer = _dialogPlayer;
        preOpenDialogPool = _preOpenDialogPool;
        dialogFocusTarget = _dialogFocusTarget != null ? _dialogFocusTarget : dialogFocusTarget;
        playDialogBeforeOpeningUi = _playDialogBeforeOpeningUi && preOpenDialogPool != null && preOpenDialogPool.Length > 0;
    }

    public int GetInteractionPriority()
    {
        return 50;
    }

    private void OpenUi()
    {
        if (paymentUI != null)
        {
            paymentUI.Open(moneyLender);
            return;
        }

        if (moneyLenderUI != null)
            moneyLenderUI.Open(moneyLender);
    }

    private bool TryPlayPreOpenDialog()
    {
        if (!playDialogBeforeOpeningUi)
            return false;

        DialogSequenceAsset dialog = GetRandomPreOpenDialog();

        if (dialog == null)
            return false;

        if (dialogPlayer == null)
            dialogPlayer = FindFirstObjectByType<DialogSequencePlayer>(FindObjectsInactive.Include);

        if (dialogFocusTarget == null)
            dialogFocusTarget = GetComponentInChildren<DialogCameraFocusTarget>();

        if (dialogPlayer == null)
            return false;

        isWaitingDialogToOpenUi = true;
        dialogPlayer.Play(dialog, dialogFocusTarget, () =>
        {
            isWaitingDialogToOpenUi = false;
            OpenUi();
        });

        return true;
    }

    private DialogSequenceAsset GetRandomPreOpenDialog()
    {
        if (preOpenDialogPool == null || preOpenDialogPool.Length == 0)
            return null;

        DialogSequenceAsset[] availableDialogs = System.Array.FindAll(
            preOpenDialogPool,
            candidate => candidate != null && candidate.HasLines
        );

        if (availableDialogs.Length == 0)
            return null;

        return availableDialogs[Random.Range(0, availableDialogs.Length)];
    }
}
