using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InvertoryManager : MonoBehaviour
{
    #region Types And Fields

    private enum InventoryTab
    {
        Fish,
        Baits
    }

    public static InvertoryManager Instance { get; private set; }

    [SerializeField] private ShipInventory shipInventory;
    [SerializeField] private BaitInventory baitInventory;
    [SerializeField] private GameObject inventoryRoot;

    [Header("Tabs")]
    [SerializeField] private Button fishTabButton;
    [SerializeField] private Button baitsTabButton;
    [SerializeField] private GameObject fishTabPanel;
    [SerializeField] private GameObject baitsTabPanel;
    [SerializeField] private GameObject[] fishTabObjects;
    [SerializeField] private GameObject[] baitsTabObjects;

    [Header("Fish")]
    [SerializeField] private TMP_Text inventoryText;
    [SerializeField] private TMP_Text kilogramText;
    [SerializeField] private Color weightNormalColor = Color.white;
    [SerializeField] private Color weightAlmostFullColor = new Color(1f, 0.82f, 0.2f, 1f);
    [SerializeField] private Color weightFullColor = new Color(1f, 0.25f, 0.25f, 1f);
    [SerializeField, Range(0f, 1f)] private float almostFullWeightPercent = 0.75f;
    [SerializeField, Range(0f, 1f)] private float fullWeightPercent = 0.95f;
    [SerializeField] private InventoryFishSlotUI[] fishGridSlots;
    [SerializeField] private Transform fishGridContent;
    [SerializeField] private InventoryFishSlotUI fishSlotTemplate;
    [SerializeField] private bool autoCreateFishGridSlots = true;

    [Header("Scroll Follow")]
    [SerializeField] private bool keepSelectionVisibleInScroll = true;
    [SerializeField] private ScrollRect fishScrollRect;
    [SerializeField] private ScrollRect discardFishScrollRect;
    [SerializeField] private ScrollRect baitScrollRect;
    [SerializeField, Min(0f)] private float selectionScrollPadding = 24f;

    [Header("Fish Discard")]
    [SerializeField] private GameObject discardModePanel;
    [SerializeField] private GameObject[] hideWhileDiscardMode;
    [SerializeField] private GameObject[] showWhileDiscardMode;
    [SerializeField] private Button discardModeButton;
    [SerializeField] private Button confirmDiscardButton;
    [SerializeField] private Button cancelDiscardButton;
    [SerializeField] private GameObject discardConfirmPanel;
    [SerializeField] private TMP_Text discardConfirmText;
    [SerializeField] private Button discardConfirmYesButton;
    [SerializeField] private Button discardConfirmNoButton;
    [SerializeField] private string discardConfirmTextFormat = "Descartar {0} peixe(s)?";
    [SerializeField] private InventoryFishSlotUI[] discardFishGridSlots;
    [SerializeField] private Transform discardFishGridContent;
    [SerializeField] private InventoryFishSlotUI discardFishSlotTemplate;
    [SerializeField] private bool autoCreateDiscardFishGridSlots = true;

    [Header("Baits")]
    [SerializeField] private TMP_Text baitInventoryText;
    [SerializeField] private TMP_Text equippedBaitText;
    [SerializeField] private Button[] baitEquipButtons;
    [SerializeField] private Button clearBaitButton;
    [SerializeField] private bool showClearBaitButton;
    [SerializeField] private BaitData[] baitSlots;
    [SerializeField] private InventoryBaitSlotUI[] baitGridSlots;
    [SerializeField] private Transform baitGridContent;
    [SerializeField] private InventoryBaitSlotUI baitSlotTemplate;
    [SerializeField] private bool autoCreateBaitGridSlots = true;

    [Header("Settings")]
    [SerializeField] private bool closeOnAwake = true;
    [SerializeField] private bool allowRuntimeFallback;
    [SerializeField] private bool logMissingReferences = true;

    [Header("Modal")]
    [SerializeField] private bool pauseTimeWhileOpen = true;
    [SerializeField] private bool hideHudWhileOpen = true;
    [SerializeField] private bool blockPauseWhileOpen = true;

    [Header("Navigation")]
    [SerializeField] private Selectable fishFirstSelected;
    [SerializeField] private Selectable baitsFirstSelected;
    [SerializeField, Range(0f, 1f)] private float selectionRecoveryMoveThreshold = 0.35f;

    private CanvasGroup inventoryCanvasGroup;
    private Coroutine inputSubscriptionRoutine;
    private InventoryTab currentTab = InventoryTab.Fish;
    private bool isInventoryVisible = true;
    private bool isShipInventorySubscribed;
    private bool isBaitInventorySubscribed;
    private bool isInputSubscribed;
    private bool areButtonsBound;
    private GameManager.GameState stateBeforeInventory = GameManager.GameState.OnFoot;
    private bool hasStoredStateBeforeInventory;
    private bool hasLoggedMissingRuntimeControls;
    private bool hasStoredKilogramTextColor;
    private bool wasSelectionRecoveryMoveHeld;
    private Color kilogramTextDefaultColor = Color.white;
    private int modalToken = UIModalManager.InvalidToken;
    private readonly HashSet<int> selectedFishIndexes = new HashSet<int>();
    private bool isDiscardMode;
    private bool isDiscardConfirmVisible;
    private GameObject lastScrolledSelection;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        InitializeReferences();
        EnsureRuntimeControls();
        BindButtons();
        SetObjectActive(discardModePanel, false);
        SetDiscardConfirmPanelVisible(false);
        UpdateDiscardControls();

        if (closeOnAwake)
            SetInventoryVisible(false);
    }

    private void OnEnable()
    {
        InitializeReferences();
        EnsureRuntimeControls();
        BindButtons();
        UpdateDiscardControls();
        TrySubscribeInput();

        if (!isInputSubscribed)
            inputSubscriptionRoutine = StartCoroutine(WaitForInputHandler());
    }

    private void OnDisable()
    {
        if (inputSubscriptionRoutine != null)
        {
            StopCoroutine(inputSubscriptionRoutine);
            inputSubscriptionRoutine = null;
        }

        UnsubscribeInput();
        UnbindButtons();
        SetDiscardMode(false);
        UIModalManager.PopModal(ref modalToken);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        if (shipInventory != null && isShipInventorySubscribed)
            shipInventory.OnFishListChange -= OnNewFishList;

        if (baitInventory != null && isBaitInventorySubscribed)
            baitInventory.OnBaitInventoryChanged -= OnBaitInventoryChanged;
    }

    private void Update()
    {
        RecoverSelectionFromMoveInput();
        KeepCurrentSelectionVisible();
    }

    private void OnValidate()
    {
        if (fullWeightPercent < almostFullWeightPercent)
            fullWeightPercent = almostFullWeightPercent;

        selectionRecoveryMoveThreshold = Mathf.Clamp01(selectionRecoveryMoveThreshold);
        selectionScrollPadding = Mathf.Max(0f, selectionScrollPadding);
    }

    #endregion

    #region Reference Resolution And Grid Setup

    private void InitializeReferences()
    {
        if (inventoryRoot == null)
            inventoryRoot = gameObject;

        if (inventoryRoot == gameObject && inventoryCanvasGroup == null)
        {
            inventoryCanvasGroup = inventoryRoot.GetComponent<CanvasGroup>();

            if (inventoryCanvasGroup == null)
                inventoryCanvasGroup = inventoryRoot.AddComponent<CanvasGroup>();
        }

        if (shipInventory == null)
            shipInventory = FindFirstObjectByType<ShipInventory>(FindObjectsInactive.Include);

        if (shipInventory != null && !isShipInventorySubscribed)
        {
            shipInventory.OnFishListChange += OnNewFishList;
            isShipInventorySubscribed = true;
        }

        if (baitInventory == null)
            baitInventory = BaitInventory.GetOrCreate();

        if (baitInventory != null && !isBaitInventorySubscribed)
        {
            baitInventory.OnBaitInventoryChanged += OnBaitInventoryChanged;
            isBaitInventorySubscribed = true;
        }

        ResolveInventoryPanels();
        ResolveInventoryButtons();
        EnsureGridSlotReferences();
    }

    private void ResolveInventoryPanels()
    {
        if (inventoryRoot == null)
            return;

        if (fishTabPanel == null)
            fishTabPanel = FindChildGameObject("FishInventory", "FishInventoryPanel", "Fishes Scroll View", "FishGridPanel");

        if (baitsTabPanel == null)
            baitsTabPanel = FindChildGameObject("BaitInventory", "BaitInventoryPanel", "BaitsInventoryPanel", "BaitGridPanel");

        if (discardModePanel == null)
            discardModePanel = FindChildGameObject("DiscardPanel", "DiscardModePanel", "FishDiscardPanel");

        fishTabPanel = ResolveTabPanelRoot(fishTabPanel, "FishInventory", "FishInventoryPanel");
        baitsTabPanel = ResolveTabPanelRoot(baitsTabPanel, "BaitInventory", "BaitInventoryPanel", "BaitsInventoryPanel");
        EnsureTabObjectFallbacks();
        ResolveScrollRects();
    }

    private void ResolveScrollRects()
    {
        if (fishScrollRect == null && fishTabPanel != null)
            fishScrollRect = fishTabPanel.GetComponentInChildren<ScrollRect>(true);

        if (discardFishScrollRect == null && discardModePanel != null)
            discardFishScrollRect = discardModePanel.GetComponentInChildren<ScrollRect>(true);

        if (baitScrollRect == null && baitsTabPanel != null)
            baitScrollRect = baitsTabPanel.GetComponentInChildren<ScrollRect>(true);

        UISelectionHelper.ConfigureVerticalOnlyScrollRect(fishScrollRect);
        UISelectionHelper.ConfigureVerticalOnlyScrollRect(discardFishScrollRect);
        UISelectionHelper.ConfigureVerticalOnlyScrollRect(baitScrollRect);
    }

    private GameObject ResolveTabPanelRoot(GameObject _currentPanel, params string[] _rootNames)
    {
        GameObject namedRoot = FindChildGameObject(_rootNames);

        if (namedRoot == null)
            return _currentPanel;

        if (_currentPanel == null ||
            _currentPanel == namedRoot ||
            _currentPanel.transform.IsChildOf(namedRoot.transform))
        {
            return namedRoot;
        }

        return _currentPanel;
    }

    private void EnsureTabObjectFallbacks()
    {
        if ((fishTabObjects == null || fishTabObjects.Length == 0) && fishTabPanel != null)
            fishTabObjects = new[] { fishTabPanel };

        if ((baitsTabObjects == null || baitsTabObjects.Length == 0) && baitsTabPanel != null)
            baitsTabObjects = new[] { baitsTabPanel };
    }

    private GameObject FindChildGameObject(params string[] _names)
    {
        if (inventoryRoot == null || _names == null)
            return null;

        Transform rootTransform = inventoryRoot.transform;

        for (int i = 0; i < _names.Length; i++)
        {
            if (string.IsNullOrEmpty(_names[i]))
                continue;

            Transform found = FindChildRecursive(rootTransform, _names[i]);

            if (found != null)
                return found.gameObject;
        }

        return null;
    }

    private void ResolveInventoryButtons()
    {
        if (inventoryRoot == null)
            return;

        if (fishTabButton == null)
            fishTabButton = FindChildComponent<Button>("FishButton", "FishTabButton", "FishTab");

        if (baitsTabButton == null)
            baitsTabButton = FindChildComponent<Button>("BaitButton", "BaitsTabButton", "BaitTab");

        if (discardModeButton == null)
            discardModeButton = FindChildComponent<Button>("DiscardButton", "DiscardModeButton");

        if (confirmDiscardButton == null)
            confirmDiscardButton = FindChildComponent<Button>("ConfirmButton", "ConfirmDiscardButton");

        if (cancelDiscardButton == null)
            cancelDiscardButton = FindChildComponent<Button>("CancelButton", "CancelDiscardButton");
    }

    private T FindChildComponent<T>(params string[] _names) where T : Component
    {
        if (inventoryRoot == null || _names == null)
            return null;

        Transform rootTransform = inventoryRoot.transform;

        for (int i = 0; i < _names.Length; i++)
        {
            if (string.IsNullOrEmpty(_names[i]))
                continue;

            Transform found = FindChildRecursive(rootTransform, _names[i]);

            if (found != null && found.TryGetComponent(out T component))
                return component;
        }

        return null;
    }

    private Transform FindChildRecursive(Transform _parent, string _name)
    {
        if (_parent == null)
            return null;

        if (_parent.name == _name)
            return _parent;

        for (int i = 0; i < _parent.childCount; i++)
        {
            Transform found = FindChildRecursive(_parent.GetChild(i), _name);

            if (found != null)
                return found;
        }

        return null;
    }

    private void EnsureGridSlotReferences()
    {
        if (!HasFishGridSlots() && fishTabPanel != null)
            fishGridSlots = fishTabPanel.GetComponentsInChildren<InventoryFishSlotUI>(true);

        if (!HasDiscardFishGridSlots() && discardModePanel != null)
            discardFishGridSlots = discardModePanel.GetComponentsInChildren<InventoryFishSlotUI>(true);

        if (!HasBaitGridSlots() && baitsTabPanel != null)
            baitGridSlots = baitsTabPanel.GetComponentsInChildren<InventoryBaitSlotUI>(true);

        if (fishSlotTemplate == null)
            fishSlotTemplate = GetFirstFishSlot();

        if (discardFishSlotTemplate == null)
            discardFishSlotTemplate = GetFirstDiscardFishSlot();

        if (baitSlotTemplate == null)
            baitSlotTemplate = GetFirstBaitSlot();

        if (fishGridContent == null)
            fishGridContent = ResolveGridContent(fishTabPanel, fishSlotTemplate != null ? fishSlotTemplate.transform : null);

        if (discardFishGridContent == null)
            discardFishGridContent = ResolveGridContent(discardModePanel, GetDiscardTemplateTransformInsidePanel());

        if (baitGridContent == null)
            baitGridContent = ResolveGridContent(baitsTabPanel, baitSlotTemplate != null ? baitSlotTemplate.transform : null);

        StoreKilogramTextColor();
    }

    private bool HasFishGridSlots()
    {
        if (fishGridSlots == null || fishGridSlots.Length == 0)
            return false;

        for (int i = 0; i < fishGridSlots.Length; i++)
        {
            if (fishGridSlots[i] != null)
                return true;
        }

        return false;
    }

    private bool HasBaitGridSlots()
    {
        if (baitGridSlots == null || baitGridSlots.Length == 0)
            return false;

        for (int i = 0; i < baitGridSlots.Length; i++)
        {
            if (baitGridSlots[i] != null)
                return true;
        }

        return false;
    }

    private bool HasDiscardFishGridSlots()
    {
        if (discardFishGridSlots == null || discardFishGridSlots.Length == 0)
            return false;

        for (int i = 0; i < discardFishGridSlots.Length; i++)
        {
            if (discardFishGridSlots[i] != null)
                return true;
        }

        return false;
    }

    private InventoryFishSlotUI GetFirstFishSlot()
    {
        if (fishGridSlots == null)
            return null;

        for (int i = 0; i < fishGridSlots.Length; i++)
        {
            if (fishGridSlots[i] != null)
                return fishGridSlots[i];
        }

        return null;
    }

    private InventoryFishSlotUI GetFirstDiscardFishSlot()
    {
        if (discardFishGridSlots != null)
        {
            for (int i = 0; i < discardFishGridSlots.Length; i++)
            {
                if (discardFishGridSlots[i] != null)
                    return discardFishGridSlots[i];
            }
        }

        return fishSlotTemplate != null ? fishSlotTemplate : GetFirstFishSlot();
    }

    private InventoryBaitSlotUI GetFirstBaitSlot()
    {
        if (baitGridSlots == null)
            return null;

        for (int i = 0; i < baitGridSlots.Length; i++)
        {
            if (baitGridSlots[i] != null)
                return baitGridSlots[i];
        }

        return null;
    }

    private Transform ResolveGridContent(GameObject _tabPanel, Transform _templateTransform)
    {
        if (_templateTransform != null && _templateTransform.parent != null)
            return _templateTransform.parent;

        if (_tabPanel == null)
            return null;

        ScrollRect scrollRect = _tabPanel.GetComponentInChildren<ScrollRect>(true);

        if (scrollRect != null && scrollRect.content != null)
            return scrollRect.content;

        return _tabPanel.transform;
    }

    private void EnsureFishGridCapacity(int _requiredCount)
    {
        if (!autoCreateFishGridSlots || _requiredCount <= 0)
            return;

        InventoryFishSlotUI template = GetFishSlotTemplate();
        Transform parent = GetFishGridContent();

        if (template == null || parent == null)
            return;

        List<InventoryFishSlotUI> slots = CollectFishGridSlots(parent);

        while (slots.Count < _requiredCount)
        {
            InventoryFishSlotUI slot = Instantiate(template, parent);
            slot.name = $"{template.name}_{slots.Count + 1:00}";
            slot.gameObject.SetActive(true);
            slots.Add(slot);
        }

        fishGridSlots = slots.ToArray();
    }

    private void EnsureDiscardFishGridCapacity(int _requiredCount)
    {
        if (!autoCreateDiscardFishGridSlots || _requiredCount <= 0)
            return;

        InventoryFishSlotUI template = GetDiscardFishSlotTemplate();
        Transform parent = GetDiscardFishGridContent();

        if (template == null || parent == null)
            return;

        List<InventoryFishSlotUI> slots = CollectDiscardFishGridSlots(parent);

        while (slots.Count < _requiredCount)
        {
            InventoryFishSlotUI slot = Instantiate(template, parent);
            slot.name = $"{template.name}_{slots.Count + 1:00}";
            slot.gameObject.SetActive(true);
            slots.Add(slot);
        }

        discardFishGridSlots = slots.ToArray();
    }

    private void EnsureBaitGridCapacity(int _requiredCount)
    {
        if (!autoCreateBaitGridSlots || _requiredCount <= 0)
            return;

        InventoryBaitSlotUI template = GetBaitSlotTemplate();
        Transform parent = GetBaitGridContent();

        if (template == null || parent == null)
            return;

        List<InventoryBaitSlotUI> slots = CollectBaitGridSlots(parent);

        while (slots.Count < _requiredCount)
        {
            InventoryBaitSlotUI slot = Instantiate(template, parent);
            slot.name = $"{template.name}_{slots.Count + 1:00}";
            slot.gameObject.SetActive(true);
            slots.Add(slot);
        }

        baitGridSlots = slots.ToArray();
    }

    private InventoryFishSlotUI GetFishSlotTemplate()
    {
        if (fishSlotTemplate != null)
            return fishSlotTemplate;

        fishSlotTemplate = GetFirstFishSlot();
        return fishSlotTemplate;
    }

    private InventoryFishSlotUI GetDiscardFishSlotTemplate()
    {
        if (discardFishSlotTemplate != null)
            return discardFishSlotTemplate;

        discardFishSlotTemplate = GetFirstDiscardFishSlot();
        return discardFishSlotTemplate;
    }

    private InventoryBaitSlotUI GetBaitSlotTemplate()
    {
        if (baitSlotTemplate != null)
            return baitSlotTemplate;

        baitSlotTemplate = GetFirstBaitSlot();
        return baitSlotTemplate;
    }

    private Transform GetFishGridContent()
    {
        if (fishGridContent != null)
            return fishGridContent;

        fishGridContent = ResolveGridContent(fishTabPanel, fishSlotTemplate != null ? fishSlotTemplate.transform : null);
        return fishGridContent;
    }

    private Transform GetDiscardFishGridContent()
    {
        if (discardFishGridContent != null)
            return discardFishGridContent;

        discardFishGridContent = ResolveGridContent(discardModePanel, GetDiscardTemplateTransformInsidePanel());
        return discardFishGridContent;
    }

    private Transform GetDiscardTemplateTransformInsidePanel()
    {
        if (discardFishSlotTemplate == null || discardModePanel == null)
            return null;

        return discardFishSlotTemplate.transform.IsChildOf(discardModePanel.transform)
            ? discardFishSlotTemplate.transform
            : null;
    }

    private Transform GetBaitGridContent()
    {
        if (baitGridContent != null)
            return baitGridContent;

        baitGridContent = ResolveGridContent(baitsTabPanel, baitSlotTemplate != null ? baitSlotTemplate.transform : null);
        return baitGridContent;
    }

    private List<InventoryFishSlotUI> CollectFishGridSlots(Transform _parent)
    {
        List<InventoryFishSlotUI> slots = new List<InventoryFishSlotUI>();
        AddUniqueFishSlot(slots, fishSlotTemplate, _parent);

        if (fishGridSlots != null)
        {
            for (int i = 0; i < fishGridSlots.Length; i++)
                AddUniqueFishSlot(slots, fishGridSlots[i], _parent);
        }

        if (_parent != null)
        {
            InventoryFishSlotUI[] childSlots = _parent.GetComponentsInChildren<InventoryFishSlotUI>(true);

            for (int i = 0; i < childSlots.Length; i++)
                AddUniqueFishSlot(slots, childSlots[i], _parent);
        }

        return slots;
    }

    private List<InventoryFishSlotUI> CollectDiscardFishGridSlots(Transform _parent)
    {
        List<InventoryFishSlotUI> slots = new List<InventoryFishSlotUI>();
        AddUniqueFishSlot(slots, discardFishSlotTemplate, _parent);

        if (discardFishGridSlots != null)
        {
            for (int i = 0; i < discardFishGridSlots.Length; i++)
                AddUniqueFishSlot(slots, discardFishGridSlots[i], _parent);
        }

        if (_parent != null)
        {
            InventoryFishSlotUI[] childSlots = _parent.GetComponentsInChildren<InventoryFishSlotUI>(true);

            for (int i = 0; i < childSlots.Length; i++)
                AddUniqueFishSlot(slots, childSlots[i], _parent);
        }

        return slots;
    }

    private List<InventoryBaitSlotUI> CollectBaitGridSlots(Transform _parent)
    {
        List<InventoryBaitSlotUI> slots = new List<InventoryBaitSlotUI>();
        AddUniqueBaitSlot(slots, baitSlotTemplate, _parent);

        if (baitGridSlots != null)
        {
            for (int i = 0; i < baitGridSlots.Length; i++)
                AddUniqueBaitSlot(slots, baitGridSlots[i], _parent);
        }

        if (_parent != null)
        {
            InventoryBaitSlotUI[] childSlots = _parent.GetComponentsInChildren<InventoryBaitSlotUI>(true);

            for (int i = 0; i < childSlots.Length; i++)
                AddUniqueBaitSlot(slots, childSlots[i], _parent);
        }

        return slots;
    }

    private void AddUniqueFishSlot(List<InventoryFishSlotUI> _slots, InventoryFishSlotUI _slot, Transform _parent)
    {
        if (_slot == null || _slots.Contains(_slot))
            return;

        if (_parent != null && !_slot.transform.IsChildOf(_parent))
            return;

        _slots.Add(_slot);
    }

    private void AddUniqueBaitSlot(List<InventoryBaitSlotUI> _slots, InventoryBaitSlotUI _slot, Transform _parent)
    {
        if (_slot == null || _slots.Contains(_slot))
            return;

        if (_parent != null && !_slot.transform.IsChildOf(_parent))
            return;

        _slots.Add(_slot);
    }

    #endregion

    #region Input And Inventory Events

    private void TrySubscribeInput()
    {
        if (isInputSubscribed || InputHandler.instance == null)
            return;

        InputHandler.instance.onInventoryPressed += ToggleInventory;
        isInputSubscribed = true;
    }

    private IEnumerator WaitForInputHandler()
    {
        while (!isInputSubscribed)
        {
            TrySubscribeInput();

            if (isInputSubscribed)
                break;

            yield return null;
        }

        inputSubscriptionRoutine = null;
    }

    private void UnsubscribeInput()
    {
        if (!isInputSubscribed)
            return;

        if (InputHandler.instance != null)
            InputHandler.instance.onInventoryPressed -= ToggleInventory;

        isInputSubscribed = false;
    }

    private void OnNewFishList(List<FishData> _ownedFishes, float _fishweight)
    {
        RefreshInventoryTexts();
    }

    private void OnBaitInventoryChanged()
    {
        RefreshInventoryTexts();
    }

    #endregion

    #region Public UI Actions

    public void ToggleInventory()
    {
        InitializeReferences();

        if (IsInventoryVisible())
        {
            CloseInventory();
            return;
        }

        OpenInventory();
    }

    public void OpenInventory()
    {
        if (!CanOpenInventory())
            return;

        StoreGameStateBeforeInventory();
        PushModalState();
        SetInventoryTab(currentTab);
        SetInventoryVisible(true);
        SelectCurrentTabControl();
    }

    public void OnClickFishTab()
    {
        SetInventoryTab(InventoryTab.Fish);
    }

    public void OnClickBaitsTab()
    {
        SetInventoryTab(InventoryTab.Baits);
        ForceCloseFishInventoryPanel();
    }

    public void OnClickEquipBaitSlot0()
    {
        TryEquipBaitSlot(0);
    }

    public void OnClickEquipBaitSlot1()
    {
        TryEquipBaitSlot(1);
    }

    public void OnClickEquipBaitSlot2()
    {
        TryEquipBaitSlot(2);
    }

    public void OnClickClearEquippedBait()
    {
        InitializeReferences();

        if (baitInventory != null)
            baitInventory.ClearEquippedBait();

        RefreshInventoryTexts();
        SelectCurrentTabControl();
    }

    public void OnClickStartDiscardMode()
    {
        SetInventoryTab(InventoryTab.Fish);
        SetDiscardMode(true);
        SelectDiscardModeControl();
    }

    public void OnClickConfirmDiscardSelection()
    {
        if (!isDiscardMode || selectedFishIndexes.Count == 0)
            return;

        SetDiscardConfirmPanelVisible(true);
        UISelectionHelper.Select(discardConfirmYesButton, inventoryRoot);
    }

    public void OnClickCancelDiscardMode()
    {
        SetDiscardMode(false);
        UISelectionHelper.Select(discardModeButton, inventoryRoot);
    }

    public void OnClickConfirmDiscard()
    {
        DiscardSelectedFish();
    }

    public void OnClickCancelDiscardConfirm()
    {
        SetDiscardConfirmPanelVisible(false);
        UISelectionHelper.Select(confirmDiscardButton, inventoryRoot);
    }

    public void CloseInventory()
    {
        bool wasVisible = IsInventoryVisible();
        SetDiscardMode(false);
        SetInventoryVisible(false);
        UISelectionHelper.ClearSelection(inventoryRoot);
        UIModalManager.PopModal(ref modalToken);

        if (wasVisible)
            RestoreGameStateAfterInventory();
    }

    public bool TryCloseInventory()
    {
        if (!IsInventoryVisible())
            return false;

        CloseInventory();
        return true;
    }

    public bool TryHandlePauseInput()
    {
        if (!IsInventoryVisible())
            return false;

        if (discardConfirmPanel != null && discardConfirmPanel.activeInHierarchy)
        {
            OnClickCancelDiscardConfirm();
            return true;
        }

        CloseInventory();
        return true;
    }

    private void HandleInventoryBack()
    {
        TryHandlePauseInput();
    }

    public static bool TryCloseOpenInventory()
    {
        if (Instance == null)
            Instance = FindFirstObjectByType<InvertoryManager>(FindObjectsInactive.Include);

        return Instance != null && Instance.TryCloseInventory();
    }

    public static bool TryHandleOpenInventoryPauseInput()
    {
        if (Instance == null)
            Instance = FindFirstObjectByType<InvertoryManager>(FindObjectsInactive.Include);

        return Instance != null && Instance.TryHandlePauseInput();
    }

    #endregion

    #region Inventory Visibility And Refresh

    private void SetInventoryVisible(bool _visible)
    {
        if (inventoryRoot == null)
            return;

        isInventoryVisible = _visible;

        if (inventoryRoot == gameObject)
        {
            if (inventoryCanvasGroup == null)
                inventoryCanvasGroup = inventoryRoot.GetComponent<CanvasGroup>();

            if (inventoryCanvasGroup == null)
                inventoryCanvasGroup = inventoryRoot.AddComponent<CanvasGroup>();

            inventoryCanvasGroup.alpha = _visible ? 1f : 0f;
            inventoryCanvasGroup.interactable = _visible;
            inventoryCanvasGroup.blocksRaycasts = _visible;
            return;
        }

        inventoryRoot.SetActive(_visible);
    }

    private void SetInventoryTab(InventoryTab _tab)
    {
        if (_tab != InventoryTab.Fish && isDiscardMode)
            SetDiscardMode(false);

        currentTab = _tab;

        SetTabObjectsActive(fishTabObjects, fishTabPanel, currentTab == InventoryTab.Fish);
        SetTabObjectsActive(baitsTabObjects, baitsTabPanel, currentTab == InventoryTab.Baits);
        SetButtonInteractable(fishTabButton, currentTab != InventoryTab.Fish);
        SetButtonInteractable(baitsTabButton, currentTab != InventoryTab.Baits);
        SetBaitControlsVisible(currentTab == InventoryTab.Baits);
        UpdateDiscardControls();
        RefreshInventoryTexts();
        EnforceExclusiveTabVisibility();

        if (IsInventoryVisible())
            SelectCurrentTabControl();
    }

    private void RefreshInventoryTexts()
    {
        InitializeReferences();

        if (currentTab == InventoryTab.Baits)
            SetBaitInventoryTexts();
        else
            SetFishInventoryTexts();

        EnsureCurrentSelectionIsUsable();
    }

    private void SetFishInventoryTexts()
    {
        List<FishData> ownedFish = shipInventory != null ? shipInventory.OwnedFish : null;
        bool hasFish = ownedFish != null && ownedFish.Count > 0;
        bool hasGridSlots = HasFishGridSlots() || (autoCreateFishGridSlots && GetFishSlotTemplate() != null);

        if (inventoryText != null)
            inventoryText.text = hasGridSlots && hasFish ? string.Empty : GetFishInventoryText();

        if (kilogramText != null)
        {
            float currentWeight = shipInventory != null ? shipInventory.GetCurrentWeight() : 0f;
            float maxWeight = shipInventory != null ? shipInventory.GetMaxCapacity() : 0f;
            kilogramText.text = maxWeight > 0f
                ? $"Peixes: {currentWeight:0.#}/{maxWeight:0.#} kg"
                : $"Peixes: {currentWeight:0.#} kg";
            kilogramText.color = GetWeightTextColor(currentWeight, maxWeight);
        }

        if (baitInventoryText != null)
            baitInventoryText.text = string.Empty;

        if (equippedBaitText != null)
            equippedBaitText.text = string.Empty;

        ValidateSelectedFishIndexes();
        UpdateDiscardControls();
        RefreshFishGrid();
    }

    private void SetBaitInventoryTexts()
    {
        TMP_Text targetText = baitInventoryText != null ? baitInventoryText : inventoryText;
        IReadOnlyList<BaitStack> stacks = baitInventory != null ? baitInventory.BaitStacks : null;
        bool hasBaits = stacks != null && stacks.Count > 0;
        bool hasGridSlots = HasBaitGridSlots() || (autoCreateBaitGridSlots && GetBaitSlotTemplate() != null);

        if (targetText != null)
            targetText.text = hasGridSlots && hasBaits ? string.Empty : GetBaitInventoryText();

        string equippedText = GetEquippedBaitText();

        if (kilogramText != null && hasStoredKilogramTextColor)
            kilogramText.color = kilogramTextDefaultColor;

        if (equippedBaitText != null)
            equippedBaitText.text = equippedText;
        else if (kilogramText != null)
            kilogramText.text = equippedText;

        RefreshBaitButtons();
        RefreshBaitGrid();
    }

    private void RefreshFishGrid()
    {
        List<FishData> ownedFish = shipInventory != null ? shipInventory.OwnedFish : null;
        int fishCount = ownedFish != null ? ownedFish.Count : 0;
        EnsureFishGridCapacity(fishCount);

        if (!HasFishGridSlots())
        {
            RefreshDiscardFishGrid();
            return;
        }

        for (int i = 0; i < fishGridSlots.Length; i++)
        {
            InventoryFishSlotUI slot = fishGridSlots[i];

            if (slot == null)
                continue;

            if (i < fishCount)
            {
                if (!slot.gameObject.activeSelf)
                    slot.gameObject.SetActive(true);

                slot.SetFish(ownedFish[i], i, HandleFishSlotSubmitted, isDiscardMode, selectedFishIndexes.Contains(i));
            }
            else if (autoCreateFishGridSlots && fishSlotTemplate != null)
            {
                slot.Clear();
                slot.gameObject.SetActive(false);
            }
            else
            {
                slot.Clear();
            }
        }

        ConfigureFishSlotNavigation(fishGridSlots, fishCount, GetFishGridContent(), GetFishGridExitUp(), GetFishGridExitDown());
        RefreshDiscardFishGrid();
    }

    private void RefreshDiscardFishGrid()
    {
        if (!isDiscardMode)
            return;

        List<FishData> ownedFish = shipInventory != null ? shipInventory.OwnedFish : null;
        int fishCount = ownedFish != null ? ownedFish.Count : 0;
        EnsureDiscardFishGridCapacity(fishCount);

        if (!HasDiscardFishGridSlots())
            return;

        for (int i = 0; i < discardFishGridSlots.Length; i++)
        {
            InventoryFishSlotUI slot = discardFishGridSlots[i];

            if (slot == null)
                continue;

            if (i < fishCount)
            {
                if (!slot.gameObject.activeSelf)
                    slot.gameObject.SetActive(true);

                slot.SetFish(ownedFish[i], i, HandleFishSlotSubmitted, true, selectedFishIndexes.Contains(i));
            }
            else if (autoCreateDiscardFishGridSlots && discardFishSlotTemplate != null)
            {
                slot.Clear();
                slot.gameObject.SetActive(false);
            }
            else
            {
                slot.Clear();
            }
        }

        ConfigureFishSlotNavigation(discardFishGridSlots, fishCount, GetDiscardFishGridContent(), GetDiscardGridExitUp(), GetDiscardGridExitDown());
    }

    private void RefreshBaitGrid()
    {
        IReadOnlyList<BaitStack> stacks = baitInventory != null ? baitInventory.BaitStacks : null;
        int stackCount = stacks != null ? stacks.Count : 0;
        EnsureBaitGridCapacity(stackCount);

        if (!HasBaitGridSlots())
            return;

        for (int i = 0; i < baitGridSlots.Length; i++)
        {
            InventoryBaitSlotUI slot = baitGridSlots[i];

            if (slot == null)
                continue;

            if (i >= stackCount)
            {
                slot.Clear();

                if (autoCreateBaitGridSlots && baitSlotTemplate != null)
                    slot.gameObject.SetActive(false);

                continue;
            }

            if (!slot.gameObject.activeSelf)
                slot.gameObject.SetActive(true);

            BaitStack stack = stacks[i];
            bool isEquipped = baitInventory != null &&
                              stack != null &&
                              stack.bait != null &&
                              baitInventory.EquippedBait != null &&
                              BaitCatalog.BaitIdMatches(stack.bait, baitInventory.EquippedBait.SaveId);

            slot.SetBait(stack, isEquipped, TryEquipBait);
        }
    }

    private string GetFishInventoryText()
    {
        if (shipInventory == null || shipInventory.OwnedFish == null || shipInventory.OwnedFish.Count == 0)
            return "Nenhum peixe no barco.";

        StringBuilder builder = new StringBuilder();

        foreach (FishData fish in shipInventory.OwnedFish)
        {
            if (fish == null || fish.typeOfFish == null)
                continue;

            builder.Append(fish.typeOfFish.fishName);
            builder.Append(", peso: ");
            builder.Append(fish.weight);
            builder.AppendLine(" kg");
            builder.AppendLine();
        }

        return builder.ToString();
    }

    private string GetBaitInventoryText()
    {
        if (baitInventory == null || baitInventory.BaitStacks.Count == 0)
            return "Nenhuma isca no inventario.";

        StringBuilder builder = new StringBuilder();

        foreach (BaitStack stack in baitInventory.BaitStacks)
        {
            if (stack == null || stack.bait == null || stack.quantity <= 0)
                continue;

            bool isEquipped = baitInventory.EquippedBait != null &&
                              BaitCatalog.BaitIdMatches(stack.bait, baitInventory.EquippedBait.SaveId);

            builder.Append(stack.bait.BaitName);
            builder.Append(" x");
            builder.Append(stack.quantity);

            if (isEquipped)
                builder.Append(" (equipada)");

            builder.AppendLine();
        }

        builder.AppendLine();
        builder.AppendLine("Equipe uma isca pelos botoes da aba de iscas.");
        return builder.ToString();
    }

    private string GetEquippedBaitText()
    {
        if (baitInventory == null || baitInventory.EquippedBait == null)
            return "Isca equipada: nenhuma";

        return $"Isca equipada: {baitInventory.EquippedBait.BaitName}";
    }

    private void StoreKilogramTextColor()
    {
        if (hasStoredKilogramTextColor || kilogramText == null)
            return;

        kilogramTextDefaultColor = kilogramText.color;
        hasStoredKilogramTextColor = true;
    }

    private Color GetWeightTextColor(float _currentWeight, float _maxWeight)
    {
        if (_maxWeight <= 0f)
            return hasStoredKilogramTextColor ? kilogramTextDefaultColor : weightNormalColor;

        float fillPercent = Mathf.Clamp01(_currentWeight / _maxWeight);

        if (fillPercent >= fullWeightPercent)
            return weightFullColor;

        if (fillPercent >= almostFullWeightPercent)
            return weightAlmostFullColor;

        return weightNormalColor;
    }

    #endregion

    #region Discard Flow

    private void HandleFishSlotSubmitted(int _fishIndex)
    {
        if (!isDiscardMode)
            return;

        if (selectedFishIndexes.Contains(_fishIndex))
            selectedFishIndexes.Remove(_fishIndex);
        else
            selectedFishIndexes.Add(_fishIndex);

        UpdateDiscardControls();
        RefreshFishGrid();
    }

    private void SetDiscardMode(bool _enabled)
    {
        if (isDiscardMode == _enabled)
        {
            UpdateDiscardControls();
            return;
        }

        isDiscardMode = _enabled;
        selectedFishIndexes.Clear();
        SetDiscardConfirmPanelVisible(false);
        UpdateDiscardControls();

        if (currentTab == InventoryTab.Fish)
            RefreshFishGrid();
    }

    private void SelectDiscardModeControl()
    {
        Selectable target = UISelectionHelper.FirstUsable(discardModePanel);

        if (target == null)
            target = UISelectionHelper.FirstUsable(fishTabPanel);

        if (target == null)
            target = cancelDiscardButton;

        UISelectionHelper.Select(target, inventoryRoot);
    }

    private void DiscardSelectedFish()
    {
        if (shipInventory == null || selectedFishIndexes.Count == 0)
        {
            SetDiscardMode(false);
            return;
        }

        List<int> indexes = new List<int>(selectedFishIndexes);
        indexes.Sort((left, right) => right.CompareTo(left));

        for (int i = 0; i < indexes.Count; i++)
            shipInventory.TryRemoveFishAt(indexes[i], out _);

        SetDiscardMode(false);
        RefreshInventoryTexts();
        UISelectionHelper.Select(discardModeButton, inventoryRoot);
    }

    private void ValidateSelectedFishIndexes()
    {
        int fishCount = shipInventory != null && shipInventory.OwnedFish != null ? shipInventory.OwnedFish.Count : 0;

        if (selectedFishIndexes.Count == 0)
            return;

        List<int> invalidIndexes = null;

        foreach (int index in selectedFishIndexes)
        {
            if (index < 0 || index >= fishCount)
            {
                invalidIndexes ??= new List<int>();
                invalidIndexes.Add(index);
            }
        }

        if (invalidIndexes == null)
            return;

        for (int i = 0; i < invalidIndexes.Count; i++)
            selectedFishIndexes.Remove(invalidIndexes[i]);
    }

    private void UpdateDiscardControls()
    {
        bool isFishTab = currentTab == InventoryTab.Fish;
        bool hasFish = shipInventory != null && shipInventory.OwnedFish != null && shipInventory.OwnedFish.Count > 0;
        bool isDiscardFlowActive = isFishTab && isDiscardMode;
        bool showDiscardMode = isDiscardFlowActive && !isDiscardConfirmVisible;

        SetButtonVisible(discardModeButton, isFishTab && !isDiscardMode);
        SetButtonVisible(confirmDiscardButton, showDiscardMode);
        SetButtonVisible(cancelDiscardButton, showDiscardMode);
        SetObjectActive(discardModePanel, showDiscardMode);

        if (isFishTab)
        {
            SetObjectsActive(hideWhileDiscardMode, !isDiscardFlowActive);
            SetObjectsActive(showWhileDiscardMode, showDiscardMode);
        }
        else
        {
            SetObjectsActive(showWhileDiscardMode, false);
        }

        SetButtonInteractable(discardModeButton, hasFish);
        SetButtonInteractable(confirmDiscardButton, selectedFishIndexes.Count > 0);
        SetButtonInteractable(cancelDiscardButton, true);

        if (discardConfirmText != null)
            discardConfirmText.text = string.Format(discardConfirmTextFormat, selectedFishIndexes.Count);
    }

    private void SetDiscardConfirmPanelVisible(bool _visible)
    {
        isDiscardConfirmVisible = _visible;
        SetObjectActive(discardConfirmPanel, _visible);
        UpdateDiscardControls();

        if (!_visible)
            return;

        if (discardConfirmText != null)
            discardConfirmText.text = string.Format(discardConfirmTextFormat, selectedFishIndexes.Count);
    }

    private void SetButtonVisible(Button _button, bool _visible)
    {
        if (_button != null)
            _button.gameObject.SetActive(_visible);
    }

    private void SetObjectsActive(GameObject[] _targets, bool _active)
    {
        if (_targets == null)
            return;

        for (int i = 0; i < _targets.Length; i++)
            SetObjectActive(_targets[i], _active);
    }

    private void SetTabObjectsActive(GameObject[] _targets, GameObject _fallback, bool _active)
    {
        bool hasFallback = _fallback != null;

        if (hasFallback)
            SetObjectActive(_fallback, _active);

        if (_targets != null)
        {
            for (int i = 0; i < _targets.Length; i++)
            {
                if (_targets[i] == null)
                    continue;

                if (hasFallback &&
                    (_targets[i] == _fallback || _targets[i].transform.IsChildOf(_fallback.transform)))
                {
                    continue;
                }

                SetObjectActive(_targets[i], _active);
            }
        }

        if (!hasFallback)
            SetObjectActive(_fallback, _active);
    }

    private void EnforceExclusiveTabVisibility()
    {
        if (currentTab == InventoryTab.Baits)
        {
            ForceCloseFishInventoryPanel();
            return;
        }

        if (currentTab == InventoryTab.Fish)
            ForceCloseBaitInventoryPanel();
    }

    private void ForceCloseFishInventoryPanel()
    {
        GameObject fishRoot = ResolveTabPanelRoot(fishTabPanel, "FishInventory", "FishInventoryPanel");
        SetObjectActive(fishRoot, false);

        if (fishRoot != fishTabPanel)
            SetObjectActive(fishTabPanel, false);
    }

    private void ForceCloseBaitInventoryPanel()
    {
        GameObject baitRoot = ResolveTabPanelRoot(baitsTabPanel, "BaitInventory", "BaitInventoryPanel", "BaitsInventoryPanel");
        SetObjectActive(baitRoot, false);

        if (baitRoot != baitsTabPanel)
            SetObjectActive(baitsTabPanel, false);
    }

    #endregion

    #region Bait Equipping

    private void TryEquipBaitSlot(int _slotIndex)
    {
        InitializeReferences();

        BaitData bait = GetBaitSlot(_slotIndex);

        if (baitInventory != null && bait != null)
            ToggleEquippedBait(bait);

        SetInventoryTab(InventoryTab.Baits);
        SelectBaitGridSlot(bait);
    }

    private void TryEquipBait(BaitData _bait)
    {
        InitializeReferences();

        if (baitInventory != null && _bait != null)
            ToggleEquippedBait(_bait);

        SetInventoryTab(InventoryTab.Baits);
        SelectBaitGridSlot(_bait);
    }

    private bool SelectBaitGridSlot(BaitData _bait)
    {
        if (_bait == null || !IsInventoryVisible() || baitGridSlots == null)
            return false;

        for (int i = 0; i < baitGridSlots.Length; i++)
        {
            InventoryBaitSlotUI slot = baitGridSlots[i];

            if (slot == null ||
                slot.CurrentBait == null ||
                !IsSameBait(slot.CurrentBait, _bait))
            {
                continue;
            }

            Selectable selectable = slot.SlotSelectable;

            if (!UISelectionHelper.IsUsable(selectable))
                continue;

            UISelectionHelper.Select(selectable, inventoryRoot);
            return true;
        }

        return false;
    }

    private bool IsSameBait(BaitData _left, BaitData _right)
    {
        if (_left == null || _right == null)
            return false;

        return _left == _right ||
               BaitCatalog.BaitIdMatches(_left, _right.SaveId) ||
               BaitCatalog.BaitIdMatches(_right, _left.SaveId);
    }

    private void RefreshBaitButtons()
    {
        if (baitEquipButtons != null)
        {
            for (int i = 0; i < baitEquipButtons.Length; i++)
            {
                if (baitEquipButtons[i] == null)
                    continue;

                BaitData bait = GetBaitSlot(i);
                bool isEquipped = IsBaitEquipped(bait);
                baitEquipButtons[i].interactable = baitInventory != null &&
                                                   bait != null &&
                                                   baitInventory.HasBait(bait);
                SetButtonText(
                    baitEquipButtons[i],
                    bait != null
                        ? $"{(isEquipped ? "Remover" : "Equipar")} {bait.BaitName}"
                        : "Equipar"
                );
            }
        }

        if (HasDedicatedClearBaitButton())
        {
            clearBaitButton.interactable = baitInventory != null && baitInventory.EquippedBait != null;
            SetButtonText(clearBaitButton, "Sem isca");
        }
    }

    private BaitData GetBaitSlot(int _slotIndex)
    {
        BaitData[] slots = BaitCatalog.GetBaitsOrDefault(baitSlots);

        if (slots == null || _slotIndex < 0 || _slotIndex >= slots.Length)
            return null;

        return slots[_slotIndex];
    }

    private void ToggleEquippedBait(BaitData _bait)
    {
        if (baitInventory == null || _bait == null)
            return;

        if (IsBaitEquipped(_bait))
        {
            baitInventory.ClearEquippedBait();
            return;
        }

        baitInventory.EquipBait(_bait);
    }

    private bool IsBaitEquipped(BaitData _bait)
    {
        return baitInventory != null &&
               baitInventory.EquippedBait != null &&
               _bait != null &&
               BaitCatalog.BaitIdMatches(_bait, baitInventory.EquippedBait.SaveId);
    }

    #endregion

    #region Modal And Runtime Controls

    private bool IsInventoryVisible()
    {
        if (inventoryRoot == null)
            return false;

        if (inventoryRoot == gameObject)
            return isInventoryVisible;

        return inventoryRoot.activeSelf;
    }

    private bool CanOpenInventory()
    {
        if (GameManager.instance == null)
            return true;

        return GameManager.instance.currentState == GameManager.GameState.OnFoot ||
               GameManager.instance.currentState == GameManager.GameState.OnBoat;
    }

    private void StoreGameStateBeforeInventory()
    {
        if (GameManager.instance == null)
            return;

        stateBeforeInventory = GameManager.instance.currentState;
        hasStoredStateBeforeInventory = true;
        GameManager.instance.SetState(GameManager.GameState.InUI);
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
            HandleInventoryBack
        );

        modalToken = UIModalManager.PushModal(request);
    }

    private void RestoreGameStateAfterInventory()
    {
        if (!hasStoredStateBeforeInventory || GameManager.instance == null)
            return;

        if (GameManager.instance.currentState == GameManager.GameState.InUI)
            GameManager.instance.SetState(stateBeforeInventory);

        hasStoredStateBeforeInventory = false;
    }

    private void EnsureRuntimeControls()
    {
        if (inventoryRoot == null)
            return;

        Transform parent = GetRuntimeControlsParent();

        if (!allowRuntimeFallback)
        {
            LogMissingRuntimeControls();

            if (baitInventoryText == null)
                baitInventoryText = inventoryText;

            if (equippedBaitText == null)
                equippedBaitText = kilogramText;

            SetBaitControlsVisible(currentTab == InventoryTab.Baits);
            return;
        }

        if (fishTabButton == null)
            fishTabButton = CreateRuntimeButton("FishTabButton", parent, new Vector2(-170f, 325f), new Vector2(150f, 48f), "Peixes");

        if (baitsTabButton == null)
            baitsTabButton = CreateRuntimeButton("BaitsTabButton", parent, new Vector2(0f, 325f), new Vector2(150f, 48f), "Iscas");

        if (baitEquipButtons == null || baitEquipButtons.Length < 3)
        {
            Button[] resizedButtons = new Button[3];

            if (baitEquipButtons != null)
            {
                for (int i = 0; i < baitEquipButtons.Length && i < resizedButtons.Length; i++)
                    resizedButtons[i] = baitEquipButtons[i];
            }

            baitEquipButtons = resizedButtons;
        }

        Vector2[] buttonPositions =
        {
            new Vector2(-250f, -255f),
            new Vector2(0f, -255f),
            new Vector2(250f, -255f)
        };

        for (int i = 0; i < baitEquipButtons.Length; i++)
        {
            if (baitEquipButtons[i] == null)
            {
                BaitData bait = GetBaitSlot(i);
                string label = bait != null ? $"Equipar {bait.BaitName}" : "Equipar";
                baitEquipButtons[i] = CreateRuntimeButton($"EquipBaitButton{i + 1}", parent, buttonPositions[i], new Vector2(220f, 46f), label);
            }
        }

        if (clearBaitButton == null)
            clearBaitButton = CreateRuntimeButton("ClearBaitButton", parent, new Vector2(250f, 325f), new Vector2(150f, 48f), "Sem isca");

        if (baitInventoryText == null)
            baitInventoryText = inventoryText;

        if (equippedBaitText == null)
            equippedBaitText = kilogramText;

        SetBaitControlsVisible(currentTab == InventoryTab.Baits);
    }

    private void LogMissingRuntimeControls()
    {
        if (!logMissingReferences || hasLoggedMissingRuntimeControls)
            return;

        List<string> missingReferences = new List<string>();

        if (fishTabButton == null)
            missingReferences.Add("FishTabButton");

        if (baitsTabButton == null)
            missingReferences.Add("BaitsTabButton");

        for (int i = 0; i < 3; i++)
        {
            if (baitEquipButtons == null || i >= baitEquipButtons.Length || baitEquipButtons[i] == null)
                missingReferences.Add($"EquipBaitButton{i + 1}");
        }

        if (missingReferences.Count == 0)
            return;

        Debug.LogWarning($"[InvertoryManager] Referencias de UI faltando: {string.Join(", ", missingReferences)}. Crie esses botoes na cena/prefab ou arraste no Inspector. Ative Allow Runtime Fallback apenas se quiser cria-los em runtime.", this);
        hasLoggedMissingRuntimeControls = true;
    }

    private Transform GetRuntimeControlsParent()
    {
        if (kilogramText != null)
            return kilogramText.transform.parent;

        if (inventoryText != null)
            return inventoryText.transform.parent;

        return inventoryRoot.transform;
    }

    private Button CreateRuntimeButton(string _name, Transform _parent, Vector2 _position, Vector2 _size, string _label)
    {
        GameObject buttonObject = new GameObject(_name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(_parent, false);

        RectTransform rectTransform = buttonObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = _position;
        rectTransform.sizeDelta = _size;

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.08f, 0.08f, 0.08f, 0.85f);

        GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(buttonObject.transform, false);

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        TextMeshProUGUI buttonText = textObject.GetComponent<TextMeshProUGUI>();
        buttonText.text = _label;
        buttonText.fontSize = 22f;
        buttonText.alignment = TextAlignmentOptions.Center;
        buttonText.color = Color.white;

        return buttonObject.GetComponent<Button>();
    }

    private void SetBaitControlsVisible(bool _visible)
    {
        bool showLegacyBaitControls = _visible && !HasBaitGridSlots();

        if (baitEquipButtons != null)
        {
            for (int i = 0; i < baitEquipButtons.Length; i++)
            {
                if (baitEquipButtons[i] != null)
                    baitEquipButtons[i].gameObject.SetActive(showLegacyBaitControls);
            }
        }

        if (HasDedicatedClearBaitButton())
            clearBaitButton.gameObject.SetActive(_visible && showClearBaitButton);
    }

    #endregion

    #region Button Binding And Selection

    private void BindButtons()
    {
        if (areButtonsBound)
            return;

        if (fishTabButton != null)
            fishTabButton.onClick.AddListener(OnClickFishTab);

        if (baitsTabButton != null)
            baitsTabButton.onClick.AddListener(OnClickBaitsTab);

        BindBaitButton(0, OnClickEquipBaitSlot0);
        BindBaitButton(1, OnClickEquipBaitSlot1);
        BindBaitButton(2, OnClickEquipBaitSlot2);

        if (HasDedicatedClearBaitButton())
            clearBaitButton.onClick.AddListener(OnClickClearEquippedBait);

        if (discardModeButton != null)
            discardModeButton.onClick.AddListener(OnClickStartDiscardMode);

        if (confirmDiscardButton != null)
            confirmDiscardButton.onClick.AddListener(OnClickConfirmDiscardSelection);

        if (cancelDiscardButton != null)
            cancelDiscardButton.onClick.AddListener(OnClickCancelDiscardMode);

        if (discardConfirmYesButton != null)
            discardConfirmYesButton.onClick.AddListener(OnClickConfirmDiscard);

        if (discardConfirmNoButton != null)
            discardConfirmNoButton.onClick.AddListener(OnClickCancelDiscardConfirm);

        areButtonsBound = true;
    }

    private void UnbindButtons()
    {
        if (!areButtonsBound)
            return;

        if (fishTabButton != null)
            fishTabButton.onClick.RemoveListener(OnClickFishTab);

        if (baitsTabButton != null)
            baitsTabButton.onClick.RemoveListener(OnClickBaitsTab);

        UnbindBaitButton(0, OnClickEquipBaitSlot0);
        UnbindBaitButton(1, OnClickEquipBaitSlot1);
        UnbindBaitButton(2, OnClickEquipBaitSlot2);

        if (HasDedicatedClearBaitButton())
            clearBaitButton.onClick.RemoveListener(OnClickClearEquippedBait);

        if (discardModeButton != null)
            discardModeButton.onClick.RemoveListener(OnClickStartDiscardMode);

        if (confirmDiscardButton != null)
            confirmDiscardButton.onClick.RemoveListener(OnClickConfirmDiscardSelection);

        if (cancelDiscardButton != null)
            cancelDiscardButton.onClick.RemoveListener(OnClickCancelDiscardMode);

        if (discardConfirmYesButton != null)
            discardConfirmYesButton.onClick.RemoveListener(OnClickConfirmDiscard);

        if (discardConfirmNoButton != null)
            discardConfirmNoButton.onClick.RemoveListener(OnClickCancelDiscardConfirm);

        areButtonsBound = false;
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

    private void ConfigureFishSlotNavigation(
        InventoryFishSlotUI[] _slots,
        int _itemCount,
        Transform _content,
        Selectable _exitUp,
        Selectable _exitDown)
    {
        if (_slots == null || _itemCount <= 0)
            return;

        int columns = GetGridColumnCount(_content, _itemCount);
        int slotCount = Mathf.Min(_itemCount, _slots.Length);

        for (int i = 0; i < slotCount; i++)
        {
            Selectable current = GetFishSlotSelectable(_slots, i, slotCount);

            if (current == null)
                continue;

            int column = i % columns;
            Navigation navigation = current.navigation;
            navigation.mode = Navigation.Mode.Explicit;
            navigation.selectOnLeft = column > 0 ? GetFishSlotSelectable(_slots, i - 1, slotCount) : null;
            navigation.selectOnRight = column < columns - 1 ? GetFishSlotSelectable(_slots, i + 1, slotCount) : null;
            navigation.selectOnUp = i - columns >= 0 ? GetFishSlotSelectable(_slots, i - columns, slotCount) : _exitUp;
            navigation.selectOnDown = i + columns < slotCount ? GetFishSlotSelectable(_slots, i + columns, slotCount) : _exitDown;
            current.navigation = navigation;
        }
    }

    private Selectable GetFishSlotSelectable(InventoryFishSlotUI[] _slots, int _index, int _slotCount)
    {
        if (_slots == null || _index < 0 || _index >= _slotCount || _index >= _slots.Length || _slots[_index] == null)
            return null;

        Selectable selectable = _slots[_index].SlotSelectable;
        return UISelectionHelper.IsUsable(selectable) ? selectable : null;
    }

    private int GetGridColumnCount(Transform _content, int _itemCount)
    {
        if (_itemCount <= 1)
            return 1;

        GridLayoutGroup grid = _content != null ? _content.GetComponent<GridLayoutGroup>() : null;

        if (grid == null && _content != null)
            grid = _content.GetComponentInParent<GridLayoutGroup>();

        if (grid == null)
            return 1;

        if (grid.constraint == GridLayoutGroup.Constraint.FixedColumnCount)
            return Mathf.Max(1, grid.constraintCount);

        if (grid.constraint == GridLayoutGroup.Constraint.FixedRowCount)
            return Mathf.Max(1, Mathf.CeilToInt((float)_itemCount / Mathf.Max(1, grid.constraintCount)));

        RectTransform contentRect = _content as RectTransform;
        float cellWidth = grid.cellSize.x + grid.spacing.x;

        if (contentRect == null || cellWidth <= 0f)
            return 1;

        return Mathf.Max(1, Mathf.FloorToInt((contentRect.rect.width + grid.spacing.x) / cellWidth));
    }

    private Selectable GetFishGridExitUp()
    {
        if (UISelectionHelper.IsUsable(fishTabButton))
            return fishTabButton;

        return UISelectionHelper.IsUsable(baitsTabButton) ? baitsTabButton : null;
    }

    private Selectable GetFishGridExitDown()
    {
        if (UISelectionHelper.IsUsable(discardModeButton))
            return discardModeButton;

        return UISelectionHelper.IsUsable(baitsTabButton) ? baitsTabButton : null;
    }

    private Selectable GetDiscardGridExitUp()
    {
        if (UISelectionHelper.IsUsable(confirmDiscardButton))
            return confirmDiscardButton;

        return UISelectionHelper.IsUsable(cancelDiscardButton) ? cancelDiscardButton : null;
    }

    private Selectable GetDiscardGridExitDown()
    {
        if (UISelectionHelper.IsUsable(cancelDiscardButton))
            return cancelDiscardButton;

        return UISelectionHelper.IsUsable(confirmDiscardButton) ? confirmDiscardButton : null;
    }

    private void SelectCurrentTabControl()
    {
        Selectable target = currentTab switch
        {
            InventoryTab.Fish => GetFishTabSelectable(),
            InventoryTab.Baits => GetBaitTabSelectable(),
            _ => null
        };

        UISelectionHelper.Select(target, inventoryRoot);
    }

    private void EnsureCurrentSelectionIsUsable()
    {
        if (!IsInventoryVisible())
            return;

        Selectable current = UISelectionHelper.CurrentSelectableInScope(GetActiveSelectionScope());

        if (UISelectionHelper.IsUsable(current))
            return;

        SelectCurrentTabControl();
    }

    private void KeepCurrentSelectionVisible()
    {
        if (!keepSelectionVisibleInScroll || !IsInventoryVisible() || EventSystem.current == null)
            return;

        GameObject selectedObject = EventSystem.current.currentSelectedGameObject;

        if (selectedObject == null)
        {
            lastScrolledSelection = null;
            return;
        }

        ScrollRect scrollRect = GetScrollRectForSelection(selectedObject);

        if (scrollRect == null || scrollRect.content == null)
            return;

        RectTransform selectedRect = selectedObject.GetComponent<RectTransform>();

        if (selectedRect == null || !selectedRect.IsChildOf(scrollRect.content))
            return;

        Canvas.ForceUpdateCanvases();
        ScrollRectToChild(scrollRect, selectedRect, selectedObject != lastScrolledSelection);
        lastScrolledSelection = selectedObject;
    }

    private ScrollRect GetScrollRectForSelection(GameObject _selectedObject)
    {
        if (_selectedObject == null)
            return null;

        if (discardConfirmPanel != null && discardConfirmPanel.activeInHierarchy)
            return null;

        if (isDiscardMode && discardModePanel != null && discardModePanel.activeInHierarchy)
        {
            if (discardFishScrollRect != null && _selectedObject.transform.IsChildOf(discardFishScrollRect.transform))
                return discardFishScrollRect;
        }

        if (currentTab == InventoryTab.Fish &&
            fishScrollRect != null &&
            _selectedObject.transform.IsChildOf(fishScrollRect.transform))
        {
            return fishScrollRect;
        }

        if (currentTab == InventoryTab.Baits &&
            baitScrollRect != null &&
            _selectedObject.transform.IsChildOf(baitScrollRect.transform))
        {
            return baitScrollRect;
        }

        return null;
    }

    private void ScrollRectToChild(ScrollRect _scrollRect, RectTransform _child, bool _selectionChanged)
    {
        UISelectionHelper.ConfigureVerticalOnlyScrollRect(_scrollRect);

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

        if (childBounds.max.y > viewportRect.yMax - selectionScrollPadding)
            offset = childBounds.max.y - (viewportRect.yMax - selectionScrollPadding);
        else if (childBounds.min.y < viewportRect.yMin + selectionScrollPadding)
            offset = childBounds.min.y - (viewportRect.yMin + selectionScrollPadding);

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

        if (childBounds.max.x > viewportRect.xMax - selectionScrollPadding)
            offset = childBounds.max.x - (viewportRect.xMax - selectionScrollPadding);
        else if (childBounds.min.x < viewportRect.xMin + selectionScrollPadding)
            offset = childBounds.min.x - (viewportRect.xMin + selectionScrollPadding);

        if (Mathf.Abs(offset) <= 0.01f)
            return;

        float target = Mathf.Clamp01(_scrollRect.horizontalNormalizedPosition + offset / hiddenWidth);
        _scrollRect.horizontalNormalizedPosition = _selectionChanged
            ? target
            : Mathf.MoveTowards(_scrollRect.horizontalNormalizedPosition, target, Time.unscaledDeltaTime * 12f);
    }

    private void RecoverSelectionFromMoveInput()
    {
        if (!IsInventoryVisible() || InputHandler.instance == null)
        {
            wasSelectionRecoveryMoveHeld = false;
            return;
        }

        bool isMovePressed = InputHandler.instance.moveInput.sqrMagnitude >= selectionRecoveryMoveThreshold * selectionRecoveryMoveThreshold;

        if (!isMovePressed)
        {
            wasSelectionRecoveryMoveHeld = false;
            return;
        }

        if (wasSelectionRecoveryMoveHeld)
            return;

        wasSelectionRecoveryMoveHeld = true;

        Selectable current = UISelectionHelper.CurrentSelectableInScope(GetActiveSelectionScope());

        if (UISelectionHelper.IsUsable(current))
            return;

        SelectCurrentTabControl();
    }

    private GameObject GetActiveSelectionScope()
    {
        if (discardConfirmPanel != null && discardConfirmPanel.activeInHierarchy)
            return discardConfirmPanel;

        if (isDiscardMode && discardModePanel != null && discardModePanel.activeInHierarchy)
            return discardModePanel;

        return inventoryRoot;
    }

    private Selectable GetFishTabSelectable()
    {
        if (discardConfirmPanel != null && discardConfirmPanel.activeInHierarchy)
            return UISelectionHelper.IsUsable(discardConfirmYesButton) ? discardConfirmYesButton : discardConfirmNoButton;

        if (isDiscardMode && discardModePanel != null && discardModePanel.activeInHierarchy)
        {
            Selectable firstInDiscardPanel = UISelectionHelper.FirstUsable(discardModePanel);

            if (firstInDiscardPanel != null)
                return firstInDiscardPanel;
        }

        if (UISelectionHelper.IsUsable(fishFirstSelected))
            return fishFirstSelected;

        Selectable firstInPanel = UISelectionHelper.FirstUsable(fishTabPanel);

        if (firstInPanel != null)
            return firstInPanel;

        return baitsTabButton;
    }

    private Selectable GetBaitTabSelectable()
    {
        if (UISelectionHelper.IsUsable(baitsFirstSelected))
            return baitsFirstSelected;

        Selectable firstInPanel = UISelectionHelper.FirstUsable(baitsTabPanel);

        if (firstInPanel != null)
            return firstInPanel;

        if (baitEquipButtons != null)
        {
            for (int i = 0; i < baitEquipButtons.Length; i++)
            {
                if (UISelectionHelper.IsUsable(baitEquipButtons[i]))
                    return baitEquipButtons[i];
            }
        }

        if (HasDedicatedClearBaitButton() && UISelectionHelper.IsUsable(clearBaitButton))
            return clearBaitButton;

        return fishTabButton;
    }

    private bool HasDedicatedClearBaitButton()
    {
        return clearBaitButton != null &&
               clearBaitButton != fishTabButton &&
               clearBaitButton != baitsTabButton;
    }

    private void SetButtonText(Button _button, string _text)
    {
        if (_button == null)
            return;

        TMP_Text buttonText = _button.GetComponentInChildren<TMP_Text>(true);

        if (buttonText != null)
            buttonText.text = _text;
    }

    private void BindBaitButton(int _index, UnityEngine.Events.UnityAction _action)
    {
        if (baitEquipButtons == null || _index < 0 || _index >= baitEquipButtons.Length || baitEquipButtons[_index] == null)
            return;

        baitEquipButtons[_index].onClick.AddListener(_action);
    }

    private void UnbindBaitButton(int _index, UnityEngine.Events.UnityAction _action)
    {
        if (baitEquipButtons == null || _index < 0 || _index >= baitEquipButtons.Length || baitEquipButtons[_index] == null)
            return;

        baitEquipButtons[_index].onClick.RemoveListener(_action);
    }

    #endregion
}
