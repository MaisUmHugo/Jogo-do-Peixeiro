using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class DockOwnerBaitShopUI : MonoBehaviour
{
    [Header("Groups")]
    [SerializeField] private GameObject baitShopGroup;
    [SerializeField] private GameObject quantityPopupGroup;
    [SerializeField] private GameObject tabButtonsGroup;

    [Header("Slots")]
    [SerializeField] private DockOwnerBaitShopSlotUI[] baitShopSlots;
    [SerializeField] private int maxBaitPurchaseQuantity = 99;
    [SerializeField, Range(0.1f, 1f)] private float quantityMoveThreshold = 0.65f;
    [SerializeField, Min(0.05f)] private float quantityMoveRepeatDelay = 0.22f;

    [Header("Quantity Popup")]
    [SerializeField] private GameObject quantityPanel;
    [SerializeField] private Image quantityIconImage;
    [SerializeField] private TMP_Text quantityTitleText;
    [SerializeField] private TMP_Text quantityText;
    [SerializeField] private TMP_Text quantityPriceText;
    [SerializeField] private TMP_Text quantityOwnedText;
    [SerializeField] private TMP_Text quantityWarningText;
    [SerializeField] private Button quantityConfirmButton;
    [SerializeField] private Button quantityCancelButton;
    [SerializeField] private Button quantityDecreaseMouseButton;
    [SerializeField] private Button quantityIncreaseMouseButton;
    [SerializeField] private Selectable quantityValueSelectable;
    [SerializeField] private Selectable quantityFirstSelected;
    [SerializeField, Min(0.1f)] private float quantityWarningDuration = 0.85f;
    [SerializeField, Min(0f)] private float quantityWarningRise = 22f;

    private BaitShop baitShop;
    private BaitInventory baitInventory;
    private PlayerMoneyManager playerMoneyManager;
    private DockOwnerPurchaseConfirmUI purchaseConfirmUI;
    private GameObject selectionScope;
    private Action<string> setStatus;
    private Action refreshOwner;
    private float playerMoney;
    private int[] baitPurchaseQuantities;
    private int pendingBaitSlotIndex = -1;
    private float nextQuantityMoveTime;
    private int lastQuantityMoveDirection;
    private int lastQuantityValueSubmitFrame = -1;
    private bool isEditingQuantityValue;
    private bool areButtonsBound;
    private Selectable selectionBeforePopup;
    private Coroutine quantityFeedbackRoutine;
    private Coroutine quantityWarningRoutine;
    private UnityEngine.Events.UnityAction[] baitSlotBuyActions;

    public bool HasOpenPopup => QuantityRoot != null && QuantityRoot.activeInHierarchy;

    private GameObject QuantityRoot => quantityPopupGroup != null ? quantityPopupGroup : quantityPanel;

    private void Awake()
    {
        ResolveSlots();
        ResolveQuantityMouseButtons();
        BindButtons();
    }

    private void OnEnable()
    {
        ResolveSlots();
        ResolveQuantityMouseButtons();
        BindButtons();
    }

    private void OnDisable()
    {
        UnbindButtons();
        ClosePopup(false);
    }

    public void Initialize(
        BaitShop _baitShop,
        BaitInventory _baitInventory,
        PlayerMoneyManager _playerMoneyManager,
        DockOwnerPurchaseConfirmUI _purchaseConfirmUI,
        GameObject _selectionScope,
        Action<string> _setStatus,
        Action _refreshOwner)
    {
        baitShop = _baitShop;
        baitInventory = _baitInventory;
        playerMoneyManager = _playerMoneyManager;
        purchaseConfirmUI = _purchaseConfirmUI;
        selectionScope = _selectionScope != null ? _selectionScope : gameObject;
        setStatus = _setStatus;
        refreshOwner = _refreshOwner;
        ResolveSlots();
        ResolveQuantityMouseButtons();
    }

    public void Refresh(float _playerMoney)
    {
        playerMoney = playerMoneyManager != null ? playerMoneyManager.PlayerMoney : _playerMoney;
        SetBaitUI();
    }

    public void TryBuySlot(int _slotIndex)
    {
        TryBuyBaitSlot(_slotIndex);
    }

    public void ChangeSlotQuantity(int _slotIndex, int _delta)
    {
        ChangeBaitQuantity(_slotIndex, _delta);
    }

    public void ChangePendingQuantity(int _delta)
    {
        ChangePendingBaitQuantity(_delta);
    }

    public void ConfirmPendingQuantity()
    {
        ConfirmPendingBaitQuantity();
    }

    public void CancelPendingQuantity()
    {
        ClosePopup(true);
    }

    public void HandleMoveInput(bool _canHandle)
    {
        if (!_canHandle || !HasOpenPopup || pendingBaitSlotIndex < 0)
            return;

        HandleQuantityValueSubmitInput();

        if (!CanHandleQuantityHorizontalInput())
            return;

        if (isEditingQuantityValue)
            ReselectQuantityValue();

        float horizontal = GetQuantityHorizontalInput();

        if (Mathf.Abs(horizontal) < quantityMoveThreshold)
        {
            lastQuantityMoveDirection = 0;
            nextQuantityMoveTime = 0f;
            return;
        }

        int direction = horizontal > 0f ? 1 : -1;

        if (direction == lastQuantityMoveDirection && Time.unscaledTime < nextQuantityMoveTime)
            return;

        ChangeBaitQuantity(pendingBaitSlotIndex, direction);
        ReselectQuantityValue();
        lastQuantityMoveDirection = direction;
        nextQuantityMoveTime = Time.unscaledTime + quantityMoveRepeatDelay;
    }

    public bool TryHandleBack()
    {
        if (!HasOpenPopup)
            return false;

        ClosePopup(true);
        return true;
    }

    public void ClosePopup(bool _restoreSelection)
    {
        SetQuantityPopupState(false);
        ClearQuantityWarning();
        pendingBaitSlotIndex = -1;
        lastQuantityMoveDirection = 0;
        nextQuantityMoveTime = 0f;
        isEditingQuantityValue = false;

        if (_restoreSelection)
            UISelectionHelper.Select(selectionBeforePopup, selectionScope);
    }

    public Selectable GetFirstSelectable()
    {
        if (baitShopSlots == null)
            return null;

        for (int i = 0; i < baitShopSlots.Length; i++)
        {
            Selectable selectable = baitShopSlots[i] != null ? baitShopSlots[i].GetSelectable() : null;

            if (UISelectionHelper.IsUsable(selectable))
                return selectable;
        }

        return null;
    }

    private void SetBaitUI()
    {
        BaitData[] baits = GetBaitsForSale();
        int slotCount = GetSlotCount();
        int visibleCount = Mathf.Min(baits.Length, slotCount);
        EnsureQuantityArray(visibleCount);

        for (int i = 0; i < slotCount; i++)
        {
            DockOwnerBaitShopSlotUI slot = baitShopSlots[i];

            if (slot == null)
                continue;

            bool hasBait = i < visibleCount && baits[i] != null;

            if (!hasBait)
            {
                slot.Clear();
                continue;
            }

            BaitData bait = baits[i];
            int maxQuantity = GetMaxPurchaseQuantity(bait);
            int ownedQuantity = baitInventory != null ? baitInventory.GetQuantity(bait) : 0;

            slot.SetBait(
                bait,
                ownedQuantity,
                maxQuantity,
                Mathf.Max(0, bait.PurchasePrice)
            );
        }

        RefreshQuantityPopup();
    }

    private BaitData[] GetBaitsForSale()
    {
        return baitShop != null ? baitShop.BaitsForSale : BaitCatalog.GetDefaultBaits();
    }

    private void EnsureQuantityArray(int _count)
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

    private int GetPurchaseQuantity(int _slotIndex, BaitData _bait)
    {
        EnsureQuantityArray(GetSlotCount());

        if (_slotIndex < 0 || baitPurchaseQuantities == null || _slotIndex >= baitPurchaseQuantities.Length)
            return GetMaxPurchaseQuantity(_bait) > 0 ? 1 : 0;

        int maxQuantity = GetMaxPurchaseQuantity(_bait);
        int minQuantity = maxQuantity > 0 ? 1 : 0;
        baitPurchaseQuantities[_slotIndex] = Mathf.Clamp(baitPurchaseQuantities[_slotIndex], minQuantity, Mathf.Max(1, maxQuantity));
        return baitPurchaseQuantities[_slotIndex];
    }

    private int GetMaxPurchaseQuantity(BaitData _bait)
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

    private int GetPurchaseCost(BaitData _bait, int _quantity)
    {
        if (_bait == null || _quantity <= 0)
            return 0;

        if (baitShop != null)
            return baitShop.GetBaitPurchaseCost(_bait, _quantity);

        return Mathf.Max(0, _bait.PurchasePrice * _quantity);
    }

    private void ChangeBaitQuantity(int _slotIndex, int _delta)
    {
        BaitData[] baits = GetBaitsForSale();

        if (_slotIndex < 0 || _slotIndex >= baits.Length || _delta == 0)
            return;

        BaitData bait = baits[_slotIndex];
        EnsureQuantityArray(GetSlotCount());

        int currentQuantity = GetPurchaseQuantity(_slotIndex, bait);
        int maxQuantity = GetMaxPurchaseQuantity(bait);
        int minQuantity = maxQuantity > 0 ? 1 : 0;
        int nextQuantity = Mathf.Clamp(currentQuantity + _delta, minQuantity, Mathf.Max(1, maxQuantity));

        if (nextQuantity == currentQuantity)
        {
            ShowQuantityLimitFeedback(bait, _delta, maxQuantity);
            return;
        }

        baitPurchaseQuantities[_slotIndex] = nextQuantity;
        SetBaitUI();
    }

    private void TryBuyBaitSlot(int _slotIndex)
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
        int maxQuantity = GetMaxPurchaseQuantity(bait);

        if (maxQuantity <= 0)
        {
            ShowQuantityLimitFeedback(bait, 1, maxQuantity);
            SetStatus("Dinheiro insuficiente.");
            return;
        }

        pendingBaitSlotIndex = _slotIndex;
        int quantity = GetPurchaseQuantity(_slotIndex, bait);

        if (QuantityRoot != null)
        {
            OpenQuantityPopup(_slotIndex);
            return;
        }

        OpenPurchaseConfirmation(_slotIndex, quantity, null);
    }

    private void ExecutePurchase(int _slotIndex)
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
        int quantity = GetPurchaseQuantity(_slotIndex, bait);

        if (quantity <= 0)
        {
            ShowQuantityLimitFeedback(bait, 1, GetMaxPurchaseQuantity(bait));
            SetStatus("Dinheiro insuficiente.");
            return;
        }

        bool success = baitShop.TryBuyBait(bait, quantity, out BaitPurchaseResult result);

        if (!success)
            ShowQuantityLimitFeedback(bait, 1, GetMaxPurchaseQuantity(bait));

        SetStatus(success ? $"{bait.BaitName} x{quantity} comprada." : GetPurchaseStatusText(result));
        refreshOwner?.Invoke();
    }

    private void OpenQuantityPopup(int _slotIndex)
    {
        pendingBaitSlotIndex = _slotIndex;
        selectionBeforePopup = UISelectionHelper.CurrentSelectableInScope(selectionScope);
        isEditingQuantityValue = true;
        lastQuantityValueSubmitFrame = Time.frameCount;
        lastQuantityMoveDirection = 0;
        nextQuantityMoveTime = Time.unscaledTime + quantityMoveRepeatDelay;
        ClearQuantityWarning();
        SetQuantityPopupState(true);
        RefreshQuantityPopup();
        UISelectionHelper.Select(
            GetQuantityPopupFirstSelectable(),
            QuantityRoot
        );
    }

    private void RefreshQuantityPopup()
    {
        if (!HasOpenPopup || pendingBaitSlotIndex < 0)
            return;

        BaitData[] baits = GetBaitsForSale();

        if (pendingBaitSlotIndex >= baits.Length)
        {
            ClosePopup(false);
            return;
        }

        BaitData bait = baits[pendingBaitSlotIndex];
        int quantity = GetPurchaseQuantity(pendingBaitSlotIndex, bait);
        int maxQuantity = GetMaxPurchaseQuantity(bait);
        int ownedQuantity = baitInventory != null ? baitInventory.GetQuantity(bait) : 0;
        int totalCost = GetPurchaseCost(bait, quantity);

        if (quantityIconImage != null)
        {
            quantityIconImage.sprite = bait != null ? bait.InventoryIcon : null;
            quantityIconImage.enabled = bait != null && bait.InventoryIcon != null;
            quantityIconImage.preserveAspect = true;
        }

        if (quantityTitleText != null)
            quantityTitleText.text = bait != null ? bait.BaitName : "Isca";

        if (quantityText != null)
            quantityText.text = quantity.ToString();

        if (quantityPriceText != null)
            quantityPriceText.text = $"Total: R$ {totalCost}";

        if (quantityOwnedText != null)
            quantityOwnedText.text = $"No inventario: {ownedQuantity}";

        if (quantityConfirmButton != null)
            quantityConfirmButton.interactable = quantity > 0;

        SetQuantityMouseButtonsInteractable(maxQuantity > 0);
    }

    private void ChangePendingBaitQuantity(int _delta)
    {
        if (pendingBaitSlotIndex < 0)
            return;

        ChangeBaitQuantity(pendingBaitSlotIndex, _delta);
        RefreshQuantityPopup();
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
            ClosePopup(false);
            return;
        }

        ClosePopup(false);
        OpenPurchaseConfirmation(slotIndex, GetPurchaseQuantity(slotIndex, baits[slotIndex]), previousSelection);
    }

    private void OpenPurchaseConfirmation(int _slotIndex, int _quantity, Selectable _restoreSelection)
    {
        BaitData[] baits = GetBaitsForSale();

        if (_slotIndex < 0 || _slotIndex >= baits.Length)
            return;

        BaitData bait = baits[_slotIndex];
        int totalCost = GetPurchaseCost(bait, _quantity);

        if (purchaseConfirmUI == null)
        {
            ExecutePurchase(_slotIndex);
            return;
        }

        purchaseConfirmUI.Open(
            "Confirmar compra",
            $"Comprar {bait.BaitName} x{_quantity} por R$ {totalCost}?",
            () => ExecutePurchase(_slotIndex),
            selectionScope,
            _restoreSelection
        );
    }

    private void ShowQuantityLimitFeedback(BaitData _bait, int _delta, int _maxQuantity)
    {
        string message = GetQuantityLimitMessage(_bait, _delta, _maxQuantity);
        ShakeQuantityValue();

        if (HasOpenPopup)
            ShowQuantityWarning(message);
        else
            SetStatus(message);
    }

    private string GetQuantityLimitMessage(BaitData _bait, int _delta, int _maxQuantity)
    {
        if (_delta < 0)
            return "Quantidade minima.";

        if (_maxQuantity <= 0)
            return "Dinheiro insuficiente.";

        int hardLimit = Mathf.Max(1, maxBaitPurchaseQuantity);

        if (_maxQuantity >= hardLimit)
            return "Maximo permitido.";

        int nextQuantityCost = GetPurchaseCost(_bait, _maxQuantity + 1);
        return nextQuantityCost > playerMoney ? "Dinheiro insuficiente." : "Maximo permitido.";
    }

    private void ShakeQuantityValue()
    {
        RectTransform rect = GetQuantityValueRect();

        if (rect == null)
            return;

        if (quantityFeedbackRoutine != null)
            StopCoroutine(quantityFeedbackRoutine);

        quantityFeedbackRoutine = StartCoroutine(ShakeRectRoutine(rect));
    }

    private RectTransform GetQuantityValueRect()
    {
        if (quantityValueSelectable != null)
            return quantityValueSelectable.transform as RectTransform;

        return quantityText != null ? quantityText.rectTransform : null;
    }

    private IEnumerator ShakeRectRoutine(RectTransform _rect)
    {
        if (_rect == null)
            yield break;

        Vector2 basePosition = _rect.anchoredPosition;
        Vector3 baseScale = _rect.localScale;
        const float duration = 0.22f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float shake = Mathf.Sin(progress * Mathf.PI * 8f) * (1f - progress) * 8f;
            _rect.anchoredPosition = basePosition + new Vector2(shake, 0f);
            _rect.localScale = baseScale * (1f + 0.08f * (1f - progress));
            yield return null;
        }

        _rect.anchoredPosition = basePosition;
        _rect.localScale = baseScale;
        quantityFeedbackRoutine = null;
    }

    private void ShowQuantityWarning(string _message)
    {
        if (quantityWarningText == null)
        {
            SetStatus(_message);
            return;
        }

        if (quantityWarningRoutine != null)
            StopCoroutine(quantityWarningRoutine);

        quantityWarningRoutine = StartCoroutine(ShowTemporaryTextRoutine(
            quantityWarningText,
            _message,
            quantityWarningDuration,
            quantityWarningRise,
            () => quantityWarningRoutine = null
        ));
    }

    private void ClearQuantityWarning()
    {
        if (quantityWarningRoutine != null)
        {
            StopCoroutine(quantityWarningRoutine);
            quantityWarningRoutine = null;
        }

        if (quantityWarningText != null)
        {
            quantityWarningText.text = string.Empty;
            Color color = quantityWarningText.color;
            color.a = 0f;
            quantityWarningText.color = color;
        }
    }

    private IEnumerator ShowTemporaryTextRoutine(
        TMP_Text _text,
        string _message,
        float _duration,
        float _rise,
        System.Action _onComplete)
    {
        if (_text == null)
            yield break;

        RectTransform rect = _text.rectTransform;
        Vector2 basePosition = rect.anchoredPosition;
        Color baseColor = _text.color;
        Color visibleColor = baseColor;
        visibleColor.a = 1f;
        float duration = Mathf.Max(0.1f, _duration);
        float elapsed = 0f;

        _text.text = _message;
        _text.color = visibleColor;
        _text.gameObject.SetActive(true);

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float fadeProgress = Mathf.InverseLerp(0.25f, 1f, progress);
            Color color = visibleColor;
            color.a = Mathf.Lerp(1f, 0f, fadeProgress);
            _text.color = color;
            rect.anchoredPosition = basePosition + Vector2.up * (_rise * progress);
            yield return null;
        }

        _text.text = string.Empty;
        rect.anchoredPosition = basePosition;
        baseColor.a = 0f;
        _text.color = baseColor;
        _onComplete?.Invoke();
    }

    private string GetPurchaseStatusText(BaitPurchaseResult _purchaseResult)
    {
        return _purchaseResult switch
        {
            BaitPurchaseResult.MissingReferences => "Sistema de iscas incompleto.",
            BaitPurchaseResult.InvalidBait => "Isca invalida.",
            BaitPurchaseResult.NotEnoughMoney => "Dinheiro insuficiente.",
            _ => "Nao foi possivel comprar a isca."
        };
    }

    private void ResolveSlots()
    {
        if (baitShopSlots != null && baitShopSlots.Length > 0)
            return;

        baitShopSlots = GetComponentsInChildren<DockOwnerBaitShopSlotUI>(true);
    }

    private int GetSlotCount()
    {
        return baitShopSlots != null ? baitShopSlots.Length : 0;
    }

    private void BindButtons()
    {
        if (areButtonsBound)
            return;

        BindSlotButtons();

        Button quantityValueButton = quantityValueSelectable as Button;

        if (quantityValueButton != null)
            quantityValueButton.onClick.AddListener(OnQuantityValueSubmit);

        if (quantityConfirmButton != null)
            quantityConfirmButton.onClick.AddListener(ConfirmPendingBaitQuantity);

        if (quantityCancelButton != null)
            quantityCancelButton.onClick.AddListener(OnCancelPendingClicked);

        if (quantityDecreaseMouseButton != null)
            quantityDecreaseMouseButton.onClick.AddListener(OnDecreaseQuantityMouseClicked);

        if (quantityIncreaseMouseButton != null)
            quantityIncreaseMouseButton.onClick.AddListener(OnIncreaseQuantityMouseClicked);

        areButtonsBound = true;
    }

    private void UnbindButtons()
    {
        if (!areButtonsBound)
            return;

        UnbindSlotButtons();

        Button quantityValueButton = quantityValueSelectable as Button;

        if (quantityValueButton != null)
            quantityValueButton.onClick.RemoveListener(OnQuantityValueSubmit);

        if (quantityConfirmButton != null)
            quantityConfirmButton.onClick.RemoveListener(ConfirmPendingBaitQuantity);

        if (quantityCancelButton != null)
            quantityCancelButton.onClick.RemoveListener(OnCancelPendingClicked);

        if (quantityDecreaseMouseButton != null)
            quantityDecreaseMouseButton.onClick.RemoveListener(OnDecreaseQuantityMouseClicked);

        if (quantityIncreaseMouseButton != null)
            quantityIncreaseMouseButton.onClick.RemoveListener(OnIncreaseQuantityMouseClicked);

        areButtonsBound = false;
    }

    private void BindSlotButtons()
    {
        if (baitShopSlots == null || baitShopSlots.Length == 0)
            return;

        baitSlotBuyActions = new UnityEngine.Events.UnityAction[baitShopSlots.Length];

        for (int i = 0; i < baitShopSlots.Length; i++)
        {
            Button button = baitShopSlots[i] != null ? baitShopSlots[i].BuyButton : null;

            if (button == null)
                continue;

            int slotIndex = i;
            UnityEngine.Events.UnityAction action = () => TryBuyBaitSlot(slotIndex);
            baitSlotBuyActions[i] = action;
            button.onClick.AddListener(action);
        }
    }

    private void UnbindSlotButtons()
    {
        if (baitShopSlots == null || baitSlotBuyActions == null)
            return;

        for (int i = 0; i < baitShopSlots.Length && i < baitSlotBuyActions.Length; i++)
        {
            Button button = baitShopSlots[i] != null ? baitShopSlots[i].BuyButton : null;

            if (button != null && baitSlotBuyActions[i] != null)
                button.onClick.RemoveListener(baitSlotBuyActions[i]);
        }

        baitSlotBuyActions = null;
    }

    private void OnQuantityValueSubmit()
    {
        SubmitQuantityValue();
    }

    private void OnCancelPendingClicked()
    {
        ClosePopup(true);
    }

    private void OnDecreaseQuantityMouseClicked()
    {
        ChangePendingBaitQuantity(-1);
        ReselectQuantityValue();
    }

    private void OnIncreaseQuantityMouseClicked()
    {
        ChangePendingBaitQuantity(1);
        ReselectQuantityValue();
    }

    private void SetObjectActive(GameObject _target, bool _active)
    {
        if (_target != null)
            _target.SetActive(_active);
    }

    private void SetQuantityPopupState(bool _isOpen)
    {
        SetObjectActive(baitShopGroup, !_isOpen);
        SetObjectActive(tabButtonsGroup, !_isOpen);
        SetObjectActive(QuantityRoot, _isOpen);
    }

    private bool CanHandleQuantityHorizontalInput()
    {
        if (!UISelectionHelper.IsUsable(quantityValueSelectable))
            return true;

        return isEditingQuantityValue;
    }

    private void HandleQuantityValueSubmitInput()
    {
        if (!WasSubmitPressedThisFrame())
            return;

        if (!isEditingQuantityValue && !IsQuantityValueSelected())
            return;

        SubmitQuantityValue();
    }

    private void SubmitQuantityValue()
    {
        if (!HasOpenPopup || pendingBaitSlotIndex < 0)
            return;

        if (lastQuantityValueSubmitFrame == Time.frameCount)
            return;

        lastQuantityValueSubmitFrame = Time.frameCount;

        if (!isEditingQuantityValue)
        {
            isEditingQuantityValue = true;
            ReselectQuantityValue();
            return;
        }

        isEditingQuantityValue = false;
        UISelectionHelper.Select(GetQuantityPopupFirstSelectable(), QuantityRoot);
    }

    private bool IsQuantityValueSelected()
    {
        if (!UISelectionHelper.IsUsable(quantityValueSelectable))
            return true;

        Selectable current = UISelectionHelper.CurrentSelectableInScope(QuantityRoot);
        return current == quantityValueSelectable;
    }

    private bool WasSubmitPressedThisFrame()
    {
        Keyboard keyboard = Keyboard.current;

        if (keyboard != null &&
            (keyboard.enterKey.wasPressedThisFrame ||
             keyboard.numpadEnterKey.wasPressedThisFrame ||
             keyboard.spaceKey.wasPressedThisFrame))
        {
            return true;
        }

        Gamepad gamepad = Gamepad.current;
        return gamepad != null && gamepad.buttonSouth.wasPressedThisFrame;
    }

    private float GetQuantityHorizontalInput()
    {
        float horizontal = InputHandler.instance != null ? InputHandler.instance.moveInput.x : 0f;

        Keyboard keyboard = Keyboard.current;

        if (keyboard != null)
        {
            if (keyboard.rightArrowKey.isPressed || keyboard.dKey.isPressed)
                horizontal = 1f;
            else if (keyboard.leftArrowKey.isPressed || keyboard.aKey.isPressed)
                horizontal = -1f;
        }

        Gamepad gamepad = Gamepad.current;

        if (gamepad != null)
        {
            float dpadX = gamepad.dpad.ReadValue().x;
            float stickX = gamepad.leftStick.ReadValue().x;

            if (Mathf.Abs(dpadX) >= quantityMoveThreshold)
                horizontal = dpadX;
            else if (Mathf.Abs(stickX) > Mathf.Abs(horizontal))
                horizontal = stickX;
        }

        return horizontal;
    }

    private void ReselectQuantityValue()
    {
        if (!HasOpenPopup || !UISelectionHelper.IsUsable(quantityValueSelectable))
            return;

        UISelectionHelper.Select(quantityValueSelectable, QuantityRoot);
    }

    private Selectable GetQuantityPopupFirstSelectable()
    {
        if (UISelectionHelper.IsUsable(quantityValueSelectable))
            return quantityValueSelectable;

        if (UISelectionHelper.IsUsable(quantityFirstSelected))
            return quantityFirstSelected;

        if (UISelectionHelper.IsUsable(quantityConfirmButton))
            return quantityConfirmButton;

        if (UISelectionHelper.IsUsable(quantityCancelButton))
            return quantityCancelButton;

        return quantityValueSelectable;
    }

    private void ResolveQuantityMouseButtons()
    {
        GameObject root = QuantityRoot != null ? QuantityRoot : gameObject;

        if (quantityDecreaseMouseButton == null)
        {
            quantityDecreaseMouseButton = FindButtonInRoot(
                root,
                "QuantityDecreaseButton",
                "DecreaseQuantityButton",
                "QuantityMinusButton",
                "MinusButton",
                "DecreaseButton"
            );
        }

        if (quantityIncreaseMouseButton == null)
        {
            quantityIncreaseMouseButton = FindButtonInRoot(
                root,
                "QuantityIncreaseButton",
                "IncreaseQuantityButton",
                "QuantityPlusButton",
                "PlusButton",
                "IncreaseButton"
            );
        }

        ConfigureMouseOnlyQuantityButton(quantityDecreaseMouseButton);
        ConfigureMouseOnlyQuantityButton(quantityIncreaseMouseButton);
    }

    private Button FindButtonInRoot(GameObject _root, params string[] _names)
    {
        if (_root == null || _names == null || _names.Length == 0)
            return null;

        Button[] buttons = _root.GetComponentsInChildren<Button>(true);

        for (int i = 0; i < _names.Length; i++)
        {
            string targetName = _names[i];

            for (int j = 0; j < buttons.Length; j++)
            {
                if (buttons[j] != null &&
                    string.Equals(buttons[j].name, targetName, StringComparison.OrdinalIgnoreCase))
                {
                    return buttons[j];
                }
            }
        }

        return null;
    }

    private void ConfigureMouseOnlyQuantityButton(Button _button)
    {
        if (_button == null)
            return;

        Navigation navigation = _button.navigation;
        navigation.mode = Navigation.Mode.None;
        _button.navigation = navigation;
    }

    private void SetQuantityMouseButtonsInteractable(bool _interactable)
    {
        if (quantityDecreaseMouseButton != null)
            quantityDecreaseMouseButton.interactable = _interactable;

        if (quantityIncreaseMouseButton != null)
            quantityIncreaseMouseButton.interactable = _interactable;
    }

    private void SetStatus(string _message)
    {
        setStatus?.Invoke(_message);
    }
}
