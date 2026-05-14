using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryBaitSlotUI : MonoBehaviour
{
    [SerializeField] private GameObject contentRoot;
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text quantityText;
    [SerializeField] private TMP_Text equippedText;
    [SerializeField] private Button equipButton;

    private BaitData currentBait;
    private Action<BaitData> onEquipClicked;

    private void Awake()
    {
        InitializeReferences();
        BindButton();
    }

    private void OnEnable()
    {
        BindButton();
    }

    private void OnDisable()
    {
        UnbindButton();
    }

    public void SetBait(BaitStack _stack, bool _isEquipped, Action<BaitData> _onEquipClicked)
    {
        InitializeReferences();

        bool hasBait = _stack != null && _stack.bait != null && _stack.quantity > 0;
        SetContentVisible(hasBait);

        currentBait = hasBait ? _stack.bait : null;
        onEquipClicked = _onEquipClicked;

        if (!hasBait)
            return;

        if (nameText != null)
            nameText.text = currentBait.BaitName;

        if (quantityText != null)
            quantityText.text = $"x{_stack.quantity}";

        if (equippedText != null)
            equippedText.text = _isEquipped ? "Equipada" : string.Empty;

        if (equipButton != null)
            equipButton.interactable = !_isEquipped;

        if (iconImage != null)
        {
            iconImage.sprite = currentBait.InventoryIcon;
            iconImage.enabled = currentBait.InventoryIcon != null;
            iconImage.preserveAspect = true;
        }
    }

    public void Clear()
    {
        InitializeReferences();
        currentBait = null;
        onEquipClicked = null;
        SetContentVisible(false);
    }

    private void InitializeReferences()
    {
        if (contentRoot == null)
            contentRoot = gameObject;

        if (iconImage == null)
            iconImage = GetComponentInChildren<Image>(true);

        if (equipButton == null)
            equipButton = GetComponentInChildren<Button>(true);

        TMP_Text[] texts = GetComponentsInChildren<TMP_Text>(true);

        if (nameText == null && texts.Length > 0)
            nameText = texts[0];

        if (quantityText == null && texts.Length > 1)
            quantityText = texts[1];

        if (equippedText == null && texts.Length > 2)
            equippedText = texts[2];
    }

    private void BindButton()
    {
        if (equipButton == null)
            return;

        equipButton.onClick.RemoveListener(HandleEquipClicked);
        equipButton.onClick.AddListener(HandleEquipClicked);
    }

    private void UnbindButton()
    {
        if (equipButton != null)
            equipButton.onClick.RemoveListener(HandleEquipClicked);
    }

    private void HandleEquipClicked()
    {
        if (currentBait != null)
            onEquipClicked?.Invoke(currentBait);
    }

    private void SetContentVisible(bool _visible)
    {
        if (contentRoot != null && contentRoot != gameObject)
        {
            contentRoot.SetActive(_visible);
            return;
        }

        CanvasGroup canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        canvasGroup.alpha = _visible ? 1f : 0f;
        canvasGroup.interactable = _visible;
        canvasGroup.blocksRaycasts = _visible;
    }
}
