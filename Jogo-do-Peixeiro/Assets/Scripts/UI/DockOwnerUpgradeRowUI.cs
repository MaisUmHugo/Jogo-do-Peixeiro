using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class DockOwnerUpgradeRowUI : MonoBehaviour
{
    [SerializeField] private DockUpgradeType upgradeType;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text levelText;
    [FormerlySerializedAs("valueText")]
    [SerializeField] private TMP_Text effectText;
    [SerializeField] private TMP_Text costText;
    [SerializeField] private Button buyButton;
    [SerializeField] private RectTransform scrollTarget;
    [SerializeField] private Image[] progressPips;
    [SerializeField] private Color filledPipColor = new Color(0.95f, 0.75f, 0.25f, 1f);
    [SerializeField] private Color emptyPipColor = new Color(1f, 1f, 1f, 0.18f);

    public DockUpgradeType UpgradeType => upgradeType;
    public Button BuyButton => buyButton;
    public bool HasTextTarget => nameText != null || descriptionText != null || levelText != null || effectText != null || costText != null;

    public bool ContainsSelection(GameObject _selectedObject)
    {
        if (_selectedObject == null)
            return false;

        Transform selectedTransform = _selectedObject.transform;

        if (selectedTransform == transform || selectedTransform.IsChildOf(transform))
            return true;

        if (buyButton == null)
            return false;

        Transform buttonTransform = buyButton.transform;
        return selectedTransform == buttonTransform || selectedTransform.IsChildOf(buttonTransform);
    }

    public RectTransform GetScrollTarget(RectTransform _scrollContent)
    {
        if (scrollTarget != null)
            return scrollTarget;

        RectTransform ownRect = transform as RectTransform;
        RectTransform buttonRect = buyButton != null ? buyButton.GetComponent<RectTransform>() : null;
        RectTransform sharedParent = GetSharedRowParent(ownRect, buttonRect, _scrollContent);

        if (sharedParent != null)
            return sharedParent;

        if (ownRect != null)
            return ownRect;

        return buttonRect;
    }

    public void SetUpgrade(
        string _name,
        string _description,
        string _level,
        string _effect,
        string _cost,
        int _currentLevel,
        int _maxLevel,
        bool _canBuy)
    {
        if (UsesSingleSummaryText())
        {
            nameText.text = string.Join(
                "\n",
                _name,
                _description,
                _level,
                _effect,
                _cost
            );
        }
        else
        {
            SetText(nameText, _name);
            SetText(descriptionText, _description);
            SetText(levelText, _level);
            SetText(effectText, _effect);
            SetText(costText, _cost);
        }

        if (buyButton != null)
            buyButton.interactable = _canBuy;

        SetProgress(_currentLevel, _maxLevel);
    }

    private bool UsesSingleSummaryText()
    {
        return nameText != null &&
               descriptionText == null &&
               levelText == null &&
               effectText == null &&
               costText == null;
    }

    private void SetText(TMP_Text _text, string _value)
    {
        if (_text != null)
            _text.text = _value;
    }

    private RectTransform GetSharedRowParent(RectTransform _ownRect, RectTransform _buttonRect, RectTransform _scrollContent)
    {
        if (_ownRect == null || _buttonRect == null || _ownRect.parent != _buttonRect.parent)
            return null;

        RectTransform parentRect = _ownRect.parent as RectTransform;

        if (parentRect == null || parentRect == _scrollContent)
            return null;

        return parentRect;
    }

    private void SetProgress(int _currentLevel, int _maxLevel)
    {
        if (progressPips == null || progressPips.Length == 0)
            return;

        int safeLevel = Mathf.Max(0, _currentLevel);
        int safeMaxLevel = Mathf.Max(1, _maxLevel);

        for (int i = 0; i < progressPips.Length; i++)
        {
            if (progressPips[i] == null)
                continue;

            int pipLevel = Mathf.CeilToInt((i + 1) * safeMaxLevel / (float)progressPips.Length);
            progressPips[i].color = safeLevel >= pipLevel ? filledPipColor : emptyPipColor;
        }
    }

}
