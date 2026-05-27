using UnityEngine;
using UnityEngine.SceneManagement;

public class FishMarketController : MonoBehaviour, IInteractable
{
    [SerializeField] private FishMarket fishMarket;
    [SerializeField] private DockOwnerUI dockOwnerUI;
    [SerializeField] private int interactionPriority = 45;
    [SerializeField] private string promptText = "Falar";
    [SerializeField] private bool sellDirectlyWhenNoUi = true;

    [Header("Interaction")]
    [SerializeField] private Transform interactionPoint;
    [SerializeField] private Transform playerRoot;
    [SerializeField, Min(0.1f)] private float interactionRange = 4f;
    [SerializeField] private bool requireOnFootState = true;

    [Header("Áudio")]
    [SerializeField, InspectorName("Direct Sell SFX")] private AudioClip directSellSfx;
    [SerializeField, Range(0f, 1f), InspectorName("Direct Sell SFX Volume")] private float directSellSfxVolume = 1f;

    [Header("Optional Dialog")]
    [SerializeField] private bool playDialogBeforeOpeningUi;
    [SerializeField] private DialogSequencePlayer dialogPlayer;
    [SerializeField] private DialogSequenceAsset firstInteractionDialog;
    [SerializeField] private DialogSequenceAsset[] repeatDialogPool;
    [SerializeField] private DialogCameraFocusTarget dialogFocusTarget;
    [SerializeField] private bool autoLoadRoteiroDialogWhenMissing = true;
    [SerializeField] private bool useVolcanoDialogInLavaScene = true;

    private bool hasPlayedFirstDialog;
    private bool isWaitingDialogToOpenUi;
    private const string RoteiroDialogLibraryResourcePath = "RoteiroDialogLibrary";

    public string PromptText => promptText;
    public Transform PromptPoint => interactionPoint != null ? interactionPoint : transform;

    private void Awake()
    {
        if (fishMarket == null)
            fishMarket = GetComponent<FishMarket>();

        if (dockOwnerUI == null)
            dockOwnerUI = FindFirstObjectByType<DockOwnerUI>(FindObjectsInactive.Include);

        TryAutoConfigureRoteiroDialog();
    }

    public bool CanInteract()
    {
        if (fishMarket == null)
            return false;

        if (isWaitingDialogToOpenUi)
            return false;

        if (requireOnFootState &&
            GameManager.instance != null &&
            GameManager.instance.currentState != GameManager.GameState.OnFoot)
        {
            return false;
        }

        return IsPlayerInRange();
    }

    public int GetInteractionPriority()
    {
        return interactionPriority;
    }

    public void ConfigureOptionalDialogs(
        DialogSequencePlayer _dialogPlayer,
        DialogSequenceAsset _firstInteractionDialog,
        DialogSequenceAsset[] _repeatDialogPool,
        DialogCameraFocusTarget _dialogFocusTarget = null,
        bool _playDialogBeforeOpeningUi = true)
    {
        dialogPlayer = _dialogPlayer;
        firstInteractionDialog = _firstInteractionDialog;
        repeatDialogPool = _repeatDialogPool;
        dialogFocusTarget = _dialogFocusTarget != null ? _dialogFocusTarget : dialogFocusTarget;
        playDialogBeforeOpeningUi = _playDialogBeforeOpeningUi && HasAnyConfiguredDialog();
    }

    public void Interact()
    {
        if (!CanInteract())
            return;

        if (TryPlayPreOpenDialog())
            return;

        OpenMarket();
    }

    private void OpenMarket()
    {
        if (dockOwnerUI != null)
        {
            dockOwnerUI.Open(fishMarket);
            return;
        }

        if (!sellDirectlyWhenNoUi)
        {
            HUDWarningUI.Instance?.ShowWarning("Painel do Dono do Porto não encontrado.");
            return;
        }

        if (fishMarket.TrySellAllFish(out int earnedMoney))
        {
            PlayDirectSellSfx();
            HUDWarningUI.Instance?.ShowWarning($"Peixes vendidos: R$ {earnedMoney}");
            return;
        }

        HUDWarningUI.Instance?.ShowWarning("Nenhum peixe no barco.");
    }

