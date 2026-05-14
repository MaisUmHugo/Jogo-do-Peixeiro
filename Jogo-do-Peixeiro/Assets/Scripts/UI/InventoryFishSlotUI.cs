using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryFishSlotUI : MonoBehaviour
{
    [SerializeField] private GameObject contentRoot;
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text weightText;
    [SerializeField] private TMP_Text valueText;

    public void SetFish(FishData _fish)
    {
        InitializeReferences();

        bool hasFish = _fish != null && _fish.typeOfFish != null;
        SetContentVisible(hasFish);

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
        SetContentVisible(false);
    }

    private void InitializeReferences()
    {
        if (contentRoot == null)
            contentRoot = gameObject;

        if (iconImage == null)
            iconImage = GetComponentInChildren<Image>(true);

        TMP_Text[] texts = GetComponentsInChildren<TMP_Text>(true);

        if (nameText == null && texts.Length > 0)
            nameText = texts[0];

        if (weightText == null && texts.Length > 1)
            weightText = texts[1];

        if (valueText == null && texts.Length > 2)
            valueText = texts[2];
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
