using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class FishDirectionPullUI : MonoBehaviour
{
    #region Fields

    [Header("References")]
    [FormerlySerializedAs("directionPull")]
    [SerializeField] private FishDirectionPull _directionPull;
    [FormerlySerializedAs("iconDatabase")]
    [SerializeField] private InputIconDatabase _iconDatabase;
    [FormerlySerializedAs("directionIcon")]
    [SerializeField] private Image _directionIcon;
    [SerializeField] private Vector2 _iconAnchoredPosition = new Vector2(0f, 120f);
    [SerializeField] private Vector2 _iconSize = new Vector2(72f, 72f);

    [Header("Backplate")]
    [SerializeField] private bool _useDirectionBackplate = true;
    [SerializeField] private Image _directionBackplate;
    [SerializeField] private Vector2 _backplateSize = new Vector2(100f, 100f);
    [SerializeField] private Color _backplateColor = new Color(0.03f, 0.04f, 0.05f, 0.72f);

    [Header("Position")]
    [SerializeField] private float _horizontalPullOffset = 260f;
    [FormerlySerializedAs("_verticalPullOffset")]
    [SerializeField] private float _upPullOffset = 180f;
    [SerializeField] private float _downPullOffset = 180f;
    [SerializeField] private bool _randomizePositionOnDirectionChange;
    [SerializeField] private float _sideVerticalOffset = 110f;
    [SerializeField, Range(0f, 1f)] private float _sideButtonHeightMultiplier = 0.55f;
    [SerializeField] private float _sideHorizontalJitter = 45f;
    [SerializeField] private float _topHorizontalRange = 260f;
    [SerializeField] private float _positionFollowSpeed = 18f;

    [Header("Canvas Relative Position")]
    [SerializeField] private bool _useCanvasRelativeOffsets;
    [SerializeField, Range(0f, 0.5f)] private float _horizontalCanvasOffset = 0.32f;
    [SerializeField, Range(0f, 0.5f)] private float _sideVerticalCanvasOffset = 0.12f;
    [SerializeField, Range(0f, 0.5f)] private float _upCanvasOffset = 0.28f;
    [SerializeField, Range(0f, 0.5f)] private float _downCanvasOffset = 0.28f;
    [SerializeField] private bool _clampInsideParent = true;
    [SerializeField] private Vector2 _parentPadding = new Vector2(72f, 72f);

    [Header("Pulse")]
    [SerializeField] private float _pulseSpeed = 6f;
    [SerializeField] private float _activeIconBaseScale = 1f;
    [SerializeField] private float _activeIconMinimumScale = 0.45f;
    [SerializeField] private float _idlePulseScale = 0.1f;
    [SerializeField] private float _activePulseScale = 0.22f;
    [SerializeField, Range(0f, 1f)] private float _idleAlpha = 0.72f;
    [SerializeField, Range(0f, 1f)] private float _activeAlpha = 1f;

    [Header("Input Color")]
    [SerializeField] private Color _idleIconColor = Color.white;
    [SerializeField] private Color _pressedIconColor = new Color(0.25f, 1f, 0.35f, 1f);

    [Header("First Pull Hint")]
    [SerializeField] private TMP_Text _holdHintText;
    [SerializeField] private string _holdHintMessage = "SEGURE A DIRECAO";
    [SerializeField] private Vector2 _holdHintOffset = new Vector2(0f, 58f);
    [SerializeField] private float _holdHintCorrectHoldTime = 0.45f;
    [SerializeField] private float _holdHintPulseSpeed = 6f;
    [SerializeField] private float _holdHintPulseScale = 0.12f;
    [SerializeField] private Color _holdHintColor = Color.white;
    [SerializeField, Min(1)] private int _successfulPullsBeforeHidingHint = 5;

    [Header("Side Prompt Height")]
    [SerializeField, Range(0f, 1f)] private float _sidePullHeightCentering = 0.75f;

    [Header("Runtime Fallback")]
    [SerializeField] private bool _allowRuntimeFallback;
    [SerializeField] private bool _logMissingReferences = true;

    private RectTransform _directionIconRect;
    private RectTransform _directionBackplateRect;
    private RectTransform _holdHintRect;
    private Vector2 _currentIconTargetPosition;
    private int _lastPromptId = -1;
    private bool _hasIconTargetPosition;
    private bool _hasLearnedHoldInput;
    private int _successfulHoldHintCount;
    private int _lastSuccessfulHoldPromptId = -1;
    private float _correctHoldTimer;
    private bool _hasLoggedMissingIcon;
    private bool _hasLoggedMissingBackplate;
    private bool _hasLoggedMissingHoldHint;

    #endregion

    #region Unity Lifecycle

    private void OnValidate()
    {
        _iconSize.x = Mathf.Max(1f, _iconSize.x);
        _iconSize.y = Mathf.Max(1f, _iconSize.y);
        _backplateSize.x = Mathf.Max(1f, _backplateSize.x);
        _backplateSize.y = Mathf.Max(1f, _backplateSize.y);
        _horizontalPullOffset = Mathf.Max(0f, _horizontalPullOffset);
        _upPullOffset = Mathf.Max(0f, _upPullOffset);
        _downPullOffset = Mathf.Max(0f, _downPullOffset);
        _sideVerticalOffset = Mathf.Max(0f, _sideVerticalOffset);
        _sideButtonHeightMultiplier = Mathf.Clamp01(_sideButtonHeightMultiplier);
        _sideHorizontalJitter = Mathf.Max(0f, _sideHorizontalJitter);
        _topHorizontalRange = Mathf.Max(0f, _topHorizontalRange);
        _positionFollowSpeed = Mathf.Max(0f, _positionFollowSpeed);
        _parentPadding.x = Mathf.Max(0f, _parentPadding.x);
        _parentPadding.y = Mathf.Max(0f, _parentPadding.y);
        _pulseSpeed = Mathf.Max(0f, _pulseSpeed);
        _activeIconBaseScale = Mathf.Max(0.01f, _activeIconBaseScale);
        _activeIconMinimumScale = Mathf.Clamp(_activeIconMinimumScale, 0.01f, _activeIconBaseScale);
        _idlePulseScale = Mathf.Max(0f, _idlePulseScale);
        _activePulseScale = Mathf.Max(_idlePulseScale, _activePulseScale);
        _holdHintCorrectHoldTime = Mathf.Max(0.01f, _holdHintCorrectHoldTime);
        _holdHintPulseSpeed = Mathf.Max(0f, _holdHintPulseSpeed);
        _holdHintPulseScale = Mathf.Max(0f, _holdHintPulseScale);
        _successfulPullsBeforeHidingHint = Mathf.Max(1, _successfulPullsBeforeHidingHint);
        _sidePullHeightCentering = Mathf.Clamp01(_sidePullHeightCentering);

        if (Application.isPlaying && isActiveAndEnabled)
            RefreshRuntimePosition();
    }

    private void Awake()
    {
        ResolveReferences();
        SetVisible(false);
    }

    private void OnEnable()
    {
        ResolveReferences();
        SubscribeToEvents();
        RefreshDisplay();
    }

    private void OnDisable()
    {
        UnsubscribeFromEvents();
        SetVisible(false);
    }

    #endregion

    #region Event Subscriptions

    private void SubscribeToEvents()
    {
        ResolveReferences();

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

    #endregion

    #region Event Handlers

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
        _correctHoldTimer = 0f;
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

    #endregion

    #region Display Refresh

    private void RefreshDisplay()
    {
        if (!CanShowDirectionPull())
        {
            SetVisible(false);
            return;
        }

        if (!UpdateIcon())
            return;

        SetVisible(true);
        UpdateIconFeedback();
        UpdateHoldHintFeedback();
    }

    private bool UpdateIcon()
    {
        if (_directionIcon == null || _iconDatabase == null)
            return false;

        InputIconAction action = GetDirectionAction(_directionPull.RequiredPullDirection);
        Sprite icon = _iconDatabase.GetIcon(InputDeviceDetector.CurrentDeviceType, action);

        if (icon == null)
        {
            SetVisible(false);
            return false;
        }

        _directionIcon.sprite = icon;
        return true;
    }

    private void UpdateIconFeedback()
    {
        if (_directionIcon == null || _directionIconRect == null || _directionPull == null)
            return;

        UpdateIconTargetPosition();

        float followT = 1f - Mathf.Exp(-_positionFollowSpeed * Time.unscaledDeltaTime);
        float pulseT = (Mathf.Sin(Time.unscaledTime * _pulseSpeed) * 0.5f) + 0.5f;
        float inputAmount = _directionPull.PullInputNormalized;

        _directionIconRect.anchoredPosition = Vector2.Lerp(
            _directionIconRect.anchoredPosition,
            _currentIconTargetPosition,
            followT
        );

        float timerScale = Mathf.Lerp(
            _activeIconMinimumScale,
            _activeIconBaseScale,
            _directionPull.ActiveDirectionTimeNormalized
        );
        float pulseAmount = Mathf.Lerp(_idlePulseScale, _activePulseScale, inputAmount);
        _directionIconRect.localScale = Vector3.one * (timerScale + (pulseT * pulseAmount));
        UpdateBackplateFeedback();

        Color color = Color.Lerp(_idleIconColor, _pressedIconColor, inputAmount);
        color.a = Mathf.Lerp(_idleAlpha, _activeAlpha, inputAmount);
        _directionIcon.color = color;
    }

    private void UpdateBackplateFeedback()
    {
        if (!_useDirectionBackplate || _directionBackplate == null || _directionBackplateRect == null || _directionIconRect == null)
            return;

        _directionBackplateRect.anchoredPosition = _directionIconRect.anchoredPosition;
        _directionBackplateRect.localScale = _directionIconRect.localScale;
        _directionBackplateRect.sizeDelta = _backplateSize;
        _directionBackplate.color = _backplateColor;
        PlaceBackplateBehindIcon();
    }

    private void UpdateHoldHintFeedback()
    {
        if (_hasLearnedHoldInput || _holdHintText == null || _holdHintRect == null || _directionIconRect == null || _directionPull == null)
        {
            SetHoldHintVisible(false);
            return;
        }

        if (_directionPull.IsCorrectPullInput)
        {
            _correctHoldTimer += Time.unscaledDeltaTime;

            if (_correctHoldTimer >= _holdHintCorrectHoldTime &&
                _lastSuccessfulHoldPromptId != _directionPull.CurrentPromptId)
            {
                _lastSuccessfulHoldPromptId = _directionPull.CurrentPromptId;
                _successfulHoldHintCount++;

                if (_successfulHoldHintCount >= _successfulPullsBeforeHidingHint)
                {
                    _hasLearnedHoldInput = true;
                    SetHoldHintVisible(false);
                    return;
                }
            }
        }
        else
        {
            _correctHoldTimer = 0f;
        }

        float pulse = (Mathf.Sin(Time.unscaledTime * _holdHintPulseSpeed) * 0.5f) + 0.5f;

        _holdHintText.text = _holdHintMessage;
        _holdHintText.color = _holdHintColor;
        _holdHintRect.anchoredPosition = _directionIconRect.anchoredPosition + _holdHintOffset;
        _holdHintRect.localScale = Vector3.one * (1f + (pulse * _holdHintPulseScale));

        SetHoldHintVisible(true);
    }

    private bool CanShowDirectionPull()
    {
        ResolveReferences();

        return _directionPull != null &&
               _directionPull.UseDirectionalPull &&
               _directionPull.IsPullActive &&
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

    #endregion

    #region Direction Positioning

    private void UpdateIconTargetPosition()
    {
        int promptId = _directionPull.CurrentPromptId;

        if (_hasIconTargetPosition && promptId == _lastPromptId)
            return;

        _lastPromptId = promptId;
        _currentIconTargetPosition = ClampToParent(GetIconPosition(_directionPull.RequiredPullDirection));
        _hasIconTargetPosition = true;
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
                return GetBottomPullPosition();

            default:
                return _iconAnchoredPosition;
        }
    }

    private Vector2 GetSidePullPosition(float _side)
    {
        float sideJitter = _randomizePositionOnDirectionChange ? Random.Range(-_sideHorizontalJitter, _sideHorizontalJitter) : 0f;
        float verticalOffset = _randomizePositionOnDirectionChange ? Random.Range(0f, GetSideVerticalOffset()) : GetSideVerticalOffset();
        float sideHeight = _iconAnchoredPosition.y + verticalOffset;
        float centeredSideHeight = Mathf.Lerp(sideHeight, 0f, _sidePullHeightCentering);

        return new Vector2(
            _iconAnchoredPosition.x + (GetHorizontalOffset() * _side) + sideJitter,
            centeredSideHeight
        );
    }

    private Vector2 GetTopPullPosition()
    {
        float horizontalOffset = _randomizePositionOnDirectionChange ? Random.Range(-_topHorizontalRange, _topHorizontalRange) : 0f;

        return new Vector2(
            _iconAnchoredPosition.x + horizontalOffset,
            _iconAnchoredPosition.y + GetUpOffset()
        );
    }

    private Vector2 GetBottomPullPosition()
    {
        float horizontalOffset = _randomizePositionOnDirectionChange ? Random.Range(-_topHorizontalRange, _topHorizontalRange) : 0f;

        return new Vector2(
            _iconAnchoredPosition.x + horizontalOffset,
            _iconAnchoredPosition.y - GetDownOffset()
        );
    }

    private float GetHorizontalOffset()
    {
        if (!_useCanvasRelativeOffsets)
            return _horizontalPullOffset;

        RectTransform parentRect = GetParentRect();
        return parentRect != null ? parentRect.rect.width * _horizontalCanvasOffset : _horizontalPullOffset;
    }

    private float GetSideVerticalOffset()
    {
        if (!_useCanvasRelativeOffsets)
            return _sideVerticalOffset * _sideButtonHeightMultiplier;

        RectTransform parentRect = GetParentRect();
        return parentRect != null
            ? parentRect.rect.height * _sideVerticalCanvasOffset * _sideButtonHeightMultiplier
            : _sideVerticalOffset * _sideButtonHeightMultiplier;
    }

    private float GetUpOffset()
    {
        if (!_useCanvasRelativeOffsets)
            return _upPullOffset;

        RectTransform parentRect = GetParentRect();
        return parentRect != null ? parentRect.rect.height * _upCanvasOffset : _upPullOffset;
    }

    private float GetDownOffset()
    {
        if (!_useCanvasRelativeOffsets)
            return _downPullOffset;

        RectTransform parentRect = GetParentRect();
        return parentRect != null ? parentRect.rect.height * _downCanvasOffset : _downPullOffset;
    }

    private Vector2 ClampToParent(Vector2 _position)
    {
        if (!_clampInsideParent)
            return _position;

        RectTransform parentRect = GetParentRect();

        if (parentRect == null)
            return _position;

        Rect rect = parentRect.rect;
        float minX = rect.xMin + _parentPadding.x;
        float maxX = rect.xMax - _parentPadding.x;
        float minY = rect.yMin + _parentPadding.y;
        float maxY = rect.yMax - _parentPadding.y;

        if (minX > maxX)
            minX = maxX = 0f;

        if (minY > maxY)
            minY = maxY = 0f;

        return new Vector2(
            Mathf.Clamp(_position.x, minX, maxX),
            Mathf.Clamp(_position.y, minY, maxY)
        );
    }

    #endregion

    #region Visibility And Runtime State

    private RectTransform GetParentRect()
    {
        if (_directionIconRect != null && _directionIconRect.parent is RectTransform iconParentRect)
            return iconParentRect;

        return transform as RectTransform;
    }

    private void SetVisible(bool _visible)
    {
        if (_directionBackplate != null)
        {
            bool showBackplate = _visible && _useDirectionBackplate;

            if (_directionBackplate.gameObject != gameObject)
                _directionBackplate.gameObject.SetActive(showBackplate);

            _directionBackplate.enabled = showBackplate;
        }

        if (_directionIcon != null)
        {
            if (_directionIcon.gameObject != gameObject)
                _directionIcon.gameObject.SetActive(_visible);

            _directionIcon.enabled = _visible;
        }

        if (!_visible && _directionIconRect != null)
            _directionIconRect.localScale = Vector3.one;

        if (!_visible && _directionBackplateRect != null)
            _directionBackplateRect.localScale = Vector3.one;

        SetHoldHintVisible(_visible && !_hasLearnedHoldInput);

        if (!_visible)
        {
            _hasIconTargetPosition = false;
            _lastPromptId = -1;
            _correctHoldTimer = 0f;
        }
    }

    private void RefreshRuntimePosition()
    {
        _hasIconTargetPosition = false;
        _lastPromptId = -1;
        RefreshDisplay();
    }

    #endregion

    #region Reference Resolution And Runtime UI

    private void ResolveReferences()
    {
        if (_directionPull == null)
            _directionPull = FindFirstObjectByType<FishDirectionPull>(FindObjectsInactive.Include);

        if (_iconDatabase == null)
            _iconDatabase = FindFirstObjectByType<InputIconDatabase>(FindObjectsInactive.Include);

        if (_directionIcon == null)
            _directionIcon = FindDirectionIcon();

        if (_directionIcon == null && _allowRuntimeFallback)
            _directionIcon = CreateDirectionIcon();

        if (_directionIcon == null)
        {
            LogMissingReference("DirectionPullIcon", ref _hasLoggedMissingIcon);
            return;
        }

        _directionIconRect = _directionIcon.rectTransform;
        EnsureDirectionBackplate();
        EnsureHoldHintText();
    }

    private Image FindDirectionIcon()
    {
        Transform existingIcon = transform.Find("DirectionPullIcon");

        if (existingIcon != null && existingIcon.TryGetComponent(out Image namedIcon))
            return namedIcon;

        Image selfImage = GetComponent<Image>();

        if (IsValidDirectionIcon(selfImage))
            return selfImage;

        Image[] childImages = GetComponentsInChildren<Image>(true);

        for (int i = 0; i < childImages.Length; i++)
        {
            if (IsValidDirectionIcon(childImages[i]))
                return childImages[i];
        }

        return null;
    }

    private bool IsValidDirectionIcon(Image _image)
    {
        if (_image == null || _image == _directionBackplate)
            return false;

        string imageName = _image.name.ToLowerInvariant();

        return _image != null &&
               !imageName.Contains("backplate") &&
               !imageName.Contains("background") &&
               !imageName.Contains("fundo");
    }

    private void EnsureDirectionBackplate()
    {
        if (!_useDirectionBackplate)
            return;

        if (_directionBackplate == null)
        {
            Transform existingBackplate = transform.Find("DirectionPullBackplate");

            if (existingBackplate != null)
                _directionBackplate = existingBackplate.GetComponent<Image>();
        }

        if (_directionBackplate == null && _allowRuntimeFallback)
            _directionBackplate = CreateDirectionBackplate();

        if (_directionBackplate == null)
            LogMissingReference("DirectionPullBackplate", ref _hasLoggedMissingBackplate);

        if (_directionBackplate == null)
            return;

        _directionBackplateRect = _directionBackplate.rectTransform;
        _directionBackplateRect.sizeDelta = _backplateSize;
        _directionBackplate.raycastTarget = false;
        _directionBackplate.color = _backplateColor;

        PlaceBackplateBehindIcon();
    }

    private void PlaceBackplateBehindIcon()
    {
        if (_directionBackplateRect == null ||
            _directionIconRect == null ||
            _directionBackplateRect.parent != _directionIconRect.parent)
        {
            return;
        }

        _directionBackplateRect.SetSiblingIndex(_directionIconRect.GetSiblingIndex());
        _directionIconRect.SetSiblingIndex(_directionBackplateRect.GetSiblingIndex() + 1);

        if (_holdHintRect != null && _holdHintRect.parent == _directionIconRect.parent)
            _holdHintRect.SetSiblingIndex(_directionIconRect.GetSiblingIndex() + 1);
    }

    private Image CreateDirectionBackplate()
    {
        Transform parent = _directionIconRect != null && _directionIconRect.parent != null
            ? _directionIconRect.parent
            : transform;

        GameObject backplateObject = new GameObject("DirectionPullBackplate", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        backplateObject.transform.SetParent(parent, false);

        RectTransform rectTransform = backplateObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = _iconAnchoredPosition;
        rectTransform.sizeDelta = _backplateSize;

        Image backplateImage = backplateObject.GetComponent<Image>();
        backplateImage.raycastTarget = false;
        backplateImage.preserveAspect = false;
        backplateImage.color = _backplateColor;
        backplateImage.enabled = true;

        return backplateImage;
    }

    private void EnsureHoldHintText()
    {
        if (_holdHintText == null)
        {
            Transform existingHint = transform.Find("DirectionPullHoldHint");

            if (existingHint != null)
                _holdHintText = existingHint.GetComponent<TMP_Text>();
        }

        if (_holdHintText == null && _allowRuntimeFallback)
            _holdHintText = CreateHoldHintText();

        if (_holdHintText == null)
            LogMissingReference("DirectionPullHoldHint", ref _hasLoggedMissingHoldHint);

        if (_holdHintText == null)
            return;

        _holdHintRect = _holdHintText.rectTransform;
        _holdHintText.raycastTarget = false;
        _holdHintText.text = _holdHintMessage;
        _holdHintText.color = _holdHintColor;
    }

    private TMP_Text CreateHoldHintText()
    {
        Transform parent = _directionIconRect != null && _directionIconRect.parent != null
            ? _directionIconRect.parent
            : transform;

        GameObject hintObject = new GameObject("DirectionPullHoldHint", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        hintObject.transform.SetParent(parent, false);

        RectTransform rectTransform = hintObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = _iconAnchoredPosition + _holdHintOffset;
        rectTransform.sizeDelta = new Vector2(160f, 40f);

        TextMeshProUGUI hintText = hintObject.GetComponent<TextMeshProUGUI>();
        hintText.alignment = TextAlignmentOptions.Center;
        hintText.fontSize = 24f;
        hintText.fontStyle = FontStyles.Bold;
        hintText.raycastTarget = false;
        hintText.text = _holdHintMessage;
        hintText.color = _holdHintColor;

        return hintText;
    }

    #endregion

    #region Runtime Helpers And Debug

    private void SetHoldHintVisible(bool _visible)
    {
        if (_holdHintText == null)
            return;

        _holdHintText.enabled = _visible;

        if (_holdHintText.gameObject != gameObject)
            _holdHintText.gameObject.SetActive(_visible);

        if (!_visible && _holdHintRect != null)
            _holdHintRect.localScale = Vector3.one;
    }

    [ContextMenu("Log Current Direction Pull UI Settings")]
    private void LogCurrentSettings()
    {
        Debug.Log(
            $"DirectionPullUI settings - Icon Anchored Position: {_iconAnchoredPosition}, " +
            $"Horizontal Pull Offset: {_horizontalPullOffset}, Up Pull Offset: {_upPullOffset}, " +
            $"Down Pull Offset: {_downPullOffset}, Side Vertical Offset: {_sideVerticalOffset}, " +
            $"Side Button Height Multiplier: {_sideButtonHeightMultiplier}, Side Pull Height Centering: {_sidePullHeightCentering}, " +
            $"Use Canvas Relative Offsets: {_useCanvasRelativeOffsets}, Horizontal Canvas Offset: {_horizontalCanvasOffset}, " +
            $"Up Canvas Offset: {_upCanvasOffset}, Down Canvas Offset: {_downCanvasOffset}"
        );
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

    private void LogMissingReference(string _referenceName, ref bool _hasLogged)
    {
        if (!_logMissingReferences || _hasLogged)
            return;

        Debug.LogWarning($"[FishDirectionPullUI] Falta {_referenceName}. Crie esse objeto na cena/prefab ou arraste no Inspector. Ative Allow Runtime Fallback apenas se quiser cria-lo em runtime.", this);
        _hasLogged = true;
    }

    #endregion
}
