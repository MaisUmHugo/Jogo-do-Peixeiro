using System.Collections.Generic;
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
    [SerializeField] private TMP_Text statusText;

    [Header("Status Feedback")]
    [SerializeField] private bool animateStatusText = true;
    [SerializeField] private float statusVisibleDuration = 1.2f;
    [SerializeField] private float statusFadeDuration = 0.35f;
    [SerializeField] private float statusRiseDistance = 24f;

    [Header("Payment Mode Groups")]
    [SerializeField] private GameObject defaultPaymentGroup;
    [SerializeField] private GameObject specialDeliveryGroup;

    [Header("Default Payment Texts")]
    [SerializeField] private TMP_Text defaultTitleText;
    [SerializeField] private TMP_Text defaultQuestText;
    [SerializeField] private TMP_Text defaultDeadlineText;
    [SerializeField] private TMP_Text defaultDebtText;
    [SerializeField] private TMP_Text defaultGoalText;
    [SerializeField] private TMP_Text defaultMoneyText;

    [Header("Special Delivery")]
    [SerializeField] private TMP_Text specialDeliveryTitleText;
    [SerializeField] private TMP_Text specialDeliveryQuestText;
    [SerializeField] private TMP_Text specialDeliveryDeadlineText;
    [SerializeField] private TMP_Text specialDeliveryDebtText;
    [SerializeField] private Image specialDeliveryFishImage;
    [SerializeField] private TMP_Text specialDeliveryFishNameText;
    [SerializeField] private TMP_Text specialDeliveryRequirementText;
    [SerializeField] private TMP_Text specialDeliveryOwnedText;
    [SerializeField] private Button specialDeliveryPreviewButton;
    [SerializeField] private FishPreviewPanelUI fishPreviewPanel;

    [Header("Special Delivery Effects")]
    [SerializeField] private bool animateSpecialDeliveryText = true;
    [SerializeField] private TMP_Text[] specialDeliveryAnimatedTexts;
    [SerializeField] private Gradient specialDeliveryTintGradient = CreateDefaultSpecialDeliveryTintGradient();
    [SerializeField] private float specialDeliveryTintSpeed = 2.5f;
    [SerializeField] private float specialDeliveryPulseSpeed = 5f;
    [SerializeField] private float specialDeliveryPulseScale = 0.08f;

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

    [Header("Áudio")]
    [SerializeField, InspectorName("Door Open SFX")] private AudioClip doorOpenSfx;
    [SerializeField, InspectorName("Door Close SFX")] private AudioClip doorCloseSfx;
    [SerializeField, InspectorName("Special Delivery SFX")] private AudioClip specialDeliveryOpenSfx;
    [SerializeField, Range(0f, 1f), InspectorName("Door Open SFX Volume")] private float doorOpenSfxVolume = 1f;
    [SerializeField, Range(0f, 1f), InspectorName("Door Close SFX Volume")] private float doorCloseSfxVolume = 1f;
    [SerializeField, Range(0f, 1f), InspectorName("Special Delivery SFX Volume")] private float specialDeliveryOpenSfxVolume = 1f;

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
    private readonly Dictionary<TMP_Text, TextFxState> specialDeliveryTextVisualStates = new Dictionary<TMP_Text, TextFxState>();
    private bool hasStatusOriginalState;
    private Color statusOriginalColor;
    private Vector3 statusOriginalLocalPosition;
    private Vector2 statusOriginalAnchoredPosition;
    private float statusMessageTimer;
    private float statusMessageTotalDuration;

    private GameObject PanelObject => panel != null ? panel : gameObject;

    #endregion

    #region Types

    private struct TextFxState
    {
        public Color Color;
        public Vector3 LocalScale;

        public TextFxState(Color _color, Vector3 _localScale)
        {
            Color = _color;
            LocalScale = _localScale;
        }
    }

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        TryResolveReferences();
        ClearStatusText();

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
        RestoreSpecialDeliveryTextFx();
        UIModalManager.PopModal(ref modalToken);
    }

    private void Update()
    {
        if (!isOpen)
            return;

        UpdateStatusTextFeedback();
        UpdateSpecialDeliveryTextFx();
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

    public void ShowStatusMessage(string _message)
    {
        SetStatus(_message);
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
        {
            PlayDoorSfx(doorOpenSfx, doorOpenSfxVolume);
            PlaySpecialDeliveryOpenSfxIfNeeded();
        }

        Refresh();
        SelectInitialControl();
    }

    public void TryPayButton()
    {
        if (moneyLender == null)
        {
            SetStatus("Cobrador não encontrado.");
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
            CloseAfterPotentialOutcome();

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

    private void CloseAfterPotentialOutcome()
    {
        if (isOpen)
            PlayDoorSfx(doorCloseSfx, doorCloseSfxVolume);

        CloseImmediate();

        if (UIModalManager.HasOpenModal)
        {
            SetGameUiState(GameManager.GameState.InUI, false, true);
            return;
        }

        SetGameUiState(GameManager.GameState.OnFoot, true, false);
    }

    private void CloseImmediate()
    {
        UISelectionHelper.ClearSelection(PanelObject);
        isOpen = false;
        ClearStatusText();
        RestoreSpecialDeliveryTextFx();
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

        if (defaultTitleText == null)
            defaultTitleText = FindChildComponent<TMP_Text>("DefaultTitleText", "PaymentTitleText", "TituloPagamentoText");

        if (defaultQuestText == null)
            defaultQuestText = FindChildComponent<TMP_Text>("DefaultQuestText", "PaymentQuestText", "QuestPaymentText");

        if (defaultDeadlineText == null)
            defaultDeadlineText = FindChildComponent<TMP_Text>("DefaultDeadlineText", "PaymentDeadlineText", "PrazoPagamentoText");

        if (defaultDebtText == null)
            defaultDebtText = FindChildComponent<TMP_Text>("DefaultDebtText", "DebtValueText", "DividaText");

        if (defaultGoalText == null)
            defaultGoalText = FindChildComponent<TMP_Text>("DefaultGoalText", "PaymentGoalText", "MetaPagamentoText");

        if (defaultMoneyText == null)
            defaultMoneyText = FindChildComponent<TMP_Text>("DefaultMoneyText", "PlayerMoneyText", "DinheiroText");

        if (specialDeliveryTitleText == null)
            specialDeliveryTitleText = FindChildComponent<TMP_Text>("SpecialDeliveryTitleText", "EntregaEspecialTitleText");

        if (specialDeliveryQuestText == null)
            specialDeliveryQuestText = FindChildComponent<TMP_Text>("SpecialDeliveryQuestText", "SpecialQuestText");

        if (specialDeliveryDeadlineText == null)
            specialDeliveryDeadlineText = FindChildComponent<TMP_Text>("SpecialDeliveryDeadlineText", "SpecialDeadlineText");

        if (specialDeliveryDebtText == null)
            specialDeliveryDebtText = FindChildComponent<TMP_Text>("SpecialDeliveryDebtText", "SpecialDebtText");

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
        if (!isOpen ||
            UIModalManager.WasBackHandledThisFrame ||
            !UIModalManager.IsTopModal(modalToken))
        {
            return;
        }

        UIModalManager.MarkBackHandledThisFrame();
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
        SetSeparatedPaymentTexts();
    }

    private void SetSeparatedPaymentTexts()
    {
        if (ShouldUseTutorialPayment())
        {
            SetTutorialSeparatedTexts();
            return;
        }

        if (IsCampaignSpecialDeliveryActive())
        {
            SetSpecialDeliverySeparatedTexts();
            return;
        }

        SetDefaultPaymentSeparatedTexts();
    }

    private void SetTutorialSeparatedTexts()
    {
        int ownedRequestedFish = tutorialController != null ? tutorialController.OwnedRequestedFishCount : 0;
        int requestedQuantity = tutorialController != null ? tutorialController.RequestedQuantity : 0;
        int requestedWeight = tutorialController != null ? tutorialController.RequestedTotalWeight : 0;
        string requestedFishName = tutorialController != null ? tutorialController.RequestedFishName : "peixe";
        string weightText = requestedWeight > 0 ? $" | Peso: {fishWeight:0}/{requestedWeight} kg" : string.Empty;

        SetText(defaultTitleText, "Pedido");
        SetText(defaultQuestText, "Quest 1 - Tutorial");
        SetText(defaultDeadlineText, GetDeadlineLine());
        SetText(defaultDebtText, GetDebtLine());
        SetText(defaultGoalText, $"Pedido: {ownedRequestedFish}/{requestedQuantity} {requestedFishName}{weightText}");
        SetText(defaultMoneyText, $"Dinheiro: R$ {playerMoney:0}");

        ClearSpecialDeliverySeparatedTexts();
    }

    private void SetDefaultPaymentSeparatedTexts()
    {
        string debtValue = GetDebtValueText();

        SetText(defaultTitleText, "Dívida");
        SetText(defaultQuestText, GetQuestLine());
        SetText(defaultDeadlineText, GetDeadlineLine());
        SetText(defaultDebtText, $"Dívida total: {debtValue}");
        SetText(defaultGoalText, GetGoalLine());
        SetText(defaultMoneyText, $"Dinheiro: R$ {playerMoney:0}");

        ClearSpecialDeliverySeparatedTexts();
    }

    private void SetSpecialDeliverySeparatedTexts()
    {
        FishScriptableObject specialFish = campaignProgress != null ? campaignProgress.SpecialDeliveryFish : null;

        SetText(specialDeliveryTitleText, "Entrega especial");
        SetText(specialDeliveryQuestText, GetQuestLine());
        SetText(specialDeliveryDeadlineText, GetDeadlineLine());
        SetText(specialDeliveryDebtText, GetSpecialDeliveryDebtLine());
        SetSpecialDeliveryVisuals(true);

        ClearDefaultPaymentSeparatedTexts();
    }

    private void ClearDefaultPaymentSeparatedTexts()
    {
        SetText(defaultTitleText, string.Empty);
        SetText(defaultQuestText, string.Empty);
        SetText(defaultDeadlineText, string.Empty);
        SetText(defaultDebtText, string.Empty);
        SetText(defaultGoalText, string.Empty);
        SetText(defaultMoneyText, string.Empty);
    }

    private void ClearSpecialDeliverySeparatedTexts()
    {
        RestoreSpecialDeliveryTextFx();
        SetText(specialDeliveryTitleText, string.Empty);
        SetText(specialDeliveryQuestText, string.Empty);
        SetText(specialDeliveryDeadlineText, string.Empty);
        SetText(specialDeliveryDebtText, string.Empty);
    }

    private void SetText(TMP_Text _text, string _value)
    {
        if (_text == null)
            return;

        bool hasValue = !string.IsNullOrWhiteSpace(_value);
        _text.gameObject.SetActive(hasValue);
        _text.text = hasValue ? _value : string.Empty;
    }

    private string GetQuestLine()
    {
        if (campaignProgress == null)
            return string.Empty;

        if (campaignProgress.GameMode == GameProgressMode.Endless)
            return $"Modo sem fim - Entrega {campaignProgress.CurrentQuestIndex}";

        if (campaignProgress.IsCampaignCompleted)
            return "Campanha concluída";

        string questLabel = $"Quest {campaignProgress.CurrentQuestIndex}";

        if (campaignProgress.IsCurrentQuestTutorial)
            return $"{questLabel} - Tutorial";

        if (campaignProgress.IsCurrentQuestFinal)
            return $"{questLabel} - Final";

        return string.IsNullOrWhiteSpace(campaignProgress.CurrentQuestName)
            ? questLabel
            : $"{questLabel} - {campaignProgress.CurrentQuestName}";
    }

    private string GetDeadlineLine()
    {
        if (campaignProgress == null ||
            campaignProgress.IsCampaignCompleted)
        {
            return string.Empty;
        }

        int daysRemaining = campaignProgress.DaysRemainingInCurrentQuest;
        return campaignProgress.HasFailedCurrentQuest
            ? "Prazo: encerrado"
            : $"Prazo: {daysRemaining} {GetDayLabel(daysRemaining)}";
    }

    private string GetGoalLine()
    {
        if (campaignProgress == null ||
            campaignProgress.IsCampaignCompleted)
        {
            return string.Empty;
        }

        if (campaignProgress.HasFailedCurrentQuest)
            return "Meta falhou";

        if (campaignProgress.CurrentQuestRequiresSpecialDelivery)
        {
            string fishName = GetFishDisplayName(campaignProgress.SpecialDeliveryFish);
            string deliveryText = $"Entrega especial: {campaignProgress.SpecialDeliveryQuantity}x {fishName}";

            if (campaignProgress.QuestDebtPaymentTarget > 0)
                return $"{deliveryText} | Meta: R$ {campaignProgress.QuestDebtPaidAmount}/{campaignProgress.QuestDebtPaymentTarget}";

            return deliveryText;
        }

        return $"Meta da quest: R$ {campaignProgress.QuestDebtPaidAmount}/{campaignProgress.QuestDebtPaymentTarget}";
    }

    private string GetDebtLine()
    {
        return $"Dívida total: {GetDebtValueText()}";
    }

    private string GetDebtValueText()
    {
        return currentDebtBalance > 0 ? $"-R$ {currentDebtBalance}" : "R$ 0";
    }

    private static string GetDayLabel(int _days)
    {
        return _days == 1 ? "dia" : "dias";
    }

    private string GetSpecialDeliveryDebtLine()
    {
        string totalDebtLine = $"Dívida total: {GetDebtValueText()}";

        if (campaignProgress == null || campaignProgress.QuestDebtPaymentTarget <= 0)
            return totalDebtLine;

        return $"{totalDebtLine}\nMeta da quest: R$ {campaignProgress.QuestDebtPaidAmount}/{campaignProgress.QuestDebtPaymentTarget}";
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

        if (campaignProgress.QuestDebtPaymentRemaining > 0)
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
        SetStatus(success ? "Entrega especial concluída." : "Você não tem peixes suficientes.");

        if (success)
        {
            CloseAfterPotentialOutcome();

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
            MoneyLender.DebtPaymentResult.MoneyTargetCompleted => "Meta em dinheiro paga. Entregue o peixe especial.",
            MoneyLender.DebtPaymentResult.Completed => "Meta da quest paga.",
            MoneyLender.DebtPaymentResult.PaidOff => "Dívida quitada.",
            _ => "Dinheiro insuficiente."
        };
    }

    private bool IsCampaignSpecialDeliveryActive()
    {
        return campaignProgress != null &&
               !campaignProgress.IsCampaignCompleted &&
               campaignProgress.CurrentQuestRequiresSpecialDelivery;
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
            FishScriptableObject specialFish = campaignProgress.SpecialDeliveryFish;
            int requiredQuantity = campaignProgress.SpecialDeliveryQuantity;
            int ownedQuantity = shipInventory != null && specialFish != null ? shipInventory.CountFish(specialFish) : 0;
            bool needsDebtPayment = campaignProgress.QuestDebtPaymentRemaining > 0;

            SetButtonText(payButton, needsDebtPayment ? "Pagar" : "Entregar");
            payButton.interactable = !campaignProgress.HasFailedCurrentQuest &&
                                     (needsDebtPayment
                                         ? currentDebtPayment > 0 && playerMoney > 0
                                         : specialFish != null &&
                                           requiredQuantity > 0 &&
                                           ownedQuantity >= requiredQuantity);
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

        if (!isSpecialDelivery)
            RestoreSpecialDeliveryTextFx();

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
            ? GetSpecialDeliveryOwnedWeightText(ownedWeight)
            : string.Empty;

        return $"Possui: {ownedQuantity} no inventário{weightText}";
    }

    private string GetSpecialDeliveryOwnedWeightText(int _ownedWeight)
    {
        int requiredWeight = Mathf.Max(0, campaignProgress.SpecialDeliveryRequiredWeight);
        return $" | Peso: {_ownedWeight}/{requiredWeight} kg";
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
            SetStatus("Painel de peixe não configurado.");
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

    #region Special Delivery Text Effects

    private void UpdateSpecialDeliveryTextFx()
    {
        if (!animateSpecialDeliveryText || !IsCampaignSpecialDeliveryActive())
            return;

        float colorTime = Mathf.Repeat(Time.unscaledTime * specialDeliveryTintSpeed, 1f);
        float pulse = 1f + Mathf.Sin(Time.unscaledTime * specialDeliveryPulseSpeed) * specialDeliveryPulseScale;
        Color tint = specialDeliveryTintGradient != null
            ? specialDeliveryTintGradient.Evaluate(colorTime)
            : Color.white;

        bool hasAssignedTexts = false;

        if (specialDeliveryAnimatedTexts != null)
        {
            for (int i = 0; i < specialDeliveryAnimatedTexts.Length; i++)
            {
                TMP_Text text = specialDeliveryAnimatedTexts[i];

                if (text == null)
                    continue;

                hasAssignedTexts = true;
                ApplySpecialDeliveryTextFx(text, tint, pulse);
            }
        }

        if (hasAssignedTexts)
            return;

        ApplySpecialDeliveryTextFx(specialDeliveryTitleText, tint, pulse);
        ApplySpecialDeliveryTextFx(specialDeliveryRequirementText, tint, pulse);
    }

    private void ApplySpecialDeliveryTextFx(TMP_Text _text, Color _color, float _scale)
    {
        if (_text == null || !_text.gameObject.activeInHierarchy)
            return;

        StoreSpecialDeliveryTextState(_text);
        TextFxState originalState = specialDeliveryTextVisualStates[_text];
        _text.color = _color;
        _text.transform.localScale = originalState.LocalScale * _scale;
    }

    private void StoreSpecialDeliveryTextState(TMP_Text _text)
    {
        if (_text == null || specialDeliveryTextVisualStates.ContainsKey(_text))
            return;

        specialDeliveryTextVisualStates.Add(_text, new TextFxState(_text.color, _text.transform.localScale));
    }

    private void RestoreSpecialDeliveryTextFx()
    {
        foreach (KeyValuePair<TMP_Text, TextFxState> pair in specialDeliveryTextVisualStates)
        {
            if (pair.Key == null)
                continue;

            pair.Key.color = pair.Value.Color;
            pair.Key.transform.localScale = pair.Value.LocalScale;
        }

        specialDeliveryTextVisualStates.Clear();
    }

    private static Gradient CreateDefaultSpecialDeliveryTintGradient()
    {
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(1f, 0.12f, 0.05f), 0f),
                new GradientColorKey(new Color(1f, 0.85f, 0.05f), 0.25f),
                new GradientColorKey(new Color(0.1f, 0.85f, 1f), 0.5f),
                new GradientColorKey(new Color(1f, 0.15f, 0.9f), 0.75f),
                new GradientColorKey(new Color(1f, 0.12f, 0.05f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 1f)
            }
        );

        return gradient;
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
        if (statusText == null)
            return;

        if (string.IsNullOrWhiteSpace(_message))
        {
            ClearStatusText();
            return;
        }

        EnsureStatusTextOriginalState();

        statusText.text = _message;
        statusText.gameObject.SetActive(true);
        statusText.color = statusOriginalColor;
        SetStatusTextOffset(0f);

        statusMessageTotalDuration = Mathf.Max(0.05f, statusVisibleDuration + statusFadeDuration);
        statusMessageTimer = statusMessageTotalDuration;
    }

    #endregion

    #region Status Feedback

    private void UpdateStatusTextFeedback()
    {
        if (statusText == null || statusMessageTimer <= 0f)
            return;

        EnsureStatusTextOriginalState();

        statusMessageTimer -= Time.unscaledDeltaTime;

        if (animateStatusText)
        {
            float elapsed = statusMessageTotalDuration - statusMessageTimer;
            float moveProgress = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, statusMessageTotalDuration));
            float fadeProgress = statusMessageTimer <= statusFadeDuration
                ? Mathf.Clamp01(statusMessageTimer / Mathf.Max(0.01f, statusFadeDuration))
                : 1f;

            Color animatedColor = statusOriginalColor;
            animatedColor.a *= fadeProgress;
            statusText.color = animatedColor;
            SetStatusTextOffset(statusRiseDistance * moveProgress);
        }

        if (statusMessageTimer <= 0f)
            ClearStatusText();
    }

    private void EnsureStatusTextOriginalState()
    {
        if (hasStatusOriginalState || statusText == null)
            return;

        statusOriginalColor = statusText.color;
        statusOriginalLocalPosition = statusText.transform.localPosition;

        if (statusText.transform is RectTransform rectTransform)
            statusOriginalAnchoredPosition = rectTransform.anchoredPosition;

        hasStatusOriginalState = true;
    }

    private void SetStatusTextOffset(float _offsetY)
    {
        if (statusText == null)
            return;

        if (statusText.transform is RectTransform rectTransform)
        {
            rectTransform.anchoredPosition = statusOriginalAnchoredPosition + new Vector2(0f, _offsetY);
            return;
        }

        statusText.transform.localPosition = statusOriginalLocalPosition + new Vector3(0f, _offsetY, 0f);
    }

    private void ClearStatusText()
    {
        if (statusText == null)
            return;

        EnsureStatusTextOriginalState();

        statusMessageTimer = 0f;
        statusText.text = string.Empty;
        statusText.color = statusOriginalColor;
        SetStatusTextOffset(0f);
        statusText.gameObject.SetActive(false);
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

    private void PlaySpecialDeliveryOpenSfxIfNeeded()
    {
        if (!ShouldPlaySpecialDeliveryOpenSfx())
            return;

        PlayDoorSfx(specialDeliveryOpenSfx, specialDeliveryOpenSfxVolume);
    }

    private bool ShouldPlaySpecialDeliveryOpenSfx()
    {
        return IsCampaignSpecialDeliveryActive() || ShouldUseTutorialPayment();
    }

    #endregion
}
