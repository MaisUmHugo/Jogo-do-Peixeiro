using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TutorialController : MonoBehaviour
{
    public static TutorialController instance;

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
        Failed
    }

    [Header("Current Step")]
    [SerializeField] private TutorialStep currentStep;

    [Header("Runtime")]
    [SerializeField] private bool runTutorial = true;
    [SerializeField] private bool blockBoatUntilMoneyLender = true;
    [SerializeField] private bool handleMoneyLenderDuringTutorial = true;
    [SerializeField] private bool failWhenDeadlineEnds = true;

    [Header("Mission")]
    [SerializeField] private FishingAreaDefinition tutorialFishingArea;
    [SerializeField] private FishScriptableObject[] fallbackAvailableFish;
    [SerializeField, Min(1)] private int minRequestedQuantity = 1;
    [SerializeField, Min(1)] private int maxRequestedQuantity = 3;
    [SerializeField, Min(1)] private int tutorialDurationDays = 3;

    [Header("References")]
    [SerializeField] private TutorialUI tutorialUI;
    [SerializeField] private TextCanvaManager textCanvaManager;
    [SerializeField] private TutorialPanelSequence basicPanelSequence;
    [SerializeField] private DayCycle dayCycle;
    [SerializeField] private ShipInventory shipInventory;

    [Header("Panels")]
    [SerializeField] private GameObject tutorialCompletePanel;
    [SerializeField] private GameObject tutorialFailedPanel;
    [SerializeField] private bool pauseGameOnFailure = true;

    [Header("Dialogs")]
    [SerializeField] private DialogData firstTalkDialog;
    [SerializeField] private DialogData noDeliveryDialog;
    [SerializeField] private DialogData readyToDeliverDialog;
    [SerializeField] private DialogData completedDialog;
    [SerializeField] private DialogData failedDialog;

    [Header("Single Marker")]
    [SerializeField] private TutorialMarker tutorialMarker;
    [SerializeField] private GameObject tutorialPointer;
    [SerializeField] private Transform moneyLenderCabinMarkerTarget;
    [SerializeField] private Transform boatMarkerTarget;
    [SerializeField] private Transform fishingSpotMarkerTarget;
    [SerializeField] private Transform dockMarkerTarget;
    [SerializeField] private Transform moneyLenderMarkerTarget;

    [Header("Legacy Step Markers")]
    [SerializeField] private GameObject moneyLenderCabinMarker;
    [SerializeField] private GameObject boatMarker;
    [SerializeField] private GameObject fishingSpotMarker;
    [SerializeField] private GameObject dockMarker;
    [SerializeField] private GameObject moneyLenderMarker;

    [Header("Scene")]
    [SerializeField] private string mainMenuSceneName = "Main Menu";

    private FishScriptableObject requestedFish;
    private int requestedQuantity;
    private int tutorialStartDay = 1;
    private bool hasAcceptedRequest;
    private bool isFinishingDelivery;

    public TutorialStep CurrentStep => currentStep;
    public FishScriptableObject RequestedFish => requestedFish;
    public int RequestedQuantity => requestedQuantity;
    public bool HasAcceptedRequest => hasAcceptedRequest;
    public bool IsTutorialFinished { get; private set; }
    public bool IsTutorialFailed { get; private set; }
    public bool IsTutorialEnabled => runTutorial;
    public bool IsTutorialRunning => runTutorial && !IsTutorialFinished && !IsTutorialFailed;

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
    }

    private void OnEnable()
    {
        TutorialEvents.MoneyLenderInteractionRequested += HandleMoneyLenderInteraction;
        TutorialEvents.BoatEntryBlockRequested += ShouldBlockBoatEntry;
        TutorialEvents.BoatEntryBlocked += HandleBoatEntryBlocked;
        TutorialEvents.BoatEntered += NotifyEnteredBoat;
        TutorialEvents.FishCaught += NotifyFishCaught;

        if (dayCycle != null)
            dayCycle.DayChanged += HandleDayChanged;
    }

    private void OnDisable()
    {
        TutorialEvents.MoneyLenderInteractionRequested -= HandleMoneyLenderInteraction;
        TutorialEvents.BoatEntryBlockRequested -= ShouldBlockBoatEntry;
        TutorialEvents.BoatEntryBlocked -= HandleBoatEntryBlocked;
        TutorialEvents.BoatEntered -= NotifyEnteredBoat;
        TutorialEvents.FishCaught -= NotifyFishCaught;

        if (dayCycle != null)
            dayCycle.DayChanged -= HandleDayChanged;
    }

    private void Start()
    {
        tutorialStartDay = dayCycle != null ? dayCycle.ElapsedDays : 1;
        SetPanelActive(tutorialCompletePanel, false);
        SetPanelActive(tutorialFailedPanel, false);

        if (!runTutorial)
        {
            ClearMarkers();
            return;
        }

        SetStep(TutorialStep.GoToMoneyLenderCabin);
    }

    public void SetStep(TutorialStep _newStep)
    {
        if (IsTutorialFinished && _newStep != TutorialStep.Finished)
            return;

        if (IsTutorialFailed && _newStep != TutorialStep.Failed)
            return;

        currentStep = _newStep;
        UpdateObjectiveText();
        UpdateMarkers();
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
            SetStep(TutorialStep.GoToFishingSpot);
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

        if (currentStep == TutorialStep.ReturnToDock)
            SetStep(TutorialStep.TalkToMoneyLender);
    }

    public void NotifyOpenedMoneyLenderUI()
    {
        if (!IsTutorialRunning)
            return;

        if (currentStep == TutorialStep.TalkToMoneyLender)
            SetStep(TutorialStep.DeliverFish);
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

    private bool HandleMoneyLenderInteraction(MoneyLender _moneyLender)
    {
        if (!IsTutorialRunning || !handleMoneyLenderDuringTutorial)
            return false;

        if (currentStep == TutorialStep.GoToMoneyLenderCabin)
        {
            StartDeliveryRequest(_moneyLender);
            return true;
        }

        if (!hasAcceptedRequest)
        {
            StartDeliveryRequest(_moneyLender);
            return true;
        }

        if (CanTryDeliverRequest())
        {
            TryFinishDelivery(_moneyLender);
            return true;
        }

        ShowMissingRequestMessage();
        return true;
    }

    private bool ShouldBlockBoatEntry()
    {
        if (!IsTutorialRunning || !blockBoatUntilMoneyLender)
            return false;

        return !hasAcceptedRequest ||
               currentStep == TutorialStep.GoToMoneyLenderCabin ||
               currentStep == TutorialStep.ReadBasicPanels;
    }

    private void HandleBoatEntryBlocked()
    {
        if (!IsTutorialRunning)
            return;

        string message = currentStep == TutorialStep.ReadBasicPanels
            ? "Leia as instruções antes de pegar o barco."
            : "Fale com o agiota antes de pegar o barco.";

        ShowWarning(message);
    }

    private void NotifyFishCaught(FishData _fishData, ShipInventory _shipInventory)
    {
        if (!IsTutorialRunning || !hasAcceptedRequest)
            return;

        if (_shipInventory != null)
            shipInventory = _shipInventory;

        int ownedRequestedFish = GetOwnedRequestedFishCount();

        if (ownedRequestedFish >= requestedQuantity)
        {
            if (currentStep == TutorialStep.CatchRequiredFish ||
                currentStep == TutorialStep.GoToFishingSpot ||
                currentStep == TutorialStep.GoToBoat)
            {
                SetStep(TutorialStep.ReturnToDock);
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

    private void StartDeliveryRequest(MoneyLender _moneyLender)
    {
        PickRandomRequest(_moneyLender);
        hasAcceptedRequest = true;
        tutorialStartDay = dayCycle != null ? dayCycle.ElapsedDays : tutorialStartDay;
        SetStep(TutorialStep.ReadBasicPanels);

        ShowDialog(firstTalkDialog, () =>
        {
            if (basicPanelSequence != null)
            {
                basicPanelSequence.Show(() => SetStep(TutorialStep.GoToBoat));
                return;
            }

            SetStep(TutorialStep.GoToBoat);
        });
    }

    private void PickRandomRequest(MoneyLender _moneyLender)
    {
        requestedFish = GetRandomAvailableFish();

        if (requestedFish == null && _moneyLender != null)
            requestedFish = _moneyLender.GetSpecificFish();

        int minQuantity = Mathf.Max(1, minRequestedQuantity);
        int maxQuantity = Mathf.Max(minQuantity, maxRequestedQuantity);
        requestedQuantity = UnityEngine.Random.Range(minQuantity, maxQuantity + 1);

        if (requestedFish == null)
            Debug.LogWarning("Tutorial sem peixe configurado para o pedido.");
    }

    private FishScriptableObject GetRandomAvailableFish()
    {
        if (tutorialFishingArea != null && tutorialFishingArea.HasFishAvailable)
            return tutorialFishingArea.GetRandomFish();

        if (fallbackAvailableFish == null || fallbackAvailableFish.Length == 0)
            return null;

        return fallbackAvailableFish[UnityEngine.Random.Range(0, fallbackAvailableFish.Length)];
    }

    private bool CanTryDeliverRequest()
    {
        return currentStep == TutorialStep.ReturnToDock ||
               currentStep == TutorialStep.TalkToMoneyLender ||
               currentStep == TutorialStep.DeliverFish ||
               GetOwnedRequestedFishCount() >= requestedQuantity;
    }

    private void TryFinishDelivery(MoneyLender _moneyLender)
    {
        if (isFinishingDelivery)
            return;

        if (requestedFish == null || GetOwnedRequestedFishCount() < requestedQuantity)
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
        });
    }

    private bool TryPayRequestedFish(MoneyLender _moneyLender)
    {
        if (_moneyLender != null)
            return _moneyLender.TryGetSpecificFishPayment(requestedFish, requestedQuantity, true);

        if (shipInventory == null)
            return false;

        return shipInventory.TryPaySpecificFish(requestedFish, requestedQuantity);
    }

    private void ShowMissingRequestMessage()
    {
        int ownedRequestedFish = GetOwnedRequestedFishCount();
        string fishName = GetRequestedFishName();
        string message = $"Você ainda precisa entregar {requestedQuantity}x {fishName}. ({ownedRequestedFish}/{requestedQuantity})";

        ShowWarning(message);
        ShowDialog(noDeliveryDialog, UpdateObjectiveText);
    }

    private void FinishTutorial()
    {
        IsTutorialFinished = true;
        isFinishingDelivery = false;
        SetStep(TutorialStep.Finished);

        ShowDialog(completedDialog, () =>
        {
            SetPanelActive(tutorialCompletePanel, true);
            ShowWarning("Tutorial concluído!");
        });
    }

    private void FailTutorial()
    {
        IsTutorialFailed = true;
        isFinishingDelivery = false;
        SetStep(TutorialStep.Failed);

        ShowDialog(failedDialog, () =>
        {
            SetPanelActive(tutorialFailedPanel, true);

            if (pauseGameOnFailure)
                Time.timeScale = 0f;

            if (GameManager.instance != null)
                GameManager.instance.SetState(GameManager.GameState.InUI);

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        });
    }

    private void ShowDialog(DialogData _dialogData, Action _onFinished = null)
    {
        if (textCanvaManager == null ||
            _dialogData == null ||
            _dialogData.senteces == null ||
            _dialogData.senteces.Length == 0)
        {
            _onFinished?.Invoke();
            return;
        }

        textCanvaManager.InitializeDialog(_dialogData, _onFinished);
    }

    private int GetOwnedRequestedFishCount()
    {
        if (shipInventory == null || requestedFish == null)
            return 0;

        return shipInventory.CountFish(requestedFish);
    }

    private string GetRequestedFishName()
    {
        if (requestedFish == null)
            return "peixe pedido";

        if (!string.IsNullOrWhiteSpace(requestedFish.fishName))
            return requestedFish.fishName;

        return requestedFish.name;
    }

    private void UpdateObjectiveText()
    {
        if (tutorialUI == null)
            return;

        string fishName = GetRequestedFishName();
        int ownedRequestedFish = GetOwnedRequestedFishCount();

        switch (currentStep)
        {
            case TutorialStep.GoToMoneyLenderCabin:
                tutorialUI.SetObjectiveText("Fale com o agiota.");
                break;

            case TutorialStep.ReadBasicPanels:
                tutorialUI.SetObjectiveText("Leia as instruções.");
                break;

            case TutorialStep.GoToBoat:
                tutorialUI.SetObjectiveText($"Pegue o barco. Pedido: {requestedQuantity}x {fishName}.");
                break;

            case TutorialStep.GoToFishingSpot:
                tutorialUI.SetObjectiveText($"Vá até um ponto de pesca. Pedido: {requestedQuantity}x {fishName}.");
                break;

            case TutorialStep.CatchRequiredFish:
                tutorialUI.SetObjectiveText($"Pesque {requestedQuantity}x {fishName}. ({ownedRequestedFish}/{requestedQuantity})");
                break;

            case TutorialStep.ReturnToDock:
                tutorialUI.SetObjectiveText("Volte para a doca.");
                break;

            case TutorialStep.TalkToMoneyLender:
                tutorialUI.SetObjectiveText("Fale com o agiota para entregar o pedido.");
                break;

            case TutorialStep.DeliverFish:
                tutorialUI.SetObjectiveText($"Entregue {requestedQuantity}x {fishName} ao agiota.");
                break;

            case TutorialStep.Finished:
                tutorialUI.SetObjectiveText("Tutorial concluído!");
                break;

            case TutorialStep.Failed:
                tutorialUI.SetObjectiveText("Tutorial falhou.");
                break;
        }
    }

    private void UpdateMarkers()
    {
        ClearMarkers();

        if (TryUpdateSingleMarker())
            return;

        switch (currentStep)
        {
            case TutorialStep.GoToMoneyLenderCabin:
                SetMarkerActive(moneyLenderCabinMarker, true);
                break;

            case TutorialStep.GoToBoat:
                SetMarkerActive(boatMarker, true);
                break;

            case TutorialStep.GoToFishingSpot:
            case TutorialStep.CatchRequiredFish:
                SetMarkerActive(fishingSpotMarker, true);
                break;

            case TutorialStep.ReturnToDock:
                SetMarkerActive(dockMarker, true);
                break;

            case TutorialStep.TalkToMoneyLender:
            case TutorialStep.DeliverFish:
                SetMarkerActive(moneyLenderMarker, true);
                break;
        }
    }

    private void ClearMarkers()
    {
        SetMarkerActive(moneyLenderCabinMarker, false);
        SetMarkerActive(boatMarker, false);
        SetMarkerActive(fishingSpotMarker, false);
        SetMarkerActive(dockMarker, false);
        SetMarkerActive(moneyLenderMarker, false);

        GameObject singleMarker = GetSingleMarkerObject();

        if (singleMarker != null)
            singleMarker.SetActive(false);
    }

    private bool TryUpdateSingleMarker()
    {
        GameObject markerObject = GetSingleMarkerObject();

        if (markerObject == null)
            return false;

        Transform target = GetCurrentMarkerTarget();
        markerObject.SetActive(target != null);

        if (target == null)
            return true;

        if (tutorialMarker != null)
        {
            tutorialMarker.SetTarget(target);
        }
        else
        {
            markerObject.transform.position = target.position;
        }

        return true;
    }

    private GameObject GetSingleMarkerObject()
    {
        if (tutorialMarker != null)
            return tutorialMarker.gameObject;

        return tutorialPointer;
    }

    private Transform GetCurrentMarkerTarget()
    {
        switch (currentStep)
        {
            case TutorialStep.GoToMoneyLenderCabin:
                return moneyLenderCabinMarkerTarget;

            case TutorialStep.GoToBoat:
                return boatMarkerTarget;

            case TutorialStep.GoToFishingSpot:
            case TutorialStep.CatchRequiredFish:
                return fishingSpotMarkerTarget;

            case TutorialStep.ReturnToDock:
                return dockMarkerTarget;

            case TutorialStep.TalkToMoneyLender:
            case TutorialStep.DeliverFish:
                return moneyLenderMarkerTarget;

            default:
                return null;
        }
    }

    private void SetMarkerActive(GameObject _marker, bool _active)
    {
        if (_marker != null)
            _marker.SetActive(_active);
    }

    private void SetPanelActive(GameObject _panel, bool _active)
    {
        if (_panel != null)
            _panel.SetActive(_active);
    }

    private void ShowWarning(string _message)
    {
        if (HUDWarningUI.Instance != null)
            HUDWarningUI.Instance.ShowWarning(_message);
        else
            Debug.Log(_message);
    }
}
