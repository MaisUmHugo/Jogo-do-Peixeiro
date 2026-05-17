using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventoryFishSlotUI : MonoBehaviour, ISelectHandler, IDeselectHandler
{
    [SerializeField] private GameObject contentRoot;
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text weightText;
    [SerializeField] private TMP_Text valueText;
    [SerializeField] private Button slotButton;
    [SerializeField] private GameObject selectedFrame;
    [SerializeField] private GameObject discardMark;
    [SerializeField] private Toggle discardToggle;

    private int currentIndex = -1;
    private Action<int> onSlotSubmitted;
    private bool hasFish;

    public Selectable SlotSelectable
    {
        get
        {
            InitializeReferences();
            return slotButton;
        }
    }

    private void Awake()
    {
        InitializeReferences();
        BindButton();
        SetSelectedFrameVisible(false);
    }

    private void OnEnable()
    {
        BindButton();
    }

    private void OnDisable()
    {
        UnbindButton();
        SetSelectedFrameVisible(false);
    }

    public void OnSelect(BaseEventData _eventData)
    {
        SetSelectedFrameVisible(hasFish);
    }

    public void OnDeselect(BaseEventData _eventData)
    {
        SetSelectedFrameVisible(false);
    }

    public void SetFish(FishData _fish)
    {
        SetFish(_fish, -1, null, false, false);
    }

    public void SetFish(FishData _fish, int _index, Action<int> _onSlotSubmitted, bool _isDiscardMode, bool _isMarkedForDiscard)
    {
        InitializeReferences();

        hasFish = _fish != null && _fish.typeOfFish != null;
        currentIndex = hasFish ? _index : -1;
        onSlotSubmitted = _onSlotSubmitted;

        SetContentVisible(hasFish);
        SetDiscardStateVisible(hasFish && _isDiscardMode, hasFish && _isMarkedForDiscard);

        if (slotButton != null)
            slotButton.interactable = hasFish;

        if (!hasFish)
            return;

        FishScriptableObject fishType = _fish.typeOfFish;

        if (nameText != null)
            nameText.text = fishType.fishName;

        if (weightText != null)
            weightText.text = $"{_fish.weight:0.#} kg";

        if (valueText != null)
            valueText.text = $"R$ {FishPriceCalculator.CalculatePrice(_fish)}";

        if (iconImage != null)
        {
            iconImage.sprite = fishType.InventoryIcon;
            iconImage.enabled = fishType.InventoryIcon != null;
            iconImage.preserveAspect = true;
        }
    }

    public void Clear()
    {
        InitializeReferences();
        hasFish = false;
        currentIndex = -1;
        onSlotSubmitted = null;

        if (slotButton != null)
            slotButton.interactable = false;

        SetSelectedFrameVisible(false);
        SetDiscardStateVisible(false, false);
        SetContentVisible(false);
    }

    private void InitializeReferences()
    {
        if (contentRoot == null || contentRoot == gameObject)
        {
            GameObject contentChild = FindChildObject("Content", "SlotContent", "FishSlotContent");
            contentRoot = contentChild != null ? contentChild : gameObject;
        }

        if (iconImage == null)
            iconImage = FindIconImage();

        if (slotButton == null)
            slotButton = FindSlotButton();

        if (selectedFrame == null)
            selectedFrame = FindChildObject("SelectedFrame", "SelectionFrame", "Selected", "Selection");

        if (discardToggle == null)
            discardToggle = GetComponentInChildren<Toggle>(true);

        if (discardMark == null)
            discardMark = FindChildObject("DiscardMark", "MarkedForDiscard", "SelectedDiscard", "Checkmark", "CheckMark");

        TMP_Text[] texts = GetComponentsInChildren<TMP_Text>(true);

        if (nameText == null && texts.Length > 0)
            nameText = texts[0];

        if (weightText == null && texts.Length > 1)
            weightText = texts[1];

        if (valueText == null && texts.Length > 2)
            valueText = texts[2];
    }

    private Button FindSlotButton()
    {
        Button rootButton = GetComponent<Button>();

        if (rootButton != null)
            return rootButton;

        Button[] buttons = GetComponentsInChildren<Button>(true);

        for (int i = 0; i < buttons.Length; i++)
        {
            string buttonName = buttons[i].name.ToLowerInvariant();

            if ((buttonName.Contains("slot") || buttonName.Contains("fish")) &&
                !buttonName.Contains("discard") &&
                !buttonName.Contains("descartar"))
            {
                return buttons[i];
            }
        }

        for (int i = 0; i < buttons.Length; i++)
        {
            string buttonName = buttons[i].name.ToLowerInvariant();

            if (!buttonName.Contains("discard") && !buttonName.Contains("descartar"))
                return buttons[i];
        }

        return null;
    }

    private Image FindIconImage()
    {
        Image[] images = GetComponentsInChildren<Image>(true);

        for (int i = 0; i < images.Length; i++)
        {
            string imageName = images[i].name.ToLowerInvariant();

            if (imageName.Contains("icon") ||
                imageName.Contains("icone") ||
                imageName.Contains("fish"))
            {
                return images[i];
            }
        }

        for (int i = 0; i < images.Length; i++)
        {
            if (images[i].gameObject == gameObject)
                continue;

            string imageName = images[i].name.ToLowerInvariant();

            if (imageName.Contains("background") ||
                imageName.Contains("frame") ||
                imageName.Contains("selected") ||
                imageName.Contains("selection") ||
                imageName.Contains("toggle") ||
                imageName.Contains("checkmark"))
            {
                continue;
            }

            if (images[i].sprite != null || images[i].color.a < 1f)
                return images[i];
        }

        return images.Length > 0 ? images[0] : null;
    }

    private GameObject FindChildObject(params string[] _names)
    {
        for (int i = 0; i < _names.Length; i++)
        {
            Transform child = FindChildRecursive(transform, _names[i]);

            if (child != null)
                return child.gameObject;
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

    private void BindButton()
    {
        if (slotButton == null)
            return;

        slotButton.onClick.RemoveListener(HandleSlotSubmitted);
        slotButton.onClick.AddListener(HandleSlotSubmitted);
    }

    private void UnbindButton()
    {
        if (slotButton != null)
            slotButton.onClick.RemoveListener(HandleSlotSubmitted);
    }

    private void HandleSlotSubmitted()
    {
        if (hasFish && currentIndex >= 0)
            onSlotSubmitted?.Invoke(currentIndex);
    }

    private void SetSelectedFrameVisible(bool _visible)
    {
        if (selectedFrame != null)
            selectedFrame.SetActive(_visible);
    }

    private void SetDiscardStateVisible(bool _showSelectionControl, bool _isMarked)
    {
        if (discardToggle != null)
        {
            discardToggle.SetIsOnWithoutNotify(_isMarked);
            discardToggle.interactable = false;
            discardToggle.gameObject.SetActive(_showSelectionControl);
            return;
        }

        if (discardMark != null)
            discardMark.SetActive(_showSelectionControl && _isMarked);
    }

    private void SetContentVisible(bool _visible)
    {
        CanvasGroup rootCanvasGroup = GetComponent<CanvasGroup>();

        if (_visible && rootCanvasGroup != null)
        {
            rootCanvasGroup.alpha = 1f;
            rootCanvasGroup.interactable = true;
            rootCanvasGroup.blocksRaycasts = true;
        }

        if (contentRoot != null && contentRoot != gameObject)
        {
            contentRoot.SetActive(_visible);
            return;
        }

        CanvasGroup canvasGroup = rootCanvasGroup;

        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        canvasGroup.alpha = _visible ? 1f : 0f;
        canvasGroup.interactable = _visible;
        canvasGroup.blocksRaycasts = _visible;
    }
}
