using UnityEngine;

public class TutorialController : MonoBehaviour
{
    public static TutorialController instance;

    public enum TutorialStep
    {
        GoToMoneyLenderCabin,
        GoToBoat,
        GoToFishingSpot,
        CatchRequiredFish,
        ReturnToDock,
        TalkToMoneyLender,
        DeliverFish,
        Finished
    }

    [Header("Current Step")]
    [SerializeField] private TutorialStep currentStep;

    [Header("Required Data")]
    [SerializeField] private int requiredFishWeight = 100;

    [Header("References")]
    [SerializeField] private TutorialUI tutorialUI;
    [SerializeField] private GameObject tutorialCompletePanel;

    [Header("Markers")]
    [SerializeField] private GameObject moneyLenderCabinMarker;
    [SerializeField] private GameObject boatMarker;
    [SerializeField] private GameObject fishingSpotMarker;
    [SerializeField] private GameObject dockMarker;
    [SerializeField] private GameObject moneyLenderMarker;

    [Header("Inventory")]
    [SerializeField] private ShipInventory shipInventory;

    public TutorialStep CurrentStep => currentStep;
    public int RequiredFishWeight => requiredFishWeight;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
    }

    private void Start()
    {
        if (tutorialCompletePanel != null)
            tutorialCompletePanel.SetActive(false);

        SetStep(TutorialStep.GoToMoneyLenderCabin);
    }

    private void Update()
    {
        if (currentStep == TutorialStep.CatchRequiredFish && shipInventory != null)
        {
            if (shipInventory.GetCurrentWeight() >= requiredFishWeight)
            {
                SetStep(TutorialStep.ReturnToDock);
            }
        }
    }

    public void SetStep(TutorialStep _newStep)
    {
        currentStep = _newStep;
        UpdateObjectiveText();
        UpdateMarkers();
    }

    public void NotifyReachedMoneyLenderCabin()
    {
        if (currentStep == TutorialStep.GoToMoneyLenderCabin)
            SetStep(TutorialStep.GoToBoat);
    }

    public void NotifyEnteredBoat()
    {
        if (currentStep == TutorialStep.GoToBoat)
            SetStep(TutorialStep.GoToFishingSpot);
    }

    public void NotifyReachedFishingSpot()
    {
        if (currentStep == TutorialStep.GoToFishingSpot)
            SetStep(TutorialStep.CatchRequiredFish);
    }

    public void NotifyReturnedToDock()
    {
        if (currentStep == TutorialStep.ReturnToDock)
            SetStep(TutorialStep.TalkToMoneyLender);
    }

    public void NotifyOpenedMoneyLenderUI()
    {
        if (currentStep == TutorialStep.TalkToMoneyLender)
            SetStep(TutorialStep.DeliverFish);
    }

    public void NotifyDeliveredFish()
    {
        if (currentStep == TutorialStep.DeliverFish)
            FinishTutorial();
    }

    private void FinishTutorial()
    {
        SetStep(TutorialStep.Finished);

        if (tutorialUI != null)
            tutorialUI.SetObjectiveText(string.Empty);

        if (tutorialCompletePanel != null)
            tutorialCompletePanel.SetActive(true);
    }

    private void UpdateObjectiveText()
    {
        if (tutorialUI == null)
            return;

        switch (currentStep)
        {
            case TutorialStep.GoToMoneyLenderCabin:
                tutorialUI.SetObjectiveText("Go to the moneylender's cabin");
                break;

            case TutorialStep.GoToBoat:
                tutorialUI.SetObjectiveText("Go to the boat");
                break;

            case TutorialStep.GoToFishingSpot:
                tutorialUI.SetObjectiveText("Go to a fishing spot");
                break;

            case TutorialStep.CatchRequiredFish:
                tutorialUI.SetObjectiveText($"Catch at least {requiredFishWeight} kg of fish");
                break;

            case TutorialStep.ReturnToDock:
                tutorialUI.SetObjectiveText("Return to the dock");
                break;

            case TutorialStep.TalkToMoneyLender:
                tutorialUI.SetObjectiveText("Talk to the moneylender");
                break;

            case TutorialStep.DeliverFish:
                tutorialUI.SetObjectiveText("Deliver the requested fish");
                break;

            case TutorialStep.Finished:
                tutorialUI.SetObjectiveText("Tutorial completed");
                break;
        }
    }

    private void UpdateMarkers()
    {
        SetMarkerActive(moneyLenderCabinMarker, false);
        SetMarkerActive(boatMarker, false);
        SetMarkerActive(fishingSpotMarker, false);
        SetMarkerActive(dockMarker, false);
        SetMarkerActive(moneyLenderMarker, false);

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

    private void SetMarkerActive(GameObject _marker, bool _active)
    {
        if (_marker != null)
            _marker.SetActive(_active);
    }
}