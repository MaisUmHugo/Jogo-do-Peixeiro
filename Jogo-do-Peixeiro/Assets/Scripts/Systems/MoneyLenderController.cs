using UnityEngine;

public class MoneyLenderController : MonoBehaviour, IInteractable
{
    private const string RoteiroDialogLibraryResourcePath = "RoteiroDialogLibrary";

    [Header("References")]
    [SerializeField] private MoneyLender moneyLender;
    [SerializeField] private PaymentUI paymentUI;
    [SerializeField] private MoneyLenderUI moneyLenderUI;

    [Header("Optional Dialog")]
    [SerializeField] private bool playDialogBeforeOpeningUi;
    [SerializeField] private DialogSequencePlayer dialogPlayer;
    [SerializeField] private DialogSequenceAsset[] preOpenDialogPool;
    [SerializeField] private DialogCameraFocusTarget dialogFocusTarget;
    [SerializeField] private bool autoLoadRoteiroExtrasWhenMissing = true;

    private bool isWaitingDialogToOpenUi;

    private void Awake()
    {
        TryAutoConfigureRoteiroExtras();
    }

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
        TryAutoConfigureRoteiroExtras();

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

    private void TryAutoConfigureRoteiroExtras()
    {
        if (!autoLoadRoteiroExtrasWhenMissing)
            return;

        if (HasAnyPreOpenDialog())
        {
            playDialogBeforeOpeningUi = true;
            return;
        }

        RoteiroDialogLibrary library = Resources.Load<RoteiroDialogLibrary>(RoteiroDialogLibraryResourcePath);

        if (library == null || library.CobradorExtras == null || library.CobradorExtras.Length == 0)
            return;

        preOpenDialogPool = library.CobradorExtras;

        if (dialogPlayer == null)
            dialogPlayer = DialogSequencePlayer.GetOrCreate();

        if (dialogFocusTarget == null)
            dialogFocusTarget = GetComponentInChildren<DialogCameraFocusTarget>(true);

        playDialogBeforeOpeningUi = HasAnyPreOpenDialog();
    }

    private bool HasAnyPreOpenDialog()
    {
        if (preOpenDialogPool == null || preOpenDialogPool.Length == 0)
            return false;

        for (int i = 0; i < preOpenDialogPool.Length; i++)
        {
            if (preOpenDialogPool[i] != null && preOpenDialogPool[i].HasLines)
                return true;
        }

        return false;
    }
}
