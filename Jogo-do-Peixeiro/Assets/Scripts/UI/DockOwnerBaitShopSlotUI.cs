using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DockOwnerBaitShopSlotUI : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text ownedQuantityText;
    [SerializeField] private TMP_Text priceText;
    [SerializeField] private Button buyButton;

    public Button BuyButton => buyButton;

    private GameObject RootObject => root != null ? root : gameObject;

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnValidate()
    {
        ResolveReferences();
    }

    public void SetBait(BaitData _bait, int _ownedQuantity, int _maxQuantity, int _unitCost)
    {
        ResolveReferences();

        bool hasBait = _bait != null;
        SetVisible(hasBait);

        if (buyButton != null)
            buyButton.interactable = hasBait && _maxQuantity > 0;

        if (!hasBait)
            return;

        if (nameText != null)
            nameText.text = _bait.BaitName;

        if (descriptionText != null)
        {
            descriptionText.text = string.IsNullOrWhiteSpace(_bait.Description)
                ? "Bonus de pesca."
                : _bait.Description;
        }

        if (ownedQuantityText != null)
            ownedQuantityText.text = $"No inventário: {_ownedQuantity}";

        if (priceText != null)
            priceText.text = _unitCost > 0 ? $"Preço: R$ {_unitCost}" : "Grátis";

        if (iconImage != null)
        {
            iconImage.sprite = _bait.InventoryIcon;
            iconImage.enabled = _bait.InventoryIcon != null;
            iconImage.preserveAspect = true;
        }
    }

    public void Clear()
    {
        ResolveReferences();

        if (buyButton != null)
            buyButton.interactable = false;

        SetVisible(false);
    }

    public bool Contains(GameObject _target)
    {
        return _target != null &&
               RootObject != null &&
               (_target == RootObject || _target.transform.IsChildOf(RootObject.transform));
    }

    public Selectable GetSelectable()
    {
        ResolveReferences();
        return buyButton;
    }

    private void SetVisible(bool _visible)
    {
        if (RootObject != null)
            RootObject.SetActive(_visible);
    }

    private void ResolveReferences()
    {
        if (root == null)
            root = gameObject;

        if (buyButton == null)
            buyButton = GetComponentInChildren<Button>(true);

        if (iconImage == null)
            iconImage = FindImageByName("IconImage", "Icon", "IconeImage", "BaitIcon", "IscaIcon");

        if (nameText == null)
            nameText = FindTextByName("NameText", "NomeText", "BaitNameText", "TitleText");

        if (descriptionText == null)
            descriptionText = FindTextByName("DescriptionText", "DescricaoText", "BaitDescriptionText");

        if (ownedQuantityText == null)
            ownedQuantityText = FindTextByName("OwnedQuantityText", "OwnedText", "InventoryQuantityText", "QuantidadeAtualText");

        if (priceText == null)
            priceText = FindTextByName("PriceText", "PrecoText", "CostText");

    }

    private Image FindImageByName(params string[] _names)
    {
        Image[] images = GetComponentsInChildren<Image>(true);

        for (int i = 0; i < images.Length; i++)
        {
            for (int j = 0; j < _names.Length; j++)
            {
                if (string.Equals(images[i].gameObject.name, _names[j], System.StringComparison.OrdinalIgnoreCase))
                    return images[i];
            }
        }

        return null;
    }

    private TMP_Text FindTextByName(params string[] _names)
    {
        TMP_Text[] texts = GetComponentsInChildren<TMP_Text>(true);

        for (int i = 0; i < texts.Length; i++)
        {
            for (int j = 0; j < _names.Length; j++)
            {
                if (string.Equals(texts[i].gameObject.name, _names[j], System.StringComparison.OrdinalIgnoreCase))
                    return texts[i];
            }
        }

        return null;
    }
}
