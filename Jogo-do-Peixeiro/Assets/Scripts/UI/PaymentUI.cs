using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PaymentUI : MonoBehaviour
{
    #region Fields

    [Header("Panel")]
    [SerializeField] private GameObject panel;
    [SerializeField] private bool closeOnAwake = true;

    [Header("Modal")]
    [SerializeField] private bool pauseTimeWhileOpen = true;
    [SerializeField] private bool hideHudWhileOpen = true;
    [SerializeField] private bool blockPauseWhileOpen = true;

    [Header("Buttons")]
    [SerializeField] private Button payButton;
    [SerializeField] private Button closeButton;

    [Header("Navigation")]
    [SerializeField] private Selectable firstSelected;

    [Header("Texts References")]
    [SerializeField] private TMP_Text paymentText;
    [SerializeField] private TMP_Text fishesText;
    [SerializeField] private TMP_Text statusText;

    [Header("Payment Mode Groups")]
    [SerializeField] private GameObject defaultPaymentGroup;
    [SerializeField] private GameObject specialDeliveryGroup;

    [Header("Special Delivery")]
    [SerializeField] private Image specialDeliveryFishImage;
    [SerializeField] private TMP_Text specialDeliveryFishNameText;
    [SerializeField] private TMP_Text specialDeliveryRequirementText;
    [SerializeField] private TMP_Text specialDeliveryOwnedText;
    [SerializeField] private Button specialDeliveryPreviewButton;
    [SerializeField] private FishPreviewPanelUI fishPreviewPanel;

    [Header("Ship References")]
    [SerializeField] private ShipInventory shipInventory;

    [Header("Money References")]
    [SerializeField] private PlayerMoneyManager playerMoneyManager;
    [SerializeField] private DebtSystem debtSystem;
    [SerializeField] private CampaignProgressSystem campaignProgress;

    [Header("Lender References")]
    [SerializeField] private MoneyLender moneyLender;

    [Header("Tutorial")]
    [SerializeField] private CampaignQuestGuidanceController tutorialController;
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
    private int currentDebtPayment;
    private int currentDebtBalance;
    private float playerMoney;
    private bool isOpen;
    private bool isSubscribed;
    private bool areButtonsBound;
    private bool isInputSubscribed;
    private bool isCampaignSubscribed;
    private int modalToken = UIModalManager.InvalidToken;

    private GameObject PanelObject => panel != null ? panel : gameObject;

    #endregion

    #region Unity Lifecycle

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
        SubscribeCampaignProgress();
        Refresh();
    }

    private void OnDisable()
    {
        UnbindButtons();
        UnsubscribeInput();
        UnsubscribeFromReferences();
        UnsubscribeCampaignProgress();
        UIModalManager.PopModal(ref modalToken);
    }

    #endregion

    #region Public UI Actions

    public void Open(MoneyLender _moneyLender)
    {
        OpenInternal(_moneyLender, null);
    }

    public void OpenForTutorial(MoneyLender _moneyLender, CampaignQuestGuidanceController _tutorialController)
    {
        if (_tutorialController == null)
            return;

        OpenInternal(_moneyLender, _tutorialController);
    }

    private void OpenInternal(MoneyLender _moneyLender, CampaignQuestGuidanceController _tutorialController)
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
        PushModalState();

        // Fecha o diálogo invocando o callback pendente para não quebrar fluxos
        // que dependem da conclusão do diálogo (ex: tutorial ReadBasicPanels ? GoToBoat)
        if (textCanvaManager != null)
            textCanvaManager.CloseDialog(true);

        SetStatus(string.Empty);
        SetGameUiState(GameManager.GameState.InUI, false, true);

        if (!wasOpen)
            PlayDoorSfx(doorOpenSfx, doorOpenSfxVolume);

        Refresh();
        SelectInitialControl();
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

        if (TryHandleCampaignSpecialDelivery())
            return;

        if (campaignProgress != null && campaignProgress.HasFailedCurrentQuest)
        {
            SetStatus("Prazo da quest encerrado.");
            Refresh();
            return;
        }

        bool success = moneyLender.TryPayDebt(out int paidAmount, out MoneyLender.DebtPaymentResult paymentResult);
        SetStatus(GetDebtPaymentStatusText(success, paidAmount, paymentResult));

        bool shouldCloseAfterPayment = success &&
            (paymentResult == MoneyLender.DebtPaymentResult.Completed ||
             paymentResult == MoneyLender.DebtPaymentResult.PaidOff);

        if (shouldCloseAfterPayment)
        {
            Close();

            if (tutorialController != null)
                tutorialController.NotifyMoneyLenderDebtPayment(success, paidAmount, paymentResult);

            return;
        }

        if (tutorialController != null)
            tutorialController.NotifyMoneyLenderDebtPayment(success, paidAmount, paymentResult);

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

    public void OnClickPreviewSpecialFish()
    {
        ShowSpecialDeliveryFishPreview();
    }

    public void Refresh()
    {
        RefreshInventorySnapshot();
        SetPaymentTexts();
        SetFishListText();
        SetPaymentModeVisuals();
        SetPayButtonState();
        EnsureSelectionIsUsable();
    }

    public void CloseForTutorialFinish()
    {
        if (isOpen)
            PlayDoorSfx(doorCloseSfx, doorCloseSfxVolume);

        CloseImmediate();
        SetGameUiState(GameManager.GameState.InUI, false, true);
    }

    #endregion

    #region Panel State

    private void Close()
    {
        if (isOpen)
            PlayDoorSfx(doorCloseSfx, doorCloseSfxVolume);

        CloseImmediate();
        SetGameUiState(GameManager.GameState.OnFoot, true, false);
    }

    private void CloseImmediate()
    {
        UISelectionHelper.ClearSelection(PanelObject);
        isOpen = false;
        UIModalManager.PopModal(ref modalToken);
        PanelObject.SetActive(false);
    }

    private void PushModalState()
    {
        if (modalToken != UIModalManager.InvalidToken)
            return;

        UIModalRequest request = UIModalRequest.Create(
            this,
            pauseTimeWhileOpen,
            hideHudWhileOpen,
            blockPauseWhileOpen,
            false,
            Close
        );

        modalToken = UIModalManager.PushModal(request);
    }

    #endregion

    #region Reference Resolution And Subscriptions

    private void TryResolveReferences()
    {
        if (shipInventory == null)
            shipInventory = FindFirstObjectByType<ShipInventory>();

        if (playerMoneyManager == null)
            playerMoneyManager = FindFirstObjectByType<PlayerMoneyManager>();

        if (debtSystem == null)
            debtSystem = DebtSystem.GetOrCreate();

        if (campaignProgress == null)
            campaignProgress = CampaignProgressSystem.GetOrCreate();

        if (moneyLender == null)
            moneyLender = FindFirstObjectByType<MoneyLender>();

        if (tutorialController == null)
            tutorialController = CampaignQuestGuidanceController.instance != null
                ? CampaignQuestGuidanceController.instance
                : FindFirstObjectByType<CampaignQuestGuidanceController>();

        if (textCanvaManager == null)
            textCanvaManager = FindFirstObjectByType<TextCanvaManager>();

        if (fishPreviewPanel == null)
            fishPreviewPanel = FindFirstObjectByType<FishPreviewPanelUI>(FindObjectsInactive.Include);

        ResolveModeGroupReferences();
    }

    private void ResolveModeGroupReferences()
    {
        if (defaultPaymentGroup == null)
            defaultPaymentGroup = FindChildGameObject("DefaultPaymentGroup", "DefaultPaymentPanel", "PagamentoPadraoGroup");

        if (specialDeliveryGroup == null)
            specialDeliveryGroup = FindChildGameObject("SpecialDeliveryGroup", "SpecialDeliveryPanel", "EntregaEspecialGroup");

        if (specialDeliveryPreviewButton == null)
            specialDeliveryPreviewButton = FindChildComponent<Button>("SpecialFishPreviewButton", "PreviewFishButton", "VerPeixeButton");

        if (specialDeliveryFishImage == null)
            specialDeliveryFishImage = FindChildComponent<Image>("SpecialFishImage", "RequestedFishImage", "PeixePedidoImage");

        if (specialDeliveryFishNameText == null)
            specialDeliveryFishNameText = FindChildComponent<TMP_Text>("SpecialFishNameText", "RequestedFishNameText", "PeixePedidoText");

        if (specialDeliveryRequirementText == null)
            specialDeliveryRequirementText = FindChildComponent<TMP_Text>("SpecialDeliveryRequirementText", "RequirementText", "RequisitoText");

        if (specialDeliveryOwnedText == null)
            specialDeliveryOwnedText = FindChildComponent<TMP_Text>("SpecialDeliveryOwnedText", "OwnedText", "PossuiText");
    }

    private void SubscribeToReferences()
    {
        if (isSubscribed)
            return;

        if (shipInventory != null)
            shipInventory.OnFishListChange += ChangeFishList;

        if (moneyLender != null)
        {
            moneyLender.OnNewFishWeightPayment += ChangePayment;
            moneyLender.OnNewDebtPayment += ChangeDebtPayment;
        }

        PlayerMoneyManager.OnMoneyChangeEvent += ChangeMoney;
        DebtSystem.OnDebtChangedEvent += ChangeDebtBalance;
        SubscribeCampaignProgress();

        isSubscribed = true;
    }

    private void UnsubscribeFromReferences()
    {
        if (!isSubscribed)
            return;

        if (shipInventory != null)
            shipInventory.OnFishListChange -= ChangeFishList;

        if (moneyLender != null)
        {
            moneyLender.OnNewFishWeightPayment -= ChangePayment;
            moneyLender.OnNewDebtPayment -= ChangeDebtPayment;
        }

        PlayerMoneyManager.OnMoneyChangeEvent -= ChangeMoney;
        DebtSystem.OnDebtChangedEvent -= ChangeDebtBalance;
        UnsubscribeCampaignProgress();

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

        if (specialDeliveryPreviewButton != null)
            specialDeliveryPreviewButton.onClick.AddListener(OnClickPreviewSpecialFish);

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

        if (specialDeliveryPreviewButton != null)
            specialDeliveryPreviewButton.onClick.RemoveListener(OnClickPreviewSpecialFish);

        areButtonsBound = false;
    }

    #endregion

    #region Input And Data Event Handlers

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
        SetPayButtonState();
    }

    private void ChangeDebtPayment(int _amount)
    {
        currentDebtPayment = _amount;
        SetPaymentTexts();
        SetPayButtonState();
    }

    private void ChangeDebtBalance(int _currentDebt, int _changeAmount)
    {
        currentDebtBalance = _currentDebt;

        if (moneyLender != null)
            currentDebtPayment = moneyLender.GetCurrentPayableDebtPayment();

        SetPaymentTexts();
        SetPayButtonState();
    }

    private void ChangeMoney(float _amount)
    {
        playerMoney = _amount;
        SetPaymentTexts();
        SetPayButtonState();
    }

    private void ChangeFishList(List<FishData> _fishList, float _fishWeight)
    {
        ownedFish.Clear();

        if (_fishList != null)
            ownedFish.AddRange(_fishList);

        fishWeight = _fishWeight;
        SetPaymentTexts();
        SetFishListText();
        SetPayButtonState();
    }

    private void RefreshInventorySnapshot()
    {
        ownedFish.Clear();
        fishWeight = 0f;
        currentFishWeightPayment = moneyLender != null ? moneyLender.CurrentFishWeightPayment : 0;
        currentDebtPayment = moneyLender != null ? moneyLender.GetCurrentPayableDebtPayment() : 0;
        currentDebtBalance = debtSystem != null ? debtSystem.CurrentDebt : 0;
        playerMoney = playerMoneyManager != null ? playerMoneyManager.PlayerMoney : 0f;

        if (shipInventory == null)
            return;

        ownedFish.AddRange(shipInventory.OwnedFish);
        fishWeight = shipInventory.GetCurrentWeight();
    }

    #endregion

    #region Payment Text Refresh

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

        if (IsCampaignSpecialDeliveryActive())
        {
            paymentText.text = GetCampaignSpecialDeliveryText();
            return;
        }

        if (IsCampaignPaymentActive())
        {
            paymentText.text = GetCampaignPaymentText();
            return;
        }

        string moneyColor = playerMoney >= currentDebtPayment ? "green" : "red";
        string debtColor = currentDebtBalance > 0 ? "red" : "green";
        string debtValue = currentDebtBalance > 0 ? $"-R$ {currentDebtBalance}" : "R$ 0";
        paymentText.text =
            $"Divida total: <color={debtColor}>{debtValue}</color>\n" +
            $"Pagamento: R$ {currentDebtPayment}\n" +
            $"Dinheiro: <color={moneyColor}>R$ {playerMoney:0}</color>";
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
            builder.Append(" kg, valor: R$ ");
            builder.AppendLine(FishPriceCalculator.CalculatePrice(fish).ToString());
        }

        fishesText.text = builder.ToString();
    }

    #endregion

    #region Payment Rules

    private bool ShouldUseTutorialPayment()
    {
        if (!useTutorialPaymentWhenAvailable)
            return false;

        if (tutorialController == null && CampaignQuestGuidanceController.instance != null)
            tutorialController = CampaignQuestGuidanceController.instance;

        return tutorialController != null && tutorialController.ShouldHandleMoneyLenderPayment(moneyLender);
    }

    private bool TryHandleCampaignSpecialDelivery()
    {
        if (!IsCampaignSpecialDeliveryActive())
            return false;

        if (campaignProgress.HasFailedCurrentQuest)
        {
            SetStatus("Prazo da entrega encerrado.");
            Refresh();
            return true;
        }

        FishScriptableObject specialFish = campaignProgress.SpecialDeliveryFish;

        if (specialFish == null || campaignProgress.SpecialDeliveryQuantity <= 0)
        {
            SetStatus("Entrega especial sem peixe configurado.");
            Refresh();
            return true;
        }

        bool success = moneyLender.TryGetSpecificFishPayment(specialFish, campaignProgress.SpecialDeliveryQuantity);
        SetStatus(success ? "Entrega especial concluida." : "Peixe especial insuficiente.");

        if (success)
        {
            Close();

            if (tutorialController != null)
                tutorialController.NotifyMoneyLenderSpecialDeliveryCompleted();

            return true;
        }

        Refresh();
        return true;
    }

    private string GetDebtPaymentStatusText(bool _success, int _paidAmount, MoneyLender.DebtPaymentResult _paymentResult)
    {
        if (!_success)
            return "Dinheiro insuficiente.";

        return _paymentResult switch
        {
            MoneyLender.DebtPaymentResult.Partial => $"Pagamento parcial: R$ {_paidAmount}.",
            MoneyLender.DebtPaymentResult.Completed => "Meta da quest paga.",
            MoneyLender.DebtPaymentResult.PaidOff => "Divida quitada.",
            _ => "Dinheiro insuficiente."
        };
    }

    private bool IsCampaignPaymentActive()
    {
        return campaignProgress != null &&
               campaignProgress.GameMode == GameProgressMode.Campaign &&
               !campaignProgress.IsCampaignCompleted &&
               !campaignProgress.CurrentQuestRequiresSpecialDelivery;
    }

    private bool IsCampaignSpecialDeliveryActive()
    {
        return campaignProgress != null &&
               campaignProgress.GameMode == GameProgressMode.Campaign &&
               !campaignProgress.IsCampaignCompleted &&
               campaignProgress.CurrentQuestRequiresSpecialDelivery;
    }

    private string GetCampaignPaymentText()
    {
        if (campaignProgress.HasFailedCurrentQuest)
            return $"Quest {campaignProgress.CurrentQuestIndex}/{campaignProgress.MaxQuestCount}\n<color=red>Prazo encerrado.</color>\nA quest falhou.";

        string moneyColor = playerMoney > 0 ? "green" : "red";
        string debtColor = currentDebtBalance > 0 ? "red" : "green";
        string deadlineColor = campaignProgress.DaysRemainingInCurrentQuest <= 1 ? "#F2C94C" : "white";
        string debtValue = currentDebtBalance > 0 ? $"-R$ {currentDebtBalance}" : "R$ 0";
        int questRemaining = campaignProgress.QuestDebtPaymentRemaining;

        return
            $"Quest {campaignProgress.CurrentQuestIndex}/{campaignProgress.MaxQuestCount}: {campaignProgress.CurrentQuestName}\n" +
            $"Prazo: <color={deadlineColor}>{campaignProgress.DaysRemainingInCurrentQuest} dia(s)</color>\n" +
            $"Meta da quest: R$ {campaignProgress.QuestDebtPaidAmount}/{campaignProgress.QuestDebtPaymentTarget}\n" +
            $"Falta na quest: R$ {questRemaining}\n" +
            $"Pagamento agora: R$ {currentDebtPayment}\n" +
            $"Divida total: <color={debtColor}>{debtValue}</color>\n" +
            $"Dinheiro: <color={moneyColor}>R$ {playerMoney:0}</color>";
    }

    private string GetCampaignSpecialDeliveryText()
    {
        if (campaignProgress.HasFailedCurrentQuest)
            return $"Quest {campaignProgress.CurrentQuestIndex}/{campaignProgress.MaxQuestCount}\n<color=red>Prazo encerrado.</color>\nA entrega falhou.";

        FishScriptableObject specialFish = campaignProgress.SpecialDeliveryFish;
        string fishName = GetFishDisplayName(specialFish);
        int requiredQuantity = campaignProgress.SpecialDeliveryQuantity;
        int ownedQuantity = shipInventory != null && specialFish != null ? shipInventory.CountFish(specialFish) : 0;
        string fishColor = ownedQuantity >= requiredQuantity && requiredQuantity > 0 ? "green" : "red";
        string deadlineColor = campaignProgress.DaysRemainingInCurrentQuest <= 1 ? "#F2C94C" : "white";
        string debtValue = currentDebtBalance > 0 ? $"-R$ {currentDebtBalance}" : "R$ 0";

        return
            $"Quest {campaignProgress.CurrentQuestIndex}/{campaignProgress.MaxQuestCount}: entrega especial\n" +
            $"Prazo: <color={deadlineColor}>{campaignProgress.DaysRemainingInCurrentQuest} dia(s)</color>\n" +
            $"Entregue: <color={fishColor}>{ownedQuantity}</color>/{requiredQuantity} {fishName}\n" +
            $"Divida total: <color=red>{debtValue}</color>";
    }

    private string GetFishDisplayName(FishScriptableObject _fish)
    {
        if (_fish == null)
            return "peixe especial";

        return !string.IsNullOrWhiteSpace(_fish.fishName) ? _fish.fishName : _fish.name;
    }

    private void SetPayButtonState()
    {
        if (payButton == null)
            return;

        if (ShouldUseTutorialPayment())
        {
            SetButtonText(payButton, "Entregar");
            payButton.interactable = true;
            return;
        }

        if (IsCampaignSpecialDeliveryActive())
        {
            SetButtonText(payButton, "Entregar");
            FishScriptableObject specialFish = campaignProgress.SpecialDeliveryFish;
            int requiredQuantity = campaignProgress.SpecialDeliveryQuantity;
            int ownedQuantity = shipInventory != null && specialFish != null ? shipInventory.CountFish(specialFish) : 0;
            payButton.interactable = !campaignProgress.HasFailedCurrentQuest &&
                                     specialFish != null &&
                                     requiredQuantity > 0 &&
                                     ownedQuantity >= requiredQuantity;
            return;
        }

        SetButtonText(payButton, "Pagar");
        payButton.interactable = moneyLender != null &&
                                 (campaignProgress == null || !campaignProgress.HasFailedCurrentQuest) &&
                                 currentDebtPayment > 0 &&
                                 playerMoney > 0;
    }

    #endregion

    #region Payment Mode Visuals

    private void SetPaymentModeVisuals()
    {
        bool isSpecialDelivery = IsCampaignSpecialDeliveryActive();

        SetObjectActive(defaultPaymentGroup, !isSpecialDelivery);
        SetObjectActive(specialDeliveryGroup, isSpecialDelivery);
        SetSpecialDeliveryVisuals(isSpecialDelivery);
    }

    private void SetSpecialDeliveryVisuals(bool _isSpecialDelivery)
    {
        FishScriptableObject specialFish = _isSpecialDelivery && campaignProgress != null
            ? campaignProgress.SpecialDeliveryFish
            : null;

        if (specialDeliveryFishImage != null)
        {
            Sprite icon = specialFish != null ? specialFish.InventoryIcon : null;
            specialDeliveryFishImage.sprite = icon;
            specialDeliveryFishImage.enabled = icon != null;
            specialDeliveryFishImage.preserveAspect = true;
        }

        if (specialDeliveryFishNameText != null)
            specialDeliveryFishNameText.text = specialFish != null ? GetFishDisplayName(specialFish) : "Peixe especial";

        if (specialDeliveryRequirementText != null)
            specialDeliveryRequirementText.text = GetSpecialDeliveryRequirementText(specialFish);

        if (specialDeliveryOwnedText != null)
            specialDeliveryOwnedText.text = GetSpecialDeliveryOwnedText(specialFish);

        if (specialDeliveryPreviewButton != null)
            specialDeliveryPreviewButton.interactable = specialFish != null && fishPreviewPanel != null;
    }

    private string GetSpecialDeliveryRequirementText(FishScriptableObject _specialFish)
    {
        if (!IsCampaignSpecialDeliveryActive() || campaignProgress == null)
            return string.Empty;

        string fishName = GetFishDisplayName(_specialFish);
        string weightText = campaignProgress.SpecialDeliveryRequiredWeight > 0
            ? $" | Peso: {campaignProgress.SpecialDeliveryRequiredWeight} kg"
            : string.Empty;

        return $"Pedido: {campaignProgress.SpecialDeliveryQuantity}x {fishName}{weightText}";
    }

    private string GetSpecialDeliveryOwnedText(FishScriptableObject _specialFish)
    {
        if (!IsCampaignSpecialDeliveryActive() || campaignProgress == null)
            return string.Empty;

        int ownedQuantity = shipInventory != null && _specialFish != null ? shipInventory.CountFish(_specialFish) : 0;
        int ownedWeight = GetOwnedSpecificFishWeight(_specialFish);
        string weightText = campaignProgress.SpecialDeliveryRequiredWeight > 0
            ? $" | Peso no barco: {ownedWeight}/{campaignProgress.SpecialDeliveryRequiredWeight} kg"
            : string.Empty;

        return $"No barco: {ownedQuantity}/{campaignProgress.SpecialDeliveryQuantity}{weightText}";
    }

    private int GetOwnedSpecificFishWeight(FishScriptableObject _specialFish)
    {
        if (_specialFish == null)
            return 0;

        int totalWeight = 0;

        for (int i = 0; i < ownedFish.Count; i++)
        {
            FishData fish = ownedFish[i];

            if (fish != null && fish.typeOfFish == _specialFish)
                totalWeight += fish.weight;
        }

        return totalWeight;
    }

    private void ShowSpecialDeliveryFishPreview()
    {
        TryResolveReferences();

        FishScriptableObject specialFish = IsCampaignSpecialDeliveryActive() && campaignProgress != null
            ? campaignProgress.SpecialDeliveryFish
            : null;

        if (specialFish == null)
        {
            SetStatus("Nenhum peixe especial configurado.");
            return;
        }

        if (fishPreviewPanel == null)
        {
            SetStatus("Painel de peixe nao configurado.");
            return;
        }

        fishPreviewPanel.ShowFish(specialFish, PanelObject);
    }

    private void SetObjectActive(GameObject _target, bool _active)
    {
        if (_target != null)
            _target.SetActive(_active);
    }

    private GameObject FindChildGameObject(params string[] _names)
    {
        Transform found = FindChildTransform(_names);
        return found != null ? found.gameObject : null;
    }

    private T FindChildComponent<T>(params string[] _names) where T : Component
    {
        Transform found = FindChildTransform(_names);
        return found != null ? found.GetComponent<T>() : null;
    }

    private Transform FindChildTransform(params string[] _names)
    {
        Transform[] children = PanelObject.GetComponentsInChildren<Transform>(true);

        for (int i = 0; i < children.Length; i++)
        {
            Transform child = children[i];

            for (int j = 0; j < _names.Length; j++)
            {
                if (child.name == _names[j])
                    return child;
            }
        }

        return null;
    }

    #endregion

    #region Selection And UI Helpers

    private void SelectInitialControl()
    {
        Selectable target = firstSelected != null ? firstSelected : GetPreferredSelectable();
        UISelectionHelper.Select(target, PanelObject);
    }

    private void EnsureSelectionIsUsable()
    {
        if (!isOpen)
            return;

        Selectable current = UISelectionHelper.CurrentSelectableInScope(PanelObject);

        if (UISelectionHelper.IsUsable(current))
            return;

        SelectInitialControl();
    }

    private Selectable GetPreferredSelectable()
    {
        if (UISelectionHelper.IsUsable(payButton))
            return payButton;

        if (UISelectionHelper.IsUsable(closeButton))
            return closeButton;

        return null;
    }

    private void SetButtonText(Button _button, string _text)
    {
        if (_button == null)
            return;

        TMP_Text buttonText = _button.GetComponentInChildren<TMP_Text>(true);

        if (buttonText != null)
            buttonText.text = _text;
    }

    private void SubscribeCampaignProgress()
    {
        if (isCampaignSubscribed || campaignProgress == null)
            return;

        campaignProgress.OnProgressChanged += Refresh;
        campaignProgress.OnQuestAdvanced += Refresh;
        campaignProgress.OnQuestDeadlineExpired += Refresh;
        campaignProgress.OnCampaignCompleted += Refresh;
        isCampaignSubscribed = true;
    }

    private void UnsubscribeCampaignProgress()
    {
        if (!isCampaignSubscribed || campaignProgress == null)
            return;

        campaignProgress.OnProgressChanged -= Refresh;
        campaignProgress.OnQuestAdvanced -= Refresh;
        campaignProgress.OnQuestDeadlineExpired -= Refresh;
        campaignProgress.OnCampaignCompleted -= Refresh;
        isCampaignSubscribed = false;
    }

    private void SetStatus(string _message)
    {
        if (statusText != null)
            statusText.text = _message;
    }

    #endregion

    #region Game State And Audio

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

    #endregion
}
