using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CampaignQuestGuidanceController : MonoBehaviour
{
    #region Types And Fields

    public static CampaignQuestGuidanceController instance;

    public enum TutorialStep
    {
        GoToMoneyLenderCabin,
        ReadBasicPanels,
        GoToBoat,
        GoToFishingSpot,
        CatchRequiredFish,
        ReturnToDock,
        TalkToMoneyLender,
        DeliverFish,
        Finished,
        Failed,
        GoToDockOwner,
        SellFish,
        PayDebt
    }

    [Header("Current Step")]
    [SerializeField] private TutorialStep currentStep;

    [Header("Runtime")]
    [SerializeField] private bool runTutorial = true;
    [SerializeField] private bool blockBoatUntilMoneyLender = true;
    [SerializeField] private bool handleMoneyLenderDuringTutorial = true;
    [SerializeField] private bool failWhenDeadlineEnds = true;
    [SerializeField] private bool useCampaignEconomyFlow = true;
    [SerializeField] private bool useBasicPanelSequence;
    [Tooltip("Desliga temporariamente os slides iniciais mesmo se useBasicPanelSequence estiver ligado no prefab/cena.")]
    [SerializeField] private bool skipBasicPanelSequenceForNow = true;

    [Header("Tutorial Slides")]
    [SerializeField] private bool showTutorialSlidePanels = true;
    [SerializeField] private TutorialPanelSequence introPanelSequence;
    [SerializeField] private TutorialPanelSequence questReceivedPanelSequence;
    [SerializeField] private TutorialPanelSequence boatAndFishingPanelSequence;
    [SerializeField] private TutorialPanelSequence dockShopPanelSequence;
    [SerializeField] private TutorialPanelSequence debtPaymentPanelSequence;
    [SerializeField] private bool showEachTutorialSlidePanelOnce = true;

    [Header("Mission")]
    [SerializeField] private FishingAreaDefinition tutorialFishingArea;
    [SerializeField] private FishScriptableObject[] fallbackAvailableFish;
    [SerializeField, Min(1)] private int minRequestedQuantity = 1;
    [SerializeField, Min(1)] private int maxRequestedQuantity = 3;
    [SerializeField, Min(0)] private int minRequestedTotalWeight = 10;
    [SerializeField, Min(0)] private int maxRequestedTotalWeight = 25;
    [SerializeField, Min(1)] private int tutorialDurationDays = 3;

    [Header("References")]
    [SerializeField] private TutorialUI tutorialUI;
    [SerializeField] private TextCanvaManager textCanvaManager;
    [SerializeField] private TutorialPanelSequence basicPanelSequence;
    [SerializeField] private DayCycle dayCycle;
    [SerializeField] private ShipInventory shipInventory;
    [SerializeField] private CampaignProgressSystem campaignProgress;

    [Header("Outcome Panel")]
    [SerializeField] private bool pauseGameOnCompletion;
    [SerializeField] private bool pauseGameOnFailure = true;
    [SerializeField] private GameOutcomePanelUI generalOutcomePanel;
    [SerializeField] private string tutorialCompleteTitle = "Tutorial concluído";
    [SerializeField] private string tutorialCompleteMessage = "Você concluiu o tutorial.";
    [SerializeField] private string tutorialFailureTitle = "Falha na quest";
    [SerializeField] private string tutorialFailureMessage = "O prazo acabou antes de concluir a meta.";

    [Header("Dialogs")]
    [SerializeField] private bool useDialogs;
    [SerializeField] private DialogSequenceData firstTalkDialog;
    [SerializeField] private DialogSequenceData noDeliveryDialog;
    [SerializeField] private DialogSequenceData readyToDeliverDialog;
    [SerializeField] private DialogSequenceData completedDialog;

    [Header("Objective Markers")]
    [SerializeField] private GameObject moneyLenderCabinMarker;
    [SerializeField] private GameObject dockMarker;
    [SerializeField] private GameObject dockOwnerMarker;
    [SerializeField] private GameObject moneyLenderMarker;

    [Header("Scene")]
    [SerializeField] private string mainMenuSceneName = "Main Menu";

    private FishScriptableObject requestedFish;
    private int requestedQuantity;
    private int requestedTotalWeight;
    private int tutorialStartDay = 1;
    private bool hasAcceptedRequest;
    private bool hasSoldFishToDockOwner;
    private bool isFinishingDelivery;
    private bool isShowingEndPanel;
    private bool isCampaignSubscribed;
    private Coroutine pendingOutcomeRoutine;
    private bool hasShownIntroSlides;
    private bool hasShownQuestReceivedSlides;
    private bool hasShownBoatAndFishingSlides;
    private bool hasShownDockShopSlides;
    private bool hasShownDebtPaymentSlides;
    private bool shouldShowDebtPaymentSlidesOnDockOwnerClose;

    public TutorialStep CurrentStep => currentStep;
    public FishScriptableObject RequestedFish => requestedFish;
    public int RequestedQuantity => requestedQuantity;
    public int RequestedTotalWeight => requestedTotalWeight;
    public int OwnedRequestedFishCount => GetOwnedRequestedFishCount();
    public float CurrentOwnedFishWeight => GetCurrentOwnedFishWeight();
    public string RequestedFishName => GetRequestedFishName();
    public bool HasEnoughRequestedFish => requestedFish != null && GetOwnedRequestedFishCount() >= requestedQuantity;
    public bool HasEnoughRequestedWeight => GetCurrentOwnedFishWeight() >= requestedTotalWeight;
    public bool HasCompletedDeliveryRequest => HasEnoughRequestedFish && HasEnoughRequestedWeight;
    public bool HasAcceptedRequest => hasAcceptedRequest;
    public bool IsTutorialFinished { get; private set; }
    public bool IsTutorialFailed { get; private set; }
    public bool IsTutorialEnabled => runTutorial;
    public bool IsTutorialRunning => runTutorial && !IsTutorialFinished && !IsTutorialFailed;
    public bool IsHandlingPayment => IsTutorialRunning && handleMoneyLenderDuringTutorial && hasAcceptedRequest;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;

        if (dayCycle == null)
            dayCycle = FindFirstObjectByType<DayCycle>();

        if (shipInventory == null)
            shipInventory = FindFirstObjectByType<ShipInventory>();

        if (textCanvaManager == null)
            textCanvaManager = FindFirstObjectByType<TextCanvaManager>();

        if (campaignProgress == null)
            campaignProgress = CampaignProgressSystem.GetOrCreate();
    }

    private void OnEnable()
    {
        TutorialEvents.MoneyLenderInteractionRequested += HandleMoneyLenderInteraction;
        TutorialEvents.BoatEntryBlockRequested += ShouldBlockBoatEntry;
        TutorialEvents.BoatEntryBlocked += HandleBoatEntryBlocked;
        TutorialEvents.BoatEntered += NotifyEnteredBoat;
        TutorialEvents.BoatExited += NotifyExitedBoat;
        TutorialEvents.FishCaught += NotifyFishCaught;
        ForcedSleepController.FishLostDuringForcedSleep += HandleForcedSleepFishLoss;
        FishMarket.OnAnySaleCompleted += HandleFishMarketSaleCompleted;
        DockOwnerUI.AnyClosed += HandleDockOwnerClosed;
        TrySubscribeCampaignProgress();

        if (dayCycle != null)
            dayCycle.DayChanged += HandleDayChanged;
    }

    private void OnDisable()
    {
        if (pendingOutcomeRoutine != null)
        {
            StopCoroutine(pendingOutcomeRoutine);
            pendingOutcomeRoutine = null;
        }

        TutorialEvents.MoneyLenderInteractionRequested -= HandleMoneyLenderInteraction;
        TutorialEvents.BoatEntryBlockRequested -= ShouldBlockBoatEntry;
        TutorialEvents.BoatEntryBlocked -= HandleBoatEntryBlocked;
        TutorialEvents.BoatEntered -= NotifyEnteredBoat;
        TutorialEvents.BoatExited -= NotifyExitedBoat;
        TutorialEvents.FishCaught -= NotifyFishCaught;
        ForcedSleepController.FishLostDuringForcedSleep -= HandleForcedSleepFishLoss;
        FishMarket.OnAnySaleCompleted -= HandleFishMarketSaleCompleted;
        DockOwnerUI.AnyClosed -= HandleDockOwnerClosed;
        UnsubscribeCampaignProgress();

        if (dayCycle != null)
            dayCycle.DayChanged -= HandleDayChanged;
    }

    private void Start()
    {
        TrySubscribeCampaignProgress();
        tutorialStartDay = dayCycle != null ? dayCycle.ElapsedDays : 1;

        if (!runTutorial)
        {
            ClearMarkers();
            SetObjectiveVisible(false);
            return;
        }

        SetObjectiveVisible(true);
        SetStep(TutorialStep.GoToMoneyLenderCabin);
        ShowIntroSlides();
    }

    private void Update()
    {
        if (isShowingEndPanel)
            KeepEndPanelUiReady();
    }

    #endregion

    #region Tutorial Notifications

    public void SetStep(TutorialStep _newStep)
    {
        if (_newStep == TutorialStep.ReadBasicPanels && !ShouldUseBasicPanelSequence())
            _newStep = TutorialStep.GoToBoat;

        if (IsTutorialFinished && _newStep != TutorialStep.Finished)
            return;

        if (IsTutorialFailed && _newStep != TutorialStep.Failed)
            return;

        currentStep = _newStep;
        UpdateObjectiveText();
        UpdateMarkers();
        HandleTutorialSlideEventsForStep(_newStep);
    }

    public void DebugDisableTutorial()
    {
        runTutorial = false;
        IsTutorialFinished = true;
        IsTutorialFailed = false;
        isFinishingDelivery = false;
        isShowingEndPanel = false;
        currentStep = TutorialStep.Finished;

        if (pendingOutcomeRoutine != null)
        {
            StopCoroutine(pendingOutcomeRoutine);
            pendingOutcomeRoutine = null;
        }

        if (generalOutcomePanel != null && generalOutcomePanel.IsShowing)
            generalOutcomePanel.Close();

        ClearMarkers();
        SetObjectiveVisible(false);
    }

    public void NotifyReachedMoneyLenderCabin()
    {
        if (currentStep == TutorialStep.GoToMoneyLenderCabin)
            UpdateObjectiveText();
    }

    public void NotifyEnteredBoat()
    {
        if (!IsTutorialRunning || !hasAcceptedRequest)
            return;

        if (currentStep == TutorialStep.GoToBoat)
            SetStep(IsCampaignEconomyFlowActive() && HasEnoughFishValueForQuestGoal()
                ? TutorialStep.GoToDockOwner
                : TutorialStep.GoToFishingSpot);
    }

    public void NotifyExitedBoat()
    {
        if (!IsTutorialRunning || !hasAcceptedRequest)
            return;

        if (IsCampaignEconomyFlowActive())
        {
            if (hasSoldFishToDockOwner)
            {
                SetStep(TutorialStep.TalkToMoneyLender);
                return;
            }

            SetStep(HasEnoughFishValueForQuestGoal()
                ? TutorialStep.GoToDockOwner
                : TutorialStep.GoToBoat);
            return;
        }

        if (HasCompletedDeliveryRequest)
        {
            SetStep(IsCampaignEconomyFlowActive() ? TutorialStep.ReturnToDock : TutorialStep.TalkToMoneyLender);
            return;
        }

        if (currentStep == TutorialStep.GoToFishingSpot ||
            currentStep == TutorialStep.CatchRequiredFish)
        {
            SetStep(TutorialStep.GoToBoat);
        }
    }

    public void NotifyReachedFishingSpot()
    {
        if (!IsTutorialRunning)
            return;

        if (currentStep == TutorialStep.GoToFishingSpot)
            SetStep(TutorialStep.CatchRequiredFish);
    }

    public void NotifyReturnedToDock()
    {
        if (!IsTutorialRunning)
            return;

        if (IsCampaignEconomyFlowActive())
        {
            if (hasSoldFishToDockOwner)
            {
                SetStep(TutorialStep.TalkToMoneyLender);
                return;
            }

            SetStep(HasEnoughFishValueForQuestGoal()
                ? TutorialStep.GoToDockOwner
                : TutorialStep.GoToBoat);
            return;
        }

        if (currentStep == TutorialStep.ReturnToDock)
        {
            SetStep(IsCampaignEconomyFlowActive() ? TutorialStep.GoToDockOwner : TutorialStep.TalkToMoneyLender);
        }
    }

    public void NotifyOpenedMoneyLenderUI()
    {
        if (!IsTutorialRunning)
            return;

        if (currentStep == TutorialStep.TalkToMoneyLender)
            SetStep(IsCampaignEconomyFlowActive() ? TutorialStep.PayDebt : TutorialStep.DeliverFish);
    }

    public void NotifyDeliveredFish()
    {
        if (currentStep == TutorialStep.DeliverFish)
            FinishTutorial();
    }

    public void RestartTutorialScene()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void ContinueAfterTutorial()
    {
        isShowingEndPanel = false;
        Time.timeScale = 1f;

        if (generalOutcomePanel != null)
            generalOutcomePanel.Close();

        if (GameManager.instance != null)
            GameManager.instance.SetState(GameManager.GameState.OnFoot);
    }

    public void CloseTutorialFailedPanel()
    {
        isShowingEndPanel = false;
        Time.timeScale = 1f;

        if (generalOutcomePanel != null)
            generalOutcomePanel.Close();

        if (GameManager.instance != null)
            GameManager.instance.SetState(GameManager.GameState.OnFoot);
    }

    #endregion

    #region Interactions And Progression

    private bool HandleMoneyLenderInteraction(MoneyLender _moneyLender, PaymentUI _paymentUI, MoneyLenderUI _moneyLenderUI)
    {
        if (!IsTutorialRunning || !handleMoneyLenderDuringTutorial)
            return false;

        if (IsCampaignEconomyFlowActive())
            return HandleCampaignEconomyMoneyLenderInteraction(_moneyLender, _paymentUI, _moneyLenderUI);

        if (currentStep == TutorialStep.GoToMoneyLenderCabin)
        {
            StartDeliveryRequest(_moneyLender, _paymentUI, _moneyLenderUI);
            return true;
        }

        if (!hasAcceptedRequest)
        {
            StartDeliveryRequest(_moneyLender, _paymentUI, _moneyLenderUI);
            return true;
        }

        OpenTutorialPaymentUI(_moneyLender, _paymentUI, _moneyLenderUI);
        return true;
    }

    private bool HandleCampaignEconomyMoneyLenderInteraction(MoneyLender _moneyLender, PaymentUI _paymentUI, MoneyLenderUI _moneyLenderUI)
    {
        if (currentStep == TutorialStep.GoToMoneyLenderCabin || !hasAcceptedRequest)
        {
            StartDeliveryRequest(_moneyLender, _paymentUI, _moneyLenderUI);
            return true;
        }

        if (!hasSoldFishToDockOwner)
        {
            if (HasEnoughFishValueForQuestGoal())
            {
                AdvanceToDockOwnerMarket("Venda os peixes no mercado do dono da doca antes de pagar o cobrador.");
            }
            else
            {
                ShowWarning("Pesque peixes suficientes para vender e pagar a meta da dívida.");
            }

            UpdateObjectiveText();
            return true;
        }

        SetStep(TutorialStep.PayDebt);
        OpenTutorialPaymentUI(_moneyLender, _paymentUI, _moneyLenderUI);

        if (_paymentUI != null)
            _paymentUI.ShowStatusMessage("Objetivo avançou: pague a meta da dívida.");
        else
            ShowWarning("Objetivo avançou: pague a meta da dívida no cobrador.");

        return true;
    }

    private bool ShouldBlockBoatEntry()
    {
        if (!IsTutorialRunning || !blockBoatUntilMoneyLender)
            return false;

        return !hasAcceptedRequest ||
               currentStep == TutorialStep.GoToMoneyLenderCabin ||
               (ShouldUseBasicPanelSequence() && currentStep == TutorialStep.ReadBasicPanels);
    }

    private void HandleBoatEntryBlocked()
    {
        if (!IsTutorialRunning)
            return;

        string message = ShouldUseBasicPanelSequence() && currentStep == TutorialStep.ReadBasicPanels
            ? "Leia as instruções antes de pegar o barco."
            : "Fale com o cobrador antes de pegar o barco.";

        ShowWarning(message);
    }

    private void NotifyFishCaught(FishData _fishData, ShipInventory _shipInventory)
    {
        if (!IsTutorialRunning || !hasAcceptedRequest)
            return;

        if (_shipInventory != null)
            shipInventory = _shipInventory;

        if (IsCampaignEconomyFlowActive())
        {
            if (HasEnoughFishValueForQuestGoal())
            {
                AdvanceToDockOwnerMarket("Peixes suficientes. Venda no mercado do dono da doca.");
                return;
            }

            if (currentStep == TutorialStep.GoToFishingSpot ||
                currentStep == TutorialStep.CatchRequiredFish ||
                currentStep == TutorialStep.GoToBoat)
            {
                SetStep(TutorialStep.CatchRequiredFish);
            }
            else
            {
                UpdateObjectiveText();
            }

            return;
        }

        int ownedRequestedFish = GetOwnedRequestedFishCount();

        if (ownedRequestedFish >= requestedQuantity)
        {
            if (HasCompletedDeliveryRequest &&
                (currentStep == TutorialStep.CatchRequiredFish ||
                currentStep == TutorialStep.GoToFishingSpot ||
                currentStep == TutorialStep.GoToBoat))
            {
                if (IsCampaignEconomyFlowActive())
                {
                    SetStep(TutorialStep.ReturnToDock);
                    return;
                }

                SetStep(TutorialStep.TalkToMoneyLender);
                return;
            }
        }

        if (currentStep == TutorialStep.GoToFishingSpot ||
            currentStep == TutorialStep.CatchRequiredFish)
        {
            SetStep(TutorialStep.CatchRequiredFish);
        }
        else
        {
            UpdateObjectiveText();
        }
    }

    private void HandleDayChanged(int _elapsedDay)
    {
        if (!IsTutorialRunning || !failWhenDeadlineEnds)
            return;

        int lastAllowedDay = tutorialStartDay + tutorialDurationDays - 1;

        if (_elapsedDay > lastAllowedDay)
            FailTutorial();
    }

    private void HandleFishMarketSaleCompleted(int _earnedMoney)
    {
        if (!IsTutorialRunning || !hasAcceptedRequest || !IsCampaignEconomyFlowActive())
            return;

        if (_earnedMoney <= 0)
            return;

        hasSoldFishToDockOwner = true;

        if (currentStep == TutorialStep.ReturnToDock ||
            currentStep == TutorialStep.GoToDockOwner ||
            currentStep == TutorialStep.SellFish ||
            currentStep == TutorialStep.GoToBoat ||
            currentStep == TutorialStep.GoToFishingSpot ||
            currentStep == TutorialStep.CatchRequiredFish)
        {
            SetStep(TutorialStep.TalkToMoneyLender);
            shouldShowDebtPaymentSlidesOnDockOwnerClose = true;
            ShowWarning("Peixes vendidos. Volte ao cobrador e pague a dívida.");
            return;
        }

        UpdateObjectiveText();
    }

    private void HandleDockOwnerClosed(DockOwnerUI _dockOwnerUI)
    {
        if (!shouldShowDebtPaymentSlidesOnDockOwnerClose)
            return;

        shouldShowDebtPaymentSlidesOnDockOwnerClose = false;

        if (!IsTutorialRunning ||
            !hasAcceptedRequest ||
            !IsCampaignEconomyFlowActive() ||
            !hasSoldFishToDockOwner)
        {
            return;
        }

        ShowDebtPaymentSlides();
    }

    private void HandleForcedSleepFishLoss(ShipInventory _shipInventory, int _lostFishCount, float _lostFishWeight, bool _wasPlayerOnBoat)
    {
        if (!IsTutorialRunning ||
            !hasAcceptedRequest ||
            !IsCampaignEconomyFlowActive() ||
            hasSoldFishToDockOwner ||
            _lostFishCount <= 0)
        {
            return;
        }

        if (_shipInventory != null)
            shipInventory = _shipInventory;

        if (HasEnoughFishValueForQuestGoal())
        {
            UpdateObjectiveText();
            return;
        }

        SetStep(TutorialStep.GoToBoat);
    }

    private void AdvanceToDockOwnerMarket(string _message = null)
    {
        bool alreadyPointingToMarket = currentStep == TutorialStep.GoToDockOwner ||
                                      currentStep == TutorialStep.SellFish;

        SetStep(TutorialStep.GoToDockOwner);

        if (!alreadyPointingToMarket && !string.IsNullOrWhiteSpace(_message))
            ShowWarning(_message);
    }

    private void HandleCampaignQuestDeadlineExpired()
    {
        if (!IsTutorialRunning || !IsCampaignEconomyTutorialEnabled())
            return;

        FailTutorial();
    }

    #endregion

    #region Delivery Request Flow

    private void StartDeliveryRequest(MoneyLender _moneyLender, PaymentUI _paymentUI, MoneyLenderUI _moneyLenderUI)
    {
        PickRandomRequest(_moneyLender);
        hasAcceptedRequest = true;
        hasSoldFishToDockOwner = false;
        tutorialStartDay = dayCycle != null ? dayCycle.ElapsedDays : tutorialStartDay;

        ShowDialog(firstTalkDialog, () =>
        {
            ShowQuestReceivedSlides(() =>
            {
                ShowBoatAndFishingSlides(() =>
                {
                    FinishDeliveryRequestIntro(_moneyLender, _paymentUI, _moneyLenderUI);
                });
            });
        }, GetDialogFocusTarget(_moneyLender));
    }

    private void FinishDeliveryRequestIntro(MoneyLender _moneyLender, PaymentUI _paymentUI, MoneyLenderUI _moneyLenderUI)
    {
        SetStep(TutorialStep.GoToBoat);

        if (IsCampaignEconomyFlowActive())
        {
            OpenTutorialPaymentUI(_moneyLender, _paymentUI, _moneyLenderUI);
            ShowWarning("Meta recebida. Vá até a doca e pegue o barco.");
            return;
        }

        OpenTutorialPaymentUI(_moneyLender, _paymentUI, _moneyLenderUI);
    }

    private void OpenTutorialPaymentUI(MoneyLender _moneyLender, PaymentUI _paymentUI, MoneyLenderUI _moneyLenderUI)
    {
        if (_paymentUI != null)
        {
            _paymentUI.OpenForTutorial(_moneyLender, this);
            return;
        }

        if (_moneyLenderUI != null)
        {
            _moneyLenderUI.OpenForTutorial(_moneyLender, this);
            return;
        }

        if (_paymentUI == null && _moneyLenderUI == null)
        {
            ShowMissingRequestMessage(false);
            return;
        }
    }

    private void PickRandomRequest(MoneyLender _moneyLender)
    {
        requestedFish = GetRandomAvailableFish();

        if (requestedFish == null && _moneyLender != null)
            requestedFish = _moneyLender.GetSpecificFish();

        int minQuantity = Mathf.Max(1, minRequestedQuantity);
        int maxQuantity = Mathf.Max(minQuantity, maxRequestedQuantity);
        requestedQuantity = UnityEngine.Random.Range(minQuantity, maxQuantity + 1);

        int minWeight = Mathf.Max(0, minRequestedTotalWeight);
        int maxWeight = Mathf.Max(minWeight, maxRequestedTotalWeight);
        requestedTotalWeight = UnityEngine.Random.Range(minWeight, maxWeight + 1);

        if (requestedFish == null)
            Debug.LogWarning("Tutorial sem peixe configurado para o pedido.");
    }

    private FishScriptableObject GetRandomAvailableFish()
    {
        if (tutorialFishingArea != null && tutorialFishingArea.HasFishAvailable)
            return tutorialFishingArea.GetRandomFish(true);

        if (fallbackAvailableFish == null || fallbackAvailableFish.Length == 0)
            return null;

        FishScriptableObject[] requestableFish = System.Array.FindAll(
            fallbackAvailableFish,
            fish => fish != null && fish.CanBeRequestedByMoneyLender
        );

        if (requestableFish == null || requestableFish.Length == 0)
            return null;

        return requestableFish[UnityEngine.Random.Range(0, requestableFish.Length)];
    }

    private bool CanTryDeliverRequest()
    {
        return currentStep == TutorialStep.ReturnToDock ||
               currentStep == TutorialStep.TalkToMoneyLender ||
               currentStep == TutorialStep.DeliverFish ||
               HasCompletedDeliveryRequest;
    }

    private void TryFinishDelivery(MoneyLender _moneyLender)
    {
        if (isFinishingDelivery)
            return;

        if (requestedFish == null || !HasCompletedDeliveryRequest)
        {
            ShowMissingRequestMessage();
            return;
        }

        isFinishingDelivery = true;
        SetStep(TutorialStep.DeliverFish);

        ShowDialog(readyToDeliverDialog, () =>
        {
            bool paid = TryPayRequestedFish(_moneyLender);
            isFinishingDelivery = false;

            if (!paid)
            {
                ShowMissingRequestMessage();
                return;
            }

            FinishTutorial();
        }, GetDialogFocusTarget(_moneyLender));
    }

    private bool TryPayRequestedFish(MoneyLender _moneyLender)
    {
        if (shipInventory == null)
            shipInventory = FindFirstObjectByType<ShipInventory>(FindObjectsInactive.Include);

        if (shipInventory == null)
            return false;

        bool paid = shipInventory.TryPayTutorialRequest(requestedFish, requestedQuantity, requestedTotalWeight);

        if (paid && _moneyLender != null)
            _moneyLender.PlayTutorialFinishFireworks();

        return paid;
    }

    #endregion

    #region Money Lender Payment API

    public bool ShouldHandleMoneyLenderPayment(MoneyLender _moneyLender)
    {
        if (IsCampaignEconomyFlowActive())
            return false;

        return IsHandlingPayment;
    }

    public void NotifyMoneyLenderDebtPayment(bool _success, int _paidAmount, MoneyLender.DebtPaymentResult _paymentResult)
    {
        if (!IsTutorialRunning || !hasAcceptedRequest || !IsCampaignEconomyTutorialEnabled())
            return;

        if (!_success)
        {
            hasSoldFishToDockOwner = false;
            SetStep(TutorialStep.GoToBoat);
            ShowWarning("Venda mais peixes para conseguir pagar.");
            return;
        }

        if (_paymentResult == MoneyLender.DebtPaymentResult.Completed ||
            _paymentResult == MoneyLender.DebtPaymentResult.PaidOff ||
            HasCampaignTutorialQuestAdvanced())
        {
            FinishTutorial();
            return;
        }

        hasSoldFishToDockOwner = false;
        SetStep(TutorialStep.GoToBoat);

        if (campaignProgress != null)
            ShowWarning($"Ainda faltam R$ {campaignProgress.QuestDebtPaymentRemaining} para fechar a quest. Pesque e venda mais peixes.");
    }

    public void NotifyMoneyLenderSpecialDeliveryCompleted()
    {
        if (!IsTutorialRunning || !hasAcceptedRequest || !IsCampaignEconomyTutorialEnabled())
            return;

        FinishTutorial();
    }

    public bool TryDeliverRequestedFish(MoneyLender _moneyLender)
    {
        return TryDeliverRequestedFishInternal(_moneyLender, null, null);
    }

    public bool TryDeliverRequestedFishFromPaymentUI(MoneyLender _moneyLender, PaymentUI _paymentUI)
    {
        return TryDeliverRequestedFishInternal(_moneyLender, _paymentUI, null);
    }

    public bool TryDeliverRequestedFishFromUI(MoneyLender _moneyLender, MoneyLenderUI _moneyLenderUI)
    {
        return TryDeliverRequestedFishInternal(_moneyLender, null, _moneyLenderUI);
    }

    private bool TryDeliverRequestedFishInternal(MoneyLender _moneyLender, PaymentUI _paymentUI, MoneyLenderUI _moneyLenderUI)
    {
        if (!IsTutorialRunning || !hasAcceptedRequest)
            return false;

        if (requestedFish == null || !HasCompletedDeliveryRequest)
        {
            ShowMissingRequestMessage(false);
            return false;
        }

        SetStep(TutorialStep.DeliverFish);

        bool paid = TryPayRequestedFish(_moneyLender);

        if (!paid)
        {
            ShowMissingRequestMessage(false);
            return false;
        }

        if (_paymentUI != null)
            _paymentUI.CloseForTutorialFinish();

        if (_moneyLenderUI != null)
            _moneyLenderUI.CloseForTutorialFinish();

        FinishTutorial();
        return true;
    }

    #endregion

    #region Tutorial Completion And Panels

    private void HandleTutorialSlideEventsForStep(TutorialStep _step)
    {
        if (!IsTutorialRunning || !hasAcceptedRequest)
            return;

        if (_step == TutorialStep.GoToDockOwner || _step == TutorialStep.SellFish)
            ShowDockShopSlides();
    }

    private void ShowIntroSlides(Action _onFinished = null)
    {
        TutorialPanelSequence sequence = introPanelSequence != null
            ? introPanelSequence
            : ShouldUseBasicPanelSequence()
                ? basicPanelSequence
                : null;

        ShowTutorialPanelOnce(sequence, ref hasShownIntroSlides, _onFinished);
    }

    private void ShowQuestReceivedSlides(Action _onFinished = null)
    {
        ShowTutorialPanelOnce(questReceivedPanelSequence, ref hasShownQuestReceivedSlides, _onFinished);
    }

    private void ShowBoatAndFishingSlides(Action _onFinished = null)
    {
        ShowTutorialPanelOnce(boatAndFishingPanelSequence, ref hasShownBoatAndFishingSlides, _onFinished);
    }

    private void ShowDockShopSlides(Action _onFinished = null)
    {
        ShowTutorialPanelOnce(dockShopPanelSequence, ref hasShownDockShopSlides, _onFinished);
    }

    private void ShowDebtPaymentSlides(Action _onFinished = null)
    {
        ShowTutorialPanelOnce(debtPaymentPanelSequence, ref hasShownDebtPaymentSlides, _onFinished);
    }

    private void ShowTutorialPanelOnce(TutorialPanelSequence _sequence, ref bool _hasShown, Action _onFinished)
    {
        if (!CanShowTutorialPanel(_sequence, _hasShown))
        {
            _onFinished?.Invoke();
            return;
        }

        _hasShown = true;
        _sequence.Show(_onFinished);
    }

    private bool CanShowTutorialPanel(TutorialPanelSequence _sequence, bool _hasShown)
    {
        if (!showTutorialSlidePanels || _sequence == null)
            return false;

        return !showEachTutorialSlidePanelOnce || !_hasShown;
    }

    private void ShowMissingRequestMessage(bool _showDialog = true)
    {
        int ownedRequestedFish = GetOwnedRequestedFishCount();
        string fishName = GetRequestedFishName();
        float ownedWeight = GetCurrentOwnedFishWeight();
        string message = $"Você ainda precisa entregar {requestedQuantity}x {fishName}. ({ownedRequestedFish}/{requestedQuantity})";

        message = $"Pedido: {requestedQuantity}x {fishName} ({ownedRequestedFish}/{requestedQuantity}) e {requestedTotalWeight}kg no total ({ownedWeight:0}/{requestedTotalWeight}).";
        ShowWarning(message);
        if (_showDialog)
            ShowDialog(noDeliveryDialog, UpdateObjectiveText);
        else
            UpdateObjectiveText();
    }

    private void FinishTutorial()
    {
        IsTutorialFinished = true;
        isFinishingDelivery = false;
        bool shouldHideGuidanceAndContinue = IsCampaignEconomyTutorialEnabled();

        SetStep(TutorialStep.Finished);
        ClearMarkers();
        SetObjectiveVisible(false);

        if (shouldHideGuidanceAndContinue)
        {
            ShowWarning("Tutorial concluído!");
            return;
        }

        ShowDialog(completedDialog, () =>
        {
            ShowCompletionOutcome();
            ShowWarning("Tutorial concluído!");
        });
    }

    private void FailTutorial()
    {
        IsTutorialFailed = true;
        isFinishingDelivery = false;
        SetStep(TutorialStep.Failed);

        ShowFailureOutcome();
    }

    private void ShowCompletionOutcome()
    {
        ShowOutcome(false, tutorialCompleteTitle, tutorialCompleteMessage, pauseGameOnCompletion);
    }

    private void ShowFailureOutcome()
    {
        ShowOutcome(true, tutorialFailureTitle, tutorialFailureMessage, pauseGameOnFailure);
    }

    private void ShowOutcome(bool _isFailure, string _title, string _message, bool _pauseGame)
    {
        if (ForcedSleepController.IsAnySleepTransitionRunning())
        {
            if (pendingOutcomeRoutine != null)
                StopCoroutine(pendingOutcomeRoutine);

            pendingOutcomeRoutine = StartCoroutine(ShowOutcomeAfterSleep(_isFailure, _title, _message, _pauseGame));
            return;
        }

        ShowOutcomeImmediate(_isFailure, _title, _message, _pauseGame);
    }

    private IEnumerator ShowOutcomeAfterSleep(bool _isFailure, string _title, string _message, bool _pauseGame)
    {
        while (ForcedSleepController.IsAnySleepTransitionRunning())
            yield return null;

        pendingOutcomeRoutine = null;
        ShowOutcomeImmediate(_isFailure, _title, _message, _pauseGame);
    }

    private void ShowOutcomeImmediate(bool _isFailure, string _title, string _message, bool _pauseGame)
    {
        if (textCanvaManager != null)
            textCanvaManager.CloseDialog();

        if (generalOutcomePanel != null)
        {
            if (_isFailure)
                generalOutcomePanel.ShowFailure(_title, _message, _pauseGame);
            else
                generalOutcomePanel.ShowCompletion(_title, _message, _pauseGame);

            isShowingEndPanel = true;
            return;
        }

        ShowWarning(_message);
    }

    private void KeepEndPanelUiReady()
    {
        if (generalOutcomePanel == null || !generalOutcomePanel.IsShowing)
        {
            isShowingEndPanel = false;
            return;
        }

        UnlockCursorForUi();
    }

    private void UnlockCursorForUi()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    #endregion

    #region Dialogs And Text Formatting

    private void ShowDialog(DialogSequenceData _dialogData, Action _onFinished = null, DialogCameraFocusTarget _cameraFocusTarget = null)
    {
        if (!useDialogs ||
            textCanvaManager == null ||
            _dialogData == null ||
            !_dialogData.HasLines)
        {
            _onFinished?.Invoke();
            return;
        }

        textCanvaManager.InitializeDialog(GetFormattedDialog(_dialogData), _onFinished, _cameraFocusTarget);
    }

    private DialogCameraFocusTarget GetDialogFocusTarget(MoneyLender _moneyLender)
    {
        if (_moneyLender == null)
            return null;

        return _moneyLender.GetComponentInChildren<DialogCameraFocusTarget>();
    }

    private DialogSequenceData GetFormattedDialog(DialogSequenceData _dialogData)
    {
        return _dialogData.GetFormatted(FormatTutorialText);
    }

    private string FormatTutorialText(string _text)
    {
        if (string.IsNullOrEmpty(_text))
            return _text;

        return _text
            .Replace("{fish}", GetRequestedFishName())
            .Replace("{quantity}", requestedQuantity.ToString())
            .Replace("{weight}", requestedTotalWeight.ToString())
            .Replace("{owned}", GetOwnedRequestedFishCount().ToString())
            .Replace("{ownedWeight}", GetCurrentOwnedFishWeight().ToString("0"));
    }

    #endregion

    #region Inventory And Objective Text

    private int GetOwnedRequestedFishCount()
    {
        if (shipInventory == null || requestedFish == null)
            return 0;

        return shipInventory.CountFish(requestedFish);
    }

    private float GetCurrentOwnedFishWeight()
    {
        if (shipInventory == null)
            return 0f;

        return shipInventory.GetCurrentWeight();
    }

    private int GetCurrentFishInventoryValue()
    {
        if (shipInventory == null)
            shipInventory = FindFirstObjectByType<ShipInventory>(FindObjectsInactive.Include);

        return shipInventory != null ? shipInventory.GetTotalFishValue() : 0;
    }

    private int GetQuestDebtPaymentRemaining()
    {
        if (campaignProgress == null)
            campaignProgress = CampaignProgressSystem.GetOrCreate();

        if (campaignProgress == null)
            return 0;

        return Mathf.Max(0, campaignProgress.QuestDebtPaymentRemaining);
    }

    private bool HasEnoughFishValueForQuestGoal()
    {
        int remainingPayment = GetQuestDebtPaymentRemaining();

        if (remainingPayment <= 0)
            return true;

        return GetCurrentFishInventoryValue() >= remainingPayment;
    }

    private string GetRequestedFishName()
    {
        if (requestedFish == null)
            return "peixe pedido";

        if (!string.IsNullOrWhiteSpace(requestedFish.fishName))
            return requestedFish.fishName;

        return requestedFish.name;
    }

    private void UpdateCampaignEconomyObjectiveText()
    {
        int fishValue = GetCurrentFishInventoryValue();
        int remainingDebtPayment = GetQuestDebtPaymentRemaining();
        int debtTarget = campaignProgress != null ? campaignProgress.QuestDebtPaymentTarget : remainingDebtPayment;
        int paidAmount = campaignProgress != null ? campaignProgress.QuestDebtPaidAmount : 0;

        switch (currentStep)
        {
            case TutorialStep.GoToMoneyLenderCabin:
                tutorialUI.SetObjectiveText("Fale com o cobrador.");
                break;

            case TutorialStep.ReadBasicPanels:
            case TutorialStep.GoToBoat:
                tutorialUI.SetObjectiveText("Vá até a doca e pegue o barco.");
                break;

            case TutorialStep.GoToFishingSpot:
            case TutorialStep.CatchRequiredFish:
                tutorialUI.SetObjectiveText($"Pesque até juntar o valor suficiente. Valor dos peixes atuais: R$ {fishValue}/{remainingDebtPayment}. Meta: R$ {paidAmount}/{debtTarget}.");
                break;

            case TutorialStep.ReturnToDock:
                tutorialUI.SetObjectiveText("Volte para a doca.");
                break;

            case TutorialStep.GoToDockOwner:
            case TutorialStep.SellFish:
                tutorialUI.SetObjectiveText("Venda os peixes na loja do dono da doca.");
                break;

            case TutorialStep.TalkToMoneyLender:
                tutorialUI.SetObjectiveText("Volte ao cobrador.");
                break;

            case TutorialStep.PayDebt:
                tutorialUI.SetObjectiveText($"Pague a meta da dívida ao cobrador. Dívida da quest: R$ {remainingDebtPayment}.");
                break;

            case TutorialStep.Failed:
                tutorialUI.SetObjectiveText("Tutorial falhou.");
                break;
        }
    }

    private void UpdateObjectiveText()
    {
        if (tutorialUI == null)
            return;

        if (currentStep == TutorialStep.Finished)
        {
            tutorialUI.ClearObjectiveText();
            SetObjectiveVisible(false);
            return;
        }

        SetObjectiveVisible(true);

        if (IsCampaignEconomyFlowActive())
        {
            UpdateCampaignEconomyObjectiveText();
            return;
        }

        string fishName = GetRequestedFishName();
        int ownedRequestedFish = GetOwnedRequestedFishCount();
        float ownedWeight = GetCurrentOwnedFishWeight();

        switch (currentStep)
        {
            case TutorialStep.GoToMoneyLenderCabin:
                tutorialUI.SetObjectiveText("Fale com o cobrador.");
                break;

            case TutorialStep.ReadBasicPanels:
                tutorialUI.SetObjectiveText("Leia as instruções.");
                break;

            case TutorialStep.GoToBoat:
                tutorialUI.SetObjectiveText($"Pegue o barco. Pedido: {requestedQuantity}x {fishName} e {requestedTotalWeight}kg no total.");
                break;

            case TutorialStep.GoToFishingSpot:
                tutorialUI.SetObjectiveText($"Vá até um ponto de pesca. Pedido: {requestedQuantity}x {fishName}.");
                break;

            case TutorialStep.CatchRequiredFish:
                tutorialUI.SetObjectiveText($"Pesque {requestedQuantity}x {fishName} ({ownedRequestedFish}/{requestedQuantity}) e junte {requestedTotalWeight}kg ({ownedWeight:0}/{requestedTotalWeight}).");
                break;

            case TutorialStep.ReturnToDock:
                tutorialUI.SetObjectiveText(IsCampaignEconomyFlowActive()
                    ? "Volte para a doca e venda os peixes ao dono da doca."
                    : "Volte para a doca.");
                break;

            case TutorialStep.TalkToMoneyLender:
                tutorialUI.SetObjectiveText(IsCampaignEconomyFlowActive()
                    ? "Volte ao cobrador para pagar a dívida da quest."
                    : "Fale com o cobrador para entregar o pedido.");
                break;

            case TutorialStep.DeliverFish:
                tutorialUI.SetObjectiveText($"Entregue {requestedQuantity}x {fishName} e {requestedTotalWeight}kg ao cobrador.");
                break;

            case TutorialStep.GoToDockOwner:
                tutorialUI.SetObjectiveText("Fale com o dono da doca para vender os peixes.");
                break;

            case TutorialStep.SellFish:
                tutorialUI.SetObjectiveText("Venda os peixes no dono da doca.");
                break;

            case TutorialStep.PayDebt:
                int remainingDebtPayment = campaignProgress != null ? campaignProgress.QuestDebtPaymentRemaining : 0;
                tutorialUI.SetObjectiveText($"Pague a dívida da quest ao cobrador. Dívida da quest: R$ {remainingDebtPayment}.");
                break;

            case TutorialStep.Finished:
                tutorialUI.SetObjectiveText("Tutorial concluído!");
                break;

            case TutorialStep.Failed:
                tutorialUI.SetObjectiveText("Tutorial falhou.");
                break;
        }
    }

    #endregion

    #region Markers And Campaign State

    private void UpdateMarkers()
    {
        ClearMarkers();

        switch (currentStep)
        {
            case TutorialStep.GoToMoneyLenderCabin:
                SetFirstAvailableMarkerActive(moneyLenderCabinMarker, moneyLenderMarker);
                break;

            case TutorialStep.GoToBoat:
                SetMarkerActive(dockMarker, true);
                break;

            case TutorialStep.GoToFishingSpot:
            case TutorialStep.CatchRequiredFish:
                break;

            case TutorialStep.ReturnToDock:
                SetMarkerActive(dockMarker, true);
                break;

            case TutorialStep.GoToDockOwner:
            case TutorialStep.SellFish:
                SetFirstAvailableMarkerActive(dockOwnerMarker, dockMarker);
                break;

            case TutorialStep.TalkToMoneyLender:
            case TutorialStep.DeliverFish:
            case TutorialStep.PayDebt:
                SetFirstAvailableMarkerActive(moneyLenderMarker, moneyLenderCabinMarker);
                break;
        }
    }

    private void ClearMarkers()
    {
        SetMarkerActive(moneyLenderCabinMarker, false);
        SetMarkerActive(dockMarker, false);
        SetMarkerActive(dockOwnerMarker, false);
        SetMarkerActive(moneyLenderMarker, false);
    }

    private void SetFirstAvailableMarkerActive(params GameObject[] _markers)
    {
        if (_markers == null)
            return;

        foreach (GameObject marker in _markers)
        {
            if (marker == null)
                continue;

            SetMarkerActive(marker, true);
            return;
        }
    }

    private void SetMarkerActive(GameObject _marker, bool _active)
    {
        if (_marker != null)
            _marker.SetActive(_active);
    }

    private void SetObjectiveVisible(bool _visible)
    {
        if (tutorialUI != null)
            tutorialUI.SetObjectiveVisible(_visible);
    }

    private bool IsCampaignEconomyFlowActive()
    {
        if (!IsCampaignEconomyTutorialEnabled())
            return false;

        return campaignProgress != null &&
               campaignProgress.IsCurrentQuestTutorial &&
               !campaignProgress.HasFailedCurrentQuest &&
               !campaignProgress.IsCampaignCompleted;
    }

    private bool ShouldUseBasicPanelSequence()
    {
        return useBasicPanelSequence &&
               !skipBasicPanelSequenceForNow &&
               basicPanelSequence != null;
    }

    private bool IsCampaignEconomyTutorialEnabled()
    {
        if (!useCampaignEconomyFlow)
            return false;

        if (campaignProgress == null)
            campaignProgress = CampaignProgressSystem.GetOrCreate();

        return campaignProgress != null &&
               campaignProgress.GameMode == GameProgressMode.Campaign;
    }

    private bool HasCampaignTutorialQuestAdvanced()
    {
        if (campaignProgress == null)
            campaignProgress = CampaignProgressSystem.GetOrCreate();

        return campaignProgress != null &&
               campaignProgress.GameMode == GameProgressMode.Campaign &&
               !campaignProgress.IsCurrentQuestTutorial;
    }

    private void TrySubscribeCampaignProgress()
    {
        if (isCampaignSubscribed)
            return;

        if (campaignProgress == null)
            campaignProgress = CampaignProgressSystem.GetOrCreate();

        if (campaignProgress == null)
            return;

        campaignProgress.OnQuestDeadlineExpired += HandleCampaignQuestDeadlineExpired;
        isCampaignSubscribed = true;
    }

    private void UnsubscribeCampaignProgress()
    {
        if (!isCampaignSubscribed || campaignProgress == null)
            return;

        campaignProgress.OnQuestDeadlineExpired -= HandleCampaignQuestDeadlineExpired;
        isCampaignSubscribed = false;
    }

    private void ShowWarning(string _message)
    {
        if (HUDWarningUI.Instance != null)
            HUDWarningUI.Instance.ShowWarning(_message);
        else
            Debug.Log(_message);
    }

    #endregion
}
