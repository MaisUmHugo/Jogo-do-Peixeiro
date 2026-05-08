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

    [Header("Future Tabs")]
    [SerializeField] private TMP_Text upgradesPlaceholderText;
    [SerializeField] private TMP_Text baitsPlaceholderText;

    [Header("Common")]
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private Button closeButton;

    [Header("References")]
    [SerializeField] private FishMarket fishMarket;
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

    public void OnClickClose()
    {
        Close();
    }

    public void Refresh()
    {
        RefreshInventorySnapshot();
        SetSellTexts();
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
            SetStatus("Upgrades em breve.");
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
            upgradesPlaceholderText.text = "Upgrades em breve.";

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
    }

    private void HandleSaleCompleted(int _earnedMoney)
    {
        Refresh();
    }

    private void TryResolveReferences()
    {
        if (fishMarket == null)
            fishMarket = FindFirstObjectByType<FishMarket>();

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

    private void SubscribeToReferences()
    {
        if (isSubscribed)
            return;

        if (shipInventory != null)
            shipInventory.OnFishListChange += ChangeFishList;

        if (fishMarket != null)
            fishMarket.OnSaleCompleted += HandleSaleCompleted;

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

    private void SetGameUiState(GameManager.GameState _state, bool _lockCursor, bool _showCursor)
    {
        if (GameManager.instance != null)
            GameManager.instance.SetState(_state);

        Cursor.lockState = _lockCursor ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = _showCursor;
    }
}
