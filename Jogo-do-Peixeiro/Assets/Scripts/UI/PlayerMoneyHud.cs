using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerMoneyHud : MonoBehaviour
{
    [SerializeField] private TMP_Text moneyText;
    [SerializeField] private TMP_Text debtText;
    [SerializeField] private TMP_Text campaignText;
    [SerializeField] private TMP_Text campaignQuestText;
    [SerializeField] private TMP_Text campaignDeadlineText;
    [SerializeField] private TMP_Text campaignGoalText;
    [SerializeField] private Image campaignDebtIcon;

    [Header("References")]
    [SerializeField] private PlayerMoneyManager playerMoneyManager;
    [SerializeField] private DebtSystem debtSystem;
    [SerializeField] private CampaignProgressSystem campaignProgress;
    [SerializeField] private DayCycle dayCycle;

    [Header("Settings")]
    [SerializeField] private bool showDebtWithMoneyWhenMissingText = true;
    [SerializeField] private bool showTotalDebtInHud;
    [SerializeField] private bool showCampaignHud = true;
    [SerializeField] private bool autoResolveCampaignHudTexts = true;
    [SerializeField] private bool useDayCycleTextColors = true;
    [SerializeField] private bool allowRuntimeFallback;
    [SerializeField] private bool logMissingReferences = true;
    [SerializeField] private string debtColor = "#D94A4A";
    [SerializeField] private string paidDebtColor = "#6CCB6C";
    [SerializeField] private string warningColor = "#F2C94C";

    private float currentMoney;
    private int currentDebt;
    private bool isCampaignSubscribed;
    private bool isDayCycleSubscribed;
    private bool isShipInventorySubscribed;
    private bool hasLoggedMissingCampaignHud;
    private ShipInventory shipInventory;

    private void OnEnable()
    {
        PlayerMoneyManager.OnMoneyChangeEvent += UpdateMoneyText;
        DebtSystem.OnDebtChangedEvent += UpdateDebtText;

        ResolveReferences();
        ResolveCampaignHudTexts();
        EnsureCampaignHud();
        SubscribeCampaignProgress();
        SubscribeDayCycleVisuals();
        SubscribeShipInventory();

        currentMoney = playerMoneyManager != null ? playerMoneyManager.PlayerMoney : 0f;
        currentDebt = debtSystem != null ? debtSystem.CurrentDebt : 0;

        RefreshHudText();
    }

    private void OnDisable()
    {
        PlayerMoneyManager.OnMoneyChangeEvent -= UpdateMoneyText;
        DebtSystem.OnDebtChangedEvent -= UpdateDebtText;
        UnsubscribeCampaignProgress();
        UnsubscribeDayCycleVisuals();
        UnsubscribeShipInventory();
    }

    private void UpdateMoneyText(float _money)
    {
        currentMoney = _money;
        RefreshHudText();
    }

    private void UpdateDebtText(int _currentDebt, int _changeAmount)
    {
        currentDebt = _currentDebt;
        RefreshHudText();
    }

    private void RefreshHudText()
    {
        string moneyLine = $"R$: {currentMoney:0}";
        string debtLine = GetDebtLine();

        if (debtText != null)
            debtText.gameObject.SetActive(showTotalDebtInHud);

        bool shouldShowDebtWithMoney = showTotalDebtInHud &&
                                       showDebtWithMoneyWhenMissingText &&
                                       (debtText == null || !debtText.gameObject.activeInHierarchy);

        if (moneyText != null)
        {
            moneyText.text = shouldShowDebtWithMoney
                ? $"{moneyLine}\n{debtLine}"
                : moneyLine;
        }

        if (debtText != null && showTotalDebtInHud)
            debtText.text = debtLine;

        if (campaignText != null)
        {
            campaignText.gameObject.SetActive(showCampaignHud);
            campaignText.text = GetCampaignLine();
        }

        SetCampaignText(campaignQuestText, GetCampaignQuestLine());
        SetCampaignText(campaignDeadlineText, GetCampaignDeadlineLine());
        SetCampaignText(campaignGoalText, GetCampaignGoalLine());
        SetCampaignDebtIconVisible(ShouldShowQuestDebtIcon());
        ApplyHudTextColors();
    }

    private string GetDebtLine()
    {
        if (currentDebt <= 0)
            return $"<color={paidDebtColor}>Dívida: R$ 0</color>";

        return $"<color={debtColor}>Dívida: -R$ {currentDebt}</color>";
    }

    private void ResolveReferences()
    {
        if (playerMoneyManager == null)
            playerMoneyManager = FindFirstObjectByType<PlayerMoneyManager>();

        if (debtSystem == null)
            debtSystem = DebtSystem.GetOrCreate();

        if (campaignProgress == null)
            campaignProgress = CampaignProgressSystem.GetOrCreate();

        if (dayCycle == null)
            dayCycle = FindFirstObjectByType<DayCycle>();

        if (shipInventory == null)
            shipInventory = FindFirstObjectByType<ShipInventory>();
    }

    private void SubscribeShipInventory()
    {
        if (isShipInventorySubscribed)
            return;

        if (shipInventory == null)
            shipInventory = FindFirstObjectByType<ShipInventory>();

        if (shipInventory == null)
            return;

        shipInventory.OnFishListChange += HandleShipInventoryChanged;
        isShipInventorySubscribed = true;
    }

    private void UnsubscribeShipInventory()
    {
        if (!isShipInventorySubscribed || shipInventory == null)
            return;

        shipInventory.OnFishListChange -= HandleShipInventoryChanged;
        isShipInventorySubscribed = false;
    }

    private void HandleShipInventoryChanged(System.Collections.Generic.List<FishData> _fishList, float _fishWeight)
    {
        RefreshHudText();
    }

    private void ResolveCampaignHudTexts()
    {
        if (!autoResolveCampaignHudTexts)
            return;

        Transform searchRoot = transform.parent != null ? transform.parent : transform;

        if (campaignQuestText == null)
            campaignQuestText = FindTextByName(searchRoot, "QuestText", "Quest Text", "CampaignQuestText", "ObjectiveQuestText", "Quest");

        if (campaignDeadlineText == null)
            campaignDeadlineText = FindTextByName(searchRoot, "PrazoText", "Prazo Text", "DeadlineText", "CampaignDeadlineText", "ObjectiveDeadlineText", "Prazo");

        if (campaignGoalText == null)
            campaignGoalText = FindTextByName(searchRoot, "MetaText", "Meta Text", "GoalText", "CampaignGoalText", "ObjectiveGoalText", "Meta");

        if (campaignDebtIcon == null)
            campaignDebtIcon = FindImageByName(searchRoot, "QuestDebtIcon", "DebtIcon", "ObjectiveDebtIcon", "MetaIcon");
    }

    private TMP_Text FindTextByName(Transform _root, params string[] _names)
    {
        if (_root == null || _names == null)
            return null;

        TMP_Text[] texts = _root.GetComponentsInChildren<TMP_Text>(true);

        for (int i = 0; i < texts.Length; i++)
        {
            TMP_Text text = texts[i];

            if (text == null)
                continue;

            for (int j = 0; j < _names.Length; j++)
            {
                if (string.Equals(text.gameObject.name, _names[j], System.StringComparison.OrdinalIgnoreCase))
                    return text;
            }
        }

        return null;
    }

    private Image FindImageByName(Transform _root, params string[] _names)
    {
        if (_root == null || _names == null)
            return null;

        Image[] images = _root.GetComponentsInChildren<Image>(true);

        for (int i = 0; i < images.Length; i++)
        {
            Image image = images[i];

            if (image == null)
                continue;

            for (int j = 0; j < _names.Length; j++)
            {
                if (string.Equals(image.gameObject.name, _names[j], System.StringComparison.OrdinalIgnoreCase))
                    return image;
            }
        }

        return null;
    }

    private void SubscribeCampaignProgress()
    {
        if (isCampaignSubscribed || campaignProgress == null)
            return;

        campaignProgress.OnProgressChanged += RefreshHudText;
        campaignProgress.OnQuestAdvanced += RefreshHudText;
        campaignProgress.OnQuestDeadlineExpired += RefreshHudText;
        campaignProgress.OnCampaignCompleted += RefreshHudText;
        isCampaignSubscribed = true;
    }

    private void UnsubscribeCampaignProgress()
    {
        if (!isCampaignSubscribed || campaignProgress == null)
            return;

        campaignProgress.OnProgressChanged -= RefreshHudText;
        campaignProgress.OnQuestAdvanced -= RefreshHudText;
        campaignProgress.OnQuestDeadlineExpired -= RefreshHudText;
        campaignProgress.OnCampaignCompleted -= RefreshHudText;
        isCampaignSubscribed = false;
    }

    private void SubscribeDayCycleVisuals()
    {
        if (!useDayCycleTextColors || isDayCycleSubscribed)
            return;

        if (dayCycle == null)
            dayCycle = FindFirstObjectByType<DayCycle>();

        if (dayCycle == null)
            return;

        dayCycle.VisualModeChanged += HandleDayCycleVisualModeChanged;
        isDayCycleSubscribed = true;
    }

    private void UnsubscribeDayCycleVisuals()
    {
        if (!isDayCycleSubscribed || dayCycle == null)
            return;

        dayCycle.VisualModeChanged -= HandleDayCycleVisualModeChanged;
        isDayCycleSubscribed = false;
    }

    private void HandleDayCycleVisualModeChanged(bool _isDayVisualMode)
    {
        RefreshHudText();
    }

    private void EnsureCampaignHud()
    {
        if (!showCampaignHud || campaignText != null || HasSeparatedCampaignTexts())
            return;

        if (!allowRuntimeFallback)
        {
            LogMissingCampaignHud();
            return;
        }

        CreateCampaignHud();
    }

    private void CreateCampaignHud()
    {
        GameObject panelObject = new GameObject("CampaignHUD", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panelObject.transform.SetParent(transform, false);

        RectTransform panelRect = panelObject.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0f, 1f);
        panelRect.anchorMax = new Vector2(0f, 1f);
        panelRect.pivot = new Vector2(0f, 1f);
        panelRect.anchoredPosition = new Vector2(24f, -24f);
        panelRect.sizeDelta = new Vector2(520f, 138f);

        Image panelImage = panelObject.GetComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0.55f);
        panelImage.raycastTarget = false;

        campaignQuestText = CreateCampaignHudText(panelObject.transform, "QuestText", new Vector2(16f, -10f), new Vector2(-16f, -48f));
        campaignDeadlineText = CreateCampaignHudText(panelObject.transform, "PrazoText", new Vector2(16f, -52f), new Vector2(-16f, -90f));
        campaignGoalText = CreateCampaignHudText(panelObject.transform, "MetaText", new Vector2(16f, -94f), new Vector2(-16f, -132f));
    }

    private TMP_Text CreateCampaignHudText(Transform _parent, string _name, Vector2 _offsetMin, Vector2 _offsetMax)
    {
        GameObject textObject = new GameObject(_name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(_parent, false);

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = _offsetMin;
        textRect.offsetMax = _offsetMax;

        TMP_Text text = textObject.GetComponent<TMP_Text>();
        text.fontSize = 24f;
        text.enableAutoSizing = true;
        text.fontSizeMin = 16f;
        text.fontSizeMax = 24f;
        text.alignment = TextAlignmentOptions.Left;
        text.color = Color.white;
        text.raycastTarget = false;
        return text;
    }

    private void LogMissingCampaignHud()
    {
        if (!logMissingReferences || hasLoggedMissingCampaignHud)
            return;

        Debug.LogWarning("[PlayerMoneyHud] Falta HUD de campanha. Crie/arraste CampaignText ou os textos QuestText, PrazoText e MetaText. Ative Allow Runtime Fallback apenas se quiser criar essa UI em runtime.", this);
        hasLoggedMissingCampaignHud = true;
    }

    private bool HasSeparatedCampaignTexts()
    {
        return campaignQuestText != null || campaignDeadlineText != null || campaignGoalText != null;
    }

    private void SetCampaignText(TMP_Text _text, string _value)
    {
        if (_text == null)
            return;

        bool hasText = showCampaignHud && !string.IsNullOrWhiteSpace(_value);
        _text.gameObject.SetActive(hasText);
        _text.text = hasText ? _value : string.Empty;
    }

    private void ApplyHudTextColors()
    {
        Color primaryColor = GetPrimaryTextColor();
        Color secondaryColor = GetSecondaryTextColor();

        SetTextColor(moneyText, secondaryColor);
        SetTextColor(debtText, secondaryColor);
        SetTextColor(campaignText, secondaryColor);
        SetTextColor(campaignQuestText, primaryColor);
        SetTextColor(campaignDeadlineText, secondaryColor);
        SetTextColor(campaignGoalText, secondaryColor);
    }

    private void SetTextColor(TMP_Text _text, Color _color)
    {
        if (_text != null)
            _text.color = _color;
    }

    private Color GetPrimaryTextColor()
    {
        return useDayCycleTextColors && dayCycle != null
            ? dayCycle.PrimaryHudTextColor
            : Color.white;
    }

    private Color GetSecondaryTextColor()
    {
        return useDayCycleTextColors && dayCycle != null
            ? dayCycle.SecondaryHudTextColor
            : Color.white;
    }

    private string GetPrimaryTextColorHex()
    {
        return useDayCycleTextColors && dayCycle != null
            ? dayCycle.PrimaryHudTextColorHex
            : warningColor;
    }

    private string GetSecondaryTextColorHex()
    {
        return useDayCycleTextColors && dayCycle != null
            ? dayCycle.SecondaryHudTextColorHex
            : "#FFFFFF";
    }

    private string GetCampaignLine()
    {
        if (!showCampaignHud || campaignProgress == null)
            return string.Empty;

        string questLine = GetCampaignQuestLine();
        string deadlineLine = GetCampaignDeadlineLine();
        string goalLine = GetCampaignGoalLine();

        if (string.IsNullOrWhiteSpace(deadlineLine))
            return $"{questLine}\n{goalLine}".Trim();

        if (string.IsNullOrWhiteSpace(goalLine))
            return $"{questLine}\n{deadlineLine}".Trim();

        return $"{questLine}\n{deadlineLine}\n{goalLine}".Trim();
    }

    private string GetCampaignQuestLine()
    {
        if (!showCampaignHud || campaignProgress == null)
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

    private string GetCampaignDeadlineLine()
    {
        if (!showCampaignHud || campaignProgress == null)
            return string.Empty;

        if (campaignProgress.IsCampaignCompleted)
            return string.Empty;

        if (campaignProgress.HasFailedCurrentQuest)
            return $"Prazo: <color={debtColor}>encerrado</color>";

        int daysRemaining = campaignProgress.DaysRemainingInCurrentQuest;
        string deadlineColor = daysRemaining <= 1 ? GetPrimaryTextColorHex() : GetSecondaryTextColorHex();
        return $"Prazo: <color={deadlineColor}>{daysRemaining} {GetDayLabel(daysRemaining)}</color>";
    }

    private string GetCampaignGoalLine()
    {
        if (!showCampaignHud || campaignProgress == null)
            return string.Empty;

        if (campaignProgress.IsCampaignCompleted)
            return $"<color={debtColor}>Nova dívida: -R$ {currentDebt}</color>";

        if (campaignProgress.HasFailedCurrentQuest)
            return "<color=#D94A4A>Meta falhou</color>";

        if (campaignProgress.CurrentQuestRequiresSpecialDelivery)
            return $"{GetQuestDebtRemainingText(campaignProgress.QuestDebtPaymentRemaining)} | {GetSpecialDeliveryProgressLine()}";

        return GetQuestDebtRemainingText(campaignProgress.QuestDebtPaymentRemaining);
    }

    private string GetQuestDebtRemainingText(int _remainingAmount)
    {
        int remainingAmount = Mathf.Max(0, _remainingAmount);
        string color = remainingAmount > 0 ? debtColor : paidDebtColor;
        string sign = remainingAmount > 0 ? "-" : string.Empty;
        return $"<color={color}>({sign}R$ {remainingAmount})</color>";
    }

    private string GetSpecialDeliveryProgressLine()
    {
        FishScriptableObject specialFish = campaignProgress != null ? campaignProgress.SpecialDeliveryFish : null;
        string fishName = GetFishDisplayName(specialFish);
        int requiredQuantity = campaignProgress != null ? Mathf.Max(0, campaignProgress.SpecialDeliveryQuantity) : 0;
        int ownedQuantity = GetOwnedSpecialDeliveryFishCount(specialFish);

        return $"Peixe objetivo: {fishName} {ownedQuantity}/{requiredQuantity}";
    }

    private int GetOwnedSpecialDeliveryFishCount(FishScriptableObject _specialFish)
    {
        if (_specialFish == null)
            return 0;

        if (shipInventory == null)
            shipInventory = FindFirstObjectByType<ShipInventory>();

        return shipInventory != null ? shipInventory.CountFish(_specialFish) : 0;
    }

    private bool ShouldShowQuestDebtIcon()
    {
        return showCampaignHud &&
               campaignProgress != null &&
               !campaignProgress.IsCampaignCompleted &&
               !campaignProgress.HasFailedCurrentQuest &&
               campaignProgress.QuestDebtPaymentTarget > 0;
    }

    private void SetCampaignDebtIconVisible(bool _visible)
    {
        if (campaignDebtIcon != null)
            campaignDebtIcon.gameObject.SetActive(_visible);
    }

    private string GetInventoryWeightLine()
    {
        if (shipInventory == null)
            shipInventory = FindFirstObjectByType<ShipInventory>();

        if (shipInventory == null)
            return string.Empty;

        float maxCapacity = shipInventory.GetMaxCapacity();

        if (maxCapacity <= 0f)
            return string.Empty;

        return $"Barco: {shipInventory.GetCurrentWeight():0.#}/{maxCapacity:0.#}kg";
    }

    private string GetFishDisplayName(FishScriptableObject _fish)
    {
        if (_fish == null)
            return "peixe especial";

        return !string.IsNullOrWhiteSpace(_fish.fishName) ? _fish.fishName : _fish.name;
    }

    private static string GetDayLabel(int _days)
    {
        return _days == 1 ? "dia" : "dias";
    }
}
