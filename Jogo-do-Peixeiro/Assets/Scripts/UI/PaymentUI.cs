using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PaymentUI : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject panel;
    [SerializeField] private bool closeOnAwake = true;

    [Header("Buttons")]
    [SerializeField] private Button payButton;
    [SerializeField] private Button closeButton;

    [Header("Texts References")]
    [SerializeField] private TMP_Text paymentText;
    [SerializeField] private TMP_Text fishesText;
    [SerializeField] private TMP_Text statusText;

    [Header("Ship References")]
    [SerializeField] private ShipInventory shipInventory;

    [Header("Lender References")]
    [SerializeField] private MoneyLender moneyLender;

    [Header("Tutorial")]
    [SerializeField] private TutorialController tutorialController;
    [SerializeField] private TextCanvaManager textCanvaManager;
    [SerializeField] private bool useTutorialPaymentWhenAvailable = true;

    [Header("Audio")]
    [SerializeField] private AudioClip doorOpenSfx;
    [SerializeField] private AudioClip doorCloseSfx;
    [SerializeField, Range(0f, 1f)] private float doorOpenSfxVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float doorCloseSfxVolume = 1f;

    private readonly List<FishData> ownedFish = new List<FishData>();
    private float fishWeight;
    private int currentFishWeightPayment;
    private bool isOpen;
    private bool isSubscribed;
    private bool areButtonsBound;
    private bool isInputSubscribed;

    private GameObject PanelObject => panel != null ? panel : gameObject;

    private void Awake()
    {
        TryResolveReferences();

        if (closeOnAwake)
            CloseImmediate();
    }

    private void OnEnable()
    {
        TryResolveReferences();
        BindButtons();
        TrySubscribeInput();
        SubscribeToReferences();
        Refresh();
    }

    private void OnDisable()
    {
        UnbindButtons();
        UnsubscribeInput();
        UnsubscribeFromReferences();
    }

    public void Open(MoneyLender _moneyLender)
    {
        OpenInternal(_moneyLender, null);
    }

    public void OpenForTutorial(MoneyLender _moneyLender, TutorialController _tutorialController)
    {
        if (_tutorialController == null)
            return;

        OpenInternal(_moneyLender, _tutorialController);
    }

    private void OpenInternal(MoneyLender _moneyLender, TutorialController _tutorialController)
    {
        bool wasOpen = isOpen;

        UnsubscribeFromReferences();

        if (_moneyLender != null)
            moneyLender = _moneyLender;

        if (_tutorialController != null)
            tutorialController = _tutorialController;

        TryResolveReferences();
        TrySubscribeInput();
        SubscribeToReferences();

        isOpen = true;
        PanelObject.SetActive(true);

        // Fecha o diálogo invocando o callback pendente para não quebrar fluxos
        // que dependem da conclusão do diálogo (ex: tutorial ReadBasicPanels ? GoToBoat)
        if (textCanvaManager != null)
            textCanvaManager.CloseDialog(true);

        SetStatus(string.Empty);
        SetGameUiState(GameManager.GameState.InUI, false, true);

        if (!wasOpen)
            PlayDoorSfx(doorOpenSfx, doorOpenSfxVolume);

        Refresh();
    }

    public void TryPayButton()
    {
        if (moneyLender == null)
        {
            SetStatus("Cobrador nao encontrado.");
            return;
        }

        if (ShouldUseTutorialPayment())
        {
            bool tutorialSuccess = tutorialController.TryDeliverRequestedFishFromPaymentUI(moneyLender, this);

            if (!tutorialSuccess)
            {
                SetStatus("Pedido incompleto.");
                Refresh();
            }

            return;
        }

        bool success = moneyLender.TryGetFishWeightPayment();
        SetStatus(success ? "Pagamento entregue." : "Peso de peixe insuficiente.");

        if (success)
        {
            Close();
            return;
        }

        Refresh();
    }

    public void OnClickPay()
    {
        TryPayButton();
    }

    public void OnClickClose()
    {
        Close();
    }

    public void Refresh()
    {
        RefreshInventorySnapshot();
        SetPaymentTexts();
        SetFishListText();
    }

    public void CloseForTutorialFinish()
    {
        if (isOpen)
            PlayDoorSfx(doorCloseSfx, doorCloseSfxVolume);

        CloseImmediate();
        SetGameUiState(GameManager.GameState.InUI, false, true);
    }

    private void Close()
    {
        if (isOpen)
            PlayDoorSfx(doorCloseSfx, doorCloseSfxVolume);

        CloseImmediate();
        SetGameUiState(GameManager.GameState.OnFoot, true, false);
    }

    private void CloseImmediate()
    {
        isOpen = false;
        PanelObject.SetActive(false);
    }

    private void TryResolveReferences()
    {
        if (shipInventory == null)
            shipInventory = FindFirstObjectByType<ShipInventory>();

        if (moneyLender == null)
            moneyLender = FindFirstObjectByType<MoneyLender>();

        if (tutorialController == null)
            tutorialController = TutorialController.instance != null
                ? TutorialController.instance
                : FindFirstObjectByType<TutorialController>();

        if (textCanvaManager == null)
            textCanvaManager = FindFirstObjectByType<TextCanvaManager>();
    }

    private void SubscribeToReferences()
    {
        if (isSubscribed)
            return;

        if (shipInventory != null)
            shipInventory.OnFishListChange += ChangeFishList;

        if (moneyLender != null)
            moneyLender.OnNewFishWeightPayment += ChangePayment;

        isSubscribed = true;
    }

    private void UnsubscribeFromReferences()
    {
        if (!isSubscribed)
            return;

        if (shipInventory != null)
            shipInventory.OnFishListChange -= ChangeFishList;

        if (moneyLender != null)
            moneyLender.OnNewFishWeightPayment -= ChangePayment;

        isSubscribed = false;
    }

    private void TrySubscribeInput()
    {
        if (isInputSubscribed || InputHandler.instance == null)
            return;

        InputHandler.instance.onPausePressed += HandlePausePressed;
        isInputSubscribed = true;
    }

    private void UnsubscribeInput()
    {
        if (!isInputSubscribed || InputHandler.instance == null)
            return;

        InputHandler.instance.onPausePressed -= HandlePausePressed;
        isInputSubscribed = false;
    }

    private void BindButtons()
    {
        if (areButtonsBound)
            return;

        if (payButton != null)
            payButton.onClick.AddListener(TryPayButton);

        if (closeButton != null)
            closeButton.onClick.AddListener(OnClickClose);

        areButtonsBound = true;
    }

    private void UnbindButtons()
    {
        if (!areButtonsBound)
            return;

        if (payButton != null)
            payButton.onClick.RemoveListener(TryPayButton);

        if (closeButton != null)
            closeButton.onClick.RemoveListener(OnClickClose);

        areButtonsBound = false;
    }

    private void HandlePausePressed()
    {
        if (!isOpen)
            return;

        Close();
    }

    private void ChangePayment(int _amount)
    {
        currentFishWeightPayment = _amount;
        SetPaymentTexts();
    }

    private void ChangeFishList(List<FishData> _fishList, float _fishWeight)
    {
        ownedFish.Clear();

        if (_fishList != null)
            ownedFish.AddRange(_fishList);

        fishWeight = _fishWeight;
        SetPaymentTexts();
        SetFishListText();
    }

    private void RefreshInventorySnapshot()
    {
        ownedFish.Clear();
        fishWeight = 0f;
        currentFishWeightPayment = moneyLender != null ? moneyLender.CurrentFishWeightPayment : 0;

        if (shipInventory == null)
            return;

        ownedFish.AddRange(shipInventory.OwnedFish);
        fishWeight = shipInventory.GetCurrentWeight();
    }

    private void SetPaymentTexts()
    {
        if (paymentText == null)
            return;

        if (ShouldUseTutorialPayment())
        {
            int ownedRequestedFish = tutorialController.OwnedRequestedFishCount;
            int requestedQuantity = tutorialController.RequestedQuantity;
            int requestedWeight = tutorialController.RequestedTotalWeight;
            string fishColor = tutorialController.HasEnoughRequestedFish ? "green" : "red";
            string weightColor = tutorialController.HasEnoughRequestedWeight ? "green" : "red";

            paymentText.text =
                $"Pedido: <color={fishColor}>{ownedRequestedFish}</color> / {requestedQuantity} {tutorialController.RequestedFishName}\n" +
                $"Peso: <color={weightColor}>{fishWeight:0}</color> / {requestedWeight} kg";
            return;
        }

        string color = currentFishWeightPayment > fishWeight ? "red" : "green";
        paymentText.text = $"Pagamento: <color={color}>{fishWeight:0}</color> / {currentFishWeightPayment} kg";
    }

    private void SetFishListText()
    {
        if (fishesText == null)
            return;

        if (ownedFish.Count == 0)
        {
            fishesText.text = "Nenhum peixe no barco.";
            return;
        }

        StringBuilder builder = new StringBuilder();

        foreach (FishData fish in ownedFish)
        {
            if (fish == null || fish.typeOfFish == null)
                continue;

            builder.Append(fish.typeOfFish.fishName);
            builder.Append(", peso: ");
            builder.Append(fish.weight);
            builder.AppendLine(" kg");
        }

        fishesText.text = builder.ToString();
    }

    private bool ShouldUseTutorialPayment()
    {
        if (!useTutorialPaymentWhenAvailable)
            return false;

        if (tutorialController == null && TutorialController.instance != null)
            tutorialController = TutorialController.instance;

        return tutorialController != null && tutorialController.ShouldHandleMoneyLenderPayment(moneyLender);
    }

    private void SetStatus(string _message)
    {
        if (statusText != null)
            statusText.text = _message;
    }

    private void SetGameUiState(GameManager.GameState _state, bool _lockCursor, bool _showCursor)
    {
        if (GameManager.instance != null)
            GameManager.instance.SetState(_state);

        Cursor.lockState = _lockCursor ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = _showCursor;
    }

    private void PlayDoorSfx(AudioClip _clip, float _volume)
    {
        if (AudioManager.Instance == null || _clip == null)
            return;

        AudioManager.Instance.PlaySfx(_clip, _volume);
    }
}