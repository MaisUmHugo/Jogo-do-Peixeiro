using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InvertoryManager : MonoBehaviour
{
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

    [Header("Fish")]
    [SerializeField] private TMP_Text inventoryText;
    [SerializeField] private TMP_Text kilogramText;
    [SerializeField] private InventoryFishSlotUI[] fishGridSlots;

    [Header("Baits")]
    [SerializeField] private TMP_Text baitInventoryText;
    [SerializeField] private TMP_Text equippedBaitText;
    [SerializeField] private Button[] baitEquipButtons;
    [SerializeField] private Button clearBaitButton;
    [SerializeField] private BaitData[] baitSlots;
    [SerializeField] private InventoryBaitSlotUI[] baitGridSlots;

    [Header("Settings")]
    [SerializeField] private bool closeOnAwake = true;
    [SerializeField] private bool allowRuntimeFallback;
    [SerializeField] private bool logMissingReferences = true;

    [Header("Navigation")]
    [SerializeField] private Selectable fishFirstSelected;
    [SerializeField] private Selectable baitsFirstSelected;

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

        if (closeOnAwake)
            SetInventoryVisible(false);
    }

    private void OnEnable()
    {
        InitializeReferences();
        EnsureRuntimeControls();
        BindButtons();
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

        if (!HasBaitGridSlots() && baitsTabPanel != null)
            baitGridSlots = baitsTabPanel.GetComponentsInChildren<InventoryBaitSlotUI>(true);
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

    public void CloseInventory()
    {
        bool wasVisible = IsInventoryVisible();
        SetInventoryVisible(false);
        UISelectionHelper.ClearSelection(inventoryRoot);

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

    public static bool TryCloseOpenInventory()
    {
        if (Instance == null)
            Instance = FindFirstObjectByType<InvertoryManager>(FindObjectsInactive.Include);

        return Instance != null && Instance.TryCloseInventory();
    }

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
        currentTab = _tab;

        SetObjectActive(fishTabPanel, currentTab == InventoryTab.Fish);
        SetObjectActive(baitsTabPanel, currentTab == InventoryTab.Baits);
        SetButtonInteractable(fishTabButton, currentTab != InventoryTab.Fish);
        SetButtonInteractable(baitsTabButton, currentTab != InventoryTab.Baits);
        SetBaitControlsVisible(currentTab == InventoryTab.Baits);
        RefreshInventoryTexts();

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
        bool hasGridSlots = HasFishGridSlots();

        if (inventoryText != null)
            inventoryText.text = hasGridSlots ? string.Empty : GetFishInventoryText();

        if (kilogramText != null)
        {
            float currentWeight = shipInventory != null ? shipInventory.GetCurrentWeight() : 0f;
            kilogramText.text = $"kilos de peixe: {currentWeight:0.#} Kg";
        }

        if (baitInventoryText != null)
            baitInventoryText.text = string.Empty;

        if (equippedBaitText != null)
            equippedBaitText.text = string.Empty;

        RefreshFishGrid();
    }

    private void SetBaitInventoryTexts()
    {
        TMP_Text targetText = baitInventoryText != null ? baitInventoryText : inventoryText;
        bool hasGridSlots = HasBaitGridSlots();

        if (targetText != null)
            targetText.text = hasGridSlots ? string.Empty : GetBaitInventoryText();

        string equippedText = GetEquippedBaitText();

        if (equippedBaitText != null)
            equippedBaitText.text = equippedText;
        else if (kilogramText != null)
            kilogramText.text = equippedText;

        RefreshBaitButtons();
        RefreshBaitGrid();
    }

    private void RefreshFishGrid()
    {
        if (!HasFishGridSlots())
            return;

        List<FishData> ownedFish = shipInventory != null ? shipInventory.OwnedFish : null;
        int fishCount = ownedFish != null ? ownedFish.Count : 0;

        for (int i = 0; i < fishGridSlots.Length; i++)
        {
            InventoryFishSlotUI slot = fishGridSlots[i];

            if (slot == null)
                continue;

            if (i < fishCount)
                slot.SetFish(ownedFish[i]);
            else
                slot.Clear();
        }
    }

    private void RefreshBaitGrid()
    {
        if (!HasBaitGridSlots())
            return;

        IReadOnlyList<BaitStack> stacks = baitInventory != null ? baitInventory.BaitStacks : null;
        int stackCount = stacks != null ? stacks.Count : 0;

        for (int i = 0; i < baitGridSlots.Length; i++)
        {
            InventoryBaitSlotUI slot = baitGridSlots[i];

            if (slot == null)
                continue;

            if (i >= stackCount)
            {
                slot.Clear();
                continue;
            }

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

    private void TryEquipBaitSlot(int _slotIndex)
    {
        InitializeReferences();

        BaitData bait = GetBaitSlot(_slotIndex);

        if (baitInventory != null && bait != null)
            baitInventory.EquipBait(bait);

        SetInventoryTab(InventoryTab.Baits);
    }

    private void TryEquipBait(BaitData _bait)
    {
        InitializeReferences();

        if (baitInventory != null && _bait != null)
            baitInventory.EquipBait(_bait);

        SetInventoryTab(InventoryTab.Baits);
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
                baitEquipButtons[i].interactable = baitInventory != null &&
                                                   bait != null &&
                                                   baitInventory.HasBait(bait);
                SetButtonText(
                    baitEquipButtons[i],
                    bait != null ? $"Equipar {bait.BaitName}" : "Equipar"
                );
            }
        }

        if (clearBaitButton != null)
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

        if (clearBaitButton == null)
            missingReferences.Add("ClearBaitButton");

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

        if (clearBaitButton != null)
            clearBaitButton.gameObject.SetActive(_visible);
    }

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

        if (clearBaitButton != null)
            clearBaitButton.onClick.AddListener(OnClickClearEquippedBait);

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

        if (clearBaitButton != null)
            clearBaitButton.onClick.RemoveListener(OnClickClearEquippedBait);

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

        Selectable current = UISelectionHelper.CurrentSelectableInScope(inventoryRoot);

        if (UISelectionHelper.IsUsable(current))
            return;

        SelectCurrentTabControl();
    }

    private Selectable GetFishTabSelectable()
    {
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

        if (UISelectionHelper.IsUsable(clearBaitButton))
            return clearBaitButton;

        return fishTabButton;
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
}
