using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DockOwnerUI : MonoBehaviour
{
    private enum DockOwnerTab
    {
        Sell,
        Upgrades,
        Baits
    }

    [Header("Panel")]
    [SerializeField] private GameObject panel;
    [SerializeField] private bool closeOnAwake = true;

    [Header("Tabs")]
    [SerializeField] private Button sellTabButton;
    [SerializeField] private Button upgradesTabButton;
    [SerializeField] private Button baitsTabButton;
    [SerializeField] private GameObject sellTabPanel;
    [SerializeField] private GameObject upgradesTabPanel;
    [SerializeField] private GameObject baitsTabPanel;

    [Header("Sell")]
    [SerializeField] private TMP_Text fishListText;
    [SerializeField] private TMP_Text totalValueText;
    [SerializeField] private TMP_Text moneyText;
    [SerializeField] private Button sellAllButton;

    [Header("Upgrades")]
    [SerializeField] private TMP_Text capacityUpgradeText;
    [SerializeField] private Button capacityUpgradeButton;
    [SerializeField] private TMP_Text boatSpeedUpgradeText;
    [SerializeField] private Button boatSpeedUpgradeButton;
    [SerializeField] private TMP_Text rodUpgradeText;
    [SerializeField] private Button rodUpgradeButton;
    [SerializeField] private TMP_Text fireproofBoatUpgradeText;
    [SerializeField] private Button fireproofBoatUpgradeButton;

    [Header("Future Tabs")]
    [SerializeField] private TMP_Text upgradesPlaceholderText;
    [SerializeField] private TMP_Text baitsPlaceholderText;

    [Header("Common")]
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private Button closeButton;

    [Header("References")]
    [SerializeField] private FishMarket fishMarket;
    [SerializeField] private DockUpgradeSystem dockUpgradeSystem;
    [SerializeField] private ShipInventory shipInventory;
    [SerializeField] private PlayerMoneyManager playerMoneyManager;

    private readonly List<FishData> ownedFish = new List<FishData>();
    private DockOwnerTab currentTab = DockOwnerTab.Sell;
    private float playerMoney;
    private bool isOpen;
    private bool isSubscribed;
    private bool areButtonsBound;
    private bool isInputSubscribed;

    private GameObject PanelObject => panel != null ? panel : gameObject;

    private void Awake()
    {
        TryResolveReferences();
        EnsureUpgradeControls();
        BindButtons();

        if (closeOnAwake)
            CloseImmediate();
    }

    private void OnEnable()
    {
        TryResolveReferences();
        EnsureUpgradeControls();
        BindButtons();
        SubscribeToReferences();
        TrySubscribeInput();
        Refresh();
    }

    private void OnDisable()
    {
        UnsubscribeFromReferences();
        UnsubscribeInput();
        UnbindButtons();
    }

    public void Open(FishMarket _fishMarket)
    {
        if (_fishMarket != null)
            fishMarket = _fishMarket;

        TryResolveReferences();
        SubscribeToReferences();
        TrySubscribeInput();

        isOpen = true;
        PanelObject.SetActive(true);
        SetGameUiState(GameManager.GameState.InUI, false, true);
        SetStatus(string.Empty);
        SetTab(DockOwnerTab.Sell);
        Refresh();
    }

    public void OnClickSellTab()
    {
        SetTab(DockOwnerTab.Sell);
    }

    public void OnClickUpgradesTab()
    {
        SetTab(DockOwnerTab.Upgrades);
    }

    public void OnClickBaitsTab()
    {
        SetTab(DockOwnerTab.Baits);
    }

    public void OnClickSellAll()
    {
        if (fishMarket == null)
        {
            SetStatus("Mercado nao encontrado.");
            return;
        }

        if (!fishMarket.TrySellAllFish(out int earnedMoney))
        {
            SetStatus("Nenhum peixe no barco.");
            Refresh();
            return;
        }

        SetStatus($"Peixes vendidos: R$ {earnedMoney}.");
        Refresh();
    }

    public void OnClickBuyCapacityUpgrade()
    {
        TryBuyUpgrade(DockUpgradeType.Capacity);
    }

    public void OnClickBuyBoatSpeedUpgrade()
    {
        TryBuyUpgrade(DockUpgradeType.BoatSpeed);
    }

    public void OnClickBuyRodUpgrade()
    {
        TryBuyUpgrade(DockUpgradeType.Rod);
    }

    public void OnClickBuyFireproofBoatUpgrade()
    {
        TryBuyUpgrade(DockUpgradeType.FireproofBoat);
    }

    public void OnClickClose()
    {
        Close();
    }

    public void Refresh()
    {
        RefreshInventorySnapshot();
        SetSellTexts();
        SetUpgradeTexts();
        SetFutureTabTexts();
    }

    private void SetTab(DockOwnerTab _tab)
    {
        currentTab = _tab;

        SetObjectActive(sellTabPanel, currentTab == DockOwnerTab.Sell);
        SetObjectActive(upgradesTabPanel, currentTab == DockOwnerTab.Upgrades);
        SetObjectActive(baitsTabPanel, currentTab == DockOwnerTab.Baits);
        SetButtonInteractable(sellTabButton, currentTab != DockOwnerTab.Sell);
        SetButtonInteractable(upgradesTabButton, currentTab != DockOwnerTab.Upgrades);
        SetButtonInteractable(baitsTabButton, currentTab != DockOwnerTab.Baits);

        if (currentTab == DockOwnerTab.Upgrades)
            SetStatus(string.Empty);
        else if (currentTab == DockOwnerTab.Baits)
            SetStatus("Iscas em breve.");
        else
            SetStatus(string.Empty);

        Refresh();
    }

    private void RefreshInventorySnapshot()
    {
        ownedFish.Clear();

        if (fishMarket != null)
        {
            shipInventory = fishMarket.ShipInventory;
            playerMoneyManager = fishMarket.PlayerMoneyManager;
        }

        if (shipInventory != null)
            ownedFish.AddRange(shipInventory.OwnedFish);

        playerMoney = playerMoneyManager != null ? playerMoneyManager.PlayerMoney : 0f;
    }

    private void SetSellTexts()
    {
        if (fishListText != null)
            fishListText.text = GetFishListText();

        if (totalValueText != null)
        {
            int totalValue = fishMarket != null ? fishMarket.GetInventorySaleValue() : 0;
            string totalText = $"Total: R$ {totalValue}";
            totalValueText.text = moneyText == null
                ? $"{totalText}\nDinheiro: R$ {playerMoney:0}"
                : totalText;
        }

        if (moneyText != null)
            moneyText.text = $"Dinheiro: R$ {playerMoney:0}";
    }

    private void SetUpgradeTexts()
    {
        if (upgradesPlaceholderText != null)
            upgradesPlaceholderText.text = "Melhore peso, barco e vara.";

        if (capacityUpgradeText != null)
            capacityUpgradeText.text = GetCapacityUpgradeText();

        if (capacityUpgradeButton != null)
            capacityUpgradeButton.interactable = dockUpgradeSystem != null && dockUpgradeSystem.CanBuyCapacityUpgrade;

        if (boatSpeedUpgradeText != null)
            boatSpeedUpgradeText.text = GetBoatSpeedUpgradeText();

        if (boatSpeedUpgradeButton != null)
            boatSpeedUpgradeButton.interactable = dockUpgradeSystem != null && dockUpgradeSystem.CanBuyBoatSpeedUpgrade;

        if (rodUpgradeText != null)
            rodUpgradeText.text = GetRodUpgradeText();

        if (rodUpgradeButton != null)
            rodUpgradeButton.interactable = dockUpgradeSystem != null && dockUpgradeSystem.CanBuyRodUpgrade;

        if (fireproofBoatUpgradeText != null)
            fireproofBoatUpgradeText.text = GetFireproofBoatUpgradeText();

        if (fireproofBoatUpgradeButton != null)
            fireproofBoatUpgradeButton.interactable = dockUpgradeSystem != null && dockUpgradeSystem.CanBuyFireproofBoatUpgrade;
    }

    private string GetCapacityUpgradeText()
    {
        if (dockUpgradeSystem == null)
            return "Upgrade de capacidade indisponivel.";

        ShipInventory targetInventory = dockUpgradeSystem.ShipInventory != null
            ? dockUpgradeSystem.ShipInventory
            : shipInventory;

        float currentCapacity = targetInventory != null ? targetInventory.GetMaxCapacity() : dockUpgradeSystem.CurrentCapacity;
        int currentLevel = dockUpgradeSystem.CapacityLevel;
        int maxLevel = dockUpgradeSystem.MaxCapacityLevel;

        if (dockUpgradeSystem.IsCapacityUpgradeMaxed)
        {
            return $"Capacidade do barco\nNivel {currentLevel}/{maxLevel}\nCapacidade: {currentCapacity:0} kg\nUpgrade maximo.";
        }

        int cost = dockUpgradeSystem.CurrentCapacityUpgradeCost;
        string moneyColor = playerMoney >= cost ? "green" : "red";

        return $"Peso\nNivel {currentLevel}/{maxLevel}\nCapacidade: {currentCapacity:0} kg -> {dockUpgradeSystem.NextCapacity:0} kg\nCusto: <color={moneyColor}>R$ {cost}</color>";
    }

    private string GetBoatSpeedUpgradeText()
    {
        if (dockUpgradeSystem == null)
            return "Upgrade de barco indisponivel.";

        int currentLevel = dockUpgradeSystem.BoatSpeedLevel;
        int maxLevel = dockUpgradeSystem.MaxBoatSpeedLevel;

        if (dockUpgradeSystem.IsBoatSpeedUpgradeMaxed)
            return $"Barco\nNivel {currentLevel}/{maxLevel}\nVelocidade: +{GetPercent(dockUpgradeSystem.BoatSpeedMultiplier - 1f)}%\nUpgrade maximo.";

        int cost = dockUpgradeSystem.CurrentBoatSpeedUpgradeCost;
        string moneyColor = playerMoney >= cost ? "green" : "red";

        return $"Barco\nNivel {currentLevel}/{maxLevel}\nVelocidade: +{GetPercent(dockUpgradeSystem.BoatSpeedMultiplier - 1f)}% -> +{GetPercent(dockUpgradeSystem.NextBoatSpeedMultiplier - 1f)}%\nCusto: <color={moneyColor}>R$ {cost}</color>";
    }

    private string GetRodUpgradeText()
    {
        if (dockUpgradeSystem == null)
            return "Upgrade de vara indisponivel.";

        int currentLevel = dockUpgradeSystem.RodLevel;
        int maxLevel = dockUpgradeSystem.MaxRodLevel;

        if (dockUpgradeSystem.IsRodUpgradeMaxed)
        {
            return $"Vara\nNivel {currentLevel}/{maxLevel}\nIndicador: -{GetPercent(1f - dockUpgradeSystem.RodIndicatorSpeedMultiplier)}%\nZona: +{GetPercent(dockUpgradeSystem.RodSuccessZoneMultiplier - 1f)}%\nUpgrade maximo.";
        }

        int cost = dockUpgradeSystem.CurrentRodUpgradeCost;
        string moneyColor = playerMoney >= cost ? "green" : "red";

        return $"Vara\nNivel {currentLevel}/{maxLevel}\nIndicador: -{GetPercent(1f - dockUpgradeSystem.RodIndicatorSpeedMultiplier)}% -> -{GetPercent(1f - dockUpgradeSystem.NextRodIndicatorSpeedMultiplier)}%\nZona: +{GetPercent(dockUpgradeSystem.RodSuccessZoneMultiplier - 1f)}% -> +{GetPercent(dockUpgradeSystem.NextRodSuccessZoneMultiplier - 1f)}%\nCusto: <color={moneyColor}>R$ {cost}</color>";
    }

    private string GetFireproofBoatUpgradeText()
    {
        if (dockUpgradeSystem == null)
            return "Upgrade especial indisponivel.";

        if (dockUpgradeSystem.HasFireproofBoatUpgrade)
            return "Barco a prova de fogo\nPermite navegar na lava.\nComprado.";

        int cost = dockUpgradeSystem.FireproofBoatUpgradeCost;
        string moneyColor = playerMoney >= cost ? "green" : "red";

        return $"Barco a prova de fogo\nPermite navegar na lava.\nCusto: <color={moneyColor}>R$ {cost}</color>";
    }

    private string GetFishListText()
    {
        if (ownedFish.Count == 0)
            return "Nenhum peixe no barco.";

        StringBuilder builder = new StringBuilder();

        foreach (FishData fish in ownedFish)
        {
            if (fish == null || fish.typeOfFish == null)
                continue;

            builder.Append(fish.typeOfFish.fishName);
            builder.Append(" | ");
            builder.Append(fish.weight);
            builder.Append(" kg | R$ ");
            builder.AppendLine(FishPriceCalculator.CalculatePrice(fish).ToString());
        }

        return builder.ToString();
    }

    private void SetFutureTabTexts()
    {
        if (upgradesPlaceholderText != null)
            upgradesPlaceholderText.text = "Melhore peso, barco e vara.";

        if (baitsPlaceholderText != null)
            baitsPlaceholderText.text = "Iscas em breve.";
    }

    private void Close()
    {
        CloseImmediate();
        SetGameUiState(GameManager.GameState.OnFoot, true, false);
    }

    private void CloseImmediate()
    {
        isOpen = false;
        PanelObject.SetActive(false);
    }

    private void HandlePausePressed()
    {
        if (!isOpen)
            return;

        Close();
    }

    private void ChangeFishList(List<FishData> _fishList, float _fishWeight)
    {
        Refresh();
    }

    private void ChangeMoney(float _money)
    {
        playerMoney = _money;
        SetSellTexts();
        SetUpgradeTexts();
    }

    private void HandleSaleCompleted(int _earnedMoney)
    {
        Refresh();
    }

    private void HandleUpgradesChanged()
    {
        Refresh();
    }

    private void TryBuyUpgrade(DockUpgradeType _upgradeType)
    {
        TryResolveReferences();

        if (dockUpgradeSystem == null)
        {
            SetStatus("Sistema de upgrades nao encontrado.");
            return;
        }

        DockUpgradePurchaseResult result;
        bool success = _upgradeType switch
        {
            DockUpgradeType.Capacity => dockUpgradeSystem.TryBuyCapacityUpgrade(out result),
            DockUpgradeType.BoatSpeed => dockUpgradeSystem.TryBuyBoatSpeedUpgrade(out result),
            DockUpgradeType.Rod => dockUpgradeSystem.TryBuyRodUpgrade(out result),
            DockUpgradeType.FireproofBoat => dockUpgradeSystem.TryBuyFireproofBoatUpgrade(out result),
            _ => TryFailPurchase(out result)
        };

        SetStatus(success ? GetUpgradeSuccessText(_upgradeType) : GetUpgradePurchaseStatusText(result));
        Refresh();
    }

    private void TryResolveReferences()
    {
        if (fishMarket == null)
            fishMarket = FindFirstObjectByType<FishMarket>();

        if (dockUpgradeSystem == null)
            dockUpgradeSystem = GetComponent<DockUpgradeSystem>();

        if (dockUpgradeSystem == null)
            dockUpgradeSystem = FindFirstObjectByType<DockUpgradeSystem>();

        if (fishMarket != null)
        {
            shipInventory = fishMarket.ShipInventory;
            playerMoneyManager = fishMarket.PlayerMoneyManager;
        }

        if (shipInventory == null)
            shipInventory = FindFirstObjectByType<ShipInventory>();

        if (playerMoneyManager == null)
            playerMoneyManager = FindFirstObjectByType<PlayerMoneyManager>();
    }

    private void EnsureUpgradeControls()
    {
        if (upgradesTabPanel == null || capacityUpgradeText == null || capacityUpgradeButton == null)
            return;

        bool needsRuntimeLayout = boatSpeedUpgradeText == null ||
                                  boatSpeedUpgradeButton == null ||
                                  rodUpgradeText == null ||
                                  rodUpgradeButton == null ||
                                  fireproofBoatUpgradeText == null ||
                                  fireproofBoatUpgradeButton == null;

        if (!needsRuntimeLayout)
            return;

        Transform upgradesParent = upgradesTabPanel.transform;

        ConfigureUpgradeText(capacityUpgradeText, new Vector2(-310f, 105f));
        ConfigureUpgradeButton(capacityUpgradeButton, new Vector2(-310f, -10f));

        boatSpeedUpgradeText = EnsureUpgradeText(boatSpeedUpgradeText, "BoatSpeedUpgradeText", upgradesParent, new Vector2(310f, 105f));
        boatSpeedUpgradeButton = EnsureUpgradeButton(boatSpeedUpgradeButton, "BoatSpeedUpgradeButton", upgradesParent, new Vector2(310f, -10f));
        rodUpgradeText = EnsureUpgradeText(rodUpgradeText, "RodUpgradeText", upgradesParent, new Vector2(-310f, -155f));
        rodUpgradeButton = EnsureUpgradeButton(rodUpgradeButton, "RodUpgradeButton", upgradesParent, new Vector2(-310f, -270f));
        fireproofBoatUpgradeText = EnsureUpgradeText(fireproofBoatUpgradeText, "FireproofBoatUpgradeText", upgradesParent, new Vector2(310f, -155f));
        fireproofBoatUpgradeButton = EnsureUpgradeButton(fireproofBoatUpgradeButton, "FireproofBoatUpgradeButton", upgradesParent, new Vector2(310f, -270f));

        if (upgradesPlaceholderText != null)
        {
            RectTransform placeholderRect = upgradesPlaceholderText.rectTransform;
            placeholderRect.anchoredPosition = new Vector2(0f, 235f);
            placeholderRect.sizeDelta = new Vector2(820f, 60f);
            upgradesPlaceholderText.fontSize = 32f;
        }
    }

    private TMP_Text EnsureUpgradeText(TMP_Text _text, string _name, Transform _parent, Vector2 _position)
    {
        if (_text == null)
        {
            GameObject textObject = Instantiate(capacityUpgradeText.gameObject, _parent);
            textObject.name = _name;
            _text = textObject.GetComponent<TMP_Text>();
        }

        ConfigureUpgradeText(_text, _position);
        return _text;
    }

    private Button EnsureUpgradeButton(Button _button, string _name, Transform _parent, Vector2 _position)
    {
        if (_button == null)
        {
            GameObject buttonObject = Instantiate(capacityUpgradeButton.gameObject, _parent);
            buttonObject.name = _name;
            _button = buttonObject.GetComponent<Button>();
        }

        ConfigureUpgradeButton(_button, _position);
        SetButtonText(_button, "Comprar");
        return _button;
    }

    private void ConfigureUpgradeText(TMP_Text _text, Vector2 _position)
    {
        if (_text == null)
            return;

        RectTransform textRect = _text.rectTransform;
        textRect.anchoredPosition = _position;
        textRect.sizeDelta = new Vector2(520f, 120f);
        _text.fontSize = 28f;
        _text.alignment = TextAlignmentOptions.Center;
    }

    private void ConfigureUpgradeButton(Button _button, Vector2 _position)
    {
        if (_button == null)
            return;

        RectTransform buttonRect = _button.GetComponent<RectTransform>();
        if (buttonRect == null)
            return;

        buttonRect.anchoredPosition = _position;
        buttonRect.sizeDelta = new Vector2(220f, 58f);
    }

    private void SetButtonText(Button _button, string _text)
    {
        if (_button == null)
            return;

        TMP_Text buttonText = _button.GetComponentInChildren<TMP_Text>(true);
        if (buttonText != null)
        {
            buttonText.text = _text;
            buttonText.fontSize = 36f;
        }
    }

    private void SubscribeToReferences()
    {
        if (isSubscribed)
            return;

        if (shipInventory != null)
            shipInventory.OnFishListChange += ChangeFishList;

        if (fishMarket != null)
            fishMarket.OnSaleCompleted += HandleSaleCompleted;

        if (dockUpgradeSystem != null)
            dockUpgradeSystem.OnUpgradesChanged += HandleUpgradesChanged;

        PlayerMoneyManager.OnMoneyChangeEvent += ChangeMoney;
        isSubscribed = true;
    }

    private void UnsubscribeFromReferences()
    {
        if (!isSubscribed)
            return;

        if (shipInventory != null)
            shipInventory.OnFishListChange -= ChangeFishList;

        if (fishMarket != null)
            fishMarket.OnSaleCompleted -= HandleSaleCompleted;

        if (dockUpgradeSystem != null)
            dockUpgradeSystem.OnUpgradesChanged -= HandleUpgradesChanged;

        PlayerMoneyManager.OnMoneyChangeEvent -= ChangeMoney;
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

        if (sellTabButton != null)
            sellTabButton.onClick.AddListener(OnClickSellTab);

        if (upgradesTabButton != null)
            upgradesTabButton.onClick.AddListener(OnClickUpgradesTab);

        if (baitsTabButton != null)
            baitsTabButton.onClick.AddListener(OnClickBaitsTab);

        if (sellAllButton != null)
            sellAllButton.onClick.AddListener(OnClickSellAll);

        if (capacityUpgradeButton != null)
            capacityUpgradeButton.onClick.AddListener(OnClickBuyCapacityUpgrade);

        if (boatSpeedUpgradeButton != null)
            boatSpeedUpgradeButton.onClick.AddListener(OnClickBuyBoatSpeedUpgrade);

        if (rodUpgradeButton != null)
            rodUpgradeButton.onClick.AddListener(OnClickBuyRodUpgrade);

        if (fireproofBoatUpgradeButton != null)
            fireproofBoatUpgradeButton.onClick.AddListener(OnClickBuyFireproofBoatUpgrade);

        if (closeButton != null)
            closeButton.onClick.AddListener(OnClickClose);

        areButtonsBound = true;
    }

    private void UnbindButtons()
    {
        if (!areButtonsBound)
            return;

        if (sellTabButton != null)
            sellTabButton.onClick.RemoveListener(OnClickSellTab);

        if (upgradesTabButton != null)
            upgradesTabButton.onClick.RemoveListener(OnClickUpgradesTab);

        if (baitsTabButton != null)
            baitsTabButton.onClick.RemoveListener(OnClickBaitsTab);

        if (sellAllButton != null)
            sellAllButton.onClick.RemoveListener(OnClickSellAll);

        if (capacityUpgradeButton != null)
            capacityUpgradeButton.onClick.RemoveListener(OnClickBuyCapacityUpgrade);

        if (boatSpeedUpgradeButton != null)
            boatSpeedUpgradeButton.onClick.RemoveListener(OnClickBuyBoatSpeedUpgrade);

        if (rodUpgradeButton != null)
            rodUpgradeButton.onClick.RemoveListener(OnClickBuyRodUpgrade);

        if (fireproofBoatUpgradeButton != null)
            fireproofBoatUpgradeButton.onClick.RemoveListener(OnClickBuyFireproofBoatUpgrade);

        if (closeButton != null)
            closeButton.onClick.RemoveListener(OnClickClose);

        areButtonsBound = false;
    }

    private void SetStatus(string _message)
    {
        if (statusText != null)
            statusText.text = _message;
    }

    private void SetObjectActive(GameObject _target, bool _active)
    {
        if (_target != null)
            _target.SetActive(_active);
    }

    private void SetButtonInteractable(Button _button, bool _interactable)
    {
        if (_button != null)
            _button.interactable = _interactable;
    }

    private string GetUpgradePurchaseStatusText(DockUpgradePurchaseResult _purchaseResult)
    {
        return _purchaseResult switch
        {
            DockUpgradePurchaseResult.MissingReferences => "Sistema de upgrades incompleto.",
            DockUpgradePurchaseResult.MaxLevel => "Upgrade maximo atingido.",
            DockUpgradePurchaseResult.AlreadyOwned => "Upgrade ja comprado.",
            DockUpgradePurchaseResult.NotEnoughMoney => "Dinheiro insuficiente.",
            _ => "Nao foi possivel comprar o upgrade."
        };
    }

    private string GetUpgradeSuccessText(DockUpgradeType _upgradeType)
    {
        return _upgradeType switch
        {
            DockUpgradeType.Capacity => "Capacidade aumentada.",
            DockUpgradeType.BoatSpeed => "Velocidade do barco aumentada.",
            DockUpgradeType.Rod => "Vara melhorada.",
            DockUpgradeType.FireproofBoat => "Barco a prova de fogo comprado.",
            _ => "Upgrade comprado."
        };
    }

    private bool TryFailPurchase(out DockUpgradePurchaseResult _result)
    {
        _result = DockUpgradePurchaseResult.MissingReferences;
        return false;
    }

    private int GetPercent(float _value)
    {
        return Mathf.RoundToInt(_value * 100f);
    }

    private void SetGameUiState(GameManager.GameState _state, bool _lockCursor, bool _showCursor)
    {
        if (GameManager.instance != null)
            GameManager.instance.SetState(_state);

        Cursor.lockState = _lockCursor ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = _showCursor;
    }
}
