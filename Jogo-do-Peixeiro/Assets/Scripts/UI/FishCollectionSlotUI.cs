using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class FishCollectionSlotUI : MonoBehaviour, ISubmitHandler, IPointerClickHandler, ISelectHandler
{
    [Header("References")]
    [SerializeField] private Button button;
    [SerializeField] private Image fishIconImage;
    [SerializeField] private TMP_Text fishNameText;
    [SerializeField] private TMP_Text priceText;
    [SerializeField] private TMP_Text weightText;
    [SerializeField] private TMP_Text capturedText;
    [SerializeField] private GameObject discoveredRoot;
    [SerializeField] private GameObject undiscoveredRoot;

    [Header("Texts")]
    [SerializeField] private string undiscoveredName = "???";
    [SerializeField] private string undiscoveredPrice = "Preço base: ???";
    [SerializeField] private string undiscoveredWeight = "Peso: ???";
    [SerializeField] private string priceFormat = "Preço base: R$ {0}";
    [SerializeField] private string weightFormat = "Peso: {0}-{1} kg";
    [SerializeField] private string capturedFormat = "Capturado: {0}";

    private FishCollectionUI owner;
    private FishScriptableObject fish;
    private bool discovered;
    private bool hasBoundButton;

    public Selectable Selectable
    {
        get
        {
            if (button == null)
                ResolveReferences();

            return button;
        }
    }
    public bool HasFish => fish != null;

    private void Awake()
    {
        ResolveReferences();
        BindButton();
    }

    private void OnEnable()
    {
        ResolveReferences();
        BindButton();
    }

    private void OnDestroy()
    {
        if (button != null)
            button.onClick.RemoveListener(HandleSelected);
    }

    public void SetFish(FishCollectionUI _owner, FishScriptableObject _fish, bool _discovered)
    {
        owner = _owner;
        fish = _fish;
        discovered = _discovered;
        Refresh();
    }

    public void OnSubmit(BaseEventData _eventData)
    {
        HandleSelected();
    }

    public void OnSelect(BaseEventData _eventData)
    {
        HandleSelected();
    }

    public void OnPointerClick(PointerEventData _eventData)
    {
        HandleSelected();
    }

    public void SelectCurrentFish()
    {
        HandleSelected();
    }

    private void HandleSelected()
    {
        if (owner != null)
            owner.SelectFish(fish, discovered);
    }

    private void Refresh()
    {
        bool hasFish = fish != null;
        gameObject.SetActive(hasFish);

        if (!hasFish)
            return;

        SetObjectActive(discoveredRoot, discovered);
        SetObjectActive(undiscoveredRoot, !discovered);

        if (button != null)
            button.interactable = hasFish;

        if (fishIconImage != null)
        {
            fishIconImage.sprite = discovered ? fish.InventoryIcon : null;
            fishIconImage.enabled = discovered && fish.InventoryIcon != null;
            fishIconImage.preserveAspect = true;
        }

        if (fishNameText != null)
            fishNameText.text = discovered ? GetFishDisplayName(fish) : undiscoveredName;

        if (priceText != null)
            priceText.text = discovered ? string.Format(priceFormat, fish.BasePrice) : undiscoveredPrice;

        if (weightText != null)
            weightText.text = discovered ? string.Format(weightFormat, fish.minWeight, fish.maxWeight) : undiscoveredWeight;

        if (capturedText != null)
            capturedText.text = discovered ? string.Format(capturedFormat, FishCaptureHistory.GetCaptureCount(fish)) : string.Empty;
    }

    private void ResolveReferences()
    {
        if (button == null)
            button = GetComponent<Button>();

        if (fishIconImage == null)
            fishIconImage = FindChildComponent<Image>("FishIcon", "Icon", "FishImage");

        if (fishNameText == null)
            fishNameText = FindChildComponent<TMP_Text>("FishNameText", "NameText", "NomeText");

        if (priceText == null)
            priceText = FindChildComponent<TMP_Text>("PriceText", "BasePriceText", "PrecoText");

        if (weightText == null)
            weightText = FindChildComponent<TMP_Text>("WeightText", "PesoText");

        if (capturedText == null)
            capturedText = FindChildComponent<TMP_Text>("CapturedText", "CaptureCountText", "CapturadoText");
    }

    private void BindButton()
    {
        if (hasBoundButton || button == null)
            return;

        button.onClick.AddListener(HandleSelected);
        hasBoundButton = true;
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

    private static string GetFishDisplayName(FishScriptableObject _fish)
    {
        if (_fish == null)
            return "Peixe";

        return !string.IsNullOrWhiteSpace(_fish.fishName) ? _fish.fishName : _fish.name;
    }

    private static void SetObjectActive(GameObject _target, bool _active)
    {
        if (_target != null)
            _target.SetActive(_active);
    }
}
