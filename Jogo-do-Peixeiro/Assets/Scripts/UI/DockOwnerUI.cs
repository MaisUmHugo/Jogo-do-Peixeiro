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

    [Header("Modal")]
    [SerializeField] private bool pauseTimeWhileOpen = true;
    [SerializeField] private bool hideHudWhileOpen = true;
    [SerializeField] private bool blockPauseWhileOpen = true;

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

    [Header("Baits")]
    [SerializeField] private TMP_Text[] baitTexts;
    [SerializeField] private Button[] baitBuyButtons;

    [Header("Common")]
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private Button closeButton;

    [Header("Navigation")]
    [SerializeField] private Selectable sellFirstSelected;
    [SerializeField] private Selectable upgradesFirstSelected;
    [SerializeField] private Selectable baitsFirstSelected;

    [Header("Runtime Fallback")]
    [SerializeField] private bool allowRuntimeFallback;
    [SerializeField] private bool logMissingReferences = true;

    [Header("References")]
    [SerializeField] private FishMarket fishMarket;
    [SerializeField] private DockUpgradeSystem dockUpgradeSystem;
    [SerializeField] private BaitShop baitShop;
    [SerializeField] private BaitInventory baitInventory;
    [SerializeField] private ShipInventory shipInventory;
    [SerializeField] private PlayerMoneyManager playerMoneyManager;

    private readonly List<FishData> ownedFish = new List<FishData>();
    private DockOwnerTab currentTab = DockOwnerTab.Sell;
    private float playerMoney;
    private bool isOpen;
    private bool isSubscribed;
    private bool areButtonsBound;
    private bool isInputSubscribed;
    private bool hasLoggedMissingUpgradeControls;
    private bool hasLoggedMissingBaitControls;
    private int modalToken = UIModalManager.InvalidToken;

    private GameObject PanelObject => panel != null ? panel : gameObject;

    private void Awake()
    {
        TryResolveReferences();
        EnsureUpgradeControls();
        EnsureBaitControls();
        BindButtons();

        if (closeOnAwake)
            CloseImmediate();
    }

    private void OnEnable()
    {
        TryResolveReferences();
        EnsureUpgradeControls();
        EnsureBaitControls();
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
        UIModalManager.PopModal(ref modalToken);
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
        PushModalState();
        SetGameUiState(GameManager.GameState.InUI, false, true);
        SetStatus(string.Empty);
        SetTab(DockOwnerTab.Sell);
        Refresh();
        SelectCurrentTabControl();
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

        if (isOpen && PanelObject.activeInHierarchy)
            SelectCurrentTabControl();
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

    public void OnClickBuyBaitSlot0()
    {
        TryBuyBaitSlot(0);
    }

    public void OnClickBuyBaitSlot1()
    {
        TryBuyBaitSlot(1);
    }

    public void OnClickBuyBaitSlot2()
    {
        TryBuyBaitSlot(2);
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
        SetBaitTexts();
        EnsureCurrentSelectionIsUsable();
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
            SetStatus(string.Empty);
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

        if (baitShop != null)
        {
            baitInventory = baitShop.BaitInventory;
            playerMoneyManager = baitShop.PlayerMoneyManager;
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

    private void SetBaitTexts()
    {
        if (baitsPlaceholderText != null)
            baitsPlaceholderText.text = "Compre iscas por unidade.";

        BaitData[] baits = GetBaitsForSale();

        if (baits.Length == 0)
        {
            if (baitsPlaceholderText != null)
                baitsPlaceholderText.text = "Nenhuma isca configurada.";

            SetBaitControlsActive(0);
            return;
        }

        int visibleCount = Mathf.Min(baits.Length, GetBaitControlCount());
        SetBaitControlsActive(visibleCount);

        for (int i = 0; i < visibleCount; i++)
        {
            BaitData bait = baits[i];

            if (baitTexts != null && i < baitTexts.Length && baitTexts[i] != null)
                baitTexts[i].text = GetBaitText(bait);

            if (baitBuyButtons != null && i < baitBuyButtons.Length && baitBuyButtons[i] != null)
                baitBuyButtons[i].interactable = baitShop != null && baitShop.CanBuyBait(bait);
        }
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

    private string GetBaitText(BaitData _bait)
    {
        if (_bait == null)
            return "Isca indisponivel.";

        int ownedQuantity = baitInventory != null ? baitInventory.GetQuantity(_bait) : 0;
        bool isEquipped = baitInventory != null &&
                          baitInventory.EquippedBait != null &&
                          BaitCatalog.BaitIdMatches(_bait, baitInventory.EquippedBait.SaveId);

        int cost = baitShop != null ? baitShop.GetBaitPurchaseCost(_bait) : _bait.PurchasePrice;
        string moneyColor = playerMoney >= cost ? "green" : "red";
        string equippedText = isEquipped ? "\nEquipada" : string.Empty;
        string description = string.IsNullOrWhiteSpace(_bait.Description) ? "Bonus de pesca." : _bait.Description;

        return $"{_bait.BaitName}\n{description}\nQtd: {ownedQuantity}\nCusto: <color={moneyColor}>R$ {cost}</color>{equippedText}";
    }

    private BaitData[] GetBaitsForSale()
    {
        return baitShop != null ? baitShop.BaitsForSale : BaitCatalog.GetDefaultBaits();
    }

    private int GetBaitControlCount()
    {
        if (baitTexts == null || baitBuyButtons == null)
            return 0;

        return Mathf.Min(baitTexts.Length, baitBuyButtons.Length);
    }

    private void SetBaitControlsActive(int _visibleCount)
    {
        if (baitTexts != null)
        {
            for (int i = 0; i < baitTexts.Length; i++)
            {
                if (baitTexts[i] != null)
                    baitTexts[i].gameObject.SetActive(i < _visibleCount);
            }
        }

        if (baitBuyButtons != null)
        {
            for (int i = 0; i < baitBuyButtons.Length; i++)
            {
                if (baitBuyButtons[i] != null)
                    baitBuyButtons[i].gameObject.SetActive(i < _visibleCount);
            }
        }
    }

    private void Close()
    {
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
            blockPauseWhileOpen
        );

        modalToken = UIModalManager.PushModal(request);
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
        SetBaitTexts();
    }

    private void HandleSaleCompleted(int _earnedMoney)
    {
        Refresh();
    }

    private void HandleUpgradesChanged()
    {
        Refresh();
    }

    private void HandleBaitInventoryChanged()
    {
        Refresh();
    }

    private void HandleBaitPurchased(BaitData _bait, int _quantity)
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

    private void TryBuyBaitSlot(int _slotIndex)
    {
        TryResolveReferences();

        BaitData[] baits = GetBaitsForSale();

        if (_slotIndex < 0 || _slotIndex >= baits.Length)
        {
            SetStatus("Isca nao encontrada.");
            return;
        }

        if (baitShop == null)
        {
            SetStatus("Loja de iscas nao encontrada.");
            return;
        }

        BaitData bait = baits[_slotIndex];
        bool success = baitShop.TryBuyBait(bait, out BaitPurchaseResult result);
        SetStatus(success ? $"{bait.BaitName} comprada." : GetBaitPurchaseStatusText(result));
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

        if (baitShop == null)
            baitShop = GetComponent<BaitShop>();

        if (baitShop == null)
            baitShop = FindFirstObjectByType<BaitShop>();

        if (baitShop == null)
            baitShop = gameObject.AddComponent<BaitShop>();

        if (fishMarket != null)
        {
            shipInventory = fishMarket.ShipInventory;
            playerMoneyManager = fishMarket.PlayerMoneyManager;
        }

        if (shipInventory == null)
            shipInventory = FindFirstObjectByType<ShipInventory>();

        if (playerMoneyManager == null)
            playerMoneyManager = FindFirstObjectByType<PlayerMoneyManager>();

        if (baitInventory == null)
            baitInventory = baitShop != null ? baitShop.BaitInventory : BaitInventory.GetOrCreate();
    }

    private void EnsureUpgradeControls()
    {
        if (upgradesTabPanel == null || capacityUpgradeText == null || capacityUpgradeButton == null)
        {
            LogMissingUpgradeControls();
            return;
        }

        bool needsRuntimeLayout = boatSpeedUpgradeText == null ||
                                  boatSpeedUpgradeButton == null ||
                                  rodUpgradeText == null ||
                                  rodUpgradeButton == null ||
                                  fireproofBoatUpgradeText == null ||
                                  fireproofBoatUpgradeButton == null;

        if (!needsRuntimeLayout)
            return;

        if (!allowRuntimeFallback)
        {
            LogMissingUpgradeControls();
            return;
        }

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

    private void EnsureBaitControls()
    {
        ResolveBaitTabReferences();

        if (baitsTabPanel == null)
        {
            LogMissingBaitControls(0);
            return;
        }

        ConfigureTabPanelRectIfDefault(baitsTabPanel);

        BaitData[] baits = GetBaitsForSale();
        int targetCount = Mathf.Clamp(baits.Length, 0, 3);

        if (targetCount == 0)
            targetCount = 3;

        if (baitTexts == null || baitTexts.Length < targetCount)
        {
            TMP_Text[] resizedTexts = new TMP_Text[targetCount];

            if (baitTexts != null)
            {
                for (int i = 0; i < baitTexts.Length && i < resizedTexts.Length; i++)
                    resizedTexts[i] = baitTexts[i];
            }

            baitTexts = resizedTexts;
        }

        if (baitBuyButtons == null || baitBuyButtons.Length < targetCount)
        {
            Button[] resizedButtons = new Button[targetCount];

            if (baitBuyButtons != null)
            {
                for (int i = 0; i < baitBuyButtons.Length && i < resizedButtons.Length; i++)
                    resizedButtons[i] = baitBuyButtons[i];
            }

            baitBuyButtons = resizedButtons;
        }

        Transform baitsParent = baitsTabPanel.transform;
        ResolveExistingBaitControls(baitsParent, targetCount);

        if (HasMissingBaitControls(targetCount) && !allowRuntimeFallback)
        {
            LogMissingBaitControls(targetCount);
            return;
        }

        if (baitsPlaceholderText == null && allowRuntimeFallback)
            baitsPlaceholderText = EnsureBaitsHeader(baitsParent);

        Vector2[] textPositions =
        {
            new Vector2(-320f, 35f),
            new Vector2(0f, 35f),
            new Vector2(320f, 35f)
        };

        Vector2[] buttonPositions =
        {
            new Vector2(-320f, -165f),
            new Vector2(0f, -165f),
            new Vector2(320f, -165f)
        };

        for (int i = 0; i < targetCount; i++)
        {
            baitTexts[i] = EnsureBaitText(baitTexts[i], $"BaitItemText{i + 1}", baitsParent, textPositions[i]);
            baitBuyButtons[i] = EnsureBaitButton(baitBuyButtons[i], $"BaitBuyButton{i + 1}", baitsParent, buttonPositions[i]);
        }

        if (baitsPlaceholderText != null)
        {
            ConfigureBaitsHeaderIfDefault(baitsPlaceholderText);
        }
    }

    private void ResolveBaitTabReferences()
    {
        if (baitsTabPanel == null)
            baitsTabPanel = FindChildGameObject("BaitsTabPanel", "BaitTabPanel", "IscasTabPanel", "IscasPanel");

        if (baitsTabPanel != null && baitsPlaceholderText == null)
            baitsPlaceholderText = FindChildText(baitsTabPanel.transform, "BaitsPlaceholderText", "BaitsTitleText", "IscasTitleText", "IscasHeaderText");
    }

    private void ConfigureTabPanelRectIfDefault(GameObject _panel)
    {
        if (_panel == null)
            return;

        RectTransform panelRect = _panel.GetComponent<RectTransform>();

        if (panelRect == null)
            return;

        if (!IsDefaultSizedRect(panelRect, 120f, 120f))
            return;

        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = new Vector2(920f, 520f);
    }

    private void ResolveExistingBaitControls(Transform _parent, int _targetCount)
    {
        TMP_Text[] existingTexts = GetExistingBaitItemTexts(_parent);
        Button[] existingButtons = GetExistingBaitButtons(_parent);

        for (int i = 0; i < _targetCount; i++)
        {
            if (baitTexts[i] == null)
            {
                baitTexts[i] = FindChildText(
                    _parent,
                    $"BaitItemText{i + 1}",
                    $"BaitText{i + 1}",
                    $"IscaText{i + 1}",
                    $"IscaItemText{i + 1}"
                );
            }

            if (baitTexts[i] == null && existingTexts != null && i < existingTexts.Length)
                baitTexts[i] = existingTexts[i];

            if (baitBuyButtons[i] == null)
            {
                baitBuyButtons[i] = FindChildButton(
                    _parent,
                    $"BaitBuyButton{i + 1}",
                    $"BuyBaitButton{i + 1}",
                    $"ComprarIscaButton{i + 1}"
                );
            }

            if (baitBuyButtons[i] == null && existingButtons != null && i < existingButtons.Length)
                baitBuyButtons[i] = existingButtons[i];
        }
    }

    private bool HasMissingBaitControls(int _targetCount)
    {
        for (int i = 0; i < _targetCount; i++)
        {
            if (baitTexts == null || i >= baitTexts.Length || baitTexts[i] == null)
                return true;

            if (baitBuyButtons == null || i >= baitBuyButtons.Length || baitBuyButtons[i] == null)
                return true;
        }

        return false;
    }

    private void LogMissingUpgradeControls()
    {
        if (!logMissingReferences || hasLoggedMissingUpgradeControls)
            return;

        List<string> missingReferences = new List<string>();

        if (upgradesTabPanel == null)
            missingReferences.Add("UpgradesTabPanel");

        if (capacityUpgradeText == null)
            missingReferences.Add("CapacityUpgradeText");

        if (capacityUpgradeButton == null)
            missingReferences.Add("CapacityUpgradeButton");

        if (boatSpeedUpgradeText == null)
            missingReferences.Add("BoatSpeedUpgradeText");

        if (boatSpeedUpgradeButton == null)
            missingReferences.Add("BoatSpeedUpgradeButton");

        if (rodUpgradeText == null)
            missingReferences.Add("RodUpgradeText");

        if (rodUpgradeButton == null)
            missingReferences.Add("RodUpgradeButton");

        if (fireproofBoatUpgradeText == null)
            missingReferences.Add("FireproofBoatUpgradeText");

        if (fireproofBoatUpgradeButton == null)
            missingReferences.Add("FireproofBoatUpgradeButton");

        if (missingReferences.Count == 0)
            return;

        Debug.LogWarning($"[DockOwnerUI] Referencias de upgrades faltando: {string.Join(", ", missingReferences)}. Crie os objetos na cena/prefab ou arraste no Inspector. Ative Allow Runtime Fallback apenas se quiser cria-los em runtime.", this);
        hasLoggedMissingUpgradeControls = true;
    }

    private void LogMissingBaitControls(int _targetCount)
    {
        if (!logMissingReferences || hasLoggedMissingBaitControls)
            return;

        List<string> missingReferences = new List<string>();

        if (baitsTabPanel == null)
            missingReferences.Add("BaitsTabPanel");

        for (int i = 0; i < _targetCount; i++)
        {
            if (baitTexts == null || i >= baitTexts.Length || baitTexts[i] == null)
                missingReferences.Add($"BaitItemText{i + 1}");

            if (baitBuyButtons == null || i >= baitBuyButtons.Length || baitBuyButtons[i] == null)
                missingReferences.Add($"BaitBuyButton{i + 1}");
        }

        if (missingReferences.Count == 0)
            return;

        Debug.LogWarning($"[DockOwnerUI] Referencias de iscas faltando: {string.Join(", ", missingReferences)}. Crie os textos/botoes dentro do BaitsTabPanel ou arraste no Inspector. Ative Allow Runtime Fallback apenas se quiser cria-los em runtime.", this);
        hasLoggedMissingBaitControls = true;
    }

    private TMP_Text EnsureBaitsHeader(Transform _parent)
    {
        if (baitsPlaceholderText != null)
            return baitsPlaceholderText;

        GameObject textObject = CreateRuntimeTextObject("BaitsPlaceholderText", _parent);
        TMP_Text text = textObject.GetComponent<TMP_Text>();
        text.text = "Compre iscas por unidade.";
        return text;
    }

    private void ConfigureBaitsHeaderIfDefault(TMP_Text _text)
    {
        if (_text == null)
            return;

        RectTransform textRect = _text.rectTransform;

        if (!IsDefaultSizedRect(textRect, 120f, 80f))
            return;

        textRect.anchoredPosition = new Vector2(0f, 235f);
        textRect.sizeDelta = new Vector2(820f, 60f);
        _text.fontSize = 32f;
        _text.alignment = TextAlignmentOptions.Center;
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

    private TMP_Text EnsureBaitText(TMP_Text _text, string _name, Transform _parent, Vector2 _position)
    {
        bool wasCreated = false;

        if (_text == null)
        {
            GameObject textObject = capacityUpgradeText != null
                ? Instantiate(capacityUpgradeText.gameObject, _parent)
                : CreateRuntimeTextObject(_name, _parent);

            textObject.name = _name;
            _text = textObject.GetComponent<TMP_Text>();
            wasCreated = true;
        }

        if (wasCreated)
            ConfigureBaitText(_text, _position);

        return _text;
    }

    private Button EnsureBaitButton(Button _button, string _name, Transform _parent, Vector2 _position)
    {
        bool wasCreated = false;

        if (_button == null)
        {
            GameObject buttonObject = capacityUpgradeButton != null
                ? Instantiate(capacityUpgradeButton.gameObject, _parent)
                : CreateRuntimeButtonObject(_name, _parent);

            buttonObject.name = _name;
            _button = buttonObject.GetComponent<Button>();
            wasCreated = true;
        }

        if (wasCreated)
        {
            ConfigureBaitButton(_button, _position);
            SetButtonText(_button, "Comprar");
        }

        return _button;
    }

    private GameObject CreateRuntimeTextObject(string _name, Transform _parent)
    {
        GameObject textObject = new GameObject(_name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(_parent, false);

        TMP_Text text = textObject.GetComponent<TMP_Text>();
        text.color = Color.white;
        text.text = string.Empty;

        return textObject;
    }

    private GameObject CreateRuntimeButtonObject(string _name, Transform _parent)
    {
        GameObject buttonObject = new GameObject(_name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(_parent, false);

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.08f, 0.08f, 0.08f, 0.85f);

        GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(buttonObject.transform, false);

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        TMP_Text text = textObject.GetComponent<TMP_Text>();
        text.text = "Comprar";
        text.fontSize = 28f;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;

        return buttonObject;
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

    private void ConfigureBaitText(TMP_Text _text, Vector2 _position)
    {
        if (_text == null)
            return;

        RectTransform textRect = _text.rectTransform;
        textRect.anchoredPosition = _position;
        textRect.sizeDelta = new Vector2(300f, 190f);
        _text.fontSize = 22f;
        _text.enableAutoSizing = true;
        _text.fontSizeMin = 14f;
        _text.fontSizeMax = 22f;
        _text.alignment = TextAlignmentOptions.Center;
        _text.overflowMode = TextOverflowModes.Ellipsis;
    }

    private void ConfigureBaitButton(Button _button, Vector2 _position)
    {
        if (_button == null)
            return;

        RectTransform buttonRect = _button.GetComponent<RectTransform>();

        if (buttonRect == null)
            return;

        buttonRect.anchoredPosition = _position;
        buttonRect.sizeDelta = new Vector2(200f, 58f);
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

        if (baitInventory != null)
            baitInventory.OnBaitInventoryChanged += HandleBaitInventoryChanged;

        if (baitShop != null)
            baitShop.OnBaitPurchased += HandleBaitPurchased;

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

        if (baitInventory != null)
            baitInventory.OnBaitInventoryChanged -= HandleBaitInventoryChanged;

        if (baitShop != null)
            baitShop.OnBaitPurchased -= HandleBaitPurchased;

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

        BindBaitButton(0, OnClickBuyBaitSlot0);
        BindBaitButton(1, OnClickBuyBaitSlot1);
        BindBaitButton(2, OnClickBuyBaitSlot2);

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

        UnbindBaitButton(0, OnClickBuyBaitSlot0);
        UnbindBaitButton(1, OnClickBuyBaitSlot1);
        UnbindBaitButton(2, OnClickBuyBaitSlot2);

        if (closeButton != null)
            closeButton.onClick.RemoveListener(OnClickClose);

        areButtonsBound = false;
    }

    private void SetStatus(string _message)
    {
        if (statusText != null)
            statusText.text = _message;
    }

    private void BindBaitButton(int _index, UnityEngine.Events.UnityAction _action)
    {
        if (baitBuyButtons == null || _index < 0 || _index >= baitBuyButtons.Length || baitBuyButtons[_index] == null)
            return;

        baitBuyButtons[_index].onClick.AddListener(_action);
    }

    private void UnbindBaitButton(int _index, UnityEngine.Events.UnityAction _action)
    {
        if (baitBuyButtons == null || _index < 0 || _index >= baitBuyButtons.Length || baitBuyButtons[_index] == null)
            return;

        baitBuyButtons[_index].onClick.RemoveListener(_action);
    }

    private void SetObjectActive(GameObject _target, bool _active)
    {
        if (_target != null)
            _target.SetActive(_active);
    }

    private GameObject FindChildGameObject(params string[] _names)
    {
        if (_names == null)
            return null;

        Transform[] children = GetComponentsInChildren<Transform>(true);

        for (int i = 0; i < children.Length; i++)
        {
            Transform child = children[i];

            if (child == null)
                continue;

            for (int j = 0; j < _names.Length; j++)
            {
                if (string.Equals(child.gameObject.name, _names[j], System.StringComparison.OrdinalIgnoreCase))
                    return child.gameObject;
            }
        }

        return null;
    }

    private Button FindChildButton(Transform _root, params string[] _names)
    {
        if (_root == null || _names == null)
            return null;

        Button[] buttons = _root.GetComponentsInChildren<Button>(true);

        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i];

            if (button == null)
                continue;

            for (int j = 0; j < _names.Length; j++)
            {
                if (string.Equals(button.gameObject.name, _names[j], System.StringComparison.OrdinalIgnoreCase))
                    return button;
            }
        }

        return null;
    }

    private TMP_Text FindChildText(Transform _root, params string[] _names)
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

    private TMP_Text[] GetExistingBaitItemTexts(Transform _root)
    {
        List<TMP_Text> texts = new List<TMP_Text>();

        if (_root == null)
            return texts.ToArray();

        TMP_Text[] children = _root.GetComponentsInChildren<TMP_Text>(true);

        for (int i = 0; i < children.Length; i++)
        {
            TMP_Text text = children[i];

            if (text == null || text == baitsPlaceholderText)
                continue;

            if (text.GetComponentInParent<Button>(true) != null)
                continue;

            if (IsBaitHeaderText(text))
                continue;

            texts.Add(text);
        }

        return texts.ToArray();
    }

    private Button[] GetExistingBaitButtons(Transform _root)
    {
        if (_root == null)
            return new Button[0];

        return _root.GetComponentsInChildren<Button>(true);
    }

    private bool IsBaitHeaderText(TMP_Text _text)
    {
        if (_text == null)
            return false;

        string objectName = _text.gameObject.name;
        return objectName.IndexOf("Placeholder", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
               objectName.IndexOf("Title", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
               objectName.IndexOf("Titulo", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
               objectName.IndexOf("Header", System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private bool IsDefaultSizedRect(RectTransform _rectTransform, float _maxWidth, float _maxHeight)
    {
        if (_rectTransform == null)
            return false;

        return Mathf.Abs(_rectTransform.sizeDelta.x) <= _maxWidth &&
               Mathf.Abs(_rectTransform.sizeDelta.y) <= _maxHeight;
    }

    private void SetButtonInteractable(Button _button, bool _interactable)
    {
        if (_button != null)
            _button.interactable = _interactable;
    }

    private void SelectCurrentTabControl()
    {
        Selectable target = currentTab switch
        {
            DockOwnerTab.Sell => GetSellTabSelectable(),
            DockOwnerTab.Upgrades => GetUpgradeTabSelectable(),
            DockOwnerTab.Baits => GetBaitTabSelectable(),
            _ => closeButton
        };

        UISelectionHelper.Select(target, PanelObject);
    }

    private void EnsureCurrentSelectionIsUsable()
    {
        if (!isOpen)
            return;

        Selectable current = UISelectionHelper.CurrentSelectableInScope(PanelObject);

        if (UISelectionHelper.IsUsable(current))
            return;

        SelectCurrentTabControl();
    }

    private Selectable GetSellTabSelectable()
    {
        if (UISelectionHelper.IsUsable(sellFirstSelected))
            return sellFirstSelected;

        if (UISelectionHelper.IsUsable(sellAllButton))
            return sellAllButton;

        return closeButton;
    }

    private Selectable GetUpgradeTabSelectable()
    {
        if (UISelectionHelper.IsUsable(upgradesFirstSelected))
            return upgradesFirstSelected;

        if (UISelectionHelper.IsUsable(capacityUpgradeButton))
            return capacityUpgradeButton;

        if (UISelectionHelper.IsUsable(boatSpeedUpgradeButton))
            return boatSpeedUpgradeButton;

        if (UISelectionHelper.IsUsable(rodUpgradeButton))
            return rodUpgradeButton;

        if (UISelectionHelper.IsUsable(fireproofBoatUpgradeButton))
            return fireproofBoatUpgradeButton;

        return closeButton;
    }

    private Selectable GetBaitTabSelectable()
    {
        if (UISelectionHelper.IsUsable(baitsFirstSelected))
            return baitsFirstSelected;

        if (baitBuyButtons != null)
        {
            for (int i = 0; i < baitBuyButtons.Length; i++)
            {
                if (UISelectionHelper.IsUsable(baitBuyButtons[i]))
                    return baitBuyButtons[i];
            }
        }

        return closeButton;
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

    private string GetBaitPurchaseStatusText(BaitPurchaseResult _purchaseResult)
    {
        return _purchaseResult switch
        {
            BaitPurchaseResult.MissingReferences => "Sistema de iscas incompleto.",
            BaitPurchaseResult.InvalidBait => "Isca invalida.",
            BaitPurchaseResult.NotEnoughMoney => "Dinheiro insuficiente.",
            _ => "Nao foi possivel comprar a isca."
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
