using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class FishDirectionPullUI : MonoBehaviour
{
    private const int MaxPromptIcons = 2;

    [Header("References")]
    [FormerlySerializedAs("directionPull")]
    [SerializeField] private FishDirectionPull _directionPull;
    [SerializeField] private FishSkillCheck _fishSkillCheck;
    [FormerlySerializedAs("iconDatabase")]
    [SerializeField] private InputIconDatabase _iconDatabase;
    [FormerlySerializedAs("directionIcon")]
    [SerializeField] private Image _directionIcon;
    [SerializeField] private Vector2 _iconAnchoredPosition = new Vector2(0f, 120f);
    [SerializeField] private Vector2 _iconSize = new Vector2(72f, 72f);

    [Header("Position")]
    [SerializeField] private float _horizontalPullOffset = 180f;
    [SerializeField] private float _verticalPullOffset = 120f;
    [SerializeField] private bool _randomizePositionOnDirectionChange = true;
    [SerializeField] private bool _placeComboIconsByDirection = true;
    [SerializeField] private float _sideVerticalOffset = 110f;
    [SerializeField] private float _sideHorizontalJitter = 45f;
    [SerializeField] private float _topHorizontalRange = 260f;
    [SerializeField] private float _comboIconSpacing = 132f;
    [SerializeField] private float _positionFollowSpeed = 18f;

    [Header("Pulse")]
    [SerializeField] private float _pulseSpeed = 6f;
    [SerializeField] private float _activeIconBaseScale = 1f;
    [SerializeField] private float _activeIconMinimumScale = 0.45f;
    [SerializeField] private float _completedIconScale = 0.92f;
    [SerializeField] private float _pendingIconScale = 0.76f;
    [SerializeField] private float _idlePulseScale = 0.1f;
    [SerializeField] private float _activePulseScale = 0.22f;
    [SerializeField, Range(0f, 1f)] private float _pendingAlpha = 0.45f;
    [SerializeField, Range(0f, 1f)] private float _completedAlpha = 0.85f;
    [SerializeField, Range(0f, 1f)] private float _idleAlpha = 0.72f;
    [SerializeField, Range(0f, 1f)] private float _activeAlpha = 1f;

    [Header("Completion Fill")]
    [SerializeField] private bool _useIconCompletionFill = true;
    [SerializeField] private bool _fillIconFromBottom = true;
    [SerializeField] private Image _completionFillIcon;
    [SerializeField] private Color _iconIncompleteColor = new Color(0.25f, 0.25f, 0.25f, 0.75f);
    [SerializeField] private Color _iconCompleteColor = Color.white;
    [SerializeField] private Color _completionFillColor = new Color(0.15f, 1f, 0.45f, 1f);

    private RectTransform _directionIconRect;
    private Image[] _directionIcons;
    private Image[] _completionFillIcons;
    private RectTransform[] _directionIconRects;
    private FishDirectionPull.FishForceDirection _lastRequiredPullDirection;
    private Vector2 _currentIconTargetPosition;
    private Vector2[] _currentIconTargetPositions;
    private int _lastPromptId = -1;
    private bool _hasIconTargetPosition;

    private void OnValidate()
    {
        _iconSize.x = Mathf.Max(1f, _iconSize.x);
        _iconSize.y = Mathf.Max(1f, _iconSize.y);
        _horizontalPullOffset = Mathf.Max(0f, _horizontalPullOffset);
        _verticalPullOffset = Mathf.Max(0f, _verticalPullOffset);
        _sideVerticalOffset = Mathf.Max(0f, _sideVerticalOffset);
        _sideHorizontalJitter = Mathf.Max(0f, _sideHorizontalJitter);
        _topHorizontalRange = Mathf.Max(0f, _topHorizontalRange);
        _comboIconSpacing = Mathf.Max(0f, _comboIconSpacing);
        _positionFollowSpeed = Mathf.Max(0f, _positionFollowSpeed);
        _pulseSpeed = Mathf.Max(0f, _pulseSpeed);
        _activeIconBaseScale = Mathf.Max(0.01f, _activeIconBaseScale);
        _activeIconMinimumScale = Mathf.Clamp(_activeIconMinimumScale, 0.01f, _activeIconBaseScale);
        _completedIconScale = Mathf.Max(0.01f, _completedIconScale);
        _pendingIconScale = Mathf.Max(0.01f, _pendingIconScale);
        _idlePulseScale = Mathf.Max(0f, _idlePulseScale);
        _activePulseScale = Mathf.Max(_idlePulseScale, _activePulseScale);
    }

    private void Awake()
    {
        if (_directionPull == null)
            _directionPull = FindFirstObjectByType<FishDirectionPull>();

        if (_fishSkillCheck == null)
            _fishSkillCheck = FindFirstObjectByType<FishSkillCheck>(FindObjectsInactive.Include);

        if (_directionIcon == null)
            _directionIcon = GetComponent<Image>();

        if (_directionIcon == null)
            _directionIcon = CreateDirectionIcon();

        _directionIconRect = _directionIcon.rectTransform;
        EnsurePromptIcons();
        SetVisible(false);
    }

    private void OnEnable()
    {
        SubscribeToEvents();
        RefreshDisplay();
    }

    private void OnDisable()
    {
        UnsubscribeFromEvents();
        SetVisible(false);
    }

    private void SubscribeToEvents()
    {
        if (_directionPull != null)
        {
            _directionPull.PullActiveChanged += HandlePullActiveChanged;
            _directionPull.PullStopped += HandlePullStopped;
            _directionPull.PromptChanged += HandlePromptChanged;
            _directionPull.PromptStateChanged += HandlePromptStateChanged;
        }

        InputDeviceDetector.DeviceTypeChanged += HandleDeviceTypeChanged;
    }

    private void UnsubscribeFromEvents()
    {
        if (_directionPull != null)
        {
            _directionPull.PullActiveChanged -= HandlePullActiveChanged;
            _directionPull.PullStopped -= HandlePullStopped;
            _directionPull.PromptChanged -= HandlePromptChanged;
            _directionPull.PromptStateChanged -= HandlePromptStateChanged;
        }

        InputDeviceDetector.DeviceTypeChanged -= HandleDeviceTypeChanged;
    }

    private void HandlePullActiveChanged(bool _isActive)
    {
        RefreshDisplay();
    }

    private void HandlePullStopped()
    {
        SetVisible(false);
    }

    private void HandlePromptChanged()
    {
        _hasIconTargetPosition = false;
        _lastPromptId = -1;
        RefreshDisplay();
    }

    private void HandlePromptStateChanged()
    {
        RefreshDisplay();
    }

    private void HandleDeviceTypeChanged(InputDeviceType _deviceType)
    {
        RefreshDisplay();
    }

    private void RefreshDisplay()
    {
        if (!CanShowDirectionPull())
        {
            SetVisible(false);
            return;
        }

        SetVisible(true);

        if (!UpdateIcon())
            return;

        UpdateIconFeedback();
    }

    private bool UpdateIcon()
    {
        if (_directionIcon == null || _iconDatabase == null)
            return false;

        int iconCount = Mathf.Clamp(_directionPull.RequiredPullDirectionCount, 1, MaxPromptIcons);

        for (int i = 0; i < MaxPromptIcons; i++)
        {
            bool shouldShowIcon = i < iconCount;

            if (_directionIcons[i] != null)
                _directionIcons[i].gameObject.SetActive(shouldShowIcon);

            if (_completionFillIcons[i] != null)
                _completionFillIcons[i].gameObject.SetActive(shouldShowIcon && _useIconCompletionFill);

            if (!shouldShowIcon)
                continue;

            InputIconAction action = GetDirectionAction(_directionPull.GetRequiredPullDirection(i));
            Sprite icon = _iconDatabase.GetIcon(InputDeviceDetector.CurrentDeviceType, action);

            if (icon == null)
            {
                SetVisible(false);
                return false;
            }

            _directionIcons[i].sprite = icon;
            UpdateCompletionFillSprite(i, icon);
        }

        return true;
    }

    private void UpdateIconFeedback()
    {
        if (_directionIcon == null || _directionIconRect == null || _directionPull == null)
            return;

        UpdateIconTargetPosition();

        float followT = 1f - Mathf.Exp(-_positionFollowSpeed * Time.unscaledDeltaTime);

        float pulseT = (Mathf.Sin(Time.unscaledTime * _pulseSpeed) * 0.5f) + 0.5f;
        int iconCount = Mathf.Clamp(_directionPull.RequiredPullDirectionCount, 1, MaxPromptIcons);

        for (int i = 0; i < iconCount; i++)
        {
            if (_directionIconRects[i] == null || _directionIcons[i] == null)
                continue;

            Vector2 iconOffset = _placeComboIconsByDirection ? Vector2.zero : GetPromptIconOffset(i, iconCount);
            Vector2 targetPosition = _currentIconTargetPositions != null && i < _currentIconTargetPositions.Length
                ? _currentIconTargetPositions[i]
                : _currentIconTargetPosition;

            _directionIconRects[i].anchoredPosition = Vector2.Lerp(
                _directionIconRects[i].anchoredPosition,
                targetPosition + iconOffset,
                followT
            );

            bool isActive = _directionPull.IsRequiredPullDirectionActive(i);
            bool isCompleted = _directionPull.IsRequiredPullDirectionCompleted(i);
            float iconProgress = _directionPull.GetRequiredPullDirectionProgress(i);
            float scale = GetIconScale(isActive, isCompleted, pulseT);
            Color color = GetIconColor(isActive, isCompleted, iconProgress);

            _directionIconRects[i].localScale = Vector3.one * scale;
            _directionIcons[i].color = color;
        }

        UpdateCompletionFill();
    }

    private float GetIconScale(bool _isActive, bool _isCompleted, float _pulseT)
    {
        if (_isCompleted)
            return _completedIconScale;

        if (!_isActive)
            return _pendingIconScale;

        float timerScale = Mathf.Lerp(
            _activeIconMinimumScale,
            _activeIconBaseScale,
            _directionPull.ActiveDirectionTimeNormalized
        );
        float pulseAmount = Mathf.Lerp(_idlePulseScale, _activePulseScale, _directionPull.PullInputNormalized);
        return timerScale + (_pulseT * pulseAmount);
    }

    private Color GetIconColor(bool _isActive, bool _isCompleted, float _progress)
    {
        if (!_useIconCompletionFill)
            return Color.white;

        Color color = Color.Lerp(_iconIncompleteColor, _iconCompleteColor, _progress);

        if (_isCompleted)
        {
            color.a *= _completedAlpha;
            return color;
        }

        if (!_isActive)
        {
            color.a *= _pendingAlpha;
            return color;
        }

        color.a *= Mathf.Lerp(_idleAlpha, _activeAlpha, _directionPull.PullInputNormalized);
        return color;
    }

    private void UpdateCompletionFill()
    {
        if (!_useIconCompletionFill || _directionPull == null || _completionFillIcons == null)
            return;

        int iconCount = Mathf.Clamp(_directionPull.RequiredPullDirectionCount, 1, MaxPromptIcons);

        for (int i = 0; i < iconCount; i++)
        {
            if (_completionFillIcons[i] == null)
                continue;

            _completionFillIcons[i].gameObject.SetActive(true);
            _completionFillIcons[i].color = _completionFillColor;
            _completionFillIcons[i].type = Image.Type.Filled;
            _completionFillIcons[i].fillMethod = Image.FillMethod.Vertical;
            _completionFillIcons[i].fillOrigin = _fillIconFromBottom ? (int)Image.OriginVertical.Bottom : (int)Image.OriginVertical.Top;
            _completionFillIcons[i].fillAmount = _directionPull.GetRequiredPullDirectionProgress(i);
        }
    }

    private bool CanShowDirectionPull()
    {
        return _directionPull != null &&
               _directionPull.UseDirectionalPull &&
               _directionPull.IsPullActive &&
               !_directionPull.IsCompletionComplete &&
               !IsSkillCheckActive() &&
               _iconDatabase != null &&
               _directionIcon != null;
    }

    private InputIconAction GetDirectionAction(FishDirectionPull.FishForceDirection _direction)
    {
        switch (_direction)
        {
            case FishDirectionPull.FishForceDirection.Left:
                return InputIconAction.MoveLeft;

            case FishDirectionPull.FishForceDirection.Right:
                return InputIconAction.MoveRight;

            case FishDirectionPull.FishForceDirection.Up:
                return InputIconAction.MoveUp;

            case FishDirectionPull.FishForceDirection.Down:
                return InputIconAction.MoveDown;

            default:
                return InputIconAction.MoveLeft;
        }
    }

    private Vector2 GetIconPosition(FishDirectionPull.FishForceDirection _direction)
    {
        switch (_direction)
        {
            case FishDirectionPull.FishForceDirection.Left:
                return GetSidePullPosition(-1f);

            case FishDirectionPull.FishForceDirection.Right:
                return GetSidePullPosition(1f);

            case FishDirectionPull.FishForceDirection.Up:
                return GetTopPullPosition();

            case FishDirectionPull.FishForceDirection.Down:
                return GetTopPullPosition();

            default:
                return _iconAnchoredPosition;
        }
    }

    private void UpdateIconTargetPosition()
    {
        int promptId = _directionPull.CurrentPromptId;

        if (_hasIconTargetPosition && promptId == _lastPromptId)
            return;

        _lastPromptId = promptId;
        _lastRequiredPullDirection = _directionPull.RequiredPullDirection;
        _currentIconTargetPosition = GetPromptPosition();

        int iconCount = Mathf.Clamp(_directionPull.RequiredPullDirectionCount, 1, MaxPromptIcons);

        for (int i = 0; i < iconCount; i++)
            _currentIconTargetPositions[i] = GetPromptPosition(i, iconCount);

        _hasIconTargetPosition = true;
    }

    private Vector2 GetPromptPosition(int _index, int _iconCount)
    {
        if (!_placeComboIconsByDirection || _iconCount <= 1)
            return _currentIconTargetPosition;

        return GetIconPosition(_directionPull.GetRequiredPullDirection(_index));
    }

    private Vector2 GetPromptPosition()
    {
        Vector2 requiredVector = _directionPull.RequiredPullVector;

        if (Mathf.Abs(requiredVector.x) > 0.1f && Mathf.Abs(requiredVector.y) > 0.1f)
        {
            float sideJitter = _randomizePositionOnDirectionChange ? Random.Range(-_sideHorizontalJitter, _sideHorizontalJitter) : 0f;

            return new Vector2(
                _iconAnchoredPosition.x + (_horizontalPullOffset * Mathf.Sign(requiredVector.x)) + sideJitter,
                _iconAnchoredPosition.y + _verticalPullOffset
            );
        }

        if (Mathf.Abs(requiredVector.x) > 0.1f)
            return GetSidePullPosition(Mathf.Sign(requiredVector.x));

        return GetTopPullPosition();
    }

    private Vector2 GetPromptIconOffset(int _index, int _iconCount)
    {
        if (_iconCount <= 1)
            return Vector2.zero;

        float spacing = Mathf.Max(110f, _comboIconSpacing);
        float startOffset = -spacing * (_iconCount - 1) * 0.5f;
        return new Vector2(startOffset + (spacing * _index), 0f);
    }

    private Vector2 GetSidePullPosition(float _side)
    {
        float sideJitter = _randomizePositionOnDirectionChange ? Random.Range(-_sideHorizontalJitter, _sideHorizontalJitter) : 0f;
        float verticalOffset = _randomizePositionOnDirectionChange ? Random.Range(0f, _sideVerticalOffset) : _sideVerticalOffset;

        return new Vector2(
            _iconAnchoredPosition.x + (_horizontalPullOffset * _side) + sideJitter,
            _iconAnchoredPosition.y + verticalOffset
        );
    }

    private Vector2 GetTopPullPosition()
    {
        float horizontalOffset = _randomizePositionOnDirectionChange ? Random.Range(-_topHorizontalRange, _topHorizontalRange) : 0f;

        return new Vector2(
            _iconAnchoredPosition.x + horizontalOffset,
            _iconAnchoredPosition.y + _verticalPullOffset
        );
    }

    private bool IsSkillCheckActive()
    {
        return _fishSkillCheck != null && _fishSkillCheck.IsSkillCheckActive;
    }

    private void SetVisible(bool _visible)
    {
        if (_directionIcon != null)
            _directionIcon.gameObject.SetActive(_visible);

        if (_directionIcons != null)
        {
            for (int i = 0; i < _directionIcons.Length; i++)
            {
                if (_directionIcons[i] != null)
                    _directionIcons[i].gameObject.SetActive(_visible && i == 0);
            }
        }

        if (_completionFillIcons != null)
        {
            for (int i = 0; i < _completionFillIcons.Length; i++)
            {
                if (_completionFillIcons[i] != null)
                    _completionFillIcons[i].gameObject.SetActive(_visible && _useIconCompletionFill && i == 0);
            }
        }

        if (!_visible && _directionIconRect != null)
            _directionIconRect.localScale = Vector3.one;

        if (!_visible)
        {
            _hasIconTargetPosition = false;
            _lastPromptId = -1;
        }
    }

    private Image CreateDirectionIcon()
    {
        GameObject iconObject = new GameObject("DirectionPullIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        iconObject.transform.SetParent(transform, false);

        RectTransform rectTransform = iconObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = _iconAnchoredPosition;
        rectTransform.sizeDelta = _iconSize;

        Image iconImage = iconObject.GetComponent<Image>();
        iconImage.raycastTarget = false;
        iconImage.preserveAspect = true;
        iconImage.enabled = true;

        return iconImage;
    }

    private void EnsurePromptIcons()
    {
        _directionIcons = new Image[MaxPromptIcons];
        _completionFillIcons = new Image[MaxPromptIcons];
        _directionIconRects = new RectTransform[MaxPromptIcons];
        _currentIconTargetPositions = new Vector2[MaxPromptIcons];

        _directionIcons[0] = _directionIcon;
        _directionIconRects[0] = _directionIcon.rectTransform;
        _completionFillIcons[0] = EnsureCompletionFillIcon(_directionIcon, _completionFillIcon, "DirectionPullCompletionFill");

        for (int i = 1; i < MaxPromptIcons; i++)
        {
            _directionIcons[i] = CreateDirectionIcon($"DirectionPullIcon{i + 1}");
            _directionIconRects[i] = _directionIcons[i].rectTransform;
            _completionFillIcons[i] = EnsureCompletionFillIcon(_directionIcons[i], null, $"DirectionPullCompletionFill{i + 1}");
        }
    }

    private Image EnsureCompletionFillIcon(Image _targetIcon, Image _existingFillIcon, string _objectName)
    {
        if (_targetIcon == null || !_useIconCompletionFill)
            return null;

        Image fillIcon = _existingFillIcon != null ? _existingFillIcon : CreateCompletionFillIcon(_targetIcon, _objectName);

        fillIcon.raycastTarget = false;
        fillIcon.preserveAspect = true;
        fillIcon.type = Image.Type.Filled;
        fillIcon.fillMethod = Image.FillMethod.Vertical;
        fillIcon.fillOrigin = _fillIconFromBottom ? (int)Image.OriginVertical.Bottom : (int)Image.OriginVertical.Top;

        return fillIcon;
    }

    private void UpdateCompletionFillSprite(int _index, Sprite _sprite)
    {
        if (_completionFillIcons == null || _index < 0 || _index >= _completionFillIcons.Length || _completionFillIcons[_index] == null)
            return;

        _completionFillIcons[_index].sprite = _sprite;
    }

    private Image CreateCompletionFillIcon(Image _targetIcon, string _objectName)
    {
        GameObject fillObject = new GameObject(_objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        fillObject.transform.SetParent(_targetIcon.transform, false);
        fillObject.transform.SetAsLastSibling();

        RectTransform rectTransform = fillObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.sizeDelta = Vector2.zero;

        Image fillImage = fillObject.GetComponent<Image>();
        fillImage.color = _completionFillColor;
        fillImage.fillAmount = 0f;

        return fillImage;
    }

    private Image CreateDirectionIcon(string _objectName)
    {
        Image iconImage = CreateDirectionIcon();
        iconImage.gameObject.name = _objectName;
        return iconImage;
    }
}
