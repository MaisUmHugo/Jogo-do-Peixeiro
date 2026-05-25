using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class FishCollectionUI : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject panel;
    [SerializeField] private Button closeButton;
    [SerializeField] private Selectable firstSelected;

    [Header("Fish List")]
    [SerializeField] private FishScriptableObject[] fishEntries;
    [SerializeField] private FishCollectionSlotUI[] slots;
    [SerializeField] private Transform slotContent;
    [SerializeField] private FishCollectionSlotUI slotTemplate;
    [SerializeField] private bool autoCreateMissingSlots = true;

    [Header("Scroll Follow")]
    [SerializeField] private bool keepSelectionVisibleInScroll = true;
    [SerializeField] private ScrollRect fishScrollRect;
    [SerializeField, Min(0f)] private float selectionScrollPadding = 24f;
    [SerializeField, Min(1f)] private float mouseWheelScrollPixels = 140f;
    [SerializeField, Min(0f)] private float manualScrollSelectionFollowDelay = 0.8f;

    [Header("Details")]
    [SerializeField] private TMP_Text selectedNameText;
    [SerializeField] private TMP_Text selectedPriceText;
    [SerializeField] private TMP_Text selectedWeightText;
    [SerializeField] private TMP_Text selectedDescriptionText;
    [SerializeField] private TMP_Text selectedCapturedText;
    [SerializeField] private TMP_Text selectedInfoText;
    [SerializeField] private FishPreviewPanelUI fishPreviewPanel;
    [SerializeField] private Button previewButton;
    [SerializeField] private string undiscoveredName = "???";
    [SerializeField] private string undiscoveredInfo = "Peixe ainda não descoberto.";

    private FishScriptableObject selectedFish;
    private bool selectedFishDiscovered;
    private GameObject previousPanelRoot;
    private bool hasBoundButtons;
    private int lastSlotSelectionFrame = -1;
    private GameObject lastScrolledSelection;
    private float suppressSelectionScrollUntil;

    private GameObject PanelObject => panel != null ? panel : gameObject;
    public bool IsOpen => PanelObject.activeSelf;

    private void Awake()
    {
        ResolveReferences();
        BindButtons();
        CloseImmediate();
    }

    private void OnEnable()
    {
        ResolveReferences();
        BindButtons();
        Refresh();
    }

    private void Update()
    {
        HandleMouseWheelScroll();
        KeepCurrentSelectionVisible();
    }

    private void OnValidate()
    {
        selectionScrollPadding = Mathf.Max(0f, selectionScrollPadding);
        mouseWheelScrollPixels = Mathf.Max(1f, mouseWheelScrollPixels);
        manualScrollSelectionFollowDelay = Mathf.Max(0f, manualScrollSelectionFollowDelay);
    }

    private void OnDestroy()
    {
        if (closeButton != null)
            closeButton.onClick.RemoveListener(Close);

        if (previewButton != null)
            previewButton.onClick.RemoveListener(OpenSelectedPreview);
    }

    public void Open()
    {
        Open(null);
    }

    public void Open(GameObject _previousPanelRoot)
    {
        previousPanelRoot = _previousPanelRoot;
        PanelObject.SetActive(true);
        Refresh(true);
        SelectInitialControl();
    }

    public void Close()
    {
        CloseImmediate();

        if (previousPanelRoot != null)
            previousPanelRoot.SetActive(true);

        UISelectionHelper.Select(UISelectionHelper.FirstUsable(previousPanelRoot), previousPanelRoot);
    }

    public void SelectFish(FishScriptableObject _fish, bool _discovered)
    {
        lastSlotSelectionFrame = Time.frameCount;
        selectedFish = _fish;
        selectedFishDiscovered = _discovered;
        UpdateSelectedDetails();
    }

    public void Refresh()
    {
        Refresh(false);
    }

    private void Refresh(bool _resetSelection)
    {
        ResolveReferences();
        EnsureSlotCapacity();

        for (int i = 0; i < slots.Length; i++)
        {
            FishCollectionSlotUI slot = slots[i];

            if (slot == null)
                continue;

            FishScriptableObject fish = fishEntries != null && i < fishEntries.Length ? fishEntries[i] : null;
            slot.SetFish(this, fish, FishCaptureHistory.IsDiscovered(fish));
        }

        if (_resetSelection || selectedFish == null)
            SelectFirstConfiguredFish();

        UpdateSelectedDetails();
    }

    public void OpenSelectedPreview()
    {
        if (lastSlotSelectionFrame == Time.frameCount)
            return;

        if (!selectedFishDiscovered || selectedFish == null || fishPreviewPanel == null)
            return;

        fishPreviewPanel.ShowFish(selectedFish, PanelObject);
    }

    private void CloseImmediate()
    {
        PanelObject.SetActive(false);
    }

    private void UpdateSelectedDetails()
    {
        if (selectedFish == null)
        {
            ClearSelectedTexts();
            SetPreviewButtonInteractable(false);
            return;
        }

        if (!selectedFishDiscovered)
        {
            SetText(selectedNameText, undiscoveredName);
            SetText(selectedPriceText, string.Empty);
            SetText(selectedWeightText, string.Empty);
            SetText(selectedDescriptionText, undiscoveredInfo);
            SetText(selectedCapturedText, string.Empty);
            SetText(selectedInfoText, undiscoveredInfo);
            SetPreviewButtonInteractable(false);
            return;
        }

        SetText(selectedNameText, GetFishDisplayName(selectedFish));
        SetText(selectedPriceText, $"Preço base: R$ {selectedFish.BasePrice}");
        SetText(selectedWeightText, $"Peso: {selectedFish.minWeight}-{selectedFish.maxWeight} kg");
        SetText(selectedDescriptionText, GetFishDescription(selectedFish));
        SetText(selectedCapturedText, $"Capturado: {FishCaptureHistory.GetCaptureCount(selectedFish)}");
        SetText(
            selectedInfoText,
            $"Preço base: R$ {selectedFish.BasePrice}\nPeso: {selectedFish.minWeight}-{selectedFish.maxWeight} kg\n{GetFishDescription(selectedFish)}\nCapturado: {FishCaptureHistory.GetCaptureCount(selectedFish)}"
        );
        SetPreviewButtonInteractable(fishPreviewPanel != null);
    }

    private void EnsureSlotCapacity()
    {
        int requiredCount = fishEntries != null ? fishEntries.Length : 0;

        if (requiredCount <= 0)
            return;

        if (slots == null)
            slots = new FishCollectionSlotUI[0];

        if (slots.Length >= requiredCount)
            return;

        if (!autoCreateMissingSlots || slotTemplate == null)
        {
            FishCollectionSlotUI[] resizedSlots = new FishCollectionSlotUI[requiredCount];

            for (int i = 0; i < slots.Length; i++)
                resizedSlots[i] = slots[i];

            slots = resizedSlots;
            return;
        }

        FishCollectionSlotUI[] createdSlots = new FishCollectionSlotUI[requiredCount];

        for (int i = 0; i < slots.Length; i++)
            createdSlots[i] = slots[i];

        Transform parent = slotContent != null ? slotContent : slotTemplate.transform.parent;

        for (int i = slots.Length; i < createdSlots.Length; i++)
        {
            FishCollectionSlotUI slot = Instantiate(slotTemplate, parent);
            slot.gameObject.SetActive(true);
            createdSlots[i] = slot;
        }

        slots = createdSlots;
    }

    private void ResolveReferences()
    {
        if (panel == null)
            panel = gameObject;

        if (closeButton == null)
            closeButton = FindChildComponent<Button>("CloseButton", "BackButton", "VoltarButton");

        if (previewButton == null)
            previewButton = FindChildComponent<Button>("PreviewButton", "FishPreviewButton", "VerPeixeButton");

        if (selectedNameText == null)
            selectedNameText = FindChildComponent<TMP_Text>("FishCollectionName", "SelectedFishNameText", "FishNameText", "NameText");

        if (selectedPriceText == null)
            selectedPriceText = FindChildComponent<TMP_Text>("FishCollectionPrice", "SelectedFishPriceText", "FishPriceText", "PriceText");

        if (selectedWeightText == null)
            selectedWeightText = FindChildComponent<TMP_Text>("FishCollectionWeight", "SelectedFishWeightText", "FishWeightText", "WeightText");

        if (selectedDescriptionText == null)
            selectedDescriptionText = FindChildComponent<TMP_Text>("FishCollectionDescription", "SelectedFishDescriptionText", "FishDescriptionText", "DescriptionText");

        if (selectedCapturedText == null)
            selectedCapturedText = FindChildComponent<TMP_Text>("FishCollectionCaptured", "SelectedFishCapturedText", "FishCapturedText", "CapturedText");

        if (selectedInfoText == null)
            selectedInfoText = FindChildComponent<TMP_Text>("SelectedFishInfoText", "FishInfoText", "InfoText");

        if (slotContent == null)
            slotContent = FindChildTransform("Content", "SlotContent", "FishContent");

        if (fishScrollRect == null && slotContent != null)
            fishScrollRect = slotContent.GetComponentInParent<ScrollRect>(true);

        if (slotTemplate == null && slotContent != null)
            slotTemplate = slotContent.GetComponentInChildren<FishCollectionSlotUI>(true);

        if ((slots == null || slots.Length == 0) && slotContent != null)
            slots = slotContent.GetComponentsInChildren<FishCollectionSlotUI>(true);

        if (fishPreviewPanel == null)
            fishPreviewPanel = FindFirstObjectByType<FishPreviewPanelUI>(FindObjectsInactive.Include);
    }

    private void BindButtons()
    {
        if (hasBoundButtons)
            return;

        if (closeButton != null)
            closeButton.onClick.AddListener(Close);

        if (previewButton != null)
            previewButton.onClick.AddListener(OpenSelectedPreview);

        hasBoundButtons = true;
    }

    private void SelectInitialControl()
    {
        FishCollectionSlotUI firstSlot = GetFirstUsableSlot();
        Selectable firstSlotSelectable = firstSlot != null ? firstSlot.Selectable : null;

        if (firstSlotSelectable != null)
        {
            UISelectionHelper.Select(firstSlotSelectable, PanelObject);
            firstSlot.SelectCurrentFish();
            return;
        }

        Selectable firstUsable = firstSelected != null ? firstSelected : UISelectionHelper.FirstUsable(PanelObject);

        if (firstUsable != null && EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(firstUsable.gameObject);
    }

    private FishCollectionSlotUI GetFirstUsableSlot()
    {
        if (slots == null)
            return null;

        for (int i = 0; i < slots.Length; i++)
        {
            FishCollectionSlotUI slot = slots[i];

            if (slot == null || !slot.HasFish)
                continue;

            Selectable selectable = slot.Selectable;

            if (UISelectionHelper.IsUsable(selectable))
                return slot;
        }

        return null;
    }

    private void SelectFirstConfiguredFish()
    {
        selectedFish = null;
        selectedFishDiscovered = false;

        if (fishEntries == null)
            return;

        for (int i = 0; i < fishEntries.Length; i++)
        {
            FishScriptableObject fish = fishEntries[i];

            if (fish == null)
                continue;

            selectedFish = fish;
            selectedFishDiscovered = FishCaptureHistory.IsDiscovered(fish);
            return;
        }
    }

    private void SetPreviewButtonInteractable(bool _interactable)
    {
        if (previewButton != null)
            previewButton.interactable = _interactable;
    }

    private void HandleMouseWheelScroll()
    {
        if (!IsOpen)
            return;

        float scrollDelta = UISelectionHelper.GetMouseScrollDeltaY();

        if (Mathf.Abs(scrollDelta) <= 0.01f)
            return;

        ResolveScrollRect();

        if (!UISelectionHelper.ApplyMouseWheelScroll(fishScrollRect, scrollDelta, mouseWheelScrollPixels))
            return;

        suppressSelectionScrollUntil = Time.unscaledTime + manualScrollSelectionFollowDelay;
        lastScrolledSelection = null;
    }

    private void KeepCurrentSelectionVisible()
    {
        if (!keepSelectionVisibleInScroll ||
            !IsOpen ||
            Time.unscaledTime < suppressSelectionScrollUntil ||
            EventSystem.current == null)
        {
            return;
        }

        ResolveScrollRect();

        if (fishScrollRect == null || fishScrollRect.content == null)
            return;

        GameObject selectedObject = EventSystem.current.currentSelectedGameObject;

        if (selectedObject == null ||
            !selectedObject.transform.IsChildOf(fishScrollRect.transform))
        {
            lastScrolledSelection = null;
            return;
        }

        RectTransform selectedRect = selectedObject.GetComponent<RectTransform>();

        if (selectedRect == null || !selectedRect.IsChildOf(fishScrollRect.content))
            return;

        Canvas.ForceUpdateCanvases();
        ScrollRectToChild(fishScrollRect, selectedRect, selectedObject != lastScrolledSelection);
        lastScrolledSelection = selectedObject;
    }

    private void ResolveScrollRect()
    {
        if (fishScrollRect == null && slotContent != null)
            fishScrollRect = slotContent.GetComponentInParent<ScrollRect>(true);

        UISelectionHelper.ConfigureVerticalOnlyScrollRect(fishScrollRect);
    }

    private void ScrollRectToChild(ScrollRect _scrollRect, RectTransform _child, bool _selectionChanged)
    {
        UISelectionHelper.ConfigureVerticalOnlyScrollRect(_scrollRect);

        RectTransform viewport = _scrollRect.viewport != null ? _scrollRect.viewport : _scrollRect.GetComponent<RectTransform>();
        RectTransform content = _scrollRect.content;

        if (viewport == null || content == null || _child == null)
            return;

        float hiddenHeight = content.rect.height - viewport.rect.height;

        if (hiddenHeight <= 0f)
            return;

        Bounds childBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(viewport, _child);
        Rect viewportRect = viewport.rect;
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

    private T FindChildComponent<T>(params string[] _names) where T : Component
    {
        T[] components = GetComponentsInChildren<T>(true);

        for (int i = 0; i < components.Length; i++)
        {
            T component = components[i];

            if (component == null)
                continue;

            for (int j = 0; j < _names.Length; j++)
            {
                if (component.gameObject.name == _names[j])
                    return component;
            }
        }

        return null;
    }

    private Transform FindChildTransform(params string[] _names)
    {
        Transform[] transforms = GetComponentsInChildren<Transform>(true);

        for (int i = 0; i < transforms.Length; i++)
        {
            Transform candidate = transforms[i];

            for (int j = 0; j < _names.Length; j++)
            {
                if (candidate.gameObject.name == _names[j])
                    return candidate;
            }
        }

        return null;
    }

    private static void SetText(TMP_Text _text, string _value)
    {
        if (_text != null)
            _text.text = _value;
    }

    private void ClearSelectedTexts()
    {
        SetText(selectedNameText, string.Empty);
        SetText(selectedPriceText, string.Empty);
        SetText(selectedWeightText, string.Empty);
        SetText(selectedDescriptionText, string.Empty);
        SetText(selectedCapturedText, string.Empty);
        SetText(selectedInfoText, string.Empty);
    }

    private static string GetFishDisplayName(FishScriptableObject _fish)
    {
        if (_fish == null)
            return "Peixe";

        return !string.IsNullOrWhiteSpace(_fish.fishName) ? _fish.fishName : _fish.name;
    }

    private static string GetFishDescription(FishScriptableObject _fish)
    {
        if (_fish == null || string.IsNullOrWhiteSpace(_fish.description))
            return "Sem descrição.";

        return _fish.description;
    }
}
