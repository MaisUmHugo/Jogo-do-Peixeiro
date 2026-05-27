using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShipInventoryHud : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject inventoryHudRoot;
    [SerializeField] private TMP_Text inventoryWeightText;
    [SerializeField] private Image inventoryCapacityFill;

    [Header("References")]
    [SerializeField] private ShipInventory shipInventory;

    [Header("Settings")]
    [SerializeField] private bool showInventoryHud = true;
    [SerializeField] private bool autoResolveReferences = true;
    [SerializeField] private bool allowRuntimeFallback;
    [SerializeField] private bool logMissingReferences = true;
    [SerializeField] private bool configureFillImage = true;
    [SerializeField] private Color inventoryBarNormalColor = new Color(0.35f, 0.78f, 0.42f, 1f);
    [SerializeField] private Color inventoryBarWarningColor = new Color(0.95f, 0.74f, 0.2f, 1f);
    [SerializeField] private Color inventoryBarFullColor = new Color(0.85f, 0.25f, 0.25f, 1f);

    private bool isShipInventorySubscribed;
    private bool hasLoggedMissingUi;
    private bool hasLoggedMissingInventory;
    private bool isHudSuppressed;

    private void OnEnable()
    {
        ResolveReferences();
        ResolveHudReferences();
        EnsureInventoryHud();
        ValidateRequiredReferences();
        SubscribeShipInventory();
        RefreshInventoryHud();
    }

    private void OnDisable()
    {
        UnsubscribeShipInventory();
    }

    private void ResolveReferences()
    {
        if (shipInventory == null)
            shipInventory = FindFirstObjectByType<ShipInventory>(FindObjectsInactive.Include);
    }

    private void ResolveHudReferences()
    {
        if (!autoResolveReferences)
            return;

        Transform searchRoot = transform.parent != null ? transform.parent : transform;

        if (inventoryWeightText == null)
            inventoryWeightText = FindTextByName(searchRoot, "InventoryWeightText", "InventoryCapacityText", "InventoryHudText", "CargaText", "PesoText");

        if (inventoryCapacityFill == null)
            inventoryCapacityFill = FindImageByName(searchRoot, "InventoryCapacityFill", "InventoryFill", "CargoFill", "CargaFill");

        if (inventoryHudRoot == null && inventoryWeightText != null)
            inventoryHudRoot = GetInventoryHudRootFromText(inventoryWeightText);
    }

    private void EnsureInventoryHud()
    {
        if (!showInventoryHud || inventoryWeightText != null || !allowRuntimeFallback)
            return;

        CreateInventoryHud();
    }

    private void CreateInventoryHud()
    {
        GameObject panelObject = new GameObject("InventoryCapacityHUD", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panelObject.transform.SetParent(transform, false);
        inventoryHudRoot = panelObject;

        RectTransform panelRect = panelObject.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.zero;
        panelRect.pivot = Vector2.zero;
        panelRect.anchoredPosition = new Vector2(24f, 24f);
        panelRect.sizeDelta = new Vector2(300f, 76f);

        Image panelImage = panelObject.GetComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0.55f);
        panelImage.raycastTarget = false;

        inventoryWeightText = CreateInventoryHudText(panelObject.transform);
        inventoryCapacityFill = CreateInventoryCapacityBar(panelObject.transform);
    }

    private TMP_Text CreateInventoryHudText(Transform _parent)
    {
        GameObject textObject = new GameObject("InventoryWeightText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(_parent, false);

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0f, 1f);
        textRect.anchorMax = Vector2.one;
        textRect.pivot = new Vector2(0f, 1f);
        textRect.offsetMin = new Vector2(14f, -38f);
        textRect.offsetMax = new Vector2(-14f, -8f);

        TMP_Text text = textObject.GetComponent<TMP_Text>();
        text.fontSize = 22f;
        text.enableAutoSizing = true;
        text.fontSizeMin = 14f;
        text.fontSizeMax = 22f;
        text.alignment = TextAlignmentOptions.Left;
        text.color = Color.white;
        text.raycastTarget = false;
        return text;
    }

    private Image CreateInventoryCapacityBar(Transform _parent)
    {
        GameObject barBackgroundObject = new GameObject("InventoryCapacityBar", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        barBackgroundObject.transform.SetParent(_parent, false);

        RectTransform backgroundRect = barBackgroundObject.GetComponent<RectTransform>();
        backgroundRect.anchorMin = Vector2.zero;
        backgroundRect.anchorMax = new Vector2(1f, 0f);
        backgroundRect.pivot = Vector2.zero;
        backgroundRect.offsetMin = new Vector2(14f, 12f);
        backgroundRect.offsetMax = new Vector2(-14f, 28f);

        Image backgroundImage = barBackgroundObject.GetComponent<Image>();
        backgroundImage.color = new Color(1f, 1f, 1f, 0.18f);
        backgroundImage.raycastTarget = false;

        GameObject fillObject = new GameObject("InventoryCapacityFill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        fillObject.transform.SetParent(barBackgroundObject.transform, false);

        RectTransform fillRect = fillObject.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;

        Image fillImage = fillObject.GetComponent<Image>();
        fillImage.color = inventoryBarNormalColor;
        fillImage.raycastTarget = false;
        ConfigureInventoryFill(fillImage);
        return fillImage;
    }

    private void SubscribeShipInventory()
    {
        if (isShipInventorySubscribed)
            return;

        if (shipInventory == null)
            ResolveReferences();

        if (shipInventory == null)
            return;

        shipInventory.OnFishListChange += ChangeShipInventory;
        isShipInventorySubscribed = true;
    }

    private void UnsubscribeShipInventory()
    {
        if (!isShipInventorySubscribed || shipInventory == null)
            return;

        shipInventory.OnFishListChange -= ChangeShipInventory;
        isShipInventorySubscribed = false;
    }

    private void ChangeShipInventory(List<FishData> _fishList, float _fishWeight)
    {
        RefreshInventoryHud();
    }

    private void RefreshInventoryHud()
    {
        if (isHudSuppressed)
        {
            SetInventoryHudActive(false);
            return;
        }

        if (!showInventoryHud)
        {
            SetInventoryHudActive(false);
            return;
        }

        if (shipInventory == null)
        {
            ResolveReferences();
            SubscribeShipInventory();
        }

        if (shipInventory == null || inventoryWeightText == null)
        {
            SetInventoryHudActive(false);
            return;
        }

        SetInventoryHudActive(true);

        float currentInventoryWeight = shipInventory.GetCurrentWeight();
        float currentInventoryCapacity = shipInventory.GetMaxCapacity();
        float fillAmount = currentInventoryCapacity > 0f
            ? Mathf.Clamp01(currentInventoryWeight / currentInventoryCapacity)
            : 0f;

        Color statusColor = GetInventoryStatusColor(fillAmount);

        inventoryWeightText.text = $"Carga: {currentInventoryWeight:0.#}/{currentInventoryCapacity:0.#} kg";
        inventoryWeightText.color = statusColor;

        if (inventoryCapacityFill != null)
        {
            ConfigureInventoryFill(inventoryCapacityFill);
            inventoryCapacityFill.fillAmount = fillAmount;
            inventoryCapacityFill.color = statusColor;
        }
    }

    private void SetInventoryHudActive(bool _active)
    {
        if (inventoryHudRoot != null)
            inventoryHudRoot.SetActive(_active);

        if (inventoryHudRoot == null && inventoryWeightText != null)
            inventoryWeightText.gameObject.SetActive(_active);

        if (inventoryHudRoot == null && inventoryCapacityFill != null)
            inventoryCapacityFill.gameObject.SetActive(_active);
    }

    public void SetHudSuppressed(bool _suppressed)
    {
        if (isHudSuppressed == _suppressed)
            return;

        isHudSuppressed = _suppressed;
        RefreshInventoryHud();
    }

    private void ConfigureInventoryFill(Image _fillImage)
    {
        if (!configureFillImage || _fillImage == null)
            return;

        _fillImage.type = Image.Type.Filled;
        _fillImage.fillMethod = Image.FillMethod.Horizontal;
        _fillImage.fillOrigin = 0;
    }

    private Color GetInventoryStatusColor(float _fillAmount)
    {
        if (_fillAmount >= 1f)
            return inventoryBarFullColor;

        if (_fillAmount >= 0.75f)
            return inventoryBarWarningColor;

        return inventoryBarNormalColor;
    }

    private void ValidateRequiredReferences()
    {
        if (shipInventory == null)
            LogMissingInventory();

        if (showInventoryHud && inventoryWeightText == null)
            LogMissingUi();
    }

    private void LogMissingInventory()
    {
        if (!logMissingReferences || hasLoggedMissingInventory)
            return;

        Debug.LogWarning("[ShipInventoryHud] Falta ShipInventory. Arraste o inventário do barco no Inspector ou mantenha um ShipInventory ativo na cena.", this);
        hasLoggedMissingInventory = true;
    }

    private void LogMissingUi()
    {
        if (!logMissingReferences || hasLoggedMissingUi)
            return;

        Debug.LogWarning("[ShipInventoryHud] Falta InventoryWeightText. Crie um TMP_Text na cena ou arraste no Inspector. Opcional: arraste InventoryCapacityFill para a barra. Ative Allow Runtime Fallback apenas se quiser criar essa UI em runtime.", this);
        hasLoggedMissingUi = true;
    }

    private TMP_Text FindTextByName(Transform _root, params string[] _names)
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

    private Image FindImageByName(Transform _root, params string[] _names)
    {
        if (_root == null || _names == null)
            return null;

        Image[] images = _root.GetComponentsInChildren<Image>(true);

        for (int i = 0; i < images.Length; i++)
        {
            Image image = images[i];

            if (image == null)
                continue;

            for (int j = 0; j < _names.Length; j++)
            {
                if (string.Equals(image.gameObject.name, _names[j], System.StringComparison.OrdinalIgnoreCase))
                    return image;
            }
        }

        return null;
    }

    private GameObject GetInventoryHudRootFromText(TMP_Text _text)
    {
        if (_text == null)
            return null;

        Transform parent = _text.transform.parent;

        if (parent != null &&
            (parent.name.IndexOf("Inventory", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
             parent.name.IndexOf("Carga", System.StringComparison.OrdinalIgnoreCase) >= 0))
        {
            return parent.gameObject;
        }

        return _text.gameObject;
    }
}
