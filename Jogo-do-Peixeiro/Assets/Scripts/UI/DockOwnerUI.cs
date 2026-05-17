using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DockOwnerUI : MonoBehaviour
{
    #region Types And Fields

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
    [SerializeField] private InventoryFishSlotUI[] sellFishSlots;
    [SerializeField] private Transform sellFishGridContent;
    [SerializeField] private InventoryFishSlotUI sellFishSlotTemplate;
    [SerializeField] private bool autoCreateSellFishSlots;
    [SerializeField] private bool keepSellSelectionVisibleInScroll = true;
    [SerializeField] private ScrollRect sellFishScrollRect;
    [SerializeField, Min(0f)] private float sellSelectionScrollPadding = 24f;
    [SerializeField] private Toggle selectAllFishToggle;
    [SerializeField] private TMP_Text selectedSaleValueText;
    [SerializeField] private TMP_Text moneyText;
    [SerializeField] private Button sellSelectedButton;

    [Header("Upgrade Rows")]
    [SerializeField] private DockOwnerUpgradeRowUI capacityUpgradeRow;
    [SerializeField] private DockOwnerUpgradeRowUI boatSpeedUpgradeRow;
    [SerializeField] private DockOwnerUpgradeRowUI rodUpgradeRow;
    [SerializeField] private DockOwnerUpgradeRowUI fireproofBoatUpgradeRow;

    [Header("Bait Shop")]
    [SerializeField] private DockOwnerBaitShopUI baitShopUI;

    [Header("Purchase Confirmation")]
    [SerializeField] private DockOwnerPurchaseConfirmUI purchaseConfirmUI;

    [Header("Common")]
    [SerializeField] private TMP_Text statusText;
    [SerializeField, Min(0.1f)] private float statusMessageDuration = 1.15f;
    [SerializeField, Min(0f)] private float statusMessageRise = 22f;
    [SerializeField] private Button closeButton;

    [Header("Navigation")]
    [SerializeField] private Selectable sellFirstSelected;
    [SerializeField] private Selectable upgradesFirstSelected;
    [SerializeField] private Selectable baitsFirstSelected;

    [Header("References")]
    [SerializeField] private FishMarket fishMarket;
    [SerializeField] private DockUpgradeSystem dockUpgradeSystem;
    [SerializeField] private BaitShop baitShop;
    [SerializeField] private BaitInventory baitInventory;
    [SerializeField] private ShipInventory shipInventory;
    [SerializeField] private PlayerMoneyManager playerMoneyManager;

    private readonly List<FishData> ownedFish = new List<FishData>();
    private readonly HashSet<FishData> selectedFishForSale = new HashSet<FishData>();
    private DockOwnerTab currentTab = DockOwnerTab.Sell;
    private float playerMoney;
    private bool isOpen;
    private bool isSubscribed;
    private bool areButtonsBound;
    private bool isInputSubscribed;
    private bool suppressSelectAllFishToggle;
    private GameObject lastScrolledSellSelection;
    private Coroutine statusFeedbackRoutine;
    private bool hasStatusBasePosition;
    private Vector2 statusBasePosition;
    private int modalToken = UIModalManager.InvalidToken;

    private GameObject PanelObject => panel != null ? panel : gameObject;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        TryResolveReferences();
        BindButtons();

        if (closeOnAwake)
            CloseImmediate();
    }

    private void OnEnable()
    {
        TryResolveReferences();
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

    private void Update()
    {
        if (baitShopUI != null)
            baitShopUI.HandleMoveInput(isOpen && currentTab == DockOwnerTab.Baits);

        KeepSellSelectionVisible();
    }

    #endregion

    #region Public UI Actions

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
        OnClickSellSelected();
    }

    public void OnClickSellSelected()
    {
        if (fishMarket == null)
        {
            SetStatus("Mercado nao encontrado.");
            return;
        }

        PruneSelectedFishForSale();

        if (selectedFishForSale.Count == 0)
        {
            SetStatus("Selecione pelo menos um peixe.");
            Refresh();
            return;
        }

        int selectedCount = selectedFishForSale.Count;
        int selectedValue = GetSelectedFishSaleValue();
        OpenPurchaseConfirmation(
            "Confirmar venda",
            $"Vender {selectedCount} peixe(s) por R$ {selectedValue}?",
            CompleteSellSelected
        );
    }

    private void CompleteSellSelected()
    {
        if (fishMarket == null)
        {
            SetStatus("Mercado nao encontrado.");
            return;
        }

        PruneSelectedFishForSale();

        if (selectedFishForSale.Count == 0)
        {
            SetStatus("Selecione pelo menos um peixe.");
            Refresh();
            return;
        }

        if (!fishMarket.TrySellFishList(selectedFishForSale, out int earnedMoney))
        {
            SetStatus("Nao foi possivel vender os peixes selecionados.");
            Refresh();
            return;
        }

        selectedFishForSale.Clear();
        SetStatus($"Peixes vendidos: R$ {earnedMoney}.");
        Refresh();
        SelectCurrentTabControl();
    }

    public void OnSelectAllFishChanged(bool _isOn)
    {
        if (suppressSelectAllFishToggle)
            return;

        selectedFishForSale.Clear();

        if (_isOn)
        {
            for (int i = 0; i < ownedFish.Count; i++)
            {
                if (ownedFish[i] != null)
                    selectedFishForSale.Add(ownedFish[i]);
            }
        }

        SetSellUI();
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
        if (baitShopUI != null)
            baitShopUI.TryBuySlot(0);
    }

    public void OnClickBuyBaitSlot1()
    {
        if (baitShopUI != null)
            baitShopUI.TryBuySlot(1);
    }

    public void OnClickBuyBaitSlot2()
    {
        if (baitShopUI != null)
            baitShopUI.TryBuySlot(2);
    }

    public void OnClickBuyBaitSlot3()
    {
        if (baitShopUI != null)
            baitShopUI.TryBuySlot(3);
    }

    public void OnClickIncreasePendingBaitQuantity()
    {
        if (baitShopUI != null)
            baitShopUI.ChangePendingQuantity(1);
    }

    public void OnClickDecreasePendingBaitQuantity()
    {
        if (baitShopUI != null)
            baitShopUI.ChangePendingQuantity(-1);
    }

    public void OnClickConfirmBaitQuantity()
    {
        if (baitShopUI != null)
            baitShopUI.ConfirmPendingQuantity();
    }

    public void OnClickCancelBaitQuantity()
    {
        if (baitShopUI != null)
            baitShopUI.CancelPendingQuantity();
    }

    public void OnClickConfirmPurchase()
    {
        if (purchaseConfirmUI != null)
            purchaseConfirmUI.Confirm();
    }

    public void OnClickCancelPurchase()
    {
        if (purchaseConfirmUI != null)
            purchaseConfirmUI.Close(true);
    }

    public void OnClickIncreaseBaitSlot0()
    {
        if (baitShopUI != null)
            baitShopUI.ChangeSlotQuantity(0, 1);
    }

    public void OnClickDecreaseBaitSlot0()
    {
        if (baitShopUI != null)
            baitShopUI.ChangeSlotQuantity(0, -1);
    }

    public void OnClickIncreaseBaitSlot1()
    {
        if (baitShopUI != null)
            baitShopUI.ChangeSlotQuantity(1, 1);
    }

    public void OnClickDecreaseBaitSlot1()
    {
        if (baitShopUI != null)
            baitShopUI.ChangeSlotQuantity(1, -1);
    }

    public void OnClickIncreaseBaitSlot2()
    {
        if (baitShopUI != null)
            baitShopUI.ChangeSlotQuantity(2, 1);
    }

    public void OnClickDecreaseBaitSlot2()
    {
        if (baitShopUI != null)
            baitShopUI.ChangeSlotQuantity(2, -1);
    }

    public void OnClickIncreaseBaitSlot3()
    {
        if (baitShopUI != null)
            baitShopUI.ChangeSlotQuantity(3, 1);
    }

    public void OnClickDecreaseBaitSlot3()
    {
        if (baitShopUI != null)
            baitShopUI.ChangeSlotQuantity(3, -1);
    }

    public void OnClickClose()
    {
        Close();
    }

    public void Refresh()
    {
        RefreshInventorySnapshot();
        SetSellUI();
        SetUpgradeUI();
        SetBaitUI();
        EnsureCurrentSelectionIsUsable();
    }

    #endregion

    #region Tab Refresh

    private void SetTab(DockOwnerTab _tab)
    {
        if (baitShopUI != null)
            baitShopUI.ClosePopup(false);

        if (purchaseConfirmUI != null)
            purchaseConfirmUI.Close(false);

        currentTab = _tab;

        SetObjectActive(sellTabPanel, currentTab == DockOwnerTab.Sell);
        SetObjectActive(upgradesTabPanel, currentTab == DockOwnerTab.Upgrades);
        SetObjectActive(baitsTabPanel, currentTab == DockOwnerTab.Baits);
        SetButtonInteractable(sellTabButton, currentTab != DockOwnerTab.Sell);
        SetButtonInteractable(upgradesTabButton, currentTab != DockOwnerTab.Upgrades);
        SetButtonInteractable(baitsTabButton, currentTab != DockOwnerTab.Baits);
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

            if (playerMoneyManager == null)
                playerMoneyManager = baitShop.PlayerMoneyManager;
        }

        if (shipInventory != null)
            ownedFish.AddRange(shipInventory.OwnedFish);

        playerMoney = playerMoneyManager != null ? playerMoneyManager.PlayerMoney : 0f;
    }

    private void SetSellUI()
    {
        PruneSelectedFishForSale();
        RefreshSellFishSlots();

        if (selectedSaleValueText != null)
            selectedSaleValueText.text = $"Selecionado: R$ {GetSelectedFishSaleValue()}";

        if (moneyText != null)
            moneyText.text = $"Dinheiro: R$ {playerMoney:0}";

        if (sellSelectedButton != null)
            sellSelectedButton.interactable = selectedFishForSale.Count > 0;

        SetSelectAllFishToggleState();
    }

    private void SetUpgradeUI()
    {
        if (dockUpgradeSystem == null)
            return;

        ShipInventory targetInventory = dockUpgradeSystem.ShipInventory != null
            ? dockUpgradeSystem.ShipInventory
            : shipInventory;

        float currentCapacity = targetInventory != null ? targetInventory.GetMaxCapacity() : dockUpgradeSystem.CurrentCapacity;

        SetUpgradeRow(
            capacityUpgradeRow,
            "Peso",
            "Aumenta o peso maximo do barco.",
            $"Nivel {dockUpgradeSystem.CapacityLevel}/{dockUpgradeSystem.MaxCapacityLevel}",
            dockUpgradeSystem.IsCapacityUpgradeMaxed
                ? $"Capacidade: {currentCapacity:0} kg"
                : $"{currentCapacity:0} kg -> {dockUpgradeSystem.NextCapacity:0} kg",
            GetUpgradeCostText(dockUpgradeSystem.CurrentCapacityUpgradeCost, dockUpgradeSystem.IsCapacityUpgradeMaxed),
            dockUpgradeSystem.CapacityLevel,
            dockUpgradeSystem.MaxCapacityLevel,
            dockUpgradeSystem.CanBuyCapacityUpgrade
        );

        SetUpgradeRow(
            boatSpeedUpgradeRow,
            "Barco",
            "Aumenta a velocidade do barco em 15%.",
            $"Nivel {dockUpgradeSystem.BoatSpeedLevel}/{dockUpgradeSystem.MaxBoatSpeedLevel}",
            dockUpgradeSystem.IsBoatSpeedUpgradeMaxed
                ? $"Velocidade: +{GetPercent(dockUpgradeSystem.BoatSpeedMultiplier - 1f)}%"
                : $"+{GetPercent(dockUpgradeSystem.BoatSpeedMultiplier - 1f)}% -> +{GetPercent(dockUpgradeSystem.NextBoatSpeedMultiplier - 1f)}%",
            GetUpgradeCostText(dockUpgradeSystem.CurrentBoatSpeedUpgradeCost, dockUpgradeSystem.IsBoatSpeedUpgradeMaxed),
            dockUpgradeSystem.BoatSpeedLevel,
            dockUpgradeSystem.MaxBoatSpeedLevel,
            dockUpgradeSystem.CanBuyBoatSpeedUpgrade
        );

        SetUpgradeRow(
            rodUpgradeRow,
            "Vara",
            "Facilita o skill check da pescaria.",
            $"Nivel {dockUpgradeSystem.RodLevel}/{dockUpgradeSystem.MaxRodLevel}",
            dockUpgradeSystem.IsRodUpgradeMaxed
                ? $"Indicador -{GetPercent(1f - dockUpgradeSystem.RodIndicatorSpeedMultiplier)}% | Zona +{GetPercent(dockUpgradeSystem.RodSuccessZoneMultiplier - 1f)}%"
                : $"Indicador -{GetPercent(1f - dockUpgradeSystem.NextRodIndicatorSpeedMultiplier)}% | Zona +{GetPercent(dockUpgradeSystem.NextRodSuccessZoneMultiplier - 1f)}%",
            GetUpgradeCostText(dockUpgradeSystem.CurrentRodUpgradeCost, dockUpgradeSystem.IsRodUpgradeMaxed),
            dockUpgradeSystem.RodLevel,
            dockUpgradeSystem.MaxRodLevel,
            dockUpgradeSystem.CanBuyRodUpgrade
        );

        SetUpgradeRow(
            fireproofBoatUpgradeRow,
            "Barco a prova de fogo",
            "Permite navegar pelo lago de lava.",
            dockUpgradeSystem.HasFireproofBoatUpgrade ? "Comprado" : "Especial",
            dockUpgradeSystem.HasFireproofBoatUpgrade ? "Liberado" : "Necessario para a lava",
            GetUpgradeCostText(dockUpgradeSystem.FireproofBoatUpgradeCost, dockUpgradeSystem.HasFireproofBoatUpgrade),
            dockUpgradeSystem.HasFireproofBoatUpgrade ? 1 : 0,
            1,
            dockUpgradeSystem.CanBuyFireproofBoatUpgrade
        );
    }

    private void SetBaitUI()
    {
        ConfigureBaitShopUI();

        if (baitShopUI != null)
            baitShopUI.Refresh(playerMoney);
    }

    #endregion

    #region Sell

    private void RefreshSellFishSlots()
    {
        ResolveSellFishSlots();
        EnsureSellFishSlotCapacity(ownedFish.Count);

        if (sellFishSlots == null || sellFishSlots.Length == 0)
            return;

        for (int i = 0; i < sellFishSlots.Length; i++)
        {
            InventoryFishSlotUI slot = sellFishSlots[i];

            if (slot == null)
                continue;

            bool hasFish = i < ownedFish.Count && ownedFish[i] != null;
            slot.gameObject.SetActive(hasFish);

            if (hasFish)
            {
                FishData fish = ownedFish[i];
                slot.SetFish(fish, i, HandleSellFishSlotSubmitted, true, selectedFishForSale.Contains(fish));
            }
            else
            {
                slot.Clear();
            }
        }
    }

    private void ResolveSellFishSlots()
    {
        if (sellFishSlots != null && sellFishSlots.Length > 0)
            return;

        if (sellFishGridContent == null)
            return;

        InventoryFishSlotUI[] foundSlots = sellFishGridContent.GetComponentsInChildren<InventoryFishSlotUI>(true);
        List<InventoryFishSlotUI> usableSlots = new List<InventoryFishSlotUI>();

        for (int i = 0; i < foundSlots.Length; i++)
        {
            if (foundSlots[i] != null)
                usableSlots.Add(foundSlots[i]);
        }

        sellFishSlots = usableSlots.ToArray();

        if (sellFishSlotTemplate == null && sellFishSlots != null && sellFishSlots.Length > 0)
            sellFishSlotTemplate = sellFishSlots[0];
    }

    private void EnsureSellFishSlotCapacity(int _requiredCount)
    {
        if (_requiredCount <= 0)
            return;

        if (sellFishGridContent == null)
            return;

        ResolveSellFishSlots();

        if (sellFishSlotTemplate == null && sellFishSlots != null && sellFishSlots.Length > 0)
            sellFishSlotTemplate = sellFishSlots[0];

        if (sellFishSlotTemplate == null)
            return;

        bool shouldAutoCreate = autoCreateSellFishSlots ||
                                sellFishSlots == null ||
                                sellFishSlots.Length <= 1;

        if (!shouldAutoCreate)
            return;

        List<InventoryFishSlotUI> slots = new List<InventoryFishSlotUI>();

        if (sellFishSlots != null)
        {
            for (int i = 0; i < sellFishSlots.Length; i++)
            {
                if (sellFishSlots[i] != null && !slots.Contains(sellFishSlots[i]))
                    slots.Add(sellFishSlots[i]);
            }
        }

        while (slots.Count < _requiredCount)
        {
            InventoryFishSlotUI slot = Instantiate(sellFishSlotTemplate, sellFishGridContent);
            slot.gameObject.name = $"SellFishSlot{slots.Count + 1}";
            slot.gameObject.SetActive(true);
            slots.Add(slot);
        }

        sellFishSlots = slots.ToArray();
    }

    private void ClearExtraSellFishSlots(int _firstExtraIndex)
    {
        if (sellFishSlots == null)
            return;

        for (int i = _firstExtraIndex; i < sellFishSlots.Length; i++)
        {
            InventoryFishSlotUI slot = sellFishSlots[i];

            if (slot == null)
                continue;

            slot.Clear();

            if (autoCreateSellFishSlots)
            {
                slot.gameObject.SetActive(false);
            }
        }
    }

    private void HandleSellFishSlotSubmitted(int _index)
    {
        if (_index < 0 || _index >= ownedFish.Count)
            return;

        FishData fish = ownedFish[_index];

        if (fish == null)
            return;

        if (selectedFishForSale.Contains(fish))
            selectedFishForSale.Remove(fish);
        else
            selectedFishForSale.Add(fish);

        SetSellUI();
    }

    private void PruneSelectedFishForSale()
    {
        if (selectedFishForSale.Count == 0)
            return;

        List<FishData> toRemove = new List<FishData>();

        foreach (FishData fish in selectedFishForSale)
        {
            if (fish == null || !ownedFish.Contains(fish))
                toRemove.Add(fish);
        }

        for (int i = 0; i < toRemove.Count; i++)
            selectedFishForSale.Remove(toRemove[i]);
    }

    private int GetSelectedFishSaleValue()
    {
        int totalValue = 0;

        foreach (FishData fish in selectedFishForSale)
        {
            if (fish != null)
                totalValue += FishPriceCalculator.CalculatePrice(fish);
        }

        return totalValue;
    }

    private void SetSelectAllFishToggleState()
    {
        if (selectAllFishToggle == null)
            return;

        suppressSelectAllFishToggle = true;
        selectAllFishToggle.SetIsOnWithoutNotify(ownedFish.Count > 0 && selectedFishForSale.Count == ownedFish.Count);
        selectAllFishToggle.interactable = ownedFish.Count > 0;
        suppressSelectAllFishToggle = false;
    }

    private void KeepSellSelectionVisible()
    {
        if (!keepSellSelectionVisibleInScroll ||
            !isOpen ||
            currentTab != DockOwnerTab.Sell ||
            EventSystem.current == null)
        {
            lastScrolledSellSelection = null;
            return;
        }

        ResolveSellFishScrollRect();

        if (sellFishScrollRect == null || sellFishScrollRect.content == null)
            return;

        GameObject selectedObject = EventSystem.current.currentSelectedGameObject;

        if (selectedObject == null || !selectedObject.transform.IsChildOf(sellFishScrollRect.transform))
        {
            lastScrolledSellSelection = null;
            return;
        }

        RectTransform selectedRect = selectedObject.GetComponent<RectTransform>();

        if (selectedRect == null || !selectedRect.IsChildOf(sellFishScrollRect.content))
            return;

        Canvas.ForceUpdateCanvases();
        ScrollRectToChild(sellFishScrollRect, selectedRect, selectedObject != lastScrolledSellSelection);
        lastScrolledSellSelection = selectedObject;
    }

    private void ScrollRectToChild(ScrollRect _scrollRect, RectTransform _child, bool _selectionChanged)
    {
        RectTransform viewport = _scrollRect.viewport != null ? _scrollRect.viewport : _scrollRect.GetComponent<RectTransform>();
        RectTransform content = _scrollRect.content;

        if (viewport == null || content == null || _child == null)
            return;

        if (_scrollRect.vertical)
            ScrollVerticallyToChild(_scrollRect, viewport, content, _child, _selectionChanged);

        if (_scrollRect.horizontal)
            ScrollHorizontallyToChild(_scrollRect, viewport, content, _child, _selectionChanged);
    }

    private void ScrollVerticallyToChild(ScrollRect _scrollRect, RectTransform _viewport, RectTransform _content, RectTransform _child, bool _selectionChanged)
    {
        float hiddenHeight = _content.rect.height - _viewport.rect.height;

        if (hiddenHeight <= 0f)
            return;

        Bounds childBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(_viewport, _child);
        Rect viewportRect = _viewport.rect;
        float offset = 0f;

        if (childBounds.max.y > viewportRect.yMax - sellSelectionScrollPadding)
            offset = childBounds.max.y - (viewportRect.yMax - sellSelectionScrollPadding);
        else if (childBounds.min.y < viewportRect.yMin + sellSelectionScrollPadding)
            offset = childBounds.min.y - (viewportRect.yMin + sellSelectionScrollPadding);

        if (Mathf.Abs(offset) <= 0.01f)
            return;

        float target = Mathf.Clamp01(_scrollRect.verticalNormalizedPosition + offset / hiddenHeight);
        _scrollRect.verticalNormalizedPosition = _selectionChanged
            ? target
            : Mathf.MoveTowards(_scrollRect.verticalNormalizedPosition, target, Time.unscaledDeltaTime * 12f);
    }

    private void ScrollHorizontallyToChild(ScrollRect _scrollRect, RectTransform _viewport, RectTransform _content, RectTransform _child, bool _selectionChanged)
    {
        float hiddenWidth = _content.rect.width - _viewport.rect.width;

        if (hiddenWidth <= 0f)
            return;

        Bounds childBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(_viewport, _child);
        Rect viewportRect = _viewport.rect;
        float offset = 0f;

        if (childBounds.max.x > viewportRect.xMax - sellSelectionScrollPadding)
            offset = childBounds.max.x - (viewportRect.xMax - sellSelectionScrollPadding);
        else if (childBounds.min.x < viewportRect.xMin + sellSelectionScrollPadding)
            offset = childBounds.min.x - (viewportRect.xMin + sellSelectionScrollPadding);

        if (Mathf.Abs(offset) <= 0.01f)
            return;

        float target = Mathf.Clamp01(_scrollRect.horizontalNormalizedPosition + offset / hiddenWidth);
        _scrollRect.horizontalNormalizedPosition = _selectionChanged
            ? target
            : Mathf.MoveTowards(_scrollRect.horizontalNormalizedPosition, target, Time.unscaledDeltaTime * 12f);
    }

    #endregion

    #region Upgrades

    private void TryBuyUpgrade(DockUpgradeType _upgradeType)
    {
        TryResolveReferences();

        if (dockUpgradeSystem == null)
        {
            SetStatus("Sistema de upgrades nao encontrado.");
            return;
        }

        int cost = GetUpgradeCost(_upgradeType);

        if (cost <= 0 || !CanBuyUpgrade(_upgradeType))
        {
            DockUpgradePurchaseResult previewResult = GetUpgradePreviewFailure(_upgradeType);
            SetStatus(GetUpgradePurchaseStatusText(previewResult));
            Refresh();
            return;
        }

        string upgradeName = GetUpgradeDisplayName(_upgradeType);
        OpenPurchaseConfirmation(
            "Confirmar upgrade",
            $"Comprar {upgradeName} por R$ {cost}?",
            () => CompleteBuyUpgrade(_upgradeType)
        );
    }

    private void CompleteBuyUpgrade(DockUpgradeType _upgradeType)
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

    private int GetUpgradeCost(DockUpgradeType _upgradeType)
    {
        if (dockUpgradeSystem == null)
            return 0;

        return _upgradeType switch
        {
            DockUpgradeType.Capacity => dockUpgradeSystem.CurrentCapacityUpgradeCost,
            DockUpgradeType.BoatSpeed => dockUpgradeSystem.CurrentBoatSpeedUpgradeCost,
            DockUpgradeType.Rod => dockUpgradeSystem.CurrentRodUpgradeCost,
            DockUpgradeType.FireproofBoat => dockUpgradeSystem.FireproofBoatUpgradeCost,
            _ => 0
        };
    }

    private bool CanBuyUpgrade(DockUpgradeType _upgradeType)
    {
        if (dockUpgradeSystem == null)
            return false;

        return _upgradeType switch
        {
            DockUpgradeType.Capacity => dockUpgradeSystem.CanBuyCapacityUpgrade,
            DockUpgradeType.BoatSpeed => dockUpgradeSystem.CanBuyBoatSpeedUpgrade,
            DockUpgradeType.Rod => dockUpgradeSystem.CanBuyRodUpgrade,
            DockUpgradeType.FireproofBoat => dockUpgradeSystem.CanBuyFireproofBoatUpgrade,
            _ => false
        };
    }

    private DockUpgradePurchaseResult GetUpgradePreviewFailure(DockUpgradeType _upgradeType)
    {
        if (dockUpgradeSystem == null)
            return DockUpgradePurchaseResult.MissingReferences;

        bool isMaxed = _upgradeType switch
        {
            DockUpgradeType.Capacity => dockUpgradeSystem.IsCapacityUpgradeMaxed,
            DockUpgradeType.BoatSpeed => dockUpgradeSystem.IsBoatSpeedUpgradeMaxed,
            DockUpgradeType.Rod => dockUpgradeSystem.IsRodUpgradeMaxed,
            DockUpgradeType.FireproofBoat => dockUpgradeSystem.HasFireproofBoatUpgrade,
            _ => true
        };

        if (isMaxed)
            return _upgradeType == DockUpgradeType.FireproofBoat
                ? DockUpgradePurchaseResult.AlreadyOwned
                : DockUpgradePurchaseResult.MaxLevel;

        int cost = GetUpgradeCost(_upgradeType);
        return playerMoney >= cost
            ? DockUpgradePurchaseResult.MissingReferences
            : DockUpgradePurchaseResult.NotEnoughMoney;
    }

    private void SetUpgradeRow(
        DockOwnerUpgradeRowUI _row,
        string _name,
        string _description,
        string _level,
        string _value,
        string _cost,
        int _currentLevel,
        int _maxLevel,
        bool _canBuy)
    {
        if (_row == null)
            return;

        _row.SetUpgrade(_name, _description, _level, _value, _cost, _currentLevel, _maxLevel, _canBuy);
    }

    private string GetUpgradeCostText(int _cost, bool _isMaxed)
    {
        if (_isMaxed)
            return "Maximo";

        string moneyColor = playerMoney >= _cost ? "green" : "red";
        return $"<color={moneyColor}>R$ {_cost}</color>";
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

    private string GetUpgradeDisplayName(DockUpgradeType _upgradeType)
    {
        return _upgradeType switch
        {
            DockUpgradeType.Capacity => "Peso",
            DockUpgradeType.BoatSpeed => "Barco",
            DockUpgradeType.Rod => "Vara",
            DockUpgradeType.FireproofBoat => "Barco a prova de fogo",
            _ => "Upgrade"
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

    #endregion

    #region Purchase Confirmation

    private void OpenPurchaseConfirmation(string _title, string _message, System.Action _purchaseAction)
    {
        if (purchaseConfirmUI == null)
        {
            _purchaseAction?.Invoke();
            return;
        }

        purchaseConfirmUI.Open(_title, _message, _purchaseAction, PanelObject);
    }

    #endregion

    #region Panel State

    private void Close()
    {
        CloseImmediate();
        SetGameUiState(GameManager.GameState.OnFoot, true, false);
    }

    private void CloseImmediate()
    {
        UISelectionHelper.ClearSelection(PanelObject);
        if (baitShopUI != null)
            baitShopUI.ClosePopup(false);

        if (purchaseConfirmUI != null)
            purchaseConfirmUI.Close(false);

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
            HandleBackPressed
        );

        modalToken = UIModalManager.PushModal(request);
    }

    private void HandlePausePressed()
    {
        if (!isOpen)
            return;

        HandleBackPressed();
    }

    private void HandleBackPressed()
    {
        if (purchaseConfirmUI != null && purchaseConfirmUI.TryHandleBack())
            return;

        if (baitShopUI != null && baitShopUI.TryHandleBack())
            return;

        Close();
    }

    #endregion

    #region References And Events

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

        if (fishMarket != null)
        {
            shipInventory = fishMarket.ShipInventory;
            playerMoneyManager = fishMarket.PlayerMoneyManager;
        }

        if (shipInventory == null)
            shipInventory = FindFirstObjectByType<ShipInventory>();

        if (playerMoneyManager == null)
            playerMoneyManager = FindFirstObjectByType<PlayerMoneyManager>();

        if (baitInventory == null && baitShop != null)
            baitInventory = baitShop.BaitInventory;

        if (baitShopUI == null)
            baitShopUI = GetComponentInChildren<DockOwnerBaitShopUI>(true);

        if (purchaseConfirmUI == null)
            purchaseConfirmUI = GetComponentInChildren<DockOwnerPurchaseConfirmUI>(true);

        ResolveUpgradeRows();
        ResolveSellFishScrollRect();
        ConfigureBaitShopUI();
    }

    private void ResolveSellFishScrollRect()
    {
        if (sellFishScrollRect != null)
            return;

        if (sellFishGridContent != null)
            sellFishScrollRect = sellFishGridContent.GetComponentInParent<ScrollRect>(true);

        if (sellFishScrollRect == null && sellTabPanel != null)
            sellFishScrollRect = sellTabPanel.GetComponentInChildren<ScrollRect>(true);
    }

    private void ResolveUpgradeRows()
    {
        DockOwnerUpgradeRowUI[] rows = GetComponentsInChildren<DockOwnerUpgradeRowUI>(true);

        for (int i = 0; i < rows.Length; i++)
        {
            DockOwnerUpgradeRowUI row = rows[i];

            if (row == null)
                continue;

            switch (row.UpgradeType)
            {
                case DockUpgradeType.Capacity:
                    if (capacityUpgradeRow == null)
                        capacityUpgradeRow = row;
                    break;
                case DockUpgradeType.BoatSpeed:
                    if (boatSpeedUpgradeRow == null)
                        boatSpeedUpgradeRow = row;
                    break;
                case DockUpgradeType.Rod:
                    if (rodUpgradeRow == null)
                        rodUpgradeRow = row;
                    break;
                case DockUpgradeType.FireproofBoat:
                    if (fireproofBoatUpgradeRow == null)
                        fireproofBoatUpgradeRow = row;
                    break;
            }
        }
    }

    private void ConfigureBaitShopUI()
    {
        if (baitShopUI == null)
            return;

        baitShopUI.Initialize(
            baitShop,
            baitInventory,
            playerMoneyManager,
            purchaseConfirmUI,
            PanelObject,
            SetStatus,
            Refresh
        );
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

    private void ChangeFishList(List<FishData> _fishList, float _fishWeight)
    {
        Refresh();
    }

    private void ChangeMoney(float _money)
    {
        playerMoney = _money;
        SetSellUI();
        SetUpgradeUI();
        SetBaitUI();
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

    #endregion

    #region Button Binding

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

        if (sellSelectedButton != null)
            sellSelectedButton.onClick.AddListener(OnClickSellSelected);

        if (selectAllFishToggle != null)
            selectAllFishToggle.onValueChanged.AddListener(OnSelectAllFishChanged);

        BindUpgradeButton(capacityUpgradeRow, OnClickBuyCapacityUpgrade);
        BindUpgradeButton(boatSpeedUpgradeRow, OnClickBuyBoatSpeedUpgrade);
        BindUpgradeButton(rodUpgradeRow, OnClickBuyRodUpgrade);
        BindUpgradeButton(fireproofBoatUpgradeRow, OnClickBuyFireproofBoatUpgrade);

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

        if (sellSelectedButton != null)
            sellSelectedButton.onClick.RemoveListener(OnClickSellSelected);

        if (selectAllFishToggle != null)
            selectAllFishToggle.onValueChanged.RemoveListener(OnSelectAllFishChanged);

        UnbindUpgradeButton(capacityUpgradeRow, OnClickBuyCapacityUpgrade);
        UnbindUpgradeButton(boatSpeedUpgradeRow, OnClickBuyBoatSpeedUpgrade);
        UnbindUpgradeButton(rodUpgradeRow, OnClickBuyRodUpgrade);
        UnbindUpgradeButton(fireproofBoatUpgradeRow, OnClickBuyFireproofBoatUpgrade);

        if (closeButton != null)
            closeButton.onClick.RemoveListener(OnClickClose);

        areButtonsBound = false;
    }

    private void BindUpgradeButton(DockOwnerUpgradeRowUI _row, UnityEngine.Events.UnityAction _action)
    {
        if (_row == null || _row.BuyButton == null)
            return;

        _row.BuyButton.onClick.AddListener(_action);
    }

    private void UnbindUpgradeButton(DockOwnerUpgradeRowUI _row, UnityEngine.Events.UnityAction _action)
    {
        if (_row == null || _row.BuyButton == null)
            return;

        _row.BuyButton.onClick.RemoveListener(_action);
    }

    #endregion

    #region Selection And Helpers

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

    private void SetStatus(string _message)
    {
        if (statusText == null)
            return;

        RectTransform rect = statusText.rectTransform;

        if (!hasStatusBasePosition)
        {
            statusBasePosition = rect.anchoredPosition;
            hasStatusBasePosition = true;
        }

        if (statusFeedbackRoutine != null)
        {
            StopCoroutine(statusFeedbackRoutine);
            statusFeedbackRoutine = null;
        }

        rect.anchoredPosition = statusBasePosition;

        if (string.IsNullOrEmpty(_message))
        {
            statusText.text = string.Empty;
            Color hiddenColor = statusText.color;
            hiddenColor.a = 0f;
            statusText.color = hiddenColor;
            return;
        }

        statusFeedbackRoutine = StartCoroutine(ShowStatusRoutine(_message));
    }

    private IEnumerator ShowStatusRoutine(string _message)
    {
        RectTransform rect = statusText.rectTransform;
        Vector2 basePosition = hasStatusBasePosition ? statusBasePosition : rect.anchoredPosition;
        Color baseColor = statusText.color;
        Color visibleColor = baseColor;
        visibleColor.a = 1f;
        float duration = Mathf.Max(0.1f, statusMessageDuration);
        float elapsed = 0f;

        statusText.text = _message;
        statusText.color = visibleColor;
        statusText.gameObject.SetActive(true);

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float fadeProgress = Mathf.InverseLerp(0.25f, 1f, progress);
            Color color = visibleColor;
            color.a = Mathf.Lerp(1f, 0f, fadeProgress);
            statusText.color = color;
            rect.anchoredPosition = basePosition + Vector2.up * (statusMessageRise * progress);
            yield return null;
        }

        statusText.text = string.Empty;
        rect.anchoredPosition = basePosition;
        baseColor.a = 0f;
        statusText.color = baseColor;
        statusFeedbackRoutine = null;
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

        if (sellFishSlots != null)
        {
            for (int i = 0; i < sellFishSlots.Length; i++)
            {
                Selectable selectable = sellFishSlots[i] != null ? sellFishSlots[i].GetComponentInChildren<Selectable>(true) : null;

                if (UISelectionHelper.IsUsable(selectable))
                    return selectable;
            }
        }

        if (UISelectionHelper.IsUsable(sellSelectedButton))
            return sellSelectedButton;

        return closeButton;
    }

    private Selectable GetUpgradeTabSelectable()
    {
        if (UISelectionHelper.IsUsable(upgradesFirstSelected))
            return upgradesFirstSelected;

        if (capacityUpgradeRow != null && UISelectionHelper.IsUsable(capacityUpgradeRow.BuyButton))
            return capacityUpgradeRow.BuyButton;

        if (boatSpeedUpgradeRow != null && UISelectionHelper.IsUsable(boatSpeedUpgradeRow.BuyButton))
            return boatSpeedUpgradeRow.BuyButton;

        if (rodUpgradeRow != null && UISelectionHelper.IsUsable(rodUpgradeRow.BuyButton))
            return rodUpgradeRow.BuyButton;

        if (fireproofBoatUpgradeRow != null && UISelectionHelper.IsUsable(fireproofBoatUpgradeRow.BuyButton))
            return fireproofBoatUpgradeRow.BuyButton;

        return closeButton;
    }

    private Selectable GetBaitTabSelectable()
    {
        if (UISelectionHelper.IsUsable(baitsFirstSelected))
            return baitsFirstSelected;

        Selectable baitSelectable = baitShopUI != null ? baitShopUI.GetFirstSelectable() : null;

        if (UISelectionHelper.IsUsable(baitSelectable))
            return baitSelectable;

        return closeButton;
    }

    private void SetGameUiState(GameManager.GameState _state, bool _lockCursor, bool _showCursor)
    {
        if (GameManager.instance != null)
            GameManager.instance.SetState(_state);

        Cursor.lockState = _lockCursor ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = _showCursor;
    }

    #endregion
}
