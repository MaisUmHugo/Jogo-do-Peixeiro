using System;
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

    [Serializable]
    private class BaitShopCard
    {
        [SerializeField] private GameObject root;
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private TMP_Text ownedQuantityText;
        [SerializeField] private TMP_Text priceText;
        [SerializeField] private TMP_Text maxQuantityText;
        [SerializeField] private Button buyButton;

        public Button BuyButton => buyButton;
        public TMP_Text FeedbackText => maxQuantityText != null ? maxQuantityText : priceText;

        public void SetBait(BaitData _bait, int _ownedQuantity, int _maxQuantity, int _unitCost)
        {
            bool hasBait = _bait != null;
            SetVisible(hasBait);

            if (buyButton != null)
                buyButton.interactable = hasBait && _maxQuantity > 0;

            if (!hasBait)
                return;

            if (nameText != null)
                nameText.text = _bait.BaitName;

            if (descriptionText != null)
            {
                descriptionText.text = string.IsNullOrWhiteSpace(_bait.Description)
                    ? "Bonus de pesca."
                    : _bait.Description;
            }

            if (ownedQuantityText != null)
                ownedQuantityText.text = $"No inventario: {_ownedQuantity}";

            if (priceText != null)
                priceText.text = _unitCost > 0 ? $"Preco: R$ {_unitCost}" : "Gratis";

            if (maxQuantityText != null)
                maxQuantityText.text = _maxQuantity > 0 ? $"Max: {_maxQuantity}" : "Sem dinheiro";

            if (iconImage != null)
            {
                iconImage.sprite = _bait.InventoryIcon;
                iconImage.enabled = _bait.InventoryIcon != null;
                iconImage.preserveAspect = true;
            }
        }

        public void Clear()
        {
            if (buyButton != null)
                buyButton.interactable = false;

            SetVisible(false);
        }

        public bool Contains(GameObject _target)
        {
            GameObject rootObject = GetRootObject();
            return _target != null &&
                   rootObject != null &&
                   (_target == rootObject || _target.transform.IsChildOf(rootObject.transform));
        }

        public Selectable GetSelectable()
        {
            return buyButton;
        }

        private void SetVisible(bool _visible)
        {
            GameObject rootObject = GetRootObject();

            if (rootObject != null)
                rootObject.SetActive(_visible);
        }

        private GameObject GetRootObject()
        {
            if (root != null)
                return root;

            return buyButton != null ? buyButton.gameObject : null;
        }
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

    [Header("Bait Shop Cards")]
    [SerializeField] private BaitShopCard[] baitShopCards;
    [SerializeField] private int maxBaitPurchaseQuantity = 99;
    [SerializeField, Range(0.1f, 1f)] private float baitQuantityMoveThreshold = 0.65f;
    [SerializeField, Min(0.05f)] private float baitQuantityMoveRepeatDelay = 0.22f;

    [Header("Bait Quantity Popup")]
    [SerializeField] private GameObject baitQuantityPanel;
    [SerializeField] private TMP_Text baitQuantityTitleText;
    [SerializeField] private TMP_Text baitQuantityText;
    [SerializeField] private TMP_Text baitQuantityPriceText;
    [SerializeField] private TMP_Text baitQuantityOwnedText;
    [SerializeField] private TMP_Text baitQuantityMaxText;
    [SerializeField] private Button baitQuantityDecreaseButton;
    [SerializeField] private Button baitQuantityIncreaseButton;
    [SerializeField] private Button baitQuantityConfirmButton;
    [SerializeField] private Button baitQuantityCancelButton;
    [SerializeField] private Selectable baitQuantityFirstSelected;

    [Header("Purchase Confirmation")]
    [SerializeField] private GameObject purchaseConfirmPanel;
    [SerializeField] private TMP_Text purchaseConfirmTitleText;
    [SerializeField] private TMP_Text purchaseConfirmMessageText;
    [SerializeField] private Button purchaseConfirmYesButton;
    [SerializeField] private Button purchaseConfirmNoButton;
    [SerializeField] private Selectable purchaseConfirmFirstSelected;

    [Header("Common")]
    [SerializeField] private TMP_Text statusText;
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
    private int[] baitPurchaseQuantities;
    private int pendingBaitSlotIndex = -1;
    private float nextBaitQuantityMoveTime;
    private int lastBaitQuantityMoveDirection;
    private GameObject lastScrolledSellSelection;
    private Action pendingPurchaseAction;
    private Selectable selectionBeforePopup;
    private Coroutine baitQuantityFeedbackRoutine;
    private UnityEngine.Events.UnityAction[] baitCardBuyActions;
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
        HandleBaitQuantityMoveInput();
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

    public void OnClickBuyBaitSlot3()
    {
        TryBuyBaitSlot(3);
    }

    public void OnClickIncreasePendingBaitQuantity()
    {
        ChangePendingBaitQuantity(1);
    }

    public void OnClickDecreasePendingBaitQuantity()
    {
        ChangePendingBaitQuantity(-1);
    }

    public void OnClickConfirmBaitQuantity()
    {
        ConfirmPendingBaitQuantity();
    }

    public void OnClickCancelBaitQuantity()
    {
        CloseBaitQuantityPopup(true);
    }

    public void OnClickConfirmPurchase()
    {
        ConfirmPendingPurchase();
    }

    public void OnClickCancelPurchase()
    {
        ClosePurchaseConfirmation(true);
    }

    public void OnClickIncreaseBaitSlot0()
    {
        ChangeBaitQuantity(0, 1);
    }

    public void OnClickDecreaseBaitSlot0()
    {
        ChangeBaitQuantity(0, -1);
    }

    public void OnClickIncreaseBaitSlot1()
    {
        ChangeBaitQuantity(1, 1);
    }

    public void OnClickDecreaseBaitSlot1()
    {
        ChangeBaitQuantity(1, -1);
    }

    public void OnClickIncreaseBaitSlot2()
    {
        ChangeBaitQuantity(2, 1);
    }

    public void OnClickDecreaseBaitSlot2()
    {
        ChangeBaitQuantity(2, -1);
    }

    public void OnClickIncreaseBaitSlot3()
    {
        ChangeBaitQuantity(3, 1);
    }

    public void OnClickDecreaseBaitSlot3()
    {
        ChangeBaitQuantity(3, -1);
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
        CloseBaitQuantityPopup(false);
        ClosePurchaseConfirmation(false);
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
        BaitData[] baits = GetBaitsForSale();
        int visibleCount = Mathf.Min(baits.Length, GetBaitShopCardCount());
        EnsureBaitQuantityArray(visibleCount);

        for (int i = 0; i < GetBaitShopCardCount(); i++)
        {
            BaitShopCard card = baitShopCards[i];

            if (card == null)
                continue;

            bool hasBait = i < visibleCount && baits[i] != null;

            if (!hasBait)
            {
                card.Clear();
                continue;
            }

            BaitData bait = baits[i];
            int maxQuantity = GetMaxBaitPurchaseQuantity(bait);
            int ownedQuantity = baitInventory != null ? baitInventory.GetQuantity(bait) : 0;

            card.SetBait(
                bait,
                ownedQuantity,
                maxQuantity,
                Mathf.Max(0, bait.PurchasePrice)
            );
        }

        RefreshBaitQuantityPopup();
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

    #region Baits

    private BaitData[] GetBaitsForSale()
    {
        return baitShop != null ? baitShop.BaitsForSale : BaitCatalog.GetDefaultBaits();
    }

    private void EnsureBaitQuantityArray(int _count)
    {
        int safeCount = Mathf.Max(0, _count);

        if (baitPurchaseQuantities != null && baitPurchaseQuantities.Length >= safeCount)
            return;

        int[] resizedQuantities = new int[safeCount];

        for (int i = 0; i < resizedQuantities.Length; i++)
            resizedQuantities[i] = 1;

        if (baitPurchaseQuantities != null)
        {
            for (int i = 0; i < baitPurchaseQuantities.Length && i < resizedQuantities.Length; i++)
                resizedQuantities[i] = Mathf.Max(1, baitPurchaseQuantities[i]);
        }

        baitPurchaseQuantities = resizedQuantities;
    }

    private int GetBaitPurchaseQuantity(int _slotIndex, BaitData _bait)
    {
        EnsureBaitQuantityArray(GetBaitShopCardCount());

        if (_slotIndex < 0 || baitPurchaseQuantities == null || _slotIndex >= baitPurchaseQuantities.Length)
            return GetMaxBaitPurchaseQuantity(_bait) > 0 ? 1 : 0;

        int maxQuantity = GetMaxBaitPurchaseQuantity(_bait);
        int minQuantity = maxQuantity > 0 ? 1 : 0;
        baitPurchaseQuantities[_slotIndex] = Mathf.Clamp(baitPurchaseQuantities[_slotIndex], minQuantity, Mathf.Max(1, maxQuantity));
        return baitPurchaseQuantities[_slotIndex];
    }

    private int GetMaxBaitPurchaseQuantity(BaitData _bait)
    {
        int limit = Mathf.Max(1, maxBaitPurchaseQuantity);

        if (baitShop != null)
            return baitShop.GetMaxAffordableQuantity(_bait, limit);

        if (_bait == null)
            return 0;

        int unitCost = Mathf.Max(0, _bait.PurchasePrice);

        if (unitCost == 0)
            return limit;

        int affordable = Mathf.FloorToInt(playerMoney / unitCost);
        return Mathf.Clamp(affordable, 0, limit);
    }

    private int GetBaitPurchaseCost(BaitData _bait, int _quantity)
    {
        if (_bait == null || _quantity <= 0)
            return 0;

        if (baitShop != null)
            return baitShop.GetBaitPurchaseCost(_bait, _quantity);

        return Mathf.Max(0, _bait.PurchasePrice * _quantity);
    }

    private void SetBaitQuantityFromSlot(int _slotIndex, int _quantity)
    {
        BaitData[] baits = GetBaitsForSale();

        if (_slotIndex < 0 || _slotIndex >= baits.Length)
            return;

        EnsureBaitQuantityArray(GetBaitShopCardCount());
        baitPurchaseQuantities[_slotIndex] = Mathf.Clamp(_quantity, 0, Mathf.Max(1, GetMaxBaitPurchaseQuantity(baits[_slotIndex])));
        SetBaitUI();
        RefreshBaitQuantityPopup();
    }

    private void ChangeBaitQuantity(int _slotIndex, int _delta)
    {
        BaitData[] baits = GetBaitsForSale();

        if (_slotIndex < 0 || _slotIndex >= baits.Length || _delta == 0)
            return;

        BaitData bait = baits[_slotIndex];
        EnsureBaitQuantityArray(GetBaitShopCardCount());

        int currentQuantity = GetBaitPurchaseQuantity(_slotIndex, bait);
        int maxQuantity = GetMaxBaitPurchaseQuantity(bait);
        int minQuantity = maxQuantity > 0 ? 1 : 0;
        int nextQuantity = Mathf.Clamp(currentQuantity + _delta, minQuantity, Mathf.Max(1, maxQuantity));

        if (nextQuantity == currentQuantity)
        {
            ShakeBaitQuantity(_slotIndex);
            return;
        }

        baitPurchaseQuantities[_slotIndex] = nextQuantity;
        SetBaitUI();
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
        int maxQuantity = GetMaxBaitPurchaseQuantity(bait);

        if (maxQuantity <= 0)
        {
            ShakeBaitQuantity(_slotIndex);
            SetStatus("Dinheiro insuficiente.");
            return;
        }

        pendingBaitSlotIndex = _slotIndex;
        int quantity = GetBaitPurchaseQuantity(_slotIndex, bait);

        if (baitQuantityPanel != null)
        {
            OpenBaitQuantityPopup(_slotIndex);
            return;
        }

        OpenBaitPurchaseConfirmation(_slotIndex, quantity);
    }

    private void ExecuteBaitPurchase(int _slotIndex)
    {
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
        int quantity = GetBaitPurchaseQuantity(_slotIndex, bait);

        if (quantity <= 0)
        {
            ShakeBaitQuantity(_slotIndex);
            SetStatus("Dinheiro insuficiente.");
            return;
        }

        bool success = baitShop.TryBuyBait(bait, quantity, out BaitPurchaseResult result);

        if (!success)
            ShakeBaitQuantity(_slotIndex);

        SetStatus(success ? $"{bait.BaitName} x{quantity} comprada." : GetBaitPurchaseStatusText(result));
        Refresh();
    }

    private void OpenBaitQuantityPopup(int _slotIndex)
    {
        pendingBaitSlotIndex = _slotIndex;
        selectionBeforePopup = UISelectionHelper.CurrentSelectableInScope(PanelObject);
        SetObjectActive(baitQuantityPanel, true);
        RefreshBaitQuantityPopup();
        UISelectionHelper.Select(
            UISelectionHelper.IsUsable(baitQuantityFirstSelected) ? baitQuantityFirstSelected : baitQuantityConfirmButton,
            baitQuantityPanel
        );
    }

    private void CloseBaitQuantityPopup(bool _restoreSelection)
    {
        SetObjectActive(baitQuantityPanel, false);
        pendingBaitSlotIndex = -1;
        lastBaitQuantityMoveDirection = 0;
        nextBaitQuantityMoveTime = 0f;

        if (_restoreSelection)
            UISelectionHelper.Select(selectionBeforePopup, PanelObject);
    }

    private void RefreshBaitQuantityPopup()
    {
        if (baitQuantityPanel == null || !baitQuantityPanel.activeInHierarchy || pendingBaitSlotIndex < 0)
            return;

        BaitData[] baits = GetBaitsForSale();

        if (pendingBaitSlotIndex >= baits.Length)
        {
            CloseBaitQuantityPopup(false);
            return;
        }

        BaitData bait = baits[pendingBaitSlotIndex];
        int quantity = GetBaitPurchaseQuantity(pendingBaitSlotIndex, bait);
        int maxQuantity = GetMaxBaitPurchaseQuantity(bait);
        int ownedQuantity = baitInventory != null ? baitInventory.GetQuantity(bait) : 0;
        int totalCost = GetBaitPurchaseCost(bait, quantity);

        if (baitQuantityTitleText != null)
            baitQuantityTitleText.text = bait != null ? bait.BaitName : "Isca";

        if (baitQuantityText != null)
            baitQuantityText.text = quantity.ToString();

        if (baitQuantityPriceText != null)
            baitQuantityPriceText.text = $"Total: R$ {totalCost}";

        if (baitQuantityOwnedText != null)
            baitQuantityOwnedText.text = $"No inventario: {ownedQuantity}";

        if (baitQuantityMaxText != null)
            baitQuantityMaxText.text = $"Maximo: {maxQuantity}";

        SetButtonInteractable(baitQuantityDecreaseButton, quantity > 1);
        SetButtonInteractable(baitQuantityIncreaseButton, quantity < maxQuantity);
        SetButtonInteractable(baitQuantityConfirmButton, quantity > 0);
    }

    private void ChangePendingBaitQuantity(int _delta)
    {
        if (pendingBaitSlotIndex < 0)
            return;

        ChangeBaitQuantity(pendingBaitSlotIndex, _delta);
        RefreshBaitQuantityPopup();
    }

    private void ConfirmPendingBaitQuantity()
    {
        if (pendingBaitSlotIndex < 0)
            return;

        int slotIndex = pendingBaitSlotIndex;
        BaitData[] baits = GetBaitsForSale();
        Selectable previousSelection = selectionBeforePopup;

        if (slotIndex >= baits.Length)
        {
            CloseBaitQuantityPopup(false);
            return;
        }

        CloseBaitQuantityPopup(false);
        OpenBaitPurchaseConfirmation(slotIndex, GetBaitPurchaseQuantity(slotIndex, baits[slotIndex]));
        selectionBeforePopup = previousSelection;
    }

    private void OpenBaitPurchaseConfirmation(int _slotIndex, int _quantity)
    {
        BaitData[] baits = GetBaitsForSale();

        if (_slotIndex < 0 || _slotIndex >= baits.Length)
            return;

        BaitData bait = baits[_slotIndex];
        int totalCost = GetBaitPurchaseCost(bait, _quantity);
        OpenPurchaseConfirmation(
            "Confirmar compra",
            $"Comprar {bait.BaitName} x{_quantity} por R$ {totalCost}?",
            () => ExecuteBaitPurchase(_slotIndex)
        );
    }

    private void HandleBaitQuantityMoveInput()
    {
        if (!isOpen || currentTab != DockOwnerTab.Baits || InputHandler.instance == null)
            return;

        int slotIndex = baitQuantityPanel != null && baitQuantityPanel.activeInHierarchy && pendingBaitSlotIndex >= 0
            ? pendingBaitSlotIndex
            : GetSelectedBaitSlotIndex();

        if (slotIndex < 0)
            return;

        float horizontal = InputHandler.instance.moveInput.x;

        if (Mathf.Abs(horizontal) < baitQuantityMoveThreshold)
        {
            lastBaitQuantityMoveDirection = 0;
            nextBaitQuantityMoveTime = 0f;
            return;
        }

        int direction = horizontal > 0f ? 1 : -1;

        if (direction == lastBaitQuantityMoveDirection && Time.unscaledTime < nextBaitQuantityMoveTime)
            return;

        ChangeBaitQuantity(slotIndex, direction);
        lastBaitQuantityMoveDirection = direction;
        nextBaitQuantityMoveTime = Time.unscaledTime + baitQuantityMoveRepeatDelay;
    }

    private int GetSelectedBaitSlotIndex()
    {
        if (EventSystem.current == null || baitShopCards == null)
            return -1;

        GameObject selectedObject = EventSystem.current.currentSelectedGameObject;

        if (selectedObject == null)
            return -1;

        for (int i = 0; i < baitShopCards.Length; i++)
        {
            if (baitShopCards[i] != null && baitShopCards[i].Contains(selectedObject))
                return i;
        }

        return -1;
    }

    private void ShakeBaitQuantity(int _slotIndex)
    {
        TMP_Text feedbackText = baitQuantityPanel != null &&
                                baitQuantityPanel.activeInHierarchy &&
                                pendingBaitSlotIndex == _slotIndex
            ? baitQuantityText
            : GetBaitCardFeedbackText(_slotIndex);

        if (feedbackText == null)
            return;

        if (baitQuantityFeedbackRoutine != null)
            StopCoroutine(baitQuantityFeedbackRoutine);

        baitQuantityFeedbackRoutine = StartCoroutine(ShakeTextRoutine(feedbackText));
    }

    private TMP_Text GetBaitCardFeedbackText(int _slotIndex)
    {
        if (baitShopCards == null ||
            _slotIndex < 0 ||
            _slotIndex >= baitShopCards.Length ||
            baitShopCards[_slotIndex] == null)
        {
            return null;
        }

        return baitShopCards[_slotIndex].FeedbackText;
    }

    private IEnumerator ShakeTextRoutine(TMP_Text _text)
    {
        RectTransform rect = _text != null ? _text.rectTransform : null;

        if (rect == null)
            yield break;

        Vector2 basePosition = rect.anchoredPosition;
        Vector3 baseScale = rect.localScale;
        const float duration = 0.22f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float shake = Mathf.Sin(progress * Mathf.PI * 8f) * (1f - progress) * 8f;
            rect.anchoredPosition = basePosition + new Vector2(shake, 0f);
            rect.localScale = baseScale * (1f + 0.08f * (1f - progress));
            yield return null;
        }

        rect.anchoredPosition = basePosition;
        rect.localScale = baseScale;
        baitQuantityFeedbackRoutine = null;
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

    #endregion

    #region Purchase Confirmation

    private void OpenPurchaseConfirmation(string _title, string _message, Action _purchaseAction)
    {
        pendingPurchaseAction = _purchaseAction;
        selectionBeforePopup = UISelectionHelper.CurrentSelectableInScope(PanelObject);

        if (purchaseConfirmTitleText != null)
            purchaseConfirmTitleText.text = _title;

        if (purchaseConfirmMessageText != null)
            purchaseConfirmMessageText.text = _message;

        if (purchaseConfirmPanel == null)
        {
            ConfirmPendingPurchase();
            return;
        }

        SetObjectActive(purchaseConfirmPanel, true);
        UISelectionHelper.Select(
            UISelectionHelper.IsUsable(purchaseConfirmFirstSelected) ? purchaseConfirmFirstSelected : purchaseConfirmYesButton,
            purchaseConfirmPanel
        );
    }

    private void ConfirmPendingPurchase()
    {
        Action action = pendingPurchaseAction;
        ClosePurchaseConfirmation(false);
        action?.Invoke();
    }

    private void ClosePurchaseConfirmation(bool _restoreSelection)
    {
        SetObjectActive(purchaseConfirmPanel, false);
        pendingPurchaseAction = null;

        if (_restoreSelection)
            UISelectionHelper.Select(selectionBeforePopup, PanelObject);
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
        CloseBaitQuantityPopup(false);
        ClosePurchaseConfirmation(false);
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
        if (purchaseConfirmPanel != null && purchaseConfirmPanel.activeInHierarchy)
        {
            ClosePurchaseConfirmation(true);
            return;
        }

        if (baitQuantityPanel != null && baitQuantityPanel.activeInHierarchy)
        {
            CloseBaitQuantityPopup(true);
            return;
        }

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

        ResolveUpgradeRows();
        ResolveBaitShopCards();
        ResolveSellFishScrollRect();
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

    private void ResolveBaitShopCards()
    {
        if (baitShopCards != null)
            return;

        baitShopCards = Array.Empty<BaitShopCard>();
    }

    private int GetBaitShopCardCount()
    {
        return baitShopCards != null ? baitShopCards.Length : 0;
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
        BindBaitCardButtons();

        if (closeButton != null)
            closeButton.onClick.AddListener(OnClickClose);

        if (baitQuantityDecreaseButton != null)
            baitQuantityDecreaseButton.onClick.AddListener(OnClickDecreasePendingBaitQuantity);

        if (baitQuantityIncreaseButton != null)
            baitQuantityIncreaseButton.onClick.AddListener(OnClickIncreasePendingBaitQuantity);

        if (baitQuantityConfirmButton != null)
            baitQuantityConfirmButton.onClick.AddListener(OnClickConfirmBaitQuantity);

        if (baitQuantityCancelButton != null)
            baitQuantityCancelButton.onClick.AddListener(OnClickCancelBaitQuantity);

        if (purchaseConfirmYesButton != null)
            purchaseConfirmYesButton.onClick.AddListener(OnClickConfirmPurchase);

        if (purchaseConfirmNoButton != null)
            purchaseConfirmNoButton.onClick.AddListener(OnClickCancelPurchase);

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
        UnbindBaitCardButtons();

        if (closeButton != null)
            closeButton.onClick.RemoveListener(OnClickClose);

        if (baitQuantityDecreaseButton != null)
            baitQuantityDecreaseButton.onClick.RemoveListener(OnClickDecreasePendingBaitQuantity);

        if (baitQuantityIncreaseButton != null)
            baitQuantityIncreaseButton.onClick.RemoveListener(OnClickIncreasePendingBaitQuantity);

        if (baitQuantityConfirmButton != null)
            baitQuantityConfirmButton.onClick.RemoveListener(OnClickConfirmBaitQuantity);

        if (baitQuantityCancelButton != null)
            baitQuantityCancelButton.onClick.RemoveListener(OnClickCancelBaitQuantity);

        if (purchaseConfirmYesButton != null)
            purchaseConfirmYesButton.onClick.RemoveListener(OnClickConfirmPurchase);

        if (purchaseConfirmNoButton != null)
            purchaseConfirmNoButton.onClick.RemoveListener(OnClickCancelPurchase);

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

    private void BindBaitCardButtons()
    {
        if (baitShopCards == null || baitShopCards.Length == 0)
            return;

        baitCardBuyActions = new UnityEngine.Events.UnityAction[baitShopCards.Length];

        for (int i = 0; i < baitShopCards.Length; i++)
        {
            Button button = baitShopCards[i] != null ? baitShopCards[i].BuyButton : null;

            if (button == null)
                continue;

            int slotIndex = i;
            UnityEngine.Events.UnityAction action = () => TryBuyBaitSlot(slotIndex);
            baitCardBuyActions[i] = action;
            button.onClick.AddListener(action);
        }
    }

    private void UnbindBaitCardButtons()
    {
        if (baitShopCards == null || baitCardBuyActions == null)
            return;

        for (int i = 0; i < baitShopCards.Length && i < baitCardBuyActions.Length; i++)
        {
            Button button = baitShopCards[i] != null ? baitShopCards[i].BuyButton : null;

            if (button != null && baitCardBuyActions[i] != null)
                button.onClick.RemoveListener(baitCardBuyActions[i]);
        }

        baitCardBuyActions = null;
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
        if (statusText != null)
            statusText.text = _message;
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

        if (baitShopCards != null)
        {
            for (int i = 0; i < baitShopCards.Length; i++)
            {
                Selectable selectable = baitShopCards[i] != null ? baitShopCards[i].GetSelectable() : null;

                if (UISelectionHelper.IsUsable(selectable))
                    return selectable;
            }
        }

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