    private bool TryPlayPreOpenDialog()
    {
        TryAutoConfigureRoteiroDialog();

        if (!playDialogBeforeOpeningUi)
            return false;

        DialogSequenceAsset dialog = GetDialogToPlay(
            out bool shouldMarkFirstDialogAsPlayed,
            out bool shouldOpenMarketAfterDialog);

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
            if (shouldMarkFirstDialogAsPlayed)
                hasPlayedFirstDialog = true;

            isWaitingDialogToOpenUi = false;

            if (shouldOpenMarketAfterDialog)
                OpenMarket();
        });

        return true;
    }

    private void TryAutoConfigureRoteiroDialog()
    {
        if (!autoLoadRoteiroDialogWhenMissing)
            return;

        if (firstInteractionDialog != null && firstInteractionDialog.HasLines)
        {
            playDialogBeforeOpeningUi = true;
            return;
        }

        RoteiroDialogLibrary library = Resources.Load<RoteiroDialogLibrary>(RoteiroDialogLibraryResourcePath);

        if (library == null)
            return;

        string sceneName = SceneManager.GetActiveScene().name;
        DialogSequenceAsset dialog = useVolcanoDialogInLavaScene && sceneName.Contains("Lava")
            ? library.DonoDocaVulcao
            : library.DonoDocaPrimeiroEncontro;

        if (dialog == null || !dialog.HasLines)
            return;

        if (dialogPlayer == null)
            dialogPlayer = DialogSequencePlayer.GetOrCreate();

        if (dialogFocusTarget == null)
            dialogFocusTarget = GetComponentInChildren<DialogCameraFocusTarget>(true);

        firstInteractionDialog = dialog;
        playDialogBeforeOpeningUi = true;
    }

    private DialogSequenceAsset GetDialogToPlay(out bool _shouldMarkFirstDialogAsPlayed, out bool _shouldOpenMarketAfterDialog)
    {
        _shouldMarkFirstDialogAsPlayed = false;
        _shouldOpenMarketAfterDialog = true;

        if (ShouldUseEarlyDockOwnerEdgeDialog())
        {
            _shouldOpenMarketAfterDialog = false;
            return GetEarlyDockOwnerEdgeDialog();
        }

        if (!hasPlayedFirstDialog && firstInteractionDialog != null && firstInteractionDialog.HasLines)
        {
            _shouldMarkFirstDialogAsPlayed = true;
            return firstInteractionDialog;
        }

        if (repeatDialogPool == null || repeatDialogPool.Length == 0)
            return null;

        DialogSequenceAsset[] availableDialogs = System.Array.FindAll(
            repeatDialogPool,
            candidate => candidate != null && candidate.HasLines
        );

        if (availableDialogs.Length == 0)
            return null;

        return availableDialogs[Random.Range(0, availableDialogs.Length)];
    }

    private bool ShouldUseEarlyDockOwnerEdgeDialog()
    {
        CampaignQuestGuidanceController guidanceController = CampaignQuestGuidanceController.instance;

        return guidanceController != null &&
               guidanceController.isActiveAndEnabled &&
               guidanceController.ShouldUseDockOwnerBeforeIntroEdgeDialog();
    }

    private DialogSequenceAsset GetEarlyDockOwnerEdgeDialog()
    {
        RoteiroDialogLibrary library = Resources.Load<RoteiroDialogLibrary>(RoteiroDialogLibraryResourcePath);
        return library != null ? library.EdgeDonoDocaAntesIntro : null;
    }

    private bool HasAnyConfiguredDialog()
    {
        if (firstInteractionDialog != null && firstInteractionDialog.HasLines)
            return true;

        if (repeatDialogPool == null)
            return false;

        for (int i = 0; i < repeatDialogPool.Length; i++)
        {
            if (repeatDialogPool[i] != null && repeatDialogPool[i].HasLines)
                return true;
        }

        return false;
    }

    private bool IsPlayerInRange()
    {
        Transform playerTransform = ResolvePlayerRoot();

        if (playerTransform == null)
            return false;

        Transform referencePoint = PromptPoint;
        return Vector3.Distance(playerTransform.position, referencePoint.position) <= interactionRange;
    }

    private Transform ResolvePlayerRoot()
    {
        if (playerRoot != null)
            return playerRoot;

        PlayerMove playerMove = FindFirstObjectByType<PlayerMove>(FindObjectsInactive.Exclude);

        if (playerMove != null)
        {
            playerRoot = playerMove.transform;
            return playerRoot;
        }

        CharacterController characterController = FindFirstObjectByType<CharacterController>(FindObjectsInactive.Exclude);

        if (characterController != null)
            playerRoot = characterController.transform;

        return playerRoot;
    }

    private void PlayDirectSellSfx()
    {
        if (AudioManager.Instance == null || directSellSfx == null)
            return;

        AudioManager.Instance.PlaySfx(directSellSfx, directSellSfxVolume);
    }
}
