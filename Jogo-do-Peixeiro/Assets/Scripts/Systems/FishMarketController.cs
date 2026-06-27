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
    [SerializeField, Range(0f, 2f), InspectorName("Direct Sell SFX Volume")] private float directSellSfxVolume = 1.5f;

    [Header("Optional Dialog")]
    [SerializeField] private bool playDialogBeforeOpeningUi;
    [SerializeField] private DialogSequencePlayer dialogPlayer;
    [SerializeField] private DialogSequenceAsset firstInteractionDialog;
    [SerializeField] private DialogSequenceAsset[] repeatDialogPool;
    [SerializeField] private DialogCameraFocusTarget dialogFocusTarget;
    [SerializeField] private bool autoLoadRoteiroDialogWhenMissing = true;
    [SerializeField] private bool useVolcanoDialogInLavaScene = true;
    [SerializeField] private bool disableStoryDialogsInEndlessMode = true;
    [SerializeField, Min(0f)] private float dialogCompletionWatchdogDelay = 0.25f;

    private bool hasPlayedFirstDialog;
    private bool isWaitingDialogToOpenUi;
    private bool pendingMarkFirstDialogAsPlayed;
    private bool pendingMarkVolcanoDialogAsPlayed;
    private bool pendingOpenMarketAfterDialog;
    private float dialogWaitStartedAt;
    private static bool hasPlayedVolcanoDialogThisRun;
    private const string RoteiroDialogLibraryResourcePath = "RoteiroDialogLibrary";

    public string PromptText => promptText;
    public Transform PromptPoint => interactionPoint != null ? interactionPoint : transform;
    public bool HasPlayedFirstDialog => hasPlayedFirstDialog;

    private void Awake()
    {
        if (fishMarket == null)
            fishMarket = GetComponent<FishMarket>();

        if (dockOwnerUI == null)
            dockOwnerUI = FindFirstObjectByType<DockOwnerUI>(FindObjectsInactive.Include);

        TryAutoConfigureRoteiroDialog();
        ApplyRunDialogStateForCurrentScene();
    }

    private void Update()
    {
        RunDialogCompletionWatchdog();
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

    public void SetFirstDialogPlayed(bool _played)
    {
        hasPlayedFirstDialog = _played;

        if (_played && IsVolcanoDialog(firstInteractionDialog))
            hasPlayedVolcanoDialogThisRun = true;
    }

    public void Interact()
    {
        if (!CanInteract())
            return;

        if (ShouldUseEarlyDockOwnerEdgeDialog())
        {
            TryPlayPreOpenDialog();
            return;
        }

        if (TryPlayPreOpenDialog())
            return;

        OpenMarket();
    }

    private void OpenMarket()
    {
        TutorialEvents.NotifyDockOwnerMarketOpened();

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
        if (ShouldSuppressStoryDialogsForCurrentMode())
            return false;

        TryAutoConfigureRoteiroDialog();
        ApplyRunDialogStateForCurrentScene();

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
        pendingMarkFirstDialogAsPlayed = shouldMarkFirstDialogAsPlayed;
        pendingMarkVolcanoDialogAsPlayed = shouldMarkFirstDialogAsPlayed && IsVolcanoDialog(dialog);
        pendingOpenMarketAfterDialog = shouldOpenMarketAfterDialog;
        dialogWaitStartedAt = Time.unscaledTime;

        dialogPlayer.Play(dialog, dialogFocusTarget, () =>
        {
            FinishPendingPreOpenDialog();
        });

        return true;
    }

    private void RunDialogCompletionWatchdog()
    {
        if (!isWaitingDialogToOpenUi)
            return;

        if (Time.unscaledTime - dialogWaitStartedAt < dialogCompletionWatchdogDelay)
            return;

        if (dialogPlayer != null && dialogPlayer.IsPlaying)
            return;

        FinishPendingPreOpenDialog();
    }

    private void FinishPendingPreOpenDialog()
    {
        if (!isWaitingDialogToOpenUi)
            return;

        bool shouldMarkFirstDialogAsPlayed = pendingMarkFirstDialogAsPlayed;
        bool shouldMarkVolcanoDialogAsPlayed = pendingMarkVolcanoDialogAsPlayed;
        bool shouldOpenMarketAfterDialog = pendingOpenMarketAfterDialog;

        isWaitingDialogToOpenUi = false;
        pendingMarkFirstDialogAsPlayed = false;
        pendingMarkVolcanoDialogAsPlayed = false;
        pendingOpenMarketAfterDialog = false;

        if (shouldMarkFirstDialogAsPlayed)
        {
            hasPlayedFirstDialog = true;

            if (shouldMarkVolcanoDialogAsPlayed)
                hasPlayedVolcanoDialogThisRun = true;
        }

        if (shouldOpenMarketAfterDialog)
            OpenMarket();
    }

    private void TryAutoConfigureRoteiroDialog()
    {
        if (!autoLoadRoteiroDialogWhenMissing)
            return;

        if (ShouldSuppressStoryDialogsForCurrentMode())
            return;

        if (firstInteractionDialog != null && firstInteractionDialog.HasLines)
        {
            playDialogBeforeOpeningUi = true;
            ApplyRunDialogStateForCurrentScene();
            return;
        }

        RoteiroDialogLibrary library = Resources.Load<RoteiroDialogLibrary>(RoteiroDialogLibraryResourcePath);

        if (library == null)
            return;

        DialogSequenceAsset dialog = ShouldUseVolcanoDialogInCurrentScene()
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
        ApplyRunDialogStateForCurrentScene();
    }

    private DialogSequenceAsset GetDialogToPlay(out bool _shouldMarkFirstDialogAsPlayed, out bool _shouldOpenMarketAfterDialog)
    {
        _shouldMarkFirstDialogAsPlayed = false;
        _shouldOpenMarketAfterDialog = true;
        ApplyRunDialogStateForCurrentScene();

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

    public static void ResetRunDialogState()
    {
        hasPlayedVolcanoDialogThisRun = false;
    }

    private void ApplyRunDialogStateForCurrentScene()
    {
        if (hasPlayedVolcanoDialogThisRun && IsVolcanoDialog(firstInteractionDialog))
            hasPlayedFirstDialog = true;
    }

    private bool ShouldUseVolcanoDialogInCurrentScene()
    {
        return useVolcanoDialogInLavaScene &&
               SceneManager.GetActiveScene().name.Contains("Lava");
    }

    private bool IsVolcanoDialog(DialogSequenceAsset _dialog)
    {
        if (_dialog == null || !ShouldUseVolcanoDialogInCurrentScene())
            return false;

        RoteiroDialogLibrary library = Resources.Load<RoteiroDialogLibrary>(RoteiroDialogLibraryResourcePath);
        return library != null && _dialog == library.DonoDocaVulcao;
    }

    private bool ShouldUseEarlyDockOwnerEdgeDialog()
    {
        if (ShouldSuppressStoryDialogsForCurrentMode())
            return false;

        CampaignQuestGuidanceController guidanceController = CampaignQuestGuidanceController.instance;

        return guidanceController != null &&
               guidanceController.isActiveAndEnabled &&
               guidanceController.ShouldUseDockOwnerBeforeIntroEdgeDialog();
    }

    private bool ShouldSuppressStoryDialogsForCurrentMode()
    {
        if (!disableStoryDialogsInEndlessMode)
            return false;

        CampaignProgressSystem campaignProgress = CampaignProgressSystem.Instance;

        if (campaignProgress == null)
            campaignProgress = FindFirstObjectByType<CampaignProgressSystem>(FindObjectsInactive.Include);

        return campaignProgress != null && campaignProgress.GameMode == GameProgressMode.Endless;
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
